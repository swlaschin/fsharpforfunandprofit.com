---
layout: post
title: "Calculator Walkthrough: Part 3"
description: "Adding the services and user interface, and dealing with disaster"
categories: ["Worked Examples"]
seriesId: "Annotated walkthroughs"
seriesOrder: 3
---

In this post, I'll continue developing a simple pocket calculator app.

In the [first post](/posts/calculator-design/), we completed a first draft of the design, using only types (no UML diagrams!).
and in the [previous post](/posts/calculator-implementation/), we created an initial implementation that exercised the design and revealed a missing requirement.

Now it's time to build the remaining components and assemble them into a complete application

## Creating the services

We have a implementation. But the implementation depends on some services, and we haven't created the services yet.

In practice though, this bit is very easy and straightforward. The types defined in the domain enforce constraints
such there is really only one way of writing the code.

I'm going to show all the code at once (below), and I'll add some comments afterwards.

```fsharp
// ================================================
// Implementation of CalculatorConfiguration
// ================================================          
module CalculatorConfiguration =

    // A record to store configuration options
    // (e.g. loaded from a file or environment)
    type Configuration = {
        decimalSeparator : string
        divideByZeroMsg : string
        maxDisplayLength: int
        }

    let loadConfig() = {
        decimalSeparator = 
            System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator
        divideByZeroMsg = "ERR-DIV0" 
        maxDisplayLength = 10
        }
        
// ================================================
// Implementation of CalculatorServices 
// ================================================          
module CalculatorServices =
    open CalculatorDomain
    open CalculatorConfiguration

    let updateDisplayFromDigit (config:Configuration) :UpdateDisplayFromDigit = 
        fun (digit, display) ->

        // determine what character should be appended to the display
        let appendCh= 
            match digit with
            | Zero -> 
                // only allow one 0 at start of display
                if display="0" then "" else "0"
            | One -> "1"
            | Two -> "2"
            | Three-> "3"
            | Four -> "4"
            | Five -> "5"
            | Six-> "6"
            | Seven-> "7"
            | Eight-> "8"
            | Nine-> "9"
            | DecimalSeparator -> 
                if display="" then 
                    // handle empty display with special case
                    "0" + config.decimalSeparator  
                else if display.Contains(config.decimalSeparator) then 
                    // don't allow two decimal separators
                    "" 
                else 
                    config.decimalSeparator
        
        // ignore new input if there are too many digits
        if (display.Length > config.maxDisplayLength) then
            display // ignore new input
        else
            // append the new char
            display + appendCh

    let getDisplayNumber :GetDisplayNumber = fun display ->
        match System.Double.TryParse display with
        | true, d -> Some d
        | false, _ -> None

    let setDisplayNumber :SetDisplayNumber = fun f ->
        sprintf "%g" f

    let setDisplayError divideByZeroMsg :SetDisplayError = fun f ->
        match f with
        | DivideByZero -> divideByZeroMsg

    let doMathOperation  :DoMathOperation = fun (op,f1,f2) ->
        match op with
        | Add -> Success (f1 + f2)
        | Subtract -> Success (f1 - f2)
        | Multiply -> Success (f1 * f2)
        | Divide -> 
            try
                Success (f1 / f2)
            with
            | :? System.DivideByZeroException -> 
                Failure DivideByZero 

    let initState :InitState = fun () -> 
        {
        display=""
        pendingOp = None
        }

    let createServices (config:Configuration) = {
        updateDisplayFromDigit = updateDisplayFromDigit config
        doMathOperation = doMathOperation
        getDisplayNumber = getDisplayNumber
        setDisplayNumber = setDisplayNumber
        setDisplayError = setDisplayError (config.divideByZeroMsg)
        initState = initState
        }
```

Some comments:

* I have created a configuration record that stores properties that are used to parameterize the services, such as the decimal separator.
* The configuration record is passed into the `createServices` function, which in turn passes the configuration on those services that need it.
* All the functions use the same approach of returning one of the types defined in the design, such as `UpdateDisplayFromDigit` or `DoMathOperation`.
* There are only a few tricky edge cases, such as trapping exceptions in division, or preventing more than one decimal separator being appended.


## Creating the user interface

For the user interface, I'm going to use WinForms rather than WPF or a web-based approach. It's simple and should work on Mono/Xamarin as well as Windows.
And it should be easy to port to other UI frameworks as well.

As is typical with UI development I spent more time on this than on any other part of the process!
I'm going to spare you all the painful iterations and just go directly to the final version.

I won't show all the code, as it is about 200 lines (and you can see it in the [gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v1-fsx)), but here are some highlights:

```fsharp
module CalculatorUI =

    open CalculatorDomain

    type CalculatorForm(initState:InitState, calculate:Calculate) as this = 
        inherit Form()

        // initialization before constructor
        let mutable state = initState()
        let mutable setDisplayedText = 
            fun text -> () // do nothing
```

The `CalculatorForm` is a subclass of `Form`, as usual. 

There are two parameters for its constructor.
One is `initState`, the function that creates an empty state, and `calculate`, the function that transforms the state based on the input.
In other words, I'm using standard constructor based dependency injection here.

There are two mutable fields (shock horror!). 

One is the state itself. Obviously, it will be modified after each button is pressed.

The second is a function called `setDisplayedText`. What's that all about?

Well, after the state has changed, we need to refresh the control (a Label) that displays the text.

The standard way to do it is to make the label control a field in the form, like this:

```fsharp
type CalculatorForm(initState:InitState, calculate:Calculate) as this = 
    inherit Form()

    let displayControl :Label = null
```

and then set it to an actual control value when the form has been initialized:

```fsharp
member this.CreateDisplayLabel() = 
    let display = new Label(Text="",Size=displaySize,Location=getPos(0,0))
    display.TextAlign <- ContentAlignment.MiddleRight
    display.BackColor <- Color.White
    this.Controls.Add(display)

    // traditional style - set the field when the form has been initialized
    displayControl <- display
```

But this has the problem that you might accidentally try to access the label control before it is initialized, causing a NRE.
Also, I'd prefer to focus on the desired behavior, rather than having a "global" field that can be accessed by anyone anywhere.

By using a function, we (a) encapsulate the access to the real control and (b) avoid any possibility of a null reference.

The mutable function starts off with a safe default implementation (`fun text -> ()`),
and is then changed to a *new* implementation when the label control is created:

```fsharp
member this.CreateDisplayLabel() = 
    let display = new Label(Text="",Size=displaySize,Location=getPos(0,0))
    this.Controls.Add(display)

    // update the function that sets the text
    setDisplayedText <-
        (fun text -> display.Text <- text)
```


## Creating the buttons

The buttons are laid out in a grid, and so I create a helper function `getPos(row,col)` that gets the physical position from a logical (row,col) on the grid.

Here's an example of creating the buttons:

```fsharp
member this.CreateButtons() = 
    let sevenButton = new Button(Text="7",Size=buttonSize,Location=getPos(1,0),BackColor=DigitButtonColor)
    sevenButton |> addDigitButton Seven

    let eightButton = new Button(Text="8",Size=buttonSize,Location=getPos(1,1),BackColor=DigitButtonColor)
    eightButton |> addDigitButton Eight

    let nineButton = new Button(Text="9",Size=buttonSize,Location=getPos(1,2),BackColor=DigitButtonColor)
    nineButton |> addDigitButton Nine

    let clearButton = new Button(Text="C",Size=buttonSize,Location=getPos(1,3),BackColor=DangerButtonColor)
    clearButton |> addActionButton Clear

    let addButton = new Button(Text="+",Size=doubleHeightSize,Location=getPos(1,4),BackColor=OpButtonColor)
    addButton |> addOpButton Add
```

And since all the digit buttons have the same behavior, as do all the math op buttons, I just created some helpers that set the event handler in a generic way:

```fsharp
let addDigitButton digit (button:Button) =
    button.Click.AddHandler(EventHandler(fun _ _ -> handleDigit digit))
    this.Controls.Add(button)

let addOpButton op (button:Button) =
    button.Click.AddHandler(EventHandler(fun _ _ -> handleOp op))
    this.Controls.Add(button)
```

I also added some keyboard support:

```fsharp
member this.KeyPressHandler(e:KeyPressEventArgs) =
    match e.KeyChar with
    | '0' -> handleDigit Zero
    | '1' -> handleDigit One
    | '2' -> handleDigit Two
    | '.' | ',' -> handleDigit DecimalSeparator
    | '+' -> handleOp Add
    // etc
```

Button clicks and keyboard presses are eventually routed into the key function `handleInput`, which does the calculation.

```fsharp
let handleInput input =
     let newState = calculate(input,state)
     state <- newState 
     setDisplayedText state.display 
    
let handleDigit digit =
     Digit digit |> handleInput 

let handleOp op =
     Op op |> handleInput 
```

As you can see, the implementation of `handleInput` is trivial.
It calls the calculation function that was injected, sets the mutable state to the result, and then updates the display.

So there you have it -- a complete calculator!

Let's try it now -- get the code from this [gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v1-fsx) and try running it as a F# script.

## Disaster strikes!

Let's start with a simple test. Try entering `1` `Add` `2` `Equals`. What would you expect?

I don't know about you, but what I *wouldn't* expect is that the calculator display shows `12`!

What's going on? Some quick experimenting shows that I have forgotten something really important --
when an `Add` or `Equals` operation happens, any subsequent digits should *not* be added to the current buffer, but instead start a new one.
Oh no! We've got a showstopper bug!

Remind me again, what idiot said "if it compiles, it probably works".* 

<sub>* Actually, that idiot would be me (among many others).</sub>

So what went wrong then? 

Well the code did compile, but it didn't work as expected, not because the code was buggy, but because *my design was flawed*.

In other words, the use of the types from the type-first design process means that I *do* have high confidence that the code I wrote is a correct implementation of the design.
But if the requirements and design are wrong, all the correct code in the world can't fix that.

We'll revisit the requirements in the next post, but meanwhile, is there a patch we can make that will fix the problem?

## Fixing the bug

Let's think of the circumstances when we start a new set of digits, vs. when we just append to the existing ones.
As we noted above, a math operation or `Equals` will force the reset.  

So why not set a flag when those operations happen? If the flag is set, then start a new display buffer,
and after that, unset the flag so that characters are appended as before.

What changes do we need to make to the code?

First, we need to store the flag somewhere. We'll store it in the `CalculatorState` of course!

```fsharp
type CalculatorState = {
    display: CalculatorDisplay
    pendingOp: (CalculatorMathOp * Number) option
    allowAppend: bool
    }
```

(*This might seem like a good solution for now, but using flags like this is really a design smell.
In the next post, I'll use a [different approach](/posts/designing-with-types-representing-states/#replace-flags) which doesn't involve flags)*

## Fixing the implementation

With this change made, compiling the `CalculatorImplementation` code now breaks everywhere a new state is created.

Actually, that's what I like about using F# -- something like adding a new field to a record is a breaking change, rather than
something that can be overlooked by mistake.

We'll make the following tweaks to the code:

* For `updateDisplayFromDigit`, we return a new state with `allowAppend` set to true.
* For `updateDisplayFromPendingOp` and `addPendingMathOp`, we return a new state with `allowAppend` set to false.

## Fixing the services

Most of the services are fine. The only service that is broken now is `initState`, which just needs to be tweaked to have `allowAppend` be true when starting.

```fsharp
let initState :InitState = fun () -> 
    {
    display=""
    pendingOp = None
    allowAppend = true
    }
```

## Fixing the user interface

The `CalculatorForm` class continues to work with no changes.

But this change does raise the question of how much the `CalculatorForm` should know about the internals of the `CalculatorDisplay` type.

Should `CalculatorDisplay` be transparent, in which case the form might break every time we change the internals?

Or should `CalculatorDisplay` be an opaque type, in which case we will need to add another "service" that extracts the buffer from the `CalculatorDisplay` type so that the form
can display it?

For now, I'm happy to tweak the form if there are changes. But in a bigger or more long-term project,
when we are trying to reduce dependencies, then yes, I would make the domain types opaque as much as possible to reduce the fragility of the design.

## Testing the patched version

Let's try out the patched version now (*you can get the code for the patched version from this [gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v1_patched-fsx)*).

Does it work now? 

Yes. Entering `1` `Add` `2` `Equals` results in `3`, as expected.

So that fixes the major bug. Phew.

But if you keep playing around with this implementation, you will encounter other <strike>bugs</strike> undocumented features too.

For example:

* `1.0 / 0.0` displays `Infinity`. What happened to our divide by zero error?
* You get strange behaviors if you enter operations in unusual orders. For example, entering `2 + + -` shows `8` on the display! 

So obviously, this code is not yet fit for purpose.


## What about Test-Driven Development?

At this point, you might be saying to yourself: "if only he had used TDD this wouldn't have happened". 

It's true -- I wrote all this code, and yet I didn't even bother to write a test that checked whether you could add two numbers properly!

If I had started out by writing tests, and letting that drive the design, then surely I wouldn't have run into this problem.

Well in this particular example, yes, I would probably would have caught the problem immediately. 
In a TDD approach, checking that `1 + 2 = 3` would have been one of the first tests I wrote!
But on the other hand, for obvious flaws like this, any interactive testing will reveal the issue too. 

To my mind, the advantages of test-driven development are that:

* it drives the *design* of the code, not just the implementation.
* it provides guarantees that code stays correct during refactoring.

So the real question is, would test-driven development help us find missing requirements or subtle edge cases?
Not necessarily. Test-driven development will only be effective if we can think of every possible case that could happen in the first place.
In that sense, TDD would not make up for a lack of imagination!

And if do have good requirements, then hopefully we can design the types to [make illegal states unrepresentable](/posts/designing-with-types-making-illegal-states-unrepresentable/)
and then we won't need the tests to provide correctness guarantees.

Now I'm not saying that I am against automated testing. In fact, I do use it all the time to verify certain requirements, and especially for integration and testing in the large.

So, for example, here is how I might test this code:

```fsharp
module CalculatorTests =
    open CalculatorDomain
    open System

    let config = CalculatorConfiguration.loadConfig()
    let services = CalculatorServices.createServices config 
    let calculate = CalculatorImplementation.createCalculate services

    let emptyState = services.initState()

    /// Given a sequence of inputs, start with the empty state
    /// and apply each input in turn. The final state is returned
    let processInputs inputs = 
        // helper for fold
        let folder state input = 
            calculate(input,state)

        inputs 
        |> List.fold folder emptyState 

    /// Check that the state contains the expected display value
    let assertResult testLabel expected state =
        let actual = state.display
        if (expected <> actual) then
            printfn "Test %s failed: expected=%s actual=%s" testLabel expected actual 
        else
            printfn "Test %s passed" testLabel 

    let ``when I input 1 + 2, I expect 3``() = 
        [Digit One; Op Add; Digit Two; Action Equals]
        |> processInputs 
        |> assertResult "1+2=3" "3"

    let ``when I input 1 + 2 + 3, I expect 6``() = 
        [Digit One; Op Add; Digit Two; Op Add; Digit Three; Action Equals]
        |> processInputs 
        |> assertResult "1+2+3=6" "6"

    // run tests
    do 
        ``when I input 1 + 2, I expect 3``()
        ``when I input 1 + 2 + 3, I expect 6``() 
```

And of course, this would be easily adapted to using [NUnit or similar](/posts/low-risk-ways-to-use-fsharp-at-work-3/). 

## How can I develop a better design?

I messed up! As I said earlier, the *implementation itself* was not the problem. I think the type-first design process worked.
The real problem was that I was too hasty and just dived into the design without really understanding the requirements.

How can I prevent this from happening again next time?

One obvious solution would be to switch to a proper TDD approach.
But I'm going to be a bit stubborn, and see if I can stay with a type-first design! 

[In the next post](/posts/calculator-complete-v2/), I will stop being so ad-hoc and over-confident,
and instead use a process that is more thorough and much more likely to prevent these kinds of errors at the design stage.

*The code for this post is available on GitHub in [this gist (unpatched)](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v1-fsx)
and [this gist (patched)](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_v1_patched-fsx).*



