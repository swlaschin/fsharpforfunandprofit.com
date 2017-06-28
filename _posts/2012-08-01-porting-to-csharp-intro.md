---
layout: post
title: "Porting from C# to F#: Introduction"
description: "Three approaches to porting existing C# code to F#"
nav: fsharp-types
seriesId: "Porting from C#"
seriesOrder: 1
---

*NOTE: Before reading this series, I suggest that you read the following series as a prerequisite: ["thinking functionally"](/series/thinking-functionally.html), ["expressions and syntax"](/series/expressions-and-syntax.html), and ["understanding F# types"](/series/understanding-fsharp-types.html).* 

For many developers, the next step after learning a new language might be to port some existing code over to it, so that they can get a good feel for the differences between the two languages.

As we pointed out earlier, functional languages are very different from imperative languages, and so trying to do a direct port of imperative code to a functional language is often not possible, and even if a crude port is done successfully, the ported code will probably not be using the functional model to its best advantage.

Of course, F# is a multi-paradigm language, and includes support for object-oriented and imperative techniques, but even so, a direct port will generally not be the best way to write the corresponding F# code.

So, in this series, we'll look at various approaches to porting existing C# code to F#. 

## Levels of porting sophistication ##

If you recall the diagram from an [earlier post](/posts/key-concepts), there are four key concepts that differentiate F# from C#.

* Function-oriented rather than object-oriented
* Expressions rather than statements 
* Algebraic types for creating domain models
* Pattern matching for flow of control

![four key concepts](/assets/img/four-concepts2.png)

And, as explained in that post and its sequels, these aspects are not just academic, but offer concrete benefits to you as a developer. 

So I have divided the porting process into three levels of sophistication (for lack of a better term), which represent how well the ported code exploits these benefits.

### Basic Level: Direct port ###

At this first level, the F# code is a direct port (where possible) of the C# code.  Classes and methods are used instead of modules and functions, and values are frequently mutated.

### Intermediate Level: Functional code ###

At the next level, the F# code has been refactored to be fully functional.  

* Classes and methods have been replaced by modules and functions, and values are generally immutable.  
* Higher order functions are used to replace interfaces and inheritance.
* Pattern matching is used extensively for control flow.
* Loops have been replaced with list functions such as "map" or recursion.

There are two different paths that can get you to this level. 

* The first path is to do a basic direct port to F#, and then refactor the F# code.
* The second path is to convert the existing imperative code to functional code while staying in C#, and only then port the functional C# code to functional F# code!  

The second option might seem clumsy, but for real code it will probably be both faster and more comfortable. Faster because you can use a tool such as Resharper to do the refactoring, and more comfortable because you are working in C# until the final port. This approach also makes it clear that the hard part is not the actual port from C# to F#, but the conversion of imperative code to functional code!  

### Advanced Level: Types represent the domain ###

At this final level, not only is the code functional, but the design itself has been changed to exploit the power of algebraic data types (especially union types). 

The domain will have been [encoded into types](/posts/designing-with-types-single-case-dus/) such that [illegal states are not even representable](/posts/designing-with-types-making-illegal-states-unrepresentable/), and [correctness is enforced at compile time](/posts/correctness-type-checking/).
For a concrete demonstration of the power of this approach, see the [shopping cart example](/posts/designing-for-correctness) in the ["why use F#" series](/series/why-use-fsharp.html) and the whole ["Designing with types" series](/series/designing-with-types.html).

This level can only be done in F#, and is not really practical in C#. 

### Porting diagram ###

Here is a diagram to help you visualize the various porting paths described above.

![four key concepts](/assets/img/porting-paths.png)
 
## The approach for this series ##

To see how these three levels work in practice, we'll apply them to some worked examples:

* The first example is a simple system for creating and scoring a ten-pin bowling game, based on the code from the well known "bowling game kata" described by "Uncle" Bob Martin. The original C# code has only one class and about 70 lines of code, but even so, it demonstrates a number of important principles.
* Next, we'll look at some shopping cart code, based on [this example](/posts/designing-for-correctness/).
* The final example is code that represents states for a subway turnstile system, also based on an example from Bob Martin. This example demonstrates how the union types in F# can represent a state transition model more easily than the OO approach. 

But first, before we get started on the detailed examples, we'll go back to basics and do some simple porting of some code snippets. That will be the topic of the next post.

