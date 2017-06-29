---
layout: post
title: "Functional Reactive Programming"
description: "Turning events into streams"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 26
categories: [Concurrency]
---

Events are everywhere.  Almost every program has to handle events, whether it be button clicks in the user interface, listening to sockets in a server, or even a system shutdown notification.

And events are the basis of one of the most common OO design patterns: the "Observer" pattern.

But as we know, event handling, like concurrency in general, can be tricky to implement.  Simple event logic is straightforward, but what about logic like "do something if two events happen in a row but do something different if only one event happens" or "do something if two events happen at roughly the same time". And how easy is it to combine these requirements in other, more complex ways?

Even you can successfully implement these requirements, the code tends to be spaghetti like and hard to understand, even with the best intentions.

Is there a approach that can make event handling easier? 

We saw in the previous post on message queues that one of the advantages of that approach was that the requests were "serialized" making it conceptually easier to deal with.
 
There is a similar approach that can be used with events. The idea is to turn a series of events into an "event stream". 
Event streams then become quite like IEnumerables, and so the obvious next step
is to treat them in much the the same way that LINQ handles collections, so that they can be filtered, mapped, split and combined.

F# has built in support for this model, as well as for the more tradition approach.

## A simple event stream ##

Let's start with a simple example to compare the two approaches. We'll implement the classic event handler approach first.

First, we define a utility function that will:

* create a timer
* register a handler for the `Elapsed` event
* run the timer for five seconds and then stop it

Here's the code:

```fsharp
open System
open System.Threading

/// create a timer and register an event handler, 
/// then run the timer for five seconds
let createTimer timerInterval eventHandler =
    // setup a timer
    let timer = new System.Timers.Timer(float timerInterval)
    timer.AutoReset <- true
    
    // add an event handler
    timer.Elapsed.Add eventHandler

    // return an async task
    async {
        // start timer...
        timer.Start()
        // ...run for five seconds...
        do! Async.Sleep 5000
        // ... and stop
        timer.Stop()
        }
```

Now test it interactively:

```fsharp
// create a handler. The event args are ignored
let basicHandler _ = printfn "tick %A" DateTime.Now

// register the handler
let basicTimer1 = createTimer 1000 basicHandler

// run the task now
Async.RunSynchronously basicTimer1 
```

Now let's create a similar utility method to create a timer, but this time it will return an "observable" as well, which is the stream of events.

```fsharp
let createTimerAndObservable timerInterval =
    // setup a timer
    let timer = new System.Timers.Timer(float timerInterval)
    timer.AutoReset <- true

    // events are automatically IObservable
    let observable = timer.Elapsed  

    // return an async task
    let task = async {
        timer.Start()
        do! Async.Sleep 5000
        timer.Stop()
        }

    // return a async task and the observable
    (task,observable)
```

And again test it interactively:

```fsharp
// create the timer and the corresponding observable
let basicTimer2 , timerEventStream = createTimerAndObservable 1000

// register that everytime something happens on the 
// event stream, print the time.
timerEventStream 
|> Observable.subscribe (fun _ -> printfn "tick %A" DateTime.Now)

// run the task now
Async.RunSynchronously basicTimer2
```

The difference is that instead of registering a handler directly with an event, 
we are "subscribing" to an event stream. Subtly different, and important.

## Counting events  ##

In this next example, we'll have a slightly more complex requirement: 

    Create a timer that ticks every 500ms. 
    At each tick, print the number of ticks so far and the current time.

To do this in a classic imperative way, we would probably create a class with a mutable counter, as below:

```fsharp
type ImperativeTimerCount() =
    
    let mutable count = 0

    // the event handler. The event args are ignored
    member this.handleEvent _ =
      count <- count + 1
      printfn "timer ticked with count %i" count
```

We can reuse the utility functions we created earlier to test it:

```fsharp
// create a handler class
let handler = new ImperativeTimerCount()

// register the handler method
let timerCount1 = createTimer 500 handler.handleEvent

// run the task now
Async.RunSynchronously timerCount1 
```

Let's see how we would do this same thing in a functional way:

```fsharp
// create the timer and the corresponding observable
let timerCount2, timerEventStream = createTimerAndObservable 500

// set up the transformations on the event stream
timerEventStream 
|> Observable.scan (fun count _ -> count + 1) 0 
|> Observable.subscribe (fun count -> printfn "timer ticked with count %i" count)

// run the task now
Async.RunSynchronously timerCount2
```

Here we see how you can build up layers of event transformations, just as you do with list transformations in LINQ.

The first transformation is `scan`, which accumulates state for each event. It is roughly equivalent to the `List.fold` function that we have seen used with lists.
In this case, the accumulated state is just a counter.

And then, for each event, the count is printed out.

Note that in this functional approach, we didn't have any mutable state, and we didn't need to create any special classes.

## Merging multiple event streams  ##

For a final example, we'll look at merging multiple event streams.

Let's make a requirement based on the well-known "FizzBuzz" problem: 

    Create two timers, called '3' and '5'. The '3' timer ticks every 300ms and the '5' timer ticks 
    every 500ms. 
    
    Handle the events as follows:
    a) for all events, print the id of the time and the time
    b) when a tick is simultaneous with a previous tick, print 'FizzBuzz'
    otherwise:
    c) when the '3' timer ticks on its own, print 'Fizz'
    d) when the '5' timer ticks on its own, print 'Buzz'

First let's create some code that both implementations can use. 

We'll want a generic event type that captures the timer id and the time of the tick.
    
```fsharp
type FizzBuzzEvent = {label:int; time: DateTime}
```

And then we need a utility function to see if two events are simultaneous. We'll be generous and allow a time difference of up to 50ms.

```fsharp
let areSimultaneous (earlierEvent,laterEvent) =
    let {label=_;time=t1} = earlierEvent
    let {label=_;time=t2} = laterEvent
    t2.Subtract(t1).Milliseconds < 50
```

In the imperative design, we'll need to keep track of the previous event, so we can compare them. 
And we'll need special case code for the first time, when the previous event doesn't exist

```fsharp
type ImperativeFizzBuzzHandler() =
 
    let mutable previousEvent: FizzBuzzEvent option = None
 
    let printEvent thisEvent  = 
      let {label=id; time=t} = thisEvent
      printf "[%i] %i.%03i " id t.Second t.Millisecond
      let simultaneous = previousEvent.IsSome && areSimultaneous (previousEvent.Value,thisEvent)
      if simultaneous then printfn "FizzBuzz"
      elif id = 3 then printfn "Fizz"
      elif id = 5 then printfn "Buzz"
 
    member this.handleEvent3 eventArgs =
      let event = {label=3; time=DateTime.Now}
      printEvent event
      previousEvent <- Some event
 
    member this.handleEvent5 eventArgs =
      let event = {label=5; time=DateTime.Now}
      printEvent event
      previousEvent <- Some event
```

Now the code is beginning to get ugly fast! Already we have mutable state, complex conditional logic, and special cases, just for such a simple requirement.

Let's test it:
        
```fsharp
// create the class
let handler = new ImperativeFizzBuzzHandler()

// create the two timers and register the two handlers
let timer3 = createTimer 300 handler.handleEvent3
let timer5 = createTimer 500 handler.handleEvent5
 
// run the two timers at the same time
[timer3;timer5]
|> Async.Parallel
|> Async.RunSynchronously
```

It does work, but are you sure the code is not buggy? Are you likely to accidentally break something if you change it?
 
The problem with this imperative code is that it has a lot of noise that obscures the the requirements. 

Can the functional version do better? Let's see!

First, we create *two* event streams, one for each timer:

```fsharp
let timer3, timerEventStream3 = createTimerAndObservable 300
let timer5, timerEventStream5 = createTimerAndObservable 500
```

Next, we convert each event on the "raw" event streams into our FizzBuzz event type:
 
```fsharp
// convert the time events into FizzBuzz events with the appropriate id
let eventStream3  = 
   timerEventStream3  
   |> Observable.map (fun _ -> {label=3; time=DateTime.Now})

let eventStream5  = 
   timerEventStream5  
   |> Observable.map (fun _ -> {label=5; time=DateTime.Now})
```

Now, to see if two events are simultaneous, we need to compare them from the two different streams somehow.

It's actually easier than it sounds, because we can: 

* combine the two streams into a single stream:
* then create pairs of sequential events
* then test the pairs to see if they are simultaneous
* then split the input stream into two new output streams based on that test

Here's the actual code to do this:
 
```fsharp
// combine the two streams
let combinedStream = 
    Observable.merge eventStream3 eventStream5
 
// make pairs of events
let pairwiseStream = 
   combinedStream |> Observable.pairwise
 
// split the stream based on whether the pairs are simultaneous
let simultaneousStream, nonSimultaneousStream = 
    pairwiseStream |> Observable.partition areSimultaneous
```


Finally, we can split the `nonSimultaneousStream` again, based on the event id:

```fsharp
// split the non-simultaneous stream based on the id
let fizzStream, buzzStream  =
    nonSimultaneousStream  
    // convert pair of events to the first event
    |> Observable.map (fun (ev1,_) -> ev1)
    // split on whether the event id is three
    |> Observable.partition (fun {label=id} -> id=3)
```

Let's review so far. We have started with the two original event streams and from them created four new ones:

* `combinedStream` contains all the events
* `simultaneousStream` contains only the simultaneous events
* `fizzStream` contains only the non-simultaneous events with id=3
* `buzzStream` contains only the non-simultaneous events with id=5

Now all we need to do is attach behavior to each stream:

```fsharp
//print events from the combinedStream
combinedStream 
|> Observable.subscribe (fun {label=id;time=t} -> 
                              printf "[%i] %i.%03i " id t.Second t.Millisecond)
 
//print events from the simultaneous stream
simultaneousStream 
|> Observable.subscribe (fun _ -> printfn "FizzBuzz")

//print events from the nonSimultaneous streams
fizzStream 
|> Observable.subscribe (fun _ -> printfn "Fizz")

buzzStream 
|> Observable.subscribe (fun _ -> printfn "Buzz")
```

Let's test it:
        
```fsharp
// run the two timers at the same time
[timer3;timer5]
|> Async.Parallel
|> Async.RunSynchronously
```

Here's all the code in one complete set:

```fsharp
// create the event streams and raw observables
let timer3, timerEventStream3 = createTimerAndObservable 300
let timer5, timerEventStream5 = createTimerAndObservable 500

// convert the time events into FizzBuzz events with the appropriate id
let eventStream3  = timerEventStream3  
                    |> Observable.map (fun _ -> {label=3; time=DateTime.Now})
let eventStream5  = timerEventStream5  
                    |> Observable.map (fun _ -> {label=5; time=DateTime.Now})

// combine the two streams
let combinedStream = 
   Observable.merge eventStream3 eventStream5
 
// make pairs of events
let pairwiseStream = 
   combinedStream |> Observable.pairwise
 
// split the stream based on whether the pairs are simultaneous
let simultaneousStream, nonSimultaneousStream = 
   pairwiseStream |> Observable.partition areSimultaneous

// split the non-simultaneous stream based on the id
let fizzStream, buzzStream  =
    nonSimultaneousStream  
    // convert pair of events to the first event
    |> Observable.map (fun (ev1,_) -> ev1)
    // split on whether the event id is three
    |> Observable.partition (fun {label=id} -> id=3)

//print events from the combinedStream
combinedStream 
|> Observable.subscribe (fun {label=id;time=t} -> 
                              printf "[%i] %i.%03i " id t.Second t.Millisecond)
 
//print events from the simultaneous stream
simultaneousStream 
|> Observable.subscribe (fun _ -> printfn "FizzBuzz")

//print events from the nonSimultaneous streams
fizzStream 
|> Observable.subscribe (fun _ -> printfn "Fizz")

buzzStream 
|> Observable.subscribe (fun _ -> printfn "Buzz")

// run the two timers at the same time
[timer3;timer5]
|> Async.Parallel
|> Async.RunSynchronously
```

The code might seem a bit long winded, but this kind of incremental, step-wise approach is very clear and self-documenting. 

Some of the benefits of this style are:

* I can see that it meets the requirements just by looking at it, without even running it. Not so with the imperative version.
* From a design point of view, each final "output" stream follows the single responsibility principle -- it only does one thing -- so it is very easy to
associate behavior with it. 
* This code has no conditionals, no mutable state, no edge cases. It would be easy to maintain or change, I hope.
* It is easy to debug. For example, I could easily "tap" the output of the `simultaneousStream` to see if it
contains what I think it contains:

```fsharp
// debugging code
//simultaneousStream |> Observable.subscribe (fun e -> printfn "sim %A" e)
//nonSimultaneousStream |> Observable.subscribe (fun e -> printfn "non-sim %A" e)
```

This would be much harder in the imperative version.
 
## Summary ##

Functional Reactive Programming (known as FRP) is a big topic, and we've only just touched on it here. I hope this introduction has given you a glimpse of the usefulness of this way of doing things.

If you want to learn more, see the documentation for the F# [Observable module](http://msdn.microsoft.com/en-us/library/ee370313), which has the basic transformations used above. 
And there is also the [Reactive Extensions (Rx)](http://msdn.microsoft.com/en-us/library/hh242985%28v=vs.103%29) library which shipped as part of .NET 4.  That contains many other additional transformations.



 
