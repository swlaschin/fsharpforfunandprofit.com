---
layout: post
title: "Object-oriented programming in F#: Introduction"
description: ""
nav: fsharp-types
seriesId: "Object-oriented programming in F#"
seriesOrder: 1
categories: [Object-oriented]
---

In this series, we'll look at how F# supports object-oriented classes and methods.  

## Should you use object-oriented features at all?

As has been stressed many times before, F# is fundamentally a functional language at heart, yet the OO features have been nicely integrated and do not have a "tacked-on" feeling. As a result, it is quite viable to use F# just as an OO language, as an alternative to C#, say.

Whether to use the OO style or the functional style is, of course, up to you.  Here are some arguments for and against.

Reasons in favor of using OO features:

* If you just want to do a direct port from C# without refactoring. (For more on this, there is a [entire series on how to port from C# to F#](/series/porting-from-csharp.html).)
* If you want to use F# primarily as an OO language, as an alternative to C#.
* If you need to integrate with other .NET languages

Reasons against using OO features:

* If you are a beginner coming from an imperative language, classes can be a crutch that hinder your understanding of functional programming.
* Classes do not have the convenient "out of the box" features that the "pure" F# data types have, such as built-in equality and comparison, pretty printing, etc.
* Classes and methods do not play well with the type inference system and higher order functions (see [discussion here](/posts/type-extensions/#downsides-of-methods)), so using them heavily means that you are making it harder to benefit from the most powerful parts of F#.

In most cases, the best approach is a hybrid one, primarily using pure F# types and functions to benefit from type inference, but occasionally using interfaces and classes when polymorphism is needed.

## Understanding the object-oriented features of F# ##

If you do decide to use the object-oriented features of F#, the following series of posts should cover everything you need to know to be productive with classes and methods in F#.

First up, how to create classes!