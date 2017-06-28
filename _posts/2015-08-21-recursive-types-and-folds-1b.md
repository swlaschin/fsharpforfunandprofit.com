---
layout: post
title: "Catamorphism examples"
description: "Applying the rules to other domains"
seriesId: "Recursive types and folds"
seriesOrder: 2
categories: [Folds, Patterns]
---

This post is the second in a series.

In the [previous post](/posts/recursive-types-and-folds/), I introduced "catamorphisms", a way of creating functions for recursive types,
and listed some rules which can be used to implement them mechanically.
In this post, we'll use these rules to implement catamorphisms for some other domains.

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
  

<a id="rules"></a>
<hr>

  
## Rules for creating catamorphisms

We saw in the previous post that creating a catamorphism is a mechanical process, and the rules were:

* Create a function parameter to handle each case in the structure.
* For non-recursive cases, pass the function parameter all the data associated with that case.
* For recursive cases, perform two steps:
  * First, call the catamorphism recursively on the nested value.
  * Then pass the handler all the data associated with that case, but with the result of the catamorphism replacing the original nested value.

Let's now see if we can apply these rules to create catamorphisms in other domains.

<a id="file-system"></a>
<hr>

## Catamorphism example: File system domain

Let's start with a very crude model of a file system:

* Each file has a name and a size.
* Each directory has a name and a size and a list of subitems.

Here's how I might model that:

```fsharp
type FileSystemItem =
    | File of File
    | Directory of Directory
and File = {name:string; fileSize:int}
and Directory = {name:string; dirSize:int; subitems:FileSystemItem list}
```

I admit it's a pretty bad model, but it's just good enough for this example!

Ok, here are some sample files and directories:

```fsharp
let readme = File {name="readme.txt"; fileSize=1}
let config = File {name="config.xml"; fileSize=2}
let build  = File {name="build.bat"; fileSize=3}
let src = Directory {name="src"; dirSize=10; subitems=[readme; config; build]}
let bin = Directory {name="bin"; dirSize=10; subitems=[]}
let root = Directory {name="root"; dirSize=5; subitems=[src; bin]}
```

Time to create the catamorphism!

Let's start by looking at the signatures to figure out what we need.

The `File` constructor takes a `File` and returns a `FileSystemItem`. Using the guidelines above, the handler for the `File` case
needs to have the signature `File -> 'r`.

```fsharp
// case constructor
File  : File -> FileSystemItem

// function parameter to handle File case 
fFile : File -> 'r
```

That's simple enough. Let's put together an initial skeleton of `cataFS`, as I'll call it:

```fsharp
let rec cataFS fFile fDir item :'r = 
    let recurse = cataFS fFile fDir 
    match item with
    | File file -> 
        fFile file
    | Directory dir -> 
        // to do
```

The `Directory` case is more complicated.  If we naively applied the guidelines above, the handler for the `Directory` case
would have the signature `Directory -> 'r`, but that would be incorrect, because the `Directory` record itself contains a 
`FileSystemItem` that needs to be replaced with an `'r` too.  How can we do this?

One way is to "explode" the `Directory` record into a tuple of `(string,int,FileSystemItem list)`, and then replace the `FileSystemItem` with `'r` in there too.
 
In other words, we have this sequence of transformations:
 
```fsharp
// case constructor (Directory as record)
Directory : Directory -> FileSystemItem

// case constructor (Directory unpacked as tuple)
Directory : (string, int, FileSystemItem list) -> FileSystemItem
//   replace with 'r ===> ~~~~~~~~~~~~~~          ~~~~~~~~~~~~~~

// function parameter to handle Directory case 
fDir :      (string, int, 'r list)             -> 'r
```

Another issue is that the data associated with the Directory case is a *list* of `FileSystemItem`s.  How can we convert that into a list of `'r`s?

Well, the `recurse` helper turns a `FileSystemItem` into an `'r`,
so we can just use `List.map` passing in `recurse` as the mapping function, and that will give us the list of `'r`s we need!

Putting it all together, we get this implementation:

```fsharp
let rec cataFS fFile fDir item :'r = 
    let recurse = cataFS fFile fDir 
    match item with
    | File file -> 
        fFile file
    | Directory dir -> 
        let listOfRs = dir.subitems |> List.map recurse 
        fDir (dir.name,dir.dirSize,listOfRs) 
```

and if we look at the type signature, we can see that it is just what we want:

```fsharp
val cataFS :
    fFile : (File -> 'r) ->
    fDir  : (string * int * 'r list -> 'r) -> 
    // input value
    FileSystemItem -> 
    // return value
    'r
```

So we're done. It's a bit complicated to set up, but once built, we have a nice reusable function that can be the basis for many others.

### File system domain: `totalSize` example

Alrighty then, let's use it in practice.
  
To start with, we can easily define a `totalSize` function that returns the total size of an item and all its subitems:

```fsharp
let totalSize fileSystemItem =
    let fFile (file:File) = 
        file.fileSize
    let fDir (name,size,subsizes) = 
        (List.sum subsizes) + size
    cataFS fFile fDir fileSystemItem
```

And here are the results:

```fsharp
readme |> totalSize  // 1
src |> totalSize     // 16 = 10 + (1 + 2 + 3)
root |> totalSize    // 31 = 5 + 16 + 10
```
  
### File system domain: `largestFile` example
  
How about a more complicated function, such as "what is the largest file in the tree?"

Before we start this one, let's think about what it should return. That is, what is the `'r`?

You might think that it's just a `File`. But what if the subdirectory is empty and there *are* no files?

So let's make `'r` a `File option` instead.
  
The function for the `File` case should return `Some file` then:
  
```fsharp
let fFile (file:File) = 
    Some file
```

The function for the `Directory` case needs more thought:

* If list of subfiles is empty, then return `None`
* If list of subfiles is non-empty, then return the largest one

```fsharp
let fDir (name,size,subfiles) = 
    match subfiles with
    | [] -> 
        None  // empty directory
    | subfiles -> 
        // return largest one
```
  
But remember that `'r` is not a `File` but a `File option`. So that means that `subfiles` is not a list of files, but a list of `File option`.

Now, how can we find the largest one of these?  We probably want to use `List.maxBy` and pass in the size. But what is the size of a `File option`?

Let's write a helper function to provide the size of a `File option`, using this logic:

* If the `File option` is `None`, return 0
* Else return the size of the file inside the option

Here's the code:

```fsharp
// helper to provide a default if missing
let ifNone deflt opt =
    defaultArg opt deflt 

// get the file size of an option    
let fileSize fileOpt = 
    fileOpt 
    |> Option.map (fun file -> file.fileSize)
    |> ifNone 0
```

Putting it all together then, we have our `largestFile` function:
  
```fsharp
let largestFile fileSystemItem =

    // helper to provide a default if missing
    let ifNone deflt opt =
        defaultArg opt deflt 

    // helper to get the size of a File option
    let fileSize fileOpt = 
        fileOpt 
        |> Option.map (fun file -> file.fileSize)
        |> ifNone 0

    // handle File case        
    let fFile (file:File) = 
        Some file

    // handle Directory case        
    let fDir (name,size,subfiles) = 
        match subfiles with
        | [] -> 
            None  // empty directory
        | subfiles -> 
            // find the biggest File option using the helper
            subfiles 
            |> List.maxBy fileSize  

    // call the catamorphism
    cataFS fFile fDir fileSystemItem
```
  
If we test it, we get the results we expect:

```fsharp
readme |> largestFile  
// Some {name = "readme.txt"; fileSize = 1}

src |> largestFile     
// Some {name = "build.bat"; fileSize = 3}

bin |> largestFile     
// None

root |> largestFile    
// Some {name = "build.bat"; fileSize = 3}
```

Again, a little bit tricky to set up, but no more than if we had to write it from scratch without using a catamorphism at all.

<a id="product"></a>
<hr>

## Catamorphism example: Product domain

Let's work with a slightly more complicated domain. This time, imagine that we make and sell products of some kind:

* Some products are bought, with an optional vendor.
* Some products are made on our premises, built from subcomponents,
  where a subcomponent is some quantity of another product. 

Here's the domain modelled as types:
  
```fsharp
type Product =
    | Bought of BoughtProduct 
    | Made of MadeProduct 
and BoughtProduct = {
    name : string 
    weight : int 
    vendor : string option }
and MadeProduct = {
    name : string 
    weight : int 
    components:Component list }
and Component = {
    qty : int
    product : Product }
```

Note that the types are mutally recursive. `Product` references `MadeProduct` which references `Component` which in turn references `Product` again.

Here are some example products:

```fsharp
let label = 
    Bought {name="label"; weight=1; vendor=Some "ACME"}
let bottle = 
    Bought {name="bottle"; weight=2; vendor=Some "ACME"}
let formulation = 
    Bought {name="formulation"; weight=3; vendor=None}

let shampoo = 
    Made {name="shampoo"; weight=10; components=
    [
    {qty=1; product=formulation}
    {qty=1; product=bottle}
    {qty=2; product=label}
    ]}

let twoPack = 
    Made {name="twoPack"; weight=5; components=
    [
    {qty=2; product=shampoo}
    ]}
```

Now to design the catamorphism, we need to do is replace the `Product` type with `'r` in all the constructors.

Just as with the previous example, the `Bought` case is easy:

```fsharp
// case constructor
Bought  : BoughtProduct -> Product

// function parameter to handle Bought case 
fBought : BoughtProduct -> 'r
```

The `Made` case is trickier. We need to expand the `MadeProduct` into a tuple. But that tuple contains a `Component`, so we need to expand that as well.
Finally we get to the inner `Product`, and we can then mechanically replace that with `'r`. 

Here's the sequence of transformations:

```fsharp
// case constructor
Made  : MadeProduct -> Product

// case constructor (MadeProduct unpacked as tuple)
Made  : (string,int,Component list) -> Product

// case constructor (Component unpacked as tuple)
Made  : (string,int,(int,Product) list) -> Product
//  replace with 'r ===> ~~~~~~~           ~~~~~~~

// function parameter to handle Made case 
fMade : (string,int,(int,'r) list)      -> 'r
```

When implementing the `cataProduct` function we need to the same kind of mapping as before, turning a list of `Component` into a list of `(int,'r)`.

We'll need a helper for that:

```fsharp
// Converts a Component into a (int * 'r) tuple
let convertComponentToTuple comp =
    (comp.qty,recurse comp.product)
```

You can see that this uses the `recurse` function to turn the inner product (`comp.product`) into an `'r` and then make a tuple `int * 'r`.

With `convertComponentToTuple` available, we can convert all the components to tuples using `List.map`:

```fsharp
let componentTuples = 
    made.components 
    |> List.map convertComponentToTuple 
```

`componentTuples` is a list of `(int * 'r)`, which is just what we need for the `fMade` function.

The complete implementation of `cataProduct` looks like this:

```fsharp
let rec cataProduct fBought fMade product :'r = 
    let recurse = cataProduct fBought fMade 

    // Converts a Component into a (int * 'r) tuple
    let convertComponentToTuple comp =
        (comp.qty,recurse comp.product)

    match product with
    | Bought bought -> 
        fBought bought 
    | Made made -> 
        let componentTuples =  // (int * 'r) list
            made.components 
            |> List.map convertComponentToTuple 
        fMade (made.name,made.weight,componentTuples) 
```

### Product domain: `productWeight` example

We can now use `cataProduct` to calculate the weight, say.

```fsharp
let productWeight product =

    // handle Bought case
    let fBought (bought:BoughtProduct) = 
        bought.weight

    // handle Made case
    let fMade (name,weight,componentTuples) = 
        // helper to calculate weight of one component tuple
        let componentWeight (qty,weight) =
            qty * weight
        // add up the weights of all component tuples
        let totalComponentWeight = 
            componentTuples 
            |> List.sumBy componentWeight 
        // and add the weight of the Made case too
        totalComponentWeight + weight

    // call the catamorphism
    cataProduct fBought fMade product
```

Let's test it interactively to make sure it works:

```fsharp
label |> productWeight    // 1
shampoo |> productWeight  // 17 = 10 + (2x1 + 1x2 + 1x3)
twoPack |> productWeight  // 39 = 5  + (2x17)
```
    
That's as we expect.

Try implementing `productWeight` from scratch, without using a helper function like `cataProduct`. Again, it's do-able,
but you'll probably waste quite bit of time getting the recursion logic right.

### Product domain: `mostUsedVendor` example

Let's do a more complex function now.  What is the most used vendor? 

The logic is simple: each time a product references a vendor, we'll give that vendor one point, and the vendor with the highest score wins.

Again, let's think about what it should return. That is, what is the `'r`?

You might think that it's just a score of some kind, but we also need to know the vendor name. Ok, a tuple then. But what if there are no vendors?

So let's make `'r` a `VendorScore option`, where we are going to create a little type `VendorScore`, rather than using a tuple.

```fsharp
type VendorScore = {vendor:string; score:int}
```

We'll also define some helpers to get data from a `VendorScore` easily:

```fsharp
let vendor vs = vs.vendor
let score vs = vs.score
```

Now, you can't determine the most used vendor over until you have results from the entire tree, so both the `Bought` case and the
`Made` case need to return a list which can added to as we recurse up the tree.
And then, after getting *all* the scores, we'll sort descending to find the vendor with the highest one.
  
So we have to make `'r` a `VendorScore list`, not just an option!
  
The logic for the `Bought` case is then:

* If the vendor is present, return a `VendorScore` with score = 1, but as a one-element list rather than as a single item.
* If the vendor is missing, return an empty list.
  
```fsharp
let fBought (bought:BoughtProduct) = 
    // set score = 1 if there is a vendor
    bought.vendor
    |> Option.map (fun vendor -> {vendor = vendor; score = 1} )
    // => a VendorScore option
    |> Option.toList
    // => a VendorScore list
```

The function for the `Made` case is more complicated.

* If list of subscores is empty, then return an empty list.
* If list of subscores is non-empty, we sum them by vendor and then return the new list.

But the list of subresults passed into the `fMade` function will not be a list of subscores, it will be a list of tuples, `qty * 'r` where `'r` is `VendorScore list`. Complicated!

What we need to do then is:

* Turn `qty * 'r` into just `'r` because we don't care about the qty in this case. We now have a list of `VendorScore list`. We can use `List.map snd` to do this.
* But now we would have a list of `VendorScore list`. We can flatten a list of lists into a simple list using `List.collect`. And in fact, using `List.collect snd` can do both steps in one go.
* Group this list by vendor so that we have a list of `key=vendor; values=VendorScore list` tuples.
* Sum up the scores for each vendor (`values=VendorScore list`) into a single value, so that we have a list of `key=vendor; values=VendorScore` tuples.

At this point the `cata` function will return a `VendorScore list`. To get the highest score, use `List.sortByDescending` then `List.tryHead`. Note that `maxBy` won't work because the list could be empty.

Here's the complete `mostUsedVendor` function:

```fsharp
let mostUsedVendor product =

    let fBought (bought:BoughtProduct) = 
        // set score = 1 if there is a vendor
        bought.vendor
        |> Option.map (fun vendor -> {vendor = vendor; score = 1} )
        // => a VendorScore option
        |> Option.toList
        // => a VendorScore list

    let fMade (name,weight,subresults) = 
        // subresults are a list of (qty * VendorScore list)

        // helper to get sum of scores
        let totalScore (vendor,vendorScores) =
            let totalScore = vendorScores |> List.sumBy score
            {vendor=vendor; score=totalScore}

        subresults 
        // => a list of (qty * VendorScore list)
        |> List.collect snd  // ignore qty part of subresult
        // => a list of VendorScore 
        |> List.groupBy vendor 
        // second item is list of VendorScore, reduce to sum
        |> List.map totalScore 
        // => list of VendorScores 

    // call the catamorphism
    cataProduct fBought fMade product
    |> List.sortByDescending score  // find highest score
    // return first, or None if list is empty
    |> List.tryHead
```

Now let's test it:

```fsharp
label |> mostUsedVendor    
// Some {vendor = "ACME"; score = 1}

formulation |> mostUsedVendor  
// None

shampoo |> mostUsedVendor  
// Some {vendor = "ACME"; score = 2}

twoPack |> mostUsedVendor  
// Some {vendor = "ACME"; score = 2}
```

  
This isn't the only possible implementation of `fMade`, of course. I could have used `List.fold` and done the whole thing in one pass,
but this version seems like the most obvious and readable implementation.

It's also true that I could have avoided using `cataProduct` altogether and written `mostUsedVendor` from scratch. If performance is an issue,
then that might be a better approach, because the generic catamorphism creates intermediate values (such as the list of `qty * VendorScore option`)
which are over general and potentially wasteful.

On other hand, by using the catamorphism, I could focus on the counting logic only and ignore the recursion logic.

So as always, you should consider the pros and cons of reuse vs. creating from scratch; the benefits of writing common code once and using it in a standardized way, versus
the performance but extra effort (and potential bugginess) of custom code.
    
<hr>
    
## Summary 

We've seen in this post how to define a recursive type, and been introduced to catamorphisms.

And we have also seen some uses for catamorphisms:

* Any function that "collapses" a recursive type, such as `Gift -> 'r`, can be written in terms of the catamorphism for that type.
* Catamorphisms can be used to hide the internal structure of the type.
* Catamorphisms can be used to create mappings from one type to another by tweaking the functions that handle each case.
* Catamorphisms can be used to create a clone of the original value by passing in the type's case constructors.

But all is not perfect in the land of catamorphisms. In fact, all the catamorphism implementations on this page have a potentially serious flaw.

In the [next post](/posts/recursive-types-and-folds-2/)
we'll see what can go wrong with them, how to fix them, and in the process look at the various kinds of "fold".

See you then!

*The source code for this post is available at [this gist](https://gist.github.com/swlaschin/dc2b3fcdca319ca8be60).*

*UPDATE: Fixed logic error in `mostUsedVendor` as pointed out by Paul Schnapp in comments. Thanks, Paul!*
