---
layout: post
title: "Thirteen ways of looking at a turtle - addendum"
description: "Bonus ways: An Abstract Data Turtle and a Capability-based Turtle."
categories: [Patterns]
---

*UPDATE: [Slides and video from my talk on this topic](/turtle/)*

In this, the third part of my two-part mega-post, I'm continuing to stretch the simple turtle graphics model to the breaking point.

In the [first](/posts/13-ways-of-looking-at-a-turtle/) and [second post](/posts/13-ways-of-looking-at-a-turtle-2/),
I described thirteen different ways of looking at a turtle graphics implementation.

Unfortunately, after I published them, I realized that there were some other ways that I had forgotten to mention. 
So in this post, you'll get to see two BONUS ways.

* [Way 14. Abstract Data Turtle](/posts/13-ways-of-looking-at-a-turtle-3/#way14), in which we encapsulate the details of a turtle implementation by using an Abstract Data Type.
* [Way 15. Capability-based Turtle](/posts/13-ways-of-looking-at-a-turtle-3/#way15), in which we control what turtle functions are available to a client, based on the current
  state of the turtle.

As a reminder, here were the previous thirteen ways:

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

All source code for this post is available [on github](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle).

<hr>

<a id="way14"></a>

## 14: Abstract Data Turtle

In this design, we use the concept of an [abstract data type](https://en.wikipedia.org/wiki/Abstract_data_type) to encapsulate the operations on a turtle.

That is, a "turtle" is defined as an opaque type along with a corresponding set of operations, in the same way that standard F# types such as `List`, `Set` and `Map` are defined.

That is, we have number of functions that work on the type, but we are not allowed to see "inside" the type itself.

In a sense, you can think of it as a third alternative to the [OO approach in way 1](/posts/13-ways-of-looking-at-a-turtle/#way1) and the [functional approach in way 2](/posts/13-ways-of-looking-at-a-turtle/#way2).

* In the OO implementation, the details of the internals are nicely encapsulated, and access is only via methods. The downside of the OO class is that it is mutable.
* In the FP implementation, the `TurtleState` is immutable, but the downside is that the internals of the state are public, and some clients may have accessed these fields,
  so if we ever change the design of `TurtleState`, these clients may break.

The abstract data type implementation combines the best of both worlds: the turtle state is immutable, as in the original FP way, but no client can access it, as in the OO way.

The design for this (and for any abstract type) is as follows:

* The turtle state type itself is public, but its constructor and fields are private.
* The functions in the associated `Turtle` module can see inside the turtle state type (and so are unchanged from the FP design).
* Because the turtle state constructor is private, we need a constructor function in the `Turtle` module.
* The client can *not* see inside the turtle state type, and so must rely entirely on the `Turtle` module functions.

That's all there is to it. We only need to add some privacy modifiers to the earlier FP version and we are done!

### The implementation

First, we are going to put both the turtle state type and the `Turtle` module inside a common module called `AdtTurtle`.
This allows the turtle state to be accessible to the functions in the `AdtTurtle.Turtle` module, while being inaccessible outside the `AdtTurtle`.

Next, the turtle state type is going to be called `Turtle` now, rather than `TurtleState`, because we are treating it almost as an object.

Finally, the associated module `Turtle` (that contains the functions) is going have some special attributes:

* `RequireQualifiedAccess` means the module name *must* be used when accessing the functions (just like `List` module)
* `ModuleSuffix` is needed so the that module can have the same name as the state type. This would not be required for generic types (e.g if we had `Turtle<'a>` instead).

```fsharp
module AdtTurtle = 

    /// A private structure representing the turtle 
    type Turtle = private {
        position : Position
        angle : float<Degrees>
        color : PenColor
        penState : PenState
    }
    
    /// Functions for manipulating a turtle
    /// "RequireQualifiedAccess" means the module name *must* 
    ///    be used (just like List module)
    /// "ModuleSuffix" is needed so the that module can 
    ///    have the same name as the state type 
    [<RequireQualifiedAccess>]
    [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module Turtle =
```

An alternative way to avoid collisions is to have the state type have a different case, or a different name with a lowercase alias, like this: 

```fsharp
type TurtleState = { ... }
type turtle = TurtleState 

module Turtle =
    let something (t:turtle) = t
```

No matter how the naming is done, we will need a way to construct a new `Turtle`.

If there are no parameters to the constructor, and the state is immutable, then we just need an initial value rather than a function (like `Set.empty` say).

Otherwise we can define a function called `make` (or `create` or similar):

```fsharp
[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module Turtle =

    /// return a new turtle with the specified color
    let make(initialColor) = {
        position = initialPosition
        angle = 0.0<Degrees>
        color = initialColor
        penState = initialPenState
    }                
```

The rest of the turtle module functions are unchanged from their implementation in [way 2](/posts/13-ways-of-looking-at-a-turtle/#way2).

### An ADT client

Let's look the client now.

First, let's check that the state really is private. If we try to create a state explicitly, as shown below, we get a compiler error:

```fsharp
let initialTurtle = {
    position = initialPosition
    angle = 0.0<Degrees>
    color = initialColor
    penState = initialPenState
}
// Compiler error FS1093: 
//    The union cases or fields of the type 'Turtle'
//    are not accessible from this code location
```

If we use the constructor and then try to directly access a field directly (such as `position`), we again get a compiler error:

```fsharp
let turtle = Turtle.make(Red)
printfn "%A" turtle.position
// Compiler error FS1093: 
//    The union cases or fields of the type 'Turtle'
//    are not accessible from this code location
```

But if we stick to the functions in the `Turtle` module, we can safely create a state value and then call functions on it, just as we did before:

```fsharp
// versions with log baked in (via partial application)
let move = Turtle.move log
let turn = Turtle.turn log
// etc

let drawTriangle() =
    Turtle.make(Red)
    |> move 100.0 
    |> turn 120.0<Degrees>
    |> move 100.0 
    |> turn 120.0<Degrees>
    |> move 100.0 
    |> turn 120.0<Degrees>
```

### Advantages and disadvantages of ADTs

*Advantages*

* All code is stateless, hence easy to test.
* The encapsulation of the state means that the focus is always fully on the behavior and properties of the type.
* Clients can never have a dependency on a particular implementation, which means that implementations can be changed safely.
* You can even swap implementations (e.g. by shadowing, or linking to a different assembly) for testing, performance, etc.
  
*Disadvantages*

* The client has to manage the current turtle state.
* The client has no control over the implementation (e.g. by using dependency injection). 

For more on ADTs in F#, see [this talk and thread](https://www.reddit.com/r/fsharp/comments/36s0zr/structuring_f_programs_with_abstract_data_types/?) by Bryan Edds.

*The source code for this version is available [here](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/14-AdtTurtle.fsx).*

<hr>

<a id="way15"></a>

## 15: Capability-based Turtle 

In the "monadic control flow" approach [(way 12)](/posts/13-ways-of-looking-at-a-turtle-2/#way12) we handled responses from the turtle telling us that it had hit a barrier.

But even though we had hit a barrier, nothing was stopping us from calling the `move` operation over and over again!  

Now imagine that, once we had hit the barrier, the `move` operation was no longer available to us.  We couldn't abuse it because it would be no longer there!

To make this work, we shouldn't provide an API, but instead, after each call, return a list of functions that the client can call to do the next step. The functions would normally include
the usual suspects of `move`, `turn`, `penUp`, etc., but when we hit a barrier, `move` would be dropped from that list.  Simple, but effective.

This technique is closely related to an authorization and security technique called *capability-based security*. If you are interested in learning more,
I have [a whole series of posts devoted to it](/posts/capability-based-security/).

### Designing a capability-based Turtle 

The first thing is to define the record of functions that will be returned after each call:

```fsharp
type MoveResponse = 
    | MoveOk 
    | HitABarrier

type SetColorResponse = 
    | ColorOk
    | OutOfInk

type TurtleFunctions = {
    move     : MoveFn option
    turn     : TurnFn
    penUp    : PenUpDownFn 
    penDown  : PenUpDownFn 
    setBlack : SetColorFn  option
    setBlue  : SetColorFn  option
    setRed   : SetColorFn  option
    }
and MoveFn =      Distance -> (MoveResponse * TurtleFunctions)
and TurnFn =      Angle    -> TurtleFunctions
and PenUpDownFn = unit     -> TurtleFunctions
and SetColorFn =  unit     -> (SetColorResponse * TurtleFunctions)
```

Let's look at these declarations in detail.

First, there is no `TurtleState` anywhere.  The published turtle functions will encapsulate the state for us.  Similarly there is no `log` function.

Next, the record of functions `TurtleFunctions` defines a field for each function in the API (`move`, `turn`, etc.):

* The `move` function is optional, meaning that it might not be available.
* The `turn`, `penUp` and `penDown` functions are always available.
* The `setColor` operation has been broken out into three separate functions, one for each color, because you might not be able to use red ink, but still be able to use blue ink.
  To indicate that these functions might not be available, `option` is used again.

We have also declared type aliases for each function to make them easier to work. Writing `MoveFn` is easier than writing `Distance -> (MoveResponse * TurtleFunctions)` everywhere!
Note that, since these definitions are mutually recursive, I was forced to use the `and` keyword.

Finally, note the difference between the signature of `MoveFn` in this design and the signature of `move` in [the earlier design of way 12](/posts/13-ways-of-looking-at-a-turtle-2/#way12).

Earlier version:

```fsharp
val move : 
    Log -> Distance -> TurtleState -> (MoveResponse * TurtleState)
```

New version:

```fsharp
val move : 
    Distance -> (MoveResponse * TurtleFunctions)
```

On the input side, the `Log` and `TurtleState` parameters are gone, and on the output side, the `TurtleState` has been replaced with `TurtleFunctions`.

This means that somehow, the output of every API function must be changed to be a `TurtleFunctions` record.
    
### Implementing the turtle operations
   
In order to decide whether we can indeed move, or use a particular color, we first need to augment the `TurtleState` type to track these factors:
    
```fsharp
type Log = string -> unit

type private TurtleState = {
    position : Position
    angle : float<Degrees>
    color : PenColor
    penState : PenState
    
    canMove : bool                // new!
    availableInk: Set<PenColor>   // new!
    logger : Log                  // new!
}
```

This has been enhanced with

* `canMove`, which if false means that we are at a barrier and should not return a valid `move` function.
* `availableInk` contains a set of colors. If a color is not in this set, then we should not return a valid `setColorXXX` function for that color.
* Finally, we've added the `log` function into the state so that we don't have to pass it explicitly to each operation. It will get set once, when the turtle is created.

The `TurtleState` is getting a bit ugly now, but that's alright, because it's private! The clients will never even see it.

With this augmented state available, we can change `move`. First we'll make it private, and second we'll set the `canMove` flag (using `moveResult <> HitABarrier`) before returning a new state:
  
```fsharp
/// Function is private! Only accessible to the client via the TurtleFunctions record
let private move log distance state =

    log (sprintf "Move %0.1f" distance)
    // calculate new position 
    let newPosition = calcNewPosition distance state.angle state.position 
    // adjust the new position if out of bounds
    let moveResult, newPosition = checkPosition newPosition 
    // draw line if needed
    if state.penState = Down then
        dummyDrawLine log state.position newPosition state.color
        
    // return the new state and the Move result
    let newState = {
        state with 
         position = newPosition
         canMove = (moveResult <> HitABarrier)   // NEW! 
        }
    (moveResult,newState) 
```

We need some way of changing `canMove` back to true! So let's assume that if you turn, you can move again. 

Let's add that logic to the `turn` function then:

```fsharp
let private turn log angle state =
    log (sprintf "Turn %0.1f" angle)
    // calculate new angle
    let newAngle = (state.angle + angle) % 360.0<Degrees>
    // NEW!! assume you can always move after turning
    let canMove = true
    // update the state
    {state with angle = newAngle; canMove = canMove} 
```

The `penUp` and `penDown` functions are unchanged, other than being made private.  

And for the last operation, `setColor`, we'll remove the ink from the availability set as soon as it is used just once!

```fsharp
let private setColor log color state =
    let colorResult = 
        if color = Red then OutOfInk else ColorOk
    log (sprintf "SetColor %A" color)
    
    // NEW! remove color ink from available inks
    let newAvailableInk = state.availableInk |> Set.remove color
    
    // return the new state and the SetColor result
    let newState = {state with color = color; availableInk = newAvailableInk}
    (colorResult,newState) 
```
 
Finally we need a function that can create a `TurtleFunctions` record from the `TurtleState`. I'll call it `createTurtleFunctions`.

Here's the complete code, and I'll discuss it in detail below:
  
```fsharp
/// Create the TurtleFunctions structure associated with a TurtleState
let rec private createTurtleFunctions state =
    let ctf = createTurtleFunctions  // alias

    // create the move function,
    // if the turtle can't move, return None
    let move = 
        // the inner function
        let f dist = 
            let resp, newState = move state.logger dist state
            (resp, ctf newState)

        // return Some of the inner function
        // if the turtle can move, or None
        if state.canMove then
            Some f
        else
            None

    // create the turn function
    let turn angle = 
        let newState = turn state.logger angle state
        ctf newState

    // create the pen state functions
    let penDown() = 
        let newState = penDown state.logger state
        ctf newState

    let penUp() = 
        let newState = penUp state.logger state
        ctf newState

    // create the set color functions
    let setColor color = 
        // the inner function
        let f() = 
            let resp, newState = setColor state.logger color state
            (resp, ctf newState)

        // return Some of the inner function 
        // if that color is available, or None
        if state.availableInk |> Set.contains color then
            Some f
        else
            None

    let setBlack = setColor Black
    let setBlue = setColor Blue
    let setRed = setColor Red
    
    // return the structure
    {
    move     = move
    turn     = turn
    penUp    = penUp 
    penDown  = penDown 
    setBlack = setBlack
    setBlue  = setBlue  
    setRed   = setRed   
    }
```
  
Let's look at how this works.

First, note that this function needs the `rec` keyword attached, as it refers to itself. I've added a shorter alias (`ctf`) for it as well.

Next, new versions of each of the API functions are created. For example, a new `turn` function is defined like this:

```fsharp
let turn angle = 
    let newState = turn state.logger angle state
    ctf newState
```
  
This calls the original `turn` function with the logger and state, and then uses the recursive call (`ctf`) to convert the new state into the record of functions.

For an optional function like `move`, it is a bit more complicated. An inner function `f` is defined, using the orginal `move`, and then either `f` is returned as `Some`,
or `None` is returned, depending on whether the `state.canMove` flag is set:

```fsharp
// create the move function,
// if the turtle can't move, return None
let move = 
    // the inner function
    let f dist = 
        let resp, newState = move state.logger dist state
        (resp, ctf newState)

    // return Some of the inner function
    // if the turtle can move, or None
    if state.canMove then
        Some f
    else
        None
```

Similarly, for `setColor`, an inner function `f` is defined and then returned or not depending on whether the color parameter is in the `state.availableInk` collection:

```fsharp
let setColor color = 
    // the inner function
    let f() = 
        let resp, newState = setColor state.logger color state
        (resp, ctf newState)

    // return Some of the inner function 
    // if that color is available, or None
    if state.availableInk |> Set.contains color then
        Some f
    else
        None
```

Finally, all these functions are added to the record:

```fsharp
// return the structure
{
move     = move
turn     = turn
penUp    = penUp 
penDown  = penDown 
setBlack = setBlack
setBlue  = setBlue  
setRed   = setRed   
}
```

And that's how you build a `TurtleFunctions` record!
  
We need one more thing: a constructor to create some initial value of the `TurtleFunctions`, since we no longer have direct access to the API. This is now the ONLY public function available to the client!
  
```fsharp
/// Return the initial turtle.
/// This is the ONLY public function!
let make(initialColor, log) = 
    let state = {
        position = initialPosition
        angle = 0.0<Degrees>
        color = initialColor
        penState = initialPenState
        canMove = true
        availableInk = [Black; Blue; Red] |> Set.ofList
        logger = log
    }                
    createTurtleFunctions state
``` 

This function bakes in the `log` function, creates a new state, and then calls `createTurtleFunctions` to return a `TurtleFunction` record for the client to use.

### Implementing a client of the capability-based turtle

Let's try using this now.  First, let's try to do `move 60` and then `move 60` again. The second move should take us to the boundary (at 100),
and so at that point the `move` function should no longer be available.

First, we create the `TurtleFunctions` record with `Turtle.make`. Then we can't just move immediately, we have to test to see if the `move` function is available first:

```fsharp
let testBoundary() =
    let turtleFns = Turtle.make(Red,log)
    match turtleFns.move with
    | None -> 
        log "Error: Can't do move 1"
    | Some moveFn -> 
        ...    
```

In the last case, the `moveFn` is available, so we can call it with a distance of 60. 

The output of the function is a pair: a `MoveResponse` type and a new `TurtleFunctions` record.

We'll ignore the `MoveResponse` and check the `TurtleFunctions` record again to see if we can do the next move:

```fsharp
let testBoundary() =
    let turtleFns = Turtle.make(Red,log)
    match turtleFns.move with
    | None -> 
        log "Error: Can't do move 1"
    | Some moveFn -> 
        let (moveResp,turtleFns) = moveFn 60.0 
        match turtleFns.move with
        | None -> 
            log "Error: Can't do move 2"
        | Some moveFn -> 
            ...
```

And finally, one more time:

```fsharp
let testBoundary() =
    let turtleFns = Turtle.make(Red,log)
    match turtleFns.move with
    | None -> 
        log "Error: Can't do move 1"
    | Some moveFn -> 
        let (moveResp,turtleFns) = moveFn 60.0 
        match turtleFns.move with
        | None -> 
            log "Error: Can't do move 2"
        | Some moveFn -> 
            let (moveResp,turtleFns) = moveFn 60.0 
            match turtleFns.move with
            | None -> 
                log "Error: Can't do move 3"
            | Some moveFn -> 
                log "Success"
```

If we run this, we get the output:

```text
Move 60.0
...Draw line from (0.0,0.0) to (60.0,0.0) using Red
Move 60.0
...Draw line from (60.0,0.0) to (100.0,0.0) using Red
Error: Can't do move 3
```

Which shows that indeed, the concept is working!

That nested option matching is really ugly, so let's whip up a quick `maybe` workflow to make it look nicer:

```fsharp
type MaybeBuilder() =         
    member this.Return(x) = Some x
    member this.Bind(x,f) = Option.bind f x
    member this.Zero() = Some()
let maybe = MaybeBuilder()
```

And a logging function that we can use inside the workflow:

```fsharp
/// A function that logs and returns Some(),
/// for use in the "maybe" workflow
let logO message =
    printfn "%s" message
    Some ()
```

Now we can try setting some colors using the `maybe` workflow:

```fsharp
let testInk() =
    maybe {
    // create a turtle
    let turtleFns = Turtle.make(Black,log)
    
    // attempt to get the "setRed" function
    let! setRedFn = turtleFns.setRed 

    // if so, use it
    let (resp,turtleFns) = setRedFn() 

    // attempt to get the "move" function
    let! moveFn = turtleFns.move 

    // if so, move a distance of 60 with the red ink
    let (resp,turtleFns) = moveFn 60.0 

    // check if the "setRed" function is still available
    do! match turtleFns.setRed with
        | None -> 
            logO "Error: Can no longer use Red ink"
        | Some _ -> 
            logO "Success: Can still use Red ink"
    
    // check if the "setBlue" function is still available
    do! match turtleFns.setBlue with
        | None -> 
            logO "Error: Can no longer use Blue ink"
        | Some _ -> 
            logO "Success: Can still use Blue ink"

    } |> ignore
```

The output of this is:

```text
SetColor Red
Move 60.0
...Draw line from (0.0,0.0) to (60.0,0.0) using Red
Error: Can no longer use Red ink
Success: Can still use Blue ink
```

Actually, using a `maybe` workflow is not a very good idea, because the first failure exits the workflow!
You'd want to come up with something a bit better for real code, but I hope that you get the idea.

### Advantages and disadvantages of a capability-based approach

*Advantages*

* Prevents clients from abusing the API.
* Allows APIs to evolve (and devolve) without affecting clients. For example, I could transition to a monochrome-only turtle by hard-coding `None` for each color function in the record of functions,
  after which I could safely remove the `setColor` implementation. During this process no client would break! This is similar to the [HATEAOS approach](https://en.wikipedia.org/wiki/HATEOAS) for RESTful web services.
* Clients are decoupled from a particular implementation because the record of functions acts as an interface.
  
*Disadvantages*

* Complex to implement.
* The client's logic is much more convoluted as it can never be sure that a function will be available! It has to check every time. 
* The API is not easily serializable, unlike some of the data-oriented APIs.

For more on capability-based security, see [my posts](/posts/capability-based-security/) or watch my ["Enterprise Tic-Tac-Toe" video](/ettt/).

*The source code for this version is available [here](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle/blob/master/15-CapabilityBasedTurtle.fsx).*

## Summary

> I was of three minds,   
> Like a finger tree   
> In which there are three immutable turtles.   
> -- *"Thirteen ways of looking at a turtle", by Wallace D Coriacea*

I feel better now that I've got these two extra ways out of my system! Thanks for reading!
  
*The source code for this post is available [on github](https://github.com/swlaschin/13-ways-of-looking-at-a-turtle).*



.