---
layout: post
title: "Control flow expressions"
description: "And how to avoid using them"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 7
---

In this post, we'll look at the control flow expressions, namely:

* if-then-else
* for x in collection  (which is the same as foreach in C#)
* for x = start to end
* while-do 

These control flow expressions are no doubt very familiar to you. But they are very "imperative" rather than functional. 

So I would strongly recommend that you do not use them if at all possible, especially when you are learning to think functionally. If you do use them as a crutch, you will find it much harder to break away from imperative thinking. 

To help you do this, I will start each section with examples of how to avoid using them by using more idiomatic constructs instead.  If you do need to use them, there are some "gotchas" that you need to be aware of.

## If-then-else

### How to avoid using if-then-else

The best way to avoid `if-then-else` is to use "match" instead. You can match on a boolean, which is similar to the classic then/else branches. But much, much better, is to avoid the equality test and actually match on the thing itself, as shown in the last implementation below.

```fsharp
// bad
let f x = 
    if x = 1 
    then "a" 
    else "b"

// not much better
let f x = 
    match x=1 with
    | true -> "a" 
    | false -> "b"

// best
let f x = 
    match x with
    | 1 -> "a" 
    | _ -> "b"
```

Part of the reason why direct matching is better is that the equality test throws away useful information that you often need to retrieve again. 

This is demonstrated by the next scenario, where we want to get the first element of a list in order to print it. Obviously, we must be careful not to attempt this for an empty list.

The first implementation does a test for empty and then a *second* operation to get the first element. A much better approach is to match and extract the element in one single step, as shown in the second implementation. 

```fsharp
// bad
let f list = 
    if List.isEmpty list
    then printfn "is empty" 
    else printfn "first element is %s" (List.head list)

// much better
let f list = 
    match list with
    | [] -> printfn "is empty" 
    | x::_ -> printfn "first element is %s" x
```

The second implementation is not only easier to understand, it is more efficient.

If the boolean test is complicated, it can still be done with match by using extra "`when`" clauses (called "guards"). Compare the first and second implementations below to see the difference.

```fsharp
// bad
let f list = 
    if List.isEmpty list
        then printfn "is empty" 
        elif (List.head list) > 0
            then printfn "first element is > 0" 
            else printfn "first element is <= 0" 

// much better
let f list = 
    match list with
    | [] -> printfn "is empty" 
    | x::_ when x > 0 -> printfn "first element is > 0" 
    | x::_ -> printfn "first element is <= 0" 
```

Again, the second implementation is easier to understand and also more efficient.

The moral of the tale is: if you find yourself using if-then-else or matching on booleans, consider refactoring your code.

### How to use if-then-else

If you do need to use if-then-else, be aware that even though the syntax looks familiar, there is a catch that you must be aware of: "`if-then-else`" is an *expression*, not a *statement*, and as with every expression in F#, it must return a value of a particular type. 

Here are two examples where the return type is a string.

```fsharp
let v = if true then "a" else "b"    // value : string
let f x = if x then "a" else "b"     // function : bool->string
```

But as a consequence, both branches must return the same type!  If this is not true, then the expression as a whole cannot return a consistent type and the compiler will complain. 

Here is an example of different types in each branch:

```fsharp
let v = if true then "a" else 2  
  // error FS0001: This expression was expected to have 
  //               type string but here has type int    
```

The "else" clause is optional, but if it is absent, the "else" clause is assumed to return unit, which means that the "then" clause must also return unit. You will get a complaint from the compiler if you make this mistake.

```fsharp
let v = if true then "a"    
  // error FS0001: This expression was expected to have type unit    
  //               but here has type string    
```

If the "then" clause returns unit, then the compiler will be happy.

```fsharp
let v2 = if true then printfn "a"   // OK as printfn returns unit
```

Note that there is no way to return early in a branch. The return value is the entire expression. In other words, the if-then-else expression is more closely related to the C# ternary if operator (<if expr>?<then expr>:<else expr>) than to the C# if-then-else statement.

### if-then-else for one liners

One of the places where if-then-else can be genuinely useful is to create simple one-liners for passing into other functions.  

```fsharp
let posNeg x = if x > 0 then "+" elif x < 0 then "-" else "0"
[-5..5] |> List.map posNeg
```

### Returning functions

Don't forget that an if-then-else expression can return any value, including function values. For example:

```fsharp
let greetings = 
    if (System.DateTime.Now.Hour < 12) 
    then (fun name -> "good morning, " + name)
    else (fun name -> "good day, " + name)

//test 
greetings "Alice"
```

Of course, both functions must have the same type, meaning that they must have the same function signature.

## Loops ##

### How to avoid using loops ###

The best way to avoid loops is to use the built in list and sequence functions instead. Almost anything you want to do can be done without using explicit loops. And often, as a side benefit, you can avoid mutable values as well. Here are some examples to start with, and for more details please read the upcoming series devoted to list and sequence operations.

Example: Printing something 10 times:

```fsharp
// bad
for i = 1 to 10 do
   printf "%i" i

// much better
[1..10] |> List.iter (printf "%i") 
```

Example: Summing a list:

```fsharp
// bad
let sum list = 
    let mutable total = 0    // uh-oh -- mutable value 
    for e in list do
        total <- total + e   // update the mutable value
    total                    // return the total

// much better
let sum list = List.reduce (+) list

//test
sum [1..10]
```

Example: Generating and printing a sequence of random numbers:

```fsharp
// bad
let printRandomNumbersUntilMatched matchValue maxValue =
  let mutable continueLooping = true  // another mutable value
  let randomNumberGenerator = new System.Random()
  while continueLooping do
    // Generate a random number between 1 and maxValue.
    let rand = randomNumberGenerator.Next(maxValue)
    printf "%d " rand
    if rand = matchValue then 
       printfn "\nFound a %d!" matchValue
       continueLooping <- false

// much better
let printRandomNumbersUntilMatched matchValue maxValue =
  let randomNumberGenerator = new System.Random()
  let sequenceGenerator _ = randomNumberGenerator.Next(maxValue)
  let isNotMatch = (<>) matchValue

  //create and process the sequence of rands
  Seq.initInfinite sequenceGenerator 
    |> Seq.takeWhile isNotMatch
    |> Seq.iter (printf "%d ")

  // done
  printfn "\nFound a %d!" matchValue

//test
printRandomNumbersUntilMatched 10 20
```

As with if-then-else, there is a moral; if you find yourself using loops and mutables, please consider refactoring your code to avoid them.

### The three types of loops 

If you want to use loops, then there are three types of loop expressions to choose from, which are similar to those in C#. 

* `for-in-do`.  This has the form `for x in enumerable do something`. It is the same as the `foreach` loop in C#, and is the form most commonly seen in F#.
* `for-to-do`.  This has the form `for x = start to finish do something`. It is the same as the standard `for (i=start; i<end; i++)` loops in C#.
* `while-do`. This has the form `while test do something`. It is the same as the `while` loop in C#.  Note that there is no `do-while` equivalent in F#.

I won't go into any more detail than this, as the usage is straightforward. If you have trouble, check the [MSDN documentation](http://msdn.microsoft.com/en-us/library/dd233227.aspx).

### How to use loops

As with if-then-else expressions, the loop expressions look familiar, but there are some catches again. 

* All looping expressions always return unit for the whole expression, so there is no way to return a value from inside a loop. 
* As with all "do" bindings, the expression inside the loop must return unit as well.
* There is no equivalent of "break" and "continue" (this can generally done better using sequences anyway)

Here's an example of the unit constraint. The expression in the loop should be unit, not int, so the compiler will complain.

```fsharp
let f =
  for i in [1..10] do
    i + i  // warning: This expression should have type 'unit'

// version 2
let f =
  for i in [1..10] do
    i + i |> ignore   // fixed
```

### Loops for one liners

One of the places where loops are used in practice is as list and sequence generators.

```fsharp
let myList = [for x in 0..100 do if x*x < 100 then yield x ]
```

## Summary

I'll repeat what I said at the top of the post: do avoid using imperative control flow when you are learning to think functionally.
And understand the exceptions that prove the rule; the one-liners whose use is acceptable.
