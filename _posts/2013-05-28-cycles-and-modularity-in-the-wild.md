---
layout: post
title: "Cycles and modularity in the wild"
description: "Comparing some real-world metrics of C# and F# projects"
categories: []
seriesId: "Dependency cycles"
seriesOrder: 3
image: "/assets/img/specflow_svg.png"
---

(*Updated 2013-06-15. See comments at the end of the post*)

(*Updated 2014-04-12. A [follow up post](/posts/roslyn-vs-fsharp-compiler/) that applies the same analysis to Roslyn*)

(*Updated 2015-01-23. A much clearer version of this analysis [has been done by Evelina Gabasova](http://evelinag.com/blog/2014/06-09-comparing-dependency-networks/).
She knows what she is talking about, so I highly recommend you read her post first!*)

This is a follow up post to two earlier posts on [module organization](/posts/recipe-part3/) and [cyclic dependencies](/posts/cyclic-dependencies/).   

I thought it would be interesting to look at some real projects written in C# and F#, and see how they compare in modularity and number of cyclic dependencies.


## The plan

My plan was to take ten or so projects written in C# and ten or so projects written in F#, and somehow compare them.

I didn't want to spend too much time on this, and so rather than trying to analyze the source files, I thought I would cheat a little and analyze the compiled assemblies, using the [Mono.Cecil](http://www.mono-project.com/Cecil) library.

This also meant that I could get the binaries directly, using NuGet.

The projects I picked were:

*C# projects*

* [Mono.Cecil](http://nuget.org/packages/Mono.Cecil/), which inspects programs and libraries in the ECMA CIL format. 
* [NUnit](http://nuget.org/packages/NUnit/)
* [SignalR](http://nuget.org/packages/Microsoft.AspNet.SignalR/) for real-time web functionality.
* [NancyFx](http://nuget.org/packages/Nancy/), a web framework
* [YamlDotNet](http://nuget.org/packages/YamlDotNet.Core/), for parsing and emitting YAML.
* [SpecFlow](http://nuget.org/packages/SpecFlow/), a BDD tool.
* [Json.NET](http://nuget.org/packages/Newtonsoft.Json/).
* [Entity Framework](http://nuget.org/packages/EntityFramework/5.0.0).
* [ELMAH](http://nuget.org/packages/elmah/), a logging framework for ASP.NET.
* [NuGet](http://nuget.org/packages/Nuget.Core/) itself. 
* [Moq](http://nuget.org/packages/Moq/), a mocking framework. 
* [NDepend](http://ndepend.com), a code analysis tool. 
* And, to show I'm being fair, a business application that I wrote in C#.


*F# projects*

Unfortunately, there is not yet a wide variety of F# projects to choose from. I picked the following:

* [FSharp.Core](http://nuget.org/packages/FSharp.Core/), the core F# library.
* [FSPowerPack](http://nuget.org/packages/FSPowerPack.Community/).
* [FsUnit](http://nuget.org/packages/FsUnit/), extensions for NUnit.
* [Canopy](http://nuget.org/packages/canopy/), a wrapper around the Selenium test automation tool.
* [FsSql](http://nuget.org/packages/FsSql/), a nice little ADO.NET wrapper.
* [WebSharper](http://nuget.org/packages/WebSharper/2.4.85.235), the web framework.
* [TickSpec](http://nuget.org/packages/TickSpec/), a BDD tool.
* [FSharpx](http://nuget.org/packages/FSharpx.Core/), an F# library.
* [FParsec](http://nuget.org/packages/FParsec/), a parser library.
* [FsYaml](http://nuget.org/packages/FsYaml/), a YAML library built on FParsec.
* [Storm](http://storm.codeplex.com/releases/view/18871), a tool for testing web services. 
* [Foq](http://nuget.org/packages/Foq/), a mocking framework. 
* Another business application that I wrote, this time in F#.

I did choose SpecFlow and TickSpec as being directly comparable, and also Moq and and Foq.

But as you can see, most of the F# projects are not directly comparable to the C# ones. For example, there is no direct F# equivalent to Nancy, or Entity Framework.

Nevertheless, I was hoping that I might observe some sort of pattern by comparing the projects. And I was right. Read on for the results!


## What metrics to use?

I wanted to examine two things: "modularity" and "cyclic dependencies". 

First, what should be the unit of "modularity"? 

From a coding point of view, we generally work with files ([Smalltalk being a notable exception](http://stackoverflow.com/questions/3561145/what-is-a-smalltalk-image)), and so it makes sense to think of the *file* as the unit of modularity. A file is used to group related items together, and if two chunks of code are in different files, they are somehow not as "related" as if they were in the same file.

In C#, the best practice is to have one class per file. So 20 files means 20 classes.  Sometimes classes have nested classes, but with rare exceptions, the nested class is in the same file as the parent class.  This means that we can ignore them and just use top-level classes as our unit of modularity, as a proxy for files.

In F#, the best practice is to have one *module* per file (or sometimes more). So 20 files means 20 modules. Behind the scenes, modules are turned into static classes, and any classes defined within the module are turned into nested classes. So again, this means that we can ignore nested classes and just use top-level classes as our unit of modularity.

The C# and F# compilers generate many "hidden" types, for things such as LINQ, lambdas, etc. In some cases, I wanted to exclude these, and only include "authored" types, which have been coded for explicitly. 
I also excluded the case classes generated by F# discriminated unions from being "authored" classes as well. That means that a union type with three cases will be counted as one authored type rather than four.

So my definition of a *top-level type* is: a type that is not nested and which is not compiler generated.

The metrics I chose for modularity were:

* **The number of top-level types** as defined above. 
* **The number of authored types** as defined above.
* **The number of all types**. This number would include the compiler generated types as well.  Comparing this number to the top-level types gives us some idea of how representative the top-level types are.
* **The size of the project**. Obviously, there will be more types in a larger project, so we need to make adjustments based on the size of the project. The size metric I picked was the number of instructions, rather than the physical size of the file. This eliminates issues with embedded resources, etc.

### Dependencies

Once we have our units of modularity, we can look at dependencies between modules. 

For this analysis, I only want to include dependencies between types in the same assembly. In other words, dependencies on system types such as `String` or `List` do not count as a dependency.

Let's say we have a top-level type `A` and another top-level type `B`. Then I say that a *dependency* exists from `A` to `B` if:

* Type `A` or any of its nested types inherits from (or implements) type `B` or any of its nested types.
* Type `A` or any of its nested types has a field, property or method that references type `B` or any of its nested types as a parameter or return value. This includes private members as well -- after all, it is still a dependency.
* Type `A` or any of its nested types has a method implementation that references type `B` or any of its nested types. 

This might not be a perfect definition.  But it is good enough for my purposes.

In addition to all dependencies, I thought it might be useful to look at "public" or "published" dependencies. A *public dependency* from `A` to `B` exists if:

* Type `A` or any of its nested types inherits from (or implements) type `B` or any of its nested types.
* Type `A` or any of its nested types has a *public* field, property or method that references type `B` or any of its nested types as a parameter or return value. 
* Finally, a public dependency is only counted if the source type itself is public.

The metrics I chose for dependencies were:

* **The total number of dependencies**. This is simply the sum of all dependencies of all types. Again, there will be more dependencies in a larger project, but we will also take the size of the project into account.
* **The number of types that have more than X dependencies**. This gives us an idea of how many types are "too" complex.

### Cyclic dependencies

Given this definition of dependency, then, a *cyclic dependency* occurs when two different top-level types depend on each other. 

Note what *not* included in this definition. If a nested type in a module depends on another nested type in the *same* module, then that is not a cyclic dependency.

If there is a cyclic dependency, then there is a set of modules that are all linked together. For example, if `A` depends on `B`, `B` depends on `C`, and then say, `C` depends on `A`, then `A`, `B` and `C` are linked together. In graph theory, this is called a *strongly connected component*.

The metrics I chose for cyclic dependencies were:

* **The number of cycles**. That is, the number of strongly connected components which had more than one module in them. 
* **The size of the largest component**. This gives us an idea of how complex the dependencies are.

I analyzed cyclic dependencies for all dependencies and also for public dependencies only.

## Doing the experiment

First, I downloaded each of the project binaries using NuGet. Then I wrote a little F# script that did the following steps for each assembly:

1. Analyzed the assembly using [Mono.Cecil](http://www.mono-project.com/Cecil) and extracted all the types, including the nested types
1. For each type, extracted the public and implementation references to other types, divided into internal (same assembly) and external (different assembly).
1. Created a list of the "top level" types.
1. Created a dependency list from each top level type to other top level types, based on the lower level dependencies.

This dependency list was then used to extract various statistics, shown below. I also rendered the dependency graphs to SVG format (using [graphViz](http://www.graphviz.org/)).

For cycle detection, I used the [QuickGraph library](http://quickgraph.codeplex.com/) to extract the strongly connected components, and then did some more processing and rendering.

If you want the gory details, here is [a link to the script](https://gist.github.com/swlaschin/5742974) that I used, and [here is the raw data](https://gist.github.com/swlaschin/5742994).

It is important to recognize that this is *not* a proper statistical study, just a quick analysis. However the results are quite interesting, as we shall see.

## Modularity

Let's look at the modularity first.

Here are the modularity-related results for the C# projects:

<table class="table table-striped table-condensed">
<thead>
<tr><th>Project</th><th>Code size</th><th>Top-level types</th><th>Authored types</th><th>All types</th><th>Code/Top</th><th>Code/Auth</th><th>Code/All</th><th>Auth/Top</th><th>All/Top</th></tr>
</thead>
<tbody>
<tr><td>ef	</td><td>269521	</td><td>514	</td><td>565	</td><td>876	</td><td>524	</td><td>477	</td><td>308	</td><td>1.1	</td><td>1.7	</td></tr>
<tr><td>jsonDotNet	</td><td>148829	</td><td>215	</td><td>232	</td><td>283	</td><td>692	</td><td>642	</td><td>526	</td><td>1.1	</td><td>1.3	</td></tr>
<tr><td>nancy	</td><td>143445	</td><td>339	</td><td>366	</td><td>560	</td><td>423	</td><td>392	</td><td>256	</td><td>1.1	</td><td>1.7	</td></tr>
<tr><td>cecil	</td><td>101121	</td><td>240	</td><td>245	</td><td>247	</td><td>421	</td><td>413	</td><td>409	</td><td>1.0	</td><td>1.0	</td></tr>
<tr><td>nuget	</td><td>114856	</td><td>216	</td><td>237	</td><td>381	</td><td>532	</td><td>485	</td><td>301	</td><td>1.1	</td><td>1.8	</td></tr>
<tr><td>signalR	</td><td>65513	</td><td>192	</td><td>229	</td><td>311	</td><td>341	</td><td>286	</td><td>211	</td><td>1.2	</td><td>1.6	</td></tr>
<tr><td>nunit	</td><td>45023	</td><td>173	</td><td>195	</td><td>197	</td><td>260	</td><td>231	</td><td>229	</td><td>1.1	</td><td>1.1	</td></tr>
<tr><td>specFlow	</td><td>46065	</td><td>242	</td><td>287	</td><td>331	</td><td>190	</td><td>161	</td><td>139	</td><td>1.2	</td><td>1.4	</td></tr>
<tr><td>elmah	</td><td>43855	</td><td>116	</td><td>140	</td><td>141	</td><td>378	</td><td>313	</td><td>311	</td><td>1.2	</td><td>1.2	</td></tr>
<tr><td>yamlDotNet	</td><td>23499	</td><td>70	</td><td>73	</td><td>73	</td><td>336	</td><td>322	</td><td>322	</td><td>1.0	</td><td>1.0	</td></tr>
<tr><td>fparsecCS	</td><td>57474	</td><td>41	</td><td>92	</td><td>93	</td><td>1402	</td><td>625	</td><td>618	</td><td>2.2	</td><td>2.3	</td></tr>
<tr><td>moq	</td><td>133189	</td><td>397	</td><td>420	</td><td>533	</td><td>335	</td><td>317	</td><td>250	</td><td>1.1	</td><td>1.3	</td></tr>
<tr><td>ndepend	</td><td>478508	</td><td>734	</td><td>828	</td><td>843	</td><td>652	</td><td>578	</td><td>568	</td><td>1.1	</td><td>1.1	</td></tr>
<tr><td>ndependPlat	</td><td>151625	</td><td>185	</td><td>205	</td><td>205	</td><td>820	</td><td>740	</td><td>740	</td><td>1.1	</td><td>1.1	</td></tr>
<tr><td>personalCS	</td><td>422147	</td><td>195	</td><td>278	</td><td>346	</td><td>2165	</td><td>1519	</td><td>1220	</td><td>1.4	</td><td>1.8	</td></tr>
<tr><td>TOTAL	</td><td>2244670	</td><td>3869	</td><td>4392	</td><td>5420	</td><td>580	</td><td>511	</td><td>414	</td><td>1.1	</td><td>1.4	</td></tr>
</tbody>
</table>

And here are the results for the F# projects:

<table class="table table-striped table-condensed">
<thead>
<tr><th>Project</th><th>Code size</th><th>Top-level types</th><th>Authored types</th><th>All types</th><th>Code/Top</th><th>Code/Auth</th><th>Code/All</th><th>Auth/Top</th><th>All/Top</th></tr>
</thead>
<tbody>
<tr><td>fsxCore	</td><td>339596	</td><td>173	</td><td>328	</td><td>2024	</td><td>1963	</td><td>1035	</td><td>168	</td><td>1.9	</td><td>11.7	</td></tr>
<tr><td>fsCore	</td><td>226830	</td><td>154	</td><td>313	</td><td>1186	</td><td>1473	</td><td>725	</td><td>191	</td><td>2.0	</td><td>7.7	</td></tr>
<tr><td>fsPowerPack	</td><td>117581	</td><td>93	</td><td>150	</td><td>410	</td><td>1264	</td><td>784	</td><td>287	</td><td>1.6	</td><td>4.4	</td></tr>
<tr><td>storm	</td><td>73595	</td><td>67	</td><td>70	</td><td>405	</td><td>1098	</td><td>1051	</td><td>182	</td><td>1.0	</td><td>6.0	</td></tr>
<tr><td>fParsec	</td><td>67252	</td><td>8	</td><td>24	</td><td>245	</td><td>8407	</td><td>2802	</td><td>274	</td><td>3.0	</td><td>30.6	</td></tr>
<tr><td>websharper	</td><td>47391	</td><td>52	</td><td>128	</td><td>285	</td><td>911	</td><td>370	</td><td>166	</td><td>2.5	</td><td>5.5	</td></tr>
<tr><td>tickSpec	</td><td>30797	</td><td>34	</td><td>49	</td><td>170	</td><td>906	</td><td>629	</td><td>181	</td><td>1.4	</td><td>5.0	</td></tr>
<tr><td>websharperHtml	</td><td>14787	</td><td>18	</td><td>28	</td><td>72	</td><td>822	</td><td>528	</td><td>205	</td><td>1.6	</td><td>4.0	</td></tr>
<tr><td>canopy	</td><td>15105	</td><td>6	</td><td>16	</td><td>103	</td><td>2518	</td><td>944	</td><td>147	</td><td>2.7	</td><td>17.2	</td></tr>
<tr><td>fsYaml	</td><td>15191	</td><td>7	</td><td>11	</td><td>160	</td><td>2170	</td><td>1381	</td><td>95	</td><td>1.6	</td><td>22.9	</td></tr>
<tr><td>fsSql	</td><td>15434	</td><td>13	</td><td>18	</td><td>162	</td><td>1187	</td><td>857	</td><td>95	</td><td>1.4	</td><td>12.5	</td></tr>
<tr><td>fsUnit	</td><td>1848	</td><td>2	</td><td>3	</td><td>7	</td><td>924	</td><td>616	</td><td>264	</td><td>1.5	</td><td>3.5	</td></tr>
<tr><td>foq	</td><td>26957	</td><td>35	</td><td>48	</td><td>103	</td><td>770	</td><td>562	</td><td>262	</td><td>1.4	</td><td>2.9	</td></tr>
<tr><td>personalFS	</td><td>118893	</td><td>30	</td><td>146	</td><td>655	</td><td>3963	</td><td>814	</td><td>182	</td><td>4.9	</td><td>21.8	</td></tr>
<tr><td>TOTAL	</td><td>1111257	</td><td>692	</td><td>1332	</td><td>5987	</td><td>1606	</td><td>834	</td><td>186	</td><td>1.9	</td><td>8.7	</td></tr>

</tbody>
</table>

The columns are:

* **Code size** is the number of CIL instructions from all methods, as reported by Cecil. 
* **Top-level types** is the total number of top-level types in the assembly, using the definition above.
* **Authored types** is the total number of types in the assembly, including nested types, enums, and so on, but excluding compiler generated types.
* **All types** is the total number of types in the assembly, including compiler generated types.

I have extended these core metrics with some extra calculated columns:

* **Code/Top** is the number of CIL instructions per top level type / module. This is a measure of how much code is associated with each unit of modularity. Generally, more is better, because you don't want to have to deal with multiple files if you don't have too. On the other hand, there is a trade off. Too many lines of code in a file makes reading the code impossible.  In both C# and F#, good practice is not to have more than 500-1000 lines of code per file, and with a few exceptions, that seems to be the case in the source code that I looked at.  
* **Code/Auth** is the number of CIL instructions per authored type.  This is a measure of how "big" each authored type is.
* **Code/All** is the number of CIL instructions per type.  This is a measure of how "big" each type is.
* **Auth/Top** is the ratio of all authored types to the top-level-types. It is a rough measure of how many authored types are in each unit of modularity.
* **All/Top** is the ratio of all types to the top-level-types. It is a rough measure of how many types are in each unit of modularity.

### Analysis

The first thing I noticed is that, with a few exceptions, the code size is bigger for the C# projects than for the F# projects.  Partly that is because I picked bigger projects, of course. But even for a somewhat comparable project like SpecFlow vs. TickSpec, the SpecFlow code size is bigger. It may well be that SpecFlow does a lot more than TickSpec, of course, but it also may be a result of using more generic code in F#. There is not enough information to know either way right now -- it would be interesting to do a true side by side comparison.

Next, the number of top-level types. I said earlier that this should correspond to the number of files in a project. Does it?

I didn't get all the sources for all the projects to do a thorough check, but I did a couple of spot checks. For example, for Nancy, there are 339 top level classes, which implies that there should be about 339 files. In fact, there are actually 322 .cs files, so not a bad estimate. 

On the other hand, for SpecFlow there are 242 top level types, but only 171 .cs files, so a bit of an overestimate there. And for Cecil, the same thing: 240 top level classes but only 128 .cs files.

For the FSharpX project, there are 173 top level classes, which implies there should be about 173 files. In fact, there are actually only 78 .fs files, so it is a serious over-estimate by a factor of more than 2.  And if we look at Storm, there are 67 top level classes. In fact, there are actually only 35 .fs files, so again it is an over-estimate by a factor of 2.  

So it looks like the number of top level classes is always an over-estimate of the number of files, but much more so for F# than for C#. It would be worth doing some more detailed analysis in this area.

### Ratio of code size to number of top-level types

The "Code/Top" ratio is consistently bigger for F# code than for C# code. Overall, the average top-level type in C# is converted into 580 instructions. But for F# that number is 1606 instructions, about three times as many.

I expect that this is because F# code is more concise than C# code. I would guess that 500 lines of F# code in a single module would create many more CIL instructions than 500 lines of C# code in a class.

If we visually plot "Code size" vs. "Top-level types", we get this chart:

![](/assets/img/Metrics_CodeSize_TopLevel.png)

What's surprising to me is how distinct the F# and C# projects are in this chart. The C# projects seem to have a consistent ratio of about 1-2 top-level types per 1000 instructions, even across different project sizes.
And the F# projects are consistent too, having a ratio of about 0.6 top-level types per 1000 instructions. 

In fact, the number of top level types in F# projects seems to taper off as projects get larger, rather than increasing linearly like the C# projects.

The message I get from this chart is that, for a given size of project, an F# implementation will have fewer modules, and presumably less complexity as a result. 

You probably noticed that there are two anomalies. Two C# projects are out of place -- the one at the 50K mark is FParsecCS and the one at the 425K mark is my business application.

I am fairly certain that this because both these implementations have some rather large C# classes in them, which helps the code ratio. Probably a necessarily evil for a parser, but in the case of my business application,
I know that it is due to cruft accumulating over the years, and there are some massive classes that ought to be refactored into smaller ones. So a metric like this is probably a *bad* sign for a C# code base.

### Ratio of code size to number of all types

On the other hand, if we compare the ratio of code to all types, including compiler generated ones, we get a very different result.

Here's the corresponding chart of "Code size" vs. "All types":

![](/assets/img/Metrics_CodeSize_AllTypes.png)

This is surprisingly linear for F#. The total number of types (including compiler generated ones) seems to depend closely on the size of the project.
On the other hand, the number of types for C# seems to vary a lot.

The average "size" of a type is somewhat smaller for F# code than for C# code.  The average type in C# is converted into about 400 instructions. But for F# that number is about 180 instructions.  

I'm not sure why this is. Is it because the F# types are more fine-grained, or could it be because the F# compiler generates many more little types than the C# compiler? Without doing a more subtle analysis, I can't tell.

### Ratio of top-level types to authored types

Having compared the type counts to the code size, let's now compare them to each other:

![](/assets/img/Metrics_TopLevel_AuthTypes.png)

Again, there is a significant difference. For each unit of modularity in C# there are an average of 1.1 authored types. But in F# the average is 1.9, and for some projects a lot more than that. 

Of course, creating nested types is trivial in F#, and quite uncommon in C#, so you could argue that this is not a fair comparison.
But surely the ability to create [a dozen types in as many lines](/posts/conciseness-type-definitions/) of F# has some effect on the quality of the design?
This is harder to do in C#, but there is nothing to stop you. So might this not mean that there is a temptation in C# to not be as fine-grained as you could potentially be?  

The project with the highest ratio (4.9) is my F# business application. I believe that this is due to this being only F# project in this list which is designed around a specific business domain,
I created many "little" types to model the domain accurately, using the concepts [described here](/series/designing-with-types.html). For other projects created using DDD principles,
I would expect to see this same high number.

## Dependencies

Now let's look at the dependency relationships between the top level classes.

Here are the results for the C# projects:

<table class="table table-striped table-condensed">
<thead>
<tr><th>Project</th><th>Top Level Types	</th><th>Total Dep. Count	</th><th>Dep/Top	</th><th>One or more dep.</th><th>Three or more dep.</th><th>Five or more dep.	</th><th>Ten or more dep.</th><th>Diagram</th></tr>
</thead>
<tbody>
<tr><td>ef	</td><td>514	</td><td>2354	</td><td>4.6	</td><td>76%	</td><td>51%	</td><td>32%	</td><td>13%	</td><td><a href='/assets/svg/ef.all.dot.svg'>svg</a> <a href='/assets/svg/ef.all.dot'>dotfile</a>	</td></tr>
<tr><td>jsonDotNet	</td><td>215	</td><td>913	</td><td>4.2	</td><td>69%	</td><td>42%	</td><td>30%	</td><td>14%	</td><td><a href='/assets/svg/jsonDotNet.all.dot.svg'>svg</a> <a href='/assets/svg/jsonDotNet.all.dot'>dotfile</a>	</td></tr>
<tr><td>nancy	</td><td>339	</td><td>1132	</td><td>3.3	</td><td>78%	</td><td>41%	</td><td>22%	</td><td>6%	</td><td><a href='/assets/svg/nancy.all.dot.svg'>svg</a> <a href='/assets/svg/nancy.all.dot'>dotfile</a>	</td></tr>
<tr><td>cecil	</td><td>240	</td><td>1145	</td><td>4.8	</td><td>73%	</td><td>43%	</td><td>23%	</td><td>13%	</td><td><a href='/assets/svg/cecil.all.dot.svg'>svg</a> <a href='/assets/svg/cecil.all.dot'>dotfile</a>	</td></tr>
<tr><td>nuget	</td><td>216	</td><td>833	</td><td>3.9	</td><td>71%	</td><td>43%	</td><td>26%	</td><td>12%	</td><td><a href='/assets/svg/nuget.all.dot.svg'>svg</a> <a href='/assets/svg/nuget.all.dot'>dotfile</a>	</td></tr>
<tr><td>signalR	</td><td>192	</td><td>641	</td><td>3.3	</td><td>66%	</td><td>34%	</td><td>19%	</td><td>10%	</td><td><a href='/assets/svg/signalR.all.dot.svg'>svg</a> <a href='/assets/svg/signalR.all.dot'>dotfile</a>	</td></tr>
<tr><td>nunit	</td><td>173	</td><td>499	</td><td>2.9	</td><td>75%	</td><td>39%	</td><td>13%	</td><td>4%	</td><td><a href='/assets/svg/nunit.all.dot.svg'>svg</a> <a href='/assets/svg/nunit.all.dot'>dotfile</a>	</td></tr>
<tr><td>specFlow	</td><td>242	</td><td>578	</td><td>2.4	</td><td>64%	</td><td>25%	</td><td>17%	</td><td>5%	</td><td><a href='/assets/svg/specFlow.all.dot.svg'>svg</a> <a href='/assets/svg/specFlow.all.dot'>dotfile</a>	</td></tr>
<tr><td>elmah	</td><td>116	</td><td>300	</td><td>2.6	</td><td>72%	</td><td>28%	</td><td>22%	</td><td>6%	</td><td><a href='/assets/svg/elmah.all.dot.svg'>svg</a> <a href='/assets/svg/elmah.all.dot'>dotfile</a>	</td></tr>
<tr><td>yamlDotNet	</td><td>70	</td><td>228	</td><td>3.3	</td><td>83%	</td><td>30%	</td><td>11%	</td><td>4%	</td><td><a href='/assets/svg/yamlDotNet.all.dot.svg'>svg</a> <a href='/assets/svg/yamlDotNet.all.dot'>dotfile</a>	</td></tr>
<tr><td>fparsecCS	</td><td>41	</td><td>64	</td><td>1.6	</td><td>59%	</td><td>29%	</td><td>5%	</td><td>0%	</td><td><a href='/assets/svg/fparsecCS.all.dot.svg'>svg</a> <a href='/assets/svg/fparsecCS.all.dot'>dotfile</a>	</td></tr>
<tr><td>moq	</td><td>397	</td><td>1100	</td><td>2.8	</td><td>63%	</td><td>29%	</td><td>17%	</td><td>7%	</td><td><a href='/assets/svg/moq.all.dot.svg'>svg</a> <a href='/assets/svg/moq.all.dot'>dotfile</a>	</td></tr>
<tr><td>ndepend	</td><td>734	</td><td>2426	</td><td>3.3	</td><td>67%	</td><td>37%	</td><td>25%	</td><td>10%	</td><td><a href='/assets/svg/ndepend.all.dot.svg'>svg</a> <a href='/assets/svg/ndepend.all.dot'>dotfile</a>	</td></tr>
<tr><td>ndependPlat	</td><td>185	</td><td>404	</td><td>2.2	</td><td>67%	</td><td>24%	</td><td>11%	</td><td>4%	</td><td><a href='/assets/svg/ndependPlat.all.dot.svg'>svg</a> <a href='/assets/svg/ndependPlat.all.dot'>dotfile</a>	</td></tr>
<tr><td>personalCS	</td><td>195	</td><td>532	</td><td>2.7	</td><td>69%	</td><td>29%	</td><td>19%	</td><td>7%	</td><td>	</td></tr>
<tr><td>TOTAL	</td><td>3869	</td><td>13149	</td><td>3.4	</td><td>70%	</td><td>37%	</td><td>22%	</td><td>9%	</td><td>	</td></tr>

</tbody>
</table>

And here are the results for the F# projects:

<table class="table table-striped table-condensed">
<thead>
<tr><th>Project</th><th>Top Level Types	</th><th>Total Dep. Count	</th><th>Dep/Top	</th><th>One or more dep.</th><th>Three or more dep.</th><th>Five or more dep.	</th><th>Ten or more dep.</th><th>Diagram</th></tr>
</thead>
<tbody>
<tr><td>fsxCore	</td><td>173	</td><td>76	</td><td>0.4	</td><td>30%	</td><td>4%	</td><td>1%	</td><td>0%	</td><td><a href='/assets/svg/fsxCore.all.dot.svg'>svg</a> <a href='/assets/svg/fsxCore.all.dot'>dotfile</a>	</td></tr>
<tr><td>fsCore	</td><td>154	</td><td>287	</td><td>1.9	</td><td>55%	</td><td>26%	</td><td>14%	</td><td>3%	</td><td><a href='/assets/svg/fsCore.all.dot.svg'>svg</a> <a href='/assets/svg/fsCore.all.dot'>dotfile</a>	</td></tr>
<tr><td>fsPowerPack	</td><td>93	</td><td>68	</td><td>0.7	</td><td>38%	</td><td>13%	</td><td>2%	</td><td>0%	</td><td><a href='/assets/svg/fsPowerPack.all.dot.svg'>svg</a> <a href='/assets/svg/fsPowerPack.all.dot'>dotfile</a>	</td></tr>
<tr><td>storm	</td><td>67	</td><td>195	</td><td>2.9	</td><td>72%	</td><td>40%	</td><td>18%	</td><td>4%	</td><td><a href='/assets/svg/storm.all.dot.svg'>svg</a> <a href='/assets/svg/storm.all.dot'>dotfile</a>	</td></tr>
<tr><td>fParsec	</td><td>8	</td><td>9	</td><td>1.1	</td><td>63%	</td><td>25%	</td><td>0%	</td><td>0%	</td><td><a href='/assets/svg/fParsec.all.dot.svg'>svg</a> <a href='/assets/svg/fParsec.all.dot'>dotfile</a>	</td></tr>
<tr><td>websharper	</td><td>52	</td><td>18	</td><td>0.3	</td><td>31%	</td><td>0%	</td><td>0%	</td><td>0%	</td><td><a href='/assets/svg/websharper.all.dot.svg'>svg</a> <a href='/assets/svg/websharper.all.dot'>dotfile</a>	</td></tr>
<tr><td>tickSpec	</td><td>34	</td><td>48	</td><td>1.4	</td><td>50%	</td><td>15%	</td><td>9%	</td><td>3%	</td><td><a href='/assets/svg/tickSpec.all.dot.svg'>svg</a> <a href='/assets/svg/tickSpec.all.dot'>dotfile</a>	</td></tr>
<tr><td>websharperHtml	</td><td>18	</td><td>37	</td><td>2.1	</td><td>78%	</td><td>39%	</td><td>6%	</td><td>0%	</td><td><a href='/assets/svg/websharperHtml.all.dot.svg'>svg</a> <a href='/assets/svg/websharperHtml.all.dot'>dotfile</a>	</td></tr>
<tr><td>canopy	</td><td>6	</td><td>8	</td><td>1.3	</td><td>50%	</td><td>33%	</td><td>0%	</td><td>0%	</td><td><a href='/assets/svg/canopy.all.dot.svg'>svg</a> <a href='/assets/svg/canopy.all.dot'>dotfile</a>	</td></tr>
<tr><td>fsYaml	</td><td>7	</td><td>10	</td><td>1.4	</td><td>71%	</td><td>14%	</td><td>0%	</td><td>0%	</td><td><a href='/assets/svg/fsYaml.all.dot.svg'>svg</a> <a href='/assets/svg/fsYaml.all.dot'>dotfile</a>	</td></tr>
<tr><td>fsSql	</td><td>13	</td><td>14	</td><td>1.1	</td><td>54%	</td><td>8%	</td><td>8%	</td><td>0%	</td><td><a href='/assets/svg/fsSql.all.dot.svg'>svg</a> <a href='/assets/svg/fsSql.all.dot'>dotfile</a>	</td></tr>
<tr><td>fsUnit	</td><td>2	</td><td>0	</td><td>0.0	</td><td>0%	</td><td>0%	</td><td>0%	</td><td>0%	</td><td><a href='/assets/svg/fsUnit.all.dot.svg'>svg</a> <a href='/assets/svg/fsUnit.all.dot'>dotfile</a>	</td></tr>
<tr><td>foq	</td><td>35	</td><td>66	</td><td>1.9	</td><td>66%	</td><td>29%	</td><td>11%	</td><td>0%	</td><td><a href='/assets/svg/foq.all.dot.svg'>svg</a> <a href='/assets/svg/foq.all.dot'>dotfile</a>	</td></tr>
<tr><td>personalFS	</td><td>30	</td><td>111	</td><td>3.7	</td><td>93%	</td><td>60%	</td><td>27%	</td><td>7%	</td><td></td></tr>
<tr><td>TOTAL	</td><td>692	</td><td>947	</td><td>1.4	</td><td>49%	</td><td>19%	</td><td>8%	</td><td>1%	</td><td>	</td></tr>

</tbody>
</table>
     
The columns are:

* **Top-level types** is the total number of top-level types in the assembly, as before.
* **Total dep. count** is the total number of dependencies between top level types.
* **Dep/Top** is the number of dependencies per top level type / module only. This is a measure of how many dependencies the average top level type/module has.  
* **One or more dep** is the number of top level types that have dependencies on one or more other top level types.  
* **Three or more dep**. Similar to above, but with dependencies on three or more other top level types.  
* **Five or more dep**. Similar to above. 
* **Ten or more dep**. Similar to above. Top level types with this many dependencies will be harder to understand and maintain. So this is measure of how complex the project is. 

The **diagram** column contains a link to a SVG file, generated from the dependencies, and also the [DOT file](http://www.graphviz.org/) that was used to generate the SVG.
See below for a discussion of these diagrams.
(Note that I can't expose the internals of my applications, so I will just give the metrics)


### Analysis

These results are very interesting. For C#, the number of total dependencies increases with project size. Each top-level type depends on 3-4 others, on average.

On the other hand, the number of total dependencies in an F# project does not seem to vary too much with project size at all. Each F# module depends on no more than 1-2 others, on average. 
And the largest project (FSharpX) has a lower ratio than many of the smaller projects. My business app and the Storm project are the only exceptions. 

Here's a chart of the relationship between code size and the number of dependencies:

![](/assets/img/Metrics_CodeSize_Dependencies.png)

The disparity between C# and F# projects is very clear.  The C# dependencies seem to grow linearly with project size, while the F# dependencies seem to be flat.

### Distribution of dependencies

The average number of dependencies per top level type is interesting, but it doesn't help us understand the variability. Are there many modules with lots of dependencies? Or does each one just have a few? 

This might make a difference in maintainability, perhaps. I would assume that a module with only one or two dependencies would be easier to understand in the context of the application that one with tens of dependencies.

Rather than doing a sophisticated statistical analysis, I thought I would keep it simple and just count how many top level types had one or more dependencies, three or more dependencies, and so on.

Here are the same results, displayed visually:

![](/assets/img/Metrics_CS_DependencyPercent.png)

![](/assets/img/Metrics_FS_DependencyPercent.png)


So, what can we deduce from these numbers?

* First, in the F# projects, more than half of the modules have no outside dependencies *at all*. This is a bit surprising, but I think it is due to the heavy use of generics compared with C# projects.

* Second, the modules in the F# projects consistently have fewer dependencies than the classes in the C# projects.

* Finally, in the F# projects, modules with a high number of dependencies are quite rare -- less than 2% overall. But in the C# projects, 9% of classes have more than 10 dependencies on other classes.

The worst offender in the F# group is my very own F# application, which is even worse than my C# application with respect to these metrics.
Again, it might be due to heavy use of non-generics in the form of domain-specific types, or it might just be that the code needs more refactoring!

### The dependency diagrams

It might be useful to look at the dependency diagrams now.  These are SVG files, so you should be able to view them in your browser.

Note that most of these diagrams are very big -- so after you open them you will need to zoom out quite a bit in order to see anything!

Let's start by comparing the diagrams for [SpecFlow](/assets/svg/specFlow.all.dot.svg) and [TickSpec](/assets/svg/tickSpec.all.dot.svg).

Here's the one for SpecFlow:

[![](/assets/img/specflow_svg.png)](/assets/svg/specFlow.all.dot.svg)

Here's the one for TickSpec:

[![](/assets/img/tickspec_svg.png)](/assets/svg/tickSpec.all.dot.svg)

Each diagram lists all the top-level types found in the project. If there is a dependency from one type to another, it is shown by an arrow.
The dependencies point from left to right where possible, so any arrows going from right to left implies that there is a cyclic dependency. 

The layout is done automatically by graphviz, but in general, the types are organized into columns or "ranks". For example, the SpecFlow diagram has 12 ranks, and the TickSpec diagram has five.

As you can see, there are generally a lot of tangled lines in a typical dependency diagram! How tangled the diagram looks is a sort of visual measure of the code complexity. 
For instance, if I was tasked to maintain the SpecFlow project, I wouldn't really feel comfortable until I had understood all the relationships between the classes. And the more complex the project, the longer it takes to come up to speed.

### OO vs functional design revealed?

The TickSpec diagram is a lot simpler than the SpecFlow one. Is that because TickSpec perhaps doesn't do as much as SpecFlow?

The answer is no, I don't think that it has anything to do with the size of the feature set at all, but rather because the code is organized differently. 

Looking at the SpecFlow classes ([dotfile](/assets/svg/specFlow.all.dot)), we can see it follows good OOD and TDD practices by creating interfaces.
There's a `TestRunnerManager` and an `ITestRunnerManager`, for example.
And there are many other patterns that commonly crop up in OOD: "listener" classes and interfaces, "provider" classes and interfaces, "comparer" classes and interfaces, and so on.

But if we look at the TickSpec modules ([dotfile](/assets/svg/tickSpec.all.dot)) there are no interfaces at all. And no "listeners", "providers" or "comparers" either.
There might well be a need for such things in the code, but either they are not exposed outside their module, or more likely, the role they play is fulfilled by functions rather than types.

I'm not picking on the SpecFlow code, by the way. It seems well designed, and is a very useful library, but I think it does highlight some of the differences between OO design and functional design.

### Moq compared with Foq

Let's also compare the diagrams for [Moq](/assets/svg/moq.all.dot.svg) and [Foq](/assets/svg/foq.all.dot.svg). These two projects do roughly the same thing, so the code should be comparable.

As before, the project written in F# has a much smaller dependency diagram. 

Looking at the Moq classes ([dotfile](/assets/svg/moq.all.dot)), we can see it includes the "Castle" library, which I didn't eliminate from the analysis.
Out of the 249 classes with dependencies, only 66 are Moq specific. If we had considered only the classes in the Moq namespace, we might have had a cleaner diagram.

On the other hand, looking at the Foq modules ([dotfile](/assets/svg/foq.all.dot)) there are only 23 modules with dependencies, fewer even than just the Moq classes alone.

So something is very different with code organization in F#.

### FParsec compared with FParsecCS

The FParsec project is an interesting natural experiment.  The project has two assemblies, roughly the same size, but one is written in C# and the other in F#.
 
It is a bit unfair to compare them directly, because the C# code is designed for parsing fast, while the F# code is more high level. But... I'm going to be unfair and compare them anyway!

Here are the diagrams for the F# assembly ["FParsec"](/assets/svg/fParsec.all.dot.svg) and C# assembly ["FParsecCS"](/assets/svg/fparsecCS.all.dot.svg). 

They are both nice and clear. Lovely code!

What's not clear from the diagram is that my methodology is being unfair to the C# assembly.

For example, the C# diagram shows that there are dependencies between `Operator`, `OperatorType`, `InfixOperator` and so on.
But in fact, looking at the source code, these classes are all in the same physical file.
In F#, they would all be in the same module, and their relationships would not count as public dependencies. So the C# code is being penalized in a way.

Even so, looking at the source code, the C# code has 20 source files compared to F#'s 8, so there is still some difference in complexity.

### What counts as a dependency?

In defence of my method though, the only thing that is keeping these FParsec C# classes together in the same file is good coding practice; it is not enforced by the C# compiler.
Another maintainer could come along and unwittingly separate them into different files, which really *would* increase the complexity. In F# you could not do that so easily, and certainly not accidentally. 

So it depends on what you mean by "module", and "dependency". In my view, a module contains things that really are "joined at the hip" and shouldn't easily be decoupled. Hence dependencies within a module don't
count, while dependencies between modules do.

Another way to think about it is that F# encourages high coupling in some areas (modules) in exchange for low coupling in others. In C#, the only kind of strict coupling available is class-based.
Anything looser, such as using namespaces, has to be enforced using good practices or a tool such as NDepend.

Whether the F# approach is better or worse depends on your preference. It does make certain kinds of refactoring harder as a result.

## Cyclic dependencies

Finally, we can turn our attention to the oh-so-evil cyclic dependencies. (If you want to know why they are bad, [read this post](/posts/cyclic-dependencies/) ).

Here are the cyclic dependency results for the C# projects.


<table class="table table-striped table-condensed">
<thead>
<tr><th>Project</th><th>Top-level types</th><th>Cycle count</th><th>Partic.</th><th>Partic.%</th><th>Max comp. size</th><th>Cycle count (public)</th><th>Partic. (public)</th><th>Partic.% (public)</th><th>Max comp. size (public)</th><th>Diagram</th></tr>
</thead>
<tbody>
<tr><td>ef	</td><td>514	</td><td>14	</td><td>123	</td><td>24%	</td><td>79	</td><td>1	</td><td>7	</td><td>1%	</td><td>7	</td><td><a href='/assets/svg/ef.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/ef.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>jsonDotNet	</td><td>215	</td><td>3	</td><td>88	</td><td>41%	</td><td>83	</td><td>1	</td><td>11	</td><td>5%	</td><td>11	</td><td><a href='/assets/svg/jsonDotNet.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/jsonDotNet.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>nancy	</td><td>339	</td><td>6	</td><td>35	</td><td>10%	</td><td>21	</td><td>2	</td><td>4	</td><td>1%	</td><td>2	</td><td><a href='/assets/svg/nancy.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/nancy.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>cecil	</td><td>240	</td><td>2	</td><td>125	</td><td>52%	</td><td>123	</td><td>1	</td><td>50	</td><td>21%	</td><td>50	</td><td><a href='/assets/svg/cecil.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/cecil.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>nuget	</td><td>216	</td><td>4	</td><td>24	</td><td>11%	</td><td>10	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td><a href='/assets/svg/nuget.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/nuget.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>signalR	</td><td>192	</td><td>3	</td><td>14	</td><td>7%	</td><td>7	</td><td>1	</td><td>5	</td><td>3%	</td><td>5	</td><td><a href='/assets/svg/signalR.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/signalR.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>nunit	</td><td>173	</td><td>2	</td><td>80	</td><td>46%	</td><td>78	</td><td>1	</td><td>48	</td><td>28%	</td><td>48	</td><td><a href='/assets/svg/nunit.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/nunit.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>specFlow	</td><td>242	</td><td>5	</td><td>11	</td><td>5%	</td><td>3	</td><td>1	</td><td>2	</td><td>1%	</td><td>2	</td><td><a href='/assets/svg/specFlow.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/specFlow.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>elmah	</td><td>116	</td><td>2	</td><td>9	</td><td>8%	</td><td>5	</td><td>1	</td><td>2	</td><td>2%	</td><td>2	</td><td><a href='/assets/svg/elmah.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/elmah.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>yamlDotNet	</td><td>70	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td><a href='/assets/svg/yamlDotNet.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/yamlDotNet.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>fparsecCS	</td><td>41	</td><td>3	</td><td>6	</td><td>15%	</td><td>2	</td><td>1	</td><td>2	</td><td>5%	</td><td>2	</td><td><a href='/assets/svg/fparsecCS.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/fparsecCS.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>moq	</td><td>397	</td><td>9	</td><td>50	</td><td>13%	</td><td>15	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td><a href='/assets/svg/moq.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/moq.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>ndepend	</td><td>734	</td><td>12	</td><td>79	</td><td>11%	</td><td>22	</td><td>8	</td><td>36	</td><td>5%	</td><td>7	</td><td><a href='/assets/svg/ndepend.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/ndepend.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>ndependPlat	</td><td>185	</td><td>2	</td><td>5	</td><td>3%	</td><td>3	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td><a href='/assets/svg/ndependPlat.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/ndependPlat.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>personalCS	</td><td>195	</td><td>11	</td><td>34	</td><td>17%	</td><td>8	</td><td>5	</td><td>19	</td><td>10%	</td><td>7	</td><td><a href='/assets/svg/personalCS.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/personalCS.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>TOTAL	</td><td>3869	</td><td>	</td><td>683	</td><td>18%	</td><td>	</td><td>	</td><td>186	</td><td>5%	</td><td>	</td><td><a href='/assets/svg/TOTAL.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/TOTAL.all.cycles.dot'>dotfile</a>	</td></tr>

</tbody>                                                     
</table>

And here are the results for the F# projects:

<table class="table table-striped table-condensed">
<thead>
<tr><th>Project</th><th>Top-level types</th><th>Cycle count</th><th>Partic.</th><th>Partic.%</th><th>Max comp. size</th><th>Cycle count (public)</th><th>Partic. (public)</th><th>Partic.% (public)</th><th>Max comp. size (public)</th><th>Diagram</th></tr>
</thead>
<tbody>
<tr><td>fsxCore	</td><td>173	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>fsCore	</td><td>154	</td><td>2	</td><td>5	</td><td>3%	</td><td>3	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td><a href='/assets/svg/fsCore.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/fsCore.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>fsPowerPack	</td><td>93	</td><td>1	</td><td>2	</td><td>2%	</td><td>2	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td><a href='/assets/svg/fsPowerPack.all.cycles.dot.svg'>svg</a> <a href='/assets/svg/fsPowerPack.all.cycles.dot'>dotfile</a>	</td></tr>
<tr><td>storm	</td><td>67	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>fParsec	</td><td>8	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>websharper	</td><td>52	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>0	</td><td>.	</td></tr>
<tr><td>tickSpec	</td><td>34	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>websharperHtml	</td><td>18	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>canopy	</td><td>6	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>fsYaml	</td><td>7	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>fsSql	</td><td>13	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>fsUnit	</td><td>2	</td><td>0	</td><td>0	</td><td>0%	</td><td>0	</td><td>0	</td><td>0	</td><td>0%	</td><td>0	</td><td>.	</td></tr>
<tr><td>foq	</td><td>35	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>personalFS	</td><td>30	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>0	</td><td>0	</td><td>0%	</td><td>1	</td><td>.	</td></tr>
<tr><td>TOTAL	</td><td>692	</td><td>	</td><td>7	</td><td>1%	</td><td>	</td><td>	</td><td>0	</td><td>0%	</td><td>	</td><td>.	</td></tr>
</tbody>
</table>

The columns are:

* **Top-level types** is the total number of top-level types in the assembly, as before.
* **Cycle count** is the number of cycles altogether. Ideally it would be zero. But larger is not necessarily worse. Better to have 10 small cycles than one giant one, I think.
* **Partic.**. The number of top level types that participate in any cycle.
* **Partic.%**. The number of top level types that participate in any cycle, as a percent of all types.
* **Max comp. size** is the number of top level types in the largest cyclic component.  This is a measure of how complex the cycle is. If there are only two mutually dependent types, then the cycle is a lot less complex than, say, 123 mutually dependent types.
* **... (public)** columns have the same definitions, but using only public dependencies.  I thought it would be interesting to see what effect it would have to limit the analysis to public dependencies only.
* The **diagram** column contains a link to a SVG file, generated from the dependencies in the cycles only, and also the [DOT file](http://www.graphviz.org/) that was used to generate the SVG. See below for an analysis.

### Analysis

If we are looking for cycles in the F# code, we will be sorely disappointed. Only two of the F# projects have cycles at all, and those are tiny. For example in FSharp.Core there is a mutual dependency between two types right next to each other in the same file, [here](https://github.com/fsharp/fsharp/blob/master/src/fsharp/FSharp.Core/quotations.fs#L146).

On the other hand, almost all the C# projects have one or more cycles. Entity Framework has the most cycles, involving 24% of the classes, and Cecil has the worst participation rate,
with over half of the classes being involved in a cycle. 

Even NDepend has cycles, although to be fair, there may be good reasons for this. First NDepend focuses on removing cycles between namespaces, not classes so much, and second,
it's possible that the cycles are between types declared in the same source file. As a result, my method may penalize well-organized C# code somewhat (as noted in the FParsec vs. FParsecCS discussion above).

![](/assets/img/Metrics_TopLevel_Participation.png)

Why the difference between C# and F#?  

* In C#, there is nothing stopping you from creating cycles -- a perfect example of accidental complexity. In fact, you have to make [a special effort](http://programmers.stackexchange.com/questions/60549/how-strictly-do-you-follow-the-no-dependency-cycle-rule-ndepend) to avoid them.  
* In F#, of course, it is the other way around. You can't easily create cycles at all. 

## My business applications compared

One more comparison. As part of my day job, I have written a number of business applications in C#, and more recently, in F#.
Unlike the other projects listed here, they are very focused on addressing a particular business need, with lots of domain specific code, custom business rules, special cases, and so on.

Both projects were produced under deadline, with changing requirements and all the usual real world constraints that stop you writing ideal code. Like most developers in my position,
I would love a chance to tidy them up and refactor them, but they do work, the business is happy, and I have to move on to new things. 

Anyway, let's see how they stack up to each other.  I can't reveal any details of the code other than the metrics, but I think that should be enough to be useful.

Taking the C# project first:

* It has 195 top level types, about 1 for every 2K of code. Comparing this with other C# projects, there should be *many more* top level types than this. And in fact, I know that this is true.
  As with many projects (this one is 6 years old) it is lower risk to just add a method to an existing class rather than refactoring it, especially under deadline.
  Keeping old code stable is always a higher priority than making it beautiful! The result is that classes grow too large over time.
* The flip side of having large classes is that there many fewer cross-class dependencies! It has some of the better scores among the C# projects.
  So it goes to show that dependencies aren't the only metric. There has to be a balance.
* In terms of cyclic dependencies, it's pretty typical for a C# project. There are a number of them (11) but the largest involves only 8 classes.

Now let's look at my F# project:

* It has 30 modules, about 1 for every 4K of code. Comparing this with other F# projects, it's not excessive, but perhaps a bit of refactoring is in order. 
  * As an aside, in my experience with maintaining this code, I have noticed that, unlike C# code, I don't feel that I *have* to add cruft to existing modules when feature requests come in.
  Instead, I find that in many cases, the faster and lower risk way of making changes is simply to create a *new* module and put all the code for a new feature in there. 
  Because the modules have no state, a function can live anywhere -- it is not forced to live in the same class.
  Over time this approach may create its own problems too (COBOL anyone?) but right now, I find it a breath of fresh air.
* The metrics show that there are an unusually large number of "authored" types per module (4.9). As I noted above, I think this is a result of having fine-grained DDD-style design.
  The code per authored type is in line with the other F# projects, so that implies they are not too big or small.
* Also, as I noted earlier, the inter-module dependencies are the worst of any F# project. I know that there are some API/service functions that depend on almost all the other modules, but this
  could be a clue that they might need refactoring. 
  * However, unlike C# code, I know exactly where to find these problem modules. I can be fairly certain that all these modules are in the top layer of my application and will thus appear at the bottom of the module list in Visual Studio.
  How can I be so sure? Because...
* In terms of cyclic dependencies, it's pretty typical for a F# project. There aren't any.


## Summary

I started this analysis from curiosity -- was there any meaningful difference in the organization of C# and F# projects? 

I was quite surprised that the distinction was so clear. Given these metrics, you could certainly predict which language the assembly was written in.

* **Project complexity**. For a given number of instructions, a C# project is likely to have many more top level types (and hence files) than an F# one -- more than double, it seems.
* **Fine-grained types**. For a given number of modules, a C# project is likely to have fewer authored types than an F# one, implying that the types are not as fine-grained as they could be.
* **Dependencies**. In a C# project, the number of dependencies between classes increases linearly with the size of the project. In an F# project, the number of dependencies is much smaller and stays  relatively flat.
* **Cycles**. In a C# project, cycles occur easily unless care is taken to avoid them. In an F# project, cycles are extremely rare, and if present, are very small.

Perhaps this has do with the competency of the programmer, rather than a difference between languages?
Well, first of all, I think that the quality of the C# projects is quite good on the whole -- I certainly wouldn't claim that I could write better code! 
And, in two cases in particular, the C# and F# projects were written by the same person, and differences were still apparent, so I don't think this argument holds up. 

## Future work

This approach of using *just* the binaries might have gone as far as it can go. For a more accurate analysis, we would need to use metrics from the source code as well (or maybe the pdb file).

For example, a high "instructions per type" metric is good if it corresponds to small source files (concise code), but not if it corresponds to large ones (bloated classes). Similarly, my definition of modularity used top-level types
rather than source files, which penalized C# somewhat over F#.  

So, I don't claim that this analysis is perfect (and I hope haven't made a terrible mistake in the analysis code!) but I think that it could be a useful starting point for further investigation.

<hr>

## Update 2013-06-15

This post caused quite a bit of interest. Based on feedback, I made the following changes:

**Assemblies profiled**

* Added Foq and Moq (at the request of Phil Trelford).
* Added the C# component of FParsec (at the request of Dave Thomas and others).
* Added two NDepend assemblies.
* Added two of my own projects, one C# and one F#.

As you can see, adding seven new data points (five C# and two F# projects) didn't change the overall analysis.

**Algorithm changes**

* Made definition of "authored" type stricter. Excluded types with "GeneratedCodeAttribute" and F# types that are subtypes of a sum type. This had an effect on the F# projects and reduced the "Auth/Top" ratio somewhat.

**Text changes**

* Rewrote some of the analysis.
* Removed the unfair comparison of YamlDotNet with FParsec.
* Added a comparison of the C# component and F# components of FParsec.
* Added a comparison of Moq and Foq.
* Added a comparison of my own two projects.

The orginal post is still available [here](/archives/cycles-and-modularity-in-the-wild_20130614.html)




