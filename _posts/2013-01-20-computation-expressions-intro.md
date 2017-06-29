---
layout: post
title: "Computation expressions: Introduction"
description: "Unwrapping the enigma..."
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 1
---

By popular request, it is time to talk about the mysteries of computation expressions, what they are, and how they can be useful in practice (and I will try to avoid using the [forbidden m-word](/about/#banned)).

In this series, you'll learn what computation expressions are, how to make your own, and some common patterns involving them. In the process, we'll also look at continuations, the bind function, wrapper types, and more.

## Background ##

Computation expressions seem to have a reputation for being abstruse and difficult to understand.  

On one hand, they're easy enough to use. Anyone who has written much F# code has certainly used standard ones like `seq{...}` or `async{...}`.

But how do you make a new one of these things? How do they work behind the scenes?

Unfortunately, many explanations seem to make things even more confusing.  There seems to be some sort of mental bridge that you have to cross. 
Once you are on the other side, it is all obvious, but to someone on this side, it is baffling.

If we turn for guidance to the [official MSDN documention](http://msdn.microsoft.com/en-us/library/dd233182.aspx), it is explicit, but quite unhelpful to a beginner. 

For example, it says that when you see the following code within a computation expression:

```fsharp
{| let! pattern = expr in cexpr |}
```

it is simply syntactic sugar for this method call:

```fsharp
builder.Bind(expr, (fun pattern -> {| cexpr |}))
```

But... what does this mean exactly?

I hope that by the end of this series, the documentation above will become obvious.  Don't believe me? Read on!

## Computation expressions in practice ##

Before going into the mechanics of computation expressions, let's look at a few trivial examples that show the same code before and after using computation expressions.

Let's start with a simple one.  Let's say we have some code, and we want to log each step. So we define a little logging function, and call it after every value is created, like so:

```fsharp
let log p = printfn "expression is %A" p

let loggedWorkflow = 
    let x = 42
    log x
    let y = 43
    log y
    let z = x + y
    log z
    //return
    z
```

If you run this, you will see the output:

```text
expression is 42
expression is 43
expression is 85
```

Simple enough.  

But it is annoying to have to explicitly write all the log statements each time. Is there a way to hide them?

Funny you should ask... A computation expression can do that. Here's one that does exactly the same thing.

First we define a new type called `LoggingBuilder`:

```fsharp
type LoggingBuilder() =
    let log p = printfn "expression is %A" p

    member this.Bind(x, f) = 
        log x
        f x

    member this.Return(x) = 
        x
```

*Don't worry about what the mysterious `Bind` and `Return` are for yet -- they will be explained soon.*

Next we create an instance of the type, `logger` in this case.

```fsharp
let logger = new LoggingBuilder()
```

So with this `logger` value, we can rewrite the original logging example like this:

```fsharp
let loggedWorkflow = 
    logger
        {
        let! x = 42
        let! y = 43
        let! z = x + y
        return z
        }
```

If you run this, you get exactly the same output, but you can see that the use of the `logger{...}` workflow has allowed us to hide the repetitive code.

### Safe division ###

Now let's look at an old chestnut.

Say that we want to divide a series of numbers, one after another, but one of them might be zero. How can we handle it? Throwing an exception is ugly.  Sounds like a good match for the `option` type though.

First we need to create a helper function that does the division and gives us back an `int option`. 
If everything is OK, we get a `Some` and if the division fails, we get a `None`.

Then we can chain the divisions together, and after each division we need to test whether it failed or not, and keep going only if it was successful.

Here's the helper function first, and then the main workflow:

```fsharp
let divideBy bottom top =
    if bottom = 0
    then None
    else Some(top/bottom)
```

Note that I have put the divisor first in the parameter list. This is so we can write an expression like `12 |> divideBy 3`, which makes chaining easier.

Let's put it to use. Here is a workflow that attempts to divide a starting number three times:

```fsharp
let divideByWorkflow init x y z = 
    let a = init |> divideBy x
    match a with
    | None -> None  // give up
    | Some a' ->    // keep going
        let b = a' |> divideBy y
        match b with
        | None -> None  // give up
        | Some b' ->    // keep going
            let c = b' |> divideBy z
            match c with
            | None -> None  // give up
            | Some c' ->    // keep going
                //return 
                Some c'
```

And here it is in use:

```fsharp
let good = divideByWorkflow 12 3 2 1
let bad = divideByWorkflow 12 3 0 1
```

The `bad` workflow fails on the third step and returns `None` for the whole thing.  

It is very important to note that the *entire workflow* has to return an `int option` as well. It can't just return an `int` because what would it evaluate to in the bad case?
And can you see how the type that we used "inside" the workflow, the option type, has to be the same type that comes out finally at the end. Remember this point -- it will crop up again later.

Anyway, this continual testing and branching is really ugly! Does turning it into a computation expression help?

Once more we define a new type (`MaybeBuilder`) and make an instance of the type (`maybe`).

```fsharp
type MaybeBuilder() =

    member this.Bind(x, f) = 
        match x with
        | None -> None
        | Some a -> f a

    member this.Return(x) = 
        Some x
   
let maybe = new MaybeBuilder()
```

I have called this one `MaybeBuilder` rather than `divideByBuilder` because the issue of dealing with option types this way, using a computation expression, is quite common, and `maybe` is the standard name for this thing.

So now that we have defined the `maybe` workflow, let's rewrite the original code to use it.

```fsharp
let divideByWorkflow init x y z = 
    maybe 
        {
        let! a = init |> divideBy x
        let! b = a |> divideBy y
        let! c = b |> divideBy z
        return c
        }    
```

Much, much nicer. The `maybe` expression has completely hidden the branching logic!

And if we test it we get the same result as before:

```fsharp
let good = divideByWorkflow 12 3 2 1
let bad = divideByWorkflow 12 3 0 1
```


### Chains of "or else" tests

In the previous example of "divide by", we only wanted to continue if each step was successful.

But sometimes it is the other way around. Sometimes the flow of control depends on a series of "or else" tests. Try one thing, and if that succeeds, you're done. Otherwise try another thing, and if that fails, try a third thing, and so on.

Let's look at a simple example. Say that we have three dictionaries and we want to find the value corresponding to a key. Each lookup might succeed or fail, so we need to chain the lookups in a series.

```fsharp
let map1 = [ ("1","One"); ("2","Two") ] |> Map.ofList
let map2 = [ ("A","Alice"); ("B","Bob") ] |> Map.ofList
let map3 = [ ("CA","California"); ("NY","New York") ] |> Map.ofList

let multiLookup key =
    match map1.TryFind key with
    | Some result1 -> Some result1   // success
    | None ->   // failure
        match map2.TryFind key with
        | Some result2 -> Some result2 // success
        | None ->   // failure
            match map3.TryFind key with
            | Some result3 -> Some result3  // success
            | None -> None // failure
```

Because everything is an expression in F# we can't do an early return, we have to cascade all the tests in a single expression.
                
Here's how this might be used:

```fsharp
multiLookup "A" |> printfn "Result for A is %A" 
multiLookup "CA" |> printfn "Result for CA is %A" 
multiLookup "X" |> printfn "Result for X is %A" 
```

It works fine, but can it be simplified? 

Yes indeed. Here is an "or else" builder that allows us to simplify these kinds of lookups:

```fsharp
type OrElseBuilder() =
    member this.ReturnFrom(x) = x
    member this.Combine (a,b) = 
        match a with
        | Some _ -> a  // a succeeds -- use it
        | None -> b    // a fails -- use b instead
    member this.Delay(f) = f()

let orElse = new OrElseBuilder()
```

Here's how the lookup code could be altered to use it:

```fsharp
let map1 = [ ("1","One"); ("2","Two") ] |> Map.ofList
let map2 = [ ("A","Alice"); ("B","Bob") ] |> Map.ofList
let map3 = [ ("CA","California"); ("NY","New York") ] |> Map.ofList

let multiLookup key = orElse {
    return! map1.TryFind key
    return! map2.TryFind key
    return! map3.TryFind key
    }
```

Again we can confirm that the code works as expected.

```fsharp
multiLookup "A" |> printfn "Result for A is %A" 
multiLookup "CA" |> printfn "Result for CA is %A" 
multiLookup "X" |> printfn "Result for X is %A" 
```

### Asynchronous calls with callbacks

Finally, let's look at callbacks.  The standard approach for doing asynchronous operations in .NET is to use a [AsyncCallback delegate](http://msdn.microsoft.com/en-us/library/ms228972.aspx) which gets called when the async operation is complete.

Here is an example of how a web page might be downloaded using this technique:

```fsharp
open System.Net
let req1 = HttpWebRequest.Create("http://fsharp.org")
let req2 = HttpWebRequest.Create("http://google.com")
let req3 = HttpWebRequest.Create("http://bing.com")

req1.BeginGetResponse((fun r1 -> 
    use resp1 = req1.EndGetResponse(r1)
    printfn "Downloaded %O" resp1.ResponseUri

    req2.BeginGetResponse((fun r2 -> 
        use resp2 = req2.EndGetResponse(r2)
        printfn "Downloaded %O" resp2.ResponseUri

        req3.BeginGetResponse((fun r3 -> 
            use resp3 = req3.EndGetResponse(r3)
            printfn "Downloaded %O" resp3.ResponseUri

            ),null) |> ignore
        ),null) |> ignore
    ),null) |> ignore
```

Lots of calls to `BeginGetResponse` and `EndGetResponse`, and the use of nested lambdas, makes this quite complicated to understand. The important code (in this case, just print statements) is obscured by the callback logic.

In fact, managing this cascading approach is always a problem in code that requires a chain of callbacks; it has even been called the ["Pyramid of Doom"](http://raynos.github.com/presentation/shower/controlflow.htm?full#PyramidOfDoom) (although [none of the solutions are very elegant](http://adamghill.com/callbacks-considered-a-smell/), IMO).

Of course, we would never write that kind of code in F#, because F# has the `async` computation expression built in, which both simplifies the logic and flattens the code.

```fsharp
open System.Net
let req1 = HttpWebRequest.Create("http://fsharp.org")
let req2 = HttpWebRequest.Create("http://google.com")
let req3 = HttpWebRequest.Create("http://bing.com")

async {
    use! resp1 = req1.AsyncGetResponse()  
    printfn "Downloaded %O" resp1.ResponseUri

    use! resp2 = req2.AsyncGetResponse()  
    printfn "Downloaded %O" resp2.ResponseUri

    use! resp3 = req3.AsyncGetResponse()  
    printfn "Downloaded %O" resp3.ResponseUri

    } |> Async.RunSynchronously
```

We'll see exactly how the `async` workflow is implemented later in this series.

## Summary ##

So we've seen some very simple examples of computation expressions, both "before" and "after",
and they are quite representative of the kinds of problems that computation expressions are useful for.

* In the logging example, we wanted to perform some side-effect between each step.
* In the safe division example, we wanted to handle errors elegantly so that we could focus on the happy path.
* In the multiple dictionary lookup example, we wanted to return early with the first success.
* And finally, in the async example, we wanted to hide the use of callbacks and avoid the "pyramid of doom".

What all the cases have in common is that the computation expression is "doing something behind the scenes" between each expression. 

If you want a bad analogy, you can think of a computation expression as somewhat like a post-commit hook for SVN or git, or a database trigger that gets called on every update.
And really, that's all that a computation expression is: something that allows you to sneak your own code in to be called *in the background*, which in turn allows you to focus on the important code in the foreground. 

Why are they called "computation expressions"? Well, it's obviously some kind of expression, so that bit is obvious. I believe that the F# team did originally want to call it "expression-that-does-something-in-the-background-between-each-let" but for some reason, people thought that was a bit unwieldy, so they settled on the shorter name "computation expression" instead.

And as to the difference between a "computation expression" and a "workflow", I use *"computation expression"* to mean the `{...}` and `let!` syntax, and reserve *"workflow"* for particular implementations where appropriate. Not all computation expression implementations are workflows. For example, it is appropriate to talk about the "async workflow" or the "maybe workflow", but the "seq workflow" doesn't sound right.

In other words, in the following code, I would say that `maybe` is the workflow we are using, and the particular chunk of code `{ let! a = .... return c }` is the computation expression.

```fsharp
maybe 
    {
    let! a = x |> divideBy y 
    let! b = a |> divideBy w
    let! c = b |> divideBy z
    return c
    }    
```

You probably want to start creating your own computation expressions now, but first we need to take a short detour into continuations. That's up next.


*Update on 2015-01-11: I have removed the counting example that used a "state" computation expression. It was too confusing and distracted from the main concepts.*