---
layout: page
title: "Designing with Capabilities"
description:  Designing using capabilities and the principle of least authority
hasComments: 1
image: "/cap/cap427.jpg"
date: 2020-01-01
---

This page contains links to the slides and code from my talk "Designing with Capabilities".

Here's the blurb for the talk:


> We all want to produce modular and robust code that is easy to test and refactor,
> and we have design principles such as SOLID that help us do that.\
> \
> In this talk I'll look at a very different approach to design using "capabilities"
> and the principle of least authority. I'll show how using these design techniques
> throughout your core domain (and not just at your API boundary) also
> leads to well-designed and modular code.\
> \
> I'll demonstrate how to design and use a capability based approach,
> how capabilities can be quickly combined and restricted easily,
> and how capabilities are a natural fit with a REST API that uses HATEAOS.

This talk is based on my blog posts on this topic:

* [A functional approach to authorization](/posts/capability-based-security/)
* [Constraining capabilities based on identity and role](/posts/capability-based-security-2/)
* [Using types as access tokens](/posts/capability-based-security-3/)

Also related are the talk and posts on designing Enterprise Tic-Tac-Toe:

* [Enterprise Tic-Tac-Toe](/posts/enterprise-tic-tac-toe/)
* [Enterprise Tic-Tac-Toe Part 2, In which I throw away the previous design](/posts/enterprise-tic-tac-toe-2/)
* [Video and slides for my "Enterprise Tic-Tac-Toe" talk](/ettt/)

## Video

Video from NDC London, Jan 15, 2016 (Click image to view video)

[![Video from NDC London, Jan 15, 2016](cap427.jpg)](https://goo.gl/hmzGFn)

## Code

The code used in the demos is [available on github](https://github.com/swlaschin/DesigningWithCapabilities).

## F#-focused Slides

Slides from NDC London, Jan 15, 2016

{{< slideshare "RsUkUq6R22bbU" "designing-with-capabilities" "Designing with Capabilities" >}}

## DDD-focused Slides

These slides from DDD Europe, Feb 3, 2017 are more DDD focused

{{< slideshare "F3MVurYSbfuH0l" "designing-with-capabilities-dddeu-2017" "Designing with Capabilities (DDD-EU 2017)" >}}



