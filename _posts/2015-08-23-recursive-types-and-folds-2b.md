---
layout: post
title: "Understanding Folds"
description: "Recursion vs. iteration"
seriesId: "Recursive types and folds"
seriesOrder: 4
categories: [Folds, Patterns]
---

This post is the fourth in a series.

In the [previous post](/posts/recursive-types-and-folds/), I introduced "folds", a way of creating top-down iterative functions for recursive types.

In this post, we'll spend some time understanding folds in more detail.

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

<a id="iteration"></a>
<hr>

## Iteration vs. recursion

We now have *three* different functions -- `cata`, `fold` and `foldback`.

So what exactly are the differences between them? We've seen that something that doesn't work with `fold` will work with `foldBack`,
but is there an easy way to remember the difference?

One way to differentiate the three approaches is by remembering this:

* `fold` is top-down *iteration*
* `cata` is bottom-up *recursion*
* `foldBack` is bottom-up *iteration*

What does this mean?

Well, for in `fold`, the accumulator was initialized at the top level, and was passed down to each lower level until the lowest and last level was reached.

In code terms, each level did this:

```text
accumulatorFromHigherLevel, combined with 
  stuffFromThisLevel 
    => stuffToSendDownToNextLowerLevel
```

In an imperative language, this is exactly a "for loop" with a mutable variable storing the accumulator.

```fsharp
var accumulator = initialValue
foreach level in levels do
{
  accumulator, combined with 
    stuffFromThisLevel 
      => update accumulator
}
```

So, this kind of top-to-bottom folding can be thought of as iteration (and in fact, the F# compiler will turn a tail-recursive function like this into an iteration behind the scenes).

On the other hand, in `cata`, the accumulator started at the bottom level, and was passed up to each higher level until the top level was reached.

In code terms, each level did this:

```text
accumulatorFromLowerLevel, combined with 
  stuffFromThisLevel 
    => stuffToSendUpToNextHigherLevel
```

This is exactly a recursive loop:
  
```fsharp
let recurse (head::tail) =
    if atBottomLevel then
       return something
    else    // if not at bottom level
       let accumulatorFromLowerLevel = recurse tail
       return stuffFromThisLevel, combined with 
          accumulatorFromLowerLevel
```

Finally, `foldback` can be thought of as "reverse iteration". The accumulator is threaded through all the levels, but starting at the
bottom rather than at the top. It has the benefits of `cata` in that the inner values are calculated first and passed back up, but because it
is iterative, there cannot be a stack overflow.

Many of the concepts we have discussed so far become clear when expressed in terms of iteration vs. recursion. For example:

* The iterative versions (`fold` and `foldback`) have no stack, and cannot cause a stack overflow.
* The "total cost" function needed no inner data, and so the top-down iterative version (`fold`) worked without problems.
* The "description" function though, needed inner text for correct formatting, and so the recursive version (`cata`) or bottom up iteration (`foldback`) was more suitable.

<a id="file-system"></a>

## Fold example: File system domain

In the last post, we described some rules for creating folds.
Let's see if we can apply these rules to create a fold in another domain,
the "File System" domain from the [second post in the series](/posts/recursive-types-and-folds-1b/#file-system).

As a reminder, here is the crude "file system" domain from that post:

```fsharp
type FileSystemItem =
    | File of File
    | Directory of Directory
and File = {name:string; fileSize:int}
and Directory = {name:string; dirSize:int; subitems:FileSystemItem list}
```

Note that each directory contains a *list* of subitems, so this is not a linear structure like `Gift`, but a tree-like structure.
Out implementation of fold will have to take this into account.

Here are some sample values:

```fsharp
let readme = File {name="readme.txt"; fileSize=1}
let config = File {name="config.xml"; fileSize=2}
let build  = File {name="build.bat"; fileSize=3}
let src = Directory {name="src"; dirSize=10; subitems=[readme; config; build]}
let bin = Directory {name="bin"; dirSize=10; subitems=[]}
let root = Directory {name="root"; dirSize=5; subitems=[src; bin]}
```

We want to create a fold, `foldFS`, say.
So, following the rules, let's add an extra accumulator parameter `acc` and pass it to the `File` case:

```fsharp
let rec foldFS fFile fDir acc item :'r = 
    let recurse = foldFS fFile fDir 
    match item with
    | File file -> 
        fFile acc file
    | Directory dir -> 
        // to do
```

The `Directory` case is trickier. We are not supposed to know about the subitems, so that means that the only data we can use
is the `name`, `dirSize`, and the accumulator passed in from a higher level. These are combined to make a new accumulator.

```fsharp
| Directory dir -> 
    let newAcc = fDir acc (dir.name,dir.dirSize) 
    // to do
```

*NOTE: I'm keeping the `name` and `dirSize` as a tuple for grouping purposes, but of course you could pass them in as separate parameters.*

Now we need to pass this new accumulator down to each subitem in turn, but each subitem will return a new accumulator of its own,
so we need to use the following approach:

* Take the newly created accumulator and pass it to the first subitem.
* Take the output of that (another accumulator) and pass it to the second subitem.
* Take the output of that (another accumulator) and pass it to the third subitem.
* And so on. The output of the last subitem is the final result.

That approach is already available to us though. It's exactly what `List.fold` does!  So here's the code for the Directory case:

```fsharp
| Directory dir -> 
    let newAcc = fDir acc (dir.name,dir.dirSize) 
    dir.subitems |> List.fold recurse newAcc 
```

And here's the entire `foldFS` function:

```fsharp
let rec foldFS fFile fDir acc item :'r = 
    let recurse = foldFS fFile fDir 
    match item with
    | File file -> 
        fFile acc file
    | Directory dir -> 
        let newAcc = fDir acc (dir.name,dir.dirSize) 
        dir.subitems |> List.fold recurse newAcc 
```

With this in place, we can rewrite the same two functions we implemented in the last post.

First, the `totalSize` function, which just sums up all the sizes:

```fsharp
let totalSize fileSystemItem =
    let fFile acc (file:File) = 
        acc + file.fileSize
    let fDir acc (name,size) = 
        acc + size
    foldFS fFile fDir 0 fileSystemItem 
```

And if we test it we get the same results as before:

```fsharp
readme |> totalSize  // 1
src |> totalSize     // 16 = 10 + (1 + 2 + 3)
root |> totalSize    // 31 = 5 + 16 + 10
```

### File system domain: `largestFile` example

We can also reimplement the "what is the largest file in the tree?" function.

As before it will return a `File option`, because the tree might be empty.  This means that the accumulator will be a `File option` too.

This time it is the `File` case handler which is tricky:

* If the accumulator being passed in is `None`, then this current file becomes the new accumulator.
* If the accumulator being passed in is `Some file`, then compare the size of that file with this file. Whichever is bigger becomes the new accumulator.

Here's the code:

```fsharp
let fFile (largestSoFarOpt:File option) (file:File) = 
    match largestSoFarOpt with
    | None -> 
        Some file                
    | Some largestSoFar -> 
        if largestSoFar.fileSize > file.fileSize then
            Some largestSoFar
        else
            Some file
```

On the other hand, the `Directory` handler is trivial -- just pass the "largest so far" accumulator down to the next level

```fsharp
let fDir largestSoFarOpt (name,size) = 
    largestSoFarOpt
```

Here's the complete implementation:

```fsharp
let largestFile fileSystemItem =
    let fFile (largestSoFarOpt:File option) (file:File) = 
        match largestSoFarOpt with
        | None -> 
            Some file                
        | Some largestSoFar -> 
            if largestSoFar.fileSize > file.fileSize then
                Some largestSoFar
            else
                Some file

    let fDir largestSoFarOpt (name,size) = 
        largestSoFarOpt

    // call the fold
    foldFS fFile fDir None fileSystemItem
```

And if we test it we get the same results as before:

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

It is interesting to compare this implementation with the [recursive version in the second post](/posts/recursive-types-and-folds-1b/#file-system).
I think that this one is easier to implement, myself.

### Tree traversal types

The various fold functions discussed so far correspond to various kinds of tree traversals:

* A `fold` function (as implemented here) is more properly called a "pre-order depth-first" tree traversal. 
* A `foldback` function would be a "post-order depth-first" tree traversal. 
* A `cata` function would not be a "traversal" at all, because each internal node deals with a list of all the subresults at once. 

By tweaking the logic, you can make other variants.

For a description of the various kinds of tree traversals, see [Wikipedia](https://en.wikipedia.org/wiki/Tree_traversal).

### Do we need `foldback`?

Do we need to implement a `foldback` function for the FileSystem domain?  

I don't think so. If we need access to the inner data, we can just use the original "naive" catamorphism implementation in the previous post.

But, hey wait, didn't I say at the beginning that we had to watch out for stack overflows?  

Yes, if the recursive type is deeply nested. But consider a file system with only two subdirectories per directory. How many directories would there
be if there were 64 nested levels? (Hint: you may be familiar with a similar problem. Something to do with [grains on a chessboard](https://en.wikipedia.org/wiki/Wheat_and_chessboard_problem)).

We saw earlier that the stack overflow issue only occurs with more than 1000 nested levels, and that level of nesting generally only occurs with *linear* recursive types, not trees
like the FileSystem domain.  


<a id="questions"></a>

## Common questions about "fold"

At this point you might be getting overwhelmed! All these different implementations with different advantages and disadvantages. 

So let's take a short break and address some common questions.

### What's the difference between "left fold" and "right fold"

There is often quite a lot of confusion around the terminology of folds: "left" vs. "right", "forward" vs. "backwards", etc.

* A *left fold* or *forward fold* is what I have just called `fold` -- the top-down iterative approach.
* A *right fold* or *backward fold* is what I have called `foldBack` -- the bottom-up iterative approach.

These terms, though, really only apply to linear recursive structures like `Gift`.
When it comes to more complex tree-like structures, these distinctions are too simple,
because there are many ways to traverse them: breadth-first, depth-first, pre-order and post-order, and so on. 

### Which type of fold function should I use?

Here are some guidelines:

* If your recursive type is not going to be too deeply nested (less than 100 levels deep, say), then the naive `cata` catamorphism we described
  in the first post is fine. It's really easy to implement mechanically -- just replace the main recursive type with `'r`. 
* If your recursive type is going to be deeply nested and you want to prevent stack overflows, use the iterative `fold`.
* If you are using an iterative fold but you need to have access to the inner data, pass a continuation function as an accumulator.
* Finally, the iterative approach is generally faster and uses less memory than the recursive approach (but that advantage is lost if you pass around too many nested continuations).
  
Another way to think about it is to look at your "combiner" function. At each step, you are combining data from the different levels:

```text
level1 data [combined with] level2 data [combined with] level3 data [combined with] level4 data
```

If your combiner function is "left associative" like this:

```text
(((level1 combineWith level2) combineWith level3) combineWith level4)
```

then use the iterative approach, but if your combiner function is "right associative" like this:

```text
(level1 combineWith (level2 combineWith (level3 combineWith level4)))
```

then use the `cata` or `foldback` approach.  

And if your combiner function doesn't care (like addition, for example), use whichever one is more convenient.

### How can I know whether code is tail-recursive or not?

It's not always obvious whether an implementation is tail-recursive or not. The easiest way to be sure is to look at the very last expression for each case.

If the call to "recurse" is the very last expression, then it is tail-recursive. If there is any other work after that, then it is not tail-recursive. 

See for yourself with the three implementations that we have discussed.

First, here's the code for the `WithACard` case in the original `cataGift` implementation:

```fsharp
| WithACard (gift,message) -> 
    fCard (recurse gift,message) 
//         ~~~~~~~  <= Call to recurse is not last expression.
//                     Tail-recursive? No!
```

The `cataGift` implementation is *not* tail-recursive.

Here's the code from the `foldGift` implementation:

```fsharp
| WithACard (innerGift,message) -> 
    let newAcc = fCard acc message 
    recurse newAcc innerGift
//  ~~~~~~~  <= Call to recurse is last expression.
//              Tail-recursive? Yes!
```

The `foldGift` implementation *is* tail-recursive.

And here's the code from the `foldbackGift` implementation:

```fsharp
| WithACard (innerGift,message) -> 
    let newGenerator innerVal =
        let newInnerVal = fCard innerVal message 
        generator newInnerVal 
    recurse newGenerator innerGift 
//  ~~~~~~~  <= Call to recurse is last expression.
//              Tail-recursive? Yes!
```

The `foldbackGift` implementation is also tail-recursive.

### How do I short-circuit a fold?

In a language like C#, you can exit a iterative loop early using `break` like this:

```csharp
foreach (var elem in collection)
{
    // do something
    
    if ( x == "error")
    {
        break;
    }
}
```

So how do you do the same thing with a fold?

The short answer is, you can't!  A fold is designed to visit all elements in turn. The Visitor Pattern has the same constraint.

There are three workarounds.

The first one is to not use `fold` at all and create your own recursive function that terminates on the required condition:

In this example, the loop exits when the sum is larger than 100:

```fsharp
let rec firstSumBiggerThan100 sumSoFar listOfInts =
    match listOfInts with
    | [] -> 
        sumSoFar // exhausted all the ints!
    | head::tail -> 
        let newSumSoFar = head + sumSoFar 
        if newSumSoFar > 100 then
            newSumSoFar 
        else
            firstSumBiggerThan100 newSumSoFar tail

// test
[30;40;50;60] |> firstSumBiggerThan100 0  // 120
[1..3..100] |> firstSumBiggerThan100 0  // 117
```

The second approach is to use `fold` but to add some kind of "ignore" flag to the accumulator that is passed around.
Once this flag is set, the remaining iterations do nothing.

Here's an example of calculating the sum, but the accumulator is actually a tuple with an `ignoreFlag` in addition to the `sumSoFar`:

```fsharp
let firstSumBiggerThan100 listOfInts =

    let folder accumulator i =
        let (ignoreFlag,sumSoFar) = accumulator
        if not ignoreFlag then
            let newSumSoFar = i + sumSoFar 
            let newIgnoreFlag  = newSumSoFar > 100 
            (newIgnoreFlag, newSumSoFar)
        else
            // pass the accumulator along
            accumulator 

    let initialAcc = (false,0)

    listOfInts 
    |> List.fold folder initialAcc  // use fold
    |> snd // get the sumSoFar

/// test    
[30;40;50;60] |> firstSumBiggerThan100  // 120
[1..3..100] |> firstSumBiggerThan100  // 117
```

The third version is a variant of the second -- create a special value to signal that the remaining data should be ignored, but wrap it in
a computation expression so that it looks more natural.

This approach is documented on [Tomas Petricek's blog](http://tomasp.net/blog/imperative-ii-break.aspx/) and the code looks like this:

```fsharp
let firstSumBiggerThan100 listOfInts =
    let mutable sumSoFar = 0
    imperative { 
        for x in listOfInts do 
            sumSoFar <- x + sumSoFar 
            if sumSoFar > 100 then do! break
    }
    sumSoFar
```

<hr>
    
## Summary 

The goal of this post was to help you understand folds better, and to show how they could be applied to a tree structure like the file system.
I hope it was helpful!

Up to this point in the series all the examples have been very concrete; we have implemented custom folds for each domain we have encountered.
Can we be a bit more generic and build some reusable fold implementations?

In the [next post](/posts/recursive-types-and-folds-3/) we'll look at generic recursive types, and how to work with them.

*The source code for this post is available at [this gist](https://gist.github.com/swlaschin/e065b0e99dd68cd35846).*




