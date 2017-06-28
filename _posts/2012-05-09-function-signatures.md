---
layout: post
title: "Function signatures"
description: "A function signature can give you some idea of what it does"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 9
categories: [Functions]
---

It may not be obvious, but F# actually has two syntaxes - one for normal (value) expressions, and one for type definitions. For example:

```fsharp
[1;2;3]      // a normal expression
int list     // a type expression 

Some 1       // a normal expression
int option   // a type expression 

(1,"a")      // a normal expression
int * string // a type expression 
```

Type expressions have a special syntax that is *different* from the syntax used in normal expressions. You have already seen many examples of this when you use the interactive session, because the type of each expression has been printed along with its evaluation. 

As you know, F# uses type inference to deduce types, so you don't often need to explicitly specify types in your code, especially for functions. But in order to work effectively in F#, you *do* need to understand the type syntax, so that you can build your own types, debug type errors, and understand function signatures. In this post, we'll focus on its use in function signatures.

Here are some example function signatures using the type syntax:

```fsharp
// expression syntax          // type syntax
let add1 x = x + 1            // int -> int 
let add x y = x + y           // int -> int -> int
let print x = printf "%A" x   // 'a -> unit
System.Console.ReadLine       // unit -> string
List.sum                      // 'a list -> 'a
List.filter                   // ('a -> bool) -> 'a list -> 'a list
List.map                      // ('a -> 'b) -> 'a list -> 'b list
```

## Understanding functions through their signatures ##

Just by examining a function's signature, you can often get some idea of what it does. Let's look at some examples and analyze them in turn.

```fsharp
// function signature 1
int -> int -> int
```

This function takes two `int` parameters and returns another, so presumably it is some sort of mathematical function such as addition, subtraction, multiplication, or exponentiation. 

```fsharp
// function signature 2
int -> unit
```

This function takes an `int` and returns a `unit`, which means that the function is doing something important as a side-effect. Since there is no useful return value, the side effect is probably something to do with writing to IO, such as logging, writing to a file or database, or something similar. 

```fsharp
// function signature 3
unit -> string
```

This function takes no input but returns a `string`, which means that the function is conjuring up a string out of thin air! Since there is no explicit input, the function probably has something to do with reading (from a file say) or generating (a random string, say). 

```fsharp
// function signature 4
int -> (unit -> string)
```

This function takes an `int` input and returns a function that when called, returns strings. Again, the function probably has something to do with reading or generating. The input probably initializes the returned function somehow. For example, the input could be a file handle, and the returned function something like `readline()`. Or the input could be a seed for a random string generator. We can't tell exactly, but we can make some educated guesses.

```fsharp
// function signature 5
'a list -> 'a 
```

This function takes a list of some type, but returns only one of that type, which means that the function is merging or choosing elements from the list. Examples of functions with this signature are `List.sum`, `List.max`, `List.head` and so on.

```fsharp
// function signature 6
('a -> bool) -> 'a list -> 'a list 
```

This function takes two parameters: the first is a function that maps something to a bool (a predicate), and the second is a list. The return value is a list of the same type. Predicates are used to determine whether a value meets some sort of criteria, so it looks like the function is choosing elements from the list based on whether the predicate is true or not and then returning a subset of the original list. A typical function with this signature is `List.filter`.

```fsharp
// function signature 7
('a -> 'b) -> 'a list -> 'b list
```

This function takes two parameters: the first maps type `'a` to type `'b`, and the second is a list of `'a`. The return value is a list of a different type `'b`. A reasonable guess is that the function takes each of the `'a`s in the list, maps them to a `'b` using the function passed in as the first parameter, and returns the new list of `'b`s. And indeed, the prototypical function with this signature is `List.map`.

### Using function signatures to find a library method ###

Function signatures are an important part of searching for library functions. The F# libraries have hundreds of functions in them and they can initially be overwhelming.  Unlike an object oriented language, you cannot simply "dot into" an object to find all the appropriate methods. However, if you know the signature of the function you are looking for, you can often narrow down the list of candidates quickly.

For example, let's say you have two lists and you are looking for a function to combine them into one. What would the signature be for this function? It would take two list parameters and return a third, all of the same type, giving the signature:

```fsharp
'a list -> 'a list -> 'a list
```

Now go to the [MSDN documentation for the F# List module](http://msdn.microsoft.com/en-us/library/ee353738), and scan down the list of functions, looking for something that matches.  As it happens, there is only one function with that signature:

```fsharp
append : 'T list -> 'T list -> 'T list 
```

which is exactly the one we want!

## Defining your own types for function signatures ##

Sometimes you may want to create your own types to match a desired function signature. You can do this using the "type" keyword, and define the type in the same way that a signature is written:

```fsharp
type Adder = int -> int
type AdderGenerator = int -> Adder
```

You can then use these types to constrain function values and parameters. 

For example, the second definition below will fail because of type constraints. If you remove the type constraint (as in the third definition) there will not be any problem.

```fsharp
let a:AdderGenerator = fun x -> (fun y -> x + y)
let b:AdderGenerator = fun (x:float) -> (fun y -> x + y)
let c                = fun (x:float) -> (fun y -> x + y)
```

## Test your understanding of function signatures ##

How well do you understand function signatures?  See if you can create simple functions that have each of these signatures. Avoid using explicit type annotations! 

```fsharp
val testA = int -> int
val testB = int -> int -> int
val testC = int -> (int -> int)      
val testD = (int -> int) -> int
val testE = int -> int -> int -> int
val testF = (int -> int) -> (int -> int)
val testG = int -> (int -> int) -> int
val testH = (int -> int -> int) -> int
```
