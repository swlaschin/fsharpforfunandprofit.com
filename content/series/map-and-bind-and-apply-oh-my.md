---
layout: series_index
title: "Understanding Map and Apply and Bind"
seriesIndexId: "Map and Bind and Apply, Oh my!"
seriesIndexOrder : 11
permalink: /series/map-and-bind-and-apply-oh-my.html
---

In this series of posts, I'll attempt to describe some of the core functions for dealing with generic data types (such as `Option` and `List`).
This is a follow-up post to [my talk on functional patterns](/fppatterns/).

Yes, I know that [I promised not to do this kind of thing](/posts/why-i-wont-be-writing-a-monad-tutorial/),
but for this post I thought I'd take a different approach from most people. Rather than talking about abstractions such as type classes,
I thought it might be useful to focus on the core functions themselves and how they are used in practice.

In other words, a sort of "man page" for `map`, `return`, `apply`, and `bind`.

So, there is a section for each function, describing their name (and common aliases), common operators, their type signature,
and then a detailed description of why they are needed and how they are used, along with some visuals (which I always find helpful).

