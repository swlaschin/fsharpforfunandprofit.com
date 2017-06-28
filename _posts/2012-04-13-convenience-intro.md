---
layout: post
title: "Convenience"
description: "Features that reduce programming drudgery and boilerplate code"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 13
categories: [Convenience]
---

In the next set of posts, we will explore a few more features of F# that I have grouped under the theme of "convenience".  These features do not necessarily result in more concise code, but they do remove much of the drudgery and boilerplate code that would be needed in C#.

* **Useful "out-of-the-box" behavior for types**. Most types that you create will immediately have some useful behavior, such as immutability and built-in equality -- functionality that has to be explicitly coded for in C#.
* **All functions are "interfaces"**, meaning that many of the roles that interfaces play in object-oriented design are implicit in the way that functions work.  And similarly, many object-oriented design patterns are unnecessary or trivial within a functional paradigm.
* **Partial application**. Complicated functions with many parameters can have some of the parameters fixed or "baked in" and yet leave other parameters open.
* **Active patterns**. Active patterns are a special kind of pattern where the pattern can be matched or detected dynamically, rather than statically.  They are great for simplifying frequently used parsing and grouping behaviors.
