---
layout: post
title: "Reinventing the Reader monad"
description: "Or, designing your own elevated world"
categories: ["Patterns"]
seriesId: "Map and Bind and Apply, Oh my!"
seriesOrder: 6
---

This post is the sixth in a series.
In the [first two posts](/posts/elevated-world/), I described some of the core functions for dealing with generic data types:  `map`, `bind`, and so on.
In the [third post](/posts/elevated-world-3/), I discussed "applicative" vs "monadic" style, and how to lift values and functions to be consistent with each other.
In the [fourth](/posts/elevated-world-4/) and [previous](/posts/elevated-world-5/) posts, I introduced `traverse` and `sequence`
as a way of working with lists of elevated values, and we saw this used in a practical example: downloading some URLs.

In this post, we'll finish up by working through another practical example, but this time we'll create our own "elevated world" as a way to deal with awkward code.
We'll see that this approach is so common that it has a name -- the "Reader monad".

## Series contents

Here's a list of shortcuts to the various functions mentioned in this series:

* **Part 1: Lifting to the elevated world**
  * [The `map` function](/posts/elevated-world/#map)
  * [The `return` function](/posts/elevated-world/#return)
  * [The `apply` function](/posts/elevated-world/#apply)
  * [The `liftN` family of functions](/posts/elevated-world/#lift)
  * [The `zip` function and ZipList world](/posts/elevated-world/#zip)
* **Part 2: How to compose world-crossing functions**    
  * [The `bind` function](/posts/elevated-world-2/#bind)
  * [List is not a monad. Option is not a monad.](/posts/elevated-world-2/#not-a-monad)
* **Part 3: Using the core functions in practice**  
  * [Independent and dependent data](/posts/elevated-world-3/#dependent)
  * [Example: Validation using applicative style and monadic style](/posts/elevated-world-3/#validation)
  * [Lifting to a consistent world](/posts/elevated-world-3/#consistent)
  * [Kleisli world](/posts/elevated-world-3/#kleisli)
* **Part 4: Mixing lists and elevated values**    
  * [Mixing lists and elevated values](/posts/elevated-world-4/#mixing)
  * [The `traverse`/`MapM` function](/posts/elevated-world-4/#traverse)
  * [The `sequence` function](/posts/elevated-world-4/#sequence)
  * ["Sequence" as a recipe for ad-hoc implementations](/posts/elevated-world-4/#adhoc)
  * [Readability vs. performance](/posts/elevated-world-4/#readability)
  * [Dude, where's my `filter`?](/posts/elevated-world-4/#filter)
* **Part 5: A real-world example that uses all the techniques**    
  * [Example: Downloading and processing a list of websites](/posts/elevated-world-5/#asynclist)
  * [Treating two worlds as one](/posts/elevated-world-5/#asyncresult)
* **Part 6: Designing your own elevated world** 
  * [Designing your own elevated world](/posts/elevated-world-6/#part6)
  * [Filtering out failures](/posts/elevated-world-6/#filtering)
  * [The Reader monad](/posts/elevated-world-6/#readermonad)
* **Part 7: Summary** 
  * [List of operators mentioned](/posts/elevated-world-7/#operators)
  * [Further reading](/posts/elevated-world-7/#further-reading)

<a id="part6"></a>
<hr>
  
## Part 6: Designing your own elevated world

The scenario we'll be working with in this post is just this:

*A customer comes to your site and wants to view information about the products they have purchased.*

In this example, we'll assume that you have a API for a key/value store (such as Redis or a NoSql database), and all the information
you need is stored there.

So the code we need will look something like this:

```text
Open API connection
Get product ids purchased by customer id using the API
For each product id:
    get the product info for that id using the API
Close API connection
Return the list of product infos
```
  
How hard can that be?  

Well, it turns out to be surprisingly tricky! Luckily, we can find a way to make it easier using the concepts in this series.

<a id="apidomain"></a>
<hr>

## Defining the domain and a dummy ApiClient

First let's define the domain types:

* There will be a `CustomerId` and `ProductId` of course.
* For the product information, we'll just define a simple `ProductInfo` with a `ProductName` field.

Here are the types:

```fsharp
type CustId = CustId of string
type ProductId = ProductId of string
type ProductInfo = {ProductName: string; } 
```

For testing our api, let's create an `ApiClient` class with some `Get` and `Set` methods, backed by a static mutable dictionary.
This is based on similar APIs such as the Redis client.

Notes:

* The `Get` and `Set` both work with objects, so I've added a casting mechanism.
* In case of errors such as a failed cast, or a missing key, I'm using the `Result` type that we've been using throughout this series.
  Therefore, both `Get` and `Set` return `Result`s rather than plain objects.
* To make it more realistic, I've also added dummy methods for `Open`, `Close` and `Dispose`.
* All methods trace a log to the console.

```fsharp
type ApiClient() =
    // static storage
    static let mutable data = Map.empty<string,obj>

    /// Try casting a value
    /// Return Success of the value or Failure on failure
    member private this.TryCast<'a> key (value:obj) =
        match value with
        | :? 'a as a ->
            Result.Success a 
        | _  ->                 
            let typeName = typeof<'a>.Name
            Result.Failure [sprintf "Can't cast value at %s to %s" key typeName]

    /// Get a value
    member this.Get<'a> (id:obj) = 
        let key =  sprintf "%A" id
        printfn "[API] Get %s" key
        match Map.tryFind key data with
        | Some o -> 
            this.TryCast<'a> key o
        | None -> 
            Result.Failure [sprintf "Key %s not found" key]

    /// Set a value
    member this.Set (id:obj) (value:obj) = 
        let key =  sprintf "%A" id
        printfn "[API] Set %s" key
        if key = "bad" then  // for testing failure paths
            Result.Failure [sprintf "Bad Key %s " key]
        else
            data <- Map.add key value data 
            Result.Success ()
           
    member this.Open() =
        printfn "[API] Opening"

    member this.Close() =
        printfn "[API] Closing"

    interface System.IDisposable with
        member this.Dispose() =
            printfn "[API] Disposing"
```


Let's do some tests:

```fsharp
do
    use api = new ApiClient()
    api.Get "K1" |> printfn "[K1] %A"

    api.Set "K2" "hello" |> ignore
    api.Get<string> "K2" |> printfn "[K2] %A"

    api.Set "K3" "hello" |> ignore
    api.Get<int> "K3" |> printfn "[K3] %A"
```

And the results are:

```text
[API] Get "K1"
[K1] Failure ["Key "K1" not found"]
[API] Set "K2"
[API] Get "K2"
[K2] Success "hello"
[API] Set "K3"
[API] Get "K3"
[K3] Failure ["Can't cast value at "K3" to Int32"]
[API] Disposing
```
  
<a id="impl1"></a>
<hr>

## A first implementation attempt 

For our first attempt at implementing the scenario, let's start with the pseudo-code from above:

```fsharp
let getPurchaseInfo (custId:CustId) : Result<ProductInfo list> =

    // Open api connection       
    use api = new ApiClient()
    api.Open()

    // Get product ids purchased by customer id
    let productIdsResult = api.Get<ProductId list> custId

    let productInfosResult = ??

    // Close api connection
    api.Close()

    // Return the list of product infos
    productInfosResult
```

So far so good, but there is a bit of a problem already. 

The `getPurchaseInfo` function takes a `CustId` as input, but it can't just output a list of `ProductInfo`s, because there might be a failure.
That means that the return type needs to be `Result<ProductInfo list>`.

Ok, how do we create our `productInfosResult`?  

Well that should be easy. If the `productIdsResult` is Success, then loop through each id and get the info for each id.
If the `productIdsResult` is Failure, then just return that failure.

```fsharp
let getPurchaseInfo (custId:CustId) : Result<ProductInfo list> =

    // Open api connection       
    use api = new ApiClient()
    api.Open()

    // Get product ids purchased by customer id
    let productIdsResult = api.Get<ProductId list> custId

    let productInfosResult =
        match productIdsResult with
        | Success productIds -> 
            let productInfos = ResizeArray()  // Same as .NET List<T>
            for productId in productIds do
                let productInfo = api.Get<ProductInfo> productId
                productInfos.Add productInfo  // mutation! 
            Success productInfos
        | Failure err ->    
            Failure err 

    // Close api connection
    api.Close()
    
    // Return the list of product infos
    productInfosResult
```

Hmmm. It's looking a bit ugly. And I'm having to use a mutable data structure (`productInfos`) to accumulate each product info and then wrap it in `Success`.

And there's a worse problem The `productInfo` that I'm getting from `api.Get<ProductInfo>` is not a `ProductInfo` at all, but a `Result<ProductInfo>`,
so `productInfos` is not the right type at all!

Let's add code to test each `ProductInfo` result. If it's a success, then add it to the list of product infos, and if it's a failure, then return the failure.

```fsharp
let getPurchaseInfo (custId:CustId) : Result<ProductInfo list> =

    // Open api connection       
    use api = new ApiClient()
    api.Open()

    // Get product ids purchased by customer id
    let productIdsResult = api.Get<ProductId list> custId

    let productInfosResult =
        match productIdsResult with
        | Success productIds -> 
            let productInfos = ResizeArray()  // Same as .NET List<T>
            let mutable anyFailures = false
            for productId in productIds do
                let productInfoResult = api.Get<ProductInfo> productId
                match productInfoResult with
                | Success productInfo ->
                    productInfos.Add productInfo 
                | Failure err ->    
                    Failure err 
            Success productInfos
        | Failure err ->    
            Failure err 

    // Close api connection
    api.Close()

    // Return the list of product infos
    productInfosResult

```

Um, no. That won't work at all. The code above will not compile. We can't do an "early return" in the loop when a failure happens.

So what do we have so far? Some really ugly code that won't even compile.

There has to be a better way.

<a id="impl2"></a>
<hr>

## A second implementation attempt 

It would be great if we could hide all this unwrapping and testing of `Result`s.  And there is -- computation expressions to the rescue.

If we create a computation expression for `Result` we can write the code like this:

```fsharp
/// CustId -> Result<ProductInfo list>
let getPurchaseInfo (custId:CustId) : Result<ProductInfo list> =
   
    // Open api connection       
    use api = new ApiClient()
    api.Open()

    let productInfosResult = Result.result {

        // Get product ids purchased by customer id
        let! productIds = api.Get<ProductId list> custId

        let productInfos = ResizeArray()  // Same as .NET List<T>
        for productId in productIds do
            let! productInfo = api.Get<ProductInfo> productId
            productInfos.Add productInfo 
        return productInfos |> List.ofSeq
        }

    // Close api connection
    api.Close()

    // Return the list of product infos
    productInfosResult
```

In `let productInfosResult = Result.result { .. }` code we create a `result` computation expression that simplifies all the unwrapping (with `let!`) and wrapping (with `return`).

And so this implementation has no explicit `xxxResult` values anywhere. However, it still has to use a mutable collection class to do the accumulation,
because the `for productId in productIds do` is not actually a real `for` loop, and we can't replace it with `List.map`, say.

### The `result` computation expression. 

Which brings us onto the implementation of the `result` computation expression.  In the previous posts, `ResultBuilder` only had two methods, `Return` and `Bind`,
but in order to get the `for..in..do` functionality, we have to implement a lot of other methods too, and it ends up being a bit more complicated.

```fsharp
module Result = 

    let bind f xResult = ...
    
    type ResultBuilder() =
        member this.Return x = retn x
        member this.ReturnFrom(m: Result<'T>) = m
        member this.Bind(x,f) = bind f x

        member this.Zero() = Failure []
        member this.Combine (x,f) = bind f x
        member this.Delay(f: unit -> _) = f
        member this.Run(f) = f()

        member this.TryFinally(m, compensation) =
            try this.ReturnFrom(m)
            finally compensation()

        member this.Using(res:#System.IDisposable, body) =
            this.TryFinally(body res, fun () -> 
            match res with 
            | null -> () 
            | disp -> disp.Dispose())

        member this.While(guard, f) =
            if not (guard()) then 
                this.Zero() 
            else
                this.Bind(f(), fun _ -> this.While(guard, f))

        member this.For(sequence:seq<_>, body) =
            this.Using(sequence.GetEnumerator(), fun enum -> 
                this.While(enum.MoveNext, this.Delay(fun () -> 
                    body enum.Current)))

    let result = new ResultBuilder()
```

I have a series about the [internals of computation expressions](/series/computation-expressions.html),
so I don't want to explain all that code here. Instead, for the rest of the post
we'll work on refactoring `getPurchaseInfo`, and by the end of it we'll see that we don't need the `result` computation expression at all.

<a id="impl3"></a>
<hr>

## Refactoring the function

The problem with the `getPurchaseInfo` function as it stands is that it mixes concerns: it both creates the `ApiClient` and does some work with it.

There a number of problems with this approach:

* If we want to do different work with the API, we have to repeat the open/close part of this code. 
  And it's possible that one of the implementations might open the API but forget to close it.
* It's not testable with a mock API client.

We can solve both of these problems by separating the creation of an `ApiClient` from its use by parameterizing the action, like this.

```fsharp
let executeApiAction apiAction  =
   
    // Open api connection       
    use api = new ApiClient()
    api.Open()

    // do something with it
    let result = apiAction api

    // Close api connection
    api.Close()

    // return result
    result
```

The action function that is passed in would look like this, with a parameter for the `ApiClient` as well as for the `CustId`:

```fsharp
/// CustId -> ApiClient -> Result<ProductInfo list>
let getPurchaseInfo (custId:CustId) (api:ApiClient) =
   
    let productInfosResult = Result.result {
        let! productIds = api.Get<ProductId list> custId

        let productInfos = ResizeArray()  // Same as .NET List<T>
        for productId in productIds do
            let! productInfo = api.Get<ProductInfo> productId
            productInfos.Add productInfo 
        return productInfos |> List.ofSeq
        }

    // return result
    productInfosResult
```

Note that `getPurchaseInfo` has *two* parameters, but `executeApiAction` expects a function with only one.

No problem! Just use partial application to bake in the first parameter:

```fsharp
let action = getPurchaseInfo (CustId "C1")  // partially apply
executeApiAction action 
```

That's why the `ApiClient` is the *second* parameter in the parameter list -- so that we can do partial application.

### More refactoring

We might need to get the product ids for some other purpose, and also the productInfo, so let's refactor those out into separate functions too:

```fsharp
/// CustId -> ApiClient -> Result<ProductId list>
let getPurchaseIds (custId:CustId) (api:ApiClient) =
    api.Get<ProductId list> custId

/// ProductId -> ApiClient -> Result<ProductInfo>
let getProductInfo (productId:ProductId) (api:ApiClient) =
    api.Get<ProductInfo> productId

/// CustId -> ApiClient -> Result<ProductInfo list>
let getPurchaseInfo (custId:CustId) (api:ApiClient) =
   
    let result = Result.result {
        let! productIds = getPurchaseIds custId api 

        let productInfos = ResizeArray()  
        for productId in productIds do
            let! productInfo = getProductInfo productId api
            productInfos.Add productInfo 
        return productInfos |> List.ofSeq
        }

    // return result
    result
```

Now, we have these nice core functions `getPurchaseIds` and `getProductInfo`, but I'm annoyed that I have to write messy code to glue them together in `getPurchaseInfo`.

Ideally, what I'd like to do is pipe the output of `getPurchaseIds` into `getProductInfo` like this:

```fsharp
let getPurchaseInfo (custId:CustId) =
    custId 
    |> getPurchaseIds 
    |> List.map getProductInfo
```

Or as a diagram:

![](/assets/img/vgfp_api_pipe.png)

But I can't, and there are two reasons why:

* First, `getProductInfo` has *two* parameters. Not just a `ProductId` but also the `ApiClient`.
* Second, even if `ApiClient` wasn't there, the input of `getProductInfo` is a simple `ProductId` but the output of `getPurchaseIds` is a `Result`.

Wouldn't it be great if we could solve both of these problems!

<a id="apiAction"></a>
<hr>

## Introducing our own elevated world

Let's address the first problem. How can we compose functions when the extra `ApiClient` parameter keeps getting in the way?

This is what a typical API calling function looks like:

![](/assets/img/vgfp_api_action1.png)

If we look at the type signature we see this, a function with two parameters:

![](/assets/img/vgfp_api_action2.png)

But *another* way to interpret this function is as a function with *one* parameter that returns another function. The returned function has an `ApiClient` parameter
and returns the final ouput.

![](/assets/img/vgfp_api_action3.png)

You might think of it like this: I have an input right now, but I won't have an actual `ApiClient` until later,
so let me use the input to create a api-consuming function that can I glue together in various ways right now, without needing a `ApiClient` at all.

Let's give this api-consuming function a name. Let's call it `ApiAction`.

![](/assets/img/vgfp_api_action4.png)

In fact, let's do more than that -- let's make it a type!

```fsharp
type ApiAction<'a> = (ApiClient -> 'a)
```

Unfortunately, as it stands, this is just a type alias for a function, not a separate type.
We need to wrap it in a [single case union](/posts/designing-with-types-single-case-dus/) to make it a distinct type.

```fsharp
type ApiAction<'a> = ApiAction of (ApiClient -> 'a)
```

### Rewriting to use ApiAction

Now that we have a real type to use, we can rewrite our core domain functions to use it. 

First `getPurchaseIds`:

```fsharp
// CustId -> ApiAction<Result<ProductId list>>
let getPurchaseIds (custId:CustId) =
       
    // create the api-consuming function
    let action (api:ApiClient) = 
        api.Get<ProductId list> custId

    // wrap it in the single case
    ApiAction action
```

The signature is now `CustId -> ApiAction<Result<ProductId list>>`, which you can interpret as meaning: "give me a CustId and I will give a you a ApiAction that, when
given an api, will make a list of ProductIds". 

Similarly, `getProductInfo` can be rewritten to return an `ApiAction`:

```fsharp
// ProductId -> ApiAction<Result<ProductInfo>>
let getProductInfo (productId:ProductId) =

    // create the api-consuming function
    let action (api:ApiClient) = 
        api.Get<ProductInfo> productId

    // wrap it in the single case
    ApiAction action
```

Notice those signatures:

* `CustId -> ApiAction<Result<ProductId list>>`
* `ProductId -> ApiAction<Result<ProductInfo>>`

This is starting to look awfully familiar. Didn't we see something just like this in the previous post, with `Async<Result<_>>`?

### ApiAction as an elevated world

If we draw diagrams of the various types involved in these two functions, we can clearly see that `ApiAction` is an elevated world, just like `List` and `Result`.
And that means that we should be able to use the *same* techniques as we have used before: `map`, `bind`, `traverse`, etc.

Here's `getPurchaseIds` as a stack diagram. The input is a `CustId` and the output is an `ApiAction<Result<List<ProductId>>>`:

![](/assets/img/vgfp_api_getpurchaseids.png)

and with `getProductInfo` the input is a `ProductId` and the output is an `ApiAction<Result<ProductInfo>>`:

![](/assets/img/vgfp_api_getproductinfo.png)

The combined function that we want, `getPurchaseInfo`, should look like this:

![](/assets/img/vgfp_api_getpurchaseinfo.png)

And now the problem in composing the two functions is very clear: the output of `getPurchaseIds` can not be used as the input for `getProductInfo`:

![](/assets/img/vgfp_api_noncompose.png)

But I think that you can see that we have some hope! There should be some way of manipulating these layers so that they *do* match up, and then we can compose them easily.

So that's what we will work on next.

### Introducting ApiActionResult

In the last post we merged `Async` and `Result` into the compound type `AsyncResult`.  We can do the same here, and create the type `ApiActionResult`.

When we make this change, our two functions become slightly simpler:

![](/assets/img/vgfp_api_apiactionresult_functions.png)

Enough diagrams -- let's write some code now. 

First, we need to define `map`, `apply`, `return` and `bind` for `ApiAction`:

```fsharp
module ApiAction = 

    /// Evaluate the action with a given api
    /// ApiClient -> ApiAction<'a> -> 'a
    let run api (ApiAction action) = 
        let resultOfAction = action api
        resultOfAction

    /// ('a -> 'b) -> ApiAction<'a> -> ApiAction<'b>
    let map f action = 
        let newAction api =
            let x = run api action 
            f x
        ApiAction newAction

    /// 'a -> ApiAction<'a>
    let retn x = 
        let newAction api =
            x
        ApiAction newAction

    /// ApiAction<('a -> 'b)> -> ApiAction<'a> -> ApiAction<'b>
    let apply fAction xAction = 
        let newAction api =
            let f = run api fAction 
            let x = run api xAction 
            f x
        ApiAction newAction

    /// ('a -> ApiAction<'b>) -> ApiAction<'a> -> ApiAction<'b>
    let bind f xAction = 
        let newAction api =
            let x = run api xAction 
            run api (f x)
        ApiAction newAction

    /// Create an ApiClient and run the action on it
    /// ApiAction<'a> -> 'a
    let execute action =
        use api = new ApiClient()
        api.Open()
        let result = run api action
        api.Close()
        result
```

Note that all the functions use a helper function called `run` which unwraps an `ApiAction` to get the function inside,
and applies this to the `api` that is also passed in. The result is the value wrapped in the `ApiAction`.

For example, if we had an `ApiAction<int>` then `run api myAction` would result in an `int`.

And at the bottom, there is a `execute` function that creates an `ApiClient`, opens the connection, runs the action, and then closes the connection.

And with the core functions for `ApiAction` defined, we can go ahead and define the functions for the compound type `ApiActionResult`,
just as we did for `AsyncResult` in the [previous post](/posts/elevated-world-5/#asyncresult):

```fsharp
module ApiActionResult = 

    let map f  = 
        ApiAction.map (Result.map f)

    let retn x = 
        ApiAction.retn (Result.retn x)

    let apply fActionResult xActionResult = 
        let newAction api =
            let fResult = ApiAction.run api fActionResult 
            let xResult = ApiAction.run api xActionResult 
            Result.apply fResult xResult 
        ApiAction newAction

    let bind f xActionResult = 
        let newAction api =
            let xResult = ApiAction.run api xActionResult 
            // create a new action based on what xResult is
            let yAction = 
                match xResult with
                | Success x -> 
                    // Success? Run the function
                    f x
                | Failure err -> 
                    // Failure? wrap the error in an ApiAction
                    (Failure err) |> ApiAction.retn
            ApiAction.run api yAction  
        ApiAction newAction
```

## Working out the transforms

Now that we have all the tools in place, we must decide on what transforms to use to change the shape of `getProductInfo` so that the input matches up.

Should we choose `map`, or `bind`, or `traverse`?

Let's play around with the stacks visually and see what happens for each kind of transform.

Before we get started, let's be explicit about what we are trying to achieve:

* We have two functions `getPurchaseIds` and `getProductInfo` that we want to combine into a single function `getPurchaseInfo`.
* We have to manipulate the *left* side (the input) of `getProductInfo` so that it matches the output of `getPurchaseIds`.
* We have to manipulate the *right* side (the output) of `getProductInfo` so that it matches the output of our ideal `getPurchaseInfo`.

![](/assets/img/vgfp_api_wanted.png)

### Map

As a reminder, `map` adds a new stack on both sides. So if we start with a generic world-crossing function like this:

![](/assets/img/vgfp_api_generic.png)

Then, after `List.map` say, we will have a new `List` stack on each site.

![](/assets/img/vgfp_api_map_generic.png)

Here's our `getProductInfo` before transformation:

![](/assets/img/vgfp_api_getproductinfo2.png)

And here is what it would look like after using `List.map`

![](/assets/img/vgfp_api_map_getproductinfo.png)

This might seem promising -- we have a `List` of `ProductId` as input now, and if we can stack a `ApiActionResult` on top we would match the output of `getPurchaseId`.

But the output is all wrong. We want the `ApiActionResult` to stay on the top. That is, we don't want a `List` of `ApiActionResult` but a `ApiActionResult` of `List`.

### Bind

Ok, what about `bind`?

If you recall, `bind` turns a "diagonal" function into a horizontal function by adding a new stack on the *left* sides. So for example,
whatever the top elevated world is on the right, that will be added to the left.

![](/assets/img/vgfp_api_generic.png)

![](/assets/img/vgfp_api_bind_generic.png)

And here is what our `getProductInfo` would look like after using `ApiActionResult.bind`

![](/assets/img/vgfp_api_bind_getproductinfo.png)

This is no good to us. We need to have a `List` of `ProductId` as input.

### Traverse

Finally, let's try `traverse`.

`traverse` turns a diagonal function of values into diagonal function with lists wrapping the values. That is, `List` is added as the top stack
on the left hand side, and the second-from-top stack on the right hand side.

![](/assets/img/vgfp_api_generic.png)

![](/assets/img/vgfp_api_traverse_generic.png)

if we try that out on `getProductInfo` we get something very promising. 

![](/assets/img/vgfp_api_traverse_getproductinfo.png)

The input is a list as needed. And the output is perfect. We wanted a `ApiAction<Result<List<ProductInfo>>>` and we now have it.

So all we need to do now is add an `ApiActionResult` to the left side. 

Well, we just saw this! It's `bind`.  So if we do that as well, we are finished.

![](/assets/img/vgfp_api_complete_getproductinfo.png)

And here it is expressed as code:

```fsharp
let getPurchaseInfo =
    let getProductInfo1 = traverse getProductInfo
    let getProductInfo2 = ApiActionResult.bind getProductInfo1 
    getPurchaseIds >> getProductInfo2
```

Or to make it a bit less ugly:

```fsharp
let getPurchaseInfo =
    let getProductInfoLifted =
        getProductInfo
        |> traverse 
        |> ApiActionResult.bind 
    getPurchaseIds >> getProductInfoLifted
```

Let's compare that with the earlier version of `getPurchaseInfo`:

```fsharp
let getPurchaseInfo (custId:CustId) (api:ApiClient) =
   
    let result = Result.result {
        let! productIds = getPurchaseIds custId api 

        let productInfos = ResizeArray()  
        for productId in productIds do
            let! productInfo = getProductInfo productId api
            productInfos.Add productInfo 
        return productInfos |> List.ofSeq
        }

    // return result
    result
```

Let's compare the two versions in a table:

<table class="table table-condensed table-striped">
<tr>
<th>Earlier verson</th>
<th>Latest function</th>
</tr>
<tr>
<td>Composite function is non-trivial and needs special code to glue the two smaller functions together</td>
<td>Composite function is just piping and composition</td>
</tr>
<tr>
<td>Uses the "result" computation expression </td>
<td>No special syntax needed</td>
</tr>
<tr>
<td>Has special code to loop through the results </td>
<td>Uses "traverse"</td>
</tr>
<tr>
<td>Uses a intermediate (and mutable) List object to accumulate the list of product infos </td>
<td>No intermediate values needed. Just a data pipeline. </td>
</tr>
</table>


### Implementing traverse

The code above uses `traverse`, but we haven't implemented it yet.
As I noted earlier, it can be implemented mechanically, following a template. 

Here it is:

```fsharp
let traverse f list =
    // define the applicative functions
    let (<*>) = ApiActionResult.apply
    let retn = ApiActionResult.retn

    // define a "cons" function
    let cons head tail = head :: tail

    // right fold over the list
    let initState = retn []
    let folder head tail = 
        retn cons <*> f head <*> tail

    List.foldBack folder list initState 
```

### Testing the implementation

Let's test it!

First we need a helper function to show results:

```fsharp
let showResult result =
    match result with
    | Success (productInfoList) -> 
        printfn "SUCCESS: %A" productInfoList
    | Failure errs -> 
        printfn "FAILURE: %A" errs
```

Next, we need to load the API with some test data:

```fsharp
let setupTestData (api:ApiClient) =
    //setup purchases
    api.Set (CustId "C1") [ProductId "P1"; ProductId "P2"] |> ignore
    api.Set (CustId "C2") [ProductId "PX"; ProductId "P2"] |> ignore

    //setup product info
    api.Set (ProductId "P1") {ProductName="P1-Name"} |> ignore
    api.Set (ProductId "P2") {ProductName="P2-Name"} |> ignore
    // P3 missing

// setupTestData is an api-consuming function
// so it can be put in an ApiAction 
// and then that apiAction can be executed
let setupAction = ApiAction setupTestData
ApiAction.execute setupAction 
```

* Customer C1 has purchased two products: P1 and P2.
* Customer C2 has purchased two products: PX and P2.
* Products P1 and P2 have some info.
* Product PX does *not* have any info.

Let's see how this works out for different customer ids. 

We'll start with Customer C1. For this customer we expect both product infos to be returned:

```fsharp
CustId "C1"
|> getPurchaseInfo
|> ApiAction.execute
|> showResult
```

And here are the results:

```text
[API] Opening
[API] Get CustId "C1"
[API] Get ProductId "P1"
[API] Get ProductId "P2"
[API] Closing
[API] Disposing
SUCCESS: [{ProductName = "P1-Name";}; {ProductName = "P2-Name";}]
```

What happens if we use a missing customer, such as CX?

```fsharp
CustId "CX"
|> getPurchaseInfo
|> ApiAction.execute
|> showResult
```

As expected, we get a nice "key not found" failure, and the rest of the operations are skipped as soon as the key is not found.

```text
[API] Opening
[API] Get CustId "CX"
[API] Closing
[API] Disposing
FAILURE: ["Key CustId "CX" not found"]
```

What about if one of the purchased products has no info? For example, customer C2 purchased PX and P2, but there is no info for PX.

```fsharp
CustId "C2"
|> getPurchaseInfo
|> ApiAction.execute
|> showResult
```

The overall result is a failure. Any bad product causes the whole operation to fail.

```text
[API] Opening
[API] Get CustId "C2"
[API] Get ProductId "PX"
[API] Get ProductId "P2"
[API] Closing
[API] Disposing
FAILURE: ["Key ProductId "PX" not found"]
```

But note that the data for product P2 is fetched even though product PX failed. Why? Because we are using the applicative version of `traverse`,
so every element of the list is fetched "in parallel".  

If we wanted to only fetch P2 once we knew that PX existed, then we should be using monadic style instead. We already seen how to write a monadic version of `traverse`,
so I leave that as an exercise for you!

<a id="filtering"></a>
<hr>
  
## Filtering out failures

In the implementation above, the `getPurchaseInfo` function failed if *any* product failed to be found. Harsh!

A real application would probably be more forgiving. Probably what should happen is that the failed products are logged, but all the successes are accumulated
and returned.

How could we do this?

The answer is simple -- we just need to modify the `traverse` function to skip failures.

First, we need to create a new helper function for `ApiActionResult`. It will allow us to pass in two functions, one for the success case
and one for the error case:

```fsharp
module ApiActionResult = 

    let map = ...
    let retn =  ...
    let apply = ...
    let bind = ...

    let either onSuccess onFailure xActionResult = 
        let newAction api =
            let xResult = ApiAction.run api xActionResult 
            let yAction = 
                match xResult with
                | Result.Success x -> onSuccess x 
                | Result.Failure err -> onFailure err
            ApiAction.run api yAction  
        ApiAction newAction
```

This helper function helps us match both cases inside a `ApiAction` without doing complicated unwrapping. We will need this for our `traverse` that skips failures.

By the way, note that `ApiActionResult.bind` can be defined in terms of `either`:

```fsharp
let bind f = 
    either 
        // Success? Run the function
        (fun x -> f x)
        // Failure? wrap the error in an ApiAction
        (fun err -> (Failure err) |> ApiAction.retn)
```

Now we can define our "traverse with logging of failures" function:

```fsharp
let traverseWithLog log f list =
    // define the applicative functions
    let (<*>) = ApiActionResult.apply
    let retn = ApiActionResult.retn

    // define a "cons" function
    let cons head tail = head :: tail

    // right fold over the list
    let initState = retn []
    let folder head tail = 
        (f head) 
        |> ApiActionResult.either 
            (fun h -> retn cons <*> retn h <*> tail)
            (fun errs -> log errs; tail)
    List.foldBack folder list initState 
```

The only difference between this and the previous implementation is this bit:

```fsharp
let folder head tail = 
    (f head) 
    |> ApiActionResult.either 
        (fun h -> retn cons <*> retn h <*> tail)
        (fun errs -> log errs; tail)
```

This says that:

* If the new first element (`f head`) is a success, lift the inner value (`retn h`) and `cons` it with the tail to build a new list.
* But if the new first element is a failure, then log the inner errors (`errs`) with the passed in logging function (`log`)
  and just reuse the current tail.
  In this way, failed elements are not added to the list, but neither do they cause the whole function to fail.

Let's create a new function `getPurchasesInfoWithLog` and try it with customer C2 and the missing product PX:

```fsharp
let getPurchasesInfoWithLog =
    let log errs = printfn "SKIPPED %A" errs 
    let getProductInfoLifted =
        getProductInfo 
        |> traverseWithLog log 
        |> ApiActionResult.bind 
    getPurchaseIds >> getProductInfoLifted

CustId "C2"
|> getPurchasesInfoWithLog
|> ApiAction.execute
|> showResult
```

The result is a Success now, but only one `ProductInfo`, for P2, is returned. The log shows that PX was skipped.

```text
[API] Opening
[API] Get CustId "C2"
[API] Get ProductId "PX"
SKIPPED ["Key ProductId "PX" not found"]
[API] Get ProductId "P2"
[API] Closing
[API] Disposing
SUCCESS: [{ProductName = "P2-Name";}]
```

<a id="readermonad"></a>
<hr>
  
## The Reader monad

If you look closely at the `ApiResult` module, you will see that `map`, `bind`, and all the other functions do not use any information about the `api`
that is passed around.  We could have made it any type and those functions would still have worked.

So in the spirit of "parameterize all the things", why not make it a parameter?

That means that we could have defined `ApiAction` as follows:

```fsharp
type ApiAction<'anything,'a> = ApiAction of ('anything -> 'a)
```

But if it can be *anything*, why call it `ApiAction` any more? It could represent any set of things that depend on an object
(such as an `api`) being passed in to them.

We are not the first people to discover this! This type is commonly called the `Reader` type and is defined like this:  

```fsharp
type Reader<'environment,'a> = Reader of ('environment -> 'a)
```

The extra type `'environment` plays the same role that `ApiClient` did in our definition of `ApiAction`. There is some environment
that is passed around as an extra parameter to all your functions, just as a `api` instance was.

In fact, we can actually define `ApiAction` in terms of `Reader` very easily:

```fsharp
type ApiAction<'a> = Reader<ApiClient,'a>
```

The set of functions for `Reader` are exactly the same as for `ApiAction`. I have just taken the code and replaced `ApiAction` with `Reader` and
`api` with `environment`!  

```fsharp
module Reader = 

    /// Evaluate the action with a given environment
    /// 'env -> Reader<'env,'a> -> 'a
    let run environment (Reader action) = 
        let resultOfAction = action environment
        resultOfAction

    /// ('a -> 'b) -> Reader<'env,'a> -> Reader<'env,'b>
    let map f action = 
        let newAction environment =
            let x = run environment action 
            f x
        Reader newAction

    /// 'a -> Reader<'env,'a>
    let retn x = 
        let newAction environment =
            x
        Reader newAction

    /// Reader<'env,('a -> 'b)> -> Reader<'env,'a> -> Reader<'env,'b>
    let apply fAction xAction = 
        let newAction environment =
            let f = run environment fAction 
            let x = run environment xAction 
            f x
        Reader newAction

    /// ('a -> Reader<'env,'b>) -> Reader<'env,'a> -> Reader<'env,'b>
    let bind f xAction = 
        let newAction environment =
            let x = run environment xAction 
            run environment (f x)
        Reader newAction
```

The type signatures are a bit harder to read now though!

The `Reader` type, plus `bind` and `return`, plus the fact that `bind` and `return` implement the monad laws, means that `Reader` is typically called "the Reader monad" .

I'm not going to delve into the Reader monad here, but I hope that you can see how it is actually a useful thing and not some bizarre ivory tower concept.

### The Reader monad vs. an explicit type

Now if you like, you could replace all the `ApiAction` code above with `Reader` code, and it would work just the same. But *should* you?

Personally, I think that while understanding the concept behind the Reader monad is important and useful, I prefer the actual implementation of `ApiAction`
as I defined it originally, an explicit type rather than an alias for `Reader<ApiClient,'a>`.  

Why? Well, F# doesn't have typeclasses, F# doesn't have partial application of type constructors, F# doesn't have "newtype". 
Basically, F# isn't Haskell! I don't think that idioms that work well in Haskell should be carried over to F# directly when the language does not offer support for it. 

If you understand the concepts, you can implement all the necessary transformations in a few lines of code.  Yes, it's a little extra work, but the
upside is less abstraction and fewer dependencies.

I would make an exception, perhaps, if your team were all Haskell experts, and the Reader monad was familiar to everyone. But for teams of different abilities, I would err on being too concrete
rather than too abstract.

## Summary

In this post, we worked through another practical example, created our own elevated world which made things *much* easier, and in
the process, accidentally re-invented the reader monad.

If you liked this, you can see a similar practical example, this time for the State monad, in my series on ["Dr Frankenfunctor and the Monadster"](/posts/monadster/).

The [next and final post](/posts/elevated-world-7/) has a quick summary of the series, and some further reading.
