---
layout: post
title: "Other interesting ways of using F# at work"
description: "Twenty six low-risk ways to use F# at work (part 5)"
categories: []
seriesId: "Low-risk ways to use F# at work"
seriesOrder: 5

---

This post is the conclusion of the series on [low-risk and incremental ways to use F# at work](/posts/low-risk-ways-to-use-fsharp-at-work/).

To wrap up, we'll look at a few more ways in which F# can help you with various development tasks around the edges, without impacting any core or mission critical code.

## Series contents

Before moving on to the content of the post, here's the full list of the twenty six ways:

**Part 1 - Using F# to explore and develop interactively**

[1. Use F# to explore the .NET framework interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-net-interactively)<br />
[2. Use F# to test your own code interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-own-code-interactively)<br />
[3. Use F# to play with webservices interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-webservices-interactively)<br />
[4. Use F# to play with UI's interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-winforms-interactively)<br />

**Part 2 - Using F# for development and devops scripts**

[5. Use FAKE for build and CI scripts](/posts/low-risk-ways-to-use-fsharp-at-work-2/#fake)<br />
[6. An F# script to check that a website is responding](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-website-responding)<br />
[7. An F# script to convert an RSS feed into CSV](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-rss-to-csv)<br />
[8. An F# script that uses WMI to check the stats of a process](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-wmi-stats)<br />
[9. Use F# for configuring and managing the cloud](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-cloud)<br />

**Part 3 - Using F# for testing**

[10. Use F# to write unit tests with readable names](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-nunit)<br />
[11. Use F# to run unit tests programmatically](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-runner)<br />
[12. Use F# to learn to write unit tests in other ways](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-other)<br />
[13. Use FsCheck to write better unit tests](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-fscheck)<br />
[14. Use FsCheck to create random dummy data](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-dummy)<br />
[15. Use F# to create mocks](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-mock)<br />
[16. Use F# to do automated browser testing](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-canopy)<br />
[17. Use F# for Behaviour Driven Development](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-bdd)<br />

**Part 4. Using F# for database related tasks**

[18. Use F# to replace LINQpad](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-linqpad)<br />
[19. Use F# to unit test stored procedures](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-testprocs)<br />
[20. Use FsCheck to generate random database records](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-randomdata)<br />
[21. Use F# to do simple ETL](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-etl)<br />
[22. Use F# to generate SQL Agent scripts](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-sqlagent)<br />

**Part 5: Other interesting ways of using F# **

[23. Use F# for parsing](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-parsers)<br />
[24. Use F# for diagramming and visualization](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-diagramming)<br />
[25. Use F# for accessing web-based data stores](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-data-access)<br />
[26. Use F# for data science and machine learning](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-data-science)<br />
[(BONUS) 27: Balance the generation schedule for the UK power station fleet](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-balance-power)<br />


----------

## Part 5: Other ways of using F# outside the core

This last group of suggestions is a bit of a mish-mash I'm afraid.
These are things that didn't fit into earlier posts, mostly concerning using F# for analysis and data processing.

<a name="other-parsers"></a>
## 23. Use F# for parsing

It is surprising how often you need to parse something in the course of routine development: splitting strings at spaces, reading a CSV file,
doing substitutions in a template, finding HTML links for a web crawler, parsing a query string in a URI, and so on.

F#, being an ML-derived language, is ideal for parsing tasks of all kinds, from simple regexes to full fledged parsers.

Of course, there are many off-the-shelf libraries for common tasks, but sometimes you need to write your own.
A good example of this is TickSpec, the BDD framework that [we saw earlier](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-bdd).

TickSpec needs to parse the so-called "Gherkin" format of Given/When/Then. Rather than create a dependency on another library,
I imagine that it was easier (and more fun) for [Phil](http://trelford.com/blog/post/TickSpec.aspx) to write his own parser in a few hundred lines.
You can see part of the [source code here](http://tickspec.codeplex.com/SourceControl/latest#TickSpec/LineParser.fs).

Another situation where it might be worth writing your own parser is when you have some complex system, such as a rules engine, which has a horrible XML configuration format.
Rather than manually editing the configuration, you could create a very simple domain specific language (DSL) that is parsed and then converted to the complex XML.

In [his book on DSLs](http://ptgmedia.pearsoncmg.com/images/9780321712943/samplepages/0321712943.pdf),
Martin Fowler gives an example of this, [a DSL that is parsed to create a state machine](http://www.informit.com/articles/article.aspx?p=1592379&seqNum=3).
And here is an [F# implementation](http://www.fssnip.net/5h) of that DSL.

For more complicating parsing tasks, I highly recommend using [FParsec](http://www.quanttec.com/fparsec/), which is perfectly suited for this kind of thing.
For example, it has been used for parsing
[search queries for FogCreek](http://blog.fogcreek.com/fparsec/), 
[CSV files](http://blog.jb55.com/post/4247991875/f-csv-parsing-with-fparsec),
[chess notation](http://github.com/iigorr/pgn.net),
and a [custom DSL for load testing scenarios](http://www.frenk.com/2012/01/real-world-f-my-experience-part-two/).

<a name="other-diagramming"></a>
## 24. Use F# for diagramming and visualization

Once you have parsed or analyzed something, it is always nice if you can display the results visually, rather than as tables full of data.

For example, in a [previous post](/posts/cycles-and-modularity-in-the-wild/) I used F# in conjunction with [GraphViz](http://www.graphviz.org/)
to create diagrams of dependency relationships. You can see a sample below:

![](/assets/img/tickspec_svg.png)

The code to generate the diagram itself was short, only about 60 lines,
which you can [see here](http://gist.github.com/swlaschin/5742974#file-type-dependency-graph-fsx-L428).

As an alternative to GraphViz, you could also consider using [FSGraph](http://github.com/piotrosz/FSGraph).

For more mathematical or data-centric visualizations, there are a number of good libraries:

* [FSharp.Charting](http://fsharp.github.io/FSharp.Charting/) for desktop visualizations that is well integrated with F# scripting.
* [FsPlot](http://github.com/TahaHachana/FsPlot) for interactive visualizations in HTML.
* [VegaHub](http://github.com/panesofglass/VegaHub), an F# library for working with [Vega](http://trifacta.github.io/vega/)
* [F# for Visualization](http://www.ffconsultancy.com/products/fsharp_for_visualization/index.html) 

And finally, there's the 800 lb gorilla -- Excel.  

Using the built-in capabilities of Excel is great, if it is available. And F# scripting plays well with Excel.

You can [chart in Excel](http://msdn.microsoft.com/en-us/library/vstudio/hh297098.aspx),
[plot functions in Excel](http://www.clear-lines.com/blog/post/Plot-functions-from-FSharp-to-Excel.aspx), and for even more power and integration,
you have the [FCell](http://fcell.io/) and [Excel-DNA](http://excel-dna.net/) projects.

<a name="other-data-access"></a>
## 25. Use F# for accessing web-based data stores

There is a lot of public data out on the web, just waiting to pulled down and loved.
With the magic of type providers, F# is a good choice for direct integrating these web-scale data stores into your workflow. 

Right now, we'll look at two data stores: Freebase and World Bank. 
More will be available soon -- see the [fsharp.org Data Access page](http://fsharp.org/data-access/) for the latest information.

## Freebase 

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/freebase.fsx).*

[Freebase](http://en.wikipedia.org/wiki/Freebase) is a large collaborative knowledge base and online collection of structured data harvested from many sources.

To get started, just link in the type provider DLL as we have seen before. 

The site is throttled, so you'll probably need an API key if you're using it a lot
([api details here](http://developers.google.com/console/help/?csw=1#activatingapis))

```fsharp
// sets the current directory to be same as the script directory
System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)

// Requires FSharp.Data under script directory 
//    nuget install FSharp.Data -o Packages -ExcludeVersion  
#r @"Packages\FSharp.Data\lib\net40\FSharp.Data.dll"
open FSharp.Data

// without a key
let data = FreebaseData.GetDataContext()

// with a key
(*
[<Literal>]
let FreebaseApiKey = "<enter your freebase-enabled google API key here>"
type FreebaseDataWithKey = FreebaseDataProvider<Key=FreebaseApiKey>
let data = FreebaseDataWithKey.GetDataContext()
*)
```

Once the type provider is loaded, you can start asking questions, such as...

*"Who are the US presidents?"*

```fsharp
data.Society.Government.``US Presidents``
|> Seq.map (fun p ->  p.``President number`` |> Seq.head, p.Name)
|> Seq.sortBy fst
|> Seq.iter (fun (n,name) -> printfn "%s was number %i" name n )
```

Result:

```text
George Washington was number 1
John Adams was number 2
Thomas Jefferson was number 3
James Madison was number 4
James Monroe was number 5
John Quincy Adams was number 6
...
Ronald Reagan was number 40
George H. W. Bush was number 41
Bill Clinton was number 42
George W. Bush was number 43
Barack Obama was number 44
```

Not bad for just four lines of code!

How about *"what awards did Casablanca win?"*

```fsharp
data.``Arts and Entertainment``.Film.Films.IndividualsAZ.C.Casablanca.``Awards Won``
|> Seq.map (fun award -> award.Year, award.``Award category``.Name)
|> Seq.sortBy fst
|> Seq.iter (fun (year,name) -> printfn "%s -- %s" year name)
```

The result is:

```text
1943 -- Academy Award for Best Director
1943 -- Academy Award for Best Picture
1943 -- Academy Award for Best Screenplay
```

So that's Freebase. Lots of good information, both useful and frivolous.

[More on how to use the Freebase type provider](http://fsharp.github.io/FSharp.Data/library/Freebase.html).

## Using Freebase to generate realistic test data

We've seen how FsCheck can be used to [generate test data](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-dummy).
Well, you can also get the same affect by getting data from Freebase, which makes the data much more realistic.

[Kit Eason](http://twitter.com/kitlovesfsharp) showed how to do this in a [tweet](http://twitter.com/kitlovesfsharp/status/296240699735695360),
and here's an example based on his code:

```fsharp
let randomElement =
    let random = new System.Random()
    fun (arr:string array) -> arr.[random.Next(arr.Length)]

let surnames = 
    FreebaseData.GetDataContext().Society.People.``Family names``
    |> Seq.truncate 1000
    |> Seq.map (fun name -> name.Name)
    |> Array.ofSeq
            
let firstnames = 
    FreebaseData.GetDataContext().Society.Celebrities.Celebrities
    |> Seq.truncate 1000
    |> Seq.map (fun celeb -> celeb.Name.Split([|' '|]).[0])
    |> Array.ofSeq

// generate ten random people and print
type Person = {Forename:string; Surname:string}
Seq.init 10 ( fun _ -> 
    {Forename = (randomElement firstnames); 
     Surname = (randomElement surnames) }
     )
|> Seq.iter (printfn "%A")
```

The results are:

<pre>
{Forename = "Kelly"; Surname = "Deasy";}
{Forename = "Bam"; Surname = "Brézé";}
{Forename = "Claire"; Surname = "Sludden";}
{Forename = "Kenneth"; Surname = "Klütz";}
{Forename = "Étienne"; Surname = "Defendi";}
{Forename = "Billy"; Surname = "Paleti";}
{Forename = "Alix"; Surname = "Nuin";}
{Forename = "Katherine"; Surname = "Desporte";}
{Forename = "Jasmine";  Surname = "Belousov";}
{Forename = "Josh";  Surname = "Kramarsic";}
</pre>

## World Bank 

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/world-bank.fsx).*

On the other extreme from Freebase is the [World Bank Open Data](http://data.worldbank.org/), which has lots of detailed economic and social information from around the world.

The setup is identical to Freebase, but no API key is needed.

```fsharp
// sets the current directory to be same as the script directory
System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)

// Requires FSharp.Data under script directory 
//    nuget install FSharp.Data -o Packages -ExcludeVersion  
#r @"Packages\FSharp.Data\lib\net40\FSharp.Data.dll"
open FSharp.Data

let data = WorldBankData.GetDataContext()
```

With the type provider set up, we can do a serious query, such as:

*"How do malnutrition rates compare between low income and high income countries?"*

```fsharp
// Create a list of countries to process
let groups = 
 [| data.Countries.``Low income``
    data.Countries.``High income``
    |]

// get data from an indicator for particular year
let getYearValue (year:int) (ind:Runtime.WorldBank.Indicator) =
    ind.Name,year,ind.Item year

// get data
[ for c in groups -> 
    c.Name,
    c.Indicators.``Malnutrition prevalence, weight for age (% of children under 5)`` |> getYearValue 2010
] 
// print the data
|> Seq.iter (
    fun (group,(indName, indYear, indValue)) -> 
       printfn "%s -- %s %i %0.2f%% " group indName indYear indValue)
```

The result is:

```text
Low income -- Malnutrition prevalence, weight for age (% of children under 5) 2010 23.19% 
High income -- Malnutrition prevalence, weight for age (% of children under 5) 2010 1.36% 
```

Similarly, here is the code to compare maternal mortality rates:

```fsharp
// Create a list of countries to process
let countries = 
 [| data.Countries.``European Union``
    data.Countries.``United Kingdom``
    data.Countries.``United States`` |]

/ get data
[ for c in countries  -> 
    c.Name,
    c.Indicators.``Maternal mortality ratio (modeled estimate, per 100,000 live births)`` |> getYearValue 2010
] 
// print the data
|> Seq.iter (
    fun (group,(indName, indYear, indValue)) -> 
       printfn "%s -- %s %i %0.1f" group indName indYear indValue)
```

The result is:

```text
European Union -- Maternal mortality ratio (modeled estimate, per 100,000 live births) 2010 9.0 
United Kingdom -- Maternal mortality ratio (modeled estimate, per 100,000 live births) 2010 12.0 
United States -- Maternal mortality ratio (modeled estimate, per 100,000 live births) 2010 21.0 
```

[More on how to use the World Bank type provider](http://fsharp.github.io/FSharp.Data/library/WorldBank.html).

<a name="other-data-science"></a>
## 26. Use F# for data science and machine learning

So you're putting all these suggestions into practice. You're parsing your web logs with FParsec,
extracting stats from your internal databases with the SQL type provider,
and pulling down external data from web services. You've got all this data -- what can you do with it?

Let's finish up by having a quick look at using F# for data science and machine learning.

As we have seen, F# is great for exploratory programming -- it has a REPL with intellisense. But unlike Python and R, your
code is type checked, so you know that your code is not going to fail with an exception halfway through a two hour processing job!

If you are familiar with the Pandas library from Python or the 'tseries' package in R, then you should
take a serious look at [Deedle](http://bluemountaincapital.github.io/Deedle/), an easy-to-use, high quality package for data and time series manipulation.
Deedle is designed to work well for exploratory programming using the REPL, but can be also used in efficient compiled .NET code.

And if you use R a lot, there's an [R type provider](http://bluemountaincapital.github.io/FSharpRProvider)(of course).
This means you can use R packages as if they were .NET libraries. How awesome is that!

There's lots of other F# friendly packages too. You can find out all about them at fsharp.org.

* [Data science](http://fsharp.org/data-science/)
* [Math](http://fsharp.org/math/)
* [Machine learning](http://fsharp.org/machine-learning)

----------

## Series summary

Phew! That was a long list of examples and a lot of code to look at. If you've made it to the end, congratulations!

I hope that this has given you some new insights into the value of F#.
It's not just a math-y or financial language -- it's a practical one too.
And it can help you with all sorts of things in your development, testing, and data management workflows. 

Finally, as I have stressed throughout this series, all these uses are safe, low risk and incremental. What's the worst that can happen? 

So go on, persuade your team mates and boss to give F# a try, and let me know how it goes.

<a name="other-balance-power"></a>

## Postscript

After I posted this, Simon Cousins tweeted that I missed one -- I can't resist adding it.

<blockquote class="twitter-tweet" lang="en"><p><a href="https://twitter.com/ScottWlaschin">@ScottWlaschin</a> 27: balance the generation schedule for the uk power station fleet. seriously, the alternative to <a href="https://twitter.com/search?q=%23fsharp&amp;src=hash">#fsharp</a> was way too risky</p>&mdash; Simon Cousins (@simontcousins) <a href="https://twitter.com/simontcousins/statuses/459591939902697472">April 25, 2014</a></blockquote>
<script async src="//platform.twitter.com/widgets.js" charset="utf-8"></script>

You can read more about Simon's real-world of use of F# (for power generation) on [his blog](http://www.simontylercousins.net/does-the-language-you-use-make-a-difference-revisited/).
There are more testimonials to F# at [fsharp.org](http://fsharp.org/testimonials/).



