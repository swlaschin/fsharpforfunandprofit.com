---
layout: post
title: "Against Railway-Oriented Programming"
description: "(when used thoughtlessly)"
date: 2019-12-20
categories: []
---

> This post is part of the [2019 F# Advent Calendar](https://sergeytihon.com/2019/11/05/f-advent-calendar-in-english-2019/).
> Check out all the other great posts there! And special thanks to Sergey Tihon for organizing this.

Six and half years ago, I wrote a post and did a talk on what I called ["Railway Oriented Programming"](https://fsharpforfunandprofit.com/rop/). It was a way for me to explain to myself and others how to use `Result`/`Either` to for chaining together error-generating functions.

To my surprise, this silly railway analogy really took off, and now there are railway-oriented programming libraries and posts for all sorts of languages, including [Ruby](https://www.morozov.is/2018/05/27/do-notation-ruby.html), [Java](https://github.com/StefanMacke/ao-railway), [JavaScript](https://dorp.io/posts/railway-oriented-programming/), [Kotlin](https://proandroiddev.com/railway-oriented-programming-in-kotlin-f1bceed399e5?gi=ce6e3bd2f69),
[Python](https://github.com/rob-earwaker/rail) and more.

I still think it's a good analogy, but I do think it is [often used thoughtlessly](https://twitter.com/scottwlaschin/status/997009818329198592), especially if it's a shiny new technique that you've just added to your toolbox.

So, in this post, I'm going to lay out reasons why you *shouldn't* use Railway-Oriented Programming! Or to be more precise, why you shouldn't use the `Result` type everywhere (because ROP is just the plumbing that is used to connect `Result`-returning functions). The [Microsoft page on error management](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/conventions#error-management) also has good advice, as does [this blog post](https://eiriktsarpalis.wordpress.com/2017/02/19/youre-better-off-using-exceptions/).

## #1 -- Don't use Result if you need diagnostics

If you care about the location of an error, having a stack trace, or other diagnostics, don't use `Result`. In particular, don't use `Result` as a substitute for exceptions but then store a stack trace or a whole exception inside a `Result`. What's the point?

Instead, think of `Result` as a glorified boolean with extra information. It's only for *expected* control-flow, not for unexpected situations.

## #2 -- Don't use Result to reinvent exceptions

I've see people use `Result` indiscriminately for all kinds of error handling, including things that would be better handled with exceptions. Don't reinvent "try-catch"!

I've also seen people trying to hide exceptions altogether. This is fruitless. No matter how many exceptions you convert into `Result`s, some will always leak out. You will always need to handle exceptions appropriately in the highest parts of the system.

## #3 -- Don't use Result if you need to fail fast

If something does go wrong, and you can't continue, don't return a `Result` and keep going. Fail fast instead with an exception or even just [exit the app immediately](https://docs.microsoft.com/en-us/dotnet/api/system.environment.failfast).

## #4 -- Don't use Result if no one will see it

If you are doing some complex control flow but the logic is hidden from the outside world, don't use `Result` just for the sake of it. Often, using an exception locally will be cleaner.

For example, let's say you are collecting information by traversing a tree, and you need to exit early when something goes wrong.

In the ROP approach, you'd have the node processing function return a `Result`, which then has to be passed to the next node processing function using `bind`, and so on. For complex navigation, you can spend a lot of time working out the logic so that the code will compile ([Haskell programmers excepted, of course](https://hackage.haskell.org/package/recursion-schemes))

On the other hand, you could define a private local exception (e.g. in the style of Python's `StopIteration`), write the iteration imperatively, throw the exception when you need to return early, and then catch the exception at the top level. As long as the code is not too long, and the exception is defined locally, this approach can often make the code clearer. And if no consumers ever see the internals, then no harm, no foul.

Another example might be when defining microservices. If the entire code is only a few hundred lines long, and is opaque to the callers, then using exceptions rather than `Result` is perfectly OK as long as they don't escape the service boundary.

## #5 -- Don't use Result if no one cares about the error cases

Typically, `Result` is defined with the error case being a discriminated union of all the things that can go wrong.

For example, lets say you want to read the text from a file, so you define a function like this:

```
type ReadTextFromFile = FileInfo -> Result<string, FileError>
```

where `FileError` is defined like this:

```
type FileError =
  | FileNotFound
  | DirectoryNotFound
  | FileNotAccessible
  | PathTooLong
  | OtherIOError of string
```

But do the consumers of this function really care about every possible thing that can go wrong reading a file?
Perhaps they just want the text, and they don't care why it didn't work. In which case, it might be simpler to return an `option` instead, like this:

```
type ReadTextFromFile = FileInfo -> string option
```

`Result` is a tool for domain modeling, so if the domain model doesn't need it, don't use it.

A similar example can be found when implementing event sourcing, in the [command processing function](https://medium.com/@dzoukr/event-sourcing-step-by-step-in-f-be808aa0ca18) which has the standard signature

```
'state -> 'command -> 'event list
```

If something goes wrong in executing the command, how does that affect the return value (the list of events created by the command) in practice? Of course you need to handle errors and log them, but do you actually need to return a `Result` from the function itself? It will make the code more complicated for not much benefit.

## #6 -- Be careful when using Result for I/O errors

If you try to open a file, but you get an error, should you wrap that in a `Result`? It depends on your domain.
If you're writing a word processor, not being able to open a file is expected and should be handled gracefully. On the other hand, if you can't open a config file that your app depends on, you shouldn't return a `Result`, you should just fail fast.

Anywhere that there is I/O there will many, many things that can go wrong. It is tempting to try to model all possibilities with a `Result`, but I strongly advise against this. Instead, only model the bare minimum that you need for your domain, and let all the other errors become exceptions.

Of course, if you follow best practices and separate your I/O from your pure business logic, then you should rarely need to work with exceptions in your core code anyway.

## #7 -- Don't use Result if you care about performance

This is more of a "be careful" than an absolute prohibition. If you know up front that you have a section of performance-sensitive code, then be wary of using `Result` there. In fact, you probably want to be wary of other built-in types too (e.g. `List`). But as always, measure to find the hotspots rather than guessing in advance so that you don't over-optimize the wrong thing.

## #8 -- Don't use Result if you care about interop

Most OO languages do not understand `Result` or other discriminated unions. If you need to return a possible failure from an API, consider using an approach that is more idiomatic for the caller. Even -- shock horror -- using null on occasion. Don't force the caller to become an expert in functional idioms just so they can call your API.


## Summary of the reasons not to use Result

* **Diagnostics**: If you care about stack traces or the location of an error, don't use `Result`.
* **Reinventing try/catch**: Why not use the language tools that are already built-in?
* **Fail fast**: If the end of your workflow will throw an exception anyway, don't use `Result` inside the workflow.
* **Local exceptions for control flow are OK**: If the control flow is complicated and private, it's OK to use exceptions for control flow.
* **Apathy**: Don't return a `Result` if no one cares about the errors.
* **I/O**: Don't try and model every possible I/O error with a Result.
* **Performance**: If you care about performance, be wary of using `Result`.
* **Interop**: If you care about interop, don't force callers to understand what `Result` is and how it works.

## When should you use Result?

So after all that negativity, what situations *should* you use `Result` for?

As I said in my book [*Domain Modeling Made Functional*](/books/), I like to classify errors into three classes:

* **Domain Errors**. These are errors that are to be expected as part of the business process, and therefore must be included in the design of the domain. For example, an order that is rejected by billing, or an order than contains an invalid product code. The business will already have procedures in place to deal with this kind of thing, and so the code will need to reflect these processes. Domain errors are part of the domain, like anything else, and so should be incorporated into our domain modeling, discussed with domain experts, and
documented in the type system if possible. Note that diagnostics are not needed -- we are using `Result` as a glorified `bool`.
* **Panics**. These are errors that leave the system in an unknown state, such as unhandleable system errors (e.g. "out of memory") or errors caused by programmer oversight (e.g. "divide by zero," "null reference"). Panics are best handled by abandoning the workflow and raising an exception which is then caught and logged at the highest appropriate level (e.g. the main function of the application or equivalent).
* **Infrastructure Errors**. These are errors that are to be expected as part of the architecture, but are not part of any business process and are not included in the domain. For example, a network timeout, or an authentication failure. Sometimes handling these should be modeled as part of the domain, and sometimes they can be treated as panics. If in doubt, ask a domain expert!

So using the definitions above:

* `Result` should only be used as part of the domain modeling process, to document expected return values. And then to ensure at compile-time that you handle all the possible *expected* error cases.
* Micro-domains, such as libraries, could also use `Result` if appropriate.

So to sum up, I think the `Result` type and railway-oriented programming are extremely useful when used appropriately, but the use-cases are more limited than you might think, and they shouldn't be used everywhere just because it's cool and interesting.

Thanks for reading! If you're interested in more F# posts, check out the rest of the [2019 F# Advent Calendar](https://sergeytihon.com/2019/11/05/f-advent-calendar-in-english-2019/).

----

{{<rawhtml>}}
<table border="0" >
<tr>
<td width="150">
<a href="/books"><img src="/books/domain-modeling-made-functional-150.jpg"></a>
</td>
<td style="padding: 10px">
And if you are interested in the functional approach to domain modeling and design, here's <a href="/books">my "Domain Modeling Made Functional" book!</a>
It's a beginner-friendly introduction that covers Domain Driven Design, modeling with types, and functional programming.
</td>
</tr>
</table>
{{</rawhtml>}}












