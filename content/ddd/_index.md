---
layout: page
title: "Domain Driven Design"
description: Slides and video from my talk "Domain Modeling Made Functional with the F# Type System"
hasComments: 1
image: "/ddd/ddd427.jpg"
---

This page contains links to the slides, video and code from my talk "Domain Modeling Made Functional".

Here's the blurb for the talk:

> Statically typed functional programming languages like F# encourage a very different way of thinking about types.
> The type system is your friend, not an annoyance, and can be used in many ways that might not be familiar to OO programmers.\
> \
> Types can be used to represent the domain in a fine-grained, self documenting way. And in many cases,
> types can even be used to encode business rules so that you literally cannot create incorrect code.
> You can then use the static type checking almost as an instant unit test -- making sure that your code is correct at compile time.\
> \
> In this talk, we'll look at some of the ways you can use types as part of a domain driven design process,
> with some simple real world examples in F#. No jargon, no maths, and no prior F# experience necessary.

I am also planning to upload some posts on these topics soon. Meanwhile, please see the [Designing with Types](/series/designing-with-types.html) series, which covers similar ground.

You should also read ["why type-first development matters"](http://tomasp.net/blog/type-first-development.aspx/) by Tomas Petricek
and a great [series](http://gorodinski.com/blog/2013/02/17/domain-driven-design-with-fsharp-and-eventstore/) of [articles](http://gorodinski.com/blog/2013/04/23/domain-driven-design-with-fsharp-validation/) by Lev Gorodinski.

##  Video

(Click image to view video)

[![Video from NDC Oslo, June 2017](ddd427.jpg)](https://goo.gl/kxVAWt)

##  Slides

{{< slideshare "A4ay4HQqJgu0Q" "domain-driven-design-with-the-f-type-system-functional-londoners-2014" "Domain Driven Design with the F# type System -- F#unctional Londoners 2014" >}}

The slides are also available from the links below:

* NDC London 2013 version ([Slideshare](http://www.slideshare.net/ScottWlaschin/ddd-with-fsharptypesystemlondonndc2013)), ([Github](https://github.com/swlaschin/NDC_London_2013))
* F#unctional Londoners 2014 version ([Speakerdeck](https://speakerdeck.com/swlaschin/domain-driven-design-with-the-f-number-type-system-f-number-unctional-londoners-2014)) with
added sections on why OO, not FP is scary, and designing with states and transitions.

## Book

I have a book all about this topic -- you can find more details on the [books page](/books/).

[![Domain Modeling Made Functional](/books/domain-modeling-made-functional-320.jpg)](/books/)

##  Getting the code

If you want to follow along with the code, then:

* If you have F# installed locally, download [this file](/ddd/ddd.fsx).
* If you don't have F# installed locally, you can run the code in your web browser at: [tryfsharp.org/create/scottw/ddd.fsx](http://www.tryfsharp.org/create/scottw/ddd.fsx)
