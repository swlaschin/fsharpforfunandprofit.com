---
layout: post
title: "Calculator Walkthrough: Part 4"
description: "Designing using a state machine"
categories: ["Worked Examples"]
seriesId: "Annotated walkthroughs"
seriesOrder: 4
---

In this series of posts, I've been developing a simple pocket calculator app.

In the [first post](/posts/calculator-design/), we completed a first draft of the design, using type-first development.
and in the [second post](/posts/calculator-implementation/), we created an initial implemeentation.

In the [previous post](/posts/calculator-complete-v1/), we created the rest of the code, including the user interface, and attempted to use it.

But the final result was unusable!  The problem wasn't that the code was buggy, it was that I didn't
spend enough time thinking about the requirements before I started coding!

Oh well. As Fred Brooks famously said: "plan to throw one away; you will, anyhow" (although that is a [bit simplistic](http://www.davewsmith.com/blog/2010/brook-revisits-plan-to-throw-one-away)).

The good news is that I have learned from the previous bad implementation, and have a plan to make the design better.

## Reviewing the bad design

Looking at the design and implementation (see [this gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v1_patched-fsx)), a few things stand out:

First, the event handling types such as `UpdateDisplayFromDigit` did not take into account the *context*, the current state of the calculator.
The `allowAppend` flag we added as a patch was one way to take the context into account, but it smells awful bad.

Second there was a bit of special case code for certain inputs (`Zero` and `DecimalSeparator`), as you can see from this code snippet:

```fsharp
let appendCh= 
    match digit with
    | Zero -> 
        // only allow one 0 at start of display
        if display="0" then "" else "0"
    | One -> "1"
    | // snip
    | DecimalSeparator -> 
        if display="" then 
            // handle empty display with special case
            "0" + config.decimalSeparator  
        else if display.Contains(config.decimalSeparator) then 
            // don't allow two decimal separators
            "" 
        else 
            config.decimalSeparator
```

This makes me think that these inputs should be treated as different *in the design itself* and not hidden in the implementation
-- after all we want the design to also act as documentation as much as possible.

## Using a finite state machine as a design tool

So if the ad-hoc, make-it-up-as-you-go-along approach failed, what should I do instead?

Well, I am a big proponent of using [finite state machines](https://en.wikipedia.org/wiki/Finite-state_machine)
("FSMs" -- not be confused with the [True FSM](https://en.wikipedia.org/wiki/Flying_Spaghetti_Monster)) where appropriate.
It is amazing how often a program can be modelled as a state machine.

What are the benefits of using state machines?  I'm going to repeat what I said in [another post](/posts/designing-with-types-representing-states/).

**Each state can have different allowable behavior.**
In other words, a state machine forces you to think about context, and what options are available in that context. 

In this case, I forgot that the context changed after an `Add` was processed, and thus the rules for accumulating digits changed too.

**All the states are explicitly documented.**
It is all too easy to have important states that are implicit but never documented.

For example, I have created special code to deal with zero and decimal separators. Currently it is buried away in the implementation, but it should be part of the design.
 
**It is a design tool that forces you to think about every possibility that could occur.**
A common cause of errors is that certain edge cases are not handled, but a state machine forces *all* cases to be thought about.

In this case, in addition to the most obvious bug, there are still some edge cases that are not dealt with properly,
such as immediately following a math operation with *another* math operation. What should happen then?


## How to implement simple finite state machines in F# ##

You are probably familiar with complex FSMs, such as those used in language parsers and regular expressions.
Those kinds of state machines are generated from rule sets or grammars, and are quite complicated.

The kinds of state machines that I'm talking about are much, much simpler.
Just a few cases at the most, with a small number of transitions, so we don't need to use complex generators.

Here's an example of what I am talking about:
![State machine](/assets/img/state_machine_1.png)

So what is the best way to implement these simple state machines in F#?

Now, designing and implementing FSMs is a complex topic in in own right, with
its own terminology ([NFAs and DFAs](https://en.wikipedia.org/wiki/Powerset_construction), [Moore vs. Mealy](https://stackoverflow.com/questions/11067994/difference-between-mealy-and-moore), etc),
and [whole businesses](http://www.stateworks.com/) built around it.

In F#, there are a number of possible approaches, such as table driven, or mutually recursive functions, or agents, or OO-style subclasses, etc.

But my preferred approach (for an ad-hoc manual implementation) makes extensive use of union types and pattern matching.

First, create a union type that represents all the states.
For example, if there are three states called "A", "B" and "C", the type would look like this:

```fsharp
type State = 
    | AState 
    | BState 
    | CState
```

In many cases, each state will need to store some data that is relevant to that state.
So we will need to create types to hold that data as well.

```fsharp
type State = 
    | AState of AStateData
    | BState of BStateData
    | CState
and AStateData = 
    {something:int}
and BStateData = 
    {somethingElse:int}
```

Next, all possible events that can happen are defined in another union type. If events have data associated with them, add that as well.

```fsharp
type InputEvent = 
    | XEvent
    | YEvent of YEventData
    | ZEvent
and YEventData =
    {eventData:string}
```

Finally, we can create a "transition" function that, given a current state and input event, returns a new state.

```fsharp
let transition (currentState,inputEvent) =
    match currentState,inputEvent with
    | AState, XEvent -> // new state
    | AState, YEvent -> // new state
    | AState, ZEvent -> // new state
    | BState, XEvent -> // new state
    | BState, YEvent -> // new state
    | CState, XEvent -> // new state
    | CState, ZEvent -> // new state
```

What I like about this approach in a language with pattern matching, like F#,
is that **if we forget to handle a particular combination of state and event, we get a compiler warning**. How awesome is that?

It's true that, for systems with many states and input events, it may be unreasonable to expect every possible combination to be explicitly handled.
But in my experience, many nasty bugs are caused by processing an event when you shouldn't, exactly as we saw with the original design accumulating digits when it shouldn't have.

Forcing yourself to consider every possible combination is thus a helpful design practice.

Now, even with a small number of states and events, the number of possible combinations gets large very quickly.
To make it more manageable in practice, I typically create a series of helper functions, one for each state, like this:

```fsharp
let aStateHandler stateData inputEvent = 
    match inputEvent with
    | XEvent -> // new state
    | YEvent _ -> // new state
    | ZEvent -> // new state

let bStateHandler stateData inputEvent = 
    match inputEvent with
    | XEvent -> // new state
    | YEvent _ -> // new state
    | ZEvent -> // new state

let cStateHandler inputEvent = 
    match inputEvent with
    | XEvent -> // new state
    | YEvent _ -> // new state
    | ZEvent -> // new state

let transition (currentState,inputEvent) =
    match currentState with
    | AState stateData -> 
        // new state
        aStateHandler stateData inputEvent 
    | BState stateData -> 
        // new state
        bStateHandler stateData inputEvent 
    | CState -> 
        // new state
        cStateHandler inputEvent 
```

So let's try this approach and attempt to implement the state diagram above:

```fsharp
let aStateHandler stateData inputEvent = 
    match inputEvent with
    | XEvent -> 
        // transition to B state
        BState {somethingElse=stateData.something}
    | YEvent _ -> 
        // stay in A state
        AState stateData 
    | ZEvent -> 
        // transition to C state
        CState 

let bStateHandler stateData inputEvent = 
    match inputEvent with
    | XEvent -> 
        // stay in B state
        BState stateData 
    | YEvent _ -> 
        // transition to C state
        CState 

let cStateHandler inputEvent = 
    match inputEvent with
    | XEvent -> 
        // stay in C state
        CState
    | ZEvent -> 
        // transition to B state
        BState {somethingElse=42}

let transition (currentState,inputEvent) =
    match currentState with
    | AState stateData -> 
        aStateHandler stateData inputEvent 
    | BState stateData -> 
        bStateHandler stateData inputEvent 
    | CState -> 
        cStateHandler inputEvent 
```

If we try to compile this, we immediately get some warnings:

* (near bStateHandler) `Incomplete pattern matches on this expression. For example, the value 'ZEvent' may indicate a case not covered by the pattern(s).`
* (near cStateHandler) `Incomplete pattern matches on this expression. For example, the value 'YEvent (_)' may indicate a case not covered by the pattern(s).`

This is really helpful. It means we have missed some edge cases and we should change our code to handle these events.

By the way, please do *not* fix the code with a wildcard match (underscore)! That defeats the purpose.
If you want to ignore an event, do it explicitly.  

Here's the fixed up code, which compiles without warnings:

```fsharp
let bStateHandler stateData inputEvent = 
    match inputEvent with
    | XEvent 
    | ZEvent -> 
        // stay in B state
        BState stateData 
    | YEvent _ -> 
        // transition to C state
        CState 

let cStateHandler inputEvent = 
    match inputEvent with
    | XEvent  
    | YEvent _ -> 
        // stay in C state
        CState
    | ZEvent -> 
        // transition to B state
        BState {somethingElse=42}
```

*You can see the code for this example in [this gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-statemachine-fsx).*

## Designing the state machine for the calculator

Let's sketch out a state machine for the calculator now. Here's a first attempt:

![Calculator state machine v1](/assets/img/calculator_states_1.png)

Each state is a box, and the events that trigger transitions (such as a digit or math operation or `Equals`) are in red.

If we follow through a sequence of events for something like `1` `Add` `2` `Equals`, you can see that we'll end up at the "Show result" state at the bottom.

But remember that we wanted to raise the handling of zero and decimal separators up to the design level?

So let's create special events for those inputs, and a new state "accumulate with decimal" that ignores subsequent decimal separators.

Here's version 2:

![Calculator state machine v1](/assets/img/calculator_states_2.png)

## Finalizing the state machine 

> "Good artists copy. Great artists steal." 
> -- Pablo Picasso ([but not really](http://quoteinvestigator.com/2013/03/06/artists-steal/)) 

At this point, I'm thinking that surely I can't be only person to have thought of using a state machine to model a calculator?
Perhaps I can do some research and <strike>steal</strike> borrow someone else's design?

Sure enough, googling for "calculator state machine" brings up all sorts of results, including [this one](http://cnx.org/contents/9bac155d-509e-46a6-b48b-30731ed08ce6@2/Finite_State_Machines_and_the_)
which has a detailed spec and state transition diagram.

Looking at that diagram, and doing some more thinking, leads to the following insights:

* The "clear" state and zero state are the same. Sometimes there is a pending op, sometimes not.
* A math operation and `Equals` are very similar in that they update the display with any pending calculation.
  The only difference is whether a pending op is added to the state or not.
* The error message case definitely needs to be a distinct state. It ignores all input other than `Clear`.

With these insights in mind then, here's version 3 of our state transition diagram:

![Calculator state machine v1](/assets/img/calculator_states_3.png)

I'm only showing the key transitions -- it would be too overwhelming to show all of them.
But it does give us enough information to get started on the detailed requirements.

As we can see, there are five states:

* ZeroState
* AccumulatorState
* AccumulatorDecimalState
* ComputedState
* ErrorState

And there are six possible inputs:

* Zero
* NonZeroDigit
* DecimalSeparator
* MathOp
* Equals
* Clear

Let's document each state, and what data it needs to store, if any.

<table class="table table-condensed table-striped">

<tr>
<th>State</th>
<th>Data associated with state</th>
<th>Special behavior?</th>
</tr>

<tr>
<td>ZeroState</td>
<td>(optional) pending op</td>
<td>Ignores all Zero input</td>
</tr>

<tr>
<td>AccumulatorState</td>
<td>buffer and (optional) pending op</td>
<td>Accumulates digits in buffer</td>
</tr>

<tr>
<td>AccumulatorDecimalState</td>
<td>buffer and (optional) pending op</td>
<td>Accumulates digits in buffer, but ignores decimal separators</td>
</tr>

<tr>
<td>ComputedState</td>
<td>Calculated number and (optional) pending op</td>
<td></td>
</tr>

<tr>
<td>ErrorState</td>
<td>Error message</td>
<td>Ignores all input other than Clear</td>
</tr>

</table>


## Documenting each state and event combination

Next we should think about what happens for each state and event combination.
As with the sample code above, we'll group them so that we only have to deal with the events for one state at a time.

Let's start with the `ZeroState` state. Here are the transitions for each type of input:

<table class="table table-condensed table-striped">

<tr>
<th>Input</th>
<th>Action</th>
<th>New State</th>
</tr>

<tr>
<td>Zero</td>
<td>(ignore)</td>
<td>ZeroState</td>
</tr>

<tr>
<td>NonZeroDigit</td>
<td>Start a new accumulator with the digit.</td>
<td>AccumulatorState</td>
</tr>

<tr>
<td>DecimalSeparator</td>
<td>Start a new accumulator with "0."</td>
<td>AccumulatorDecimalState</td>
</tr>

<tr>
<td>MathOp</td>
<td>Go to Computed or ErrorState state.
   <br>If there is a pending op, update the display based on the result of the calculation (or error).
   <br>Also, if calculation was successful, push a new pending op, built from the event, using a current number of "0".
   </td>
<td>ComputedState</td>
</tr>

<tr>
<td>Equals</td>
<td>As with MathOp, but without any pending op</td>
<td>ComputedState</td>
</tr>

<tr>
<td>Clear</td>
<td>(ignore)</td>
<td>ZeroState</td>
</tr>

</table>

We can repeat the process with the `AccumulatorState` state. Here are the transitions for each type of input:

<table class="table table-condensed table-striped">

<tr>
<th>Input</th>
<th>Action</th>
<th>New State</th>
</tr>

<tr>
<td>Zero</td>
<td>Append "0" to the buffer.</td>
<td>AccumulatorState</td>
</tr>

<tr>
<td>NonZeroDigit</td>
<td>Append the digit to the buffer.</td>
<td>AccumulatorState</td>
</tr>

<tr>
<td>DecimalSeparator</td>
<td>Append the separator to the buffer, and transition to new state.</td>
<td>AccumulatorDecimalState</td>
</tr>

<tr>
<td>MathOp</td>
<td>Go to Computed or ErrorState state.
   <br>If there is a pending op, update the display based on the result of the calculation (or error).
   <br>Also, if calculation was successful, push a new pending op, built from the event, using a current number based on whatever is in the accumulator.
   </td>

<td>ComputedState</td>
</tr>

<tr>
<td>Equals</td>
<td>As with MathOp, but without any pending op</td>
<td>ComputedState</td>
</tr>

<tr>
<td>Clear</td>
<td>Go to Zero state. Clear any pending op.</td>
<td>ZeroState</td>
</tr>

</table>

The event handling for `AccumulatorDecimalState` state is the same, except that `DecimalSeparator` is ignored.

What about the `ComputedState` state. Here are the transitions for each type of input:

<table class="table table-condensed table-striped">

<tr>
<th>Input</th>
<th>Action</th>
<th>New State</th>
</tr>

<tr>
<td>Zero</td>
<td>Go to ZeroState state, but preserve any pending op</td>
<td>ZeroState</td>
</tr>

<tr>
<td>NonZeroDigit</td>
<td>Start a new accumulator, preserving any pending op</td>
<td>AccumulatorState</td>
</tr>

<tr>
<td>DecimalSeparator</td>
<td>Start a new decimal accumulator, preserving any pending op</td>
<td>AccumulatorDecimalState</td>
</tr>

<tr>
<td>MathOp</td>
<td>Stay in Computed state. Replace any pending op with a new one built from the input event</td>
<td>ComputedState</td>
</tr>

<tr>
<td>Equals</td>
<td>Stay in Computed state. Clear any pending op</td>
<td>ComputedState</td>
</tr>

<tr>
<td>Clear</td>
<td>Go to Zero state. Clear any pending op.</td>
<td>ZeroState</td>
</tr>

</table>

Finally, the `ErrorState` state is very easy. :

<table class="table table-condensed table-striped">

<tr>
<th>Input</th>
<th>Action</th>
<th>New State</th>
</tr>

<tr>
<td>Zero, NonZeroDigit, DecimalSeparator<br>MathOp, Equals</td>
<td>(ignore)</td>
<td>ErrorState</td>
</tr>

<tr>
<td>Clear</td>
<td>Go to Zero state. Clear any pending op.</td>
<td>ZeroState</td>
</tr>

</table>

## Converting the states into F# code

Now that we've done all this work, the conversion into types is straightforward.

Here are the main types:

```fsharp
type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
// five states        
and CalculatorState = 
    | ZeroState of ZeroStateData 
    | AccumulatorState of AccumulatorStateData 
    | AccumulatorWithDecimalState of AccumulatorStateData 
    | ComputedState of ComputedStateData 
    | ErrorState of ErrorStateData 
// six inputs
and CalculatorInput = 
    | Zero 
    | Digit of NonZeroDigit
    | DecimalSeparator
    | MathOp of CalculatorMathOp
    | Equals 
    | Clear
// data associated with each state
and ZeroStateData = 
    PendingOp option
and AccumulatorStateData = 
    {digits:DigitAccumulator; pendingOp:PendingOp option}
and ComputedStateData = 
    {displayNumber:Number; pendingOp:PendingOp option}
and ErrorStateData = 
    MathOperationError
```

If we compare these types to the first design (below), we have now made it clear that there is something special about `Zero` and `DecimalSeparator`,
as they have been promoted to first class citizens of the input type.

```fsharp
// from the old design
type CalculatorInput = 
    | Digit of CalculatorDigit
    | Op of CalculatorMathOp
    | Action of CalculatorAction
        
// from the new design        
type CalculatorInput = 
    | Zero 
    | Digit of NonZeroDigit
    | DecimalSeparator
    | MathOp of CalculatorMathOp
    | Equals 
    | Clear
```        

Also, in the old design, we had a single state type (below) that stored data for all contexts, while in the new design, the state is *explicitly different* for each context.
The types `ZeroStateData`, `AccumulatorStateData`, `ComputedStateData`, and `ErrorStateData` make this obvious.

```fsharp
// from the old design
type CalculatorState = {
    display: CalculatorDisplay
    pendingOp: (CalculatorMathOp * Number) option
    }
    
// from the new design    
type CalculatorState = 
    | ZeroState of ZeroStateData 
    | AccumulatorState of AccumulatorStateData 
    | AccumulatorWithDecimalState of AccumulatorStateData 
    | ComputedState of ComputedStateData 
    | ErrorState of ErrorStateData 
```        

Now that we have the basics of the new design, we need to define the other types referenced by it:

```fsharp
and DigitAccumulator = string
and PendingOp = (CalculatorMathOp * Number)
and Number = float
and NonZeroDigit= 
    | One | Two | Three | Four 
    | Five | Six | Seven | Eight | Nine
and CalculatorMathOp = 
    | Add | Subtract | Multiply | Divide
and MathOperationResult = 
    | Success of Number 
    | Failure of MathOperationError
and MathOperationError = 
    | DivideByZero
```

And finally, we can define the services:

```fsharp
// services used by the calculator itself
type AccumulateNonZeroDigit = NonZeroDigit * DigitAccumulator -> DigitAccumulator 
type AccumulateZero = DigitAccumulator -> DigitAccumulator 
type AccumulateSeparator = DigitAccumulator -> DigitAccumulator 
type DoMathOperation = CalculatorMathOp * Number * Number -> MathOperationResult 
type GetNumberFromAccumulator = AccumulatorStateData -> Number

// services used by the UI or testing
type GetDisplayFromState = CalculatorState -> string
type GetPendingOpFromState = CalculatorState -> string

type CalculatorServices = {
    accumulateNonZeroDigit :AccumulateNonZeroDigit 
    accumulateZero :AccumulateZero 
    accumulateSeparator :AccumulateSeparator
    doMathOperation :DoMathOperation 
    getNumberFromAccumulator :GetNumberFromAccumulator 
    getDisplayFromState :GetDisplayFromState 
    getPendingOpFromState :GetPendingOpFromState 
    }
```

Note that because the state is much more complicated, I've added helper function `getDisplayFromState` that extracts the display text from the state.
This helper function will be used the UI or other clients (such as tests) that need to get the text to display.

I've also added a `getPendingOpFromState`, so that we can show the pending state in the UI as well.

## Creating a state-based implementation

Now we can create a state-based implementation, using the pattern described earlier.

*(The complete code is available in [this gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v2-fsx).)*

Let's start with the main function that does the state transitions:

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    // create some local functions with partially applied services
    let handleZeroState = handleZeroState services
    let handleAccumulator = handleAccumulatorState services
    let handleAccumulatorWithDecimal = handleAccumulatorWithDecimalState services
    let handleComputed = handleComputedState services
    let handleError = handleErrorState 

    fun (input,state) -> 
        match state with
        | ZeroState stateData -> 
            handleZeroState stateData input
        | AccumulatorState stateData -> 
            handleAccumulator stateData input
        | AccumulatorWithDecimalState stateData -> 
            handleAccumulatorWithDecimal stateData input
        | ComputedState stateData -> 
            handleComputed stateData input
        | ErrorState stateData -> 
            handleError stateData input
```
                
As you can see, it passes the responsibility to a number of handlers, one for each state, which will be discussed below.

But before we do that, I thought it might be instructive to compare the new state-machine based design with the (buggy!) one I did previously.

Here is the code from the previous one:

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    fun (input,state) -> 
        match input with
        | Digit d ->
            let newState = updateDisplayFromDigit services d state
            newState //return
        | Op op ->
            let newState1 = updateDisplayFromPendingOp services state
            let newState2 = addPendingMathOp services op newState1 
            newState2 //return
        | Action Clear ->
            let newState = services.initState()
            newState //return
        | Action Equals ->
            let newState = updateDisplayFromPendingOp services state
            newState //return
```

If we compare the two implementations, we can see that there has been a shift of emphasis from events to state.
You can see this by comparing how main pattern matching is done in the two implementations:

* In the original version, the focus was on the input, and the state was secondary.
* In the new version, the focus is on the state, and the input is secondary.

The focus on *input* over *state*, ignoring the context, is why the old version was such a bad design.

To repeat what I said above, many nasty bugs are caused by processing an event when you shouldn't (as we saw with the original design).
I feel much more confident in the new design because of the explicit emphasis on state and context from the very beginning.

In fact, I'm not alone in noticing these kinds of issues.
Many people think that classic "[event-driven programming](https://en.wikipedia.org/wiki/Event-driven_programming)" is flawed
and recommend a more "state driven approach" (e.g. [here](http://www.barrgroup.com/Embedded-Systems/How-To/State-Machines-Event-Driven-Systems) and [here](http://seabites.wordpress.com/2011/12/08/your-ui-is-a-statechart/)),
just as I have done here.

## Creating the handlers

We have already documented the requirements for each state transition, so writing the code is straightforward.
We'll start with the code for the `ZeroState` handler:

```fsharp
let handleZeroState services pendingOp input = 
    // create a new accumulatorStateData object that is used when transitioning to other states
    let accumulatorStateData = {digits=""; pendingOp=pendingOp}
    match input with
    | Zero -> 
        ZeroState pendingOp // stay in ZeroState 
    | Digit digit -> 
        accumulatorStateData 
        |> accumulateNonZeroDigit services digit 
        |> AccumulatorState  // transition to AccumulatorState  
    | DecimalSeparator -> 
        accumulatorStateData 
        |> accumulateSeparator services 
        |> AccumulatorWithDecimalState  // transition to AccumulatorWithDecimalState  
    | MathOp op -> 
        let nextOp = Some op
        let newState = getComputationState services accumulatorStateData nextOp 
        newState  // transition to ComputedState or ErrorState
    | Equals -> 
        let nextOp = None
        let newState = getComputationState services accumulatorStateData nextOp 
        newState  // transition to ComputedState or ErrorState
    | Clear -> 
        ZeroState None // transition to ZeroState and throw away any pending ops
```

Again, the *real* work is done in helper functions such as `accumulateNonZeroDigit` and `getComputationState`. We'll look at those in a minute.

Here is the code for the `AccumulatorState` handler:

```fsharp
let handleAccumulatorState services stateData input = 
    match input with
    | Zero -> 
        stateData 
        |> accumulateZero services 
        |> AccumulatorState  // stay in AccumulatorState  
    | Digit digit -> 
        stateData 
        |> accumulateNonZeroDigit services digit 
        |> AccumulatorState  // stay in AccumulatorState  
    | DecimalSeparator -> 
        stateData 
        |> accumulateSeparator services 
        |> AccumulatorWithDecimalState  // transition to AccumulatorWithDecimalState
    | MathOp op -> 
        let nextOp = Some op
        let newState = getComputationState services stateData nextOp 
        newState  // transition to ComputedState or ErrorState
    | Equals -> 
        let nextOp = None
        let newState = getComputationState services stateData nextOp 
        newState  // transition to ComputedState or ErrorState
    | Clear -> 
        ZeroState None // transition to ZeroState and throw away any pending op
```

Here is the code for the `ComputedState` handler:

```fsharp
let handleComputedState services stateData input = 
    let emptyAccumulatorStateData = {digits=""; pendingOp=stateData.pendingOp}
    match input with
    | Zero -> 
        ZeroState stateData.pendingOp  // transition to ZeroState with any pending op
    | Digit digit -> 
        emptyAccumulatorStateData 
        |> accumulateNonZeroDigit services digit 
        |> AccumulatorState  // transition to AccumulatorState  
    | DecimalSeparator -> 
        emptyAccumulatorStateData 
        |> accumulateSeparator services 
        |> AccumulatorWithDecimalState  // transition to AccumulatorWithDecimalState  
    | MathOp op -> 
        // replace the pending op, if any
        let nextOp = Some op
        replacePendingOp stateData nextOp 
    | Equals -> 
        // replace the pending op, if any
        let nextOp = None
        replacePendingOp stateData nextOp 
    | Clear -> 
        ZeroState None // transition to ZeroState and throw away any pending op
```

## The helper functions

Finally, let's look at the helper functions:

The accumulator helpers are trivial -- they just call the appropriate service and wrap the result in an `AccumulatorData` record.

```fsharp
let accumulateNonZeroDigit services digit accumulatorData =
    let digits = accumulatorData.digits
    let newDigits = services.accumulateNonZeroDigit (digit,digits)
    let newAccumulatorData = {accumulatorData with digits=newDigits}
    newAccumulatorData // return
```

The `getComputationState` helper is much more complex -- the most complex function in the entire code base, I should think.

It's very similar to the `updateDisplayFromPendingOp` that we implemented before,
but there are a couple of changes:

* The `services.getNumberFromAccumulator` code can never fail, because of the state-based approach. That makes life simpler!
* The `match result with Success/Failure` code now returns *two* possible states: `ComputedState` or `ErrorState`.
* If there is no pending op, we *still* need to return a valid `ComputedState`, which is what `computeStateWithNoPendingOp` does.

```fsharp
let getComputationState services accumulatorStateData nextOp = 

    // helper to create a new ComputedState from a given displayNumber 
    // and the nextOp parameter
    let getNewState displayNumber =
        let newPendingOp = 
            nextOp |> Option.map (fun op -> op,displayNumber )
        {displayNumber=displayNumber; pendingOp = newPendingOp }
        |> ComputedState

    let currentNumber = 
        services.getNumberFromAccumulator accumulatorStateData 

    // If there is no pending op, create a new ComputedState using the currentNumber
    let computeStateWithNoPendingOp = 
        getNewState currentNumber 

    maybe {
        let! (op,previousNumber) = accumulatorStateData.pendingOp
        let result = services.doMathOperation(op,previousNumber,currentNumber)
        let newState =
            match result with
            | Success resultNumber ->
                // If there was a pending op, create a new ComputedState using the result
                getNewState resultNumber 
            | Failure error -> 
                error |> ErrorState
        return newState
        } |> ifNone computeStateWithNoPendingOp 

```

Finally, we have a new piece of code that wasn't in the previous implementation at all! 

What do you do when you get two math ops in a row? We just replace the old pending op (if any) with the new one (if any).

```fsharp
let replacePendingOp (computedStateData:ComputedStateData) nextOp = 
    let newPending = maybe {
        let! existing,displayNumber  = computedStateData.pendingOp
        let! next = nextOp
        return next,displayNumber  
        }
    {computedStateData with pendingOp=newPending}
    |> ComputedState
```

## Completing the calculator

To complete the application, we just need to implement the services and the UI, in the same way as we did before.  

As it happens, we can reuse almost all of the previous code. The only thing that has really changed
is the way that the input events are structured, which affects how the button handlers are created.

You can get the code for the state machine version of the calculator [here](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v2-fsx).

If you try it out the new code, I think that you will find that it works first time, and feels much more robust. Another win for state-machine driven design!

## Exercises

If you liked this design, and want to work on something similar, here are some exercises that you could do:

* First, you could add some other operations. What would you have to change to implement unary ops such as `1/x` and `sqrt`?
* Some calculators have a back button. What would you have to do to implement this? Luckily all the data structures are immutable, so it should be easy!
* Most calculators have a one-slot memory with store and recall. What would you have to change to implement this? 
* The logic that says that there are only 10 chars allowed on the display is still hidden from the design. How would you make this visible?


## Summary

I hope you found this little experiment useful. I certainly learned something, namely:
don't shortcut requirements gathering, and consider using a state based approach from the beginning -- it might save you time in the long run!

