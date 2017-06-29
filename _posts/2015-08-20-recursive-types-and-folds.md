---
layout: post
title: "Introduction to recursive types"
description: "Don't fear the catamorphism..."
seriesId: "Recursive types and folds"
seriesOrder: 1
categories: [Folds, Patterns]
---

In this series, we'll look at recursive types and how to use them, and on the way, we'll look at
catamorphisms, tail recursion, the difference between left and right folds, and more.

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

  
<a id="basic-recursive-type"></a>
<hr>

## A basic recursive type

Let's start with a simple example -- how to model a gift.  

As it happens, I'm a very lazy gift-giver.  I always give people a book or chocolates. I generally wrap them,
and sometimes, if I'm feeling particularly extravagant, I put them in a gift box and add a card too.

Let's see how I might model this in types:

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

You can see that three of the cases are "containers" that refer to another `Gift`. The `Wrapped` case has some paper and a inner gift, as does the `Boxed` case, as does the
`WithACard` case. The two other cases, `Book` and `Chocolate`, do not refer to a gift and can be considered "leaf" nodes or terminals.

The presence of a reference to an inner `Gift` in those three cases makes `Gift` a *recursive type*.
Note that, unlike functions, the `rec` keyword is *not* needed for defining recursive types.

Let's create some example values:

```fsharp
// a Book
let wolfHall = {title="Wolf Hall"; price=20m}

// a Chocolate
let yummyChoc = {chocType=SeventyPercent; price=5m}

// A Gift
let birthdayPresent = WithACard (Wrapped (Book wolfHall, HappyBirthday), "Happy Birthday")
//  WithACard (
//    Wrapped (
//      Book {title = "Wolf Hall"; price = 20M},
//      HappyBirthday),
//    "Happy Birthday")

// A Gift
let christmasPresent = Wrapped (Boxed (Chocolate yummyChoc), HappyHolidays)
//  Wrapped (
//    Boxed (
//      Chocolate {chocType = SeventyPercent; price = 5M}),
//    HappyHolidays)
```

Before we start working with these values, a word of advice...

### Guideline: Avoid infinitely recursive types

I suggest that, in F#, every recursive type should consist of a mix of recursive and non-recursive cases.
If there were no non-recursive elements, such as `Book`, all values of the type would have to be infinitely recursive. 

For example, in the `ImpossibleGift` type below, all the cases are recursive. To construct any one of the cases you need an inner gift, and that needs to be constructed too, and so on.

```fsharp
type ImpossibleGift =
    | Boxed of ImpossibleGift 
    | WithACard of ImpossibleGift * message:string
```

It is possible to create such types if you allow [laziness](https://wiki.haskell.org/Tying_the_Knot), mutation, or reflection.
But in general, in a non-lazy language like F#, it's a good idea to avoid such types.

### Working with recursive types

End of public service announcement -- let's get coding!

First, say that we want a description of the gift. The logic will be:

* For the two non-recursive cases, return a string describing that case.
* For the three recursive cases, return a string that describes the case, but also includes the description of the inner gift.
  This means that `description` function is going to refer to itself, and therefore it must be marked with the `rec` keyword.

Here's an example implementation:  

```fsharp
let rec description gift =
    match gift with 
    | Book book -> 
        sprintf "'%s'" book.title 
    | Chocolate choc -> 
        sprintf "%A chocolate" choc.chocType
    | Wrapped (innerGift,style) -> 
        sprintf "%s wrapped in %A paper" (description innerGift) style
    | Boxed innerGift -> 
        sprintf "%s in a box" (description innerGift) 
    | WithACard (innerGift,message) -> 
        sprintf "%s with a card saying '%s'" (description innerGift) message
```

Note the recursive calls like this one in the `Boxed` case:

```fsharp
    | Boxed innerGift -> 
        sprintf "%s in a box" (description innerGift) 
                               ~~~~~~~~~~~ <= recursive call
```

If we try this with our example values, let's see what we get:

```fsharp
birthdayPresent |> description  
// "'Wolf Hall' wrapped in HappyBirthday paper with a card saying 'Happy Birthday'"

christmasPresent |> description  
// "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

That looks pretty good to me. Things like `HappyHolidays` look a bit funny without spaces, but it's good enough to demonstrate the idea.

What about creating another function?  For example, what is the total cost of a gift?

For `totalCost`, the logic will be:

* Books and chocolate capture the price in the case-specific data, so use that.
* Wrapping adds `0.5` to the cost.
* A box adds `1.0` to the cost.
* A card adds `2.0` to the cost.

```fsharp
let rec totalCost gift =
    match gift with 
    | Book book -> 
        book.price
    | Chocolate choc -> 
        choc.price
    | Wrapped (innerGift,style) -> 
        (totalCost innerGift) + 0.5m
    | Boxed innerGift -> 
        (totalCost innerGift) + 1.0m
    | WithACard (innerGift,message) -> 
        (totalCost innerGift) + 2.0m
```

And here are the costs for the two examples:

```fsharp
birthdayPresent |> totalCost 
// 22.5m

christmasPresent |> totalCost 
// 6.5m
```

Sometimes, people ask what is inside the box or wrapping paper.  A `whatsInside` function is easy to implement -- just ignore the container cases
and return something for the non-recursive cases.

```fsharp
let rec whatsInside gift =
    match gift with 
    | Book book -> 
        "A book"
    | Chocolate choc -> 
        "Some chocolate"
    | Wrapped (innerGift,style) -> 
        whatsInside innerGift
    | Boxed innerGift -> 
        whatsInside innerGift
    | WithACard (innerGift,message) -> 
        whatsInside innerGift
```

And the results:

```fsharp
birthdayPresent |> whatsInside 
// "A book"

christmasPresent |> whatsInside 
// "Some chocolate"
```

So that's a good start -- three functions, all quite easy to write.

<a id="parameterize"></a>

## Parameterize all the things

However, these three functions have some duplicate code.
In addition to the unique application logic, each function is doing its own pattern matching and its own logic for recursively visiting the inner gift.

How can we separate the navigation logic from the application logic?

Answer: Parameterize all the things!

As always, we can parameterize the application logic by passing in functions.  In this case, we will need *five* functions, one for each case.

Here is the new, parameterized version -- I'll explain why I have called it `cataGift` shortly.

```fsharp
let rec cataGift fBook fChocolate fWrapped fBox fCard gift =
    match gift with 
    | Book book -> 
        fBook book
    | Chocolate choc -> 
        fChocolate choc
    | Wrapped (innerGift,style) -> 
        let innerGiftResult = cataGift fBook fChocolate fWrapped fBox fCard innerGift
        fWrapped (innerGiftResult,style)
    | Boxed innerGift -> 
        let innerGiftResult = cataGift fBook fChocolate fWrapped fBox fCard innerGift
        fBox innerGiftResult 
    | WithACard (innerGift,message) -> 
        let innerGiftResult = cataGift fBook fChocolate fWrapped fBox fCard innerGift
        fCard (innerGiftResult,message) 
```

You can see this function is created using a purely mechanical process:

* Each function parameter (`fBook`, `fChocolate`, etc) corresponds to a case.
* For the two non-recursive cases, the function parameter is passed all the data associated with that case.
* For the three recursive cases, there are two steps:
  * First, the `cataGift` function is called recursively on the `innerGift` to get an `innerGiftResult`
  * Then the appropriate handler is passed all the data associated with that case, but with `innerGiftResult` replacing `innerGift`.

Let's rewrite total cost using the generic `cataGift` function.

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

Notes: 

* The `innerGiftResult` is now the total cost of the inner gift, so I have renamed it to `innerCost`.
* The `totalCostUsingCata` function itself is not recursive, because it uses the `cataGift` function, and so no longer needs the `rec` keyword.

And this function gives the same result as before:

```fsharp
birthdayPresent |> totalCostUsingCata 
// 22.5m
```

We can rewrite the `description` function using `cataGift` in the same way, changing `innerGiftResult` to `innerText`.

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

And the results are the same as before:

```fsharp
birthdayPresent |> descriptionUsingCata  
// "'Wolf Hall' wrapped in HappyBirthday paper with a card saying 'Happy Birthday'"

christmasPresent |> descriptionUsingCata  
// "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

<a id="catamorphisms"></a>

## Introducing catamorphisms

The `cataGift` function we wrote above is called a "[catamorphism](https://en.wikipedia.org/wiki/Catamorphism)", from the Greek components "down + shape".
In normal usage, a catamorphism is a function that "collapses" a recursive type into a new value based on its *structure*.
In fact, you can think of a catamorphism as a sort of "visitor pattern".

A catamorphism is very powerful concept,
because it is the most fundamental function that you can define for a structure like this. *Any other function* can be defined in terms of it.

That is, if we want to create a function with signature `Gift -> string` or `Gift -> int`,
we can use a catamorphism to create it by specifying a function for each case in the `Gift` structure.

We saw above how we could rewrite `totalCost` as `totalCostUsingCata` using the catamorphism, and we'll see lots of other examples later.


### Catamorphisms and folds

Catamorphisms are often called "folds", but there is more than one kind of fold, so I will tend to use
"catamorphism" to refer the *concept* and "fold" to refer to a specific kind of implementation.

I will talk in detail about the various kinds of folds in a [subsequent post](/posts/recursive-types-and-folds-2/),
so for the rest of this post I will just use "catamorphism".

### Tidying up the implementation

The `cataGift` implementation above was deliberately long-winded so that you could see each step. Once you understand what is going on though,
you can simplify it somewhat.

First, the `cataGift fBook fChocolate fWrapped fBox fCard` crops up three times, once for each recursive case. Let's assign it a name like `recurse`:

```fsharp
let rec cataGift fBook fChocolate fWrapped fBox fCard gift =
    let recurse = cataGift fBook fChocolate fWrapped fBox fCard
    match gift with 
    | Book book -> 
        fBook book
    | Chocolate choc -> 
        fChocolate choc
    | Wrapped (innerGift,style) -> 
        let innerGiftResult = recurse innerGift
        fWrapped (innerGiftResult,style)
    | Boxed innerGift -> 
        let innerGiftResult = recurse innerGift
        fBox innerGiftResult 
    | WithACard (innerGift,message) -> 
        let innerGiftResult = recurse innerGift
        fCard (innerGiftResult,message) 
```

The `recurse` function has the simple signature `Gift -> 'a` -- that is, it converts a `Gift` to the return type we need, and so we can use it
to work with the various `innerGift` values.

The other thing is to replace `innerGift` with just `gift` in the recursive cases -- this is called "shadowing".
The benefit is that the "outer" `gift` is no longer visible to the case-handling code, and so we can't accidentally recurse into it, which would cause an infinite loop.

Generally I avoid shadowing, but this is one case where it actually is a good practice, because it eliminates a particularly nasty kind of bug.

Here's the version after the clean up:

```fsharp
let rec cataGift fBook fChocolate fWrapped fBox fCard gift =
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

One more thing. I'm going to explicitly annotate the return type and call it `'r`. Later on in this series we'll be dealing with other
generic types such as `'a` and `'b`, so it will be helpful to be consistent and always have a standard name for the return type.

```fsharp
let rec cataGift fBook fChocolate fWrapped fBox fCard gift :'r =
//                                name the return type =>  ~~~~ 
```

Here's the final version:

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


It's much simpler than the original implementation, and also demonstrates the symmetry between a case constructor like `Wrapped (gift,style)`
and the corresponding handler `fWrapped (recurse gift,style)`. Which leads us nicely to...

### The relationship between the type constructors and the handlers

Here is the signature for the `cataGift` function. You can see that each case handler function (`fBook`, `fBox`, etc.) has the same pattern:
an input which contains all the data for that case, and a common output type `'r`.  

```fsharp
val cataGift :
  fBook:(Book -> 'r) ->
  fChocolate:(Chocolate -> 'r) ->
  fWrapped:('r * WrappingPaperStyle -> 'r) ->
  fBox:('r -> 'r) ->
  fCard:('r * string -> 'r) -> 
  // input value
  gift:Gift -> 
  // return value
  'r
```

Another way to think about this is that: everywhere that there is a `Gift` type in the constructor, it has been replaced with an `'r`.

For example:

* The `Gift.Book` constructor takes a `Book` and returns a `Gift`. The `fBook` handler takes a `Book` and returns an `'r`.
* The `Gift.Wrapped` constructor takes a `Gift * WrappingPaperStyle` and returns a `Gift`. The `fWrapped` handler takes an `'r * WrappingPaperStyle` as input and returns an `'r`.

Here is that relationship expressed through type signatures:

```fsharp
// The Gift.Book constructor 
Book -> Gift

// The fBook handler
Book -> 'r

// The Gift.Wrapped constructor 
Gift * WrappingPaperStyle -> Gift

// The fWrapped handler
'r   * WrappingPaperStyle -> 'r

// The Gift.Boxed constructor 
Gift -> Gift

// The fBox handler
'r   -> 'r
```

and so on for all of the rest. 

<a id="benefits"></a>

## Benefits of catamorphisms 

There is a lot of theory behind catamorphisms, but what are the benefits in practice?

Why bother to create a special function like `cataGift`? Why not just leave the original functions alone?

There are number of reasons, including:

* **Reuse**. Later, we will be creating quite complex catamorphisms. It's nice if you only have to get the logic right once.
* **Encapsulation**. By exposing functions only, you hide the internal structure of the data type.
* **Flexibility**. Functions are more flexible than pattern matching -- they can be composed, partially applied, etc.
* **Mapping**. With a catamorphism in hand you can easily create functions that map the various cases to new structures.

It's true that most of these benefits apply to non-recursive types as well, but recursive types tend to be more complex so the benefits of
encapsulation, flexibility, etc. are correspondingly stronger.
 
In the following sections, we'll look at the last three points in more detail.

### Using function parameters to hide internal structure 

The first benefit is that a catamorphism abstracts out the internal design. By using functions, the client code is somewhat isolated from the internal
structure. Again, you can think of the Visitor pattern as analogous in the OO world. 

For example, if all the clients used the catamorphism function rather than pattern matching, I could safely rename cases, and even, with a bit of care, add or remove cases.

Here's an example. Let's say that I had a earlier design for `Gift` that didn't have the `WithACard` case. I'll call it version 1:

```fsharp
type Gift =
    | Book of Book
    | Chocolate of Chocolate 
    | Wrapped of Gift * WrappingPaperStyle
    | Boxed of Gift 
```

And say that we built and published a catamorphism function for that structure:

```fsharp
let rec cataGift fBook fChocolate fWrapped fBox gift :'r =
    let recurse = cataGift fBook fChocolate fWrapped fBox 
    match gift with 
    | Book book -> 
        fBook book
    | Chocolate choc -> 
        fChocolate choc
    | Wrapped (gift,style) -> 
        fWrapped (recurse gift,style)
    | Boxed gift -> 
        fBox (recurse gift)
```

Note that this has only *four* function parameters.

Now suppose that version 2 of `Gift` comes along, which adds the `WithACard` case:

```fsharp
type Gift =
    | Book of Book
    | Chocolate of Chocolate 
    | Wrapped of Gift * WrappingPaperStyle
    | Boxed of Gift 
    | WithACard of Gift * message:string
```

and now there are five cases.

Often, when we add a new case, we *want* to break all the clients and force them to deal with the new case.

But sometimes, we don't. And so we can stay compatible with the original `cataGift` by handling the extra case silently, like this:

```fsharp
/// Uses Gift_V2 but is still backwards compatible with the earlier "cataGift".
let rec cataGift fBook fChocolate fWrapped fBox gift :'r =
    let recurse = cataGift fBook fChocolate fWrapped fBox 
    match gift with 
    | Book book -> 
        fBook book
    | Chocolate choc -> 
        fChocolate choc
    | Wrapped (gift,style) -> 
        fWrapped (recurse gift,style)
    | Boxed gift -> 
        fBox (recurse gift)
    // pass through the new case silently        
    | WithACard (gift,message) -> 
        recurse gift
```

This function still has only four function parameters -- there is no special behavior for the `WithACard` case.

There are a number of alternative ways of being compatible, such as returning a default value.
The important point is that the clients are not aware of the change.

**Aside: Using active patterns to hide data**

While we're on the topic of hiding the structure of a type, I should mention that you can also use active patterns to do this.

For example, we could create a active pattern for the first four cases, and ignore the `WithACard` case.

```fsharp
let rec (|Book|Chocolate|Wrapped|Boxed|) gift =
    match gift with 
    | Gift.Book book -> 
        Book book
    | Gift.Chocolate choc -> 
        Chocolate choc
    | Gift.Wrapped (gift,style) -> 
        Wrapped (gift,style)
    | Gift.Boxed gift -> 
        Boxed gift
    | Gift.WithACard (gift,message) -> 
        // ignore the message and recurse into the gift
        (|Book|Chocolate|Wrapped|Boxed|) gift
```

The clients can pattern match on the four cases without knowing that the new case even exists:

```fsharp
let rec whatsInside gift =
    match gift with 
    | Book book -> 
        "A book"
    | Chocolate choc -> 
        "Some chocolate"
    | Wrapped (gift,style) -> 
        whatsInside gift
    | Boxed gift -> 
        whatsInside gift
```

### Case-handling functions vs. pattern matching

Catamorphisms use function parameters, and as noted above, functions are more flexible than pattern matching due to tools such composition, partial application, etc.

Here's an example where all the "container" cases are ignored, and only the "content" cases are handled. 

```fsharp
let handleContents fBook fChocolate gift =
    let fWrapped (innerGiftResult,style) =   
        innerGiftResult
    let fBox innerGiftResult = 
        innerGiftResult
    let fCard (innerGiftResult,message) = 
        innerGiftResult

    // call the catamorphism
    cataGift fBook fChocolate fWrapped fBox fCard gift
```

And here it is in use, with the two remaining cases handled "inline" using piping:

```fsharp
birthdayPresent 
|> handleContents 
    (fun book -> "The book you wanted for your birthday") 
    (fun choc -> "Your fave chocolate")
// Result => "The book you wanted for your birthday"

christmasPresent 
|> handleContents 
    (fun book -> "The book you wanted for Christmas") 
    (fun choc -> "Don't eat too much over the holidays!")
// Result => "Don't eat too much over the holidays!"
```

Of course this could be done with pattern matching, but being able to work with the existing `cataGift` function directly makes life easier.

### Using catamorphisms for mapping

I said above that a catamorphism is a function that "collapses" a recursive type into a new value.
For example, in `totalCost`, the recursive gift structure was collapsed into a single cost value. 

But a "single value" can be more than just a primitive -- it can be a complicated structure too, such as a another recursive structure.

In fact, catamorphisms are great for mapping one kind of structure onto another, especially if they are very similar.

For example, let's say that I have a chocolate-loving room-mate who will stealthily remove and devour any chocolate in a gift, leaving the wrapping untouched,
but discarding the box and gift card.

What's left at the end is a "gift minus chocolate" that we can model as follows:

```fsharp
type GiftMinusChocolate =
    | Book of Book
    | Apology of string
    | Wrapped of GiftMinusChocolate * WrappingPaperStyle
```

We can easily map from a `Gift` to a `GiftMinusChocolate`, because the cases are almost parallel. 

* A `Book` is passed through untouched.
* `Chocolate` is eaten and replaced with an `Apology`.
* The `Wrapped` case is passed through untouched.
* The `Box` and `WithACard` cases are ignored.

Here's the code:

```fsharp
let removeChocolate gift =
    let fBook (book:Book) = 
        Book book
    let fChocolate (choc:Chocolate) = 
        Apology "sorry I ate your chocolate"
    let fWrapped (innerGiftResult,style) = 
        Wrapped (innerGiftResult,style) 
    let fBox innerGiftResult = 
        innerGiftResult
    let fCard (innerGiftResult,message) = 
        innerGiftResult
    // call the catamorphism
    cataGift fBook fChocolate fWrapped fBox fCard gift
```

And if we test...

```fsharp
birthdayPresent |> removeChocolate
// GiftMinusChocolate = 
//     Wrapped (Book {title = "Wolf Hall"; price = 20M}, HappyBirthday)

christmasPresent |> removeChocolate
// GiftMinusChocolate = 
//     Wrapped (Apology "sorry I ate your chocolate", HappyHolidays)
```

### Deep copying

One more thing. Remember that each case-handling function takes the data associated with that case?
That means that we can just use *the original case constructors* as the functions!

To see what I mean, let's define a function called `deepCopy` that clones the original value.
Each case handler is just the corresponding case constructor:

```fsharp
let deepCopy gift =
    let fBook book = 
        Book book 
    let fChocolate (choc:Chocolate) = 
        Chocolate choc
    let fWrapped (innerGiftResult,style) = 
        Wrapped (innerGiftResult,style) 
    let fBox innerGiftResult = 
        Boxed innerGiftResult
    let fCard (innerGiftResult,message) = 
        WithACard (innerGiftResult,message) 
    // call the catamorphism
    cataGift fBook fChocolate fWrapped fBox fCard gift
```

We can simplify this further by removing the redundant parameters for each handler:

```fsharp
let deepCopy gift =
    let fBook = Book 
    let fChocolate = Chocolate 
    let fWrapped = Wrapped 
    let fBox = Boxed 
    let fCard = WithACard 
    // call the catamorphism
    cataGift fBook fChocolate fWrapped fBox fCard gift
```

You can test that this works yourself:

```fsharp
christmasPresent |> deepCopy
// Result => 
//   Wrapped ( 
//    Boxed (Chocolate {chocType = SeventyPercent; price = 5M;}),
//    HappyHolidays)
```

So that leads to another way of thinking about a catamorphism: 

* A catamorphism is a function for a recursive type such that
  when you pass in the type's case constructors, you get a "clone" function. 

### Mapping and transforming in one pass
  
A slight variant on `deepCopy` allows us to recurse through an object and change bits of it as we do so. 

For example, let's say I don't like milk chocolate. Well, I can write a function that upgrades the gift to better quality chocolate and leaves all the other cases alone.

```fsharp
let upgradeChocolate gift =
    let fBook = Book 
    let fChocolate (choc:Chocolate) = 
        Chocolate {choc with chocType = SeventyPercent}
    let fWrapped = Wrapped 
    let fBox = Boxed 
    let fCard = WithACard 
    // call the catamorphism
    cataGift fBook fChocolate fWrapped fBox fCard gift
```  

And here it is in use:
```fsharp
// create some chocolate I don't like
let cheapChoc = Boxed (Chocolate {chocType=Milk; price=5m})

// upgrade it!
cheapChoc |> upgradeChocolate
// Result =>
//   Boxed (Chocolate {chocType = SeventyPercent; price = 5M})
```  

If you are thinking that this is beginning to smell like a `map`, you'd be right.
We'll look at generic maps in the [sixth post, as part of a discussion of generic recursive types](/posts/recursive-types-and-folds-3b/#map).
  
  
<a id="rules"></a>
  
## Rules for creating catamorphisms

We saw above that creating a catamorphism is a mechanical process:

* Create a function parameter to handle each case in the structure.
* For non-recursive cases, pass the function parameter all the data associated with that case.
* For recursive cases, perform two steps:
  * First, call the catamorphism recursively on the nested value.
  * Then pass the handler all the data associated with that case, but with the result of the catamorphism replacing the original nested value.

Let's now see if we can apply these rules to create catamorphisms in other domains.

<hr>
    
## Summary 

We've seen in this post how to define a recursive type, and been introduced to catamorphisms.

In the [next post](/posts/recursive-types-and-folds-1b/)
we'll uses these rules to create catamorphisms for some other domains.

See you then!

*The source code for this post is available at [this gist](https://gist.github.com/swlaschin/60938b4417d12cfa0a97).*



