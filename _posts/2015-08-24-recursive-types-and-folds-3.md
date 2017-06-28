---
layout: post
title: "Generic recursive types"
description: "Implementing a domain in three ways"
seriesId: "Recursive types and folds"
seriesOrder: 5
categories: [Folds, Patterns]
---


This post is the fifth in a series.

In the [previous post](/posts/recursive-types-and-folds-2b/), we spent some time understanding folds for specific domain types.

In this post, we'll broaden our horizons and look at how to use generic recursive types.

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

<a id="linkedlist"></a>
<hr>

## LinkedList: A generic recursive type

Here's a question: if you only have algebraic types, and you can only combine them as products ([tuples](/posts/tuples/), [records](/posts/records/))
or sums ([discriminated unions](/posts/discriminated-unions/)), then how can you make a list type just by using these operations?

The answer is, of course, recursion!

Let's start with the most basic recursive type: the list.

I'm going to call my version `LinkedList`, but it is basically the same as the `list` type in F#.

So, how do you define a list in a recursive way? 

Well, it's either empty, or it consists of an element plus another list.
In other words we can define it as a choice type ("discriminated union") like this:

```fsharp
type LinkedList<'a> = 
    | Empty
    | Cons of head:'a * tail:LinkedList<'a>
```

The `Empty` case represents an empty list. The `Cons` case has a tuple: the head element, and the tail, which is another list.

We can then define a particular `LinkedList` value like this:

```fsharp
let linkedList = Cons (1, Cons (2, Cons(3, Empty)))  
```

Using the native F# list type, the equivalent definition would be:

```fsharp
let linkedList = 1 :: 2 :: 3 :: []
```

which is just `[1; 2; 3]`

### `cata` for LinkedList

Following the rules in the [first post in this series](/posts/recursive-types-and-folds/#rules),
we can mechanically create a `cata` function by replacing `Empty` and `Cons` with `fEmpty` and `fCons`:

```fsharp
module LinkedList = 

    let rec cata fCons fEmpty list :'r=
        let recurse = cata fCons fEmpty 
        match list with
        | Empty -> 
            fEmpty
        | Cons (element,list) -> 
            fCons element (recurse list)
```

*Note: We will be putting all the functions associated with `LinkedList<'a>` in a module called `LinkedList`. One nice thing about using generic types is that the type name does not clash with a similar module name!*

As always, the signatures of the case handling functions are parallel to the signatures of the type constructors, with `LinkedList` replaced by `'r`.

```fsharp
val cata : 
    fCons:('a -> 'r -> 'r) ->   
    fEmpty:'r ->                
    list:LinkedList<'a> 
    -> 'r
```

### `fold` for LinkedList

We can also create a top-down iterative `fold` function using the rules in the [earlier post](/posts/recursive-types-and-folds-2/#rules).

```fsharp
module LinkedList = 

    let rec cata ...
    
    let rec foldWithEmpty fCons fEmpty acc list :'r=
        let recurse = foldWithEmpty fCons fEmpty 
        match list with
        | Empty -> 
            fEmpty acc 
        | Cons (element,list) -> 
            let newAcc = fCons acc element 
            recurse newAcc list
```

This `foldWithEmpty` function is not quite the same as the standard `List.fold` function, because it has an extra function parameter for the empty case (`fEmpty`).
However, if we eliminate that parameter and just return the accumulator we get this variant:

```fsharp
module LinkedList = 

    let rec fold fCons acc list :'r=
        let recurse = fold fCons 
        match list with
        | Empty -> 
            acc 
        | Cons (element,list) -> 
            let newAcc = fCons acc element 
            recurse newAcc list
```

If we compare the signature with the [List.fold documentation](https://msdn.microsoft.com/en-us/library/ee353894.aspx) we can see that they are equivalent,
with `'State` replaced by `'r` and `'T list` replaced by `LinkedList<'a>`:

```fsharp
LinkedList.fold : ('r     -> 'a -> 'r    ) -> 'r      -> LinkedList<'a> -> 'r
List.fold       : ('State -> 'T -> 'State) -> 'State -> 'T list         -> 'State
```


Let's test that `fold` works by doing a small sum:

```fsharp
let linkedList = Cons (1, Cons (2, Cons(3, Empty)))  
linkedList |> LinkedList.fold (+) 0
// Result => 6
```

### `foldBack` for LinkedList

Finally we can create a `foldBack` function, using the "function accumulator" approach described in the previous post:

```fsharp
module LinkedList = 

    let rec cata ...
    
    let rec fold ...

    let foldBack fCons list acc :'r=
        let fEmpty' generator = 
            generator acc 
        let fCons' generator element= 
            fun innerResult -> 
                let newResult = fCons element innerResult 
                generator newResult 
        let initialGenerator = id
        foldWithEmpty fCons' fEmpty' initialGenerator  list 
```

Again, if we compare the signature with the [List.foldBack documentation](https://msdn.microsoft.com/en-us/library/ee353846.aspx), they are also equivalent,
with `'State` replaced by `'r` and `'T list` replaced by `LinkedList<'a>`:

```fsharp
LinkedList.foldBack : ('a -> 'r     -> 'r    ) -> LinkedList<'a> -> 'r     -> 'r
List.foldBack       : ('T -> 'State -> 'State) -> 'T list        -> 'State -> 'State
```

### Using `foldBack` to convert between list types

In the [first post](/posts/recursive-types-and-folds/#benefits) we noted that catamorphisms could be used for converting between types of similar structure.

Let's demonstrate that now by creating some functions that convert from `LinkedList` to the native `list` type and back again.

To convert a `LinkedList` to a native `list` all we need to do is replace `Cons` with `::` and `Empty` with `[]`:

```fsharp
module LinkedList = 

    let toList linkedList = 
        let fCons head tail = head::tail
        let initialState = [] 
        foldBack fCons linkedList initialState 
```

To convert the other way, we need to replace `::` with `Cons` and `[]` with `Empty`:

```fsharp
module LinkedList = 

    let ofList list = 
        let fCons head tail = Cons(head,tail)
        let initialState = Empty
        List.foldBack fCons list initialState 
```

Simple!  Let's test `toList`:

```fsharp
let linkedList = Cons (1, Cons (2, Cons(3, Empty)))  
linkedList |> LinkedList.toList       
// Result => [1; 2; 3]
```

and `ofList`:

```fsharp
let list = [1;2;3]
list |> LinkedList.ofList       
// Result => Cons (1,Cons (2,Cons (3,Empty)))
```

Both work as expected.

### Using `foldBack` to implement other functions

I said earlier that a catamorphism function (for linear lists, `foldBack`) is the most basic function available for a recursive type, and in fact is the *only* function you need!

Let's see for ourselves by implementing some other common functions using `foldBack`.

Here's `map` defined in terms of `foldBack`:

```fsharp
module LinkedList = 

    /// map a function "f" over all elements
    let map f list = 
        // helper function    
        let folder head tail =
            Cons(f head,tail)
            
        foldBack folder list Empty
```

And here's a test:

```fsharp
let linkedList = Cons (1, Cons (2, Cons(3, Empty)))  

linkedList |> LinkedList.map (fun i -> i+10)
// Result => Cons (11,Cons (12,Cons (13,Empty)))
```

Here's `filter` defined in terms of `foldBack`:

```fsharp
module LinkedList = 

    /// return a new list of elements for which "pred" is true
    let filter pred list = 
        // helper function
        let folder head tail =
            if pred head then 
                Cons(head,tail)
            else
                tail

        foldBack folder list Empty
```

And here's a test:

```fsharp
let isOdd n = (n%2=1)
let linkedList = Cons (1, Cons (2, Cons(3, Empty)))  

linkedList |> LinkedList.filter isOdd
// Result => Cons (1,Cons (3,Empty))
```

Finally, here's `rev` defined in terms of `fold`:

```fsharp
/// reverse the elements of the list
let rev list = 
    // helper function
    let folder tail head =
        Cons(head,tail)

    fold folder Empty list 
```

And here's a test:

```fsharp
let linkedList = Cons (1, Cons (2, Cons(3, Empty)))  
linkedList |> LinkedList.rev
// Result => Cons (3,Cons (2,Cons (1,Empty)))
```

So, I hope you're convinced!

### Avoiding generator functions

I mentioned earlier that there was an alternative and (sometimes) more efficient way to implement `foldBack` without using generators or continuations.

As we have seen, `foldBack` is reverse iteration, which means that it is the same as `fold` applied to a reversed list!

So we could implement it like this:

```fsharp
let foldBack_ViaRev fCons list acc :'r=
    let fCons' acc element = 
        // just swap the params!
        fCons element acc 
    list
    |> rev
    |> fold fCons' acc 
```

It involves making an extra copy of the list, but on the other hand there is no longer a large set of pending continuations. It might
be worth comparing the profile of the two versions in your environment if performance is an issue.


<a id="revisiting-gift"></a>

## Making the Gift domain generic

In the rest of this post, we'll look at the `Gift` type and see if we can make it more generic.

As a reminder, here is the original design:

```fsharp
type Gift =
    | Book of Book
    | Chocolate of Chocolate 
    | Wrapped of Gift * WrappingPaperStyle
    | Boxed of Gift 
    | WithACard of Gift * message:string
```

Three of the cases are recursive and two are non-recursive.  

Now, the focus of this particular design was on modelling the domain, which is why there are so many separate cases.

But if we want to focus on *reusability* instead of domain modelling, then we should simplify the design to the essentials, and all these special cases now become a hindrance.

To make this ready for reuse, then, let's collapse all the non-recursive cases into one case, say `GiftContents`,
and all the recursive cases into another case, say `GiftDecoration`, like this:  

```fsharp
// unified data for non-recursive cases
type GiftContents = 
    | Book of Book
    | Chocolate of Chocolate 

// unified data for recursive cases
type GiftDecoration = 
    | Wrapped of WrappingPaperStyle
    | Boxed 
    | WithACard of string

type Gift =
    // non-recursive case
    | Contents of GiftContents
    // recursive case
    | Decoration of Gift * GiftDecoration
```

The main `Gift` type has only two cases now: the non-recursive one and the recursive one.

<a id="container"></a>

## Defining a generic Container type 

Now that the type is simplified, we can "genericize" it by allowing *any* kind of contents *and* any kind of decoration.

```fsharp
type Container<'ContentData,'DecorationData> =
    | Contents of 'ContentData
    | Decoration of 'DecorationData * Container<'ContentData,'DecorationData> 
```

And as before, we can mechanically create a `cata` and `fold` and `foldBack` for it, using the standard process:

```fsharp
module Container = 

    let rec cata fContents fDecoration (container:Container<'ContentData,'DecorationData>) :'r = 
        let recurse = cata fContents fDecoration 
        match container with
        | Contents contentData -> 
            fContents contentData 
        | Decoration (decorationData,subContainer) -> 
            fDecoration decorationData (recurse subContainer)
            
    (*
    val cata :
        // function parameters
        fContents:('ContentData -> 'r) ->
        fDecoration:('DecorationData -> 'r -> 'r) ->
        // input
        container:Container<'ContentData,'DecorationData> -> 
        // return value
        'r
    *)
            
    let rec fold fContents fDecoration acc (container:Container<'ContentData,'DecorationData>) :'r = 
        let recurse = fold fContents fDecoration 
        match container with
        | Contents contentData -> 
            fContents acc contentData 
        | Decoration (decorationData,subContainer) -> 
            let newAcc = fDecoration acc decorationData
            recurse newAcc subContainer
            
    (*
    val fold :
        // function parameters
        fContents:('a -> 'ContentData -> 'r) ->
        fDecoration:('a -> 'DecorationData -> 'a) ->
        // accumulator
        acc:'a -> 
        // input
        container:Container<'ContentData,'DecorationData> -> 
        // return value
        'r
    *)
            
    let foldBack fContents fDecoration (container:Container<'ContentData,'DecorationData>) :'r = 
        let fContents' generator contentData =
            generator (fContents contentData)
        let fDecoration' generator decorationData =
            let newGenerator innerValue =
                let newInnerValue = fDecoration decorationData innerValue 
                generator newInnerValue 
            newGenerator 
        fold fContents' fDecoration' id container
            
    (*
    val foldBack :
        // function parameters
        fContents:('ContentData -> 'r) ->
        fDecoration:('DecorationData -> 'r -> 'r) ->
        // input
        container:Container<'ContentData,'DecorationData> -> 
        // return value
        'r
    *)
```


### Converting the gift domain to use the Container type 

Let's convert the gift type to this generic Container type:

```fsharp
type Gift = Container<GiftContents,GiftDecoration>
```

Now we need some helper methods to construct values while hiding the "real" cases of the generic type:

```fsharp
let fromBook book = 
    Contents (Book book)

let fromChoc choc = 
    Contents (Chocolate choc)

let wrapInPaper paperStyle innerGift = 
    let container = Wrapped paperStyle 
    Decoration (container, innerGift)

let putInBox innerGift = 
    let container = Boxed
    Decoration (container, innerGift)

let withCard message innerGift = 
    let container = WithACard message
    Decoration (container, innerGift)
```

Finally we can create some test values:

```fsharp
let wolfHall = {title="Wolf Hall"; price=20m}
let yummyChoc = {chocType=SeventyPercent; price=5m}

let birthdayPresent = 
    wolfHall 
    |> fromBook
    |> wrapInPaper HappyBirthday
    |> withCard "Happy Birthday"
 
let christmasPresent = 
    yummyChoc
    |> fromChoc
    |> putInBox
    |> wrapInPaper HappyHolidays
```


### The `totalCost` function using the Container type

The "total cost" function can be written using `fold`, since it doesn't need any inner data.

Unlike the earlier implementations, we only have two function parameters, `fContents` and `fDecoration`, so each of these
will need some pattern matching to get at the "real" data.

Here's the code:

```fsharp
let totalCost gift =  

    let fContents costSoFar contentData = 
        match contentData with
        | Book book ->
            costSoFar + book.price
        | Chocolate choc ->
            costSoFar + choc.price

    let fDecoration costSoFar decorationInfo = 
        match decorationInfo with
        | Wrapped style ->
            costSoFar + 0.5m
        | Boxed ->
            costSoFar + 1.0m
        | WithACard message ->
            costSoFar + 2.0m

    // initial accumulator
    let initialAcc = 0m

    // call the fold
    Container.fold fContents fDecoration initialAcc gift 
```

And the code works as expected:

```fsharp
birthdayPresent |> totalCost 
// 22.5m

christmasPresent |> totalCost 
// 6.5m
```

### The `description` function using the Container type

The "description" function needs to be written using `foldBack`, since it *does* need the inner data. As with the code above,
we need some pattern matching to get at the "real" data for each case.

```fsharp
let description gift =

    let fContents contentData = 
        match contentData with
        | Book book ->
            sprintf "'%s'" book.title
        | Chocolate choc ->
            sprintf "%A chocolate" choc.chocType

    let fDecoration decorationInfo innerText = 
        match decorationInfo with
        | Wrapped style ->
            sprintf "%s wrapped in %A paper" innerText style
        | Boxed ->
            sprintf "%s in a box" innerText 
        | WithACard message ->
            sprintf "%s with a card saying '%s'" innerText message 

    // main call
    Container.foldBack fContents fDecoration gift  
```

And again the code works as we want:

```fsharp
birthdayPresent |> description
// CORRECT "'Wolf Hall' wrapped in HappyBirthday paper with a card saying 'Happy Birthday'"

christmasPresent |> description
// CORRECT "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

<a id="another-gift"></a>

## A third way to implement the gift domain 

That all looks quite nice, doesn't it?

But I have to confess that I have been holding something back.

None of that code above was strictly necessary, because it turns out that there is yet *another* way to model a `Gift`,
without creating any new generic types at all!

The `Gift` type is basically a linear sequence of decorations, with some content as the final step. We can just model this as a pair -- a `Content` and a list of `Decoration`.
Or to make it a little friendlier, a record with two fields: one for the content and one for the decorations.

```fsharp
type Gift = {contents: GiftContents; decorations: GiftDecoration list}
```

That's it! No other new types needed!

### Building values using the record type

As before, let's create some helpers to construct values using this type:

```fsharp
let fromBook book = 
    { contents = (Book book); decorations = [] }

let fromChoc choc = 
    { contents = (Chocolate choc); decorations = [] }

let wrapInPaper paperStyle innerGift = 
    let decoration = Wrapped paperStyle 
    { innerGift with decorations = decoration::innerGift.decorations }

let putInBox innerGift = 
    let decoration = Boxed
    { innerGift with decorations = decoration::innerGift.decorations }

let withCard message innerGift = 
    let decoration = WithACard message
    { innerGift with decorations = decoration::innerGift.decorations }
```

With these helper functions, the way the values are constructed is *identical* to the previous version. This is why it is good to hide your raw constructors, folks!

```fsharp
let wolfHall = {title="Wolf Hall"; price=20m}
let yummyChoc = {chocType=SeventyPercent; price=5m}

let birthdayPresent = 
    wolfHall 
    |> fromBook
    |> wrapInPaper HappyBirthday
    |> withCard "Happy Birthday"
 
let christmasPresent = 
    yummyChoc
    |> fromChoc
    |> putInBox
    |> wrapInPaper HappyHolidays
```

### The `totalCost` function using the record type

The `totalCost` function is even easier to write now. 

```fsharp
let totalCost gift =  
    
    let contentCost = 
        match gift.contents with
        | Book book ->
            book.price
        | Chocolate choc ->
            choc.price

    let decorationFolder costSoFar decorationInfo = 
        match decorationInfo with
        | Wrapped style ->
            costSoFar + 0.5m
        | Boxed ->
            costSoFar + 1.0m
        | WithACard message ->
            costSoFar + 2.0m

    let decorationCost = 
        gift.decorations |> List.fold decorationFolder 0m

    // total cost
    contentCost + decorationCost 
```

### The `description` function using the record type

Similarly, the `description` function is also easy to write.

```fsharp
let description gift =

    let contentDescription = 
        match gift.contents with
        | Book book ->
            sprintf "'%s'" book.title
        | Chocolate choc ->
            sprintf "%A chocolate" choc.chocType

    let decorationFolder decorationInfo innerText = 
        match decorationInfo with
        | Wrapped style ->
            sprintf "%s wrapped in %A paper" innerText style
        | Boxed ->
            sprintf "%s in a box" innerText 
        | WithACard message ->
            sprintf "%s with a card saying '%s'" innerText message 

    List.foldBack decorationFolder gift.decorations contentDescription
```

<a id="compare"></a>

## Abstract or concrete? Comparing the three designs 

If you are confused by this plethora of designs, I don't blame you!

But as it happens, the three different definitions are actually interchangable:

**The original version**

```fsharp
type Gift =
    | Book of Book
    | Chocolate of Chocolate 
    | Wrapped of Gift * WrappingPaperStyle
    | Boxed of Gift 
    | WithACard of Gift * message:string
```

**The generic container version**

```fsharp
type Container<'ContentData,'DecorationData> =
    | Contents of 'ContentData
    | Decoration of 'DecorationData * Container<'ContentData,'DecorationData> 
    
type GiftContents = 
    | Book of Book
    | Chocolate of Chocolate 

type GiftDecoration = 
    | Wrapped of WrappingPaperStyle
    | Boxed 
    | WithACard of string

type Gift = Container<GiftContents,GiftDecoration>
```

**The record version**

```fsharp
type GiftContents = 
    | Book of Book
    | Chocolate of Chocolate 

type GiftDecoration = 
    | Wrapped of WrappingPaperStyle
    | Boxed 
    | WithACard of string

type Gift = {contents: GiftContents; decorations: GiftDecoration list}
```

If this is not obvious, it might be helpful to read my post on [data type sizes](/posts/type-size-and-design/). It explains how two types can be "equivalent",
even though they appear to be completely different at first glance.

### Picking a design 

So which design is best?  The answer is, as always, "it depends".

For modelling and documenting a domain, I like the first design with the five explicit cases. 
Being easy for other people to understand is more important to me than introducing abstraction for the sake of reusability.

If I wanted a reusable model that was applicable in many situations, I'd probably choose the second ("Container") design. It seems to me that this type
does represent a commonly encountered situation, where the contents are one kind of thing and the wrappers are another kind of thing.
This abstraction is therefore likely to get some use.  

The final "pair" model is fine, but by separating the two components, we've over-abstracted the design for this scenario. In other situations, this
design might be a great fit (e.g. the decorator pattern), but not here, in my opinion.

There is one further choice which gives you the best of all worlds.

As I noted above, all the designs are logically equivalent, which means there are "lossless" mappings between them.
In that case, your "public" design can be the domain-oriented one, like the first one, but behind the scenes you can map it to a more efficient and reusable "private" type.

Even the F# list implementation itself does this.
For example, some of the functions in the `List` module, such `foldBack` and `sort`, convert the list into an array, do the operations, and then convert it back to a list again.

<hr>
    
## Summary 

In this post we looked at some ways of modelling the `Gift` as a generic type, and the pros and cons of each approach.

In the [next post](/posts/recursive-types-and-folds-3b/) we'll look at real-world examples of using a generic recursive type.

*The source code for this post is available at [this gist](https://gist.github.com/swlaschin/c423a0f78b22496a0aff).*