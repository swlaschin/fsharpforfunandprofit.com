---
layout: series_index
title: "The 'functional approach to authorization' series"
seriesIndexId: "A functional approach to authorization"
seriesIndexOrder : 16
permalink: /series/a-functional-approach-to-authorization.html
---

In this series of posts, I'll look at how you might handle the common security challenge of authorization.
That is, how can you ensure that clients of your code can only do what you want them to do?

This series will sketch out two different approaches, first using an approach called *capability based security*, and second using statically checked types to emulate access tokens.

Interestingly, both approaches tend to produce a cleaner, more modular design as a side effect, which is why I like them! 

