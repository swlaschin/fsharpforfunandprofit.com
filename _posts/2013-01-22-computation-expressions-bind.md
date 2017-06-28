---
layout: post
title: "Introducing 'bind'"
description: "Steps towards creating our own 'let!' "
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 3
---

In the last post we talked about how we can think of `let` as a nice syntax for doing continuations behind scenes.
And we introduced a `pipeInto` function that allowed us to add hooks into the continuation pipeline.

Now we are ready to look at our first builder method, `Bind`, which formalizes this approach and is the core of any computation expression.

### Introducing "Bind "

The [MSDN page on computation expressions](http://msdn.microsoft.com/en-us/library/dd233182.aspx) describes the `let!` expression as syntactic sugar for a `Bind` method. Let's look at this again:

Here's the `let!` expression documentation, along with a real example:

```fsharp
// documentation
{| let! pattern = expr in cexpr |}

// real example
let! x = 43 in some expression
```

And here's the `Bind` method documentation, along with a real example:

```fsharp
// documentation
builder.Bind(expr, (fun pattern -> {| cexpr |}))

// real example
builder.Bind(43, (fun x -> some expression))
```

Notice a few interesting things about this:

* `Bind` takes two parameters, an expression (`43`) and a lambda. 
* The parameter of the lambda (`x`) is bound to the expression passed in as the first parameter. (In this case at least. More on this later.)
* The parameters of `Bind` are reversed from the order they are in `let!`.

So in other words, if we chain a number of `let!` expressions together like this:

```fsharp
let! x = 1
let! y = 2
let! z = x + y
``` 

the compiler converts it to calls to `Bind`, like this:

```fsharp
Bind(1, fun x ->
Bind(2, fun y ->
Bind(x + y, fun z ->
etc
``` 

I think you can see where we are going with this by now.

Indeed, our `pipeInto` function is exactly the same as the `Bind` method. 

This is a key insight: *computation expressions are just a way to create nice syntax for something that we could do ourselves*.

### A standalone bind function

Having a "bind" function like this is actually a standard functional pattern, and it is not dependent on computation expressions at all.

First, why is it called "bind"? Well, as we've seen, a "bind" function or method can be thought of as feeding an input value to a function. This is known as "[binding](/posts/function-values-and-simple-values/)" a value to the parameter of the function (recall that all functions have only [one parameter](/posts/currying/)).

So when you think of `bind` this this way, you can see that it is similar to piping or composition.

In fact, you can turn it into an infix operation like this:

```fsharp
let (>>=) m f = pipeInto(m,f)
```

*By the way, this symbol ">>=" is the standard way of writing bind as an infix operator. If you ever see it used in other F# code, that is probably what it represents.*

Going back to the safe divide example, we can now write the workflow on one line, like this:

```fsharp
let divideByWorkflow x y w z = 
    x |> divideBy y >>= divideBy w >>= divideBy z 
```

You might be wondering exactly how this is different from normal piping or composition? It's not immediately obvious.

The answer is twofold:

* First, the `bind` function has *extra* customized behavior for each situation. It is not a generic function, like pipe or composition.

* Second, the input type of the value parameter (`m` above) is not necessarily the same as the output type of the function parameter (`f` above), and so one of the things that bind does is handle this mismatch elegantly so that functions can be chained.

As we will see in the next post, bind generally works with some "wrapper" type. The value parameter might be of `WrapperType<TypeA>`, and then the signature of the function parameter of `bind` function is always `TypeA -> WrapperType<TypeB>`. 

In the particular case of the `bind` for safe divide, the wrapper type is `Option`. The type of the value parameter (`m` above) is `Option<int>` and the signature of the function parameter (`f` above) is `int -> Option<int>`.

To see bind used in a different context, here is an example of the logging workflow expressed using a infix bind function:

```fsharp
let (>>=) m f = 
    printfn "expression is %A" m
    f m

let loggingWorkflow = 
    1 >>= (+) 2 >>= (*) 42 >>= id
```

In this case, there is no wrapper type. Everything is an `int`. But even so, `bind` has the special behavior that performs the logging behind the scenes.

## Option.bind and the "maybe" workflow revisited

In the F# libraries, you will see `Bind` functions or methods in many places. Now you know what they are for!

A particularly useful one is `Option.bind`, which does exactly what we wrote by hand above, namely

* If the input parameter is `None`, then don't call the continuation function.
* If the input parameter is `Some`, then do call the continuation function, passing in the contents of the `Some`.

Here was our hand-crafted function:

```fsharp
let pipeInto (m,f) =
   match m with
   | None -> 
       None
   | Some x -> 
       x |> f
```

And here is the implementation of `Option.bind`:

```fsharp
module Option = 
    let bind f m =
       match m with
       | None -> 
           None
       | Some x -> 
           x |> f 
```

There is a moral in this -- don't be too hasty to write your own functions. There may well be library functions that you can reuse.

Here is the "maybe" workflow, rewritten to use `Option.bind`:

```fsharp
type MaybeBuilder() =
    member this.Bind(m, f) = Option.bind f m
    member this.Return(x) = Some x
```


## Reviewing the different approaches so far ##

We've used four different approaches for the "safe divide" example so far. Let's put them together side by side and compare them once more.

*Note: I have renamed the original `pipeInto` function to `bind`, and used `Option.bind` instead of our original custom implementation.*

First the original version, using an explicit workflow:

```fsharp
module DivideByExplicit = 

    let divideBy bottom top =
        if bottom = 0
        then None
        else Some(top/bottom)

    let divideByWorkflow x y w z = 
        let a = x |> divideBy y 
        match a with
        | None -> None  // give up
        | Some a' ->    // keep going
            let b = a' |> divideBy w
            match b with
            | None -> None  // give up
            | Some b' ->    // keep going
                let c = b' |> divideBy z
                match c with
                | None -> None  // give up
                | Some c' ->    // keep going
                    //return 
                    Some c'
    // test
    let good = divideByWorkflow 12 3 2 1
    let bad = divideByWorkflow 12 3 0 1
```

Next, using our own version of "bind"  (a.k.a. "pipeInto")      

```fsharp
module DivideByWithBindFunction = 

    let divideBy bottom top =
        if bottom = 0
        then None
        else Some(top/bottom)

    let bind (m,f) =
        Option.bind f m

    let return' x = Some x
       
    let divideByWorkflow x y w z = 
        bind (x |> divideBy y, fun a ->
        bind (a |> divideBy w, fun b ->
        bind (b |> divideBy z, fun c ->
        return' c 
        )))

    // test
    let good = divideByWorkflow 12 3 2 1
    let bad = divideByWorkflow 12 3 0 1
```

Next, using a computation expression:

```fsharp
module DivideByWithCompExpr = 

    let divideBy bottom top =
        if bottom = 0
        then None
        else Some(top/bottom)

    type MaybeBuilder() =
        member this.Bind(m, f) = Option.bind f m
        member this.Return(x) = Some x

    let maybe = new MaybeBuilder()

    let divideByWorkflow x y w z = 
        maybe 
            {
            let! a = x |> divideBy y 
            let! b = a |> divideBy w
            let! c = b |> divideBy z
            return c
            }    

    // test
    let good = divideByWorkflow 12 3 2 1
    let bad = divideByWorkflow 12 3 0 1
```

And finally, using bind as an infix operation:

```fsharp
module DivideByWithBindOperator = 

    let divideBy bottom top =
        if bottom = 0
        then None
        else Some(top/bottom)

    let (>>=) m f = Option.bind f m

    let divideByWorkflow x y w z = 
        x |> divideBy y 
        >>= divideBy w 
        >>= divideBy z 

    // test
    let good = divideByWorkflow 12 3 2 1
    let bad = divideByWorkflow 12 3 0 1
```

Bind functions turn out to be very powerful. In the next post we'll see that combining `bind` with wrapper types creates an elegant way of passing extra information around in the background. 

## Exercise: How well do you understand?

Before you move on to the next post, why don't you test yourself to see if you have understood everything so far?

Here is a little exercise for you.

**Part 1 - create a workflow** 

First, create a function that parses a string into a int:

```fsharp
let strToInt str = ???
```

and then create your own computation expression builder class so that you can use it in a workflow, as shown below.

```fsharp
let stringAddWorkflow x y z = 
    yourWorkflow 
        {
        let! a = strToInt x
        let! b = strToInt y
        let! c = strToInt z
        return a + b + c
        }    

// test
let good = stringAddWorkflow "12" "3" "2"
let bad = stringAddWorkflow "12" "xyz" "2"
```

**Part 2 -- create a bind function** 

Once you have the first part working, extend the idea by adding two more functions:

```fsharp
let strAdd str i = ???
let (>>=) m f = ???
```

And then with these functions, you should be able to write code like this:

```fsharp
let good = strToInt "1" >>= strAdd "2" >>= strAdd "3"
let bad = strToInt "1" >>= strAdd "xyz" >>= strAdd "3"
```


## Summary ##

Here's a summary of the points covered in this post:

* Computation expressions provide a nice syntax for continuation passing, hiding the chaining logic for us.
* `bind` is the key function that links the output of one step to the input of the next step.
* The symbol `>>=` is the standard way of writing bind as an infix operator. 
