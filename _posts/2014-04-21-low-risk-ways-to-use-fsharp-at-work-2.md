---
layout: post
title: "Using F# for development and devops scripts"
description: "Twenty six low-risk ways to use F# at work (part 2)"
categories: []
seriesId: "Low-risk ways to use F# at work"
seriesOrder: 2

---

This post is a continuation of the series on [low-risk ways to use F# at work](/posts/low-risk-ways-to-use-fsharp-at-work/).
I've been suggesting a number of ways you can get your hands dirty with F# in a low-risk, incremental way, without affecting any mission critical code.

In this one, we'll talk about using F# for builds and other development and devops scripts.

If you're new to F#, you might want to read the sections on [getting started](/posts/low-risk-ways-to-use-fsharp-at-work/#getting-started) and
[working with NuGet](/posts/low-risk-ways-to-use-fsharp-at-work/#working-with-nuget) in the previous post.

## Series contents

Here's a list of shortcuts to the twenty six ways:

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

## Part 2: Using F# for development and devops scripts

The next set of suggestions relates to using F# for the various scripts that revolve around development activities: builds, continuous integration, deployment, etc.

For these kinds of small tasks, you need a good scripting language with a REPL.
You could use PowerShell, or [ScriptCS](http://scriptcs.net/), or even Python. But why not give F# a go?

* F# feels lightweight like Python (few or no type declarations).
* F# can access .NET libraries, both the core ones and those downloaded via NuGet. 
* F# has type providers (a big advantage over PowerShell and ScriptCS) that let you easily access a wide range of data sources.
* All this in a concise, type-safe manner, with intellisense too!

Using F# in this way will allow you and your fellow developers to use F# code to solve practical problems.
There shouldn't be any resistance from managers for this low-risk approach -- in the worse case you can easily switch to using a different tool.

A hidden agenda, of course, is that once your fellow developers get a chance to play with F#, they'll be hooked,
and you'll be one step closer to using [F# end to end](http://colinbul.wordpress.com/2013/02/23/f-end-to-end/)!

### What can you do with F# scripts?

In the next few sections we'll see three examples of F# scripts:

* <a href="#dev-website-responding">An F# script to check that a website is responding</a><br />
* <a href="#dev-rss-to-csv">An F# script to convert an RSS feed into CSV</a><br />
* <a href="#dev-wmi-stats">An F# script that uses WMI to check the stats of a process</a><br />

But of course, you can integrate F# scripts with almost any .NET library. Here are other suggestions for utilities that can be scripted:

* Simple file copying, directory traversal, and archiving (e.g. of log files).
  If you're using .NET 4.5, you can use the new [System.IO.Compression.ZipArchive](http://msdn.microsoft.com/en-us/library/vstudio/hh485720.aspx)
  class to do zipping and unzipping without needing a third party library.
* Doing things with JSON, either with a known format
  (using the [JSON Type Provider](http://fsharp.github.io/FSharp.Data/library/JsonProvider.html))
  or unknown format (using the [JSON parser](http://fsharp.github.io/FSharp.Data/library/JsonValue.html)).
* Interacting with GitHub using [Octokit](http://www.nuget.org/packages/Octokit/).
* Extracting data from, or manipulating data in, Excel. F# supports COM for doing Office automation, or you can use one of the type providers or libraries.
* Doing numerics with [Math.NET](http://numerics.mathdotnet.com/).
* Web crawling, link checking, and screenscraping. The built-in async workflows and agents make this kind of "multithreaded" code very easy to write.
* Scheduling things with [Quartz.NET](http://www.quartz-scheduler.net/).

If these suggestions whet your interest, and you want to use more F#, then check out the [F# community projects](http://fsharp.org/community/projects/) page.
It's a great source of useful libraries being written for F#, and most of them will work well with F# scripting.

### Debugging F# scripts

A great thing about using F# scripts is that you don't need to create a whole project, nor launch Visual Studio.

But if you need to debug a script, and you're not in Visual Studio, what can you do? Here are some tips:

* First, you can just use tried and true printing to the console using `printfn`.
  I generally wrap this in a simple `log` function so that I can turn logging on or off with a flag.
* You can use the [FsEye](http://code.google.com/p/fseye/) tool to inspect and watch variables in an interactive session.
* Finally, you can still use the Visual Studio debugger. The trick is to [attach the debugger](http://stackoverflow.com/a/9337016/1136133) to the 
  fsi.exe process, and then you can use [`Debugger.Break`](http://msdn.microsoft.com/en-us/library/vstudio/system.diagnostics.debugger.break)
  to halt at a certain point.

<a name="fake"></a>

## 5. Use FAKE for build and CI scripts

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/fake.fsx).*

Let's start with [FAKE](http://fsharp.github.io/FAKE/), which is a cross platform build automation tool written in F#, analogous to Ruby's [Rake](http://rake.rubyforge.org/).

FAKE has built-in support for git, NuGet, unit tests, Octopus Deploy, Xamarin and more, and makes it easy to develop complex scripts with dependencies.

You can even use it with [TFS to avoid using XAML](http://blog.ctaggart.com/2014/01/code-your-tfs-builds-in-f-instead-of.html).

One reason to use FAKE rather than something like Rake is that you can standardize on .NET code throughout your tool chain.
In theory, you could use [NAnt](http://en.wikipedia.org/wiki/NAnt) instead, but in practice, no thanks, because XML.
[PSake](http://github.com/psake/psake) is also a possibility, but more complicated than FAKE, I think. 

You can also use FAKE to remove dependencies on a particular build server. For example, rather than using TeamCity's integration to run tests and other tasks,
you might consider [doing them in FAKE](http://www.jamescrowley.co.uk/2014/04/22/code-coverage-using-dotcover-and-f-make/) instead, which means you can run full builds
without having TeamCity installed.

Here's an example of a very simple FAKE script, taken from [a more detailed example on the FAKE site](http://fsharp.github.io/FAKE/gettingstarted.html).

```fsharp
// Include Fake lib
// Assumes NuGet has been used to fetch the FAKE libraries
#r "packages/FAKE/tools/FakeLib.dll"
open Fake

// Properties
let buildDir = "./build/"

// Targets
Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "Default" (fun _ ->
    trace "Hello World from FAKE"
)

// Dependencies
"Clean"
  ==> "Default"

// start build
RunTargetOrDefault "Default"
```

The syntax takes a little getting used to, but that effort is well spent.

Some further reading on FAKE:

* [Migrating to FAKE](http://bugsquash.blogspot.co.uk/2010/11/migrating-to-fake.html).
* [Hanselman on FAKE](http://www.hanselman.com/blog/ExploringFAKEAnFBuildSystemForAllOfNET.aspx). Many of the comments are from people who are using FAKE actively.
* [A NAnt user tries out FAKE](http://putridparrot.com/blog/trying-fake-out/).

<a name="dev-website-responding"></a>

## 6. An F# script to check that a website is responding

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/dev-website-responding.fsx).*

This script checks that a website is responding with a 200.
This might be useful as the basis for a post-deployment smoke test, for example.

```fsharp
// Requires FSharp.Data under script directory 
//    nuget install FSharp.Data -o Packages -ExcludeVersion  
#r @"Packages\FSharp.Data\lib\net40\FSharp.Data.dll"
open FSharp.Data

let queryServer uri queryParams = 
    try
        let response = Http.Request(uri, query=queryParams, silentHttpErrors = true)
        Some response 
    with
    | :? System.Net.WebException as ex -> None

let sendAlert uri message = 
    // send alert via email, say
    printfn "Error for %s. Message=%O" uri message

let checkServer (uri,queryParams) = 
    match queryServer uri queryParams with
    | Some response -> 
        printfn "Response for %s is %O" uri response.StatusCode 
        if (response.StatusCode <> 200) then
            sendAlert uri response.StatusCode 
    | None -> 
        sendAlert uri "No response"

// test the sites    
let google = "http://google.com", ["q","fsharp"]
let bad = "http://example.bad", []

[google;bad]
|> List.iter checkServer 
```

The result is:

```text
Response for http://google.com is 200
Error for http://example.bad. Message=No response
```

Note that I'm using the Http utilities code in `Fsharp.Data`, which provides a nice wrapper around `HttpClient`.
[More on HttpUtilities here](http://fsharp.github.io/FSharp.Data/library/Http.html).

<a name="dev-rss-to-csv"></a>

## 7. An F# script to convert an RSS feed into CSV

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/dev-rss-to-csv.fsx).*

Here's a little script that uses the Xml type provider to parse an RSS feed (in this case, [F# questions on StackOverflow](https://stackoverflow.com/questions/tagged/f%23?sort=newest&pageSize=10))
and convert it to a CSV file for later analysis.
 
Note that the RSS parsing code is just one line of code! Most of the code is concerned with writing the CSV.
Yes, I could have used a CSV library (there are lots on NuGet) but I thought I'd leave it as is to show you how simple it is.
 
```fsharp
// sets the current directory to be same as the script directory
System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)

// Requires FSharp.Data under script directory 
//    nuget install FSharp.Data -o Packages -ExcludeVersion 
#r @"Packages\FSharp.Data\lib\net40\FSharp.Data.dll"
#r "System.Xml.Linq.dll"
open FSharp.Data

type Rss = XmlProvider<"http://stackoverflow.com/feeds/tag/f%23">

// prepare a string for writing to CSV            
let prepareStr obj =
    obj.ToString()
     .Replace("\"","\"\"") // replace single with double quotes
     |> sprintf "\"%s\""   // surround with quotes

// convert a list of strings to a CSV
let listToCsv list =
    let combine s1 s2 = s1 + "," + s2
    list 
    |> Seq.map prepareStr 
    |> Seq.reduce combine 

// extract fields from Entry
let extractFields (entry:Rss.Entry) = 
    [entry.Title.Value; 
     entry.Author.Name; 
     entry.Published.ToShortDateString()]

// write the lines to a file
do 
    use writer = new System.IO.StreamWriter("fsharp-questions.csv")
    let feed = Rss.GetSample()
    feed.Entries
    |> Seq.map (extractFields >> listToCsv)
    |> Seq.iter writer.WriteLine
    // writer will be closed automatically at the end of this scope
``` 
    
Note that the type provider generates intellisense (shown below) to show you the available properties based on the actual contents of the feed. That's very cool.

![](/assets/img/fsharp-xml-dropdown.png)    

The result is something like this:

```text
"Optimising F# answer for Euler #4","DropTheTable","18/04/2014"
"How to execute a function, that creates a lot of objects, in parallel?","Lawrence Woodman","01/04/2014"
"How to invoke a user defined function using R Type Provider","Dave","19/04/2014"
"Two types that use themselves","trn","19/04/2014"
"How does function [x] -> ... work","egerhard","19/04/2014"
```

For more on the XML type provider, [see the FSharp.Data pages](http://fsharp.github.io/FSharp.Data/library/XmlProvider.html).
    
<a name="dev-wmi-stats"></a>
    
## 8. An F# script that uses WMI to check the stats of a process

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/dev-wmi-stats.fsx).*

If you use Windows, being able to access WMI is very useful.
Luckily there is an F# type provider for WMI that makes using it easy. 

In this example, we'll get the system time and also check some stats for a process.
This could be useful during and after a load test, for example. 

```fsharp
// sets the current directory to be same as the script directory
System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)

// Requires FSharp.Management under script directory 
//    nuget install FSharp.Management -o Packages -ExcludeVersion 
#r @"System.Management.dll"
#r @"Packages\FSharp.Management\lib\net40\FSharp.Management.dll"
#r @"Packages\FSharp.Management\lib\net40\FSharp.Management.WMI.dll"

open FSharp.Management

// get data for the local machine
type Local = WmiProvider<"localhost">
let data = Local.GetDataContext()

// get the time and timezone on the machine
let time = data.Win32_UTCTime |> Seq.head
let tz = data.Win32_TimeZone |> Seq.head
printfn "Time=%O-%O-%O %O:%O:%O" time.Year time.Month time.Day time.Hour time.Minute time.Second 
printfn "Timezone=%O" tz.StandardName 

// find the "explorer" process
let explorerProc = 
    data.Win32_PerfFormattedData_PerfProc_Process
    |> Seq.find (fun proc -> proc.Name.Contains("explorer") )

// get stats about it
printfn "ElapsedTime=%O" explorerProc.ElapsedTime
printfn "ThreadCount=%O" explorerProc.ThreadCount
printfn "HandleCount=%O" explorerProc.HandleCount
printfn "WorkingSetPeak=%O" explorerProc.WorkingSetPeak
printfn "PageFileBytesPeak=%O" explorerProc.PageFileBytesPeak
```

The output is something like this:

```text
Time=2014-4-20 14:2:35
Timezone=GMT Standard Time
ElapsedTime=2761906
ThreadCount=67
HandleCount=3700
WorkingSetPeak=168607744
PageFileBytesPeak=312565760
```

Again, using a type provider means that you get intellisense (shown below). Very useful for the hundreds of WMI options.

![](/assets/img/fsharp-wmi-dropdown.png)

[More on the WMI type provider here](http://fsprojects.github.io/FSharp.Management/WMIProvider.html).


<a name="dev-cloud"></a>

## 9. Use F# for configuring and managing the cloud

One area which deserves special mention is using F# for configuring and managing cloud services.
The [cloud page](http://fsharp.org/cloud/) at fsharp.org has many helpful links.

For simple scripting, [Fog](http://dmohl.github.io/Fog/) is a nice wrapper for Azure. 

So for example, to upload a blob, the code is as simple as this:

```fsharp
UploadBlob "testcontainer" "testblob" "This is a test" |> ignore
```

or to add and receive messages:

```fsharp
AddMessage "testqueue" "This is a test message" |> ignore

let result = GetMessages "testqueue" 20 5
for m in result do
    DeleteMessage "testqueue" m
```

What's especially nice about using F# for this is that you can do it in micro scripts -- you don't need any heavy tooling.

   
## Summary
   
I hope you found these suggestions useful. Let me know in the comments if you apply them in practice.

Next up: using F# for testing.
