---
layout: post
title: "Overview of F# expressions"
description: "Control flows, lets, dos, and more"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 3
---

In this post we'll look at the different kinds of expressions that are available in F# and some general tips for using them.

## Is everything really an expression?

You might be wondering how "everything is an expression" actually works in practice.

Let's start with some basic expression examples that should be familiar:

```fsharp
1                            // literal
[1;2;3]                      // list expression
-2                           // prefix operator	
2 + 2                        // infix operator	
"string".Length              // dot lookup
printf "hello"               // function application
```

No problems there. Those are obviously expressions.

But here are some more complex things which are *also* expressions. That is, each of these returns a value that can be used for something else. 

```fsharp
fun () -> 1                  // lambda expression

match 1 with                 // match expression
    | 1 -> "a"
    | _ -> "b"

if true then "a" else "b"    // if-then-else

for i in [1..10]             // for loop
  do printf "%i" i

try                          // exception handling
  let result = 1 / 0
  printfn "%i" result
with
  | e -> 
     printfn "%s" e.Message


let n=1 in n+2               // let expression
```

In other languages, these might be statements, but in F# they really do return values, as you can see by binding a value to the result:

```fsharp
let x1 = fun () -> 1                  

let x2 = match 1 with                 
         | 1 -> "a"
         | _ -> "b"

let x3 = if true then "a" else "b"    

let x4 = for i in [1..10]             
          do printf "%i" i

let x5 = try                          
            let result = 1 / 0
            printfn "%i" result
         with
            | e -> 
                printfn "%s" e.Message


let x6 = let n=1 in n+2
```

## What kinds of expressions are there?

There are lots of diffent kinds of expressions in F#, about 50 currently.  Most of them are trivial and obvious, such as literals, operators, function application, "dotting into", and so on.

The more interesting and high-level ones can be grouped as follows:

* Lambda expressions
* "Control flow" expressions, including:
  * The match expression (with the `match..with` syntax)
  * Expressions related to imperative control flow, such as if-then-else, loops 
  * Exception-related expressions
* "let" and "use" expressions
* Computation expressions such as `async {..}`
* Expressions related to object-oriented code, including casts, interfaces, etc

We have already discussed lambdas in the ["thinking functionally"](/series/thinking-functionally.html) series, and as noted earlier, computation expressions and object-oriented expressions will be left to later series.

So, in upcoming posts in this series, we will focus on "control flow" expressions and "let" expressions.
 
### "Control flow" expressions 

In imperative languages, control flow expressions like if-then-else, for-in-do, and match-with are normally implemented as statements with side-effects, In F#, they are all implemented as just another type of expression. 

In fact, it is not even helpful to think of "control flow" in a functional language; the concept doesn't really exist.  Better to just think of the program as a giant expression containing sub-expressions, some of which are evaluated and some of which are not.  If you can get your head around this way of thinking, you have a good start on thinking functionally.

There will be some upcoming posts on these different types of control flow expressions:

* [The match expression](/posts/match-expression)
* [Imperative control flow: if-then-else and for loops](/posts/control-flow-expressions)
* [Exceptions](/posts/exceptions)

### "let" bindings as expressions 

What about `let x=something`? In the examples above we saw:

```fsharp
let x5 = let n=1 in n+2
```

How can "`let`" be an expression? The reason will be discussed in the next post on ["let", "use" and "do"](/posts/let-use-do).

## General tips for using expressions 

But before we cover the important expression types in details, here are some tips for using expressions in general. 

### Multiple expressions on one line 

Normally, each expression is put on a new line. But you can use a semicolon to separate expressions on one line if you need to. Along with its use as a separator for list and record elements, this is one of the few times where a semicolon is used in F#.

```fsharp
let f x =                           // one expression per line
      printfn "x=%i" x
      x + 1

let f x = printfn "x=%i" x; x + 1   // all on same line with ";"
```

The rule about requiring unit values until the last expression still applies, of course:

```fsharp
let x = 1;2              // error: "1;" should be a unit expression
let x = ignore 1;2       // ok
let x = printf "hello";2 // ok
```

### Understanding expression evaluation order 

In F#, expressions are evaluated from the "inside out" -- that is, as soon as a complete subexpression is "seen", it is evaluated.

Have a look at the following code and try to guess what will happen, then evaluate the code and see.

```fsharp
// create a clone of if-then-else
let test b t f = if b then t else f

// call it with two different choices
test true (printfn "true") (printfn "false")
```

What happens is that both "true" and "false" are printed, even though the test function will never actually evaluate the "else" branch.  Why? Because the `(printfn "false")` expression is evaluated immediately, regardless of how the test function will be using it.

This style of evaluation is called "eager". It has the advantage that it is easy to understand, but it does mean that it can be inefficient on occasion.

The alternative style of evaluation is called "lazy", whereby expressions are only evaluated when they are needed.  The Haskell language follows this approach, so a similar example in Haskell would only print "true".

In F#, there are a number of techniques to force expressions *not* to be evaluated immediately. The simplest it to wrap it in a function that only gets evaluated on demand:

```fsharp
// create a clone of if-then-else that accepts functions rather than simple values
let test b t f = if b then t() else f()

// call it with two different functions
test true (fun () -> printfn "true") (fun () -> printfn "false")
```

The problem with this is that now the "true" function might be evaluated twice by mistake, when we only wanted to evaluate it once!

So, the preferred way for expressions not to be evaluated immediately is to use the `Lazy<>` wrapper.

```fsharp
// create a clone of if-then-else with no restrictions...
let test b t f = if b then t else f

// ...but call it with lazy values
let f = test true (lazy (printfn "true")) (lazy (printfn "false"))
```

The final result value `f` is also a lazy value, and can be passed around without being evaluated until you are finally ready to get the result.

```fsharp
f.Force()     // use Force() to force the evaluation of a lazy value
```

If you never need the result, and never call `Force()`, then the wrapped value will never be evaluated.

There will much more on laziness in an upcoming series on performance.
