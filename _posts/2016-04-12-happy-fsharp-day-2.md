---
layout: post
title: "Happy F# Day!"
description: "Growing and getting better each year"
categories: []
---

Last year, I wrote a post called ["Happy F# Day"](/posts/happy-fsharp-day/), explaining why I thought F# needed a "birthday" that we could celebrate as a community.

Here we are one year later! I'm going to promote that date (April 12th) again, and use it an excuse to review what has happened in the last year in the world of F#.

And a *lot* has happened in the last year.  Here's my personal take. Apologies in advance to anyone or any project I forget to mention -- there is so much going on!

## The mainstreaming of F# ##

First, my (entirely anecdotal) impression is that F# is now being perceived as more mainstream, and that interest in F# from C# developers (and others too)
has increased slowly but steadily throughout the year as more and more developers have become exposed to it.

> There is an interesting phenomenon happening at Xamarin. 
> Whenever one of our engineers starts working with F#, they tend to embrace it and stay there.
> -- *[Miguel de Icaza](https://www.reddit.com/r/programmerchat/comments/4dxpcp/i_am_miguel_de_icaza_i_started_xamarin_mono_gnome/d1vasd6?context=3)*

Just to take two recent examples, [Miguel de Icaza promotes F# on reddit](https://www.reddit.com/r/programmerchat/comments/4dxpcp/i_am_miguel_de_icaza_i_started_xamarin_mono_gnome/d1v954m)
and gets upvoted. And the viewing figures for F# videos at [fsharpConf](https://blogs.msdn.microsoft.com/mvpawardprogram/2016/04/11/its-not-too-late-to-catch-the-fsharpconf-action/)
are in the 20,000 to 40,000 range -- pretty impressive I think.

F# seems to be represented at almost every .NET conference now, and last year saw the first specialist conferences: [F# Exchange](http://trelford.com/blog/post/FSharpEx.aspx) in London
(with the [second one happening in a few days](https://skillsmatter.com/conferences/7145-f-exchange-2016)), [F# Gotham](http://www.fsharpgotham.com/) in New York City,
and the global, online, [fsharpConf](http://fsharpconf.com/).

Stack Overflow did a developer survey this time last year. According to those developers [F# is the best-paying .NET language in which to program and also the most loved](https://twitter.com/lobrien/status/615216594969452544).
Sadly, it's [only 3rd in lovability this year](https://stackoverflow.com/research/developer-survey-2016#technology-most-loved-dreaded-and-wanted) but still one of the [best paid!](https://stackoverflow.com/research/developer-survey-2016#technology-top-paying-tech) :)

The launch of companies like Jet, with its [decision to use F#](http://techgroup.jet.com/blog/2015/03-22-on-how-jet-chose/),
and Tachyus (["we are using 100% F# to build the back end of our platform"](https://news.ycombinator.com/item?id=7543093)) also helped to show that F# is no longer a risky choice.

## F# tooling

Another reason for the wider acceptance of F# is that the F# tooling is becoming very solid. For example:

* The (now indispensable) Paket had its [v1 release just under a year ago](https://fsprojects.github.io/Paket/release-notes.html). 
* [Ionide](http://ionide.io/) rapidly become the solution for those who prefer Atom or VS Code as their editor.
  And [emacs](https://melpa.org/#/fsharp-mode) and [vim](https://github.com/fsharp/vim-fsharp) got some love too!
* Web development in F# got a lot of attention, with [Suave](https://suave.io) becoming very popular (even promoted by [Scott Hanselman](http://www.hanselman.com/blog/RunningSuaveioAndFWithFAKEInAzureWebAppsWithGitAndTheDeployButton.aspx)),
  [Freya 1.0](http://docs.freya.io/en/latest/) being released, and a major release of [Websharper](http://websharper.com/blog-entry/4323/websharper-3-0-released).
  The new [Fable](https://github.com/fsprojects/Fable) FS-to-JS project is getting a lot of traction too.
* Other tools and libraries in the F# ecosystem continued to improve, including
  [Visual F# PowerTools](https://fsprojects.github.io/VisualFSharpPowerTools/index.html),
  data science with [FsLab](http://fslab.org/),
  property based testing with [FsCheck](https://fscheck.github.io/FsCheck/),
  documentation using [FSharp.Formatting](https://tpetricek.github.io/FSharp.Formatting/),
  presentations with [FsReveal](https://fsprojects.github.io/FsReveal/),
  builds with [FAKE](https://github.com/fsharp/Fake),
  distributed programming with [mBrace](http://mbrace.io/),
  concurrent programming with [Hopac](https://hopac.github.io/Hopac/Hopac.html),
  logging ([Logary](https://logary.github.io/)),
  profiling ([PrivateEye](http://www.privateeye.io/)),
  cloud IDE ([Cloudsharper](http://cloudsharper.com/)),
  and many, many more. Apologies if I missed your favorite!
* And of course, [F# 4.0 was released](https://blogs.msdn.microsoft.com/dotnet/2015/07/20/announcing-the-rtm-of-visual-f-4-0/), which also marked the shift to a fully open and collaborative development process.
 
## F# and Microsoft and Xamarin
 
Of course in the .NET world, the big news has been Microsoft's embrace of open source and cross-platform code.
Soon, we hope, F# will be running on all platforms using CoreCLR, fully supported by Microsoft!

Microsoft gave F# some love at this years [Build](https://channel9.msdn.com/Events/Build/2016/T661) and even [sat David (from the F# team) next to Mads (from C#)!](https://channel9.msdn.com/Events/Build/2016/C920)

The Xamarin team have always been great fans of F#, [supporting it as a first class language](https://developer.xamarin.com/guides/cross-platform/fsharp/).
And now that [Xamarin Studio has a free Community Edition](https://blog.xamarin.com/xamarin-for-all/), there are no barriers to F# developers who want to build mobile apps.
 
## F# education

On the educational side of things, the [F# Software Foundation became a proper non-profit at the end of 2015](http://foundation.fsharp.org/fssf_granted_501_c_3_nonprofit_status), and
with all that paperwork out of the way (thank you Reed!), could start doing things. The FSSF started a [mentorship program](http://fsharp.org/mentorship/) in March, and just recently launched a [speakers program](http://foundation.fsharp.org/speakers_program_launch).

Also [fsharp.tv](https://fsharp.tv/) got funded via Kickstarter, and Pluralsight doubled the number of [F# courses](https://www.pluralsight.com/search?q=f%23&categories=course) it offers
-- more evidence for rising interest in F#.

I'll also add a shameless plug for a very useful F# book that came out this year: ["Machine Learning Projects for .NET Developers"](https://www.apress.com/9781430267676) by Mathias Brandewinder.
 
## The F# community
 
As always, the F# community was amazing. The number of people blogging and tweeting about F# seems to be growing all the time.

<blockquote class="twitter-tweet" data-lang="en"><p lang="en" dir="ltr">Testing the just-phrased hypothesis that any tweet with an <a href="https://twitter.com/hashtag/fsharp?src=hash">#fsharp</a> hashtag gets a like or a retweet.</p>&mdash; Dima (@UniqueDima) <a href="https://twitter.com/UniqueDima/status/692908823468732416">January 29, 2016</a></blockquote>
<script async src="//platform.twitter.com/widgets.js" charset="utf-8"></script>

This really hit home for me when I saw the amazing number and quality of the posts for the
[F# Advent Calendar 2015 in English](https://sergeytihon.wordpress.com/2015/10/25/f-advent-calendar-in-english-2015/) and [Japanese](http://connpass.com/event/22056/).

[Community for F#](http://c4fsharp.net/) continues to do sterling work -- it seems like they are hosting [an F# video](https://www.youtube.com/channel/UCCQPh0mSMaVpRcKUeWPotSA/feed) every few days!

And of course, where would we be without the essential [F# weekly](https://sergeytihon.wordpress.com/category/f-weekly/). 

My heartfelt thanks to all the people who have created the blogs, videos, tools and code mentioned above, and the many hundreds of other people who have made the F# community so great.

And of course, a special thanks to Don Syme, who this year was awarded a very well deserved [Silver Medal from Royal Academy of Engineering](https://blogs.technet.microsoft.com/inside_microsoft_research/2015/07/01/microsoft-researcher-don-syme-honored-with-silver-medal-from-royal-academy-of-engineering/).
  
This was a great year for F#. I have a feeling that next year will be even better. 
  
 
Happy F# Day!