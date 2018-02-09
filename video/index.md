---
layout: page
title: "Videos"
description: 
nav: video
hasComments: 1
image: "/video/fppatterns320.jpg"
---

Like to watch videos? You're in the right place!

This page lists all my talks that are available on video. Each talk is about one hour long.
Slides and more information are also available for each talk -- see the link at the end of each description.

For more F# videos, visit the [Community for F#](http://c4fsharp.net/#online-presentations) site.

------


<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Functional Design Patterns

In object-oriented development, we are all familiar with design patterns
such as the Strategy pattern and Decorator pattern, and design principles such as SOLID. 

The functional programming community has design patterns and principles as well. 

This talk provides an overview of some of these, and presents some 
demonstrations of FP design in practice. **Over [100,000 views on YouTube](https://www.youtube.com/watch?v=E8I19uA-wGY)!**

<a href="../fppatterns/">Slides and more information</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/aptvwN"><img alt="Video from NDC London 2014" src="fppatterns320.jpg"></a>
</div>
    
</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Domain Modeling Made Functional 

Statically typed functional programming languages like F# encourage a very different way of thinking about types.
The type system is your friend, not an annoyance, and can be used in many ways that might not be familiar to OO programmers. 

Types can be used to represent the domain in a fine-grained, self documenting way. And in many cases,
types can even be used to encode business rules so that you literally cannot create incorrect code.
You can then use the static type checking almost as an instant unit test -- making sure that your code is correct at compile time. 

In this talk, we'll look at some of the ways you can use types as part of a domain driven design process,
with some simple real world examples in F#. No jargon, no maths, and no prior F# experience necessary. 

<a href="../ddd/">Slides and more information</a>


</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/kxVAWt"><img alt="Video from NDC Oslo, June 2017" src="ddd320.jpg"></a>
</div>
    
</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## A functional approach to error handling<br>*(Railway Oriented Programming)*

Many examples in functional programming assume that you are always on the "happy path". 
But to create a robust real world application you must deal with validation, logging, 
network and service errors, and other annoyances. 

So, how do you handle all this in a clean functional way? 

This talk will provide a brief introduction to this topic, 
using a fun and easy-to-understand railway analogy.

<a href="../rop/">Slides and more information</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/Lv5ZAo"><img alt="Video from NDC London 2014" src="rop320.jpg"></a>
</div>
    
</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## F# for C# Programmers

Curious about F# and want to understand how is it different from C#? 

In this talk, we'll look at the basics of coding in F#, and how functional programming differs from object-oriented programming.
Along the way, there will be many examples showing the same code written in C# and F# so that you can see for yourself how the two languages differ in style and approach.

<a href="../csharp/">Slides and more information</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/LdQNGD"><img alt="Video from NDC Oslo, June 14, 2017" src="csharp320.jpg"></a>
</div>

</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## The Power of Composition

Composition is a fundamental principle of functional programming, but how is it different from an object-oriented approach, and how do you use it in practice? 

In this talk for beginners, we'll start by going over the basic concepts of functional programming, and then look at some different ways that composition can be used to build large things from small things. 

After that, we'll see how composition is used in practice, beginning with a simple FizzBuzz example, and ending with a complete (object-free!) web application.

<a href="../composition/">Slides and more information</a>

</div>

<div class="col-sm-4" style="float:left;">
COMING SOON!
<!--
<a href="https://goo.gl/LdQNGD"><img alt="Video from NDC London, Jan 17, 2018" src="composition320.jpg"></a>
-->
</div>

</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Understanding parser combinators

Traditionally, writing parsers has been hard, involving arcane tools like Lex and Yacc.
An alternative approach is to write a parser in your favourite programming language, 
using a "parser combinator" library and concepts no more complicated than regular expressions. 

In this talk, we'll do a deep dive into parser combinators.
We'll build a parser combinator library from scratch in F# using functional programming techniques, 
and then use it to implement a full featured JSON parser.

<a href="../parser/">Slides and more information</a>

This talk is based on my blog posts on this topic:

* <a href="/series/understanding-parser-combinators.html">Understanding parser combinators</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/Cxa7NR"><img alt="Video from NDC Oslo, Jun 9, 2016" src="parser320.jpg"></a>
</div>
    
</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Thirteen ways of looking at a turtle


In this fast-paced talk, I'll start with the well known API for turtle graphics, and then stretch 
it to the breaking point by creating 13 different implementations, each demonstrating a different functional programming technique. 

Along the way, we'll use partial application, functional dependency injection, validation 
with Success/Failure results, the Either monad, the State monad, agents with message queues, event sourcing,
stream processing, capability-based design, and the interpreter pattern (aka free monad). Phew! 

<a href="../turtle/">Slides and more information</a>

This talk is based on my blog posts on this topic:

* <a href="/posts/13-ways-of-looking-at-a-turtle/">Thirteen ways of looking at a turtle</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/2BN161"><img alt="Video from NDC London, Jan 18, 2017" src="turtle320.jpg"></a>
</div>

</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Property-based testing

We are all familiar with example-based testing, as typified by TDD and BDD. 
Property-based testing takes a very different approach, where a single test is run hundreds of times with randomly generated inputs. 

Property-based testing is a great way to find edge cases, and also helps you to 
understand and document the behaviour of your code under all conditions. 

This talk will introduce property-based testing and show how it works, 
and why you should consider adding it to your arsenal of testing tools.  

<a href="../pbt/">Slides and more information</a>

This talk is based on my blog posts on this topic:

* <a href="/posts/property-based-testing/">An introduction to property-based testing</a>
* <a href="/posts/property-based-testing-2/">Choosing properties for property-based testing</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/p2kfWH"><img alt="Video from Functional Programming eXchange, Jun 11, 2015" src="pbt320.jpg"></a>
</div>
    
</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Dr Frankenfunctor and the Monadster

You've got a pile of assorted functions lying around. Each one is useful and reliable, but they just don't fit together properly. 
How can you assemble them into a complete system that can stand on its own two feet and terrorize the local villagers?

In this session, I'll show how functional programming can transform all sorts of existing code into shapes that are plug-compatible and which can be bolted together effortlessly.

<a href="../monadster/">Slides and more information</a>

This talk is based on my blog post on this topic:

* <a href="/posts/monadster/">Dr Frankenfunctor and the Monadster</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/8TwY8C"><img alt="Video from NDC London 2016, Jan 14, 2016" src="monadster320.jpg"></a>
</div>
    
</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Enterprise Tic-Tac-Toe

Follow along as I ridiculously over-engineer a simple game to demonstrate how 
functional programming can be used to create a real-world "enterprise-ready" application.

Topics covered include: encoding business rules into types, data hiding with parametric polymorphism, 
using functions for capability-based security, exposing a REST API with 
HATEAOS, the functional approach to logging, actors, scalability and more. 

<a href="../ettt/">Slides and more information</a>

This talk is based on my blog posts on this topic:

* <a href="/posts/enterprise-tic-tac-toe/">Enterprise Tic-Tac-Toe</a>
* <a href="/posts/enterprise-tic-tac-toe-2/">Enterprise Tic-Tac-Toe Part 2, In which I throw away the previous design</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/YE4pW2"><img alt="Video from NDC Oslo, Jun 17, 2015" src="ettt320.jpg"></a>
</div>

</div>

------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Designing with Capabilities

We all want to produce modular and robust code that is easy to test and refactor, 
and we have design principles such as SOLID that help us do that.

In this talk I'll look at a very different approach to design using "capabilities" 
and the principle of least authority. I'll show how using these design techniques 
throughout your core domain (and not just at your API boundary) also 
leads to well-designed and modular code.

I'll demonstrate how to design and use a capability based approach, 
how capabilities can be quickly combined and restricted easily, 
and how capabilities are a natural fit with a REST API that uses HATEAOS.

<a href="../cap/">Slides and more information</a>

This talk is based on my blog posts on this topic:

* <a href="/posts/capability-based-security/">A functional approach to authorization</a>
* <a href="/posts/capability-based-security-2/">Constraining capabilities based on identity and role</a>
* <a href="/posts/capability-based-security-3/">Using types as access tokens</a>

</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/hmzGFn"><img alt="Video from NDC London, Jan 15, 2016" src="cap320.jpg"></a>
</div>
    
</div>


------

<div class="row video">  

<div class="col-sm-8" style="float:right;" markdown="1">

## Code Review/Refactor Live

In this live session for [Community for F#](http://c4fsharp.net/), I refactor some ugly code into something a bit cleaner and functional, to give viewers an insight into basic F# coding practices, and also show how to avoid some common code smells.

[GitHub repository with code samples is here](https://github.com/swlaschin/RefactoringFSharp).


</div>

<div class="col-sm-4" style="float:left;">
<a href="https://goo.gl/6L5Sbd"><img alt="Code Review/Refactor Live" src="refactoring320.jpg"></a>
</div>
    
</div>



