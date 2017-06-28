---
layout: post
title: "Concurrency"
description: "The next major revolution in how we write software?"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 23
categories: [Concurrency]
---


We hear a lot about concurrency nowadays, how important it is, and how it is ["the next major revolution in how we write software"](http://www.gotw.ca/publications/concurrency-ddj.htm).  

So what do we actually mean by "concurrency" and how can F# help?

The simplest definition of concurrency is just "several things happening at once, and maybe interacting with each other". It seems a trivial definition, but the key point is that most computer programs (and languages) are designed to work serially, on one thing at a time, and are not well-equipped to handle concurrency.

And even if computer programs are written to handle concurrency, there is an even more serious problem:  our brains do not do well when thinking about concurrency. It is commonly acknowledged that writing code that handles concurrency is extremely hard. Or I should say, writing concurrent code that is *correct* is extremely hard! It's very easy to write concurrent code that is buggy; there might be race conditions, or operations might not be atomic, or tasks might be starved or blocked unnecessarily, and these issues are hard to find by looking at the code or using a debugger.

Before talking about the specifics of F#, let's try to classify some of the common types of concurrency scenarios that we have to deal with as developers: 

* **"Concurrent Multitasking"**. This is when we have a number of concurrent tasks (e.g. processes or threads) within our direct control, and we want them to communicate with each other and share data safely.
* **"Asynchronous" programming**. This is when we initiate a conversation with a separate system outside our direct control, and then wait for it to get back to us. Common cases of this are when talking to the filesystem, a database, or the network. These situations are typically I/O bound, so you want to do something else useful while you are waiting. These types of tasks are often *non-deterministic* as well, meaning that running the same program twice might give a different result.
* **"Parallel" programming**. This is when we have a single task that we want to split into independant subtasks, and then run the subtasks in parallel, ideally using all the cores or CPUs that are available. These situations are typically CPU bound. Unlike the async tasks, parallelism is typically  *deterministic*, so running the same program twice will give the same result.
* **"Reactive" programming**. This is when we do not initiate tasks ourselves, but are focused on listening for events which we then process as fast as possible. This situation occurs when designing servers, and when working with a user interface.

Of course, these are vague definitions and overlap in practice. In general, though, for all these cases, the actual implementations that address these scenarios tend to use two distinct approaches: 

* If there are lots of different tasks that need to share state or resources without waiting, then use a "buffered asynchronous" design.
* If there are lots of identical tasks that do not need to share state, then use parallel tasks using "fork/join" or "divide and conquer" approaches.

## F# tools for concurrent programming ##

F# offers a number of different approaches to writing concurrent code:

* For multitasking and asynchronous problems, F# can directly use all the usual .NET suspects, such as `Thread` 
`AutoResetEvent`, `BackgroundWorker` and `IAsyncResult`. But it also offers a much simpler model for all types of async IO and background task management, called "asynchronous workflows". 
We will look at these in the next post.

* An alternative approach for asynchronous problems is to use message queues and the ["actor model"](http://en.wikipedia.org/wiki/Actor_model) (this is the "buffered asynchronous" design mentioned above). F# has a built in implementation of the actor model called `MailboxProcessor`.
  I am a big proponent of designing with actors and message queues, as it decouples the various components and allows you to think serially about each one.

* For true CPU parallelism, F# has convenient library code that builds on the asynchronous workflows mentioned above, and it can also use the .NET [Task Parallel Library](http://msdn.microsoft.com/en-us/library/dd460717.aspx).

* Finally, the functional approach to event handling and reactive programming is quite different from the traditional approach. The functional approach treats events as "streams" which can be filtered, 
split, and combined in much the the same way that LINQ handles collections.  F# has built in support for this model, as well as for the standard event-driven model.


