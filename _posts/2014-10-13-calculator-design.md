---
layout: post
title: "Calculator Walkthrough: Part 1"
description: "The type-first approach to designing a Calculator"
categories: ["Worked Examples","DDD"]
seriesId: "Annotated walkthroughs"
seriesOrder: 1

---

One comment I hear often is a complaint about the gap between theory and practice in F# and functional programming in general.
In other words, you know the theory, but how do you actually design and implement an application using FP principles?

So I thought it might be useful to show you how I personally would go about designing and implementing some little applications from beginning to end.

These will be sort of annotated "live coding" sessions. I'll take a problem and start coding it, taking you through my thought process at each stage.
I will make mistakes too, so you'll see how I deal with that, and do backtracking and refactoring.

Please be aware that I'm not claiming that this is production ready code. The code I'm going to show you is more like a exploratory sketch, and as
a result I will do certain bad things (like not testing!) which I would not do for more critical code.

For this first post in the series, I'll be developing a simple pocket calculator app, like this:

![Calculator image](/assets/img/calculator_1.png)

## My development approach

My approach to software development is eclectic and pragmatic -- I like to mix different techniques and alternate between top-down and bottom-up approaches.

Typically I start with the requirements -- I'm a fan of [requirements-driven design](http://fsharpforfunandprofit.com/posts/roman-numeral-kata/)!
Ideally, I would aim to become an expert in the domain as well.

Next, I work on modelling the domain, using [domain-driven design](http://fsharpforfunandprofit.com/ddd/)
with a focus on domain events (["event storming"](http://ziobrando.blogspot.co.uk/2013/11/introducing-event-storming.html)), not just static data ("aggregates" in DDD terminology).

As part of the modelling process, I sketch a design using [type-first development](http://tomasp.net/blog/type-first-development.aspx/)
to [create types](/series/designing-with-types.html) that represent both the domain data types ("nouns") and the domain activities ("verbs").

After doing a first draft of the domain model, I typically switch to a "bottom up" approach and code a small prototype that exercises the model that I have defined so far.

Doing some real coding at this point acts as a reality check. It ensures that the domain model actually makes sense and is not too abstract.
And of course, it often drives more questions about the requirements and domain model,
so I go back to step 1, do some refining and refactoring, and rinse and repeat until I am happy.

(Now if I was working with a team on a large project, at this point we could also start [building a real system incrementally](http://www.growing-object-oriented-software.com/)
and start on the user interface (e.g. with paper prototypes). Both of these activities will typically generate yet more questions and changes in requirements too, so
the whole process is cyclical at all levels.)

So this would be my approach in a perfect world. In practice, of course, the world is not perfect. There is bad management to contend with,
a lack of requirements, silly deadlines and more, all of which mean that I rarely get to use an ideal process.

But in this example, I'm the boss, so if I don't like the result, I've only myself to blame!

## Getting started

So, let's get started. What should we do first?  

Normally I would start with requirements.  But do I *really* need to spend a lot of time writing up requirements for a calculator?

I'm going to be lazy and say no. Instead I'm just to dive in -- I'm confident that I know how a calculator works.
(*As you'll see later, I was wrong! Trying to write up the requirements would have been a good exercise, as there are some interesting edge cases.*)

So let's start with the type-first design instead. 

In my designs, every use-case is a function, with one input and one output. 

For this example then, we need to model the public interface to the Calculator as a function. Here's the signature:

```fsharp
type Calculate = CalculatorInput -> CalculatorOutput
```

That was easy!  The first question then is: are there any other use-cases that we need to model?
I think for now, no. We'll just start with a single case that handles all the inputs.


## Defining the input and output to the function

But now we have created two new types, `CalculatorInput` and `CalculatorOutput`, that are undefined
(and if you type this into a F# script file, you'll have red squigglies to remind you).
We'd better define those now.

Before moving on, I should make it very clear that the input and output types for this function are going to be pure and clean.
When designing our domain we never want to be dealing with the messy world of strings, primitive datatypes, validation, and so on.

Instead there will typically be a validation/transformation function that converts from the messy untrusted world into our lovely, pristine domain on the way in,
and another similar function that does the reverse on the way out.

![Domain input and output](/assets/img/domain_input_output.png)


Ok, let's work on the `CalculatorInput` first. What would the structure of the input look like?
 
First, obviously, there will be some keystrokes, or some other way of communicating the intent of the user.
But also, since the calculator is stateless, we need to pass in some state as well. This state would contain, for example, the digits typed in so far.

As to the output, the function will have to emit a new, updated state, of course.

But do we need anything else, such as a structure containing formatted output for display? I don't think we do.
We want to isolate ourselves from the display logic, so we'll just let the UI turn the state into
something that can be displayed.

What about errors? In [other posts](/rop/), I have spent a lot of time talking about error handling. Is it needed in this case?

In this case, I think not. In a cheap pocket calculator, any errors are shown right in the display, so we'll stick with that approach for now.

So here's the new version of the function:

```fsharp
type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
```

`CalculatorInput` now means the keystrokes or whatever, and `CalculatorState` is the state.

Notice that I have defined this function using a [tuple](/posts/tuples/) (`CalculatorInput * CalculatorState`) as input,
rather than as two separate parameters (which would look like `CalculatorInput -> CalculatorState -> CalculatorState`).
I did this because both parameters are always needed and a tuple makes this clear -- I don't want to be partially applying the input, for example.

In fact I do this for all functions when doing type-first design. Every function has one input and one output.
This doesn't mean that there might not be potential for doing partial application later, just that, at the design stage, I only want one parameter.

Also note that things that are not part of the pure domain (such as configuration and connection strings) will *never* be shown at this stage,
although, at implementation time, they will of course be added to the functions that implement the design.

## Defining the CalculatorState type

Now let's look at the `CalculatorState`. All I can think of that we need right now is something to hold the information to display.

```fsharp
type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
and CalculatorState = {
    display: CalculatorDisplay
    }
```

I've defined a type `CalculatorDisplay`, firstly as documentation to make it clear what the field value is used for,
and secondly, so I can postpone deciding what the display actually is!

So what should the type of the display be? A float? A string? A list of characters? A record with multiple fields?

Well, I'm going to go for `string`, because, as I said above, we might need to display errors. 

```fsharp
type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
and CalculatorState = {
    display: CalculatorDisplay
    }
and CalculatorDisplay = string
```

Notice that I am using `and` to connect the type definitions together. Why?

Well, F# compiles from top to bottom, so you must define a type before it is used. The following code will not compile:

```fsharp
type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
type CalculatorState = {
    display: CalculatorDisplay
    }
type CalculatorDisplay = string
```

I could fix this by changing the order of the declarations,
but since I am in "sketch" mode, and I don't want to reorder things all the time,
I will just append new declarations to the bottom and use `and` to connect them.

In the final production code though, when the design has stabilized, I *would* reorder these types to avoid using `and`.
The reason is that `and` can [hide cycles between types](/posts/cyclic-dependencies/) and prevent refactoring.

## Defining the CalculatorInput type

For the `CalculatorInput` type, I'll just list all the buttons on the calculator!

```fsharp
// as above
and CalculatorInput = 
    | Zero | One | Two | Three | Four 
    | Five | Six | Seven | Eight | Nine
    | DecimalSeparator
    | Add | Subtract | Multiply | Divide
    | Equals | Clear
```

Some people might say: why not use a `char` as the input? But as I explained above, in my domain I only want to deal with ideal data.
By using a limited set of choices like this, I never have to deal with unexpected input.

Also, a side benefit of using abstract types rather than chars is that `DecimalSeparator` is not assumed to be ".".
The actual separator should be obtained by first getting the current culture (`System.Globalization.CultureInfo.CurrentCulture`)
and then using `CurrentCulture.NumberFormat.CurrencyDecimalSeparator` to get the separator. By hiding this implementation detail from the design,
changing the actual separator used will have minimal effect on the code.

## Refining the design: handling digits

So that's a first pass at the design done. Now let's dig deeper and define some of the internal processes.

Let's start with how the digits are handled.

When a digit key is pressed, we want to append the digit to the current display. Let's define a function type that represents that:

```fsharp
type UpdateDisplayFromDigit = CalculatorDigit * CalculatorDisplay -> CalculatorDisplay
```

The `CalculatorDisplay` type is the one we defined earlier, but what is this new `CalculatorDigit` type?

Well obviously we need some type to represent all the possible digits that can be used as input.
Other inputs, such as `Add` and `Clear`, would not be valid for this function.

```fsharp
type CalculatorDigit = 
    | Zero | One | Two | Three | Four 
    | Five | Six | Seven | Eight | Nine
    | DecimalSeparator
```

So the next question is, how do we get a value of this type? Do we need a function that maps a `CalculatorInput` to a `CalculatorDigit` type, like this?

```fsharp
let convertInputToDigit (input:CalculatorInput) =
    match input with
        | Zero -> CalculatorDigit.Zero
        | One -> CalculatorDigit.One
        | etc
        | Add -> ???
        | Clear -> ???
```

In many situations, this might be necessary, but in this case it seems like overkill.
And also, how would this function deal with non-digits such as `Add` and `Clear`?

So let's just redefine the `CalculatorInput` type to use the new type directly:

```fsharp
type CalculatorInput = 
    | Digit of CalculatorDigit
    | Add | Subtract | Multiply | Divide
    | Equals | Clear
```

While we're at it, let's classify the other buttons as well.

I would classify `Add | Subtract | Multiply | Divide` as math operations,
and as for `Equals | Clear`, I'll just call them "actions" for lack of better word.

Here's the complete refactored design with new types `CalculatorDigit`, `CalculatorMathOp` and `CalculatorAction`:

```fsharp
type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
and CalculatorState = {
    display: CalculatorDisplay
    }
and CalculatorDisplay = string
and CalculatorInput = 
    | Digit of CalculatorDigit
    | Op of CalculatorMathOp
    | Action of CalculatorAction
and CalculatorDigit = 
    | Zero | One | Two | Three | Four 
    | Five | Six | Seven | Eight | Nine
    | DecimalSeparator
and CalculatorMathOp = 
    | Add | Subtract | Multiply | Divide
and CalculatorAction = 
    | Equals | Clear

type UpdateDisplayFromDigit = CalculatorDigit * CalculatorDisplay -> CalculatorDisplay    
```

This is not the only approach. I could have easily left `Equals` and `Clear` as separate choices.

Now let's revisit `UpdateDisplayFromDigit` again. Do we need any other parameters? For example, do we need any other part of the state?

No, I can't think of anything else. When defining these functions, I want to be as minimal as possible. Why pass in the whole calculator state if you only need the display?

Also, would `UpdateDisplayFromDigit` ever return an error? For example, surely we can't add digits indefinitely -- what happens when we are not allowed to?
And is there some other combination of inputs that might cause an error? For example, inputting nothing but decimal separators! What happens then?

For this little project, I will assume that neither of these will create an explicit error, but instead, bad input will be rejected silently.
In other words, after 10 digits, say, other digits will be ignored. And after the first decimal separator, subsequent ones will be ignored as well.

Alas, I cannot encode these requirements in the design. But that fact that `UpdateDisplayFromDigit`
does not return any explicit error type *does* at least tell me that errors will be handled silently.

## Refining the design: the math operations

Now let's move on to the math operations.  

These are all binary operations, taking two numbers and spitting out a new result. 

A function type to represent this would look like this:

```fsharp
type DoMathOperation = CalculatorMathOp * Number * Number -> Number
```

If there were unary operations as well, such as `1/x`, we would need a different type for those, but we don't, so we can keep things simple.

Next decision: what numeric type should we use? Should we make it generic?  

Again, let's just keep it simple and use `float`. But we'll keep the `Number` alias around to decouple the representation a bit. Here's the updated code:

```fsharp
type DoMathOperation = CalculatorMathOp * Number * Number -> Number
and Number = float
```


Now let's ponder `DoMathOperation`, just as we did for `UpdateDisplayFromDigit` above. 

Question 1: Is this the minimal set of parameters? For example, do we need any other part of the state?

Answer: No, I can't think of anything else. 

Question 2: Can `DoMathOperation` ever return an error? 

Answer: Yes! What about dividing by zero?  

So how should we handle errors?
Let's create a new type that represents a result of a math operation, and make that the output of `DoMathOperation`:

The new type, `MathOperationResult` will have two choices (discriminated union) between `Success` and `Failure`.

```fsharp
type DoMathOperation = CalculatorMathOp * Number * Number -> MathOperationResult 
and Number = float
and MathOperationResult = 
    | Success of Number 
    | Failure of MathOperationError
and MathOperationError = 
    | DivideByZero
```

We could have also used the built-in generic `Choice` type, or even a full ["railway oriented programming"](/rop/) approach, but since this is a sketch of the design,
I want the design to stand alone, without a lot of dependencies, so I'll just define the specific type right here.

Any other errors? NaNs or underflows or overflows? I'm not sure. We have the `MathOperationError` type, and it would be easy to extend it as needed.

## Where do numbers come from?

We've defined `DoMathOperation` to use `Number` values as input. But where does a `Number` come from? 

Well they come from the sequence of digits that have been entered -- converting the digits into a float. 

One approach would be to store a `Number` in the state along with the string display, and update it as each digit comes in.

I'm going to take a simpler approach, and just get the number from the display directly. In other words, we need a function that looks like this:

```fsharp
type GetDisplayNumber = CalculatorDisplay -> Number
```

Thinking about it though, the function could fail, because the display string could be "error" or something. So let's return an option instead.
 
```fsharp
type GetDisplayNumber = CalculatorDisplay -> Number option
```

Similarly, when we *do* have a successful result, we will want to display it, so we need a function that works in the other direction:

```fsharp
type SetDisplayNumber = Number -> CalculatorDisplay 
```

This function can never error (I hope), so we don't need the `option`.

## Refining the design: handling a math operation input

We're not done with math operations yet, though!

What is the visible effect when the input is `Add`? None!

The `Add` event needs another number to be entered later, so the `Add` event is somehow kept pending,
waiting for the next number.

If you think about, we not only have to keep the `Add` event pending, but also the previous number, ready to be added to the latest number that is input.

Where will we keep track of this? In the `CalculatorState` of course!

Here's our first attempt to add the new fields:

```fsharp
and CalculatorState = {
    display: CalculatorDisplay
    pendingOp: CalculatorMathOp 
    pendingNumber: Number
    }
```

But sometimes there isn't a pending operation, so we have to make it optional:

```fsharp
and CalculatorState = {
    display: CalculatorDisplay
    pendingOp: CalculatorMathOp option
    pendingNumber: Number option
    }
```

But this is wrong too!  Can we have a `pendingOp` without a `pendingNumber`, or vice versa? No. They live and die together.

This implies that the state should contain a pair, and the whole pair is optional, like this:

```fsharp
and CalculatorState = {
    display: CalculatorDisplay
    pendingOp: (CalculatorMathOp * Number) option
    }
```

But now we are still missing a piece. If the operation is added to the state as pending,
when does the operation actually get *run* and the result displayed?

Answer: when the `Equals` button is pushed, or indeed any another math op button. We'll deal with that later.

## Refining the design: handling the Clear button

We've got one more button to handle, the `Clear` button. What does it do?

Well, it obviously just resets the state so that the display is empty and any pending operations are removed.

I'm going to call this function `InitState` rather than "clear", and here is its signature:

```fsharp
type InitState = unit -> CalculatorState 
```

## Defining the services

At this point, we have everything we need to switch to bottom up development.
I'm eager to try building a trial implementation of the `Calculate` function, to see if the design is usable, and if we've missed anything.

But how can I create a trial implementation without implementing the whole thing?

This is where all these types come in handy. We can define a set of "services" that the `calculate` function will use, but without actually implementing them!

Here's what I mean:

```fsharp
type CalculatorServices = {
    updateDisplayFromDigit: UpdateDisplayFromDigit 
    doMathOperation: DoMathOperation 
    getDisplayNumber: GetDisplayNumber 
    setDisplayNumber: SetDisplayNumber 
    initState: InitState 
    }
```

We've created a set of services that can be injected into an implementation of the `Calculate` function.
With these in place, we can code the `Calculate` function immediately and deal with the implementation of the services later. 

At this point, you might be thinking that this seems like overkill for a tiny project.

It's true -- we don't want this to turn into [FizzBuzz Enterprise Edition](https://github.com/EnterpriseQualityCoding/FizzBuzzEnterpriseEdition)!

But I'm demonstrating a principle here. By separating the "services" from the core code, you can start prototyping immediately.
The goal is not to make a production ready codebase, but to find any issues in the design. We are still in the requirements discovery phase. 

This approach should not be unfamiliar to you -- it is directly equivalent to the OO principle of
creating a bunch of interfaces for services and then injecting them into the core domain. 

## Review

So let's review -- with the addition of the services, our initial design is complete.
Here is all the code so far:

```fsharp
type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
and CalculatorState = {
    display: CalculatorDisplay
    pendingOp: (CalculatorMathOp * Number) option
    }
and CalculatorDisplay = string
and CalculatorInput = 
    | Digit of CalculatorDigit
    | Op of CalculatorMathOp
    | Action of CalculatorAction
and CalculatorDigit = 
    | Zero | One | Two | Three | Four 
    | Five | Six | Seven | Eight | Nine
    | DecimalSeparator
and CalculatorMathOp = 
    | Add | Subtract | Multiply | Divide
and CalculatorAction = 
    | Equals | Clear
and UpdateDisplayFromDigit = 
    CalculatorDigit * CalculatorDisplay -> CalculatorDisplay
and DoMathOperation = 
    CalculatorMathOp * Number * Number -> MathOperationResult 
and Number = float
and MathOperationResult = 
    | Success of Number 
    | Failure of MathOperationError
and MathOperationError = 
    | DivideByZero

type GetDisplayNumber = 
    CalculatorDisplay -> Number option
type SetDisplayNumber = 
    Number -> CalculatorDisplay 

type InitState = 
    unit -> CalculatorState 

type CalculatorServices = {
    updateDisplayFromDigit: UpdateDisplayFromDigit 
    doMathOperation: DoMathOperation 
    getDisplayNumber: GetDisplayNumber 
    setDisplayNumber: SetDisplayNumber 
    initState: InitState 
    }
```


## Summary

I think that this is quite nice. We haven't written any "real" code yet, but with a bit of thought, we have already built quite a detailed design.

In the [next post](/posts/calculator-implementation), I'll put this design to the test by attempting to create an implementation.

*The code for this post is available in this [gist](https://gist.github.com/swlaschin/0e954cbdc383d1f5d9d3#file-calculator_design-fsx) on GitHub.*

