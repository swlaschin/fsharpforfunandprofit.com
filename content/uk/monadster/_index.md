---
layout: page
title: "Dr Frankenfunctor and the Monadster"
hasComments: 1
image: "/monadster/monadster427.jpg"
date: 2020-01-01
---

This page contains links to the slides and video from my talk "Dr Frankenfunctor and the Monadster".

*WARNING: This talk contains:*

* *gruesome topics*
* *strained analogies*
* *discussion of monads*

Here's the blurb for the talk:

> You've got a pile of assorted functions lying around. Each one is useful and reliable, but they just don't fit together properly.   How can you assemble them into a complete system that can stand on its own two feet and terrorize the local villagers?\
  \   In this session, I'll show how functional programming can transform all sorts of existing code into shapes that are plug-compatible and which can be bolted together effortlessly.\
  \ SAFETY NOTE: The techniques demonstrated are perfectly harmless and can even be used at your workplace -- no lightning bolts required.

## Video

Video from *NDC London 2016*, Jan 14, 2016 (Click image to view video)

[![Video from NDC London 2016, Jan 14, 2016](monadster427.jpg)](https://goo.gl/8TwY8C)

## Slides

Slides from *Leetspeak 2015*, Stockholm, Oct 10, 2015

{{< slideshare "4oCVwCraxCrAfx" "dr-frankenfunctor-and-the-monadster" "Dr Frankenfunctor and the Monadster" >}}

## Related posts

This talk inspired a series of blog posts on this same topic:

* [Dr Frankenfunctor and the Monadster](/posts/monadster/)
* [Completing the body of the Monadster](/posts/monadster-2/)
* [Refactoring the Monadster](/posts/monadster-3/)

Also related are the posts on map and bind, and on two-track error handling:

* [Map and Bind and Apply, Oh my!](/series/map-and-bind-and-apply-oh-my.html)
* [A functional approach to error handling ("Railway Oriented Programming")](/rop/)

## Background

In the summer of 2015, the team organizing [Leetspeak](http://leetspeak.se/) asked me to present a talk on the theme of "It's Alive".

I was feeling uninspired, so of course I grasped at the most obvious and unimaginative metaphor possible -- the story of Frankenstein. And the idea of building a body from various ill-fitting parts then led naturally to the talk being about function composition, and transforming ill-fitting functions to a consistent shape using `map` and `bind`.

Generally I don't like to make my talks *too* complicated, but the organizers explicitly said that they wanted hard and complex stuff! Ok then!

And they seemed to like it:

{{< rawtweet >}}

<blockquote class="twitter-tweet" data-partner="tweetdeck"><p lang="en" dir="ltr">.<a href="https://twitter.com/ScottWlaschin">@ScottWlaschin</a> talk is exactly what we aimed for <a href="https://twitter.com/hashtag/leetspeak?src=hash">#leetspeak</a>. Leaves you mindblown and feeling that there&#39;s stuff you still need to learn.:D</p>&mdash; Michal Lusiak (@mlusiak) <a href="https://twitter.com/mlusiak/status/652777865952526337">October 10, 2015</a></blockquote>
<script async src="//platform.twitter.com/widgets.js" charset="utf-8"></script>

{{< /rawtweet >}}

So, apologies if you find it too intense. I plead diminished responsibility!


