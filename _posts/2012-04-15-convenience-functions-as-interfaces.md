---
layout: post
title: "Functions as interfaces"
description: "OO design patterns can be trivial when functions are used"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 15
categories: [Convenience, Functions]
---


An important aspect of functional programming is that, in a sense, all functions are "interfaces", meaning that many of the roles that interfaces play in object-oriented design are implicit in the way that functions work. 

In fact, one of the critical design maxims, "program to an interface, not an implementation", is something you get for free in F#.

To see how this works, let's compare the same design pattern in C# and F#. For example, in C# we might want to use the "decorator pattern" to enhance some core code. 

Let's say that we have a calculator interface:

```csharp
interface ICalculator 
{
   int Calculate(int input);
}
```

And then a specific implementation:

```csharp
class AddingCalculator: ICalculator
{
   public int Calculate(int input) { return input + 1; }
}
```

And then if we want to add logging, we can wrap the core calculator implementation inside a logging wrapper.

```csharp
class LoggingCalculator: ICalculator
{
   ICalculator _innerCalculator;

   LoggingCalculator(ICalculator innerCalculator)
   {
      _innerCalculator = innerCalculator;
   }

   public int Calculate(int input) 
   { 
      Console.WriteLine("input is {0}", input);
      var result  = _innerCalculator.Calculate(input);
      Console.WriteLine("result is {0}", result);
      return result; 
   }
}
```

So far, so straightforward. But note that, for this to work, we must have defined an interface for the classes. If there had been no `ICalculator` interface, it would be necessary to retrofit the existing code.

And here is where F# shines. In F#, you can do the same thing without having to define the interface first. Any function can be transparently swapped for any other function as long as the signatures are the same. 

Here is the equivalent F# code.

```fsharp
let addingCalculator input = input + 1

let loggingCalculator innerCalculator input = 
   printfn "input is %A" input
   let result = innerCalculator input
   printfn "result is %A" result
   result
```   

In other words, the signature of the function *is* the interface.  
   
## Generic wrappers

Even nicer is that by default, the F# logging code can be made completely generic so that it will work for *any* function at all. Here are some examples:

```fsharp
let add1 input = input + 1
let times2 input = input * 2

let genericLogger anyFunc input = 
   printfn "input is %A" input   //log the input
   let result = anyFunc input    //evaluate the function
   printfn "result is %A" result //log the result
   result                        //return the result

let add1WithLogging = genericLogger add1
let times2WithLogging = genericLogger times2
```

The new "wrapped" functions can be used anywhere the original functions could be used -- no one can tell the difference!

```fsharp
// test
add1WithLogging 3
times2WithLogging 3

[1..5] |> List.map add1WithLogging
```

Exactly the same generic wrapper approach can be used for other things. For example, here is a generic wrapper for timing a function.

```fsharp
let genericTimer anyFunc input = 
   let stopwatch = System.Diagnostics.Stopwatch()
   stopwatch.Start() 
   let result = anyFunc input  //evaluate the function
   printfn "elapsed ms is %A" stopwatch.ElapsedMilliseconds
   result

let add1WithTimer = genericTimer add1WithLogging 

// test
add1WithTimer 3
```

The ability to do this kind of generic wrapping is one of the great conveniences of the function-oriented approach. You can take any function and create a similar function based on it.  As long as the new function has exactly the same inputs and outputs as the original function, the new can be substituted for the original anywhere.  Some more examples:

* It is easy to write a generic caching wrapper for a slow function, so that the value is only calculated once.
* It is also easy to write a generic "lazy" wrapper for a function, so that the inner function is only called when a result is needed

## The strategy pattern 

We can apply this same approach to another common design pattern, the "strategy pattern." 

Let's use the familiar example of inheritance: an `Animal` superclass with `Cat` and `Dog` subclasses, each of which overrides a `MakeNoise()` method to make different noises. 

In a true functional design, there are no subclasses, but instead the `Animal` class would have a `NoiseMaking` function that would be passed in with the constructor.   This approach is exactly the same as the "strategy" pattern in OO design.

```fsharp
type Animal(noiseMakingStrategy) = 
   member this.MakeNoise = 
      noiseMakingStrategy() |> printfn "Making noise %s" 
   
// now create a cat 
let meowing() = "Meow"
let cat = Animal(meowing)
cat.MakeNoise

// .. and a dog
let woofOrBark() = if (System.DateTime.Now.Second % 2 = 0) 
                   then "Woof" else "Bark"
let dog = Animal(woofOrBark)
dog.MakeNoise
dog.MakeNoise  //try again a second later
```

Note that again, we do not have to define any kind of `INoiseMakingStrategy` interface first. Any function with the right signature will work.
As a consequence, in the functional model, the standard .NET "strategy" interfaces such as `IComparer`, `IFormatProvider`, and `IServiceProvider` become irrelevant.

Many other design patterns can be simplified in the same way.

