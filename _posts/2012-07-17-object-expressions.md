---
layout: post
title: "Object expressions"
description: ""
nav: fsharp-types
seriesId: "Object-oriented programming in F#"
seriesOrder: 5
categories: [Object-oriented, Interfaces]
---

So as we saw in the [previous post](/posts/interfaces/), implementing interfaces in F# is a bit more awkward than in C#. But F# has a trick up its sleeve, called "object expressions".

With object expressions, you can implement an interface on-the-fly, without having to create a class.  

### Implementing interfaces with object expressions

Object expressions are most commonly used to implement interfaces. 
To do this, you use the syntax `new MyInterface with ...`, and the wrap the whole thing in curly braces (one of the few uses for them in F#!)

Here is some example code that creates a number of objects, each of which implements `IDisposable`.

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

If you execute this code, you will see the output below. You can see that `Dispose()` is indeed being called when the objects go out of scope. 

<pre>
using first resource
    do something with   inner resource 1
    inner resource 1 disposed
    do something with   inner resource 2
    inner resource 2 disposed
    do something with   inner resource 3
    inner resource 3 disposed
using second resource
done.
second resource disposed
first resource disposed
</pre>

We can take the same approach with the `IAddingService` and create one on the fly as well.

```fsharp
let makeAdder id = 
   { new IAddingService with 
     member this.Add x y =
         printfn "Adder%i is adding" id 
         let result = x + y   
         printfn "%i + %i = %i" x y result 
         result 
         }

let testAdders = 
    for i in [1..3] do
        let adder = makeAdder i
        let result = adder.Add i i 
        () //ignore result
```

Object expressions are extremely convenient, and can greatly reduce the number of classes you need to create if you are interacting with an interface heavy library.
