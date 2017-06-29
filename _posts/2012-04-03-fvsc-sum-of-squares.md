---
layout: post
title: "Comparing F# with C#: A simple sum"
description: "In which we attempt to sum the squares from 1 to N without using a loop"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 3
categories: [F# vs C#]
---


To see what some real F# code looks like, let's start with a simple problem:  "sum the squares from 1 to N". 

We'll compare an F# implementation with a C# implementation. First, the F# code:

```fsharp
// define the square function
let square x = x * x

// define the sumOfSquares function
let sumOfSquares n = 
   [1..n] |> List.map square |> List.sum

// try it
sumOfSquares 100
```

The mysterious looking `|>` is called the pipe operator. It just pipes the output of one expression into the input of the next. So the code for `sumOfSquares` reads as:

1. Create a list of 1 to n (square brackets construct a list).
1. Pipe the list into the library function called `List.map`, transforming the input list into an output list using the "square" function we just defined.
1. Pipe the resulting list of squares into the library function called `List.sum`. Can you guess what it does?
1. There is no explicit "return" statement. The output of `List.sum` is the overall result of the function.

Next, here's a C# implementation using the classic (non-functional) style of a C-based language. (A more functional version using LINQ is discussed later.)

```csharp
public static class SumOfSquaresHelper
{
   public static int Square(int i)
   {
      return i * i;
   }

   public static int SumOfSquares(int n)
   {
      int sum = 0;
      for (int i = 1; i <= n; i++)
      {
         sum += Square(i);
      }
      return sum;
   }
}
```

What are the differences?

* The F# code is more compact
* The F# code didn't have any type declarations
* F# can be developed interactively

Let's take each of these in turn.

### Less code

The most obvious difference is that there is a lot more C# code. 13 C# lines compared with 3 F# lines (ignoring comments). The C# code has lots of "noise", things like curly braces, semicolons, etc. And in C# the functions cannot stand alone, but need to be added to some class ("SumOfSquaresHelper"). F# uses whitespace instead of parentheses, needs no line terminator, and the functions can stand alone. 

In F# it is common for entire functions to be written on one line, as the "square" function is. The `sumOfSquares` function could also have been written on one line. In C# this is normally frowned upon as bad practice.

When a function does have multiple lines, F# uses indentation to indicate a block of code, which eliminates the need for braces. (If you have ever used Python, this is the same idea). So the `sumOfSquares` function could also have been written this way:

```fsharp
let sumOfSquares n = 
   [1..n] 
   |> List.map square 
   |> List.sum
```

The only drawback is that you have to indent your code carefully. Personally, I think it is worth the trade-off. 

### No type declarations

The next difference is that the C# code has to explicitly declare all the types used. For example, the `int i` parameter and `int SumOfSquares` return type.
Yes, C# does allow you to use the "var" keyword in many places, but not for parameters and return types of functions.

In the F# code we didn't declare any types at all. This is an important point: F# looks like an untyped language,
but it is actually just as type-safe as C#, in fact, even more so!
F# uses a technique called "type inference" to infer the types you are using from their context. It works amazingly very well most of the time, and reduces the code complexity immensely.

In this case, the type inference algorithm notes that we started with a list of integers. That in turn implies that the square function and the sum function must be taking ints as well, and that the final value must be an int. You can see what the inferred types are by looking at the result of the compilation in the interactive window. You'll see something like:

```fsharp
val square : int -> int
```

which means that the "square" function takes an int and returns an int.

If the original list had used floats instead, the type inference system would have deduced that the square function used floats instead. Try it and see:

```fsharp
// define the square function
let squareF x = x * x

// define the sumOfSquares function
let sumOfSquaresF n = 
   [1.0 .. n] |> List.map squareF |> List.sum  // "1.0" is a float

sumOfSquaresF 100.0
```

The type checking is very strict! If you try using a list of floats (`[1.0..n]`) in the original `sumOfSquares` example, or a list of ints (`[1 ..n]`) in the `sumOfSquaresF` example, you will get a type error from the compiler.

### Interactive development

Finally, F# has an interactive window where you can test the code immediately and play around with it. In C# there is no easy way to do this. 

For example, I can write my square function and immediately test it:

```fsharp
// define the square function
let square x = x * x

// test
let s2 = square 2
let s3 = square 3
let s4 = square 4
```

When I am satisfied that it works, I can move on to the next bit of code.

This kind of interactivity encourages an incremental approach to coding that can become addictive!

Furthermore, many people claim that designing code interactively enforces good design practices such as decoupling and explicit dependencies,
and therefore, code that is suitable for interactive evaluation will also be code that is easy to test. Conversely, code that cannot be
tested interactively will probably be hard to test as well.

### The C# code revisited

My original example was written using "old-style" C#.  C# has incorporated a lot of functional features, and it is possible to rewrite the example in a more compact way using the LINQ extensions. 

So here is another C# version -- a line-for-line translation of the F# code.

```csharp
public static class FunctionalSumOfSquaresHelper
{
   public static int SumOfSquares(int n)
   {
      return Enumerable.Range(1, n)
         .Select(i => i * i)
         .Sum();
   }
}
```

However, in addition to the noise of the curly braces and periods and semicolons, the C# version needs to declare the parameter and return types, unlike the F# version. 

Many C# developers may find this a trivial example, but still resort back to loops when the logic becomes more complicated. In F# though, you will almost never see explicit loops like this.
See for example, [this post on eliminating boilerplate from more complicated loops](http://fsharpforfunandprofit.com/posts/conciseness-extracting-boilerplate/).


