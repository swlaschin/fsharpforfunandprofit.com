---
layout: post
title: "Calculator Walkthrough: Part 2"
description: "Testing the design with a trial implementation"
categories: ["Worked Examples"]
seriesId: "Annotated walkthroughs"
seriesOrder: 2

---

In this post, I'll continue developing a simple pocket calculator app, like this:

![Calculator image](/assets/img/calculator_1.png)

In the [previous post](/posts/calculator-design/), we completed a first draft of the design, using only types (no UML diagrams!).

Now it's time to create a trial implementation that uses the design.

Doing some real coding at this point acts as a reality check. It ensures that the domain model actually makes sense and is not too abstract.
And of course, it often drives more questions about the requirements and domain model.


## First implementation

So let's try implementing the main calculator function, and see how we do.

First, we can immediately create a skeleton that matches each kind of input and processes it accordingly.

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    fun (input,state) -> 
        match input with
        | Digit d ->
            let newState = // do something
            newState //return
        | Op op ->
            let newState = // do something
            newState //return
        | Action Clear ->
            let newState = // do something
            newState //return
        | Action Equals ->
            let newState = // do something
            newState //return
```

You can see that this skeleton has a case for each type of input to handle it appropriately.
Note that in all cases, a new state is returned.

This style of writing a function might look strange though. Let's look at it a bit more closely.

First, we can see that `createCalculate` is the not the calculator function itself, but a function that *returns* another function.
The returned function is a value of type `Calculate` -- that's what the `:Calculate` at the end means.

Here's just the top part:

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    fun (input,state) -> 
        match input with
            // code
```

Since it is returning a function, I chose to write it using a lambda. That's what the `fun (input,state) -> ` is for.

But I could have also written it using an inner function, like this

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    let innerCalculate (input,state) = 
        match input with
            // code
    innerCalculate // return the inner function
```

Both approaches are basically the same* -- take your pick!

<sub>* Although there might be some performance differences.</sub>

## Dependency injection of services

But `createCalculate` doesn't just return a function, it also has a `services` parameter.
This parameter is used for doing the "dependency injection" of the services.

That is, the services are only used in `createCalculate` itself, and are not visible in the function of type `Calculate` that is returned.

The "main" or "bootstrapper" code that assembles all the components for the application would look something like this:

```fsharp
// create the services
let services = CalculatorServices.createServices()

// inject the services into the "factory" method
let calculate = CalculatorImplementation.createCalculate services

// the returned "calculate" function is of type Calculate 
// and can be passed into the UI, for example

// create the UI and run it
let form = new CalculatorUI.CalculatorForm(calculate)
form.Show()
```

## Implementation: handling digits

Now let's start implementing the various parts of the calculation function. We'll start with the digits handling logic.

To keep the main function clean, let's pass the reponsibility for all the work to a helper function `updateDisplayFromDigit`, like this:

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    fun (input,state) -> 
        match input with
        | Digit d ->
            let newState = updateDisplayFromDigit services d state
            newState //return
```

Note that I'm creating a `newState` value from the result of `updateDisplayFromDigit` and then returning it as a separate step. 

I could have done the same thing in one step, without an explicit `newState` value, as shown below:

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    fun (input,state) -> 
        match input with
        | Digit d ->
            updateDisplayFromDigit services d state
```

Neither approach is automatically best. I would pick one or the other depending on the context. 

For simple cases, I would avoid the extra line as being unnecessary, but sometimes having an explicit return value is more readable.
The name of the value tells you an indication of the return type, and it gives you something to watch in the debugger, if you need to.

Alright, let's implement `updateDisplayFromDigit` now. It's pretty straightforward.

* first use the `updateDisplayFromDigit` in the services to actually update the display
* then create a new state from the new display and return it.

```fsharp
let updateDisplayFromDigit services digit state =
    let newDisplay = services.updateDisplayFromDigit (digit,state.display)
    let newState = {state with display=newDisplay}
    newState //return
```

## Implementation: handling Clear and Equals 

Before we move onto the implementation of the math operations, lets look at handling `Clear` and `Equals`, as they are simpler.

For `Clear`, just init the state, using the provided `initState` service.

For `Equals`, we check if there is a pending math op. If there is, run it and update the display, otherwise do nothing.
We'll put that logic in a helper function called `updateDisplayFromPendingOp`.

So here's what `createCalculate` looks like now:

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    fun (input,state) -> 
        match input with
        | Digit d -> // as above
        | Op op -> // to do
        | Action Clear ->
            let newState = services.initState()
            newState //return
        | Action Equals ->
            let newState = updateDisplayFromPendingOp services state
            newState //return
```

Now to `updateDisplayFromPendingOp`. I spent a few minutes thinking about, and I've come up with the following algorithm for updating the display:

* First, check if there is any pending op. If not, then do nothing.
* Next, try to get the current number from the display. If you can't, then do nothing.
* Next, run the op with the pending number and the current number from the display. If you get an error, then do nothing.
* Finally, update the display with the result and return a new state. 
* The new state also has the pending op set to `None`, as it has been processed.

And here's what that logic looks like in imperative style code:

```fsharp
// First version of updateDisplayFromPendingOp 
// * very imperative and ugly
let updateDisplayFromPendingOp services state =
    if state.pendingOp.IsSome then
        let op,pendingNumber = state.pendingOp.Value
        let currentNumberOpt = services.getDisplayNumber state.display
        if currentNumberOpt.IsSome then
            let currentNumber = currentNumberOpt.Value 
            let result = services.doMathOperation (op,pendingNumber,currentNumber)
            match result with
            | Success resultNumber ->
                let newDisplay = services.setDisplayNumber resultNumber 
                let newState = {display=newDisplay; pendingOp=None}
                newState //return
            | Failure error -> 
                state // original state is untouched
        else
            state // original state is untouched
    else
        state // original state is untouched
```

Ewww! Don't try that at home! 

That code does follow the algorithm exactly, but is really ugly and also error prone (using `.Value` on an option is a code smell).

On the plus side, we did make extensive use of our "services", which has isolated us from the actual implementation details.

So, how can we rewrite it to be more functional?

<a id="bind"></a>

## Bumping into bind

The trick is to recognize that the pattern "if something exists, then act on that value" is exactly the `bind` pattern discussed [here](/posts/computation-expressions-continuations/)
and [here](/rop/).

In order to use the bind pattern effectively, it's a good idea to break the code into many small chunks.

First, the code `if state.pendingOp.IsSome then do something` can be replaced by `Option.bind`. 

```fsharp
let updateDisplayFromPendingOp services state =
    let result =
        state.pendingOp
        |> Option.bind ???
```

But remember that the function has to return a state.
If the overall result of the bind is `None`, then we have *not* created a new state, and we must return the original state that was passed in.

This can be done with the built-in `defaultArg` function which, when applied to an option, returns the option's value if present, or the second parameter if `None`.

```fsharp
let updateDisplayFromPendingOp services state =
    let result =
        state.pendingOp
        |> Option.bind ???
    defaultArg result state
```

You can also tidy this up a bit as well by piping the result directly into `defaultArg`, like this:

```fsharp
let updateDisplayFromPendingOp services state =
    state.pendingOp
    |> Option.bind ???
    |> defaultArg <| state
```

I admit that the reverse pipe for `state` looks strange -- it's definitely an acquired taste!

Onwards! Now what about the parameter to `bind`?  When this is called, we know that pendingOp is present, so we can write a lambda with those parameters, like this:

```fsharp
let result = 
    state.pendingOp
    |> Option.bind (fun (op,pendingNumber) ->
        let currentNumberOpt = services.getDisplayNumber state.display
        // code
        )
```

Alternatively, we could create a local helper function instead, and connect it to the bind, like this:

```fsharp
let executeOp (op,pendingNumber) = 
    let currentNumberOpt = services.getDisplayNumber state.display
    /// etc

let result = 
    state.pendingOp
    |> Option.bind executeOp 
```

I myself generally prefer the second approach when the logic is complicated, as it allows a chain of binds to be simple.
That is, I try to make my code look like:

```fsharp
let doSomething input = return an output option
let doSomethingElse input = return an output option
let doAThirdThing input = return an output option

state.pendingOp
|> Option.bind doSomething
|> Option.bind doSomethingElse
|> Option.bind doAThirdThing
```

Note that in this approach, each helper function has a non-option for input but always must output an *option*.

## Using bind in practice

Once we have the pending op, the next step is to get the current number from the display so we can do the addition (or whatever). 

Rather than having a lot of logic, I'm going keep the helper function (`getCurrentNumber`) simple. 

* The input is the pair (op,pendingNumber)
* The output is the triple (op,pendingNumber,currentNumber) if currentNumber is `Some`, otherwise `None`.

In other words, the signature of `getCurrentNumber` will be `pair -> triple option`, so we can be sure that is usable with the `Option.bind` function.

How to convert the pair into the triple? This can be done just by using `Option.map` to convert the currentNumber option to a triple option.
If the currentNumber is `Some`, then the output of the map is `Some triple`.
On the other hand, if the currentNumber is `None`, then the output of the map is `None` also.

```fsharp
let getCurrentNumber (op,pendingNumber) = 
    let currentNumberOpt = services.getDisplayNumber state.display
    currentNumberOpt 
    |> Option.map (fun currentNumber -> (op,pendingNumber,currentNumber))

let result = 
    state.pendingOp
    |> Option.bind getCurrentNumber
    |> Option.bind ???
```

We can rewrite `getCurrentNumber` to be a bit more idiomatic by using pipes:

```fsharp
let getCurrentNumber (op,pendingNumber) = 
    state.display
    |> services.getDisplayNumber 
    |> Option.map (fun currentNumber -> (op,pendingNumber,currentNumber))
```

Now that we have a triple with valid values, we have everything we need to write a helper function for the math operation.

* It takes a triple as input (the output of `getCurrentNumber`)
* It does the math operation
* It then pattern matches the Success/Failure result and outputs the new state if applicable.

```fsharp
let doMathOp (op,pendingNumber,currentNumber) = 
    let result = services.doMathOperation (op,pendingNumber,currentNumber)
    match result with
    | Success resultNumber ->
        let newDisplay = services.setDisplayNumber resultNumber 
        let newState = {display=newDisplay; pendingOp=None}
        Some newState //return something
    | Failure error -> 
        None // failed
```

Note that, unlike the earlier version with nested ifs, this version returns `Some` on success and `None` on failure.

## Displaying errors

Writing the code for the `Failure` case made me realize something.
If there is a failure, we are not displaying it *at all*, just leaving the display alone. Shouldn't we show an error or something?

Hey, we just found a requirement that got overlooked! This is why I like to create an implementation of the design as soon as possible.
Writing real code that deals with all the cases will invariably trigger a few "what happens in this case?" moments.

So how are we going to implement this new requirement?

In order to do this, we'll need a new "service" that accepts a `MathOperationError` and generates a `CalculatorDisplay`.

```fsharp
type SetDisplayError = MathOperationError -> CalculatorDisplay 
```

and we'll need to add it to the `CalculatorServices` structure too:

```fsharp
type CalculatorServices = {
    // as before
    setDisplayNumber: SetDisplayNumber 
    setDisplayError: SetDisplayError 
    initState: InitState 
    }
```

`doMathOp` can now be altered to use the new service. Both `Success` and `Failure` cases now result in a new display, which in turn is wrapped in a new state.

```fsharp
let doMathOp (op,pendingNumber,currentNumber) = 
    let result = services.doMathOperation (op,pendingNumber,currentNumber)
    let newDisplay = 
        match result with
        | Success resultNumber ->
            services.setDisplayNumber resultNumber 
        | Failure error -> 
            services.setDisplayError error
    let newState = {display=newDisplay;pendingOp=None}
    Some newState //return something
```

I'm going to leave the `Some` in the result, so we can stay with `Option.bind` in the result pipeline*. 

<sub>* An alternative would be to not return `Some`, and then use `Option.map` in the result pipeline</sub><p></p>

Putting it all together, we have the final version of `updateDisplayFromPendingOp`.
Note that I've also added a `ifNone` helper that makes defaultArg better for piping.

```fsharp
// helper to make defaultArg better for piping
let ifNone defaultValue input = 
    // just reverse the parameters!
    defaultArg input defaultValue 

// Third version of updateDisplayFromPendingOp 
// * Updated to show errors on display in Failure case
// * replaces awkward defaultArg syntax
let updateDisplayFromPendingOp services state =
    // helper to extract CurrentNumber
    let getCurrentNumber (op,pendingNumber) = 
        state.display
        |> services.getDisplayNumber 
        |> Option.map (fun currentNumber -> (op,pendingNumber,currentNumber))

    // helper to do the math op
    let doMathOp (op,pendingNumber,currentNumber) = 
        let result = services.doMathOperation (op,pendingNumber,currentNumber)
        let newDisplay = 
            match result with
            | Success resultNumber ->
                services.setDisplayNumber resultNumber 
            | Failure error -> 
                services.setDisplayError error
        let newState = {display=newDisplay;pendingOp=None}
        Some newState //return something

    // connect all the helpers
    state.pendingOp
    |> Option.bind getCurrentNumber
    |> Option.bind doMathOp 
    |> ifNone state // return original state if anything fails
```

## Using a "maybe" computation expression instead of bind

So far, we've being using "bind" directly.  That has helped by removing the cascading `if/else`.

But F# allows you to hide the complexity in a different way, by creating [computation expressions](/posts/computation-expressions-intro/).

Since we are dealing with Options, we can create a "maybe" computation expression that allows clean handling of options.
(If we were dealing with other types, we would need to create a different computation expression for each type).

Here's the definition -- only four lines!
```fsharp
type MaybeBuilder() =
    member this.Bind(x, f) = Option.bind f x
    member this.Return(x) = Some x

let maybe = new MaybeBuilder()
```

With this computation expression available, we can use `maybe` instead of bind, and our code would look something like this:

```fsharp
let doSomething input = return an output option
let doSomethingElse input = return an output option
let doAThirdThing input = return an output option

let finalResult = maybe {
    let! result1 = doSomething
    let! result2 = doSomethingElse result1
    let! result3 = doAThirdThing result2
    return result3
    }
```

In our case, then we can write yet another version of `updateDisplayFromPendingOp` -- our fourth!

```fsharp
// Fourth version of updateDisplayFromPendingOp 
// * Changed to use "maybe" computation expression
let updateDisplayFromPendingOp services state =

    // helper to do the math op
    let doMathOp (op,pendingNumber,currentNumber) = 
        let result = services.doMathOperation (op,pendingNumber,currentNumber)
        let newDisplay = 
            match result with
            | Success resultNumber ->
                services.setDisplayNumber resultNumber 
            | Failure error -> 
                services.setDisplayError error
        {display=newDisplay;pendingOp=None}
        
    // fetch the two options and combine them
    let newState = maybe {
        let! (op,pendingNumber) = state.pendingOp
        let! currentNumber = services.getDisplayNumber state.display
        return doMathOp (op,pendingNumber,currentNumber)
        }
    newState |> ifNone state
```

Note that in *this* implementation, I don't need the `getCurrentNumber` helper any more, as I can just call `services.getDisplayNumber` directly.

So, which of these variants do I prefer?

It depends. 

* If there is a very strong "pipeline" feel, as in [the ROP](/rop/) approach, then I prefer using an explicit `bind`.
* On the other hand, if I am pulling options from many different places, and I want to combine them in various ways, the `maybe` computation expression makes it easier.

So, in this case, I'll go for the last implementation, using `maybe`.

## Implementation: handling math operations

Now we are ready to do the implementation of the math operation case. 

First, if there is a pending operation, the result will be shown on the display, just as for the `Equals` case.
But *in addition*, we need to push the new pending operation onto the state as well.

For the math operation case, then, there will be *two* state transformations, and `createCalculate` will look like this:

```fsharp
let createCalculate (services:CalculatorServices) :Calculate = 
    fun (input,state) -> 
        match input with
        | Digit d -> // as above
        | Op op ->
            let newState1 = updateDisplayFromPendingOp services state
            let newState2 = addPendingMathOp services op newState1 
            newState2 //return
```

We've already defined `updateDisplayFromPendingOp` above.
So we just need `addPendingMathOp` as a helper function to push the operation onto the state.

The algorithm for `addPendingMathOp` is:

* Try to get the current number from the display. If you can't, then do nothing.
* Update the state with the op and current number. 

Here's the ugly version:

```fsharp
// First version of addPendingMathOp 
// * very imperative and ugly
let addPendingMathOp services op state = 
    let currentNumberOpt = services.getDisplayNumber state.display
    if currentNumberOpt.IsSome then 
        let currentNumber = currentNumberOpt.Value 
        let pendingOp = Some (op,currentNumber)
        let newState = {state with pendingOp=pendingOp}
        newState //return
    else                
        state // original state is untouched
```

Again, we can make this more functional using exactly the same techniques we used for `updateDisplayFromPendingOp`.

So here's the more idiomatic version using `Option.map` and a `newStateWithPending` helper function:

```fsharp
// Second version of addPendingMathOp 
// * Uses "map" and helper function
let addPendingMathOp services op state = 
    let newStateWithPending currentNumber =
        let pendingOp = Some (op,currentNumber)
        {state with pendingOp=pendingOp}
        
    state.display
    |> services.getDisplayNumber 
    |> Option.map newStateWithPending 
    |> ifNone state
```

And here's one using `maybe`:

```fsharp
// Third version of addPendingMathOp 
// * Uses "maybe"
let addPendingMathOp services op state = 
    maybe {            
        let! currentNumber = 
            state.display |> services.getDisplayNumber 
        let pendingOp = Some (op,currentNumber)
        return {state with pendingOp=pendingOp}
        }
    |> ifNone state // return original state if anything fails
```

As before, I'd probably go for the last implementation using `maybe`. But the `Option.map` one is fine too. 

## Implementation: review

Now we're done with the implementation part. Let's review the code:

```fsharp
let updateDisplayFromDigit services digit state =
    let newDisplay = services.updateDisplayFromDigit (digit,state.display)
    let newState = {state with display=newDisplay}
    newState //return

let updateDisplayFromPendingOp services state =

    // helper to do the math op
    let doMathOp (op,pendingNumber,currentNumber) = 
        let result = services.doMathOperation (op,pendingNumber,currentNumber)
        let newDisplay = 
            match result with
            | Success resultNumber ->
                services.setDisplayNumber resultNumber 
            | Failure error -> 
                services.setDisplayError error
        {display=newDisplay;pendingOp=None}
        
    // fetch the two options and combine them
    let newState = maybe {
        let! (op,pendingNumber) = state.pendingOp
        let! currentNumber = services.getDisplayNumber state.display
        return doMathOp (op,pendingNumber,currentNumber)
        }
    newState |> ifNone state

let addPendingMathOp services op state = 
    maybe {            
        let! currentNumber = 
            state.display |> services.getDisplayNumber 
        let pendingOp = Some (op,currentNumber)
        return {state with pendingOp=pendingOp}
        }
    |> ifNone state // return original state if anything fails

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

Not bad -- the whole implementation is less than 60 lines of code.

## Summary

We have proved that our design is reasonable by making an implementation -- plus we found a missed requirement.

In the [next post](/posts/calculator-complete-v1/), we'll implement the services and the user interface to create a complete application.

*The code for this post is available in this [gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_implementation-fsx) on GitHub.*