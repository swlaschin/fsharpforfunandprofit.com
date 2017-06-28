---
layout: post
title: "Conciseness"
description: "Why is conciseness important?"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 7
categories: [Conciseness]
---

After having seen some simple code, we will now move on to demonstrating the major themes (conciseness, convenience, correctness, concurrency and completeness), filtered through the concepts of types, functions and pattern matching.

With the next few posts, we'll examine the features of F# that aid conciseness and readability.

An important goal for most mainstream programming languages is a good balance of readability and conciseness. Too much conciseness can result in hard-to-understand or obfuscated code (APL anyone?), while too much verbosity can easily swamp the underlying meaning. Ideally, we want a high signal-to-noise ratio, where every word and character in the code contributes to the meaning of the code, and there is minimal boilerplate.

Why is conciseness important? Here are a few reasons:

* **A concise language tends to be more declarative**, saying *what* the code should do rather than *how* to do it. That is, declarative code is more focused on the high-level logic rather than the 
nuts and bolts of the implementation.
* **It is easier to reason about correctness** if there are fewer lines of code to reason about!
* And of course, **you can see more code on a screen** at a time. This might seem trivial, but the more you can see, the more you can grasp as well. 

As you have seen, compared with C#, F# is generally much more concise. This is due to features such as:

* **Type inference** and **low overhead type definitions**. One of the major reasons for F#'s conciseness and readability is its type system. F# makes it very easy to create new types as you need them. They don't cause visual clutter either in their definition or in use, and the type inference system means that you can use them freely without getting distracted by complex type syntax.
* **Using functions to extract boilerplate code**. The DRY principle ("don't repeat yourself") is a core principle of good design in functional languages as well as object-oriented languages. In F# it is extremely easy to extract repetitive code into common utility functions, which allows you to focus on the important stuff.  
* **Composing complex code from simple functions** and **creating mini-languages**. The functional approach makes it easy to create a set of basic operations and then combine these building blocks in various ways to build up more complex behaviors. In this way, even the most complex code is still very concise and readable.
* **Pattern matching**. We've seen pattern matching as a glorified switch statement, but in fact it is much more general, as it can compare expressions in a number of ways, matching on values, conditions, and types, and then assign or extract values, all at the same time.
