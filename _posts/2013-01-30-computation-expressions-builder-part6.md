---
layout: post
title: "Implementing a builder: The rest of the standard methods"
description: "Implementing While, Using, and exception handling"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 11
---

We're coming into the home stretch now. There are only a few more builder methods that need to be covered, and then you will be ready to tackle anything! 

These methods are:

* `While` for repetition.
* `TryWith` and `TryFinally` for handling exceptions.
* `Use` for managing disposables

Remember, as always, that not all methods need to be implemented. If `While` is not relevant to you, don't bother with it.

One important note before we get started: **all the methods discussed here rely on [delays](/posts/computation-expressions-builder-part3/)** being used. If you are not using delay functions, then none of the methods will give the expected results.

## Implementing "While"

We all know what "while" means in normal code, but what does it mean in the context of a computation expression?
To understand, we have to revisit the concept of continuations again. 

In previous posts, we saw that a series of expressions is converted into a chain of continuations like this:

```fsharp
Bind(1,fun x -> 
   Bind(2,fun y -> 
     Bind(x + y,fun z -> 
        Return(z)  // or Yield
```

And this is the key to understanding a "while" loop -- it can be expanded in the same way.  

First, some terminology. A while loop has two parts:

* There is a test at the top of the "while" loop which is evaluated each time to determine whether the body should be run. When it evaluates to false, the while loop is "exited". In computation expressions, the test part is known as the **"guard"**. 
  The test function has no parameters, and returns a bool, so its signature is `unit -> bool`, of course.
* And there is the body of the "while" loop, evaluated each time until the "while" test fails. In computation expressions, this is a delay function that evaluates to a wrapped value. Since the body of the while loop is always the same, the same function is evaluated each time. 
  The body function has no parameters, and returns nothing, and so its signature is just `unit -> wrapped unit`.

With this in place, we can create pseudo-code for a while loop using continuations:

```fsharp
// evaluate test function
let bool = guard()  
if not bool 
then
    // exit loop
    return what??
else
    // evaluate the body function
    body()         
   
    // back to the top of the while loop 
    
    // evaluate test function again
    let bool' = guard()  
    if not bool' 
    then
        // exit loop
        return what??
    else 
        // evaluate the body function again
        body()         
        
        // back to the top of the while loop
        
        // evaluate test function a third time
        let bool'' = guard()  
        if not bool'' 
        then
            // exit loop
            return what??
        else
            // evaluate the body function a third time
            body()         
            
            // etc
```

One question that is immediately apparent is: what should be returned when the while loop test fails?  Well, we have seen this before with `if..then..`, and the answer is of course to use the `Zero` value. 

The next thing is that the `body()` result is being discarded. Yes, it is a unit function, so there is no value to return, but even so, in our expressions, we want to be able to hook into this so we can add behavior behind the scenes.  And of course, this calls for using the `Bind` function. 

So here is a revised version of the pseudo-code, using `Zero` and `Bind`:

```fsharp
// evaluate test function
let bool = guard()  
if not bool 
then
    // exit loop
    return Zero
else
    // evaluate the body function
    Bind( body(), fun () ->  
       
        // evaluate test function again
        let bool' = guard()  
        if not bool' 
        then
            // exit loop
            return Zero
        else 
            // evaluate the body function again
            Bind( body(), fun () ->  
            
                // evaluate test function a third time
                let bool'' = guard()  
                if not bool'' 
                then
                    // exit loop
                    return Zero
                else
                    // evaluate the body function again
                    Bind( body(), fun () ->  
                    
                    // etc
```

In this case, the continuation function passed into `Bind` has a unit parameter, because the `body` function does not have a value.

Finally, the pseudo-code can be simplified by collapsing it into a recursive function like this:

```fsharp
member this.While(guard, body) =
    // evaluate test function
    if not (guard()) 
    then 
        // exit loop
        this.Zero() 
    else
        // evaluate the body function 
        this.Bind( body(), fun () -> 
            // call recursively
            this.While(guard, body))  
```

And indeed, this is the standard "boiler-plate" implementation for `While` in almost all builder classes. 

It is a subtle but important point that the value of `Zero` must be chosen properly. In previous posts, we saw that we could set the value for `Zero` to be `None` or `Some ()` depending on the workflow.  For `While` to work however, the `Zero` *must be* set to `Some ()` and not `None`, because passing `None` into `Bind` will cause the whole thing to aborted early.

Also note that, although this is a recursive function, we didn't need the `rec` keyword. It is only needed for standalone functions that are recursive, not methods.

### "While" in use

Let's look at it being used in the `trace` builder.  Here's the complete builder class, with the `While` method:

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
        Some x

    member this.ReturnFrom(x) = 
        x
        
    member this.Zero() = 
        printfn "Zero"
        this.Return ()

    member this.Delay(f) = 
        printfn "Delay"
        f

    member this.Run(f) = 
        f()

    member this.While(guard, body) =
        printfn "While: test"
        if not (guard()) 
        then 
            printfn "While: zero"
            this.Zero() 
        else
            printfn "While: body"
            this.Bind( body(), fun () -> 
                this.While(guard, body))  

// make an instance of the workflow                
let trace = new TraceBuilder()
```

If you look at the signature for `While`, you will see that the `body` parameter is `unit -> unit option`, that is, a delayed function. As noted above, if you don't implement `Delay` properly, you will get unexpected behavior and cryptic compiler errors.

```fsharp
type TraceBuilder =
    // other members
    member
      While : guard:(unit -> bool) * body:(unit -> unit option) -> unit option

```

And here is a simple loop using a mutable value that is incremented each time round.

```fsharp
let mutable i = 1
let test() = i < 5
let inc() = i <- i + 1

let m = trace { 
    while test() do
        printfn "i is %i" i
        inc() 
    } 
```

## Handling exceptions with "try..with"

Exception handling is implemented in a similar way.

If we look at a `try..with` expression for example, it has two parts:

* There is the body of the "try", evaluated once. In a computation expressions, this will be a delayed function that evaluates to a wrapped value. The body function has no parameters, and so its signature is just `unit -> wrapped type`.
* The "with" part handles the exception. It has an exception as a parameters, and returns the same type as the "try" part, so its signature is `exception -> wrapped type`.

With this in place, we can create pseudo-code for the exception handler:

```fsharp
try
    let wrapped = delayedBody()  
    wrapped  // return a wrapped value
with
| e -> handlerPart e
```

And this maps exactly to a standard implementation:

```fsharp
member this.TryWith(body, handler) =
    try 
        printfn "TryWith Body"
        this.ReturnFrom(body())
    with 
        e ->
            printfn "TryWith Exception handling"
            handler e
```

As you can see, it is common to use pass the returned value through `ReturnFrom` so that it gets the same treatment as other wrapped values.

Here is an example snippet to test how the handling works:

```fsharp
trace { 
    try
        failwith "bang"
    with
    | e -> printfn "Exception! %s" e.Message
    } |> printfn "Result %A"
```


## Implementing "try..finally"

`try..finally` is very similar to `try..with`.

* There is the body of the "try", evaluated once. The body function has no parameters, and so its signature is `unit -> wrapped type`.
* The "finally" part is always called. It has no parameters, and returns a unit, so its signature is `unit -> unit`.

Just as with `try..with`, the standard implementation is obvious.

```fsharp
member this.TryFinally(body, compensation) =
    try 
        printfn "TryFinally Body"
        this.ReturnFrom(body())
    finally 
        printfn "TryFinally compensation"
        compensation() 
```

Another little snippet:

```fsharp
trace { 
    try
        failwith "bang"
    finally
        printfn "ok" 
    } |> printfn "Result %A"
```

## Implementing "using"

The final method to implement is `Using`.  This is the builder method for implementing the `use!` keyword.

This is what the MSDN documentation says about `use!`:

```text
{| use! value = expr in cexpr |} 
```

is translated to:

```text
builder.Bind(expr, (fun value -> builder.Using(value, (fun value -> {| cexpr |} ))))
```

In other words, the `use!` keyword triggers both a `Bind` and a `Using`. First a `Bind` is done to unpack the wrapped value,
and then the unwrapped disposable is passed into `Using` to ensure disposal, with the continuation function as the second parameter.

Implementing this is straightforward.  Similar to the other methods, we have a body, or continuation part, of the "using" expression, which is evaluated once. This body function has a "disposable" parameter, and so its signature is `#IDisposable -> wrapped type`.   

Of course we want to ensure that the disposable value is always disposed no matter what, so we need to wrap the call to the body function in a `TryFinally`.

Here's a standard implementation:

```fsharp
member this.Using(disposable:#System.IDisposable, body) =
    let body' = fun () -> body disposable
    this.TryFinally(body', fun () -> 
        match disposable with 
            | null -> () 
            | disp -> disp.Dispose())
```

Notes:

* The parameter to `TryFinally` is a `unit -> wrapped`, with a *unit* as the first parameter, so we created a delayed version of the body that is passed in.
* Disposable is a class, so it could be `null`, and we have to handle that case specially. Otherwise we just dispose it in the "finally" continuation.

Here's a demonstration of `Using` in action. Note that the `makeResource` makes a *wrapped* disposable.  If it wasn't wrapped, we wouldn't need the special
`use!` and could just use a normal `use` instead.

```fsharp
let makeResource name =
    Some { 
    new System.IDisposable with
    member this.Dispose() = printfn "Disposing %s" name
    }

trace { 
    use! x = makeResource "hello"
    printfn "Disposable in use"
    return 1
    } |> printfn "Result: %A" 
```


## "For" revisited

Finally, we can revisit how `For` is implemented.  In the previous examples, `For` took a simple list parameter. But with `Using` and `While` under our belts, we can change it to accept any `IEnumerable<_>` or sequence.

Here's the standard implementation for `For` now:

```fsharp
member this.For(sequence:seq<_>, body) =
       this.Using(sequence.GetEnumerator(),fun enum -> 
            this.While(enum.MoveNext, 
                this.Delay(fun () -> body enum.Current)))
 ```

As you can see, it is quite different from the previous implementation, in order to handle a generic `IEnumerable<_>`.

* We explicitly iterate using an `IEnumerator<_>`.
* `IEnumerator<_>` implements `IDisposable`, so we wrap the enumerator in a `Using`.
* We use `While .. MoveNext` to iterate.
* Next, we pass the `enum.Current` into the body function
* Finally, we delay the call to the body function using `Delay`

## Complete code without tracing 

Up to now, all the builder methods have been made more complex than necessary by the adding of tracing and printing expressions. The tracing is helpful to understand what is going on,
but it can obscure the simplicity of the methods.

So as a final step, let's have a look at the complete code for the "trace" builder class, but this time without any extraneous code at all.  Even though the code is cryptic, the purpose and implementation of each method should now be familiar to you.

```fsharp
type TraceBuilder() =

    member this.Bind(m, f) = 
        Option.bind f m

    member this.Return(x) = Some x

    member this.ReturnFrom(x) = x

    member this.Yield(x) = Some x

    member this.YieldFrom(x) = x
    
    member this.Zero() = this.Return ()

    member this.Delay(f) = f

    member this.Run(f) = f()

    member this.While(guard, body) =
        if not (guard()) 
        then this.Zero() 
        else this.Bind( body(), fun () -> 
            this.While(guard, body))  

    member this.TryWith(body, handler) =
        try this.ReturnFrom(body())
        with e -> handler e

    member this.TryFinally(body, compensation) =
        try this.ReturnFrom(body())
        finally compensation() 

    member this.Using(disposable:#System.IDisposable, body) =
        let body' = fun () -> body disposable
        this.TryFinally(body', fun () -> 
            match disposable with 
                | null -> () 
                | disp -> disp.Dispose())

    member this.For(sequence:seq<_>, body) =
        this.Using(sequence.GetEnumerator(),fun enum -> 
            this.While(enum.MoveNext, 
                this.Delay(fun () -> body enum.Current)))
                
```

After all this discussion, the code seems quite tiny now. And yet this builder implements every standard method, uses delayed functions.
A lot of functionality in a just a few lines!
