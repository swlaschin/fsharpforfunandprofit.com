---
layout: post
title: "Implementing a builder: Combine"
description: "How to return multiple values at once"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 7
---

In this post we're going to look at returning multiple values from a computation expression using the `Combine` method.

## The story so far...

So far, our expression builder class looks like this:

```fsharp
type TraceBuilder() =
    member this.Bind(m, f) = 
        match m with 
        | None -> 
            printfn "Binding with None. Exiting."
        | Some a -> 
            printfn "Binding with Some(%A). Continuing" a
        Option.bind f m

    member this.Return(x) = 
        printfn "Returning a unwrapped %A as an option" x
        Some x

    member this.ReturnFrom(m) = 
        printfn "Returning an option (%A) directly" m
        m

    member this.Zero() = 
        printfn "Zero"
        None

    member this.Yield(x) = 
        printfn "Yield an unwrapped %A as an option" x
        Some x

    member this.YieldFrom(m) = 
        printfn "Yield an option (%A) directly" m
        m
        
// make an instance of the workflow                
let trace = new TraceBuilder()
```

And this class has worked fine so far. But we are about to run into a problem...

## A problem with two 'yields'

Previously, we saw how `yield` could be used to return values just like `return`.

Normally, `yield` is not used just once, of course, but multiple times in order to return values at different stages of a process such as an enumeration. So let's try that:

```fsharp
trace { 
    yield 1
    yield 2
    } |> printfn "Result for yield then yield: %A" 
```

But uh-oh, we get an error message:

```text
This control construct may only be used if the computation expression builder defines a 'Combine' method.
```

And if you use `return` instead of `yield`, you get the same error.

```fsharp
trace { 
    return 1
    return 2
    } |> printfn "Result for return then return: %A" 
```

And this problem occurs in other contexts too.  For example, if we want to do something and then return, like this:

```fsharp
trace { 
    if true then printfn "hello" 
    return 1
    } |> printfn "Result for if then return: %A" 
```

We get the same error message about a missing 'Combine' method.

## Understanding the problem

So what's going on here?  

To understand, let's go back to the behind-the-scenes view of the computation expression. We have seen that `return` and `yield` are really just the last step in a series of continuations, like this:

```fsharp
Bind(1,fun x -> 
   Bind(2,fun y -> 
     Bind(x + y,fun z -> 
        Return(z)  // or Yield
```

You can think of `return` (or `yield`) as "resetting" the indentation, if you like. So when we `return/yield` and then `return/yield` again, we are generating code like this: 

```fsharp
Bind(1,fun x -> 
   Bind(2,fun y -> 
     Bind(x + y,fun z -> 
        Yield(z)  
// start a new expression        
Bind(3,fun w -> 
   Bind(4,fun u -> 
     Bind(w + u,fun v -> 
        Yield(v)
```

But really this can be simplified to:

```fsharp
let value1 = some expression 
let value2 = some other expression 
```

In other words, we now have *two* values in our computation expression. And then the obvious question is, how should these two values be combined to give a single result for the computation expression as a whole? 

This is a very important point. **Return and yield do *not* generate an early return from a computation expression**.  No, the entire computation expression, all the way to the last curly brace, is *always* evaluated and results in a *single* value.  Let me repeat that. Every part of the computation expression is *always evaluated* -- there is no short circuiting going on.  If we want to short circuit and return early, we have to write our own code to do that (and we'll see how to do that later).

So, back to the pressing question. We have two expressions resulting in two values: how should those multiple values be combined into one? 

## Introducing "Combine"

The answer is by using the `Combine` method, which takes two *wrapped* values and combines them to make another wrapped value. Exactly how this works is up to us.

In our case, we are dealing specifically with `int options`, so one simple implementation that leaps to mind it just to add the numbers together. Each parameter is an `option` of course (the wrapped type), so we need to pick them apart and handle the four possible cases:

```fsharp
type TraceBuilder() =
    // other members as before

    member this.Combine (a,b) = 
        match a,b with
        | Some a', Some b' ->
            printfn "combining %A and %A" a' b' 
            Some (a' + b')
        | Some a', None ->
            printfn "combining %A with None" a' 
            Some a'
        | None, Some b' ->
            printfn "combining None with %A" b' 
            Some b'
        | None, None ->
            printfn "combining None with None"
            None

// make a new instance        
let trace = new TraceBuilder()
```

Running the test code again:

```fsharp
trace { 
    yield 1
    yield 2
    } |> printfn "Result for yield then yield: %A" 
```

But now we get a different error message:

```text
This control construct may only be used if the computation expression builder defines a 'Delay' method
```

The `Delay` method is a hook that allows you to delay evaluation of a computation expression until needed -- we'll discuss this in detail very soon; but for now, let's create a default implementation:

```fsharp
type TraceBuilder() =
    // other members as before

    member this.Delay(f) = 
        printfn "Delay"
        f()

// make a new instance        
let trace = new TraceBuilder()
```

Running the test code again:

```fsharp
trace { 
    yield 1
    yield 2
    } |> printfn "Result for yield then yield: %A" 
```

And finally we get the code to complete. 

```text
Delay
Yield an unwrapped 1 as an option
Delay
Yield an unwrapped 2 as an option
combining 1 and 2
Result for yield then yield: Some 3
```

The result of the entire workflow is the sum of all the yields, namely `Some 3`.

If we have a "failure" in the workflow (e.g. a `None`), the second yield doesn't occur and the overall result is `Some 1` instead.

```fsharp
trace { 
    yield 1
    let! x = None
    yield 2
    } |> printfn "Result for yield then None: %A" 
```

We can have three `yields` rather than two:

```fsharp
trace { 
    yield 1
    yield 2
    yield 3
    } |> printfn "Result for yield x 3: %A" 
```

The result is what you would expect, `Some 6`.
        
We can even try mixing up `yield` and `return` together. Other than the syntax difference, the overall effect is the same.

```fsharp
trace { 
    yield 1
    return 2
    } |> printfn "Result for yield then return: %A" 

trace { 
    return 1
    return 2
    } |> printfn "Result for return then return: %A" 
```

## Using Combine for sequence generation

Adding numbers up is not really the point of `yield`, although you might perhaps use a similar idea for constructing concatenated strings, somewhat like `StringBuilder`.

No, `yield` is naturally used as part of sequence generation, and now that we understand `Combine`, we can extend our "ListBuilder" workflow (from last time) with the required methods. 

* The `Combine` method is just list concatenation. 
* The `Delay` method can use a default implementation for now. 

Here's the full class:

```fsharp
type ListBuilder() =
    member this.Bind(m, f) = 
        m |> List.collect f

    member this.Zero() = 
        printfn "Zero"
        []
        
    member this.Yield(x) = 
        printfn "Yield an unwrapped %A as a list" x
        [x]

    member this.YieldFrom(m) = 
        printfn "Yield a list (%A) directly" m
        m

    member this.For(m,f) =
        printfn "For %A" m
        this.Bind(m,f)
        
    member this.Combine (a,b) = 
        printfn "combining %A and %A" a b 
        List.concat [a;b]

    member this.Delay(f) = 
        printfn "Delay"
        f()

// make an instance of the workflow                
let listbuilder = new ListBuilder()
```

And here it is in use:

```fsharp
listbuilder { 
    yield 1
    yield 2
    } |> printfn "Result for yield then yield: %A" 

listbuilder { 
    yield 1
    yield! [2;3]
    } |> printfn "Result for yield then yield! : %A" 
```

And here's a more complicated example with a `for` loop and some `yield`s.

```fsharp
listbuilder { 
    for i in ["red";"blue"] do
        yield i
        for j in ["hat";"tie"] do
            yield! [i + " " + j;"-"]
    } |> printfn "Result for for..in..do : %A" 
```

And the result is:

```text
["red"; "red hat"; "-"; "red tie"; "-"; "blue"; "blue hat"; "-"; "blue tie"; "-"]    
```

You can see that by combining `for..in..do` with `yield`, we are not too far away from the built-in `seq` expression syntax (except that `seq` is lazy, of course).

I would strongly encourage you to play around with this a bit until you are clear on what is going on behind the scenes.
As you can see from the example above, you can use `yield` in creative ways to generate all sorts of irregular lists, not just simple ones.

*Note: If you're wondering about `While`, we're going to hold off on it for a bit, until after we have looked at `Delay` in an upcoming post*.

## Order of processing for "combine"

The `Combine` method only has two parameters.  So what happens when you combine more than two values? For example, here are four values to combine:

```fsharp
listbuilder { 
    yield 1
    yield 2
    yield 3
    yield 4
    } |> printfn "Result for yield x 4: %A" 
```

If you look at the output you can see that the values are combined pair-wise, as you might expect.  

```text
combining [3] and [4]
combining [2] and [3; 4]
combining [1] and [2; 3; 4]
Result for yield x 4: [1; 2; 3; 4]
```

A subtle but important point is that they are combined "backwards", starting from the last value.  First "3" is combined with "4", and the result of that is then combined with "2", and so on.

![Combine](/assets/img/combine.png)

## Combine for non-sequences

In the second of our earlier problematic examples, we didn't have a sequence; we just had two separate expressions in a row.

```fsharp
trace { 
    if true then printfn "hello"  //expression 1
    return 1                      //expression 2
    } |> printfn "Result for combine: %A" 
```

How should these expressions be combined?  

There are a number of common ways of doing this, depending on the concepts that the workflow supports. 

### Implementing combine for workflows with "success" or "failure"

If the workflow has some concept of "success" or "failure", then a standard approach is:

* If the first expression "succeeds" (whatever that means in context), then use that value. 
* Otherwise use the value of the second expression. 

In this case, we also generally use the "failure" value for `Zero`.

This approach is useful for chaining together a series of "or else" expressions where the first success "wins" and becomes the overall result.  

```text
if (do first expression)
or else (do second expression)
or else (do third expression)
```

For example, for the `maybe` workflow, it is common to return the first expression if it is `Some`, but otherwise the second expression, like this:

```fsharp
type TraceBuilder() =
    // other members as before
    
    member this.Zero() = 
        printfn "Zero"
        None  // failure
    
    member this.Combine (a,b) = 
        printfn "Combining %A with %A" a b
        match a with
        | Some _ -> a  // a succeeds -- use it
        | None -> b    // a fails -- use b instead
        
// make a new instance        
let trace = new TraceBuilder()
```

**Example: Parsing**

Let's try a parsing example with this implementation:

```fsharp
type IntOrBool = I of int | B of bool

let parseInt s = 
    match System.Int32.TryParse(s) with
    | true,i -> Some (I i)
    | false,_ -> None

let parseBool s = 
    match System.Boolean.TryParse(s) with
    | true,i -> Some (B i)
    | false,_ -> None

trace { 
    return! parseBool "42"  // fails
    return! parseInt "42"
    } |> printfn "Result for parsing: %A" 
```

We get the following result:

```text
Some (I 42)
```

You can see that the first `return!` expression is `None`, and ignored. So the overall result is the second expression, `Some (I 42)`.

**Example: Dictionary lookup**

In this example, we'll try looking up the same key in a number of dictionaries, and return when we find a value:

```fsharp
let map1 = [ ("1","One"); ("2","Two") ] |> Map.ofList
let map2 = [ ("A","Alice"); ("B","Bob") ] |> Map.ofList

trace { 
    return! map1.TryFind "A"
    return! map2.TryFind "A"
    } |> printfn "Result for map lookup: %A" 
```

We get the following result:

```text
Result for map lookup: Some "Alice"
```

You can see that the first lookup is `None`, and ignored. So the overall result is the second lookup.

As you can see, this technique is very convenient when doing parsing or evaluating a sequence of (possibly unsuccessful) operations.

### Implementing combine for workflows with sequential steps 

If the workflow has the concept of sequential steps, then the overall result is just the value of the last step, and all the previous steps are evaluated only for their side effects.

In normal F#, this would be written:

```text
do some expression
do some other expression 
final expression
```

Or using the semicolon syntax, just:

```text
some expression; some other expression; final expression
```

In normal F#, each expression (other than the last) evaluates to the unit value.  

The equivalent approach for a computation expression is to treat each expression (other than the last) as a *wrapped* unit value, and "pass it into" the next expression, and so on, until you reach the last expression.  

This is exactly what bind does, of course, and so the easiest implementation is just to reuse the `Bind` method itself. Also, for this approach to work it is important that `Zero` is the wrapped unit value.

```fsharp
type TraceBuilder() =
    // other members as before

    member this.Zero() = 
        printfn "Zero"
        this.Return ()  // unit not None

    member this.Combine (a,b) = 
        printfn "Combining %A with %A" a b
        this.Bind( a, fun ()-> b )
        
// make a new instance        
let trace = new TraceBuilder()
```

The difference from a normal bind is that the continuation has a unit parameter, and evaluates to `b`.  This in turn forces `a` to be of type `WrapperType<unit>` in general, or `unit option` in our case.

Here's an example of sequential processing that works with this implementation of `Combine`:

```fsharp
trace { 
    if true then printfn "hello......."
    if false then printfn ".......world"
    return 1
    } |> printfn "Result for sequential combine: %A" 
```

Here's the following trace. Note that the result of the whole expression was the result of the last expression in the sequence, just like normal F# code.

```text
hello.......
Zero
Returning a unwrapped <null> as an option
Zero
Returning a unwrapped <null> as an option
Returning a unwrapped 1 as an option
Combining Some null with Some 1
Combining Some null with Some 1
Result for sequential combine: Some 1
```

### Implementing combine for workflows that build data structures

Finally, another common pattern for workflows is that they build data structures. In this case, `Combine` should merge the two data structures in whatever way is appropriate.
And the `Zero` method should create an empty data structure, if needed (and if even possible). 

In the "list builder" example above, we used exactly this approach. `Combine` was just list concatenation and `Zero` was the empty list.

## Guidelines for mixing "Combine" and "Zero"

We have looked at two different implementations for `Combine` for option types. 

* The first one used options as "success/failure" indicators, when the first success "won". In this case `Zero` was defined as `None`
* The second one was sequential, In this case `Zero` was defined as `Some ()`

Both cases worked nicely, but was that luck, or are there are any guidelines for implementing `Combine` and `Zero` correctly?

First, note that `Combine` does *not* have to give the same result if the parameters are swapped.
That is, `Combine(a,b)` need not be the same as `Combine(b,a)`. The list builder is a good example of this.

On the other hand there is a useful rule that connects `Zero` and `Combine`.

**Rule: `Combine(a,Zero)` should be the same as `Combine(Zero,a)` which should the same as just `a`.**

To use an analogy from arithmetic, you can think of `Combine` like addition (which is not a bad analogy -- it really is "adding" two values). And `Zero` is just the number zero, of course! So the rule above can be expressed as:

**Rule: `a + 0` is the same as `0 + a` is the same as just `a`, where `+` means `Combine` and `0` means `Zero`.**

If you look at the first `Combine` implementation ("success/failure") for option types, you'll see that it does indeed comply with this rule, as does the second implementation ("bind" with `Some()`).

On the other hand, if we had used the "bind" implementation of `Combine` but left `Zero` defined as `None`, it would *not* have obeyed the addition rule, which would be a clue that we had got something wrong.


## "Combine" without bind

As with all the builder methods, if you don't need them, you don't need to implement them.  So for a workflow that is strongly sequential, you could easily create a builder class with `Combine`, `Zero`, and `Yield`, say, without having to implement `Bind` and `Return` at all.

Here's an example of a minimal implementation that works:

```fsharp
type TraceBuilder() =

    member this.ReturnFrom(x) = x

    member this.Zero() = Some ()

    member this.Combine (a,b) = 
        a |> Option.bind (fun ()-> b )

    member this.Delay(f) = f()

// make an instance of the workflow                
let trace = new TraceBuilder()
```

And here it is in use:

```fsharp
trace { 
    if true then printfn "hello......."
    if false then printfn ".......world"
    return! Some 1
    } |> printfn "Result for minimal combine: %A" 
```

Similarly, if you have a data-structure oriented workflow, you could just implement `Combine` and some other helpers. For example, here is a minimal implementation of our list builder class:

```fsharp
type ListBuilder() =

    member this.Yield(x) = [x]

    member this.For(m,f) =
        m |> List.collect f

    member this.Combine (a,b) = 
        List.concat [a;b]

    member this.Delay(f) = f()

// make an instance of the workflow                
let listbuilder = new ListBuilder()
```

And even with the minimal implementation, we can write code like this:

```fsharp
listbuilder { 
    yield 1
    yield 2
    } |> printfn "Result: %A" 

listbuilder { 
    for i in [1..5] do yield i + 2
    yield 42
    } |> printfn "Result: %A" 
```


## A standalone "Combine" function

In a previous post, we saw that the "bind" function is often used as standalone function, and is normally given the operator `>>=`.

The `Combine` function too, is often used as a standalone function. Unlike bind, there is no standard symbol -- it can vary depending on how the combine function works.

A symmetric combination operation is often written as `++` or `<+>`. And the "left-biased" combination (that is, only do the second expression if the first
one fails) that we used earlier for options is sometimes written as `<++`. 

So here is an example of a standalone left-biased combination of options, as used in a dictionary lookup example.

```fsharp
module StandaloneCombine = 

    let combine a b = 
        match a with
        | Some _ -> a  // a succeeds -- use it
        | None -> b    // a fails -- use b instead

    // create an infix version
    let ( <++ ) = combine

    let map1 = [ ("1","One"); ("2","Two") ] |> Map.ofList
    let map2 = [ ("A","Alice"); ("B","Bob") ] |> Map.ofList

    let result = 
        (map1.TryFind "A") 
        <++ (map1.TryFind "B")
        <++ (map2.TryFind "A")
        <++ (map2.TryFind "B")
        |> printfn "Result of adding options is: %A"
```


## Summary 

What have we learned about `Combine` in this post?

* You need to implement `Combine` (and `Delay`) if you need to combine or "add" more than one wrapped value in a computation expression.
* `Combine` combines values pairwise, from last to first.
* There is no universal implementation of `Combine` that works in all cases -- it needs to be customized according the particular needs of the workflow.
* There is a sensible rule that relates `Combine` with `Zero`.
* `Combine` doesn't require `Bind` to be implemented.
* `Combine` can be exposed as a standalone function

In the next post, we'll add logic to control exactly when the internal expressions get evaluated, and introduce true short circuiting and lazy evaluation.