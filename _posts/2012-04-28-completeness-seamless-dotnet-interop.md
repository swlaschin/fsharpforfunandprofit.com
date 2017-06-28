---
layout: post
title: "Seamless interoperation with .NET libraries"
description: "Some convenient features for working with .NET libraries"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 28
categories: [Completeness]
---


We have already seen many examples of F#'s use with the .NET libraries, such as using `System.Net.WebRequest` and `System.Text.RegularExpressions`. And the integration was indeed seamless.

For more complex requirements, F# natively supports .NET classes, interfaces, and structures, so the interop is still very straightforward.  For example, you can write an `ISomething` interface in C# and have the implementation be done in F#. 

But not only can F# call into existing .NET code, it can also expose almost any .NET API back to other languages. For example, you can write classes and methods in F# and expose them to C#, VB or COM.  You can even do the above example backwards -- define an `ISomething` interface in F# and have the implementation be done in C#! The benefit of all this is that you don't have to discard any of your existing code base; you can start using F# for some things while retaining C# or VB for others, and pick the best tool for the job.  

In addition to the tight integration though, there are a number of nice features in F# that often make working with .NET libraries more convenient than C# in some ways. Here are some of my favorites:

* You can use `TryParse` and `TryGetValue` without passing an "out" parameter.
* You can resolve method overloads by using argument names, which also helps with type inference.
* You can use "active patterns" to convert .NET APIs into more friendly code.
* You can dynamically create objects from an interface such as `IDisposable` without creating a concrete class.
* You can mix and match "pure" F# objects with existing .NET APIs

## TryParse and TryGetValue ##

The `TryParse` and `TryGetValue` functions for values and dictionaries are frequently used to avoid extra exception handling. But the C# syntax is a bit clunky. Using them from F# is more elegant because F# will automatically convert the function into a tuple where the first element is the function return value and the second is the "out" parameter. 

```fsharp
//using an Int32
let (i1success,i1) = System.Int32.TryParse("123");
if i1success then printfn "parsed as %i" i1 else printfn "parse failed"

let (i2success,i2) = System.Int32.TryParse("hello");
if i2success then printfn "parsed as %i" i2 else printfn "parse failed"

//using a DateTime
let (d1success,d1) = System.DateTime.TryParse("1/1/1980");
let (d2success,d2) = System.DateTime.TryParse("hello");

//using a dictionary
let dict = new System.Collections.Generic.Dictionary<string,string>();
dict.Add("a","hello")
let (e1success,e1) = dict.TryGetValue("a");
let (e2success,e2) = dict.TryGetValue("b");
```

## Named arguments to help type inference

In C# (and .NET in general), you can have overloaded methods with many different parameters. F# can have trouble with this. For example, here is an attempt to create a `StreamReader`:

```fsharp
let createReader fileName = new System.IO.StreamReader(fileName)
// error FS0041: A unique overload for method 'StreamReader' 
//               could not be determined
```

The problem is that F# does not know if the argument is supposed to be a string or a stream. You could explicitly specify the type of the argument, but that is not the F# way! 

Instead, a nice workaround is enabled by the fact that in F#, when calling methods in .NET libraries, you can specify named arguments.

```fsharp
let createReader2 fileName = new System.IO.StreamReader(path=fileName)
```

In many cases, such as the one above, just using the argument name is enough to resolve the type issue. And using explicit argument names can often help to make the code more legible anyway.

## Active patterns for .NET functions ##

There are many situations where you want to use pattern matching against .NET types, but the native libraries do not support this. Earlier, we briefly touched on the F# feature called "active patterns" which allows you to dynamically create choices to match on. This can be very for useful .NET integration. 

A common case is that a .NET library class has a number of mutually exclusive `isSomething`, `isSomethingElse` methods, which have to be tested with horrible looking cascading if-else statements. Active patterns can hide all the ugly testing, letting the rest of your code use a more natural approach. 

For example, here's the code to test for various `isXXX` methods for `System.Char`.

```fsharp
let (|Digit|Letter|Whitespace|Other|) ch = 
   if System.Char.IsDigit(ch) then Digit
   else if System.Char.IsLetter(ch) then Letter
   else if System.Char.IsWhiteSpace(ch) then Whitespace
   else Other
```

Once the choices are defined, the normal code can be straightforward: 

```fsharp
let printChar ch = 
  match ch with
  | Digit -> printfn "%c is a Digit" ch
  | Letter -> printfn "%c is a Letter" ch
  | Whitespace -> printfn "%c is a Whitespace" ch
  | _ -> printfn "%c is something else" ch

// print a list
['a';'b';'1';' ';'-';'c'] |> List.iter printChar
```

Another common case is when you have to parse text or error codes to determine the type of an exception or result. Here's an example that uses an active pattern to parse the error number associated with `SqlExceptions`, making them more palatable. 

First, set up the active pattern matching on the error number:

```fsharp
open System.Data.SqlClient

let (|ConstraintException|ForeignKeyException|Other|) (ex:SqlException) = 
   if ex.Number = 2601 then ConstraintException 
   else if ex.Number = 2627 then ConstraintException 
   else if ex.Number = 547 then ForeignKeyException 
   else Other 
```

Now we can use these patterns when processing SQL commands:

```fsharp
let executeNonQuery (sqlCommmand:SqlCommand) = 
    try
       let result = sqlCommmand.ExecuteNonQuery()
       // handle success
    with 
    | :?SqlException as sqlException -> // if a SqlException
        match sqlException with         // nice pattern matching
        | ConstraintException  -> // handle constraint error
        | ForeignKeyException  -> // handle FK error
        | _ -> reraise()          // don't handle any other cases
    // all non SqlExceptions are thrown normally
```

## Creating objects directly from an interface ##

F# has another useful feature called "object expressions". This is the ability to directly create objects from an interface or abstract class without having to define a concrete class first. 

In the example below, we create some objects that implement `IDisposable` using a `makeResource` helper function.

```fsharp
// create a new object that implements IDisposable
let makeResource name = 
   { new System.IDisposable 
     with member this.Dispose() = printfn "%s disposed" name }

let useAndDisposeResources = 
    use r1 = makeResource "first resource"
    printfn "using first resource" 
    for i in [1..3] do
        let resourceName = sprintf "\tinner resource %d" i
        use temp = makeResource resourceName 
        printfn "\tdo something with %s" resourceName 
    use r2 = makeResource "second resource"
    printfn "using second resource" 
    printfn "done." 
```

The example also demonstrates how the "`use`" keyword automatically disposes a resource when it goes out of scope. Here is the output:


	using first resource
		do something with 	inner resource 1
		inner resource 1 disposed
		do something with 	inner resource 2
		inner resource 2 disposed
		do something with 	inner resource 3
		inner resource 3 disposed
	using second resource
	done.
	second resource disposed
	first resource disposed

## Mixing .NET interfaces with pure F# types ##

The ability to create instances of an interface on the fly means that it is easy to mix and match interfaces from existing APIs with pure F# types.

For example, say that you have a preexisting API which uses the `IAnimal` interface, as shown below.

```fsharp
type IAnimal = 
   abstract member MakeNoise : unit -> string

let showTheNoiseAnAnimalMakes (animal:IAnimal) = 
   animal.MakeNoise() |> printfn "Making noise %s" 
```

But we want to have all the benefits of pattern matching, etc, so we have created pure F# types for cats and dogs instead of classes. 

```fsharp
type Cat = Felix | Socks
type Dog = Butch | Lassie 
```

But using this pure F# approach means that that we cannot pass the cats and dogs to the `showTheNoiseAnAnimalMakes` function directly.

However, we don't have to create new sets of concrete classes just to implement `IAnimal`. Instead, we can dynamically create the `IAnimal` interface by extending the pure F# types.

```fsharp
// now mixin the interface with the F# types
type Cat with
   member this.AsAnimal = 
        { new IAnimal 
          with member a.MakeNoise() = "Meow" }

type Dog with
   member this.AsAnimal = 
        { new IAnimal 
          with member a.MakeNoise() = "Woof" }
```

Here is some test code:

```fsharp
let dog = Lassie
showTheNoiseAnAnimalMakes (dog.AsAnimal)

let cat = Felix
showTheNoiseAnAnimalMakes (cat.AsAnimal)
```

This approach gives us the best of both worlds. Pure F# types internally, but the ability to convert them into interfaces as needed to interface with libraries.

## Using reflection to examine F# types ##

F# gets the benefit of the .NET reflection system, which means that you can do all sorts of interesting things that are not directly available to you using the syntax of the language itself.  The `Microsoft.FSharp.Reflection` namespace has a number of functions that are designed to help specifically with F# types.

For example, here is a way to print out the fields in a record type, and the choices in a union type.

```fsharp
open System.Reflection
open Microsoft.FSharp.Reflection

// create a record type...
type Account = {Id: int; Name: string}

// ... and show the fields
let fields = 
    FSharpType.GetRecordFields(typeof<Account>)
    |> Array.map (fun propInfo -> propInfo.Name, propInfo.PropertyType.Name)

// create a union type...
type Choices = | A of int | B of string

// ... and show the choices
let choices = 
    FSharpType.GetUnionCases(typeof<Choices>)
    |> Array.map (fun choiceInfo -> choiceInfo.Name)
```
