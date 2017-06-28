---
layout: post
title: "Computation expressions and wrapper types"
description: "Using types to assist the workflow"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 4
---

In the previous post, we were introduced to the "maybe" workflow, which allowed us to hide the messiness of chaining together option types.

A typical use of the "maybe" workflow looked something like this:

```fsharp
let result = 
    maybe 
        {
        let! anInt = expression of Option<int>
        let! anInt2 = expression of Option<int>
        return anInt + anInt2 
        }    
```

As we saw before, there is some apparently strange behavior going on here:

* In the `let!` lines, the expression on the *right* of the equals is an `int option`, but the value on the *left* is just an `int`. The `let!` has "unwrapped" the option before binding it to the value. 

* And in the `return` line, the opposite occurs. The expression being returned is an `int`, but the value of the whole workflow (`result`) is an `int option`. That is, the `return` has "wrapped" the raw value back into an option.

We will follow up these observations in this post, and we will see that this leads to one of the major uses of computation expressions: namely, to implicitly unwrap and rewrap values that are stored in some sort of wrapper type.

## Another example 

Let's look at another example. Say that we are accessing a database, and we want to capture the result in a Success/Error union type, like this:

```fsharp
type DbResult<'a> = 
    | Success of 'a
    | Error of string
```

We then use this type in our database access methods. Here are some very simple stubs to give you an idea of how the `DbResult` type might be used:

```fsharp
let getCustomerId name =
    if (name = "") 
    then Error "getCustomerId failed"
    else Success "Cust42"

let getLastOrderForCustomer custId =
    if (custId = "") 
    then Error "getLastOrderForCustomer failed"
    else Success "Order123"

let getLastProductForOrder orderId =
    if (orderId  = "") 
    then Error "getLastProductForOrder failed"
    else Success "Product456"
```


Now let's say we want to chain these calls together. First get the customer id from the name, and then get the order for the customer id, and then get the product from the order.

Here's the most explicit way of doing it. As you can see, we have to have pattern matching at each step.

```fsharp
let product = 
    let r1 = getCustomerId "Alice"
    match r1 with 
    | Error _ -> r1
    | Success custId ->
        let r2 = getLastOrderForCustomer custId 
        match r2 with 
        | Error _ -> r2
        | Success orderId ->
            let r3 = getLastProductForOrder orderId 
            match r3 with 
            | Error _ -> r3
            | Success productId ->
                printfn "Product is %s" productId
                r3
```

Really ugly code. And the top-level flow has been submerged in the error handling logic.  

Computation expressions to the rescue!  We can write one that handles the branching of Success/Error behind the scenes:

```fsharp
type DbResultBuilder() =

    member this.Bind(m, f) = 
        match m with
        | Error _ -> m
        | Success a -> 
            printfn "\tSuccessful: %s" a
            f a

    member this.Return(x) = 
        Success x

let dbresult = new DbResultBuilder()
```

And with this workflow, we can focus on the big picture and write much cleaner code:

```fsharp
let product' = 
    dbresult {
        let! custId = getCustomerId "Alice"
        let! orderId = getLastOrderForCustomer custId
        let! productId = getLastProductForOrder orderId 
        printfn "Product is %s" productId
        return productId
        }
printfn "%A" product'
```

And if there are errors, the workflow traps them nicely and tells us where the error was, as in this example below:

```fsharp
let product'' = 
    dbresult {
        let! custId = getCustomerId "Alice"
        let! orderId = getLastOrderForCustomer "" // error!
        let! productId = getLastProductForOrder orderId 
        printfn "Product is %s" productId
        return productId
        }
printfn "%A" product''
```


## The role of wrapper types in workflows

So now we have seen two workflows (the `maybe` workflow and the `dbresult` workflow), each with their own corresponding wrapper type (`Option<T>` and `DbResult<T>` respectively).

These are not just special cases. In fact, *every* computation expression *must* have an associated wrapper type. And the wrapper type is often designed specifically to go hand-in-hand with the workflow that we want to manage.

The example above demonstrates this clearly. The `DbResult` type we created is more than just a simple type for return values; it actually has a critical role in the workflow by "storing" the current state of the workflow, and whether it is succeeding or failing at each step. By using the various cases of the type itself, the `dbresult` workflow can manage the transitions for us, hiding them from view and enabling us to focus on the big picture.

We'll learn how to design a good wrapper type later in the series, but first let's look at how they are manipulated.


## Bind and Return and wrapper types

Let's look again at the definition of the `Bind` and `Return` methods of a computation expression.

We'll start off with the easy one, `Return`. The signature of `Return` [as documented on MSDN](http://msdn.microsoft.com/en-us/library/dd233182.aspx) is just this:

```fsharp
member Return : 'T -> M<'T>
```

In other words, for some type `T`, the `Return` method just wraps it in the wrapper type. 

*Note: In signatures, the wrapper type is normally called `M`, so `M<int>` is the wrapper type applied to `int` and `M<string>` is the wrapper type applied to `string`, and so on.*

And we've seen two examples of this usage. The `maybe` workflow returns a `Some`, which is an option type, and the `dbresult` workflow returns `Success`, which is part of the `DbResult` type.

```fsharp
// return for the maybe workflow
member this.Return(x) = 
    Some x

// return for the dbresult workflow
member this.Return(x) = 
    Success x
```

Now let's look at `Bind`.  The signature of `Bind` is this:

```fsharp
member Bind : M<'T> * ('T -> M<'U>) -> M<'U>
```

It looks complicated, so let's break it down.  It takes a tuple `M<'T> * ('T -> M<'U>)` and returns a `M<'U>`, where `M<'U>` means the wrapper type applied to type `U`.

The tuple in turn has two parts: 

* `M<'T>` is a wrapper around type `T`, and  
* `'T -> M<'U>` is a function that takes a *unwrapped* `T` and creates a *wrapped* `U`.

In other words, what `Bind` does is:

* Take a *wrapped* value.
* Unwrap it and do any special "behind the scenes" logic.
* Then, optionally apply the function to the *unwrapped* value to create a new *wrapped* value.
* Even if the function is *not* applied, `Bind` must still return a *wrapped* `U`.

With this understanding, here are the `Bind` methods that we have seen already:

```fsharp
// return for the maybe workflow
member this.Bind(m,f) = 
   match m with
   | None -> None
   | Some x -> f x

// return for the dbresult workflow
member this.Bind(m, f) = 
    match m with
    | Error _ -> m
    | Success x -> 
        printfn "\tSuccessful: %s" x
        f x
```

Look over this code and make sure that you understand why these methods do indeed follow the pattern described above.

Finally, a picture is always useful. Here is a diagram of the various types and functions:

![diagram of bind](/assets/img/bind.png)

* For `Bind`, we start with a wrapped value (`m` here), unwrap it to a raw value of type `T`, and then (maybe) apply the function `f` to it to get a wrapped value of type `U`.
* For `Return`, we start with a value (`x` here), and simply wrap it.


### The type wrapper is generic

Note that all the functions use generic types (`T` and `U`) other than the wrapper type itself, which must be the same throughout. For example, there is nothing stopping the `maybe` binding function from taking an `int` and returning a `Option<string>`, or taking a `string` and then returning an `Option<bool>`.  The only requirement is that it always return an `Option<something>`.

To see this, we can revisit the example above, but rather than using strings everywhere, we will create special types for the customer id, order id, and product id. This means that each step in the chain will be using a different type.

We'll start with the types again, this time defining `CustomerId`, etc.

```fsharp
type DbResult<'a> = 
    | Success of 'a
    | Error of string

type CustomerId =  CustomerId of string
type OrderId =  OrderId of int
type ProductId =  ProductId of string
```

The code is almost identical, except for the use of the new types in the `Success` line.

```fsharp
let getCustomerId name =
    if (name = "") 
    then Error "getCustomerId failed"
    else Success (CustomerId "Cust42")

let getLastOrderForCustomer (CustomerId custId) =
    if (custId = "") 
    then Error "getLastOrderForCustomer failed"
    else Success (OrderId 123)

let getLastProductForOrder (OrderId orderId) =
    if (orderId  = 0) 
    then Error "getLastProductForOrder failed"
    else Success (ProductId "Product456")
```


Here's the long-winded version again. 


```fsharp
let product = 
    let r1 = getCustomerId "Alice"
    match r1 with 
    | Error e -> Error e
    | Success custId ->
        let r2 = getLastOrderForCustomer custId 
        match r2 with 
        | Error e -> Error e
        | Success orderId ->
            let r3 = getLastProductForOrder orderId 
            match r3 with 
            | Error e -> Error e
            | Success productId ->
                printfn "Product is %A" productId
                r3
```

There are a couple of changes worth discussing: 

* First, the `printfn` at the bottom uses the "%A" format specifier rather than "%s". This is required because the `ProductId` type is a union type now.
* More subtly, there seems to be unnecessary code in the error lines. Why write `| Error e -> Error e`?  The reason is that the incoming error that is being matched against is of type `DbResult<CustomerId>` or `DbResult<OrderId>`, but the *return* value must be of type `DbResult<ProductId>`. So, even though the two `Error`s look the same, they are actually of different types. 

Next up, the builder, which hasn't changed at all except for the `| Error e -> Error e` line.

```fsharp
type DbResultBuilder() =

    member this.Bind(m, f) = 
        match m with
        | Error e -> Error e
        | Success a -> 
            printfn "\tSuccessful: %A" a
            f a

    member this.Return(x) = 
        Success x

let dbresult = new DbResultBuilder()
```

Finally, we can use the workflow as before.  

```fsharp
let product' = 
    dbresult {
        let! custId = getCustomerId "Alice"
        let! orderId = getLastOrderForCustomer custId
        let! productId = getLastProductForOrder orderId 
        printfn "Product is %A" productId
        return productId
        }
printfn "%A" product'
```

At each line, the returned value is of a *different* type (`DbResult<CustomerId>`,`DbResult<OrderId>`, etc), but because they have the same wrapper type in common, the bind works as expected.

And finally, here's the workflow with an error case.

```fsharp
let product'' = 
    dbresult {
        let! custId = getCustomerId "Alice"
        let! orderId = getLastOrderForCustomer (CustomerId "") //error
        let! productId = getLastProductForOrder orderId 
        printfn "Product is %A" productId
        return productId
        }
printfn "%A" product''
```


## Composition of computation expressions

We've seen that every computation expression *must* have an associated wrapper type. This wrapper type is used in both `Bind` and `Return`, which leads to a key benefit:

* *the output of a `Return` can be fed to the input of a `Bind`*

In other words, because a workflow returns a wrapper type, and because `let!` consumes a wrapper type, you can put a "child" workflow on the right hand side of a `let!` expression.

For example, say that you have a workflow called `myworkflow`. Then you can write the following:

```fsharp
let subworkflow1 = myworkflow { return 42 }
let subworkflow2 = myworkflow { return 43 }

let aWrappedValue = 
    myworkflow {
        let! unwrappedValue1 = subworkflow1
        let! unwrappedValue2 = subworkflow2
        return unwrappedValue1 + unwrappedValue2
        }
```

Or you can even "inline" them, like this:

```fsharp
let aWrappedValue = 
    myworkflow {
        let! unwrappedValue1 = myworkflow {
            let! x = myworkflow { return 1 }
            return x
            }
        let! unwrappedValue2 = myworkflow {
            let! y = myworkflow { return 2 }
            return y
            }
        return unwrappedValue1 + unwrappedValue2
        }
```

If you have used the `async` workflow, you probably have done this already, because an async workflow typically contains other asyncs embedded in it:

```fsharp
let a = 
    async {
        let! x = doAsyncThing  // nested workflow
        let! y = doNextAsyncThing x // nested workflow
        return x + y
    }
```

## Introducing "ReturnFrom"

We have been using `return` as a way of easily wrapping up an unwrapped return value.

But sometimes we have a function that already returns a wrapped value, and we want to return it directly.  `return` is no good for this, because it requires an unwrapped type as input.

The solution is a variant on `return` called `return!`, which takes a *wrapped type* as input and returns it.

The corresponding method in the "builder" class is called `ReturnFrom`. Typically the implementation just returns the wrapped type "as is" (although of course, you can always add extra logic behind the scenes).

Here is a variant on the "maybe" workflow to show how it can be used:

```fsharp
type MaybeBuilder() =
    member this.Bind(m, f) = Option.bind f m
    member this.Return(x) = 
        printfn "Wrapping a raw value into an option"
        Some x
    member this.ReturnFrom(m) = 
        printfn "Returning an option directly"
        m

let maybe = new MaybeBuilder()
```

And here it is in use, compared with a normal `return`.

```fsharp
// return an int
maybe { return 1  }

// return an Option
maybe { return! (Some 2)  }
```

For a more realistic example, here is `return!` used in conjunction with `divideBy`:

```fsharp
// using return
maybe 
    {
    let! x = 12 |> divideBy 3
    let! y = x |> divideBy 2
    return y  // return an int
    }    

// using return!    
maybe 
    {
    let! x = 12 |> divideBy 3
    return! x |> divideBy 2  // return an Option
    }    
```

## Summary

This post introduced wrapper types and how they related to `Bind`, `Return` and `ReturnFrom`, the core methods of any builder class.

In the next post, we'll continue to look at wrapper types, including using lists as wrapper types.
    
