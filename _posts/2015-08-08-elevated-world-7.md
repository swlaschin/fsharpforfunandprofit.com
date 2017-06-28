---
layout: post
title: "Map and Bind and Apply, a summary"
description: ""
categories: ["Patterns"]
seriesId: "Map and Bind and Apply, Oh my!"
seriesOrder: 7
---

## Series summary

Well, [this series](/series/map-and-bind-and-apply-oh-my.html) turned out to be longer than I originally planned. Thanks for making it to the end!

I hope that this discussion has been helpful in understanding the various function transformations like `map` and `bind`, and given you
some useful techniques for dealing with world-crossing functions -- maybe even demystified the m-word a bit!

If you want to start using these kinds of functions in your own code, I hope that you can see how easy they are to write, but
you should also consider using one of the excellent F# utility libraries that contain these and much more:

* **ExtCore** ([source](https://github.com/jack-pappas/ExtCore), [NuGet](https://www.nuget.org/packages/ExtCore/)). 
  ExtCore provides extensions to the F# core library (FSharp.Core) and aims to help you build industrial-strength F# applications. 
  These extensions include additional functions for modules such as Array, List, Set, and Map; immutable IntSet, IntMap, LazyList, and 
  Queue collections; a variety of computation expressions (workflows); and "workflow collections" -- collections modules which have been 
  adapted to work seamlessly from within workflows.
  
* **FSharpx.Extras** ([home page](https://fsprojects.github.io/FSharpx.Extras/)). 
  FSharpx.Extras is part of the FSharpx series of libraries.
  It implements several standard monads (State, Reader, Writer, Either, Continuation, Distribution), 
  validation with applicative functors, general functions like flip, and some asynchronous programming utilities,
  and functions to make C# - F# interop easier.
  
For example, the monadic traverse `List.traverseResultM` that I implemented [in this post](/posts/elevated-world-4/#traverse) is already available in ExtCore 
[here](https://github.com/jack-pappas/ExtCore/blob/4fc2302e74a9b5217d980e5ce2680f0b3db26c3d/ExtCore/ControlCollections.Choice.fs#L398).
  
And if you liked this series, I have posts explaining the State monad in my series on ["Dr Frankenfunctor and the Monadster"](/posts/monadster/)
and the Either monad in my talk ["Railway Oriented Programming"](/rop/).

As I said at the very beginning, writing this up has been a learning process for me too.
I am not an expert, so if I have made any errors please do let me know.

Thanks!

## Series contents

Here's a list of shortcuts to the various functions mentioned in this series:

* **Part 1: Lifting to the elevated world**
  * [The `map` function](/posts/elevated-world/#map)
  * [The `return` function](/posts/elevated-world/#return)
  * [The `apply` function](/posts/elevated-world/#apply)
  * [The `liftN` family of functions](/posts/elevated-world/#lift)
  * [The `zip` function and ZipList world](/posts/elevated-world/#zip)
* **Part 2: How to compose world-crossing functions**    
  * [The `bind` function](/posts/elevated-world-2/#bind)
  * [List is not a monad. Option is not a monad.](/posts/elevated-world-2/#not-a-monad)
* **Part 3: Using the core functions in practice**  
  * [Independent and dependent data](/posts/elevated-world-3/#dependent)
  * [Example: Validation using applicative style and monadic style](/posts/elevated-world-3/#validation)
  * [Lifting to a consistent world](/posts/elevated-world-3/#consistent)
  * [Kleisli world](/posts/elevated-world-3/#kleisli)
* **Part 4: Mixing lists and elevated values**    
  * [Mixing lists and elevated values](/posts/elevated-world-4/#mixing)
  * [The `traverse`/`MapM` function](/posts/elevated-world-4/#traverse)
  * [The `sequence` function](/posts/elevated-world-4/#sequence)
  * ["Sequence" as a recipe for ad-hoc implementations](/posts/elevated-world-4/#adhoc)
  * [Readability vs. performance](/posts/elevated-world-4/#readability)
  * [Dude, where's my `filter`?](/posts/elevated-world-4/#filter)
* **Part 5: A real-world example that uses all the techniques**    
  * [Example: Downloading and processing a list of websites](/posts/elevated-world-5/#asynclist)
  * [Treating two worlds as one](/posts/elevated-world-5/#asyncresult)
* **Part 6: Designing your own elevated world** 
  * [Designing your own elevated world](/posts/elevated-world-6/#part6)
  * [Filtering out failures](/posts/elevated-world-6/#filtering)
  * [The Reader monad](/posts/elevated-world-6/#readermonad)
* **Part 7: Summary** 
  * [List of operators mentioned](/posts/elevated-world-7/#operators)
  * [Further reading](/posts/elevated-world-7/#further-reading)

<a id="operators"></a>
<hr>
  
## Appendix: List of operators mentioned

Unlike OO languages, functional programming languages are known for their [strange operators](http://en.cppreference.com/w/cpp/language/operator_precedence),
so I thought it would be helpful to document the ones that have been used in this series, with links back to the relevant discussion.

Operator  | Equivalent function | Discussion
-------------|---------|----
`>>`  | Left-to-right composition | Not part of this series, but [discussed here](/posts/function-composition/)
`<<`  | Right-to-left composition | As above
<code>&#124;></code>  | Left-to-right piping | As above
<code>&lt;&#124;</code> | Right-to-left piping | As above
`<!>` | `map` | [Discussed here](/posts/elevated-world/#map)
`<$>` | `map` | Haskell operator for map, but not a valid operator in F#, so I'm using `<!>` in this series.
`<*>` | `apply` | [Discussed here](/posts/elevated-world/#apply)
`<*`  | - | One sided combiner. [Discussed here](/posts/elevated-world/#lift)
`*>`  | - | One sided combiner. [Discussed here](/posts/elevated-world/#lift)
`>>=` | Left-to-right `bind` | [Discussed here](/posts/elevated-world-2/#bind)
`=<<` | Right-to-left `bind` | As above
`>=>` | Left-to-right Kleisli composition | [Discussed here](/posts/elevated-world-3/#kleisli)
`<=<` | Right-to-left Kleisli composition | As above


<a id="further-reading"></a>
<hr>
  
## Appendix: Further reading

Alternative tutorials:

* [You Could Have Invented Monads! (And Maybe You Already Have)](http://blog.sigfpe.com/2006/08/you-could-have-invented-monads-and.html).
* [Functors, Applicatives and Monads in pictures](http://adit.io/posts/2013-04-17-functors,_applicatives,_and_monads_in_pictures.html).
* [Kleisli composition à la Up-Goer Five](http://mergeconflict.com/kleisli-composition-a-la-up-goer-five/). I think this one is funny.
* [Eric Lippert's series on monads in C#](http://ericlippert.com/category/monads/).

For the academically minded:

* [Monads for Functional Programming](http://homepages.inf.ed.ac.uk/wadler/papers/marktoberdorf/baastad.pdf) (PDF), by Philip Wadler. One of the first monad papers.
* [Applicative Programming with Effects](http://www.soi.city.ac.uk/~ross/papers/Applicative.pdf) (PDF), by Conor McBride and Ross Paterson.
* [The Essence of the Iterator Pattern](http://www.comlab.ox.ac.uk/jeremy.gibbons/publications/iterator.pdf) (PDF), by Jeremy Gibbons and Bruno Oliveira.

F# examples:

* [F# ExtCore](https://github.com/jack-pappas/ExtCore) and
  [FSharpx.Extras](https://github.com/fsprojects/FSharpx.Extras/blob/master/src/FSharpx.Extras/ComputationExpressions/Monad.fs) have lots of useful code.
* [FSharpx.Async](https://github.com/fsprojects/FSharpx.Async/blob/master/src/FSharpx.Async/Async.fs) has `map`, `apply`, `liftN` (called "Parallel"), `bind`, and other useful extensions for `Async`.
* Applicatives are very well suited for parsing, as explained in these posts:
  * [Parsing with applicative functors in F#](http://bugsquash.blogspot.co.uk/2011/01/parsing-with-applicative-functors-in-f.html).
  * [Dive into parser combinators: parsing search queries with F# and FParsec in Kiln](http://blog.fogcreek.com/fparsec/).

