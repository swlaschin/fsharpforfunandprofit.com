---
layout: post
title: "Using functions as building blocks"
description: "Function composition and mini-languages make code more readable"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 11
categories: [Conciseness, Functions]
---

A well-known principle of good design is to create a set of basic operations and then combine these building blocks in various ways to build up more complex behaviors. In object-oriented languages, this goal gives rise to a number of implementation approaches such as "fluent interfaces", "strategy pattern", "decorator pattern", and so on. In F#, they are all done the same way, via function composition. 

Let's start with a simple example using integers. Say that we have created some basic functions to do arithmetic:

```fsharp
// building blocks
let add2 x = x + 2
let mult3 x = x * 3
let square x = x * x

// test
[1..10] |> List.map add2 |> printfn "%A"
[1..10] |> List.map mult3 |> printfn "%A"
[1..10] |> List.map square |> printfn "%A" 
```

Now we want to create new functions that build on these:

```fsharp
// new composed functions
let add2ThenMult3 = add2 >> mult3
let mult3ThenSquare = mult3 >> square 
```

The "`>>`" operator is the composition operator. It means: do the first function, and then do the second. 

Note how concise this way of combining functions is. There are no parameters, types or other irrelevant noise.

To be sure, the examples could also have been written less concisely and more explicitly as:

```fsharp
let add2ThenMult3 x = mult3 (add2 x)
let mult3ThenSquare x = square (mult3 x) 
```

But this more explicit style is also a bit more cluttered:

* In the explicit style, the x parameter and the parentheses must be added, even though they don't add to the meaning of the code.
* And in the explicit style, the functions are written back-to-front from the order they are applied. In my example of `add2ThenMult3` I want to add 2 first, and then multiply. The `add2 >> mult3` syntax makes this visually clearer than `mult3(add2 x)`.

Now let's test these compositions:

```fsharp
// test
add2ThenMult3 5
mult3ThenSquare 5
[1..10] |> List.map add2ThenMult3 |> printfn "%A"
[1..10] |> List.map mult3ThenSquare |> printfn "%A"
```

## Extending existing functions

Now say that we want to decorate these existing functions with some logging behavior. We can compose these as well, to make a new function with the logging built in.

```fsharp
// helper functions;
let logMsg msg x = printf "%s%i" msg x; x     //without linefeed 
let logMsgN msg x = printfn "%s%i" msg x; x   //with linefeed

// new composed function with new improved logging!
let mult3ThenSquareLogged = 
   logMsg "before=" 
   >> mult3 
   >> logMsg " after mult3=" 
   >> square
   >> logMsgN " result=" 

// test
mult3ThenSquareLogged 5
[1..10] |> List.map mult3ThenSquareLogged //apply to a whole list
```

Our new function, `mult3ThenSquareLogged`, has an ugly name, but it is easy to use and nicely hides the complexity of the functions that went into it. You can see that if you define your building block functions well, this composition of functions can be a powerful way to get new functionality.

But wait, there's more!  Functions are first class entities in F#, and can be acted on by any other F# code. Here is an example of using the composition operator to collapse a list of functions into a single operation.

```fsharp
let listOfFunctions = [
   mult3; 
   square;
   add2;
   logMsgN "result=";
   ]

// compose all functions in the list into a single one
let allFunctions = List.reduce (>>) listOfFunctions 

//test
allFunctions 5
```

## Mini languages

Domain-specific languages (DSLs) are well recognized as a technique to create more readable and concise code. The functional approach is very well suited for this.

If you need to, you can go the route of having a full "external" DSL with its own lexer, parser, and so on, and there are various toolsets for F# that make this quite straightforward.

But in many cases, it is easier to stay within the syntax of F#, and just design a set of "verbs" and "nouns" that encapsulate the behavior we want.

The ability to create new types concisely and then match against them makes it very easy to set up fluent interfaces quickly. For example, here is a little function that calculates dates using a simple vocabulary. Note that two new enum-style types are defined just for this one function.

```fsharp
// set up the vocabulary
type DateScale = Hour | Hours | Day | Days | Week | Weeks
type DateDirection = Ago | Hence

// define a function that matches on the vocabulary
let getDate interval scale direction =
    let absHours = match scale with
                   | Hour | Hours -> 1 * interval
                   | Day | Days -> 24 * interval
                   | Week | Weeks -> 24 * 7 * interval
    let signedHours = match direction with
                      | Ago -> -1 * absHours 
                      | Hence ->  absHours 
    System.DateTime.Now.AddHours(float signedHours)

// test some examples
let example1 = getDate 5 Days Ago
let example2 = getDate 1 Hour Hence

// the C# equivalent would probably be more like this:
// getDate().Interval(5).Days().Ago()
// getDate().Interval(1).Hour().Hence()
```

The example above only has one "verb", using lots of types for the "nouns".

The following example demonstrates how you might build the functional equivalent of a fluent interface with many "verbs". 

Say that we are creating a drawing program with various shapes. Each shape has a color, size, label and action to be performed when clicked, and we want a fluent interface to configure each shape.

Here is an example of what a simple method chain for a fluent interface in C# might look like:

```fsharp
FluentShape.Default
   .SetColor("red")
   .SetLabel("box")
   .OnClick( s => Console.Write("clicked") );
```

Now the concept of "fluent interfaces" and "method chaining" is really only relevant for object-oriented design. In a functional language like F#, the nearest equivalent would be the use of the pipeline operator to chain a set of functions together.

Let's start with the underlying Shape type:

```fsharp
// create an underlying type
type FluentShape = {
    label : string; 
    color : string; 
    onClick : FluentShape->FluentShape // a function type
    }
```
	
We'll add some basic functions:

```fsharp
let defaultShape = 
    {label=""; color=""; onClick=fun shape->shape}

let click shape = 
    shape.onClick shape

let display shape = 
    printfn "My label=%s and my color=%s" shape.label shape.color
    shape   //return same shape
```

For "method chaining" to work, every function should return an object that can be used next in the chain. So you will see that the "`display`" function returns the shape, rather than nothing.

Next we create some helper functions which we expose as the "mini-language", and will be used as building blocks by the users of the language. 

```fsharp
let setLabel label shape = 
   {shape with FluentShape.label = label}

let setColor color shape = 
   {shape with FluentShape.color = color}

//add a click action to what is already there
let appendClickAction action shape = 
   {shape with FluentShape.onClick = shape.onClick >> action}
```

Notice that `appendClickAction` takes a function as a parameter and composes it with the existing click action. As you start getting deeper into the functional approach to reuse, you start seeing many more "higher order functions" like this, that is, functions that act on other functions. Combining functions like this is one of the keys to understanding the functional way of programming. 

Now as a user of this "mini-language", I can compose the base helper functions into more complex functions of my own, creating my own function library. (In C# this kind of thing might be done using extension methods.)

```fsharp
// Compose two "base" functions to make a compound function.
let setRedBox = setColor "red" >> setLabel "box" 

// Create another function by composing with previous function.
// It overrides the color value but leaves the label alone.
let setBlueBox = setRedBox >> setColor "blue"  

// Make a special case of appendClickAction
let changeColorOnClick color = appendClickAction (setColor color)   
```

I can then combine these functions together to create objects with the desired behavior.

```fsharp
//setup some test values
let redBox = defaultShape |> setRedBox
let blueBox = defaultShape |> setBlueBox 

// create a shape that changes color when clicked
redBox 
    |> display
    |> changeColorOnClick "green"
    |> click
    |> display  // new version after the click

// create a shape that changes label and color when clicked
blueBox 
    |> display
    |> appendClickAction (setLabel "box2" >> setColor "green")  
    |> click
    |> display  // new version after the click
```

In the second case, I actually pass two functions to `appendClickAction`, but I compose them into one first. This kind of thing is trivial to do with a well structured functional library, but it is quite hard to do in C# without having lambdas within lambdas.

Here is a more complex example. We will create a function "`showRainbow`" that, for each color in the rainbow, sets the color and displays the shape.

```fsharp
let rainbow =
    ["red";"orange";"yellow";"green";"blue";"indigo";"violet"]

let showRainbow = 
    let setColorAndDisplay color = setColor color >> display 
    rainbow 
    |> List.map setColorAndDisplay 
    |> List.reduce (>>)

// test the showRainbow function
defaultShape |> showRainbow 
```

Notice that the functions are getting more complex, but the amount of code is still quite small. One reason for this is that the function parameters can often be ignored when doing function composition, which reduces visual clutter. For example, the "`showRainbow`" function does take a shape as a parameter, but it is not explicitly shown! This elision of parameters is called "point-free" style and will be discussed further in the ["thinking functionally"](/series/thinking-functionally.html) series
