---
layout: page
title: "Why use F#?"
description: "Why you should consider using F# for your next project"
nav: why-use-fsharp
hasIcons: 1
image: "/assets/img/four-concepts2.png"
---

Although F# is great for specialist areas such as scientific or data analysis, it is also an excellent choice for enterprise development. Here are five good reasons why you should consider using F# for  your next project. 

<div class="row whyuse" >  
<div class="col-md-6" style="float:right;" markdown="1">

## <img src="/assets/img/glyphicons/glyphicons_030_pencil.png" class="bs-icon"> Conciseness

F# is not cluttered up with [coding "noise"](/posts/fvsc-sum-of-squares/) such as curly brackets, semicolons and so on. 

You almost never have to specify the type of an object, thanks to a powerful [type inference system](/posts/conciseness-type-inference/). 
And, compared with C#, it generally takes [fewer lines of code](/posts/fvsc-download/) to solve the same problem.

</div>
<div class="col-md-6" style="float:left;" markdown="1">

```fsharp
// one-liners
[1..100] |> List.sum |> printfn "sum=%d"

// no curly braces, semicolons or parentheses
let square x = x * x
let sq = square 42 

// simple types in one line
type Person = {First:string; Last:string}

// complex types in a few lines
type Employee = 
  | Worker of Person
  | Manager of Employee list

// type inference
let jdoe = {First="John"; Last="Doe"}
let worker = Worker jdoe
```

</div>
</div>


<div class="row whyuse" > 
<div class="col-md-6" style="float:right;" markdown="1">

## <img src="/assets/img/glyphicons/glyphicons_343_thumbs_up.png" class="bs-icon"> Convenience

Many common programming tasks are much simpler in F#.  This includes things like creating and using [complex type definitions](/posts/conciseness-type-definitions/), doing [list processing](/posts/conciseness-extracting-boilerplate/), [comparison and equality](/posts/convenience-types/), [state machines](/posts/designing-with-types-representing-states/), and much more.

And because functions are first class objects, it is very easy to create powerful and reusable code by creating functions that have <a href="/posts/conciseness-extracting-boilerplate/">other functions as parameters</a>, or that <a href="/posts/conciseness-functions-as-building-blocks/">combine existing functions</a> to create new functionality. 

</div>
    
<div class="col-md-6" style="float:left;" markdown="1">

```fsharp
// automatic equality and comparison
type Person = {First:string; Last:string}
let person1 = {First="john"; Last="Doe"}
let person2 = {First="john"; Last="Doe"}
printfn "Equal? %A"  (person1 = person2)

// easy IDisposable logic with "use" keyword
use reader = new StreamReader(..)

// easy composition of functions
let add2times3 = (+) 2 >> (*) 3
let result = add2times3 5
```

</div>
</div>

<div class="row whyuse" > 
<div class="col-md-6" style="float:right;" markdown="1">

<h2><img src="/assets/img/glyphicons/glyphicons_150_check.png" class="bs-icon"> Correctness</h2>


F# has a <a href="/posts/correctness-type-checking/">powerful type system</a> which prevents many common errors such as <a href="/posts/the-option-type/#option-is-not-null">null reference exceptions</a>.

Values are <a href="/posts/correctness-immutability/">immutable by default</a>, which prevents a large class of errors.

In addition, you can often encode business logic using the <a href="/posts/correctness-exhaustive-pattern-matching/">type system</a> itself in such a way that it is actually <a href="/posts/designing-for-correctness/">impossible to write incorrect code</a> or mix up <a href="/posts/units-of-measure/">units of measure</a>, greatly reducing the need for unit tests.   

</div>

<div class="col-md-6" style="float:left;" markdown="1">

```fsharp
// strict type checking
printfn "print string %s" 123 //compile error

// all values immutable by default
person1.First <- "new name"  //assignment error 

// never have to check for nulls
let makeNewString str = 
   //str can always be appended to safely
   let newString = str + " new!"
   newString

// embed business logic into types
emptyShoppingCart.remove   // compile error!

// units of measure
let distance = 10<m> + 10<ft> // error!
```

</div>
</div>


<div class="row whyuse" > 
<div class="col-md-6" style="float:right;" markdown="1">

<h2><img src="/assets/img/glyphicons/glyphicons_054_clock.png" class="bs-icon"> Concurrency</h2>


F# has a number of built-in libraries to help when more than one thing at a time is happening. Asynchronous programming is <a href="/posts/concurrency-async-and-parallel/">very easy</a>, as is parallelism. F# also has a built-in <a href="/posts/concurrency-actor-model/">actor model</a>, and excellent support for event handling and <a href="/posts/concurrency-reactive/">functional reactive programming</a>. 

And of course, because data structures are immutable by default, sharing state and avoiding locks is much easier.

</div>

<div class="col-md-6" style="float:left;" markdown="1">

```fsharp
// easy async logic with "async" keyword
let! result = async {something}

// easy parallelism
Async.Parallel [ for i in 0..40 -> 
      async { return fib(i) } ]

// message queues
MailboxProcessor.Start(fun inbox-> async{
	let! msg = inbox.Receive()
	printfn "message is: %s" msg
	})
```

</div>
</div>	

<div class="row whyuse" > 
<div class="col-md-6" style="float:right;" markdown="1">
<h2><img src="/assets/img/glyphicons/glyphicons_280_settings.png" class="bs-icon"> Completeness</h2>


Although it is a functional language at heart, F# does support other styles which are not 100% pure, which makes it much easier to interact with the non-pure world of web sites, databases, other applications, and so on. In particular, F# is designed as a hybrid functional/OO language, so it can do <a href="/posts/completeness-anything-csharp-can-do/">virtually everything that C# can do</a>.  

Of course, F# is <a href="/posts/completeness-seamless-dotnet-interop/">part of the .NET ecosystem</a>, which gives you seamless access to all the third party .NET libraries and tools. It runs on most platforms, including Linux and smart phones (via Mono).

Finally, it is well integrated with Visual Studio, which means you get a great IDE with IntelliSense support, a debugger, and many plug-ins for unit tests, source control, and other development tasks. Or on Linux, you can use the MonoDevelop IDE instead.


</div>

<div class="col-md-6" style="float:left;" markdown="1">

```fsharp
// impure code when needed
let mutable counter = 0

// create C# compatible classes and interfaces
type IEnumerator<'a> = 
    abstract member Current : 'a
    abstract MoveNext : unit -> bool 

// extension methods
type System.Int32 with
    member this.IsEven = this % 2 = 0

let i=20
if i.IsEven then printfn "'%i' is even" i
	
// UI code
open System.Windows.Forms 
let form = new Form(Width = 400, Height = 300, 
   Visible = true, Text = "Hello World") 
form.TopMost <- true
form.Click.Add (fun args -> printfn "clicked!")
form.Show()
```

</div>
	
</div>

The following series of posts demonstrates each of these F# benefits, using standalone snippets of F# code (and often with C# code for comparison).  

<div class="well">
    {% for post in site.data.series["Why use F#?"].posts %}
	<div><a href="{{ post.url }}/" title="{{ post.title | escape }}">{{ post.seriesOrder }}. {{ post.title | escape }}</a></div>
	{% endfor %}
</div>

