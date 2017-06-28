---
layout: post
title: "Completeness"
description: "F# is part of the whole .NET ecosystem"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 27
categories: [Completeness]
---

In this final set of posts, we will look at some other aspects of F# under the theme of "completeness".  

Programming languages coming from the academic world tend to focus on elegance and purity over real-world usefulness, while more mainstream business languages such as C# and Java are valued precisely because they are pragmatic; they can work in a wide array of situations and have extensive tools and libraries to meet almost every need. In other words, to be useful in the enterprise, a language needs to be *complete*, not just well-designed.

F# is unusual in that it successfully bridges both worlds. Although all the examples so far have focused on F# as an elegant functional language, it does support an object-oriented paradigm as well, and can integrate easily with other .NET languages and tools. As a result, F# is not a isolated island, but benefits from being part of the whole .NET ecosystem.

The other aspects that make F# "complete" are being an official .NET language (with all the support and documentation that that entails) and being designed to work in Visual Studio (which provides a nice editor with IntelliSense support, a debugger, and so on).  These benefits should be obvious and won't be discussed here.

So, in this last section, we'll focus on two particular areas:

* **Seamless interoperation with .NET libraries**. Obviously, there can be a mismatch between the functional approach of F# and the imperative approach that is designed into the base libraries. We'll look at some of the features of F# that make this integration easier.
* **Full support for classes and other C# style code**. F# is designed as a hybrid functional/OO language, so it can do almost everything that C# can do as well. We'll have a quick tour of the syntax for these other features.
