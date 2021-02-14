---
layout: page
title: "Railway Oriented Programming"
description: Slides and videos explaining a functional approach to error handling
hasComments: 1
image: "/rop/rop427.jpg"
date: 2020-01-01
---

This page contains links to the slides and code from my talk "Railway Oriented Programming".

Here's the blurb for the talk:

> Many examples in functional programming assume that you are always on the "happy path".
> But to create a robust real world application you must deal with validation, logging,
> network and service errors, and other annoyances.
> \
> \
> So, how do you handle all this in a clean functional way?
> \
> \
> This talk will provide a brief introduction to this topic,
> using a fun and easy-to-understand railway analogy.

I am also planning to upload some posts on these topics soon. Meanwhile, please see the [recipe for a functional app](/series/a-recipe-for-a-functional-app.html) series, which covers similar ground.

If you want to to see some real code, I have created
[this project on Github that compares normal C# with F# using the ROP approach](https://github.com/swlaschin/Railway-Oriented-Programming-Example)

WARNING: This is a useful approach to error handling, but please don't take it to extremes! See my post on ["Against Railway-Oriented Programming"](/posts/against-railway-oriented-programming/).

## Videos

I presented on this topic at NDC London 2014 (click image to view video)

[![Video from NDC London 2014](rop427.jpg)](https://goo.gl/Lv5ZAo)

Other videos of this talk are available are from [NDC Oslo 2014](http://vimeo.com/97344498)
and [Functional Programming eXchange, 2014](https://skillsmatter.com/skillscasts/4964-railway-oriented-programming)


## Slides

Slides from Functional Programming eXchange, March 14, 2014

{{< slideshare "9eUxEVfdTUTTh8" "railway-oriented-programming" "Railway Oriented Programming" >}}

The powerpoint slides are also available from [Github](https://github.com/swlaschin/RailwayOrientedProgramming). Feel free to borrow from them!

{{<  book_page_explain >}}

{{< linktarget "monads" >}}

## Relationship to the Either monad and Kleisli composition ##

Any Haskellers reading this will immediately recognize this approach as the [`Either` type](http://book.realworldhaskell.org/read/error-handling.html),
specialized to use a list of a custom error type for the Left case. In Haskell, something like: `type TwoTrack a b = Either [a] (b,[a])`

I'm certainly not trying to claim that I invented this approach at all (although I do lay claim to the silly analogy).  So why did I not use the standard Haskell terminology?

First, **this post is not trying to be a monad tutorial**, but is instead focused on solving the specific problem of error handling.

Most people coming to F# are not familiar with monads. I'd rather present an approach that is visual, non-intimidating, and generally more intuitive for many people.

I am strong believer in a ["begin with the concrete, and move to the abstract"](https://byorgey.wordpress.com/2009/01/12/abstraction-intuition-and-the-monad-tutorial-fallacy/)
pedagogical approach. In my experience, once you are familiar with this particular approach, the higher level abstractions are easier to grasp later.

Second, I would be incorrect to claim that my two-track type with bind is a monad anyway -- a monad is more complicated than that, and I just didn't want to get into the monad laws here.

Third, and most importantly, `Either` is too general a concept. **I wanted to present a recipe, not a tool**.

For example, if I want a recipe for making a loaf of bread, saying "just use flour and an oven" is not very helpful.

And so, in the same way, if I want a recipe for handling errors, saying "just use Either with bind" is not very helpful.

So, in this approach, I'm presenting a whole series of techniques:

* Using a list of custom error types on both the left and right sides of Either (rather than, say, `Either String a`).
* "bind" (`>>=`) for integrating monadic functions into the pipeline.
* Kleisli composition (`>=>`) for composing monadic functions.
* "map" (`fmap`) for integrating non-monadic functions into the pipeline.
* "tee" for integrating unit functions into the pipeline (because F# doesn't use the IO monad).
* Mapping from exceptions to error cases.
* `&&&` for combining monadic functions in parallel (e.g. for validation).
* The benefits of custom error types for domain driven design.
* And obvious extensions for logging, domain events, compensating transactions, and more.

I hope you can see that this is a more comprehensive approach than "just use the Either monad"!

My goal here is to provide a template that is versatile enough to be
used in almost all situations, yet constrained enough to enforce a consistent style.
That is, there is basically only one way to write the code. This is extremely helpful to anyone who has to maintain the code later,
as they can immediately understand how it is put together.

I'm not saying that this is the *only* way to do it. But I think this approach is a good start.

As an aside, even in the Haskell community [there is no consistent approach to error handling](http://www.randomhacks.net/2007/03/10/haskell-8-ways-to-report-errors/), which
can make things [confusing for beginners](http://programmers.stackexchange.com/questions/252977/cleanest-way-to-report-errors-in-haskell).
I know that there is a [lot of content](http://www.fpcomplete.com/school/starting-with-haskell/basics-of-haskell/10_Error_Handling)
about the individual [error handling techniques](http://hackage.haskell.org/package/errors), but I'm not aware of a document that brings all these tools
together in a comprehensive way.

## How can I use this in my own code?

* If you want a ready-made F# library that works with NuGet, check out the [Chessie project](https://fsprojects.github.io/Chessie/).
* If you want to see a sample web-service using these techniques, [I have created a project on GitHub](https://github.com/swlaschin/Railway-Oriented-Programming-Example).
* You can also [see the ROP approach applied to FizzBuzz](/posts/railway-oriented-programming-carbonated/)!

F# does not have type classes, and so you don't really have a reusable way to do monads (although the [FSharpX library](https://github.com/fsprojects/fsharpx/blob/master/src/FSharpx.Core/ComputationExpressions/Monad.fs)
has a useful approach).  This means the `Rop.fs` library defines all its functions from scratch.
(In some ways though, this isolation can be helpful because there are no external dependencies at all.)

## Further reading

> *"One bind does not a monad make" -- Aristotle*

As I mentioned above, one reason why I stayed away from monads is that defining a monad correctly is *not* just a matter of implementing "bind" and "return".
It is an algebraic structure that needs to obey the monad laws (which in turn are just the [monoid laws](/posts/monoids-without-tears/) in a specific situation)
and that was a path I did not want to head down in this particular talk.

However if you are interested in more detail on `Either` and Kleisi composition, here are some links that might be useful:

* **Monads in general**.
  * [Stack overflow answer on monads](http://stackoverflow.com/questions/44965/what-is-a-monad)
  * [Stack overflow answer by Eric Lippert](http://stackoverflow.com/questions/2704652/monad-in-plain-english-for-the-oop-programmer-with-no-fp-background/2704795#2704795)
  * [Monads in pictures](http://adit.io/posts/2013-04-17-functors,_applicatives,_and_monads_in_pictures.html)
  * ["You Could Have Invented Monads"](http://blog.sigfpe.com/2006/08/you-could-have-invented-monads-and.html)
  * [Haskell tutorial](https://www.haskell.org/tutorial/monads.html)
  * [The hardcore definition on nLab](http://ncatlab.org/nlab/show/monad)
* **The `Either` monad**.
  * [School of Haskell](http://www.fpcomplete.com/school/starting-with-haskell/basics-of-haskell/10_Error_Handling)
  * [Real World Haskell on error handling](http://book.realworldhaskell.org/read/error-handling.html) (halfway down)
  * [LYAH on error handling](http://learnyouahaskell.com/for-a-few-monads-more) (halfway down)
* **Kleisli categories and composition**
  * [Post at FPComplete](http://www.fpcomplete.com/user/Lkey/kleisli)
  * [Post by Bartosz Milewski](http://bartoszmilewski.com/2014/12/23/kleisli-categories/)
  * [The hardcore definition on nLab](http://ncatlab.org/nlab/show/Kleisli+category)
* **Comprehensive error handling approaches**
  * [Item 5 in this post](http://www.randomhacks.net/2007/03/10/haskell-8-ways-to-report-errors/)
  * I'm not aware of other approaches that cover all the techniques discussed in this talk.
    If you do know of any, ping me in the comments and I'll update this page.