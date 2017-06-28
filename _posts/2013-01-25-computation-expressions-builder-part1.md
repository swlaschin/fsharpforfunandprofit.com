---
layout: post
title: "Implementing a builder: Zero and Yield"
description: "Getting started with the basic builder methods"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 6
---

Having covered bind and continuations, and the use of wrapper types, we're finally ready to take on the full set of methods associated with "builder" classes.

If you look at the [MSDN documentation](http://msdn.microsoft.com/en-us/library/dd233182.aspx), you'll see not just `Bind` and `Return`, but also other strangely named methods like `Delay` and `Zero`. What are *they* for?  That's what this and the next few posts will answer.

## The plan of action

To demonstrate how to create a builder class, we will create a custom workflow which uses all of the possible builder methods.  

But rather than starting at the top and trying to explain what these methods mean without context, we'll work from the bottom up, starting with a simple workflow and adding methods only as needed to solve a problem or an error. In the process, you'll come to understand how F# processes computation expressions in detail.

The outline of this process is:

* Part 1: In this first part, we'll look at what methods are needed for a basic workflow. We'll introduce `Zero`, `Yield`, `Combine` and `For`.
* Part 2: Next, we'll look at how to delay the execution of your code, so that it is only evaluated when needed. We'll introduce `Delay` and `Run`, and look at lazy computations.
* Part 3: Finally, we'll cover the rest of the methods: `While`, `Using`, and exception handling.

## Before we get started

Before we dive into creating the workflow, here are some general comments.

### The documentation for computation expressions

First, as you might have noticed, the MSDN documentation for computation expressions is meagre at best, and although not inaccurate, can be misleading. For example, the signatures of the builder methods are *more* flexible than they appear to be, and this can be used to implement some features that might not be obvious if you work from the documentation alone. We will show an example of this later.

If you want more detailed documentation, there are two sources I can recommend. For an detailed overview of the concepts behind computation expressions, a great resource is the [paper "The F# Expression Zoo" by Tomas Petricek and Don Syme](http://tomasp.net/academic/papers/computation-zoo/computation-zoo.pdf). And for the most accurate up-to-date technical documentation, you should read the [F# language specification](http://research.microsoft.com/en-us/um/cambridge/projects/fsharp/manual/spec.pdf), which has a section on computation expressions. 

### Wrapped and unwrapped types

When you are trying to understand the signatures as documented, remember that what I have been calling the "unwrapped" type is normally written as `'T` and the "wrapped" type is normally written `M<'T>`. That is, when you see that the `Return` method has the signature `'T -> M<'T>` it means `Return` takes an unwrapped type and returns a wrapped type.

As I have in the earlier posts in this series, I will continue to use "unwrapped" and "wrapped" to describe the relationship between these types, but as we move forward these terms will be stretched to the breaking point, so I will also start using other terminology, such as "computation type" instead of "wrapped type". I hope that when we reach this point, the reason for the change will be clear and understandable.

Also, in my examples, I will generally try to keep things simple by using code such as:

<pre>
let! x = ...wrapped type value...
</pre>

But this is actually an oversimplification. To be precise, the "x" can be any *pattern* not just a single value, and the "wrapped type" value can,
of course, be an *expression* that evaluates to a wrapped type.
The MSDN documentation uses this more precise approach. It uses "pattern" and "expression" in the definitions, such as `let! pattern = expr in cexpr`.

Here are some examples of using patterns and expressions in a `maybe` computation expression,
where `Option` is the wrapped type, and the right hand side expressions are `options`: 

```fsharp
// let! pattern = expr in cexpr
maybe {
    let! x,y = Some(1,2) 
    let! head::tail = Some( [1;2;3] )
    // etc
    }
```

Having said this, I will continue to use the oversimplified examples, so as not to add extra complication to an already complicated topic!

### Implementing special methods in the builder class (or not)

The MSDN documentation shows that each special operation (such as `for..in`, or `yield`) is translated into one or more calls to methods in the builder class.

There is not always a one-to-one correspondence, but generally, to support the syntax for a special operation, you *must* implement a corresponding method in the builder class, otherwise the compiler will complain and give you an error.  

On the other hand, you do *not* need to implement every single method if you don't need the syntax. For example, we have already implemented the `maybe` workflow quite nicely by only implementing the two methods `Bind` and `Return`. We don't need to implement `Delay`, `Use`, and so on, if we don't need to use them.

To see what happens if you have not implemented a method, let's try to use the `for..in..do` syntax in our `maybe` workflow like this:

```fsharp
maybe { for i in [1;2;3] do i }
```

We will get the compiler error:

```text
This control construct may only be used if the computation expression builder defines a 'For' method
```

Sometimes you get will errors that might be cryptic unless you know what is going on behind the scenes. 
For example, if you forget to put `return` in your workflow, like this:

```fsharp
maybe { 1 }
```

You will get the compiler error:

```text
This control construct may only be used if the computation expression builder defines a 'Zero' method
```

You might be asking: what is the `Zero` method? And why do I need it?  The answer to that is coming right up.

### Operations with and without '!'

Obviously, many of the special operations come in pairs, with and without a "!" symbol. For example: `let` and `let!` (pronounced "let-bang"), `return` and `return!`, `yield` and `yield!` and so on.

The difference is easy to remember when you realize that the operations *without* a "!" always have *unwrapped* types on the right hand side, while the ones *with* a "!" always have *wrapped* types. 

So for example, using the `maybe` workflow, where `Option` is the wrapped type, we can compare the different syntaxes:

```fsharp
let x = 1           // 1 is an "unwrapped" type
let! x = (Some 1)   // Some 1 is a "wrapped" type
return 1            // 1 is an "unwrapped" type
return! (Some 1)    // Some 1 is a "wrapped" type
yield 1             // 1 is an "unwrapped" type
yield! (Some 1)     // Some 1 is a "wrapped" type
```

The "!" versions are particularly important for composition, because the wrapped type can be the result of *another* computation expression of the same type.

```fsharp
let! x = maybe {...)       // "maybe" returns a "wrapped" type

// bind another workflow of the same type using let!
let! aMaybe = maybe {...)  // create a "wrapped" type
return! aMaybe             // return it

// bind two child asyncs inside a parent async using let!
let processUri uri = async {
    let! html = webClient.AsyncDownloadString(uri)
    let! links = extractLinks html
    ... etc ...
    }
```

## Diving in - creating a minimal implementation of a workflow

Let's start! We'll begin by creating a minimal version of the "maybe" workflow (which we'll rename as "trace") with every method instrumented, so we can see what is going on. We'll use this as our testbed throughout this post.

Here's the code for the first version of the `trace` workflow:

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

// make an instance of the workflow 
let trace = new TraceBuilder()
```

Nothing new here, I hope. We have already seen all these methods before.

Now let's run some sample code through it:

```fsharp
trace { 
    return 1
    } |> printfn "Result 1: %A" 

trace { 
    return! Some 2
    } |> printfn "Result 2: %A" 

trace { 
    let! x = Some 1
    let! y = Some 2
    return x + y
    } |> printfn "Result 3: %A" 

trace { 
    let! x = None
    let! y = Some 1
    return x + y
    } |> printfn "Result 4: %A" 
```

Everything should work as expected, in particular, you should be able to see that the use of `None` in the 4th example caused the next two lines (`let! y = ... return x+y`) to be skipped and the result of the whole expression was `None`.

## Introducing "do!"

Our expression supports `let!`, but what about `do!`? 

In normal F#, `do` is just like `let`, except that the expression doesn't return anything useful (namely, a unit value).

Inside a computation expression, `do!` is very similar. Just as `let!` passes a wrapped result to the `Bind` method, so does `do!`, except that in the case of `do!` the "result" is the unit value, and so a *wrapped* version of unit is passed to the bind method.  

Here is a simple demonstration using the `trace` workflow:

```fsharp
trace { 
    do! Some (printfn "...expression that returns unit")
    do! Some (printfn "...another expression that returns unit")
    let! x = Some (1)
    return x
    } |> printfn "Result from do: %A" 
```

Here is the output:

<pre>
...expression that returns unit
Binding with Some(&lt;null>). Continuing
...another expression that returns unit
Binding with Some(&lt;null>). Continuing
Binding with Some(1). Continuing
Returning a unwrapped 1 as an option
Result from do: Some 1
</pre>

You can verify for yourself that a `unit option` is being passed to `Bind` as a result of each `do!`.

## Introducing "Zero"

What is the smallest computation expression you can get away with? Let's try nothing at all:

```fsharp
trace { 
    } |> printfn "Result for empty: %A" 
```

We get an error immediately:

```text
This value is not a function and cannot be applied
```

Fair enough. If you think about it, it doesn't make sense to have nothing at all in a computation expression. After all, it's purpose is to chain expressions together.

Next, what about a simple expression with no `let!` or `return`?
 
```fsharp
trace { 
    printfn "hello world"
    } |> printfn "Result for simple expression: %A" 
```

Now we get a different error:

```text
This control construct may only be used if the computation expression builder defines a 'Zero' method
```

So why is the `Zero` method needed now but we haven't needed it before? The answer is that in this particular case we haven't returned anything explicitly, yet the computation expression as a whole *must* return a wrapped value. So what value should it return?

In fact, this situation will occur any time the return value of the computation expression has not been explicitly given. The same thing happens if you have an `if..then` expression without an else clause. 

```fsharp
trace { 
    if false then return 1
    } |> printfn "Result for if without else: %A" 
```

In normal F# code, an "if..then" without an "else" would result in a unit value, but in a computation expression, the particular return value must be a member of the wrapped type, and the compiler does not know what value this is.

The fix is to tell the compiler what to use -- and that is the purpose of the `Zero` method. 

### What value should you use for Zero?

So which value *should* you use for `Zero`? It depends on the kind of workflow you are creating. 

Here are some guidelines that might help:

* **Does the workflow have a concept of "success" or "failure"?** If so, use the "failure" value for `Zero`. For example, in our `trace` workflow, we use `None` to indicate failure, and so we can use `None` as the Zero value.
* **Does the workflow have a concept of "sequential processing"?** That is, in your workflow you do one step and then another, with some processing behind the scenes.  In normal F# code, an expression that did return anything explicitly would evaluate to unit. So to parallel this case, your `Zero` should be the *wrapped* version of unit. For example, in a variant on an option-based workflow, we might use `Some ()` to mean `Zero` (and by the way, this would always be the same as `Return ()` as well).
* **Is the workflow primarily concerned with manipulating data structures?** If so, `Zero` should be the "empty" data structure. For example, in a "list builder" workflow, we would use the empty list as the Zero value.

The `Zero` value also has an important role to play when combining wrapped types. So stay tuned, and we'll revisit Zero in the next post.

### A Zero implementation

So now let's extend our testbed class with a `Zero` method that returns `None`, and try again.

```fsharp
type TraceBuilder() =
    // other members as before
    member this.Zero() = 
        printfn "Zero"
        None

// make a new instance        
let trace = new TraceBuilder()

// test
trace { 
    printfn "hello world"
    } |> printfn "Result for simple expression: %A" 

trace { 
    if false then return 1
    } |> printfn "Result for if without else: %A" 
```

The test code makes it clear that `Zero` is being called behind the scenes. And `None` is the return value for the expression as whole. *Note: `None` may print out as `<null>`. You can ignore this.*

### Do you always need a Zero?

Remember, you *not required* to have a `Zero`, but only if it makes sense in the context of the workflow. For example `seq` does not allow zero, but `async` does:

```fsharp
let s = seq {printfn "zero" }    // Error
let a = async {printfn "zero" }  // OK
```


## Introducing "Yield"

In C#, there is a "yield" statement that, within an iterator, is used to return early and then picks up where you left off when you come back.

And looking at the docs, there is a "yield" available in F# computation expressions as well. What does it do? Let's try it and see.

```fsharp
trace { 
    yield 1
    } |> printfn "Result for yield: %A" 
```

And we get the error:

```text
This control construct may only be used if the computation expression builder defines a 'Yield' method
```

No surprise there. So what should the implementation of "yield" method look like?  The MSDN documentation says that it has the signature `'T -> M<'T>`, which is exactly the same as the signature for the `Return` method. It must take an unwrapped value and wrap it.

So let's implement it the same way as `Return` and retry the test expression.

```fsharp
type TraceBuilder() =
    // other members as before

    member this.Yield(x) = 
        printfn "Yield an unwrapped %A as an option" x
        Some x

// make a new instance        
let trace = new TraceBuilder()

// test
trace { 
    yield 1
    } |> printfn "Result for yield: %A" 
```

This works now, and it seems that it can be used as an exact substitute for `return`. 

There is a also a `YieldFrom` method that parallels the `ReturnFrom` method. And it behaves the same way, allowing you to yield a wrapped value rather than a unwrapped one.

So let's add that to our list of builder methods as well:

```fsharp
type TraceBuilder() =
    // other members as before

    member this.YieldFrom(m) = 
        printfn "Yield an option (%A) directly" m
        m

// make a new instance        
let trace = new TraceBuilder()

// test
trace { 
    yield! Some 1
    } |> printfn "Result for yield!: %A" 
```

At this point you might be wondering: if `return` and `yield` are basically the same thing, why are there two different keywords?  The answer is mainly so that you can enforce appropriate syntax by implementing one but not the other.  For example, the `seq` expression *does* allow `yield` but *doesn't* allow `return`, while the `async` does allow `return`, but does not allow `yield`, as you can see from the snippets below.  

```fsharp
let s = seq {yield 1}    // OK
let s = seq {return 1}   // error

let a = async {return 1} // OK
let a = async {yield 1}  // error
```

In fact, you could create slightly different behavior for `return` vs. `yield`, so that, for example, using `return` stops the rest of the computation expression from being evaluated, while `yield` doesn't.

More generally, of course, `yield` should be used for sequence/enumeration semantics, while `return` is normally used once per expression. (We'll see how `yield` can be used multiple times in the next post.)

## Revisiting "For" 

We talked about the `for..in..do` syntax in the last post. So now let's revisit the "list builder" that we discussed earlier and add the extra methods. We already saw how to define `Bind` and `Return` for a list in a previous post, so we just need to implement the additional methods. 

* The `Zero` method just returns an empty list. 
* The `Yield` method can be implemented in the same way as `Return`.
* The `For` method can be implemented the same as `Bind`.

```fsharp
type ListBuilder() =
    member this.Bind(m, f) = 
        m |> List.collect f

    member this.Zero() = 
        printfn "Zero"
        []

    member this.Return(x) = 
        printfn "Return an unwrapped %A as a list" x
        [x]

    member this.Yield(x) = 
        printfn "Yield an unwrapped %A as a list" x
        [x]
        
    member this.For(m,f) =
        printfn "For %A" m
        this.Bind(m,f)

// make an instance of the workflow                
let listbuilder = new ListBuilder()
```

And here is the code using `let!`:

```fsharp
listbuilder { 
    let! x = [1..3]
    let! y = [10;20;30]
    return x + y
    } |> printfn "Result: %A" 
```

And here is the equivalent code using `for`:    

```fsharp
listbuilder { 
    for x in [1..3] do
    for y in [10;20;30] do
    return x + y
    } |> printfn "Result: %A" 
```

You can see that both approaches give the same result.

## Summary  

In this post, we've seen how to implement the basic methods for a simple computation expression.  

Some points to reiterate:

* For simple expressions you don't need to implement all the methods.
* Things with bangs have wrapped types on the right hand side.
* Things without bangs have unwrapped types on the right hand side.
* You need to implement `Zero` if you want a workflow that doesn't explicitly return a value.
* `Yield` is basically equivalent to `Return`, but `Yield` should be used for sequence/enumeration semantics.
* `For` is basically equivalent to `Bind` in simple cases.

In the next post, we'll look at what happens when we need to combine multiple values.
