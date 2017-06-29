---
layout: post
title: "Partial Application"
description: "How to fix some of a function's parameters"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 16
categories: [Convenience, Functions, Partial Application]
---

A particularly convenient feature of F# is that complicated functions with many parameters can have some of the parameters fixed or "baked in" and yet leave other parameters open.  In this post, we'll take a quick look at how this might be used in practice.

Let's start with a very simple example of how this works. We'll start with a trivial function:

```fsharp
// define a adding function
let add x y = x + y

// normal use 
let z = add 1 2
```

But we can do something strange as well -- we can call the function with only one parameter!

```fsharp
let add42 = add 42
```

The result is a new function that has the "42" baked in, and now takes only one parameter instead of two!  This technique is called "partial application", and it means that, for any function, you can "fix" some of the parameters and leave other ones open to be filled in later.

```fsharp
// use the new function
add42 2
add42 3
```

With that under our belt, let's revisit the generic logger that we saw earlier:

```fsharp
let genericLogger anyFunc input = 
   printfn "input is %A" input   //log the input
   let result = anyFunc input    //evaluate the function
   printfn "result is %A" result //log the result
   result                        //return the result
```

Unfortunately, I have hard-coded the logging operations.  Ideally, I'd like to make this more generic so that I can choose how logging is done. 

Of course, F# being a functional programming language, we will do this by passing functions around. 

In this case we would pass "before" and "after" callback functions to the library function, like this:

```fsharp
let genericLogger before after anyFunc input = 
   before input               //callback for custom behavior
   let result = anyFunc input //evaluate the function
   after result               //callback for custom behavior
   result                     //return the result
```

You can see that the logging function now has four parameters. The "before" and "after" actions are passed in as explicit parameters as well as the function and its input. To use this in practice, we just define the functions and pass them in to the library function along with the final int parameter:

```fsharp
let add1 input = input + 1

// reuse case 1
genericLogger 
    (fun x -> printf "before=%i. " x) // function to call before 
    (fun x -> printfn " after=%i." x) // function to call after
    add1                              // main function
    2                                 // parameter 

// reuse case 2
genericLogger
    (fun x -> printf "started with=%i " x) // different callback 
    (fun x -> printfn " ended with=%i" x) 
    add1                              // main function
    2                                 // parameter 
```

This is a lot more flexible. I don't have to create a new function every time I want to change the behavior -- I can define the behavior on the fly. 

But you might be thinking that this is a bit ugly. A library function might expose a number of callback functions and it would be inconvenient to have to pass the same functions in over and over.

Luckily, we know the solution for this. We can use partial application to fix some of the parameters. So in this case, let's define a new function which fixes the `before` and `after` functions, as well as the `add1` function, but leaves the final parameter open.

```fsharp
// define a reusable function with the "callback" functions fixed
let add1WithConsoleLogging = 
    genericLogger
        (fun x -> printf "input=%i. " x) 
        (fun x -> printfn " result=%i" x)
        add1
        // last parameter NOT defined here yet!
```

The new "wrapper" function is called with just an int now, so the code is much cleaner. As in the earlier example, it can be used anywhere the original `add1` function could be used without any changes.

```fsharp
add1WithConsoleLogging 2
add1WithConsoleLogging 3
add1WithConsoleLogging 4
[1..5] |> List.map add1WithConsoleLogging 
```

## The functional approach in C# ##

In a classical object-oriented approach, we would probably have used inheritance to do this kind of thing. For instance, we might have had an abstract `LoggerBase` class, with virtual methods for "`before`" and "`after`" and the function to execute.  And then to implement a particular kind of behavior, we would have created a new subclass and overridden the virtual methods as needed.

But classical style inheritance is now becoming frowned upon in object-oriented design, and composition of objects is much preferred. And indeed, in "modern" C#, we would probably write the code in the same way as F#, either by using events or by passing functions in.

Here's the F# code translated into C# (note that I had to specify the types for each Action)

```csharp
public class GenericLoggerHelper<TInput, TResult>
{
    public TResult GenericLogger(
        Action<TInput> before,
        Action<TResult> after,
        Func<TInput, TResult> aFunc,
        TInput input)
    {
        before(input);             //callback for custom behavior
        var result = aFunc(input); //do the function
        after(result);             //callback for custom behavior
        return result;
    }
}
```

And here it is in use:

```csharp
[NUnit.Framework.Test]
public void TestGenericLogger()
{
    var sut = new GenericLoggerHelper<int, int>();
    sut.GenericLogger(
        x => Console.Write("input={0}. ", x),
        x => Console.WriteLine(" result={0}", x),
        x => x + 1,
        3);
}
```

In C#, this style of programming is required when using the LINQ libraries, but many developers have not embraced it fully to make their own code more generic and adaptable. And it's not helped by the ugly `Action<>` and `Func<>` type declarations that are required. But it can certainly make the code much more reusable. 
