---
layout: post
title: "Thirteen ways of looking at a turtle (part 2)"
description: "Continuing with examples of event sourcing, FRP, monadic control flow, and an interpreter."
categories: [Patterns]
---

*UPDATE: [Slides and video from my talk on this topic](/turtle/)*

> This post is part of the [F# Advent Calendar in English 2015](https://sergeytihon.wordpress.com/2015/10/25/f-advent-calendar-in-english-2015/) project.
> Check out all the other great posts there! And special thanks to Sergey Tihon for organizing this.

In this two-part mega-post, I'm stretching the simple turtle graphics model to the limit while demonstrating partial application, validation, the concept of "lifting",
agents with message queues, dependency injection, the State monad, event sourcing, stream processing, and an interpreter!

In the [previous post](/posts/13-ways-of-looking-at-a-turtle/), we covered the first nine ways of looking at a turtle. In this post, we'll look at the remaining four.

As a reminder, here are the thirteen ways:

* [Way 1. A basic object-oriented approach](/posts/13-ways-of-looking-at-a-turtle/#way1), in which we create a class with mutable state.
* [Way 2. A basic functional approach](/posts/13-ways-of-looking-at-a-turtle/#way2), in which we create a module of functions with immutable state.
* [Way 3. An API with a object-oriented core](/posts/13-ways-of-looking-at-a-turtle/#way3), in which we create an object-oriented API that calls a stateful core class.
* [Way 4. An API with a functional core](/posts/13-ways-of-looking-at-a-turtle/#way4), in which we create an stateful API that uses stateless core functions.
* [Way 5. An API in front of an agent](/posts/13-ways-of-looking-at-a-turtle/#way5), in which we create an API that uses a message queue to communicate with an agent.
* [Way 6. Dependency injection using interfaces](/posts/13-ways-of-looking-at-a-turtle/#way6), in which we decouple the implementation from the API using an interface or record of functions.
* [Way 7. Dependency injection using functions](/posts/13-ways-of-looking-at-a-turtle/#way7), in which we decouple the implementation from the API by passing a function parameter.
* [Way 8. Batch processing using a state monad](/posts/13-ways-of-looking-at-a-turtle/#way8), in which we create a special "turtle workflow" computation expression to track state for us.
* [Way 9. Batch processing using command objects](/posts/13-ways-of-looking-at-a-turtle/#way9), in which we create a type to represent a turtle command, and then process a list of commands all at once.
* [Interlude: Conscious decoupling with data types](/posts/13-ways-of-looking-at-a-turtle/#decoupling). A few notes on using data vs. interfaces for decoupling.
* [Way 10. Event sourcing](/posts/13-ways-of-looking-at-a-turtle-2/#way10), in which  state is built from a list of past events.
* [Way 11. Functional Retroactive Programming (stream processing)](/posts/13-ways-of-looking-at-a-turtle-2/#way11), in which business logic is based on reacting to earlier events.
* [Episode V: The Turtle Strikes Back](/posts/13-ways-of-looking-at-a-turtle-2/#strikes-back), in which the turtle API changes so that some commands may fail.
* [Way 12. Monadic control flow](/posts/13-ways-of-looking-at-a-turtle-2/#way12), in which we make decisions in the turtle workflow based on results from earlier commands.
* [Way 13. A turtle interpreter](/posts/13-ways-of-looking-at-a-turtle-2/#way13), in which we completely decouple turtle programming from turtle implementation, and nearly encounter the free monad.
* [Review of all the techniques used](/posts/13-ways-of-looking-at-a-turtle-2/#review).

and 2 bonus ways for the extended edition:

* [Way 14. Abstract Data Turtle](/posts/13-ways-of-looking-at-a-turtle-3/#way14), in which we encapsulate the details of a turtle implementation by using an Abstract Data Type.
* [Way 15. Capability-based Turtle](/posts/13-ways-of-looking-at-a-turtle-3/#way15), in which we control what turtle functions are available to a client, based on the current
  state of the turtle.

It's turtles all the way down!

All source code for this post is available [on github](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle).

<hr>

<a id="way10"></a>

## 10: Event sourcing -- Building state from a list of past events

In this design, we build on the "command" concept used in the [Agent (way 5)](/posts/13-ways-of-looking-at-a-turtle/#way5)
and [Batch (way 9)](/posts/13-ways-of-looking-at-a-turtle/#way9) approaches, but replacing "commands" with "events" as the method of updating state.

The way that it works is:

* The client sends a `Command` to a `CommandHandler`.
* Before processing a `Command`, the `CommandHandler` first rebuilds the current state
  from scratch using the past events associated with that particular turtle.
* The `CommandHandler` then validates the command and decides what to do based on the current (rebuilt) state.
  It generates a (possibly empty) list of events.
* The generated events are stored in an `EventStore` for the next command to use.

![](/assets/img/turtle-event-source.png)

In this way, neither the client nor the command handler needs to track state.  Only the `EventStore` is mutable.

### The Command and Event types

We will start by defining the types relating to our event sourcing system. First, the types related to commands:

```fsharp
type TurtleId = System.Guid

/// A desired action on a turtle
type TurtleCommandAction = 
    | Move of Distance 
    | Turn of Angle
    | PenUp 
    | PenDown 
    | SetColor of PenColor

/// A command representing a desired action addressed to a specific turtle
type TurtleCommand = {
    turtleId : TurtleId
    action : TurtleCommandAction 
    }
```

Note that the command is addressed to a particular turtle using a `TurtleId`.

Next, we will define two kinds of events that can be generated from a command:

* A `StateChangedEvent` which represents what changed in the state
* A `MovedEvent` which represents the start and end positions of a turtle movement.

```fsharp
/// An event representing a state change that happened
type StateChangedEvent = 
    | Moved of Distance 
    | Turned of Angle
    | PenWentUp 
    | PenWentDown 
    | ColorChanged of PenColor

/// An event representing a move that happened
/// This can be easily translated into a line-drawing activity on a canvas
type MovedEvent = {
    startPos : Position 
    endPos : Position 
    penColor : PenColor option
    }

/// A union of all possible events
type TurtleEvent = 
    | StateChangedEvent of StateChangedEvent
    | MovedEvent of MovedEvent
```

It is an important part of event sourcing that all events are labeled in the past tense: `Moved` and `Turned` rather than `Move` and `Turn`. The event are facts -- they have happened in the past.

### The Command handler

The next step is to define the functions that convert a command into events.

We will need:

* A (private) `applyEvent` function that updates the state from a previous event. 
* A (private) `eventsFromCommand` function that determines what events to generate, based on the command and the state.
* A public `commandHandler` function that handles the command, reads the events from the event store and calls the other two functions.

Here's `applyEvent`. You can see that it is very similar to the `applyCommand` function that we saw in the [previous batch-processing example](/posts/13-ways-of-looking-at-a-turtle/#way9).

```fsharp
/// Apply an event to the current state and return the new state of the turtle
let applyEvent log oldState event =
    match event with
    | Moved distance ->
        Turtle.move log distance oldState 
    | Turned angle ->
        Turtle.turn log angle oldState 
    | PenWentUp ->
        Turtle.penUp log oldState 
    | PenWentDown ->
        Turtle.penDown log oldState 
    | ColorChanged color ->
        Turtle.setColor log color oldState 
```

The `eventsFromCommand` function contains the key logic for validating the command and creating events. 

* In this particular design, the command is always valid, so at least one event is returned.
* The `StateChangedEvent` is created from the `TurtleCommand` in a direct one-to-one map of the cases.
* The `MovedEvent` is only created from the `TurtleCommand` if the turtle has changed position.

```fsharp
// Determine what events to generate, based on the command and the state.
let eventsFromCommand log command stateBeforeCommand =

    // --------------------------
    // create the StateChangedEvent from the TurtleCommand
    let stateChangedEvent = 
        match command.action with
        | Move dist -> Moved dist
        | Turn angle -> Turned angle
        | PenUp -> PenWentUp 
        | PenDown -> PenWentDown 
        | SetColor color -> ColorChanged color

    // --------------------------
    // calculate the current state from the new event
    let stateAfterCommand = 
        applyEvent log stateBeforeCommand stateChangedEvent

    // --------------------------
    // create the MovedEvent 
    let startPos = stateBeforeCommand.position 
    let endPos = stateAfterCommand.position 
    let penColor = 
        if stateBeforeCommand.penState=Down then
            Some stateBeforeCommand.color
        else
            None                        

    let movedEvent = {
        startPos = startPos 
        endPos = endPos 
        penColor = penColor
        }

    // --------------------------
    // return the list of events
    if startPos <> endPos then
        // if the turtle has moved, return both the stateChangedEvent and the movedEvent 
        // lifted into the common TurtleEvent type
        [ StateChangedEvent stateChangedEvent; MovedEvent movedEvent]                
    else
        // if the turtle has not moved, return just the stateChangedEvent 
        [ StateChangedEvent stateChangedEvent]    
```

Finally, the `commandHandler` is the public interface. It is passed in some dependencies as parameters:  a logging function, a function to retrieve the historical events
from the event store, and a function to save the newly generated events into the event store.

```fsharp
/// The type representing a function that gets the StateChangedEvents for a turtle id
/// The oldest events are first
type GetStateChangedEventsForId =
     TurtleId -> StateChangedEvent list

/// The type representing a function that saves a TurtleEvent 
type SaveTurtleEvent = 
    TurtleId -> TurtleEvent -> unit

/// main function : process a command
let commandHandler 
    (log:string -> unit) 
    (getEvents:GetStateChangedEventsForId) 
    (saveEvent:SaveTurtleEvent) 
    (command:TurtleCommand) =

    /// First load all the events from the event store
    let eventHistory = 
        getEvents command.turtleId
    
    /// Then, recreate the state before the command
    let stateBeforeCommand = 
        let nolog = ignore // no logging when recreating state
        eventHistory 
        |> List.fold (applyEvent nolog) Turtle.initialTurtleState
    
    /// Construct the events from the command and the stateBeforeCommand
    /// Do use the supplied logger for this bit
    let events = eventsFromCommand log command stateBeforeCommand 
    
    // store the events in the event store
    events |> List.iter (saveEvent command.turtleId)
```

### Calling the command handler

Now we are ready to send events to the command handler.

First we need some helper functions that create commands:

```fsharp
// Command versions of standard actions   
let turtleId = System.Guid.NewGuid()
let move dist = {turtleId=turtleId; action=Move dist} 
let turn angle = {turtleId=turtleId; action=Turn angle} 
let penDown = {turtleId=turtleId; action=PenDown} 
let penUp = {turtleId=turtleId; action=PenUp} 
let setColor color = {turtleId=turtleId; action=SetColor color} 
```

And then we can draw a figure by sending the various commands to the command handler:

```fsharp
let drawTriangle() = 
    let handler = makeCommandHandler()
    handler (move 100.0)
    handler (turn 120.0<Degrees>)
    handler (move 100.0)
    handler (turn 120.0<Degrees>)
    handler (move 100.0)
    handler (turn 120.0<Degrees>)
```

NOTE: I have not shown how to create the command handler or event store, see the code for full details.

### Advantages and disadvantages of event sourcing

*Advantages*

* All code is stateless, hence easy to test.
* Supports replay of events.

*Disadvantages*

* Can be more complex to implement than a CRUD approach (or at least, less support from tools and libraries).
* If care is not taken, the command handler can get overly complex and evolve into implementing too much business logic.   


*The source code for this version is available [here](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/10-EventSourcing.fsx).*

<hr>

<a id="way11"></a>

## 11: Functional Retroactive Programming (stream processing)

In the event sourcing example above, all the domain logic (in our case, just tracing the state) is embedded in the command handler. One drawback of this is that,
as the application evolves, the logic in the command handler can become very complex.

A way to avoid this is to combine ["functional reactive programming"](https://en.wikipedia.org/wiki/Functional_reactive_programming) with event sourcing
to create a design  where the domain logic is performed on the "read-side", by listening to events ("signals") emitted from the event store.

In this approach, the "write-side" follows the same pattern as the event-sourcing example.
A client sends a `Command` to a `commandHandler`, which converts that to a list of events and stores them in an `EventStore`.

However the `commandHandler` only does the *minimal* amount of work, such as updating state, and does NOT do any complex domain logic.
The complex logic is performed by one or more downstream "processors" (also sometimes called "aggregators") that subscribe to the event stream.

![](/assets/img/turtle-frp.png)

You can even think of these events as "commands" to the processors, and of course, the processors can generate new events for another processor to consume,
so this approach can be extended into an architectural style where an application consists of a set of command handlers linked by an event store.

This techique is often called ["stream processing"](http://www.confluent.io/blog/making-sense-of-stream-processing/).
However, Jessica Kerr once called this approach ["Functional Retroactive Programming"](https://twitter.com/jessitron/status/408554836578537472) -- I like that, so I'm going to steal that name!

![](/assets/img/turtle-stream-processor.png)

### Implementing the design

For this implementation, the `commandHandler` function is the same as in the event sourcing example, except that no work (just logging!) is done at all. The command handler *only* rebuilds state
and generates events. How the events are used for business logic is no longer in its scope.

The new stuff comes in creating the processors.

However, before we can create a processor, we need some helper functions that can filter the event store feed to only include turtle specific events,
and of those only `StateChangedEvent`s or `MovedEvent`s.

```fsharp
// filter to choose only TurtleEvents
let turtleFilter ev = 
    match box ev with
    | :? TurtleEvent as tev -> Some tev
    | _ -> None

// filter to choose only MovedEvents from TurtleEvents
let moveFilter = function 
    | MovedEvent ev -> Some ev
    | _ -> None

// filter to choose only StateChangedEvent from TurtleEvents
let stateChangedEventFilter = function 
    | StateChangedEvent ev -> Some ev
    | _ -> None
```

Now let's create a processor that listens for movement events and moves a physical turtle when the virtual turtle is moved. 

We will make the input to the processor be an `IObservable` -- an event stream -- so that it is not coupled to any specific source such as the `EventStore`.
We will connect the `EventStore` "save" event to this processor when the application is configured.

```fsharp
/// Physically move the turtle
let physicalTurtleProcessor (eventStream:IObservable<Guid*obj>) =

    // the function that handles the input from the observable
    let subscriberFn (ev:MovedEvent) =
        let colorText = 
            match ev.penColor with
            | Some color -> sprintf "line of color %A" color
            | None -> "no line"
        printfn "[turtle  ]: Moved from (%0.2f,%0.2f) to (%0.2f,%0.2f) with %s" 
            ev.startPos.x ev.startPos.y ev.endPos.x ev.endPos.y colorText 

    // start with all events
    eventStream
    // filter the stream on just TurtleEvents
    |> Observable.choose (function (id,ev) -> turtleFilter ev)
    // filter on just MovedEvents
    |> Observable.choose moveFilter
    // handle these
    |> Observable.subscribe subscriberFn
```

In this case we are just printing the movement -- I'll leave the building of an [actual Lego Mindstorms turtle](https://www.youtube.com/watch?v=pcJHLClDKVw) as an exercise for the reader!

Let's also create a processor that draws lines on a graphics display:

```fsharp
/// Draw lines on a graphics device
let graphicsProcessor (eventStream:IObservable<Guid*obj>) =

    // the function that handles the input from the observable
    let subscriberFn (ev:MovedEvent) =
        match ev.penColor with
        | Some color -> 
            printfn "[graphics]: Draw line from (%0.2f,%0.2f) to (%0.2f,%0.2f) with color %A" 
                ev.startPos.x ev.startPos.y ev.endPos.x ev.endPos.y color
        | None -> 
            ()  // do nothing

    // start with all events
    eventStream
    // filter the stream on just TurtleEvents
    |> Observable.choose (function (id,ev) -> turtleFilter ev)
    // filter on just MovedEvents
    |> Observable.choose moveFilter
    // handle these
    |> Observable.subscribe subscriberFn 
```
       
And finally, let's create a processor that accumulates the total distance moved so that we can keep track of how much ink has been used, say. 

```fsharp
/// Listen for "moved" events and aggregate them to keep
/// track of the total ink used
let inkUsedProcessor (eventStream:IObservable<Guid*obj>) =

    // Accumulate the total distance moved so far when a new event happens
    let accumulate distanceSoFar (ev:StateChangedEvent) =
        match ev with
        | Moved dist -> 
            distanceSoFar + dist 
        | _ -> 
            distanceSoFar 

    // the function that handles the input from the observable
    let subscriberFn distanceSoFar  =
        printfn "[ink used]: %0.2f" distanceSoFar  

    // start with all events
    eventStream
    // filter the stream on just TurtleEvents
    |> Observable.choose (function (id,ev) -> turtleFilter ev)
    // filter on just StateChangedEvent
    |> Observable.choose stateChangedEventFilter
    // accumulate total distance
    |> Observable.scan accumulate 0.0
    // handle these
    |> Observable.subscribe subscriberFn 
```

This processor uses `Observable.scan` to accumulate the events into a single value -- the total distance travelled.

### Processors in practice

Let's try these out!

For example, here is `drawTriangle`: 

```fsharp
let drawTriangle() = 
    // clear older events
    eventStore.Clear turtleId   

    // create an event stream from an IEvent
    let eventStream = eventStore.SaveEvent :> IObservable<Guid*obj>

    // register the processors
    use physicalTurtleProcessor = EventProcessors.physicalTurtleProcessor eventStream 
    use graphicsProcessor = EventProcessors.graphicsProcessor eventStream 
    use inkUsedProcessor = EventProcessors.inkUsedProcessor eventStream 

    let handler = makeCommandHandler
    handler (move 100.0)
    handler (turn 120.0<Degrees>)
    handler (move 100.0)
    handler (turn 120.0<Degrees>)
    handler (move 100.0)
    handler (turn 120.0<Degrees>)
```

Note that `eventStore.SaveEvent` is cast into an `IObservable<Guid*obj>` (that is, an event stream) before being passed to the processors as a parameter.

`drawTriangle` generates this output:

```text
[ink used]: 100.00
[turtle  ]: Moved from (0.00,0.00) to (100.00,0.00) with line of color Black
[graphics]: Draw line from (0.00,0.00) to (100.00,0.00) with color Black
[ink used]: 100.00
[ink used]: 200.00
[turtle  ]: Moved from (100.00,0.00) to (50.00,86.60) with line of color Black
[graphics]: Draw line from (100.00,0.00) to (50.00,86.60) with color Black
[ink used]: 200.00
[ink used]: 300.00
[turtle  ]: Moved from (50.00,86.60) to (0.00,0.00) with line of color Black
[graphics]: Draw line from (50.00,86.60) to (0.00,0.00) with color Black
[ink used]: 300.00
```

You can see that all the processors are handling events successfully.

The turtle is moving, the graphics processor is drawing lines, and the ink used processor has correctly calculated the total distance moved as 300 units.

Note, though, that the ink used processor is emitting output on *every* state change (such as turning), rather than only when actual movement happens.

We can fix this by putting a pair `(previousDistance, currentDistance)` in the stream, and then filtering out those events where the values are the same.

Here's the new `inkUsedProcessor` code, with the following changes:

* The `accumulate` function now emits a pair.
* There is a new filter `changedDistanceOnly`.

```fsharp
/// Listen for "moved" events and aggregate them to keep
/// track of the total distance moved
/// NEW! No duplicate events! 
let inkUsedProcessor (eventStream:IObservable<Guid*obj>) =

    // Accumulate the total distance moved so far when a new event happens
    let accumulate (prevDist,currDist) (ev:StateChangedEvent) =
        let newDist =
            match ev with
            | Moved dist -> 
                currDist + dist
            | _ -> 
                currDist
        (currDist, newDist)

    // convert unchanged events to None so they can be filtered out with "choose"
    let changedDistanceOnly (currDist, newDist) =
        if currDist <> newDist then 
            Some newDist 
        else 
            None

    // the function that handles the input from the observable
    let subscriberFn distanceSoFar  =
        printfn "[ink used]: %0.2f" distanceSoFar  

    // start with all events
    eventStream
    // filter the stream on just TurtleEvents
    |> Observable.choose (function (id,ev) -> turtleFilter ev)
    // filter on just StateChangedEvent
    |> Observable.choose stateChangedEventFilter
    // NEW! accumulate total distance as pairs
    |> Observable.scan accumulate (0.0,0.0)   
    // NEW! filter out when distance has not changed
    |> Observable.choose changedDistanceOnly
    // handle these
    |> Observable.subscribe subscriberFn 
```

With these changes, the output of `drawTriangle` looks like this:

```text
[ink used]: 100.00
[turtle  ]: Moved from (0.00,0.00) to (100.00,0.00) with line of color Black
[graphics]: Draw line from (0.00,0.00) to (100.00,0.00) with color Black
[ink used]: 200.00
[turtle  ]: Moved from (100.00,0.00) to (50.00,86.60) with line of color Black
[graphics]: Draw line from (100.00,0.00) to (50.00,86.60) with color Black
[ink used]: 300.00
[turtle  ]: Moved from (50.00,86.60) to (0.00,0.00) with line of color Black
[graphics]: Draw line from (50.00,86.60) to (0.00,0.00) with color Black
```

and there are no longer any duplicate messages from the `inkUsedProcessor`.

### Advantages and disadvantages of stream processing

*Advantages*

* Same advantages as event-sourcing.
* Decouples stateful logic from other non-intrinsic logic.
* Easy to add and remove domain logic without affecting the core command handler.

*Disadvantages*

* More complex to implement.

*The source code for this version is available [here](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/11-FRP.fsx).*

<a id="strikes-back"></a>

<hr>

## Episode V: The Turtle Strikes Back

So far, we have not had to make decisions based on the turtle's state. So, for the two final approaches, we will change the
turtle API so that some commands may fail.

For example, we might say that the turtle must move within a limited arena, and a `move` instruction may cause the turtle to hit the barrier.
In this case, the `move` instruction can return a choice of `MovedOk` or `HitBarrier`.

Or let's say that there is only a limited amount of colored ink. In this case, trying to set the color may return an "out of ink" response.

So let's update the turtle functions with these cases. First the new response types for `move` and `setColor`:

```fsharp
type MoveResponse = 
    | MoveOk 
    | HitABarrier

type SetColorResponse = 
    | ColorOk
    | OutOfInk
```

We will need a bounds checker to see if the turtle is in the arena.
Say that if the position tries to go outside the square (0,0,100,100), the response is `HitABarrier`:

```fsharp
// if the position is outside the square (0,0,100,100) 
// then constrain the position and return HitABarrier
let checkPosition position =
    let isOutOfBounds p = 
        p > 100.0 || p < 0.0
    let bringInsideBounds p = 
        max (min p 100.0) 0.0

    if isOutOfBounds position.x || isOutOfBounds position.y then
        let newPos = {
            x = bringInsideBounds position.x 
            y = bringInsideBounds position.y }
        HitABarrier,newPos
    else
        MoveOk,position
```

And finally, the `move` function needs an extra line to check the new position:

```fsharp
let move log distance state =
    let newPosition = ...
    
    // adjust the new position if out of bounds
    let moveResult, newPosition = checkPosition newPosition 
    
    ...
```

Here's the complete `move` function:

```fsharp
let move log distance state =
    log (sprintf "Move %0.1f" distance)
    // calculate new position 
    let newPosition = calcNewPosition distance state.angle state.position 
    // adjust the new position if out of bounds
    let moveResult, newPosition = checkPosition newPosition 
    // draw line if needed
    if state.penState = Down then
        dummyDrawLine log state.position newPosition state.color
    // return the new state and the Move result
    let newState = {state with position = newPosition}
    (moveResult,newState) 
```

We will make similar changes for the `setColor` function too, returning `OutOfInk` if we attempt to set the color to `Red`.

```fsharp
let setColor log color state =
    let colorResult = 
        if color = Red then OutOfInk else ColorOk
    log (sprintf "SetColor %A" color)
    // return the new state and the SetColor result
    let newState = {state with color = color}
    (colorResult,newState) 
```

With the new versions of the turtle functions available, we have to create implementations that can respond to the error cases. That will be done in the next two examples.
        
*The source code for the new turtle functions is available [here](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/FPTurtleLib2.fsx).*
        
<hr>

<a id="way12"></a>

## 12: Monadic control flow

In this approach, we will reuse the `turtle` workflow from [way 8](/posts/13-ways-of-looking-at-a-turtle/#way8).
This time though, we will make decisions for the next command based on the result of the previous one.

Before we do that though, let's look at what effect the change to `move` will have on our code.  Let's say that we want to move forwards a few times using `move 40.0`, say.   

If we write the code using `do!` as we did before, we get a nasty compiler error:

```fsharp
let drawShape() = 
    // define a set of instructions 
    let t = turtle {
        do! move 60.0   
        // error FS0001: 
        // This expression was expected to have type
        //    Turtle.MoveResponse    
        // but here has type
        //     unit    
        do! move 60.0 
        } 
    // etc                
```

Instead, we need to use `let!` and assign the response to something.

In the following code, we assign the response to a value and then ignore it!  

```fsharp
let drawShapeWithoutResponding() = 
    // define a set of instructions 
    let t = turtle {
        let! response = move 60.0 
        let! response = move 60.0 
        let! response = move 60.0 
        return ()
        } 

    // finally, run the monad using the initial state
    runT t initialTurtleState 
```

The code does compile and work, but if we run it the output shows that, by the third call, we are banging our turtle against the wall (at 100,0) and not moving anywhere.

```text
Move 60.0
...Draw line from (0.0,0.0) to (60.0,0.0) using Black
Move 60.0
...Draw line from (60.0,0.0) to (100.0,0.0) using Black
Move 60.0
...Draw line from (100.0,0.0) to (100.0,0.0) using Black
```

### Making decisions based on a response

Let's say that our response to a `move` that returns `HitABarrier` is to turn 90 degrees and wait for the next command. Not the cleverest algorithm, but it will do for demonstration purposes!

Let's design a function to implement this. The input will be a `MoveResponse`, but what will the output be?  We want to encode the `turn` action somehow, but the raw `turn` function needs
state input that we don't have.  So instead let's return a `turtle` workflow that represents the instruction we *want* to do, when the state becomes available (in the `run` command).

So here is the code:

```fsharp
let handleMoveResponse moveResponse = turtle {
    match moveResponse with
    | Turtle.MoveOk -> 
        () // do nothing
    | Turtle.HitABarrier ->
        // turn 90 before trying again
        printfn "Oops -- hit a barrier -- turning"
        do! turn 90.0<Degrees>
    }
```

The type signature looks like this:

```fsharp
val handleMoveResponse : MoveResponse -> TurtleStateComputation<unit>
```

which means that it is a monadic (or "diagonal") function -- one that starts in the normal world and ends in the `TurtleStateComputation` world.

These are exactly the functions that we can use "bind" with, or within computation expressions, `let!` or `do!`.

Now we can add this `handleMoveResponse` step after `move` in the turtle workflow:

```fsharp
let drawShape() = 
    // define a set of instructions 
    let t = turtle {
        let! response = move 60.0 
        do! handleMoveResponse response 

        let! response = move 60.0 
        do! handleMoveResponse response 

        let! response = move 60.0 
        do! handleMoveResponse response 
        } 

    // finally, run the monad using the initial state
    runT t initialTurtleState 
```

And the result of running it is:

```text
Move 60.0
...Draw line from (0.0,0.0) to (60.0,0.0) using Black
Move 60.0
...Draw line from (60.0,0.0) to (100.0,0.0) using Black
Oops -- hit a barrier -- turning
Turn 90.0
Move 60.0
...Draw line from (100.0,0.0) to (100.0,60.0) using Black
```

You can see that the move response worked. When the turtle hit the edge at (100,0) it turned 90 degrees and the next move succeeded (from (100,0) to (100,60)).

So there you go! This code demonstrates how you can make decisions inside the `turtle` workflow while the state is being passed around behind the scenes.

### Advantages and disadvantages

*Advantages*

* Computation expressions allow the code to focus on the logic while taking care of the "plumbing" -- in this case, the turtle state.

*Disadvantages*

* Still coupled to a particular implementation of the turtle functions.
* Computation expressions can be complex to implement and how they work is not obvious for beginners.

*The source code for this version is available [here](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/12-BranchingOnResponse.fsx).*

<hr>

<a id="way13"></a>

## 13: A turtle interpreter 

For our final approach, we'll look at a way to *completely* decouple the programming of the turtle from its interpretation.  

This is similar to the [batch processing using command objects](/posts/13-ways-of-looking-at-a-turtle/#way9) approach,
but is enhanced to support responding to the output of a command.

### Designing an interpreter

The approach we will take is to design an "interpreter" for a set of turtle commands, where the client provides the commands to the turtle,
and responds to outputs from the turtle, but the actual turtle functions are provided later by a particular implementation.

In other words, we have a chain of interleaved commands and turtle functions that look like this:

![](/assets/img/turtle-interpreter-chain.png)

So how can we model this design in code?

For a first attempt, let's model the chain as a sequence of request/response pairs. We send a command to the turtle and it responds appropriately with
a `MoveResponse` or whatever, like this:

```fsharp
// we send this to the turtle...
type TurtleCommand = 
    | Move of Distance 
    | Turn of Angle
    | PenUp
    | PenDown
    | SetColor of PenColor

// ... and the turtle replies with one of these
type TurtleResponse = 
    | Moved of MoveResponse
    | Turned 
    | PenWentUp
    | PenWentDown
    | ColorSet of SetColorResponse
```

The problem is that we cannot be sure that the response correctly matches the command.  For example, if I send a `Move` command, I expect to get a `MoveResponse`, and never
a `SetColorResponse`. But this implementation doesn't enforce that!

We want to [make illegal states unrepresentable](/posts/designing-with-types-making-illegal-states-unrepresentable/) -- how can we do that?

The trick is to combine the request and response in *pairs*. That is, for a `Move` command, there is an associated function which is given a `MoveResponse` as input, and similarly for each other combination.
Commands that have no response can be considered as returning `unit` for now.

```fsharp
Move command => pair of (Move command parameters), (function MoveResponse -> something)
Turn command => pair of (Turn command parameters), (function unit -> something)
etc
```

The way this works is that:

* The client creates a command, say `Move 100`, and also provides the additional function that handles the response.
* The turtle implementation for the Move command (inside the interpreter) processes the input (a `Distance`) and then generates a `MoveResponse`.
* The interpreter then takes this `MoveResponse` and calls the associated function in the pair, as supplied by the client.

By associating the `Move` command with a function in this way, we can *guarantee* that the internal turtle implementation must accept a `distance` and return a `MoveResponse`, just as we want.

The next question is: what is the `something` that is the output?  It is the output after the client has handled the response -- that is, another command/response chain!

So we can model the whole chain of pairs as a recursive structure:

![](/assets/img/turtle-interpreter-nested.png)

Or in code:

```fsharp
type TurtleProgram = 
    //         (input params)  (response)
    | Move     of Distance   * (MoveResponse -> TurtleProgram)
    | Turn     of Angle      * (unit -> TurtleProgram)
    | PenUp    of (* none *)   (unit -> TurtleProgram)
    | PenDown  of (* none *)   (unit -> TurtleProgram)
    | SetColor of PenColor   * (SetColorResponse -> TurtleProgram)
```

I've renamed the type from `TurtleCommand` to `TurtleProgram` because it is no longer just a command, but is now a complete chain of commands and associated response handlers.

There's a problem though! Every step needs yet another `TurtleProgram` to follow -- so when will it stop?  We need some way of saying that there is no next command.

To solve this issue, we will add a special `Stop` case to the program type:

```fsharp
type TurtleProgram = 
    //         (input params)  (response)
    | Stop
    | Move     of Distance   * (MoveResponse -> TurtleProgram)
    | Turn     of Angle      * (unit -> TurtleProgram)
    | PenUp    of (* none *)   (unit -> TurtleProgram)
    | PenDown  of (* none *)   (unit -> TurtleProgram)
    | SetColor of PenColor   * (SetColorResponse -> TurtleProgram)
```

Note that there is no mention of `TurtleState` in this structure. How the turtle state is managed is internal to the interpreter, and is not part of the "instruction set", as it were.

`TurtleProgram` is an example of an Abstract Syntax Tree (AST) -- a structure that represents a program to interpreted (or compiled). 

### Testing the interpreter

Let's create a little program using this model. Here's our old friend `drawTriangle`:

```fsharp
let drawTriangle = 
    Move (100.0, fun response -> 
    Turn (120.0<Degrees>, fun () -> 
    Move (100.0, fun response -> 
    Turn (120.0<Degrees>, fun () -> 
    Move (100.0, fun response -> 
    Turn (120.0<Degrees>, fun () -> 
    Stop))))))
```

This program is a data structure containing only client commands and responses -- there are no actual turtle functions in it anywhere!
And yes, it is really ugly right now, but we will fix that shortly.

Now the next step is to interpret this data structure.

Let's create an interpreter that calls the real turtle functions. How would we implement the `Move` case, say?

Well, just as described above:

* Get the distance and associated function from the `Move` case
* Call the real turtle function with the distance and current turtle state, to get a `MoveResult` and a new turtle state.
* Get the next step in the program by passing the `MoveResult` to the associated function
* Finally call the interpreter again (recursively) with the new program and new turtle state.

```fsharp
let rec interpretAsTurtle state program =
    ...
    match program  with
    | Move (dist,next) ->
        let result,newState = Turtle.move log dist state 
        let nextProgram = next result  // compute the next step
        interpretAsTurtle newState nextProgram 
    ...        
```

You can see that the updated turtle state is passed as a parameter to the next recursive call, and so no mutable field is needed.

Here's the full code for `interpretAsTurtle`:

```fsharp
let rec interpretAsTurtle state program =
    let log = printfn "%s"

    match program  with
    | Stop -> 
        state
    | Move (dist,next) ->
        let result,newState = Turtle.move log dist state 
        let nextProgram = next result  // compute the next step 
        interpretAsTurtle newState nextProgram 
    | Turn (angle,next) ->
        let newState = Turtle.turn log angle state 
        let nextProgram = next()       // compute the next step
        interpretAsTurtle newState nextProgram 
    | PenUp next ->
        let newState = Turtle.penUp log state 
        let nextProgram = next()
        interpretAsTurtle newState nextProgram 
    | PenDown next -> 
        let newState = Turtle.penDown log state 
        let nextProgram = next()
        interpretAsTurtle newState nextProgram 
    | SetColor (color,next) ->
        let result,newState = Turtle.setColor log color state 
        let nextProgram = next result
        interpretAsTurtle newState nextProgram 
```

Let's run it:

```fsharp
let program = drawTriangle
let interpret = interpretAsTurtle   // choose an interpreter 
let initialState = Turtle.initialTurtleState
interpret initialState program |> ignore
```

and the output is exactly what we have seen before:

```text
Move 100.0
...Draw line from (0.0,0.0) to (100.0,0.0) using Black
Turn 120.0
Move 100.0
...Draw line from (100.0,0.0) to (50.0,86.6) using Black
Turn 120.0
Move 100.0
...Draw line from (50.0,86.6) to (0.0,0.0) using Black
Turn 120.0
```

But unlike all the previous approaches we can take *exactly the same program* and interpret it in a new way.
We don't need to set up any kind of dependency injection, we just need to use a different interpreter.

So let's create another interpreter that aggregates the distance travelled, without caring about the turtle state:

```fsharp
let rec interpretAsDistance distanceSoFar program =
    let recurse = interpretAsDistance 
    let log = printfn "%s"
    
    match program with
    | Stop -> 
        distanceSoFar
    | Move (dist,next) ->
        let newDistanceSoFar = distanceSoFar + dist
        let result = Turtle.MoveOk   // hard-code result
        let nextProgram = next result 
        recurse newDistanceSoFar nextProgram 
    | Turn (angle,next) ->
        // no change in distanceSoFar
        let nextProgram = next()
        recurse distanceSoFar nextProgram 
    | PenUp next ->
        // no change in distanceSoFar
        let nextProgram = next()
        recurse distanceSoFar nextProgram 
    | PenDown next -> 
        // no change in distanceSoFar
        let nextProgram = next()
        recurse distanceSoFar nextProgram 
    | SetColor (color,next) ->
        // no change in distanceSoFar
        let result = Turtle.ColorOk   // hard-code result
        let nextProgram = next result
        recurse distanceSoFar nextProgram 
```

In this case, I've aliased `interpretAsDistance` as `recurse` locally to make it obvious what kind of recursion is happening.

Let's run the same program with this new interpreter:

```fsharp
let program = drawTriangle           // same program  
let interpret = interpretAsDistance  // choose an interpreter 
let initialState = 0.0
interpret initialState program |> printfn "Total distance moved is %0.1f"
```

and the output is again exactly what we expect:

```text
Total distance moved is 300.0
```

### Creating a "turtle program" workflow

That code for creating a program to interpret was pretty ugly! Can we create a computation expression to make it look nicer?

Well, in order to create a computation expression, we need `return` and `bind` functions, and those require that the
`TurtleProgram` type be generic.

No problem! Let's make `TurtleProgram` generic then:

```fsharp
type TurtleProgram<'a> = 
    | Stop     of 'a
    | Move     of Distance * (MoveResponse -> TurtleProgram<'a>)
    | Turn     of Angle    * (unit -> TurtleProgram<'a>)
    | PenUp    of            (unit -> TurtleProgram<'a>)
    | PenDown  of            (unit -> TurtleProgram<'a>)
    | SetColor of PenColor * (SetColorResponse -> TurtleProgram<'a>)
```

Note that the `Stop` case has a value of type `'a` associated with it now.  This is needed so that we can implement `return` properly:

```fsharp
let returnT x = 
    Stop x  
```

The `bind` function is more complicated to implement. Don't worry about how it works right now -- the important thing is that the types match up and it compiles!

```fsharp
let rec bindT f inst  = 
    match inst with
    | Stop x -> 
        f x
    | Move(dist,next) -> 
        (*
        Move(dist,fun moveResponse -> (bindT f)(next moveResponse)) 
        *)
        // "next >> bindT f" is a shorter version of function response
        Move(dist,next >> bindT f) 
    | Turn(angle,next) -> 
        Turn(angle,next >> bindT f)  
    | PenUp(next) -> 
        PenUp(next >> bindT f)
    | PenDown(next) -> 
        PenDown(next >> bindT f)
    | SetColor(color,next) -> 
        SetColor(color,next >> bindT f)
```

With `bind` and `return` in place, we can create a computation expression:

```fsharp
// define a computation expression builder
type TurtleProgramBuilder() =
    member this.Return(x) = returnT x
    member this.Bind(x,f) = bindT f x
    member this.Zero(x) = returnT ()

// create an instance of the computation expression builder
let turtleProgram = TurtleProgramBuilder()
```

We can now create a workflow that handles `MoveResponse`s just as in the monadic control flow example (way 12) earlier.

```fsharp
// helper functions
let stop = fun x -> Stop x
let move dist  = Move (dist, stop)
let turn angle  = Turn (angle, stop)
let penUp  = PenUp stop 
let penDown  = PenDown stop 
let setColor color = SetColor (color,stop)

let handleMoveResponse log moveResponse = turtleProgram {
    match moveResponse with
    | Turtle.MoveOk -> 
        ()
    | Turtle.HitABarrier ->
        // turn 90 before trying again
        log "Oops -- hit a barrier -- turning"
        let! x = turn 90.0<Degrees>
        ()
    }

// example
let drawTwoLines log = turtleProgram {
    let! response = move 60.0
    do! handleMoveResponse log response 
    let! response = move 60.0
    do! handleMoveResponse log response 
    }
```

Let's interpret this using the real turtle functions (assuming that the `interpretAsTurtle` function has been modified to handle the new generic structure):
    
```fsharp
let log = printfn "%s"
let program = drawTwoLines log 
let interpret = interpretAsTurtle 
let initialState = Turtle.initialTurtleState
interpret initialState program |> ignore
```

The output shows that the `MoveResponse` is indeed being handled correctly when the barrier is encountered:

```text
Move 60.0
...Draw line from (0.0,0.0) to (60.0,0.0) using Black
Move 60.0
...Draw line from (60.0,0.0) to (100.0,0.0) using Black
Oops -- hit a barrier -- turning
Turn 90.0
```

### Refactoring the `TurtleProgram` type into two parts

This approach works fine, but it bothers me that there is a special `Stop` case in the `TurtleProgram` type. It would nice if we could somehow
just focus on the five turtle actions and ignore it.

As it turns out, there *is* a way to do this.  In Haskell and Scalaz it would be called a "free monad", but since F# doesn't support typeclasses,
I'll just call it the "free monad pattern" that you can use to solve the problem. There's a little bit of boilerplate that
you have to write, but not much.

The trick is to separate the api cases and "stop"/"keep going" logic into two separate types, like this: 

```fsharp
/// Create a type to represent each instruction
type TurtleInstruction<'next> = 
    | Move     of Distance * (MoveResponse -> 'next)
    | Turn     of Angle    * 'next
    | PenUp    of            'next
    | PenDown  of            'next
    | SetColor of PenColor * (SetColorResponse -> 'next)

/// Create a type to represent the Turtle Program
type TurtleProgram<'a> = 
    | Stop of 'a
    | KeepGoing of TurtleInstruction<TurtleProgram<'a>>
```

Note that I've also changed the responses for `Turn`, `PenUp` and `PenDown` to be single values rather than a unit function. `Move` and `SetColor` remain as functions though.

In this new "free monad" approach, the only custom code we need to write is a simple `map` function for the api type, in this case `TurtleInstruction`:

```fsharp
let mapInstr f inst  = 
    match inst with
    | Move(dist,next) ->      Move(dist,next >> f) 
    | Turn(angle,next) ->     Turn(angle,f next)  
    | PenUp(next) ->          PenUp(f next)
    | PenDown(next) ->        PenDown(f next)
    | SetColor(color,next) -> SetColor(color,next >> f)
```

The rest of the code (`return`, `bind`, and the computation expression) is
[always implemented exactly the same way](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/4a8cdf3bda9fc9db030842e99f78487aea928e57/13-Interpreter-v2.fsx#L67), regardless of the particular api.
That is, more boilerplate is needed but less thinking is required!

The interpreters need to change in order to handle the new cases. Here's a snippet of the new version of `interpretAsTurtle`:

```fsharp
let rec interpretAsTurtle log state program =
    let recurse = interpretAsTurtle log 
    
    match program with
    | Stop a -> 
        state
    | KeepGoing (Move (dist,next)) ->
        let result,newState = Turtle.move log dist state 
        let nextProgram = next result // compute next program
        recurse newState nextProgram 
    | KeepGoing (Turn (angle,next)) ->
        let newState = Turtle.turn log angle state 
        let nextProgram = next        // use next program directly
        recurse newState nextProgram 
```

And we also need to adjust the helper functions when creating a workflow. You can see below that we now have slightly
more complicated code like `KeepGoing (Move (dist, Stop))` instead of the simpler code in the original interpreter.

```fsharp
// helper functions
let stop = Stop()
let move dist  = KeepGoing (Move (dist, Stop))    // "Stop" is a function
let turn angle  = KeepGoing (Turn (angle, stop))  // "stop" is a value
let penUp  = KeepGoing (PenUp stop)
let penDown  = KeepGoing (PenDown stop)
let setColor color = KeepGoing (SetColor (color,Stop))

let handleMoveResponse log moveResponse = turtleProgram {
    ... // as before

// example
let drawTwoLines log = turtleProgram {
    let! response = move 60.0
    do! handleMoveResponse log response 
    let! response = move 60.0
    do! handleMoveResponse log response 
    }
```

But with those changes, we are done, and the code works just as before.

### Advantages and disadvantages of the interpreter pattern

*Advantages*

* *Decoupling.* An abstract syntax tree completely decouples the program flow from the implementation and allows lots of flexibility.
* *Optimization*. Abstract syntax trees can be manipulated and changed *before* running them, in order to do optimizations or other transformations. As an example, for the turtle program,
  we could process the tree and collapse all contiguous sequences of `Turn` into a single `Turn` operation.
  This is a simple optimization which saves on the number of times we need to communicate with a physical turtle. [Twitter's Stitch library](https://engineering.twitter.com/university/videos/introducing-stitch)
  does something like this, but obviously, in a more sophisticated way. [This video has a good explanation](https://www.youtube.com/watch?v=VVpmMfT8aYw&feature=youtu.be&t=625).
* *Minimal code for a lot of power*. The "free monad" approach to creating abstract syntax trees allows you to focus on the API and ignore the Stop/KeepGoing logic, and also means that only a minimal amount of code needs to be customized.
  For more on the free monad, start with [this excellent video](https://www.youtube.com/watch?v=hmX2s3pe_qk) and then see [this post](http://underscore.io/blog/posts/2015/04/14/free-monads-are-simple.html)
  and [this one](http://www.haskellforall.com/2012/06/you-could-have-invented-free-monads.html).

*Disadvantages*

* Complex to understand.
* Only works well if there are a limited set of operations to perform.
* Can be inefficient if the ASTs get too large.

*The source code for this version is available [here (original version)](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/13-Interpreter-v1.fsx)
and [here ("free monad" version)](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/13-Interpreter-v2.fsx).*

<hr>

<a id="review"></a>

## Review of techniques used

In this post, we looked at thirteen different ways to implement a turtle API, using a wide variety of different techniques.  Let's quickly run down all the techniques that were used:

* **Pure, stateless functions**. As seen in all of the FP-oriented examples. All these are very easy to test and mock.
* **Partial application**. As first seen in [the simplest FP example (way 2)](/posts/13-ways-of-looking-at-a-turtle/#way2), when the turtle functions had the logging function applied so that the main flow could use piping,
  and thereafter used extensively, particularly in the ["dependency injection using functions approach" (way 7)](/posts/13-ways-of-looking-at-a-turtle/#way7).
* **Object expressions** to implement an interface without creating a class, as seen in [way 6](/posts/13-ways-of-looking-at-a-turtle/#way6).
* **The Result type** (a.k.a the Either monad). Used in all the functional API examples ([e.g. way 4](/posts/13-ways-of-looking-at-a-turtle/#way4)) to return an error rather than throw an exception. 
* **Applicative "lifting"** (e.g. `lift2`) to lift normal functions to the world of `Result`s, again [in way 4](/posts/13-ways-of-looking-at-a-turtle/#way4) and others.
* **Lots of different ways of managing state**:
  * mutable fields (way 1)
  * managing state explicitly and piping it though a series of functions (way 2)
  * having state only at the edge (the functional core/imperative shell in way 4)
  * hiding state in an agent (way 5)
  * threading state behind the scenes in a state monad (the `turtle` workflow in ways 8 and 12)
  * avoiding state altogether by using batches of commands (way 9) or batches of events (way 10) or an interpreter (way 13)
* **Wrapping a function in a type**. Used in [way 8](/posts/13-ways-of-looking-at-a-turtle/#way8) to manage state (the State monad) and in [way 13](/posts/13-ways-of-looking-at-a-turtle/#way13) to store responses.
* **Computation expressions**, lots of them! We created and used three:
  * `result` for working with errors
  * `turtle` for managing turtle state
  * `turtleProgram` for building an AST in the interpreter approach ([way 13](/posts/13-ways-of-looking-at-a-turtle-2/#way13)).
* **Chaining of monadic functions** in the `result` and `turtle` workflows. The underlying functions are monadic ("diagonal") and would not normally compose properly,
  but inside a workflow, they can be sequenced easily and transparently.
* **Representing behavior as a data structure** in the ["functional dependency injection" example (way 7)](/posts/13-ways-of-looking-at-a-turtle/#way7) so that a single function could be passed in rather than a whole interface.
* **Decoupling using a data-centric protocol** as seen in the agent, batch command, event sourcing, and interpreter examples.
* **Lock free and async processing** using an agent (way 5).
* **The separation of "building" a computation vs. "running" it**, as seen in the `turtle` workflows (ways 8 and 12) and the `turtleProgram` workflow (way 13: interpreter).
* **Use of event sourcing to rebuild state** from scratch rather than maintaining mutable state in memory, as seen in the [event sourcing (way 10)](/posts/13-ways-of-looking-at-a-turtle-2/#way10)
   and [FRP (way 11)](/posts/13-ways-of-looking-at-a-turtle-2/#way11) examples.
* **Use of event streams** and [FRP (way 11)](/posts/13-ways-of-looking-at-a-turtle-2/#way11) to break business logic into small, independent, and decoupled processors rather than having a monolithic object.

I hope it's clear that examining these thirteen ways is just a fun exercise, and I'm not suggesting that you immediately convert all your code to use stream processors and interpreters! And, especially
if you are working with people who are new to functional programming, I would tend to stick with the earlier (and simpler) approaches unless there is a clear benefit in exchange for the extra complexity.

<hr>

## Summary

> When the tortoise crawled out of sight,   
> It marked the edge   
> Of one of many circles.   
> -- *"Thirteen ways of looking at a turtle", by Wallace D Coriacea*

I hope you enjoyed this post. I certainly enjoyed writing it. As usual, it ended up much longer than I intended, so I hope that the effort of reading it was worth it to you!

If you like this kind of comparative approach, and want more, check out [the posts by Yan Cui, who is doing something similar](http://theburningmonk.com/fsharp-exercises-in-programming-style/) on his blog.

Enjoy the rest of the [F# Advent Calendar](https://sergeytihon.wordpress.com/2015/10/25/f-advent-calendar-in-english-2015/). Happy Holidays!  
  
*The source code for this post is available [on github](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle).*



