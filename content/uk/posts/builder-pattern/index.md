---
layout: post
title: "The Builder Pattern in F#"
description: ""
date: 2021-07-23
draft: false
---

In this post, we'll look at the "builder pattern", and discuss various ways of implementing it in F#.

## What is the builder pattern?

The [builder pattern](https://wiki.c2.com/?BuilderPattern) is used when you need to construct a valid object using a series of individual steps, rather than using a single constructor. This need arises when:

* you want to break the construction into steps for clarity. This can be useful when [constructing complex objects for testing](http://www.natpryce.com/articles/000714.html). It can also be used for performance, such the [StringBuilder class in C#](https://docs.microsoft.com/en-us/dotnet/api/system.text.stringbuilder#StringAndSB)  and Java.
* you want to validate each construction step separately.
* you don't have all the data available, e.g. while a user is filling out a multi-part form, or when partial data arrives as events or messages. (This last one is sometimes also called an [Aggregator](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Aggregator.html)).


{{<alertinfo>}}
In F# there is another concept called "builder" -- a computation expression builder. This is not related to the classic "builder pattern" at all. For more, see the [computation expressions series](../computation-expressions-intro/).
{{</alertinfo>}}

----

COMING SOON

