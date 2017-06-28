---
layout: post
title: "Thinking Functionally: Introduction"
description: "A look at the basics of functional programming"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 1
---

Now that you have seen some of the power of F# in the ["why use F#"](/series/why-use-fsharp.html) series, we're going to step back and look at the fundamentals of functional  programming -- what does it really mean to "program functionally", and how this approach is different from object oriented or imperative programming.

### Changing the way you think ###

It is important to understand that functional programming is not just a stylistic difference; it is a completely different way of thinking about programming, in the way that truly object-oriented programming (in Smalltalk say) is also a different way of thinking from a traditional imperative language such as C. 

F# does allow non-functional styles, and it is tempting to retain the habits you already are familiar with. You could just use F# in a non-functional way without really changing your mindset, and not realize what you are missing. To get the most out of F#, and to be fluent and comfortable with functional programming in general, it is critical that you think functionally, not imperatively.
And that is the goal of this series: to help you understand functional programming in a deep way, and help to change the way you think.

This will be a quite abstract series, although I will use lots of short code examples to demonstrate the points. We will cover the following points:

* **Mathematical functions**. The first post introduces the mathematical ideas behind functional languages, and the benefits that come from this approach.
* **Functions and values**. The next post introduces functions and values, showing how "values" are different from variables, and why there are similarities between function and simple values. 
* **Types**.  Then we move on to the basic types that work with functions: primitive types such as string and int; the unit type, function types, and generic types.
* **Functions with multiple parameters**. Next, I explain the concepts of "currying" and "partial application". This is where your brain can start to hurt, if you are coming from an imperative background!
* **Defining functions**. Then some posts devoted to the many different ways to define and combine functions.
* **Function signatures**. Then a important post on the critical topic of function signatures: what they mean and how to use them as an aid to understanding.
* **Organizing functions**. Once you know how to create functions, how can you organize them to make them available to the rest of your code?
