---
layout: post
title: "Using F# for testing"
description: "Twenty six low-risk ways to use F# at work (part 3)"
categories: []
seriesId: "Low-risk ways to use F# at work"
seriesOrder: 3

---

This post is a continuation of the previous series on [low-risk and incremental ways to use F# at work](/posts/low-risk-ways-to-use-fsharp-at-work/) --
how can you get your hands dirty with F# in a low-risk, incremental way, without affecting any mission critical code?

In this one, we'll talk about using F# for testing.

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
   
## Part 3 - Using F# for testing

If you want to start writing useful code in F# without touching core code, writing tests is a great way to start.

Not only does F# have a more compact syntax, it also has many nice features, such as the "double backtick" syntax,
that make test names much more readable.

As with all of the suggestions in this series, I think this is a low risk option.
Test methods tend to be short, so almost anyone will be able to read them without having to understand F# deeply.
In the worst-case, you can easily port them back to C#.



<a name="test-nunit"></a>

## 10. Use F# to write unit tests with readable names

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/TestWithNUnit.fs).*

Just like C#, F# can be used to write standard unit tests using the standard frameworks like NUnit, MsUnit, xUnit, etc.

Here's an example of a test class written for use with NUnit. 

```fsharp
[<TestFixture>]
type TestClass() = 

    [<Test>]
    member this.When2IsAddedTo2Expect4() = 
        Assert.AreEqual(4, 2+2)
``` 

As you can see, there's a class with the `TestFixture` attribute, and a public void method with the `Test` attribute.
All very standard.

But there are some nice extras you get when you use F# rather than C#. First you can use the double backtick syntax to create more readable names,
and second, you can use `let` bound functions in modules rather than classes, which simplifies the code.

```fsharp
[<Test>]
let ``When 2 is added to 2 expect 4``() = 
    Assert.AreEqual(4, 2+2)
``` 

The double backtick syntax makes the test results much easier to read. Here is the output of the test with a standard class name:

```text
TestClass.When2IsAddedTo2Expect4
Result: Success
``` 

vs. the output using the more friendly name:

```text
MyUnitTests.When 2 is added to 2 expect 4
Result: Success
``` 

So if you want to write test names that are accessible to non-programmers, give F# a go!

<a name="test-runner"></a>

## 11. Use F# to run unit tests programmatically

Often, you might want to run the unit tests programmatically. This can be for various reasons,
such as using custom filters, or doing custom logging, or not wanting to install NUnit on test machines.

One simple way to do this is to use the [Fuchu library](http://github.com/mausch/Fuchu) which lets you organize tests directly, especially parameterized tests, without any
complex test attributes. 

Here's an example:

```fsharp
let add1 x = x + 1

// a simple test using any assertion framework:
// Fuchu's own, Nunit, FsUnit, etc
let ``Assert that add1 is x+1`` x _notUsed = 
   NUnit.Framework.Assert.AreEqual(x+1, add1 x)

// a single test case with one value
let simpleTest = 
   testCase "Test with 42" <| 
     ``Assert that add1 is x+1`` 42

// a parameterized test case with one param
let parameterizedTest i = 
   testCase (sprintf "Test with %i" i) <| 
     ``Assert that add1 is x+1`` i
```

You can run these tests directly in F# interactive using code like this: `run simpleTest`.

You can also combine these tests into one or more lists, or hierarchical lists of lists:

```fsharp
// create a hierarchy of tests 
// mark it as the start point with the "Tests" attribute
[<Fuchu.Tests>]
let tests = 
   testList "Test group A" [
      simpleTest 
      testList "Parameterized 1..10" ([1..10] |> List.map parameterizedTest) 
      testList "Parameterized 11..20" ([11..20] |> List.map parameterizedTest) 
   ]
```

*The code above is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/OrganizeTestsWithFuchu.fs).*

Finally, with Fuchu, the test assembly becomes its own test runner. Just make the assembly a console app instead of a library and add this code to the `program.fs` file:

```fsharp
[<EntryPoint>]
let main args = 
    let exitCode = defaultMainThisAssembly args
    
    Console.WriteLine("Press any key")
    Console.ReadLine() |> ignore

    // return the exit code
    exitCode 
```

[More on Fuchu here](http://bugsquash.blogspot.co.uk/2012/06/fuchu-functional-test-library-for-net.html).

### Using the NUnit test runner

If you do need to use an existing test runner (such as the NUnit one), then
it's very simple to put together a simple script to do this.

I've made a little example, below, using the `Nunit.Runners` package.

All right, this might not be the most exciting use of F#, but it does show off F#'s "object expression" syntax to 
create the `NUnit.Core.EventListener` interface, so I thought I'd leave it in as a demo.

```fsharp
// sets the current directory to be same as the script directory
System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)

// Requires Nunit.Runners under script directory 
//    nuget install NUnit.Runners -o Packages -ExcludeVersion 

#r @"Packages\NUnit.Runners\tools\lib\nunit.core.dll"
#r @"Packages\NUnit.Runners\tools\lib\nunit.core.interfaces.dll"

open System
open NUnit.Core

module Setup = 
    open System.Reflection
    open NUnit.Core
    open System.Diagnostics.Tracing

    let configureTestRunner path (runner:TestRunner) = 
        let package = TestPackage("MyPackage")
        package.Assemblies.Add(path) |> ignore
        runner.Load(package) |> ignore

    let createListener logger =

        let replaceNewline (s:string) = 
            s.Replace(Environment.NewLine, "")

        // This is an example of F#'s "object expression" syntax.
        // You don't need to create a class to implement an interface
        {new NUnit.Core.EventListener
            with
        
            member this.RunStarted(name:string, testCount:int) =
                logger "Run started "

            member this.RunFinished(result:TestResult ) = 
                logger ""
                logger "-------------------------------"
                result.ResultState
                |> sprintf "Overall result: %O" 
                |> logger 

            member this.RunFinished(ex:Exception) = 
                ex.StackTrace 
                |> replaceNewline 
                |> sprintf "Exception occurred: %s" 
                |> logger 

            member this.SuiteFinished(result:TestResult) = ()
            member this.SuiteStarted(testName:TestName) = ()

            member this.TestFinished(result:TestResult)=
                result.ResultState
                |> sprintf "Result: %O" 
                |> logger 

            member this.TestOutput(testOutput:TestOutput) = 
                testOutput.Text 
                |> replaceNewline 
                |> logger 

            member this.TestStarted(testName:TestName) = 
                logger ""
            
                testName.FullName 
                |> replaceNewline 
                |> logger 

            member this.UnhandledException(ex:Exception) = 
                ex.StackTrace 
                |> replaceNewline 
                |> sprintf "Unhandled exception occurred: %s"
                |> logger 
            }


// run all the tests in the DLL
do 
    let dllPath = @".\bin\MyUnitTests.dll"

    CoreExtensions.Host.InitializeService();

    use runner = new NUnit.Core.SimpleTestRunner()
    Setup.configureTestRunner dllPath runner
    let logger = printfn "%s"
    let listener = Setup.createListener logger
    let result = runner.Run(listener, TestFilter.Empty, true, LoggingThreshold.All)

    // if running from the command line, wait for user input
    Console.ReadLine() |> ignore

    // if running from the interactive session, reset session before recompiling MyUnitTests.dll
``` 

*The code above is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/nunit-test-runner.fsx).*

<a name="test-other"></a>

## 12. Use F# to learn to write unit tests in other ways

The [unit test code above](#test-nunit) is familiar to all of us, but there are other ways to write tests.
Learning to code in different styles is a great way to add some new techniques to your repertoire and expand your thinking in general,
so let's have a quick look at some of them.

First up is [FsUnit](http://github.com/fsharp/FsUnit), which replaces `Assert` with a more fluent and idiomatic approach (natural language and piping).

Here's a snippet:

```fsharp
open NUnit.Framework
open FsUnit

let inline add x y = x + y

[<Test>]
let ``When 2 is added to 2 expect 4``() = 
    add 2 2 |> should equal 4

[<Test>]
let ``When 2.0 is added to 2.0 expect 4.01``() = 
    add 2.0 2.0 |> should (equalWithin 0.1) 4.01

[<Test>]
let ``When ToLower(), expect lowercase letters``() = 
    "FSHARP".ToLower() |> should startWith "fs"
``` 

*The above code is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/TestWithFsUnit.fs).*
 
A very different approach is used by [Unquote](https://github.com/SwensenSoftware/unquote).
The Unquote approach is to wrap any F# expression in [F# quotations](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/code-quotations) and then evaluate it.
If a test expression throws an exception, the test will fail and print not just the exception, but each step up to the point of the exception.
This information could potentially give you much more insight in why the assert fails.

Here's a very simple example:
 
```fsharp
open Swensen.Unquote

[<Test>]
let ``When 2 is added to 2 expect 4``() = 
    test <@ 2 + 2 = 4 @>
``` 

There are also a number of shortcut operators such as `=!` and `>!` that allow you to write your tests even more simply -- no asserts anywhere!

```fsharp
open Swensen.Unquote

[<Test>]
let ``2 + 2 is 4``() = 
   let result = 2 + 2
   result =! 4

[<Test>]
let ``2 + 2 is bigger than 5``() = 
   let result = 2 + 2
   result >! 5
``` 
<a name="test-fscheck"></a>

*The above code is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/TestWithUnquote.fs).*

## 13. Use FsCheck to write better unit tests

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/TestWithFsCheck.fs).*

Let's say that we have written a function that converts numbers to Roman numerals, and we want to create some test cases for it.

We might start writing tests like this:

```fsharp
[<Test>]
let ``Test that 497 is CDXCVII``() = 
    arabicToRoman 497 |> should equal "CDXCVII"
``` 

But the problem with this approach is that it only tests a very specific example. There might be some edge cases that we haven't thought of.

A much better approach is to find something that must be true for *all* cases. Then we can create a test that checks that this something (a "property") is true for
all cases, or at least a large random subset.

For example, in the Roman numeral example, we can say that one property is "all Roman numerals have at most one 'V' character" or "all Roman numerals have at most three 'X' characters".
We can then construct tests that check this property is indeed true.

This is where [FsCheck](http://github.com/fsharp/FsCheck) can help.
FsCheck is a framework designed for exactly this kind of property-based testing. It's written in F# but it works equally well for testing C# code.

So, let's see how we'd use FsCheck for our Roman numerals.

First, we define some properties that we expect to hold for all Roman numerals.

```fsharp
let maxRepetitionProperty ch count (input:string) = 
    let find = String.replicate (count+1) ch
    input.Contains find |> not

// a property that holds for all roman numerals
let ``has max rep of one V`` roman = 
    maxRepetitionProperty "V" 1 roman 

// a property that holds for all roman numerals
let ``has max rep of three Xs`` roman = 
    maxRepetitionProperty "X" 3 roman 
``` 

With this in place we create tests that:

1. Create a property checker function suitable for passing to FsCheck.
1. Use the `Check.Quick` function to generate hundreds of random test cases and send them into that property checker.

```fsharp
open FsCheck

[<Test>]
let ``Test that roman numerals have no more than one V``() = 
    let property num = 
        // convert the number to roman and check the property
        arabicToRoman num |> ``has max rep of one V``

    Check.QuickThrowOnFailure property

[<Test>]
let ``Test that roman numerals have no more than three Xs``() = 
    let property num = 
        // convert the number to roman and check the property
        arabicToRoman num |> ``has max rep of three Xs``

    Check.QuickThrowOnFailure property
```

Here are the results of the test. You can see that 100 random numbers have been tested, not just one.

```text
Test that roman numerals have no more than one V
   Ok, passed 100 tests.

Test that roman numerals have no more than three Xs
   Ok, passed 100 tests.
``` 

If we changed the test to be "Test that roman numerals have no more than TWO Xs", then the test result is false, and looks like this:

```text
Falsifiable, after 33 tests 

30
``` 

In other words, after generating 33 different inputs, FsCheck has correctly found a number (30) that does not meet the required property. Very nice!

### Using FsCheck in practice

Not all situations have properties that can be tested this way, but you might find that it is more common than you think.

For example, property based testing is especially useful for "algorithmic" code. Here a few examples:

* If you reverse a list and then reverse it again, you get the original list.
* If you factorize an integer and then multiply the factors, you get the original number.

But even in Boring Line-Of-Business Applications™, you may find that property based testing has a place. For example, here are some things that can be expressed as properties:

* **Roundtripping**. For example, if you save a record to a database and then reload it, the record's fields should be unchanged. 
  Similarly, if you serialize and then deserialize something, you should get the original thing back.
* **Invariants**. If you add products to a sales order, the sum of the individual lines should be the same as the order total.
  Or, the sum of word counts for each page should be the sum of the word count for the entire book.
  More generally, if you calculate things via two different paths, you should get the same answer ([monoid homomorphisms!](/posts/monoids-part2/#monoid-homomorphism))
* **Rounding**. If you add ingredients to a recipe, the sum of the ingredient percentages (with 2 place precision) should always be exactly 100%.
  Similar rules are needed for most partitioning logic, such as shares, tax calculations, etc. 
  (e.g. [the "share pie" example in the DDD book](http://books.google.co.uk/books?id=xColAAPGubgC&pg=PA198&lpg=PA198&dq=%22domain+driven+design%22+%22share+pie%22&source=bl&ots=q9-HdfTK4p&sig=IUnHGFUdwQv2p0tuWVbrqqwdAk4&hl=en&sa=X&ei=IdFbU5bLK8SMOPLFgfgC&ved=0CC8Q6AEwAA#v=onepage&q=%22domain%20driven%20design%22%20%22share%20pie%22&f=false)).  
  Making sure you get the rounding right in situations like this is where FsCheck shines.
  
See this [SO question](http://stackoverflow.com/questions/2446242/difficulty-thinking-of-properties-for-fscheck?rq=1) for other ideas.  

FsCheck is also very useful for doing refactoring, because once you trust that the tests are extremely thorough, you can confidently work on tweaks and optimization.

Some more links for FsCheck:

* I have written [an introduction to property-based testing](http://fsharpforfunandprofit.com/posts/property-based-testing/) and [a follow up on choosing properties for property-based testing](http://fsharpforfunandprofit.com/posts/property-based-testing-2/).
* [FsCheck documentation](http://github.com/fsharp/FsCheck/blob/master/Docs/Documentation.md).
* [An article on using FsCheck in practice](http://www.clear-lines.com/blog/post/FsCheck-and-XUnit-is-The-Bomb.aspx).
* [My post on the Roman Numerals kata that mentions FsCheck](/posts/roman-numeral-kata/).


For more on property-based testing in general, look for articles and videos about QuickCheck. 

* [Intro to QuickCheck by John Hughes](http://www.cs.utexas.edu/~ragerdl/fmcad11/slides/tutorial-a.pdf) (PDF)
* Fascinating talk on [using QuickCheck to find bugs in Riak](https://skillsmatter.com/skillscasts/4505-quickchecking-riak) ([another version](http://www.cs.utexas.edu/~ragerdl/fmcad11/slides/tutorial-a.pdf)) (videos)


<a name="test-dummy"></a>

## 14. Use FsCheck to create random dummy data

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/RandomDataWithFsCheck.fs).*

In addition to doing testing, FsCheck can be used to create random dummy data.

For example, below is the complete code for generating random customers. 

When you combine this with the SQL Type Provider (discussed later) or CSV writer, you can easily
generate thousands of rows of random customers in a database or CSV file.
Or you can use it with the JSON type provider to call a web service for testing validation logic, or load testing.

*(Dont worry about not understanding the code -- this sample is just to show you how easy it is!)*

```fsharp
// domain objects
type EmailAddress = EmailAddress of string
type PhoneNumber = PhoneNumber of string
type Customer = {
    name: string
    email: EmailAddress
    phone: PhoneNumber
    birthdate: DateTime
    }

// a list of names to sample
let possibleNames = [
    "Georgianne Stephan"
    "Sharolyn Galban"
    "Beatriz Applewhite"
    "Merissa Cornwall"
    "Kenneth Abdulla"
    "Zora Feliz"
    "Janeen Strunk"
    "Oren Curlee"
    ]

// generate a random name by picking from the list at random
let generateName() = 
    FsCheck.Gen.elements possibleNames 

// generate a random EmailAddress by combining random users and domains
let generateEmail() = 
    let userGen = FsCheck.Gen.elements ["a"; "b"; "c"; "d"; "e"; "f"]
    let domainGen = FsCheck.Gen.elements ["gmail.com"; "example.com"; "outlook.com"]
    let makeEmail u d = sprintf "%s@%s" u d |> EmailAddress
    FsCheck.Gen.map2 makeEmail userGen domainGen 

// generate a random PhoneNumber 
let generatePhone() = 
    let areaGen = FsCheck.Gen.choose(100,999)
    let n1Gen = FsCheck.Gen.choose(1,999)
    let n2Gen = FsCheck.Gen.choose(1,9999)
    let makeNumber area n1 n2 = sprintf "(%03i)%03i-%04i" area n1 n2 |> PhoneNumber
    FsCheck.Gen.map3 makeNumber areaGen n1Gen n2Gen 
    
// generate a random birthdate
let generateDate() = 
    let minDate = DateTime(1920,1,1).ToOADate() |> int
    let maxDate = DateTime(2014,1,1).ToOADate() |> int
    let oaDateGen = FsCheck.Gen.choose(minDate,maxDate)
    let makeDate oaDate = float oaDate |> DateTime.FromOADate 
    FsCheck.Gen.map makeDate oaDateGen

// a function to create a customer
let createCustomer name email phone birthdate =
    {name=name; email=email; phone=phone; birthdate=birthdate}

// use applicatives to create a customer generator
let generateCustomer = 
    createCustomer 
    <!> generateName() 
    <*> generateEmail() 
    <*> generatePhone() 
    <*> generateDate() 

[<Test>]
let printRandomCustomers() =
    let size = 0
    let count = 10
    let data = FsCheck.Gen.sample size count generateCustomer

    // print it
    data |> List.iter (printfn "%A")
```

And here is a sampling of the results:

```text
{name = "Georgianne Stephan";
 email = EmailAddress "d@outlook.com";
 phone = PhoneNumber "(420)330-2080";
 birthdate = 11/02/1976 00:00:00;}

{name = "Sharolyn Galban";
 email = EmailAddress "e@outlook.com";
 phone = PhoneNumber "(579)781-9435";
 birthdate = 01/04/2011 00:00:00;}

{name = "Janeen Strunk";
 email = EmailAddress "b@gmail.com";
 phone = PhoneNumber "(265)405-6619";
 birthdate = 21/07/1955 00:00:00;}
```


<a name="test-mock"></a>

## 15. Use F# to create mocks

If you're using F# to write test cases for code written in C#, you may want to create mocks and stubs for interfaces.

In C# you might use [Moq](http://github.com/Moq/moq4) or [NSubstitute](http://nsubstitute.github.io/).
In F# you can use object expressions to create interfaces directly, or the [Foq library](http://foq.codeplex.com/).

Both are easy to do, and in a way that is similar to Moq.

Here's some Moq code in C#:

```csharp
// Moq Method
var mock = new Mock<IFoo>();
mock.Setup(foo => foo.DoSomething("ping")).Returns(true);
var instance = mock.Object;

// Moq Matching Arguments:
mock.Setup(foo => foo.DoSomething(It.IsAny<string>())).Returns(true);

// Moq Property
mock.Setup(foo => foo.Name ).Returns("bar");
``` 

And here's the equivalent Foq code in F#:

```fsharp
// Foq Method
let mock = 
    Mock<IFoo>()
        .Setup(fun foo -> <@ foo.DoSomething("ping") @>).Returns(true)
        .Create()

// Foq Matching Arguments
mock.Setup(fun foo -> <@ foo.DoSomething(any()) @>).Returns(true)

// Foq Property
mock.Setup(fun foo -> <@ foo.Name @>).Returns("bar")
``` 

For more on mocking in F#, see:

* [F# as a Unit Testing Language](http://trelford.com/blog/post/fstestlang.aspx)
* [Mocking with Foq](http://trelford.com/blog/post/Foq.aspx)
* [Testing and mocking your C# code with F#](http://www.clear-lines.com/blog/post/Testing-and-mocking-your-C-sharp-code-with-F-sharp.aspx)

And you need to mock external services such as SMTP over the wire, there is an interesting tool called [mountebank](http://www.mbtest.org/),
which is [easy to interact with in F#](http://nikosbaxevanis.com/blog/2014/04/22/mountebank-mocks-with-f-number/).

<a name="test-canopy"></a>

## 16. Use F# to do automated browser testing

In addition to unit tests, you should be doing some kind of automated web testing,
driving the browser with [Selenium](http://docs.seleniumhq.org/) or [WatiN](http://watin.sourceforge.net/).

But what language should you write the automation in? Ruby? Python? C#? I think you know the answer!

To make your life even easier, try using [Canopy](http://lefthandedgoat.github.io/canopy/), a web testing framework built on top of Selenium and written in F#.
Their site claims *"Quick to learn. Even if you've never done UI Automation, and don't know F#."*, and I'm inclined to believe them.

Below is a snippet taken from the Canopy site. As you can see, the code is simple and easy to understand.

Also, FAKE integrates with Canopy, so you can [run automated browser tests as part of a CI build](http://fsharp.github.io/FAKE/canopy.html).

```fsharp
//start an instance of the firefox browser
start firefox

//this is how you define a test
"taking canopy for a spin" &&& fun _ ->
    //go to url
    url "http://lefthandedgoat.github.io/canopy/testpages/"

    //assert that the element with an id of 'welcome' has
    //the text 'Welcome'
    "#welcome" == "Welcome"

    //assert that the element with an id of 'firstName' has the value 'John'
    "#firstName" == "John"

    //change the value of element with
    //an id of 'firstName' to 'Something Else'
    "#firstName" << "Something Else"

    //verify another element's value, click a button,
    //verify the element is updated
    "#button_clicked" == "button not clicked"
    click "#button"
    "#button_clicked" == "button clicked"

//run all tests
run()
```


<a name="test-bdd"></a>
## 17. Use F# for Behaviour Driven Development

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/TestsInFsharp/TickSpec.StepDefinitions.fs).*

If you're not familiar with Behaviour Driven Development (BDD), the idea is that you express requirements in a way that is both human-readable and *executable*.

The standard format (Gherkin) for writing these tests uses the Given/When/Then syntax -- here's an example:

```text
Feature: Refunded or replaced items should be returned to stock

Scenario 1: Refunded items should be returned to stock
	Given a customer buys a black jumper
	And I have 3 black jumpers left in stock 
	When they return the jumper for a refund 
	Then I should have 4 black jumpers in stock
```

If you are using BDD already with .NET, you're probably using [SpecFlow](http://www.specflow.org/) or similar.

You should consider using [TickSpec](http://tickspec.codeplex.com/) instead
because, as with all things F#, the syntax is much more lightweight. 

For example, here's the full implementation of the scenario above. 

```fsharp
type StockItem = { Count : int }

let mutable stockItem = { Count = 0 }

let [<Given>] ``a customer buys a black jumper`` () = 
    ()
      
let [<Given>] ``I have (.*) black jumpers left in stock`` (n:int) =  
    stockItem <- { stockItem with Count = n }
      
let [<When>] ``they return the jumper for a refund`` () =  
    stockItem <- { stockItem with Count = stockItem.Count + 1 }
      
let [<Then>] ``I should have (.*) black jumpers in stock`` (n:int) =     
    let passed = (stockItem.Count = n)
    Assert.True(passed)
```
 
The C# equivalent has a lot more clutter, and the lack of double backtick syntax really hurts:

```csharp
[Given(@"a customer buys a black jumper")]
public void GivenACustomerBuysABlackJumper()
{
   // code
}

[Given(@"I have (.*) black jumpers left in stock")]
public void GivenIHaveNBlackJumpersLeftInStock(int n)
{
   // code
}
``` 

*Examples taken from the [TickSpec](http://tickspec.codeplex.com/) site.*

## Summary of testing in F# ##

You can of course combine all the test techniques we've seen so far ([as this slide deck demonstrates](http://www.slideshare.net/bartelink/testing-cinfdublinaltnet2013)):

* Unit tests (FsUnit, Unquote) and property-based tests (FsCheck). 
* Automated acceptance tests (or at least a smoke test) written in BDD (TickSpec) driven by browser automation (Canopy).
* Both types of tests run on every build (with FAKE).

There's a lot of advice on test automation out there, and you'll find that it is easy to port concepts from other languages to these F# tools. Have fun!



