---
layout: post
title: "Correctness"
description: "How to write 'compile time unit tests'"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 18
categories: [Correctness]
---

As a programmer, you are constantly judging the code that you and others write. In an ideal world, you should be able to look at a piece of code and easily understand exactly what it does; and of course, being concise, clear and readable is a major factor in this. 

But more importantly, you have to be able to convince yourself that the code *does what it is supposed to do*. As you program, you are constantly reasoning about code correctness, and the little compiler in your brain is checking the code for errors and possible mistakes. 

So how can a programming language help you with this?  

A modern imperative language like C# provides many ways that you are already familiar with: type checking, scoping and naming rules, access modifiers and so on. And, in recent versions, static code analysis and code contracts.  

All these techniques mean that the compiler can take on a lot of the burden of checking for correctness. If you make a mistake, the compiler will warn you.

But F# has some additional features that can have a huge impact on ensuring correctness. The next few posts will be devoted to four of them:

* **Immutability**, which enables code to behave much more predictably.
* **Exhaustive pattern matching**, which traps many common errors at compile time.
* **A strict type system**, which is your friend, not your enemy. You can use the static type checking almost as an instant "compile time unit test".
* **An expressive type system** that can help you "make illegal states unrepresentable"* . We'll see how to design a real-world example that demonstrates this.

<sub>* Thanks to Yaron Minsky at Jane Street for this phrase.</sub>
