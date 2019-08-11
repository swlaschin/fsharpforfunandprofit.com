---
layout: page
title: "Explore this site"
nav: site-contents
description: "A guide for visitors"
hasNoCode: 1
---


## Getting started

* [Why use F#?](/why-use-fsharp/) A one page tour of F#. If you like it, there is a [30 page series](/series/why-use-fsharp.html) that goes into detail on each feature.
* [Installing and using F#](/installing-and-using/) will get you started.
* [F# syntax in 60 seconds](/posts/fsharp-in-60-seconds/) 
* [Learning F#](/learning-fsharp/) has tips to help you learn more effectively. 
* [Troubleshooting F#](/troubleshooting-fsharp/) for when you have problems getting your code to compile.

and then you can try...

* [Twenty six low-risk ways to use F# at work](/posts/low-risk-ways-to-use-fsharp-at-work/). You can start right now -- no permission needed!

## Thinking Functionally

* [Thinking functionally](/series/thinking-functionally.html) starts from basics and explains how and why functions work the way they do.
* The ["Introduction to functional design patterns"](/fppatterns/) talk and the ["Railway Oriented Programming"](/rop/) talk provide more examples of the functional way of thinking.

## Understanding F# ##

The following are tutorials on the key concepts of F#. 

* [Talk: F# for C# Developers](/csharp/)
* [Expressions and syntax](/series/expressions-and-syntax.html) covers the common expressions such as pattern matching, and has a post on indentation.
* [Understanding F# types](/series/understanding-fsharp-types.html) explains how to define and use the various types, including tuples, records, unions, and options.
* [Choosing between collection functions](/posts/list-module-functions/). If you are coming to F# from C#, the large number of list functions can be overwhelming, so I have written this post to help guide you to the one you want.
* [Understanding computation expressions](/series/computation-expressions.html) demystifies them and shows how you can create your own.
* [Object-oriented programming in F#](/series/object-oriented-programming-in-fsharp.html).
* [Organizing modules in a project](/posts/recipe-part3/)  in which we look at the overall structure of a F# project. In particular: (a) what code should be in which modules and (b) how the modules should be organized within a project.
* [Dependency cycles](/series/dependency-cycles.html). One of the most common complaints about F# is that it requires code to be in dependency order. That is, you cannot use forward references to code that hasn’t been seen by the compiler yet. In this series, I discuss dependency cycles, why they are bad, and how to get rid of them.     
* [Porting from C#](/series/porting-from-csharp.html). Do you want to port C# code to F#? In this series of posts we’ll look at various approaches to this, and the design decisions and trade-offs involved.     

## Functional Design 

These talks and posts show how FP-oriented design is different from OO design.

* [Talk: Domain modelling with the F# type system](/ddd/). Statically typed functional programming languages like F# encourage a very different way of thinking about types. The type system is your friend, not an annoyance, and can be used in many ways that might not be familiar to OO programmers. 
Types can be used to represent the domain in a fine-grained, self documenting way. And in many cases, types can even be used to encode business rules so that you literally cannot create incorrect code. You can then use the static type checking almost as an instant unit test – making sure that your code is correct at compile time. 
* [Designing with types](/series/designing-with-types.html) explains how to use types as part of the design process, making illegal states unrepresentable.
* [Algebraic type sizes and domain modelling](/posts/type-size-and-design/)
* [Talk: The Power of Composition](/composition/). Composition is a fundamental principle of functional programming, but how is it different from an object-oriented approach, and how do you use it in practice? In this talk for beginners, we'll start by going over the basic concepts of functional programming, and then look at some different ways that composition can be used to build large things from small things.
* [Talk: Thirteen ways of looking at a turtle](/turtle/) demonstrates many different techniques for implementing a turtle graphics API, including state monads, agents, interpreters, and more! See linked page for associated posts.
* [Talk: Enterprise Tic-Tac-Toe](/ettt/). Follow along as I ridiculously over-engineer a simple game to demonstrate how functional programming can be used to create a real-world “enterprise-ready” application.  
* [Talk: Designing with Capabilities](/cap/) demonstrates a very different approach to design using "capabilities" and the principle of least authority. I’ll show how using these design techniques throughout your core domain (and not just at your API boundary) also leads to well-designed and modular code. 

## Functional Patterns

These talks and posts explain some core patterns in functional programming -- concepts such as "map", "bind", monads and more.

* [Talk: Introduction to functional design patterns](/fppatterns/).
* [Talk: Railway Oriented Programming](/rop/): A functional approach to error handling. See linked page for associated posts.
* [Monoids without tears](/posts/monoids-without-tears/): A mostly mathless discussion of a common functional pattern.
* [Talk: Understanding Parser Combinators](/parser/): Creating a parser combinator library from scratch. See linked page for associated posts.
* [Talk: State Monad](/monadster/): An introduction to handling state using the tale of Dr Frankenfunctor and the Monadster.   See linked page for associated posts.
* [Reader Monad](/posts/elevated-world-6/): Reinventing the Reader monad.
* [Map, bind, apply, lift, sequence and traverse](/series/map-and-bind-and-apply-oh-my.html): A series describing some of the core functions for dealing with generic data types. 
* [Fold and recursive types](/series/recursive-types-and-folds.html): A look at recursive types, catamorphisms, tail recursion, the difference between left and right folds, and more.
* [The "A functional approach to authorization" Series](/series/a-functional-approach-to-authorization.html). How to handle the common security challenge of authorization using "capabilities". Also available as a [talk](/cap/).

{% include book_page_pdf.inc %}

## Testing

* [An introduction to property-based testing](/posts/property-based-testing/)
* [Choosing properties for property-based testing](/posts/property-based-testing-2/)
* [Talk: Property-based testing](/pbt/): the lazy programmer's guide to writing 1000's of tests.

## Examples and Walkthroughs

These posts provide detailed worked examples with lots of code!

* [Commentary on 'Roman Numerals Kata with Commentary'](/posts/roman-numeral-kata/). My approach to the Roman Numerals Kata.
* [Worked example: Designing for correctness](/posts/designing-for-correctness/): How to make illegal states unrepresentable (a shopping cart example).
* [Worked example: Stack based calculator](/posts/stack-based-calculator/): Using a simple stack to demonstrate the power of combinators.
* [Worked example: Parsing commmand lines](/posts/pattern-matching-command-line/): Using pattern matching in conjunction with custom types.
* [Worked example: Roman numerals](/posts/roman-numerals/): Another pattern matching example.
* [Calculator Walkthrough](/posts/calculator-design/): The type-first approach to designing a Calculator.
* [Enterprise Tic-Tac-Toe](/posts/enterprise-tic-tac-toe/): A walkthrough of the design decisions in a purely functional implementation
* [Writing a JSON Parser](/posts/understanding-parser-combinators-4/).

## Specific topics in F# ##

General:

* [Four key concepts](/posts/key-concepts/) that differentiate F# from a standard imperative language.
* [Understanding F# indentation](/posts/fsharp-syntax/).
* [The downsides of using methods](/posts/type-extensions/#downsides-of-methods).
* [Formatted text using `printf`](/posts/printf/).

Functions:

* [Currying](/posts/currying/).
* [Partial Application](/posts/partial-application/).
  
Control Flow: 

* [Match..with expressions](/posts/match-expression/) and [creating folds to hide the matching](/posts/match-expression/#folds).
* [If-then-else and loops](/posts/control-flow-expressions/).
* [Exceptions](/posts/exceptions/). 

Types: 

* [Option Types](/posts/the-option-type/) especially on why [None is not the same as null](/posts/the-option-type/#option-is-not-null).
* [Record Types](/posts/records/).
* [Tuple Types](/posts/tuples/).
* [Discriminated Unions](/posts/discriminated-unions/).
* [Algebraic type sizes and domain modelling](/posts/type-size-and-design/).

F# for C# Developers:

* [Talk: F# for C# Developers](/csharp/).
* [Porting from C#](/series/porting-from-csharp.html).

## Other posts

* [Ten reasons not to use a statically typed functional programming language](/posts/ten-reasons-not-to-use-a-functional-programming-language/). A rant against something I don't get.
* [Why I won't be writing a monad tutorial](/posts/why-i-wont-be-writing-a-monad-tutorial/)
* [Is your programming language unreasonable?](/posts/is-your-language-unreasonable/) or, why predictability is important.
* [We don't need no stinking UML diagrams](/posts/no-uml-diagrams/) or, why in many cases, using UML for class diagrams is not necessary.
* [Introvert and extrovert programming languages](/posts/introvert-vs-extrovert/)
* [Swapping type-safety for high performance using compiler directives](/posts/typesafe-performance-with-compiler-directives/)

## Support This Site 

{% include donate.inc %}



