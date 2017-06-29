---
layout: post
title: "Function Values and Simple Values"
description: "Binding not assignment"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 3
---

Let's look at the simple function again

```fsharp
let add1 x = x + 1
```

What does the "x" mean here? It means:

1. Accept some value from the input domain.
2. Use the name "x" to represent that value so that we can refer to it later.

This process of using a name to represent a value is called "binding". The name "x" is "bound" to the input value. 

So if we evaluate the function with the input 5 say, what is happening is that everywhere we see "x" in the original definition, we replace it with "5", sort of like search and replace in a word processor. 

```fsharp
let add1 x = x + 1
add1 5
// replace "x" with "5"
// add1 5 = 5 + 1 = 6
// result is 6
```

It is important to understand that this is not assignment. "x" is not a "slot" or variable that is assigned to the value and can be assigned to another value later on. It is a onetime association of the name "x" with the value. The value is one of the predefined integers, and cannot change. And so, once bound, x cannot change either; once associated with a value, always associated with a value. 

This concept is a critical part of thinking functionally: *there are no "variables", only values*.

## Function values ##

If you think about this a bit more, you will see that the name "`add1`" itself is just a binding to "the function that adds one to its input". The function itself is independent of the name it is bound to.

When you type `let add1 x = x + 1` you are telling the F# compiler "every time you see the name "`add1`", replace it with the function that adds 1 to its input". "`add1`" is called a **function value**.

To see that the function is independent of its name, try:

```fsharp
let add1 x = x + 1
let plus1 = add1
add1 5
plus1 5
```

You can see that "`add1`" and "`plus1`" are two names that refer ("bound to") to the same function.

You can always identify a function value because its signature has the standard form `domain -> range`. Here is a generic function value signature:

```fsharp
val functionName : domain -> range
```

## Simple values ##

Imagine an operation that always returned the integer 5 and didn't have any input. 

![](/assets/img/Functions_Const.png)
 
This would be a "constant" operation.

How would we write this in F#?  We want to tell the F# compiler "every time you see the name `c`, replace it with 5". Here's how:

```fsharp
let c = 5
```

which when evaluated, returns:

```fsharp
val c : int = 5
```

There is no mapping arrow this time, just a single int. What's new is an equals sign with the actual value printed after it. The F# compiler knows that this binding has a known value which it will always return, namely the value 5. 

In other words, we've just defined a constant, or in F# terms, a simple value. 

You can always tell a simple value from a function value because all simple values have a signature that looks like:

```fsharp
val aName: type = constant     // Note that there is no arrow
```

## Simple values vs. function values ##

It is important to understand that in F#, unlike languages such as C#, there is very little difference between simple values and function values. They are both values which can be bound to names (using the same keyword `let`) and then passed around. And in fact, one of the key aspects of thinking functionally is exactly that: *functions are values that can be passed around as inputs to other functions*, as we will soon see.

Note that there is a subtle difference between a simple value and a function value. A function always has a domain and range and must be "applied" to an argument to get a result. A simple value does not need to be evaluated after being bound. Using the example above, if we wanted to define a "constant function" that returns five we would have to use 

```fsharp
let c = fun()->5    
// or
let c() = 5
```

The signature for these functions is:

```fsharp
val c : unit -> int
```

instead of:

```fsharp
val c : int = 5
```

More on unit, function syntax and anonymous functions later.

## "Values" vs. "Objects" ##

In a functional programming language like F#, most things are called "values". In an object-oriented language like C#, most things are called "objects". So what is the difference between a "value" and an "object"?  

A value, as we have seen above, is just a member of a domain. The domain of ints, the domain of strings, the domain of functions that map ints to strings, and so on. In principle, values are immutable. And values do not have any behavior attached them. 

An object, in a standard definition, is an encapsulation of a data structure with its associated behavior (methods). In general, objects are expected to have state (that is, be mutable), and all operations that change the internal state must be provided by the object itself (via "dot" notation).

In F#, even the primitive values have some object-like behavior. For example, you can dot into a string to get its length:

```fsharp
"abc".Length
```

But, in general, we will avoid using "object" for standard values in F#, reserving it to refer to instances of true classes, or other values that expose member methods.

## Naming Values ##

Standard naming rules are used for value and function names, basically, any alphanumeric string, including underscores.  There are a couple of extras:

You can put an apostrophe anywhere in a name, except the first character. So: 

```fsharp
A'b'c     begin'  // valid names
```

The final tick is often used to signal some sort of "variant" version of a value:

```fsharp
let f = x
let f' = derivative f
let f'' = derivative f'
```

or define variants of existing keywords

```fsharp
let if' b t f = if b then t else f
```

You can also put double backticks around any string to make a valid identifier.

```fsharp
``this is a name``  ``123``    //valid names
```

You might want to use the double backtick trick sometimes:

* When you want to  use an identifier that is the same as a keyword 

```fsharp
let ``begin`` = "begin"
```

* When trying to use natural language for business rules, unit tests, or BDD style executable specifications a la Cucumber. 

```fsharp
let ``is first time customer?`` = true
let ``add gift to order`` = ()
if ``is first time customer?`` then ``add gift to order``

// Unit test 
let [<Test>] ``When input is 2 then expect square is 4``=  
   // code here

// BDD clause
let [<Given>] ``I have (.*) N products in my cart`` (n:int) =  
   // code here
```

Unlike C#, the naming convention for F# is that functions and values start with lowercase letters rather than uppercase (`camelCase` rather than `PascalCase`) unless designed for exposure to other .NET languages.  Types and modules use uppercase however.
