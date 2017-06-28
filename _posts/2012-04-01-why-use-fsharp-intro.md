---
layout: post
title: "Introduction to the 'Why use F#' series"
description: "An overview of the benefits of F#"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 1
---

This series of posts will give you a guided tour through the main features of F# and then show you ways that F# can help you in your day-to-day development.  

### Key benefits of F# compared with C# ###

If you are already familiar with C# or Java, you might be wondering why it would be worth learning yet another language. F# has some major benefits which I have grouped under the following themes:

* **Conciseness**. F# is not cluttered up with coding "noise" such as curly brackets, semicolons and so on. You almost never have to specify the type of an object, thanks to a powerful type inference system. And it generally takes less lines of code to solve the same problem. 
* **Convenience**. Many common programming tasks are much simpler in F#.  This includes things like creating and using complex type definitions, doing list processing, comparison and equality, state machines, and much more.  And because functions are first class objects, it is very easy to create powerful and reusable code by creating functions that have other functions as parameters, or that combine existing functions to create new functionality. 
* **Correctness**. F# has a very powerful type system which prevents many common errors such as null reference exceptions. And in addition, you can often encode business logic using the type system itself, so that it is actually impossible to write incorrect code, because it is caught at compile time as a type error.
* **Concurrency**. F# has a number of built-in tools and libraries to help with programming systems when more than one thing at a time is happening. Asynchronous programming is directly supported, as is parallelism. F# also has a message queuing system, and excellent support for event handling and reactive programming. And because data structures are immutable by default, sharing state and avoiding locks is much easier.
* **Completeness**.  Although F# is a functional language at heart, it does support other styles which are not 100% pure, which makes it much easier to interact with the non-pure world of web sites, databases, other applications, and so on. In particular, F# is designed as a hybrid functional/OO language, so it can do almost everything that C# can do as well.  Of course, F# integrates seamlessly with the .NET ecosystem, which gives you access to all the third party .NET libraries and tools. Finally, it is part of Visual Studio, which means you get a good editor with IntelliSense support, a debugger, and many plug-ins for unit tests, source control, and other development tasks. 

In the rest of this series of posts, I will try to demonstrate each of these F# benefits, using standalone snippets of F# code (and often with C# code for comparison).  I'll briefly cover all the major features of F#, including pattern matching, function composition, and concurrent programming.  By the time you have finished this series, I hope that you will have been impressed with the power and elegance of F#, and you will be encouraged to use it for your next project!

### How to read and use the example code ###

All the code snippets in these posts have been designed to be run interactively. I strongly recommend that you evaluate the snippets as you read each post. The source for any large code files will be linked to from the post.

This series is not a tutorial, so I will not go too much into *why* the code works.  Don't worry if you cannot understand some of the details; the goal of the series is just to introduce you to F# and whet your appetitite for learning it more deeply.

If you have experience in languages such as C# and Java, you have probably found that you can get a pretty good understanding of source code written in other similar languages, even if you aren't familiar with the keywords or the libraries. You might ask "how do I assign a variable?" or "how do I do a loop?", and with these answers be able to do some basic programming quite quickly.

This approach will not work for F#, because in its pure form there are no variables, no loops, and no objects.  Don't be frustrated - it will eventually make sense! If you want to learn F# in more depth, there are some helpful tips on the ["learning F#"](/learning-fsharp/) page.


