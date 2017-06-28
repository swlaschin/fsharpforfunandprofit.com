---
layout: post
title: "Introducing Folds"
description: "Threading state through a recursive data structure"
seriesId: "Recursive types and folds"
seriesOrder: 3
categories: [Folds, Patterns]
---

This post is the third in a series.

In the [first post](/posts/recursive-types-and-folds/), I introduced "catamorphisms", a way of creating functions for recursive types,
and in the [second post](/posts/recursive-types-and-folds-1b/), we created a few catamorphism implementations.

But at the end of the previous post, I noted that all the catamorphism implementations so far have had a potentially serious flaw.

In this post, we'll look at the flaw and how to work around it, and in the process look at folds, tail-recursion and the difference between "left fold" and "right fold".

## Series contents

Here's the contents of this series:

* **Part 1: Introduction to recursive types and catamorphisms**
  * [A simple recursive type](/posts/recursive-types-and-folds/#basic-recursive-type)
  * [Parameterize all the things](/posts/recursive-types-and-folds/#parameterize)
  * [Introducing catamorphisms](/posts/recursive-types-and-folds/#catamorphisms)
  * [Benefits of catamorphisms](/posts/recursive-types-and-folds/#benefits)
  * [Rules for creating a catamorphism](/posts/recursive-types-and-folds/#rules)
* **Part 2: Catamorphism examples**  
  * [Catamorphism example: File system domain](/posts/recursive-types-and-folds-1b/#file-system)
  * [Catamorphism example: Product domain](/posts/recursive-types-and-folds-1b/#product)
* **Part 3: Introducing folds**    
  * [A flaw in our catamorphism implementation](/posts/recursive-types-and-folds-2/#flaw)
  * [Introducing `fold`](/posts/recursive-types-and-folds-2/#fold)
  * [Problems with fold](/posts/recursive-types-and-folds-2/#problems)
  * [Using functions as accumulators](/posts/recursive-types-and-folds-2/#functions)
  * [Introducing `foldback`](/posts/recursive-types-and-folds-2/#foldback)
  * [Rules for creating a fold](/posts/recursive-types-and-folds-2/#rules)
* **Part 4: Understanding folds**      
  * [Iteration vs. recursion](/posts/recursive-types-and-folds-2b/#iteration)
  * [Fold example: File system domain](/posts/recursive-types-and-folds-2b/#file-system)  
  * [Common questions about "fold"](/posts/recursive-types-and-folds-2b/#questions)
* **Part 5: Generic recursive types**  
  * [LinkedList: A generic recursive type](/posts/recursive-types-and-folds-3/#linkedlist)
  * [Making the Gift domain generic](/posts/recursive-types-and-folds-3/#revisiting-gift)
  * [Defining a generic Container type](/posts/recursive-types-and-folds-3/#container)
  * [A third way to implement the gift domain](/posts/recursive-types-and-folds-3/#another-gift)
  * [Abstract or concrete? Comparing the three designs](/posts/recursive-types-and-folds-3/#compare)
* **Part 6: Trees in the real world**  
  * [Defining a generic Tree type](/posts/recursive-types-and-folds-3b/#tree)
  * [The Tree type in the real world](/posts/recursive-types-and-folds-3b/#reuse)
  * [Mapping the Tree type](/posts/recursive-types-and-folds-3b/#map)
  * [Example: Creating a directory listing](/posts/recursive-types-and-folds-3b/#listing)
  * [Example: A parallel grep](/posts/recursive-types-and-folds-3b/#grep)
  * [Example: Storing the file system in a database](/posts/recursive-types-and-folds-3b/#database)
  * [Example: Serializing a Tree to JSON](/posts/recursive-types-and-folds-3b/#tojson)
  * [Example: Deserializing a Tree from JSON](/posts/recursive-types-and-folds-3b/#fromjson)
  * [Example: Deserializing a Tree from JSON - with error handling](/posts/recursive-types-and-folds-3b/#json-with-error-handling)

<a id="flaw"></a>
<hr>

## A flaw in our catamorphism implementation

Before we look at the flaw, let's first review the recursive type `Gift` and the associated catamorphism `cataGift` that we created for it.

Here's the domain:

```fsharp
type Book = {title: string; price: decimal}

type ChocolateType = Dark | Milk | SeventyPercent
type Chocolate = {chocType: ChocolateType ; price: decimal}

type WrappingPaperStyle = 
    | HappyBirthday
    | HappyHolidays
    | SolidColor

type Gift =
    | Book of Book
    | Chocolate of Chocolate 
    | Wrapped of Gift * WrappingPaperStyle
    | Boxed of Gift 
    | WithACard of Gift * message:string
```

Here are some example values that we'll be using in this post:

```fsharp
// A Book
let wolfHall = {title="Wolf Hall"; price=20m}
// A Chocolate
let yummyChoc = {chocType=SeventyPercent; price=5m}
// A Gift
let birthdayPresent = WithACard (Wrapped (Book wolfHall, HappyBirthday), "Happy Birthday")
// A Gift
let christmasPresent = Wrapped (Boxed (Chocolate yummyChoc), HappyHolidays)
```

Here's the catamorphism:

```fsharp
let rec cataGift fBook fChocolate fWrapped fBox fCard gift :'r =
    let recurse = cataGift fBook fChocolate fWrapped fBox fCard
    match gift with 
    | Book book -> 
        fBook book
    | Chocolate choc -> 
        fChocolate choc
    | Wrapped (gift,style) -> 
        fWrapped (recurse gift,style)
    | Boxed gift -> 
        fBox (recurse gift)
    | WithACard (gift,message) -> 
        fCard (recurse gift,message) 
```

and here is a `totalCostUsingCata` function built using `cataGift`:

```fsharp
let totalCostUsingCata gift =
    let fBook (book:Book) = 
        book.price
    let fChocolate (choc:Chocolate) = 
        choc.price
    let fWrapped  (innerCost,style) = 
        innerCost + 0.5m
    let fBox innerCost = 
        innerCost + 1.0m
    let fCard (innerCost,message) = 
        innerCost + 2.0m
    // call the catamorphism
    cataGift fBook fChocolate fWrapped fBox fCard gift
```

### What's the flaw?

So what is wrong with this implementation?  Let's stress test it and find out!

What we'll do is create a `Box` inside a `Box` inside a `Box` a very large number of times, and see what happens.

Here's a little helper function to create nested boxes:

```fsharp
let deeplyNestedBox depth =
    let rec loop depth boxSoFar =
        match depth with
        | 0 -> boxSoFar 
        | n -> loop (n-1) (Boxed boxSoFar)
    loop depth (Book wolfHall)
```

Let's try it to make sure it works:

```fsharp
deeplyNestedBox 5
// Boxed (Boxed (Boxed (Boxed (Boxed (Book {title = "Wolf Hall"; price = 20M})))))

deeplyNestedBox 10
//  Boxed(Boxed(Boxed(Boxed(Boxed
//   (Boxed(Boxed(Boxed(Boxed(Boxed(Book {title = "Wolf Hall";price = 20M}))))))))))
```

Now try running `totalCostUsingCata` with these deeply nested boxes:

```fsharp
deeplyNestedBox 10 |> totalCostUsingCata       // OK     30.0M
deeplyNestedBox 100 |> totalCostUsingCata      // OK    120.0M
deeplyNestedBox 1000 |> totalCostUsingCata     // OK   1020.0M
```

So far so good.

But if we use much larger numbers, we soon run into a stack overflow exception:

```fsharp
deeplyNestedBox 10000 |> totalCostUsingCata  // Stack overflow?
deeplyNestedBox 100000 |> totalCostUsingCata // Stack overflow?
```

The exact number which causes an error depends on the environment, available memory, and so on.
But it is a certainty that you will run into it when you start using largish numbers.

Why is this happening?

### The problem with deep recursion 

Recall that the definition of the cost for the Boxed case (`fBox`) was `innerCost + 1.0m`.
And what is the inner cost?  That's another box too, so we end up with a chain of calculations looking like this:

```fsharp
innerCost + 1.0m where innerCost = 
  innerCost2 + 1.0m where innerCost2 = 
    innerCost3 + 1.0m where innerCost3 = 
      innerCost4 + 1.0m where innerCost4 = 
        ...
        innerCost999 + 1.0m where innerCost999 = 
          innerCost1000 + 1.0m where innerCost1000 = 
            book.price
```

In other words, `innerCost1000` has to be calculated before `innerCost999` can be calculated,
and 999 other inner costs have to be calculated before the top level `innerCost` can be calculated.

Every level is waiting for its inner cost to be calculated before doing the calculation for that level.

All these unfinished calculations are stacked up waiting for the inner one to complete. And when you have too many? Boom! Stack overflow!

### The solution to stack overflows

The solution to this problem is simple. Rather than each level waiting for the inner cost to be calculated, each level calculates the cost so far, using an accumulator,
and passes that down to the next inner level. When we get to the bottom level, we have the final answer.

```fsharp
costSoFar = 1.0m; evaluate calcInnerCost with costSoFar: 
  costSoFar = costSoFar + 1.0m; evaluate calcInnerCost with costSoFar: 
    costSoFar = costSoFar + 1.0m; evaluate calcInnerCost with costSoFar: 
      costSoFar = costSoFar + 1.0m; evaluate calcInnerCost with costSoFar: 
        ...
        costSoFar = costSoFar + 1.0m; evaluate calcInnerCost with costSoFar: 
          costSoFar = costSoFar + 1.0m; evaluate calcInnerCost with costSoFar: 
            finalCost = costSoFar + book.price   // final answer
```

The big advantange of this approach is that all calculations at a particular level are *completely finished* before the next lowel level is called.
Which means that the level and its associated data can be safely discarded from the stack. Which means no stack overflow!

An implementation like this, where the higher levels can be safely discarded, is called *tail recursive*.

### Reimplementating the `totalCost` function with an accumulator

Let's rewrite the total cost function from scratch, using an accumulator called `costSoFar`:

```fsharp
let rec totalCostUsingAcc costSoFar gift =
    match gift with 
    | Book book -> 
        costSoFar + book.price  // final result
    | Chocolate choc -> 
        costSoFar + choc.price  // final result
    | Wrapped (innerGift,style) -> 
        let newCostSoFar = costSoFar + 0.5m
        totalCostUsingAcc newCostSoFar innerGift 
    | Boxed innerGift -> 
        let newCostSoFar = costSoFar + 1.0m
        totalCostUsingAcc newCostSoFar innerGift 
    | WithACard (innerGift,message) -> 
        let newCostSoFar = costSoFar + 2.0m
        totalCostUsingAcc newCostSoFar innerGift 
```

A few things to note:

* The new version of the function has an extra parameter (`costSoFar`). We will have to provide an initial value for this (such as zero) when we call it at the top level.
* The non-recursive cases (`Book` and `Chocolate`) are the end points. The take the cost so far and add it to their price, and then that is the final result.
* The recursive cases calculate a new `costSoFar` based on the parameter that is passed in. The new `costSoFar` is then passed down to the next lower level,
  just as in the example above.

Let's stress test this version:

```fsharp
deeplyNestedBox 1000 |> totalCostUsingAcc 0.0m     // OK    1020.0M
deeplyNestedBox 10000 |> totalCostUsingAcc 0.0m    // OK   10020.0M
deeplyNestedBox 100000 |> totalCostUsingAcc 0.0m   // OK  100020.0M
deeplyNestedBox 1000000 |> totalCostUsingAcc 0.0m  // OK 1000020.0M
```

Excellent. Up to one million nested levels without a hiccup.

<a id="fold"></a>

## Introducing "fold"

Now let's apply the same design principle to the catamorphism implementation. 

We'll create a new function `foldGift`.
We'll introduce an accumulator `acc` that we will thread through each level, and the non-recursive cases will return the final accumulator.

```fsharp
let rec foldGift fBook fChocolate fWrapped fBox fCard acc gift :'r =
    let recurse = foldGift fBook fChocolate fWrapped fBox fCard 
    match gift with 
    | Book book -> 
        let finalAcc = fBook acc book
        finalAcc     // final result
    | Chocolate choc -> 
        let finalAcc = fChocolate acc choc
        finalAcc     // final result
    | Wrapped (innerGift,style) -> 
        let newAcc = fWrapped acc style
        recurse newAcc innerGift 
    | Boxed innerGift -> 
        let newAcc = fBox acc 
        recurse newAcc innerGift 
    | WithACard (innerGift,message) -> 
        let newAcc = fCard acc message 
        recurse newAcc innerGift
```

If we look at the type signature, we can see that it is subtly different. The type of the accumulator `'a` is being used everywhere now.
The only time where the final return type is used is in the two non-recursive cases (`fBook` and `fChocolate`).

```fsharp
val foldGift :
  fBook:('a -> Book -> 'r) ->
  fChocolate:('a -> Chocolate -> 'r) ->
  fWrapped:('a -> WrappingPaperStyle -> 'a) ->
  fBox:('a -> 'a) ->
  fCard:('a -> string -> 'a) -> 
  // accumulator
  acc:'a -> 
  // input value
  gift:Gift -> 
  // return value
  'r
```

Let's look at this more closely, and compare the signatures of the original catamorphism from the last post with the signatures of the new `fold` function.

First of all, the non-recursive cases:

```fsharp
// original catamorphism
fBook:(Book -> 'r)
fChocolate:(Chocolate -> 'r)

// fold
fBook:('a -> Book -> 'r)
fChocolate:('a -> Chocolate -> 'r)
```

As you can see, with "fold", the non-recursive cases take an extra parameter (the accumulator) and return the `'r` type.

This is a very important point: *the type of the accumulator does not need to be the same as the return type.*
We will need to take advantage of this shortly.

What about the recursive cases? How did their signature change?

```fsharp
// original catamorphism
fWrapped:('r -> WrappingPaperStyle -> 'r) 
fBox:('r -> 'r) 

// fold
fWrapped:('a -> WrappingPaperStyle -> 'a)
fBox:('a -> 'a)
```

For the recursive cases, the structure is identical but all use of the `'r` type has been replaced with the `'a` type.
The recursive cases do not use the `'r` type at all.
  
### Reimplementating the `totalCost` function using fold

Once again, we can reimplement the total cost function, but this time using the `foldGift` function:

```fsharp
let totalCostUsingFold gift =  

    let fBook costSoFar (book:Book) = 
        costSoFar + book.price
    let fChocolate costSoFar (choc:Chocolate) = 
        costSoFar + choc.price
    let fWrapped costSoFar style = 
        costSoFar + 0.5m
    let fBox costSoFar = 
        costSoFar + 1.0m
    let fCard costSoFar message = 
        costSoFar + 2.0m

    // initial accumulator
    let initialAcc = 0m

    // call the fold
    foldGift fBook fChocolate fWrapped fBox fCard initialAcc gift 
```

And again, we can process very large numbers of nested boxes without a stack overflow:

```fsharp
deeplyNestedBox 100000 |> totalCostUsingFold  // no problem   100020.0M
deeplyNestedBox 1000000 |> totalCostUsingFold // no problem  1000020.0M
```

<a id="problems"></a>

## Problems with fold

So using fold solves all our problems, right? 

Unfortunately, no. 

Yes, there are no more stack overflows, but we have another problem now.

### Reimplementation of the `description` function

To see what the problem is, let's revisit the `description` function that we created in the first post.

The original one was not tail-recursive, so let's make it safer and reimplement it using `foldGift`.

```fsharp
let descriptionUsingFold gift =
    let fBook descriptionSoFar (book:Book) = 
        sprintf "'%s' %s" book.title descriptionSoFar

    let fChocolate descriptionSoFar (choc:Chocolate) = 
        sprintf "%A chocolate %s" choc.chocType descriptionSoFar

    let fWrapped descriptionSoFar style = 
        sprintf "%s wrapped in %A paper" descriptionSoFar style

    let fBox descriptionSoFar = 
        sprintf "%s in a box" descriptionSoFar 

    let fCard descriptionSoFar message = 
        sprintf "%s with a card saying '%s'" descriptionSoFar message

    // initial accumulator
    let initialAcc = ""

    // main call
    foldGift fBook fChocolate fWrapped fBox fCard initialAcc gift 
```

Let's see what the output is:

```fsharp
birthdayPresent |> descriptionUsingFold  
// "'Wolf Hall'  with a card saying 'Happy Birthday' wrapped in HappyBirthday paper"

christmasPresent |> descriptionUsingFold  
// "SeventyPercent chocolate  wrapped in HappyHolidays paper in a box"
```

These outputs are wrong! The order of the decorations has been mixed up.

It's supposed to be a wrapped book with a card, not a book and a card wrapped together.
And it's supposed to be chocolate in a box, then wrapped, not wrapped chocolate in a box!

```fsharp
// OUTPUT: "'Wolf Hall'  with a card saying 'Happy Birthday' wrapped in HappyBirthday paper"
// CORRECT "'Wolf Hall' wrapped in HappyBirthday paper with a card saying 'Happy Birthday'"

// OUTPUT: "SeventyPercent chocolate  wrapped in HappyHolidays paper in a box"
// CORRECT "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

What has gone wrong?

The answer is that the correct description for each layer depends on the description of the layer below. We can't "pre-calculate" the description for a layer
and pass it down to the next layer using a `descriptionSoFar` accumulator.

But now we have a dilemma: a layer depends on information from the layer below, but we want to avoid a stack overflow.

<a id="functions"></a>

## Using functions as accumulators

Remember that the accumulator type does not have to be the same as the return type. We can use anything as an accumulator, even a function!

So what we'll do is, rather than passing a `descriptionSoFar` as the accumulator, we'll pass a function (`descriptionGenerator` say)
that will build the appropriate description given the value of the next layer down.

Here's the implementation for the non-recursive cases:

```fsharp
let fBook descriptionGenerator (book:Book) = 
    descriptionGenerator (sprintf "'%s'" book.title)
//  ~~~~~~~~~~~~~~~~~~~~  <= a function as an accumulator!

let fChocolate descriptionGenerator (choc:Chocolate) = 
    descriptionGenerator (sprintf "%A chocolate" choc.chocType)
```

The implementation for recursive cases is a bit more complicated:

* We are given an accumulator (`descriptionGenerator`) as a parameter.
* We need to create a new accumulator (a new `descriptionGenerator`) to pass down to the next lower layer.
* The *input* to the description generator will be all the data accumulated from the lower layers.
  We manipulate that to make a new description and then call the `descriptionGenerator` passed in from the higher layer.

It's more complicated to talk about than to demonstrate, so here are implementations for two of the cases:

```fsharp
let fWrapped descriptionGenerator style = 
    let newDescriptionGenerator innerText =
        let newInnerText = sprintf "%s wrapped in %A paper" innerText style
        descriptionGenerator newInnerText 
    newDescriptionGenerator 

let fBox descriptionGenerator = 
    let newDescriptionGenerator innerText =
        let newInnerText = sprintf "%s in a box" innerText 
        descriptionGenerator newInnerText 
    newDescriptionGenerator 
```

We can simplify that code a little by using a lambda directly:

```fsharp
let fWrapped descriptionGenerator style = 
    fun innerText ->
        let newInnerText = sprintf "%s wrapped in %A paper" innerText style
        descriptionGenerator newInnerText 

let fBox descriptionGenerator = 
    fun innerText ->
        let newInnerText = sprintf "%s in a box" innerText 
        descriptionGenerator newInnerText 
```

We could continue to make it more compact using piping and other things, but I think that what we have here is a good balance between conciseness and obscurity.

Here is the entire function:

```fsharp
let descriptionUsingFoldWithGenerator gift =

    let fBook descriptionGenerator (book:Book) = 
        descriptionGenerator (sprintf "'%s'" book.title)

    let fChocolate descriptionGenerator (choc:Chocolate) = 
        descriptionGenerator (sprintf "%A chocolate" choc.chocType)

    let fWrapped descriptionGenerator style = 
        fun innerText ->
            let newInnerText = sprintf "%s wrapped in %A paper" innerText style
            descriptionGenerator newInnerText 

    let fBox descriptionGenerator = 
        fun innerText ->
            let newInnerText = sprintf "%s in a box" innerText 
            descriptionGenerator newInnerText 

    let fCard descriptionGenerator message = 
        fun innerText ->
            let newInnerText = sprintf "%s with a card saying '%s'" innerText message 
            descriptionGenerator newInnerText 

    // initial DescriptionGenerator
    let initialAcc = fun innerText -> innerText 

    // main call
    foldGift fBook fChocolate fWrapped fBox fCard initialAcc gift 
```

Again, I'm using overly descriptive intermediate values to make it clear what is going on.

If we try `descriptionUsingFoldWithGenerator` now, we get the correct answers again:

```fsharp
birthdayPresent |> descriptionUsingFoldWithGenerator  
// CORRECT "'Wolf Hall' wrapped in HappyBirthday paper with a card saying 'Happy Birthday'"

christmasPresent |> descriptionUsingFoldWithGenerator  
// CORRECT "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

<a id="foldback"></a>

## Introducing "foldback"

Now that we understand what to do, let's make a generic version that that handles the generator function logic for us.
This one we will call "foldback":

*By the way, I'm going to use term "generator" here. In other places, it is commonly referred to as a "continuation" function, often abbreviated to just "k".*

Here's the implementation:

```fsharp
let rec foldbackGift fBook fChocolate fWrapped fBox fCard generator gift :'r =
    let recurse = foldbackGift fBook fChocolate fWrapped fBox fCard 
    match gift with 
    | Book book -> 
        generator (fBook book)
    | Chocolate choc -> 
        generator (fChocolate choc)
    | Wrapped (innerGift,style) -> 
        let newGenerator innerVal =
            let newInnerVal = fWrapped innerVal style
            generator newInnerVal 
        recurse newGenerator innerGift 
    | Boxed innerGift -> 
        let newGenerator innerVal =
            let newInnerVal = fBox innerVal 
            generator newInnerVal 
        recurse newGenerator innerGift 
    | WithACard (innerGift,message) -> 
        let newGenerator innerVal =
            let newInnerVal = fCard innerVal message 
            generator newInnerVal 
        recurse newGenerator innerGift 
```

You can see that it is just like the `descriptionUsingFoldWithGenerator` implementation, except that we are using generic `newInnerVal` and `generator` values.

The type signatures are similar to the original catamorphism, except that every case works with `'a` only now.
The only time `'r` is used is in the generator function itself!

```fsharp
val foldbackGift :
  fBook:(Book -> 'a) ->
  fChocolate:(Chocolate -> 'a) ->
  fWrapped:('a -> WrappingPaperStyle -> 'a) ->
  fBox:('a -> 'a) ->
  fCard:('a -> string -> 'a) ->
  // accumulator
  generator:('a -> 'r) -> 
  // input value
  gift:Gift -> 
  // return value
  'r
```

*The `foldback` implementation above is written from scratch. If you want a fun exercise, see if you can write `foldback` in terms of `fold`.*

Let's rewrite the `description` function using `foldback`: 

```fsharp
let descriptionUsingFoldBack gift =
    let fBook (book:Book) = 
        sprintf "'%s'" book.title 
    let fChocolate (choc:Chocolate) = 
        sprintf "%A chocolate" choc.chocType
    let fWrapped innerText style = 
        sprintf "%s wrapped in %A paper" innerText style
    let fBox innerText = 
        sprintf "%s in a box" innerText 
    let fCard innerText message = 
        sprintf "%s with a card saying '%s'" innerText message 
    // initial DescriptionGenerator
    let initialAcc = fun innerText -> innerText 
    // main call
    foldbackGift fBook fChocolate fWrapped fBox fCard initialAcc gift 
```

And the results are still correct:

```fsharp
birthdayPresent |> descriptionUsingFoldBack
// CORRECT "'Wolf Hall' wrapped in HappyBirthday paper with a card saying 'Happy Birthday'"

christmasPresent |> descriptionUsingFoldBack
// CORRECT "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

### Comparing `foldback` to the original catamorphism

The implementation of `descriptionUsingFoldBack` is almost identical to the version in the last post that used the original catamorphism `cataGift`.

Here's the version using `cataGift`:

```fsharp
let descriptionUsingCata gift =
    let fBook (book:Book) = 
        sprintf "'%s'" book.title 
    let fChocolate (choc:Chocolate) = 
        sprintf "%A chocolate" choc.chocType
    let fWrapped (innerText,style) = 
        sprintf "%s wrapped in %A paper" innerText style
    let fBox innerText = 
        sprintf "%s in a box" innerText
    let fCard (innerText,message) = 
        sprintf "%s with a card saying '%s'" innerText message
    // call the catamorphism
    cataGift fBook fChocolate fWrapped fBox fCard gift
```

And here's the version using `foldbackGift`:

```fsharp
let descriptionUsingFoldBack gift =
    let fBook (book:Book) = 
        sprintf "'%s'" book.title 
    let fChocolate (choc:Chocolate) = 
        sprintf "%A chocolate" choc.chocType
    let fWrapped innerText style = 
        sprintf "%s wrapped in %A paper" innerText style
    let fBox innerText = 
        sprintf "%s in a box" innerText 
    let fCard innerText message = 
        sprintf "%s with a card saying '%s'" innerText message 
    // initial DescriptionGenerator
    let initialAcc = fun innerText -> innerText    // could be replaced with id
    // main call
    foldbackGift fBook fChocolate fWrapped fBox fCard initialAcc gift 
```

All the handler functions are basically identical. The only change is the addition of an initial generator function, which is just `id` in this case.

However, although the code looks the same in both cases, they differ in their recursion safety.  The `foldbackGift` version is still tail recursive, and can handle
very large nesting depths, unlike the `cataGift` version.  

But this implementation is not perfect either. The chain of nested functions can get very slow and generate a lot of garbage, and for this particular example, there is an even
faster way, which we'll look at in the next post.

### Changing the type signature of `foldback` to avoid a mixup 

In `foldGift` the signature for `fWrapped` is:

```fsharp
fWrapped:('a -> WrappingPaperStyle -> 'a) 
```

But in `foldbackGift` the signature for `fWrapped` is:

```fsharp
fWrapped:('a -> WrappingPaperStyle -> 'a) 
```

Can you spot the difference? No, me neither.

The two functions are very similar, yet work very differently. In the `foldGift` version, the first parameter is the accumulator from the *outer* levels,
while in `foldbackGift` version, the first parameter is the accumulator from the *inner* levels. Quite an important distinction!

It is therefore common to change the signature of the `foldBack` version so that the accumulator
always comes *last*, while in the normal `fold` function, the accumulator always comes *first*.

```fsharp
let rec foldbackGift fBook fChocolate fWrapped fBox fCard gift generator :'r =
//swapped =>                                              ~~~~~~~~~~~~~~ 

    let recurse = foldbackGiftWithAccLast fBook fChocolate fWrapped fBox fCard 

    match gift with 
    | Book book -> 
        generator (fBook book)
    | Chocolate choc -> 
        generator (fChocolate choc)

    | Wrapped (innerGift,style) -> 
        let newGenerator innerVal =
            let newInnerVal = fWrapped style innerVal 
//swapped =>                           ~~~~~~~~~~~~~~ 
            generator newInnerVal 
        recurse innerGift newGenerator  
//swapped =>    ~~~~~~~~~~~~~~~~~~~~~~ 

    | Boxed innerGift -> 
        let newGenerator innerVal =
            let newInnerVal = fBox innerVal 
            generator newInnerVal 
        recurse innerGift newGenerator  
//swapped =>    ~~~~~~~~~~~~~~~~~~~~~~ 

    | WithACard (innerGift,message) -> 
        let newGenerator innerVal =
            let newInnerVal = fCard message innerVal 
//swapped =>                        ~~~~~~~~~~~~~~~~ 
            generator newInnerVal 
        recurse innerGift newGenerator 
//swapped =>    ~~~~~~~~~~~~~~~~~~~~~~ 
```

This change shows up in the type signature. The `Gift` value comes before the accumulator now:

```fsharp
val foldbackGift :
  fBook:(Book -> 'a) ->
  fChocolate:(Chocolate -> 'a) ->
  fWrapped:(WrappingPaperStyle -> 'a -> 'a) ->
  fBox:('a -> 'a) ->
  fCard:(string -> 'a -> 'a) ->
  // input value
  gift:Gift -> 
  // accumulator
  generator:('a -> 'r) -> 
  // return value
  'r
```

and now we *can* tell the two versions apart easily.

```fsharp
// fold
fWrapped:('a -> WrappingPaperStyle -> 'a) 

// foldback
fWrapped:(WrappingPaperStyle -> 'a -> 'a)
```


<a id="rules"></a>

## Rules for creating a fold

To finish up this post, let's summarize the rules for creating a fold.

In the first post we saw that creating a catamorphism is a mechanical process that [follows rules](/posts/recursive-types-and-folds/#rules).
The same is true for creating a iterative top-down fold. The process is:

* Create a function parameter to handle each case in the structure.
* Add an additional parameter as an accumulator.
* For non-recursive cases, pass the function parameter the accumulator plus all the data associated with that case.
* For recursive cases, perform two steps:
  * First, pass the handler the accumulator plus all the data associated with that case (except the inner recursive data). The result is a new accumulator value.
  * Then, call the fold recursively on the nested value using the new accumulator value.

Note that each handler only "sees" (a) the data for that case, and (b) the accumulator passed to it from the outer level.
It does not have access to the results from the inner levels.
  
<hr>
    
## Summary 

We've seen in this post how to define a tail-recursive implementation of a catamorphism, called "fold" and the reverse version "foldback".

In the [next post](/posts/recursive-types-and-folds-2b/) we'll step back a bit and spend some time understanding what "fold" really means,
and at some guidelines for choosing between `fold`, `foldback` and `cata`.

We'll then see if we can apply these rules to another domain.

*The source code for this post is available at [this gist](https://gist.github.com/swlaschin/df4427d0043d7146e592).*
