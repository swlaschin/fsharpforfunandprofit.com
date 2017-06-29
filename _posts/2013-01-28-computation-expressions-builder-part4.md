---
layout: post
title: "Implementing a builder: Overloading"
description: "Stupid method tricks"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 9
---

In this post, we'll take a detour and look at some tricks you can do with methods in a computation expression builder.

Ultimately, this detour will lead to a dead end, but I hope the journey might provide some more insight into good practices for designing your own computation expressions.

## An insight: builder methods can be overloaded

At some point, you might have an insight: 

* The builder methods are just normal class methods, and unlike standalone functions, methods can support [overloading with different parameter types](/posts/type-extensions/#method-overloading), which means we can create *different implementations* of any method, as long as the parameter types are different.

So then you might get excited about this and how it could be used. But it turns out to be less useful than you might think. Let's look at some examples.

## Overloading "return"

Say that you have a union type. You might consider overloading `Return` or `Yield` with multiple implementations for each union case. 

For example, here's a very simple example where `Return` has two overloads:

```fsharp
type SuccessOrError = 
| Success of int
| Error of string

type SuccessOrErrorBuilder() =
    
    member this.Bind(m, f) = 
        match m with
        | Success s -> f s
        | Error _ -> m

    /// overloaded to accept ints
    member this.Return(x:int) = 
        printfn "Return a success %i" x
        Success x

    /// overloaded to accept strings
    member this.Return(x:string) = 
        printfn "Return an error %s" x
        Error x

// make an instance of the workflow                
let successOrError = new SuccessOrErrorBuilder()

```

And here it is in use:

```fsharp
successOrError { 
    return 42
    } |> printfn "Result for success: %A" 
// Result for success: Success 42

successOrError { 
    return "error for step 1"
    } |> printfn "Result for error: %A" 
//Result for error: Error "error for step 1"    
```

What's wrong with this, you might think?

Well, first, if we go back to the [discussion on wrapper types](/posts/computation-expressions-wrapper-types-part2/), we made the point that wrapper types should be *generic*. Workflows should be reusable as much as possible -- why tie the implementation to any particular primitive type?

What that means in this case is that the union type should be resigned to look like this:  

```fsharp
type SuccessOrError<'a,'b> = 
| Success of 'a
| Error of 'b
```

But as a consequence of the generics, the `Return` method can't be overloaded any more!

Second, it's probably not a good idea to expose the internals of the type inside the expression like this anyway. The concept of "success" and "failure" cases is useful, but a better way would be to hide the "failure" case and handle it automatically inside `Bind`, like this:

```fsharp
type SuccessOrError<'a,'b> = 
| Success of 'a
| Error of 'b

type SuccessOrErrorBuilder() =
    
    member this.Bind(m, f) = 
        match m with
        | Success s -> 
            try
                f s
            with
            | e -> Error e.Message
        | Error _ -> m

    member this.Return(x) = 
        Success x

// make an instance of the workflow                
let successOrError = new SuccessOrErrorBuilder()
```

In this approach, `Return` is only used for success, and the failure cases are hidden.

```fsharp
successOrError { 
    return 42
    } |> printfn "Result for success: %A" 

successOrError { 
    let! x = Success 1
    return x/0
    } |> printfn "Result for error: %A" 
```

We'll see more of this technique in an upcoming post.

## Multiple Combine implementations

Another time when you might be tempted to overload a method is when implementing `Combine`.

Let's revisit the `Combine` method for the `trace` workflow. If you remember, in the previous implementation of `Combine`, we just added the numbers together. 

But what if we change our requirements, and say that:

* if we yield multiple values in the `trace` workflow, then we want to combine them into a list. 

A first attempt using combine might look this:

```fsharp
member this.Combine (a,b) = 
    match a,b with
    | Some a', Some b' ->
        printfn "combining %A and %A" a' b' 
        Some [a';b']
    | Some a', None ->
        printfn "combining %A with None" a' 
        Some [a']
    | None, Some b' ->
        printfn "combining None with %A" b' 
        Some [b']
    | None, None ->
        printfn "combining None with None"
        None
```

In the `Combine` method, we unwrap the value from the passed-in option and combine them into a list wrapped in a `Some` (e.g. `Some [a';b']`).

For two yields it works as expected:

```fsharp
trace { 
    yield 1
    yield 2
    } |> printfn "Result for yield then yield: %A" 
   
// Result for yield then yield: Some [1; 2]
```

And for a yielding a `None`, it also works as expected:

```fsharp
trace { 
    yield 1
    yield! None
    } |> printfn "Result for yield then None: %A" 

// Result for yield then None: Some [1]
```

But what happens if there are *three* values to combine? Like this:

```fsharp
trace { 
    yield 1
    yield 2
    yield 3
    } |> printfn "Result for yield x 3: %A" 
```

If we try this, we get a compiler error:

```text
error FS0001: Type mismatch. Expecting a
    int option    
but given a
    'a list option    
The type 'int' does not match the type ''a list'        
```
        
What is the problem?  

The answer is that after combining the 2nd and 3rd values (`yield 2; yield 3`), we get an option containing a *list of ints* or `int list option`. The error happens when we attempt to combine the first value (`Some 1`) with the combined value (`Some [2;3]`). That is, we are passing a `int list option` as the second parameter of `Combine`, but the first parameter is still a normal `int option`. The compiler is telling you that it wants the second parameter to be the same type as the first.

But, here's where we might want use our overloading trick. We can create *two* different implementations of `Combine`, with different types for the second parameter, one that takes an `int option` and the other taking an `int list option`.

So here are the two methods, with different parameter types:

```fsharp
/// combine with a list option
member this.Combine (a, listOption) = 
    match a,listOption with
    | Some a', Some list ->
        printfn "combining %A and %A" a' list 
        Some ([a'] @ list)
    | Some a', None ->
        printfn "combining %A with None" a'
        Some [a']
    | None, Some list ->
        printfn "combining None with %A" list
        Some list
    | None, None ->
        printfn "combining None with None"
        None

/// combine with a non-list option
member this.Combine (a,b) = 
    match a,b with
    | Some a', Some b' ->
        printfn "combining %A and %A" a' b' 
        Some [a';b']
    | Some a', None ->
        printfn "combining %A with None" a' 
        Some [a']
    | None, Some b' ->
        printfn "combining None with %A" b' 
        Some [b']
    | None, None ->
        printfn "combining None with None"
        None
```

Now if we try combining three results, as before, we get what we expect.

```fsharp
trace { 
    yield 1
    yield 2
    yield 3
    } |> printfn "Result for yield x 3: %A" 

// Result for yield x 3: Some [1; 2; 3]    
```

Unfortunately, this trick has broken some previous code! If you try yielding a `None` now, you will get a compiler error.

```fsharp
trace { 
    yield 1
    yield! None
    } |> printfn "Result for yield then None: %A" 
```

The error is:

```text
error FS0041: A unique overload for method 'Combine' could not be determined based on type information prior to this program point. A type annotation may be needed. 
```

But hold on, before you get too annoyed, try thinking like the compiler.  If you were the compiler, and you were given a `None`, which method would *you* call?

There is no correct answer, because a `None` could be passed as the second parameter to *either* method.  The compiler does not know where this is a None of type `int list option` (the first method) or a None of type `int option` (the second method).

As the compiler reminds us, a type annotation will help, so let's give it one. We'll force the None to be an `int option`.
        
```fsharp
trace { 
    yield 1
    let x:int option = None
    yield! x
    } |> printfn "Result for yield then None: %A" 
```        
        
This is ugly, of course, but in practice might not happen very often.  

More importantly, this is a clue that we have a bad design. Sometimes the computation expression returns an `'a option` and sometimes it returns an `'a list option`. We should be consistent in our design, so that the computation expression always returns the *same* type, no matter how many `yield`s are in it.

That is, if we *do* want to allow multiple `yield`s, then we should use `'a list option` as the wrapper type to begin with rather than just a plain option. In this case the `Yield` method would create the list option, and the `Combine` method could be collapsed to a single method again.

Here's the code for our third version:

```fsharp
type TraceBuilder() =
    member this.Bind(m, f) = 
        match m with 
        | None -> 
            printfn "Binding with None. Exiting."
        | Some a -> 
            printfn "Binding with Some(%A). Continuing" a
        Option.bind f m

    member this.Zero() = 
        printfn "Zero"
        None

    member this.Yield(x) = 
        printfn "Yield an unwrapped %A as a list option" x
        Some [x]

    member this.YieldFrom(m) = 
        printfn "Yield an option (%A) directly" m
        m

    member this.Combine (a, b) = 
        match a,b with
        | Some a', Some b' ->
            printfn "combining %A and %A" a' b'
            Some (a' @ b')
        | Some a', None ->
            printfn "combining %A with None" a'
            Some a'
        | None, Some b' ->
            printfn "combining None with %A" b'
            Some b'
        | None, None ->
            printfn "combining None with None"
            None

    member this.Delay(f) = 
        printfn "Delay"
        f()

// make an instance of the workflow                
let trace = new TraceBuilder()
```

And now the examples work as expected without any special tricks:

```fsharp
trace { 
    yield 1
    yield 2
    } |> printfn "Result for yield then yield: %A" 

// Result for yield then yield: Some [1; 2]

trace { 
    yield 1
    yield 2
    yield 3
    } |> printfn "Result for yield x 3: %A" 

// Result for yield x 3: Some [1; 2; 3]
    
trace { 
    yield 1
    yield! None
    } |> printfn "Result for yield then None: %A" 

// Result for yield then None: Some [1]
```

Not only is the code cleaner, but as in the `Return` example, we have made our code more generic as well, having gone from a specific type (`int option`) to a more generic type (`'a option`).

## Overloading "For"

One legitimate case where overloading might be needed is the `For` method.  Some possible reasons:

* You might want to support different kinds of collections (e.g. list *and* `IEnumerable`)
* You might have a more efficient looping implementation for certain kinds of collections. 
* You might have a "wrapped" version of a list (e.g. LazyList) and you want support looping for both unwrapped and wrapped values. 

Here's an example of our list builder that has been extended to support sequences as well as lists:

```fsharp
type ListBuilder() =
    member this.Bind(m, f) = 
        m |> List.collect f

    member this.Yield(x) = 
        printfn "Yield an unwrapped %A as a list" x
        [x]

    member this.For(m,f) =
        printfn "For %A" m
        this.Bind(m,f)

    member this.For(m:_ seq,f) =
        printfn "For %A using seq" m
        let m2 = List.ofSeq m
        this.Bind(m2,f)

// make an instance of the workflow                
let listbuilder = new ListBuilder()
```

And here is it in use:

```fsharp
listbuilder { 
    let list = [1..10]
    for i in list do yield i
    } |> printfn "Result for list: %A" 

listbuilder { 
    let s = seq {1..10}
    for i in s do yield i
    } |> printfn "Result for seq : %A" 
```

If you comment out the second `For` method, you will see the "sequence` example will indeed fail to compile. So the overload is needed.

## Summary 

So we've seen that methods can be overloaded if needed, but be careful at jumping to this solution immediately, because having to doing this may be a sign of a weak design.

In the next post, we'll go back to controlling exactly when the expressions get evaluated, this time using a delay *outside* the builder.
