---
layout: page
title: "Site Contents"
nav: site-contents
description: "A directory of useful pages"
hasIcons: 1
hasNoCode: 1
---


## Getting started

* [Installing and using F#](/installing-and-using/) will get you started.
* [Why use F#?](/why-use-fsharp/) An interactive tour of F#.
* [Learning F#](/learning-fsharp/) has tips to help you learn more effectively. 
* [Troubleshooting F#](/troubleshooting-fsharp/) for when you have problems getting your code to compile.

and then you can try...

* [Twenty six low-risk ways to use F# at work](/posts/low-risk-ways-to-use-fsharp-at-work/). You can start right now -- no permission needed!

## Tutorials

The following series are tutorials on the key concepts of F#. 

* [Thinking functionally](/series/thinking-functionally.html) starts from basics and explains how and why functions work the way they do.
* [Expressions and syntax](/series/expressions-and-syntax.html) covers the common expressions such as pattern matching, and has a post on indentation.
* [Understanding F# types](/series/understanding-fsharp-types.html) explains how to define and use the various types, including tuples, records, unions, and options.
* [Designing with types](/series/designing-with-types.html) explains how to use types as part of the design process, making illegal states unrepresentable.
* [Choosing between collection functions](/posts/list-module-functions/). If you are coming to F# from C#, the large number of list functions can be overwhelming, so I have written this post to help guide you to the one you want.
* [Property-based testing](/pbt/): the lazy programmer's guide to writing 1000's of tests.
* [Understanding computation expressions](/series/computation-expressions.html) demystifies them and shows how you can create your own.

## Functional patterns

These posts explain some core patterns in functional programming -- concepts such as "map", "bind", monads and more.

* [Introduction to functional design patterns](/fppatterns/).
* [Railway Oriented Programming](/rop/): A functional approach to error handling
* [State Monad](/monadster/): An introduction to handling state using the tale of Dr Frankenfunctor and the Monadster.
* [Reader Monad](/posts/elevated-world-6/): Reinventing the Reader monad.
* [Map, bind, apply, lift, sequence and traverse](/series/map-and-bind-and-apply-oh-my.html): A series describing some of the core functions for dealing with generic data types. 
* [Monoids without tears](/posts/monoids-without-tears/): A mostly mathless discussion of a common functional pattern.
* [Fold and recursive types](/series/recursive-types-and-folds.html): A look at recursive types, catamorphisms, tail recursion, the difference between left and right folds, and more.
* [Understanding Parser Combinators](/posts/understanding-parser-combinators/): Creating a parser combinator library from scratch.
* [Thirteen ways of looking at a turtle](/posts/13-ways-of-looking-at-a-turtle/): demonstrates many different techniques for implementing a turtle graphics API, including state monads, agents, interpreters, and more!

## Worked examples

These posts provide detailed worked examples with lots of code!

* [Designing for correctness](/posts/designing-for-correctness/): How to make illegal states unrepresentable (a shopping cart example).
* [Stack based calculator](/posts/stack-based-calculator/): Using a simple stack to demonstrate the power of combinators.
* [Parsing commmand lines](/posts/pattern-matching-command-line/): Using pattern matching in conjunction with custom types.
* [Roman numerals](/posts/roman-numerals/): Another pattern matching example.
* [Calculator Walkthrough](/posts/calculator-design/): The type-first approach to designing a Calculator.
* [Enterprise Tic-Tac-Toe](/posts/enterprise-tic-tac-toe/): A walkthrough of the design decisions in a purely functional implementation
* [Writing a JSON Parser](/posts/understanding-parser-combinators-4/).

## Specific topics in F# ##

General:

* [Four key concepts](/posts/key-concepts/) that differentiate F# from a standard imperative language.
* [Understanding F# indentation](/posts/fsharp-syntax/).
* [The downsides of using methods](/posts/type-extensions/#downsides-of-methods).

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
* [Discriminated Unions](/posts/the-option-type/).
* [Algebraic type sizes and domain modelling](/posts/type-size-and-design/).


## Controversial posts

* [Is your programming language unreasonable?](/posts/is-your-language-unreasonable/) or, why predictability is important.
* [Commentary on 'Roman Numerals Kata with Commentary'](/posts/roman-numeral-kata/). My approach to the Roman Numerals Kata.
* [Ten reasons not to use a statically typed functional programming language](/posts/ten-reasons-not-to-use-a-functional-programming-language/). A rant against something I don't get.
* [We don't need no stinking UML diagrams](/posts/no-uml-diagrams/) or, why in many cases, using UML for class diagrams is not necessary.
