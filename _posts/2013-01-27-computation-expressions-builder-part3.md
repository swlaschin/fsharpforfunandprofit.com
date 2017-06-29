---
layout: post
title: "Implementing a builder: Delay and Run"
description: "Controlling when functions execute"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 8
---

In the last few posts we have covered all the basic methods (Bind, Return, Zero, and Combine) needed to create your own computation expression builder. In this post, we'll look at some of the extra features needed to make the workflow more efficient, by controlling when expressions get evaluated.

## The problem: avoiding unnecessary evaluations

Let's say that we have created a "maybe" style workflow as before. But this time we want to use the "return" keyword to return early and stop any more processing being done.

Here is our complete builder class. The key method to look at is `Combine`, in which we simply ignore any secondary expressions after the first return.

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
        printfn "Return an unwrapped %A as an option" x
        Some x

    member this.Zero() = 
        printfn "Zero"
        None

    member this.Combine (a,b) = 
        printfn "Returning early with %A. Ignoring second part: %A" a b 
        a

    member this.Delay(f) = 
        printfn "Delay"
        f()

// make an instance of the workflow                
let trace = new TraceBuilder()
```

Let's see how it works by printing something, returning, and then printing something else:

```fsharp
trace { 
    printfn "Part 1: about to return 1"
    return 1
    printfn "Part 2: after return has happened"
    } |> printfn "Result for Part1 without Part2: %A"  
```

The debugging output should look something like the following, which I have annotated:

```text
// first expression, up to "return"
Delay
Part 1: about to return 1
Return an unwrapped 1 as an option

// second expression, up to last curly brace.
Delay
Part 2: after return has happened
Zero   // zero here because no explicit return was given for this part

// combining the two expressions
Returning early with Some 1. Ignoring second part: <null>

// final result
Result for Part1 without Part2: Some 1
```

We can see a problem here. The "Part 2: after return" was printed, even though we were trying to return early.  

Why? Well I'll repeat what I said in the last post: **return and yield do *not* generate an early return from a computation expression**. The entire computation expression, all the way to the last curly brace, is *always* evaluated and results in a single value.  

This is a problem, because you might get unwanted side effects (such as printing a message in this case) and your code is doing something unnecessary, which might cause performance problems. 

So, how can we avoid evaluating the second part until we need it?  

## Introducing "Delay"

The answer to the question is straightforward -- simply wrap part 2 of the expression in a function and only call this function when needed, like this.

```fsharp
let part2 = 
    fun () -> 
        printfn "Part 2: after return has happened"
        // do other stuff
        // return Zero

// only evaluate if needed
if needed then
   let result = part2()        
```

Using this technique, part 2 of the computation expression can be processed completely, but because the expression returns a function, nothing actually *happens* until the function is called.  
But the `Combine` method will never call it, and so the code inside it does not run at all.

And this is exactly what the `Delay` method is for.  Any result from `Return` or `Yield` is immediately wrapped in a "delay" function like this, and then you can choose whether to run it or not.

Let's change the builder to implement a delay:

```fsharp
type TraceBuilder() =
    // other members as before

    member this.Delay(funcToDelay) = 
        let delayed = fun () ->
            printfn "%A - Starting Delayed Fn." funcToDelay
            let delayedResult = funcToDelay()
            printfn "%A - Finished Delayed Fn. Result is %A" funcToDelay delayedResult
            delayedResult  // return the result 

        printfn "%A - Delaying using %A" funcToDelay delayed
        delayed // return the new function
```

As you can see, the `Delay` method is given a function to execute. Previously, we executed it immediately.  What we're doing now is wrapping this function in another function and returning the delayed function instead.  I have added a number of trace statements before and after the function is wrapped.

If you compile this code, you can see that the signature of `Delay` has changed. Before the change, it returned a concrete value (an option in this case), but now it returns a function.

```fsharp
// signature BEFORE the change
member Delay : f:(unit -> 'a) -> 'a

// signature AFTER the change
member Delay : f:(unit -> 'b) -> (unit -> 'b)
```

By the way, we could have implemented `Delay` in a much simpler way, without any tracing, just by returning the same function that was passed in, like this:

```fsharp
member this.Delay(f) = 
    f
```

Much more concise! But in this case, I wanted to add some detailed tracing information as well.

Now let's try again:

```fsharp
trace { 
    printfn "Part 1: about to return 1"
    return 1
    printfn "Part 2: after return has happened"
    } |> printfn "Result for Part1 without Part2: %A"  
```

Uh-oh. This time nothing happens at all! What went wrong?

If we look at the output we see this:

<code>
Result for Part1 without Part2: &lt;fun:Delay@84-5>
</code>

Hmmm. The output of the whole `trace` expression is now a *function*, not an option. Why? Because we created all these delays, but we never "undelayed" them by actually calling the function!

One way to do this is to assign the output of the computation expression to a function value, say `f`, and then evaluate it.

```fsharp
let f = trace { 
    printfn "Part 1: about to return 1"
    return 1
    printfn "Part 2: after return has happened"
    } 
f() |> printfn "Result for Part1 without Part2: %A"  
```

This works as expected, but is there a way to do this from inside the computation expression itself? Of course there is!

## Introducing "Run"

The `Run` method exists for exactly this reason. It is called as the final step in the process of evaluating a computation expression, and can be used to undo the delay.

Here's an implementation:

```fsharp
type TraceBuilder() =
    // other members as before

    member this.Run(funcToRun) = 
        printfn "%A - Run Start." funcToRun
        let runResult = funcToRun()
        printfn "%A - Run End. Result is %A" funcToRun runResult
        runResult // return the result of running the delayed function
```

Let's try one more time:

```fsharp
trace { 
    printfn "Part 1: about to return 1"
    return 1
    printfn "Part 2: after return has happened"
    } |> printfn "Result for Part1 without Part2: %A"  
```

And the result is exactly what we wanted. The first part is evaluated, but the second part is not. And the result of the entire computation expression is an option, not a function.

## When is delay called?

The way that `Delay` is inserted into the workflow is straightforward, once you understand it.

* The bottom (or innermost) expression is delayed.
* If this is combined with a prior expression, the output of `Combine` is also delayed.
* And so on, until the final delay is fed into `Run`.

Using this knowledge, let's review what happened in the example above:

* The first part of the expression is the print statement plus `return 1`.
* The second part of the expression is the print statement without an explicit return, which means that `Zero()` is called
* The `None` from the `Zero` is fed into `Delay`, resulting in a "delayed option", that is, a function that will evaluate to an `option` when called.
* The option from part 1 and the delayed option from part 2 are combined in `Combine` and the second one is discarded. 
* The result of the combine is turned into another "delayed option".
* Finally, the delayed option is fed to `Run`, which evaluates it and returns a normal option.

Here is a diagram that represents this process visually:

![Delay](/assets/img/ce_delay.png)


If we look at the debug trace for the example above, we can see in detail what happened. It's a little confusing, so I have annotated it.
Also, it helps to remember that working *down* this trace is the same as working *up* from the bottom of the diagram above, because the outermost code is run first.

```text
// delaying the overall expression (the output of Combine)
<fun:clo@160-66> - Delaying using <fun:delayed@141-3>

// running the outermost delayed expression (the output of Combine)
<fun:delayed@141-3> - Run Start.
<fun:clo@160-66> - Starting Delayed Fn.

// the first expression results in Some(1)
Part 1: about to return 1
Return an unwrapped 1 as an option

// the second expression is wrapped in a delay
<fun:clo@162-67> - Delaying using <fun:delayed@141-3>

// the first and second expressions are combined
Combine. Returning early with Some 1. Ignoring <fun:delayed@141-3>

// overall delayed expression (the output of Combine) is complete
<fun:clo@160-66> - Finished Delayed Fn. Result is Some 1
<fun:delayed@141-3> - Run End. Result is Some 1

// the result is now an Option not a function
Result for Part1 without Part2: Some 1
```

## "Delay" changes the signature of "Combine"

When `Delay` is introduced into the pipeline like this, it has an effect on the signature of `Combine`.

When we originally wrote `Combine` we were expecting it to handle `options`.  But now it is handling the output of `Delay`, which is a function.

We can see this if we hard-code the types that `Combine` expects, with `int option` type annotations like this:

```fsharp
member this.Combine (a: int option,b: int option) = 
    printfn "Returning early with %A. Ignoring %A" a b 
    a
```

If this is done, we get an compiler error in the "return" expression:

```fsharp
trace { 
    printfn "Part 1: about to return 1"
    return 1
    printfn "Part 2: after return has happened"
    } |> printfn "Result for Part1 without Part2: %A" 
```        

The error is:

<pre>
error FS0001: This expression was expected to have type
    int option    
but here has type
    unit -> 'a    
</pre>

In other words, the `Combine` is being passed a delayed function (`unit -> 'a`), which doesn't match our explicit signature.

So what happens when we *do* want to combine the parameters, but they are passed in as a function instead of as a simple value?

The answer is straightforward: just call the function that was passed in to get the underlying value. 

Let's demonstrate that using the adding example from the previous post.

```fsharp
type TraceBuilder() =
    // other members as before

    member this.Combine (m,f) = 
        printfn "Combine. Starting second param %A" f
        let y = f()
        printfn "Combine. Finished second param %A. Result is %A" f y

        match m,y with
        | Some a, Some b ->
            printfn "combining %A and %A" a b 
            Some (a + b)
        | Some a, None ->
            printfn "combining %A with None" a 
            Some a
        | None, Some b ->
            printfn "combining None with %A" b 
            Some b
        | None, None ->
            printfn "combining None with None"
            None
```

In this new version of `Combine`, the *second* parameter is now a function, not an `int option`. So to combine them, we must first evaluate the function before doing the combination logic.

If we test this out:

```fsharp
trace { 
    return 1
    return 2
    } |> printfn "Result for return then return: %A" 
```

We get the following (annotated) trace:

```text
// entire expression is delayed
<fun:clo@318-69> - Delaying using <fun:delayed@295-6>

// entire expression is run
<fun:delayed@295-6> - Run Start.

// delayed entire expression is run
<fun:clo@318-69> - Starting Delayed Fn.

// first return
Returning a unwrapped 1 as an option

// delaying second return
<fun:clo@319-70> - Delaying using <fun:delayed@295-6>

// combine starts
Combine. Starting second param <fun:delayed@295-6>

    // delayed second return is run inside Combine
    <fun:clo@319-70> - Starting Delayed Fn.
    Returning a unwrapped 2 as an option
    <fun:clo@319-70> - Finished Delayed Fn. Result is Some 2
    // delayed second return is complete

Combine. Finished second param <fun:delayed@295-6>. Result is Some 2
combining 1 and 2
// combine is complete

<fun:clo@318-69> - Finished Delayed Fn. Result is Some 3
// delayed entire expression is complete

<fun:delayed@295-6> - Run End. Result is Some 3
// Run is complete

// final result is printed
Result for return then return: Some 3
```

## Understanding the type constraints

Up to now, we have used only our "wrapped type" (e.g. `int option`) and the delayed version (e.g. `unit -> int option`) in the implementation of our builder. 

But in fact we can use other types if we like, subject to certain constraints.
In fact, understanding exactly what the type constraints are in a computation expression can clarify how everything fits together.

For example, we have seen that:

* The output of `Return` is passed into `Delay`, so they must have compatible types. 
* The output of `Delay` is passed into the second parameter of `Combine`.
* The output of `Delay` is also passed into `Run`.

But the output of `Return` does *not* have to be our "public" wrapped type. It could be an internally defined type instead. 

![Delay](/assets/img/ce_return.png)

Similarly, the delayed type does not have to be a simple function, it could be any type that satisfies the constraints.

So, given a simple set of return expressions, like this:

```fsharp
    trace { 
        return 1
        return 2
        return 3
        } |> printfn "Result for return x 3: %A" 
```

Then a diagram that represents the various types and their flow would look like this:

![Delay](/assets/img/ce_types.png)

And to prove that this is valid, here is an implementation with distinct types for `Internal` and `Delayed`:

```fsharp
type Internal = Internal of int option
type Delayed = Delayed of (unit -> Internal)

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
        Internal (Some x) 

    member this.ReturnFrom(m) = 
        printfn "Returning an option (%A) directly" m
        Internal m

    member this.Zero() = 
        printfn "Zero"
        Internal None

    member this.Combine (Internal x, Delayed g) : Internal = 
        printfn "Combine. Starting %A" g
        let (Internal y) = g()
        printfn "Combine. Finished %A. Result is %A" g y
        let o = 
            match x,y with
            | Some a, Some b ->
                printfn "Combining %A and %A" a b 
                Some (a + b)
            | Some a, None ->
                printfn "combining %A with None" a 
                Some a
            | None, Some b ->
                printfn "combining None with %A" b 
                Some b
            | None, None ->
                printfn "combining None with None"
                None
        // return the new value wrapped in a Internal
        Internal o                

    member this.Delay(funcToDelay) = 
        let delayed = fun () ->
            printfn "%A - Starting Delayed Fn." funcToDelay
            let delayedResult = funcToDelay()
            printfn "%A - Finished Delayed Fn. Result is %A" funcToDelay delayedResult
            delayedResult  // return the result 

        printfn "%A - Delaying using %A" funcToDelay delayed
        Delayed delayed // return the new function wrapped in a Delay

    member this.Run(Delayed funcToRun) = 
        printfn "%A - Run Start." funcToRun
        let (Internal runResult) = funcToRun()
        printfn "%A - Run End. Result is %A" funcToRun runResult
        runResult // return the result of running the delayed function

// make an instance of the workflow                
let trace = new TraceBuilder()
```

And the method signatures in the builder class methods look like this:

```fsharp
type Internal = | Internal of int option
type Delayed = | Delayed of (unit -> Internal)

type TraceBuilder =
class
  new : unit -> TraceBuilder
  member Bind : m:'a option * f:('a -> 'b option) -> 'b option
  member Combine : Internal * Delayed -> Internal
  member Delay : funcToDelay:(unit -> Internal) -> Delayed
  member Return : x:int -> Internal
  member ReturnFrom : m:int option -> Internal
  member Run : Delayed -> int option
  member Zero : unit -> Internal
end
```

Creating this artifical builder is overkill of course, but the signatures clearly show how the various methods fit together.

## Summary

In this post, we've seen that: 

* You need to implement `Delay` and `Run` if you want to delay execution within a computation expression.
* Using `Delay` changes the signature of `Combine`.
* `Delay` and `Combine` can use internal types that are not exposed to clients of the computation expression.

The next logical step is wanting to delay execution *outside* a computation expression until you are ready, and that will be the topic on the next but one post.
But first, we'll take a little detour to discuss method overloads.
