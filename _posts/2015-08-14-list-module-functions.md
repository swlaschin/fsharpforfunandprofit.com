---
layout: post
title: "Choosing between collection functions"
description: "A guide for the perplexed"
categories: []
---

There's more to learning a new language than the language itself. In order to be productive, you need to memorize a big chunk of the standard library
and be aware of most of the rest of it. For example, if you know C#, you can pick up Java-the-language quite quickly, but you won't really get up to speed
until you are comfortable with the Java Class Library as well.

Similarly, you can't really be effective in F# until you have some familiarity with all the F# functions that work with collections.

In C# there are only a few LINQ methods you need to know<sup>1</sup> (`Select`, `Where`, and so on).
But in F#, there are currently almost 100 functions in the List module (and similar counts in the Seq and Array modules). That's a lot!
 
<small><sup>1</sup> Yes, there are more, but you can get by with just a few. In F# it's more important to know them all.<small> 

If you are coming to F# from C#, then, the large number of list functions can be overwhelming.

So I have written this post to help guide you to the one you want.
And for fun, I've done it in a "Choose Your Own Adventure" style!
 
![](/assets/img/cyoa_list_module.jpg) 
 
## What collection do I want?

First, a table with information about the different kinds of standard collections. There are five "native" F# ones: `list`, `seq`, `array`, `map` and `set`,
and `ResizeArray` and `IDictionary` are also often used.

<table class="table table-condensed table-striped">
<tr>
<th></th>
<th>Immutable?</th>
<th>Notes</th>
</tr>
<tr>
<th>list</th>
<td>Yes</td>
<td>
    <b>Pros:</b>
    <ul>
    <li>Pattern matching available.</li>
    <li>Complex iteration available via recursion.</li>
    <li>Forward iteration is fast. Prepending is fast.</li>
    </ul>
    <b>Cons:</b>
    <ul>
    <li>Indexed access and other access styles are slow.</li>
    </ul>
</td>
</tr>
<tr>
<th>seq</th>
<td>Yes</td>
<td>
    <p>Alias for <code>IEnumerable</code>.</p>
    <b>Pros:</b>
    <ul>
    <li>Lazy evaluation</li>
    <li>Memory efficient (only one element at a time loaded)</li>
    <li>Can represent an infinite sequence.</li>
    <li>Interop with .NET libraries that use IEnumerable.</li>
    </ul>
    <b>Cons:</b>
    <ul>
    <li>No pattern matching.</li>
    <li>Forward only iteration.</li>
    <li>Indexed access and other access styles are slow.</li>
    </ul>
</td>
</tr>
<tr>
<th>array</th>
<td>No</td>
<td>
    <p>Same as BCL <code>Array</code>.</p>
    <b>Pros:</b>
    <ul>
    <li>Fast random access</li>
    <li>Memory efficient and cache locality, especially with structs.</li>
    <li>Interop with .NET libraries that use Array.</li>
    <li>Support for 2D, 3D and 4D arrays</li>
    </ul>
    <b>Cons:</b>
    <ul>
    <li>Limited pattern matching.</li>
    <li>Not <a href="https://en.wikipedia.org/wiki/Persistent_data_structure">persistent</a>.</li>
    </ul>
</td>
</tr>
<tr>
<th>map</th>
<td>Yes</td>
<td>Immutable dictionary. Requires keys to implement <code>IComparable</code>.</td>
</tr>
<tr>
<th>set</th>
<td>Yes</td>
<td>Immutable set. Requires elements to implement <code>IComparable</code>.</td>
</tr>
<tr>
<th>ResizeArray</th>
<td>No</td>
<td>Alias for BCL <code>List</code>. Pros and cons similar to array, but resizable.</td>
</tr>
<tr>
<th>IDictionary</th>
<td>Yes</td>
<td>
    <p>For an alternate dictionary that does not requires elements to implement <code>IComparable</code>,
    you can use the BCL <a href="https://msdn.microsoft.com/en-us/library/s4ys34ea.aspx">IDictionary</a>.
    The constructor is <a href="https://msdn.microsoft.com/en-us/library/ee353774.aspx"><code>dict</code></a> in F#.</p>
    <p>Note that mutation methods such as <code>Add</code> are present, but will cause a runtime error if called.</p>
</td>	
</tr>
</table>



These are the main collection types that you will encounter in F#, and will be good enough for all common cases.

If you need other kinds of collections though, there are lots of choices:

* You can use the collection classes in .NET, either the [traditional, mutable ones](https://msdn.microsoft.com/en-us/library/system.collections.generic)
  or the newer ones such as those in the [System.Collections.Immutable namespace](https://msdn.microsoft.com/en-us/library/system.collections.immutable.aspx ).
* Alternatively, you can use one of the F# collection libraries:  
  * [**FSharpx.Collections**](https://fsprojects.github.io/FSharpx.Collections/), part of the FSharpx series of projects.
  * [**ExtCore**](https://github.com/jack-pappas/ExtCore/tree/master/ExtCore). Some of these are drop-in (almost) replacements for the Map and Set types in FSharp.Core which provide improved performance in specific scenarios (e.g., HashMap). Others provide unique functionality to help tackle specific coding tasks (e.g., LazyList and LruCache).
  * [**Funq**](https://github.com/GregRos/Funq): high performance, immutable data structures for .NET.
  * [**Persistent**](https://persistent.codeplex.com/documentation): some efficient persistent (immutable) data structures.

## About the documentation 

All functions are available for `list`, `seq` and `array` in F# v4 unless noted. The `Map` and `Set` modules have some of them as well, but I won't be discussing `map` and `set` here.

For the function signatures I will use `list` as the standard collection type. The signatures for the `seq` and `array` versions will be similar.

Many of these functions are not yet documented on MSDN so I'm going to link directly to the source code on GitHub, which has the up-to-date comments. 
Click on the function name for the link.

## Note on availability
 
The availability of these functions may depend on which version of F# you use.

* In F# version 3 (Visual Studio 2013), there was some degree of inconsistency between Lists, Arrays and Sequences.
* In F# version 4 (Visual Studio 2015), this inconsistency has been eliminated, and almost all functions are available for all three collection types.

If you want to know what changed between F# v3 and F# v4, please see [this chart](http://blogs.msdn.com/cfs-filesystemfile.ashx/__key/communityserver-blogs-components-weblogfiles/00-00-01-39-71-metablogapi/3125.collectionAPI_5F00_254EA354.png)
(from [here](http://blogs.msdn.com/b/fsharpteam/archive/2014/11/12/announcing-a-preview-of-f-4-0-and-the-visual-f-tools-in-vs-2015.aspx)).
The chart shows the new APIs in F# v4 (green), previously-existing APIs (blue), and intentional remaining gaps (white). 

Some of the functions documented below are not in this chart -- these are newer still! If you are using an older version of F#,
you can simply reimplement them yourself using the code on GitHub.
 
With that disclaimer out of the way, you can start your adventure!
 
<a id="toc"></a> 
<hr>  
## Table of contents

* [1. What kind of collection do you have?](#1)
* [2. Creating a new collection](#2)
* [3. Creating a new empty or one-element collection](#3)
* [4. Creating a new collection of known size](#4)
* [5. Creating a new collection of known size with each element having the same value](#5)
* [6. Creating a new collection of known size with each element having a different value](#6)
* [7. Creating a new infinite collection](#7)
* [8. Creating a new collection of indefinite size](#8)
* [9. Working with one list](#9)
* [10. Getting an element at a known position](#10)
* [11. Getting an element by searching](#11)
* [12. Getting a subset of elements from a collection](#12)
* [13. Partitioning, chunking and grouping](#13)
* [14. Aggregating or summarizing a collection](#14)
* [15. Changing the order of the elements](#15)
* [16. Testing the elements of a collection](#16)
* [17. Transforming each element to something different](#17)
* [18. Iterating over each element](#18)
* [19. Threading state through an iteration](#19)
* [20. Working with the index of each element](#20)
* [21. Transforming the whole collection to a different collection type](#21)
* [22. Changing the behavior of the collection as a whole](#22)
* [23. Working with two collections](#23)
* [24. Working with three collections](#24)
* [25. Working with more than three collections](#25)
* [26. Combining and uncombining collections](#26)
* [27. Other array-only functions](#27)
* [28. Using sequences with disposables](#28)

<a id="1"></a> 
<hr>  
## 1. What kind of collection do you have?

What kind of collection do you have?

* If you don't have a collection, and want to create one, go to [section 2](#2).
* If you already have a collection that you want to work with, go to [section 9](#9).
* If you have two collections that you want to work with, go to [section 23](#23).
* If you have three collections that you want to work with, go to [section 24](#24).
* If you have more than three collections that you want to work with, go to [section 25](#25).
* If you want to combine or uncombine collections, go to [section 26](#26).

<a id="2"></a> 
<hr>  
## 2. Creating a new collection

So you want to create a new collection. How do you want to create it?

* If the new collection will be empty or will have one element, go to [section 3](#3).
* If the new collection is a known size, go to [section 4](#4).
* If the new collection is potentially infinite, go to [section 7](#7).
* If you don't know how big the collection will be, go to [section 8](#8).

<a id="3"></a> 
<hr>  
## 3. Creating a new empty or one-element collection 

If you want to create a new empty or one-element collection, use these functions:

* [`empty : 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L142).
  Returns an empty list of the given type. 
* [`singleton : value:'T -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L635).
  Returns a list that contains one item only.

If you know the size of the collection in advance, it is generally more efficient to use a different function. See [section 4](#4) below.

### Usage examples

```fsharp
let list0 = List.empty
// list0 = []

let list1 = List.singleton "hello"
// list1 = ["hello"]
```


<a id="4"></a> 
<hr>  
## 4. Creating a new collection of known size

* If all elements of the collection will have the same value, go to [section 5](#5).
* If elements of the collection could be different, go to [section 6](#6).


<a id="5"></a> 
<hr>  
## 5. Creating a new collection of known size with each element having the same value

If you want to create a new collection of known size with each element having the same value, you want to use `replicate`:

* [`replicate : count:int -> initial:'T -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L602).
  Creates a collection by replicating the given initial value. 
* (Array only) [`create : count:int -> value:'T -> 'T[]`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/array.fsi#L125). 
  Creates an array whose elements are all initially the supplied value. 
* (Array only) [`zeroCreate : count:int -> 'T[]`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/array.fsi#L467).
  Creates an array where the entries are initially the default value. 

`Array.create` is basically the same as `replicate` (although with a subtly different implementation!) but `replicate` was only implemented for `Array` in F# v4.

### Usage examples

```fsharp
let repl = List.replicate 3 "hello"
// val repl : string list = ["hello"; "hello"; "hello"]

let arrCreate = Array.create 3 "hello"
// val arrCreate : string [] = [|"hello"; "hello"; "hello"|]

let intArr0 : int[] = Array.zeroCreate 3
// val intArr0 : int [] = [|0; 0; 0|]

let stringArr0 : string[] = Array.zeroCreate 3
// val stringArr0 : string [] = [|null; null; null|]
```

Note that for `zeroCreate`, the target type must be known to the compiler.


<a id="6"></a> 
<hr>  
## 6. Creating a new collection of known size with each element having a different value

If you want to create a new collection of known size with each element having a potentially different value, you can choose one of three ways:

* [`init : length:int -> initializer:(int -> 'T) -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L347).
  Creates a collection by calling the given generator on each index.
* For lists and arrays, you can also use the literal syntax such as `[1; 2; 3]` (lists) and `[|1; 2; 3|]` (arrays).
* For lists and arrays and seqs, you can use the comprehension syntax `for .. in .. do .. yield`.

### Usage examples

```fsharp
// using list initializer
let listInit1 = List.init 5 (fun i-> i*i)
// val listInit1 : int list = [0; 1; 4; 9; 16]

// using list comprehension
let listInit2 = [for i in [1..5] do yield i*i]
// val listInit2 : int list = [1; 4; 9; 16; 25]

// literal 
let listInit3 = [1; 4; 9; 16; 25]
// val listInit3 : int list = [1; 4; 9; 16; 25]

let arrayInit3 = [|1; 4; 9; 16; 25|]
// val arrayInit3 : int [] = [|1; 4; 9; 16; 25|]
```

Literal syntax allows for an increment as well:

```fsharp
// literal with +2 increment
let listOdd= [1..2..10]
// val listOdd : int list = [1; 3; 5; 7; 9]
```

The comprehension syntax is even more flexible because you can `yield` more than once:

```fsharp
// using list comprehension
let listFunny = [
    for i in [2..3] do 
        yield i
        yield i*i
        yield i*i*i
        ]
// val listFunny : int list = [2; 4; 8; 3; 9; 27]
```

and it can also be used as a quick and dirty inline filter:

```fsharp
let primesUpTo n = 
   let rec sieve l  = 
      match l with 
      | [] -> []
      | p::xs -> 
            p :: sieve [for x in xs do if (x % p) > 0 then yield x]
   [2..n] |> sieve 

primesUpTo 20
// [2; 3; 5; 7; 11; 13; 17; 19]
```

Two other tricks: 

* You can use `yield!` to return a list rather than a single value
* You can also use recursion 

Here is an example of both tricks being used to count up to 10 by twos:

```fsharp
let rec listCounter n = [
    if n <= 10 then
        yield n
        yield! listCounter (n+2)
    ]

listCounter 3
// val it : int list = [3; 5; 7; 9]
listCounter 4
// val it : int list = [4; 6; 8; 10]
```

<a id="7"></a> 
<hr>  
## 7. Creating a new infinite collection

If you want an infinite list, you have to use a seq rather than a list or array.  

* [`initInfinite : initializer:(int -> 'T) -> seq<'T>`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/seq.fsi#L599).
  Generates a new sequence which, when iterated, will return successive elements by calling the given function. 
* You can also use a seq comprehension with a recursive loop to generate an infinite sequence.

### Usage examples

```fsharp
// generator version
let seqOfSquares = Seq.initInfinite (fun i -> i*i)
let firstTenSquares = seqOfSquares |> Seq.take 10

firstTenSquares |> List.ofSeq // [0; 1; 4; 9; 16; 25; 36; 49; 64; 81]

// recursive version
let seqOfSquares_v2 = 
    let rec loop n = seq {
        yield n * n
        yield! loop (n+1)
        }
    loop 1
let firstTenSquares_v2 = seqOfSquares_v2 |> Seq.take 10 
```

<a id="8"></a> 
<hr>  
## 8. Creating a new collection of indefinite size

Sometimes you don't know how big the collection will be in advance. In this case you need a function that will keep adding elements until it gets a signal to stop.
`unfold` is your friend here, and the "signal to stop" is whether you return a `None` (stop) or a `Some` (keep going).

* [`unfold : generator:('State -> ('T * 'State) option) -> state:'State -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L846).
  Returns a collection that contains the elements generated by the given computation.

### Usage examples

This example reads from the console in a loop until an empty line is entered:

```fsharp
let getInputFromConsole lineNo =
    let text = System.Console.ReadLine()
    if System.String.IsNullOrEmpty(text) then
        None
    else
        // return value and new threaded state
        // "text" will be in the generated sequence
        Some (text,lineNo+1)

let listUnfold = List.unfold getInputFromConsole 1
```

`unfold` requires that a state be threaded through the generator. You can ignore it (as in the `ReadLine` example above), or you can
use it to keep track of what you have done so far. For example, you can create a Fibonacci series generator using `unfold`:

```fsharp
let fibonacciUnfolder max (f1,f2)  =
    if f1 > max then
        None
    else
        // return value and new threaded state
        let fNext = f1 + f2
        let newState = (f2,fNext)
        // f1 will be in the generated sequence
        Some (f1,newState)

let fibonacci max = List.unfold (fibonacciUnfolder max) (1,1)
fibonacci 100
// int list = [1; 1; 2; 3; 5; 8; 13; 21; 34; 55; 89]
```

<a id="9"></a> 
<hr>  
## 9. Working with one list

If you are working with one list and...

* If you want to get an element at a known position, go to [section 10](#10)
* If you want to get one element by searching, go to [section 11](#11)
* If you want to get a subset of the collection, go to [section 12](#12)
* If you want to partition, chunk, or group a collection into smaller collections, go to [section 13](#13)
* If you want to aggregate or summarize the collection into a single value, go to [section 14](#14)
* If you want to change the order of the elements, go to [section 15](#15)
* If you want to test the elements in the collection, go to [section 16](#16)
* If you want to transform each element to something different, go to [section 17](#17)
* If you want to iterate over each element, go to [section 18](#18)
* If you want to thread state through an iteration, go to [section 19](#19)
* If you need to know the index of each element while you are iterating or mapping, go to [section 20](#20)
* If you want to transform the whole collection to a different collection type, go to [section 21](#21)
* If you want to change the behaviour of the collection as a whole, go to [section 22](#22)
* If you want to mutate the collection in place, go to [section 27](#27)
* If you want to use a lazy  collection with an IDisposable, go to [section 28](#28)

<a id="10"></a> 
<hr>  
## 10. Getting an element at a known position

The following functions get a element in the collection by position:

* [`head : list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L333).
  Returns the first element of the collection.
* [`last : list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L398).
  Returns the last element of the collection.
* [`item : index:int -> list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L520).
  Indexes into the collection. The first element has index 0.<br>
  NOTE: Avoid using `nth` and `item` for lists and sequences. They are not designed for random access, and so they will be slow in general.  
* [`nth : list:'T list -> index:int -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L520). 
  The older version of `item`. NOTE: Deprecated in v4 -- use `item` instead.
* (Array only) [`get : array:'T[] -> index:int -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/array.fsi#L220).
  Yet another version  of `item`.
* [`exactlyOne : list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L165).
  Returns the only element of the collection.

But what if the collection is empty? Then `head` and `last` will fail with an exception (ArgumentException).

And if the index is not found in the collection? Then another exception again (ArgumentException for lists, IndexOutOfRangeException for arrays). 

I would therefore recommend that you avoid these functions in general and use the `tryXXX` equivalents below:

* [`tryHead : list:'T list -> 'T option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L775).
  Returns the first element of the collection, or None if the collection is empty.
* [`tryLast : list:'T list -> 'T option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L411).
  Returns the last element of the collection, or None if the collection is empty.
* [`tryItem : index:int -> list:'T list -> 'T option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L827).
  Indexes into the collection, or None if the index is not valid.

### Usage examples

```fsharp
let head = [1;2;3] |> List.head
// val head : int = 1

let badHead : int = [] |> List.head
// System.ArgumentException: The input list was empty.

let goodHeadOpt = 
    [1;2;3] |> List.tryHead 
// val goodHeadOpt : int option = Some 1

let badHeadOpt : int option = 
    [] |> List.tryHead 
// val badHeadOpt : int option = None    

let goodItemOpt = 
    [1;2;3] |> List.tryItem 2
// val goodItemOpt : int option = Some 3

let badItemOpt = 
    [1;2;3] |> List.tryItem 99
// val badItemOpt : int option = None
```

As noted, the `item` function should be avoided for lists. For example, if you want to process each item in a list, and you come from an imperative background,
you might write a loop with something like this:

```fsharp
// Don't do this!
let helloBad = 
    let list = ["a";"b";"c"]
    let listSize = List.length list
    [ for i in [0..listSize-1] do
        let element = list |> List.item i
        yield "hello " + element 
    ]
// val helloBad : string list = ["hello a"; "hello b"; "hello c"]
```

Don't do that! Use something like `map` instead. It's both more concise and more efficient:

```fsharp
let helloGood = 
    let list = ["a";"b";"c"]
    list |> List.map (fun element -> "hello " + element)
// val helloGood : string list = ["hello a"; "hello b"; "hello c"]
```

<a id="11"></a> 
<hr>  
## 11.	Getting an element by searching

You can search for an element or its index using `find` and `findIndex`:

* [`find : predicate:('T -> bool) -> list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L201).
  Returns the first element for which the given function returns true.
* [`findIndex : predicate:('T -> bool) -> list:'T list -> int`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L222).
  Returns the index of the first element for which the given function returns true.

And you can also search backwards:

* [`findBack : predicate:('T -> bool) -> list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L211).
  Returns the last element for which the given function returns true.
* [`findIndexBack : predicate:('T -> bool) -> list:'T list -> int`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L233).
  Returns the index of the last element for which the given function returns true.

But what if the item cannot be found? Then these will fail with an exception (`KeyNotFoundException`).

I would therefore recommend that, as with `head` and `item`, you avoid these functions in general and use the `tryXXX` equivalents below: 

* [`tryFind : predicate:('T -> bool) -> list:'T list -> 'T option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L800).
  Returns the first element for which the given function returns true, or None if no such element exists.
* [`tryFindBack : predicate:('T -> bool) -> list:'T list -> 'T option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L809).
  Returns the last element for which the given function returns true, or None if no such element exists.
* [`tryFindIndex : predicate:('T -> bool) -> list:'T list -> int option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L819).
  Returns the index of the first element for which the given function returns true, or None if no such element exists.
* [`tryFindIndexBack : predicate:('T -> bool) -> list:'T list -> int option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L837).
  Returns the index of the last element for which the given function returns true, or None if no such element exists.

If you are doing a `map` before a `find` you can often combine the two steps into a single one using `pick` (or better, `tryPick`). See below for a usage example.

* [`pick : chooser:('T -> 'U option) -> list:'T list -> 'U`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L561).
  Applies the given function to successive elements, returning the first result where the chooser function returns Some.
* [`tryPick : chooser:('T -> 'U option) -> list:'T list -> 'U option`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L791).
  Applies the given function to successive elements, returning the first result where the chooser function returns Some, or None if no such element exists.


### Usage examples

```fsharp
let listOfTuples = [ (1,"a"); (2,"b"); (3,"b"); (4,"a"); ]

listOfTuples |> List.find ( fun (x,y) -> y = "b")
// (2, "b")

listOfTuples |> List.findBack ( fun (x,y) -> y = "b")
// (3, "b")

listOfTuples |> List.findIndex ( fun (x,y) -> y = "b")
// 1

listOfTuples |> List.findIndexBack ( fun (x,y) -> y = "b")
// 2

listOfTuples |> List.find ( fun (x,y) -> y = "c")
// KeyNotFoundException
```

With `pick`, rather than returning a bool, you return an option:

```fsharp
listOfTuples |> List.pick ( fun (x,y) -> if y = "b" then Some (x,y) else None)
// (2, "b")
```

<a id="pick-vs-find"></a> 
### Pick vs. Find

That 'pick' function might seem unnecessary, but it is useful when dealing with functions that return options.

For example, say that there is a function `tryInt` that parses a string and returns `Some int` if the string is a valid int, otherwise `None`.

```fsharp
// string -> int option
let tryInt str = 
    match System.Int32.TryParse(str) with
    | true, i -> Some i
    | false, _ -> None
```

And now say that we want to find the first valid int in a list. The crude way would be:

* map the list using `tryInt`
* find the first one that is a `Some` using `find`
* get the value from inside the option using `Option.get`

The code might look something like this:

```fsharp
let firstValidNumber = 
    ["a";"2";"three"]
    // map the input
    |> List.map tryInt 
    // find the first Some
    |> List.find (fun opt -> opt.IsSome)
    // get the data from the option
    |> Option.get
// val firstValidNumber : int = 2
```

But `pick` will do all these steps at once! So the code becomes much simpler:

```fsharp
let firstValidNumber = 
    ["a";"2";"three"]
    |> List.pick tryInt 
```

If you want to return many elements in the same way as `pick`, consider using `choose` (see [section 12](#12)).

<a id="12"></a> 
<hr>  
## 12. Getting a subset of elements from a collection

The previous section was about getting one element. How can you get more than one element?  Well you're in luck! There's lots of functions to choose from.

To extract elements from the front, use one of these:

* [`take: count:int -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L746).
  Returns the first N elements of the collection.
* [`takeWhile: predicate:('T -> bool) -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L756).
  Returns a collection that contains all elements of the original collection while the given predicate returns true, and then returns no further elements.
* [`truncate: count:int -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L782).
  Returns at most N elements in a new collection.

To extract elements from the rear, use one of these:

* [`skip: count:int -> list: 'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L644).
  Returns the collection after removing the first N elements.
* [`skipWhile: predicate:('T -> bool) -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L652).
  Bypasses elements in a collection while the given predicate returns true, and then returns the remaining elements of the collection.
* [`tail: list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L730).
  Returns the collection after removing the first element.

To extract other subsets of elements, use one of these:

* [`filter: predicate:('T -> bool) -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L241).
  Returns a new collection containing only the elements of the collection for which the given function returns true.
* [`except: itemsToExclude:seq<'T> -> list:'T list -> 'T list when 'T : equality`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L155).
  Returns a new collection with the distinct elements of the input collection which do not appear in the itemsToExclude sequence, using generic hash and equality comparisons to compare values.
* [`choose: chooser:('T -> 'U option) -> list:'T list -> 'U list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L55).
  Applies the given function to each element of the collection. Returns a collection comprised of the elements where the function returns Some.
* [`where: predicate:('T -> bool) -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L866).
  Returns a new collection containing only the elements of the collection for which the given predicate returns true.
  NOTE: "where" is a synonym for "filter".
* (Array only) `sub : 'T [] -> int -> int -> 'T []`.
  Creates an array that contains the supplied subrange, which is specified by starting index and length.
* You can also use slice syntax: `myArray.[2..5]`. See below for examples.

To reduce the list to distinct elements, use one of these:

* [`distinct: list:'T list -> 'T list when 'T : equality`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L107).
  Returns a collection that contains no duplicate entries according to generic hash and equality comparisons on the entries.
* [`distinctBy: projection:('T -> 'Key) -> list:'T list -> 'T list when 'Key : equality`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L118).
  Returns a collection that contains no duplicate entries according to the generic hash and equality comparisons on the keys returned by the given key-generating function.

### Usage examples
  
Taking elements from the front:

```fsharp
[1..10] |> List.take 3    
// [1; 2; 3]

[1..10] |> List.takeWhile (fun i -> i < 3)    
// [1; 2]

[1..10] |> List.truncate 4
// [1; 2; 3; 4]

[1..2] |> List.take 3    
// System.InvalidOperationException: The input sequence has an insufficient number of elements.

[1..2] |> List.takeWhile (fun i -> i < 3)  
// [1; 2]

[1..2] |> List.truncate 4
// [1; 2]   // no error!
```

Taking elements from the rear:

```fsharp
[1..10] |> List.skip 3    
// [4; 5; 6; 7; 8; 9; 10]

[1..10] |> List.skipWhile (fun i -> i < 3)    
// [3; 4; 5; 6; 7; 8; 9; 10]

[1..10] |> List.tail
// [2; 3; 4; 5; 6; 7; 8; 9; 10]

[1..2] |> List.skip 3    
// System.ArgumentException: The index is outside the legal range.

[1..2] |> List.skipWhile (fun i -> i < 3)  
// []

[1] |> List.tail |> List.tail
// System.ArgumentException: The input list was empty.
```

To extract other subsets of elements:

```fsharp
[1..10] |> List.filter (fun i -> i%2 = 0) // even
// [2; 4; 6; 8; 10]

[1..10] |> List.where (fun i -> i%2 = 0) // even
// [2; 4; 6; 8; 10]

[1..10] |> List.except [3;4;5]
// [1; 2; 6; 7; 8; 9; 10]
```

To extract a slice:

```fsharp
Array.sub [|1..10|] 3 5
// [|4; 5; 6; 7; 8|]

[1..10].[3..5] 
// [4; 5; 6]

[1..10].[3..] 
// [4; 5; 6; 7; 8; 9; 10]

[1..10].[..5] 
// [1; 2; 3; 4; 5; 6]
```

Note that slicing on lists can be slow, because they are not random access. Slicing on arrays is fast however.
  
To extract the distinct elements:
  
```fsharp
[1;1;1;2;3;3] |> List.distinct
// [1; 2; 3]

[ (1,"a"); (1,"b"); (1,"c"); (2,"d")] |> List.distinctBy fst
// [(1, "a"); (2, "d")]
```

  
<a id="choose-vs-fliter"></a> 
### Choose vs. Filter

As with `pick`, the `choose` function might seem awkward, but it is useful when dealing with functions that return options.
  
In fact, `choose` is to `filter` as [`pick` is to `find`](#pick-vs-find), Rather than using a boolean filter, the signal is `Some` vs. `None`.

As before, say that there is a function `tryInt` that parses a string and returns `Some int` if the string is a valid int, otherwise `None`.

```fsharp
// string -> int option
let tryInt str = 
    match System.Int32.TryParse(str) with
    | true, i -> Some i
    | false, _ -> None
```

And now say that we want to find all the valid ints in a list. The crude way would be:

* map the list using `tryInt`
* filter to only include the ones that are `Some` 
* get the value from inside each option using `Option.get`

The code might look something like this:

```fsharp
let allValidNumbers = 
    ["a";"2";"three"; "4"]
    // map the input
    |> List.map tryInt 
    // include only the "Some"
    |> List.filter (fun opt -> opt.IsSome)
    // get the data from each option
    |> List.map Option.get
// val allValidNumbers : int list = [2; 4]
```

But `choose` will do all these steps at once! So the code becomes much simpler:

```fsharp
let allValidNumbers = 
    ["a";"2";"three"; "4"]
    |> List.choose tryInt 
```

If you already have a list of options, you can filter and return the "Some" in one step by passing `id` into `choose`:

```fsharp
let reduceOptions = 
    [None; Some 1; None; Some 2]
    |> List.choose id
// val reduceOptions : int list = [1; 2]
```

If you want to return the first element in the same way as `choose`, consider using `pick` (see [section 11](#11)).

If you want to do a similar action as `choose` but for other wrapper types (such as a Success/Failure result), there is [a discussion here](/posts/elevated-world-5/).
  
<a id="13"></a> 
<hr>  
## 13.	Partitioning, chunking and grouping 

There are lots of different ways to split a collection! Have a look at the usage examples to see the differences:

* [`chunkBySize: chunkSize:int -> list:'T list -> 'T list list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L63).
  Divides the input collection into chunks of size at most `chunkSize`.
* [`groupBy : projection:('T -> 'Key) -> list:'T list -> ('Key * 'T list) list when 'Key : equality`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L325).
  Applies a key-generating function to each element of a collection and yields a list of unique keys. Each unique key contains a list of all elements that match to this key.
* [`pairwise: list:'T list -> ('T * 'T) list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L541).
  Returns a collection of each element in the input collection and its predecessor, with the exception of the first element which is only returned as the predecessor of the second element.
* (Except Seq) [`partition: predicate:('T -> bool) -> list:'T list -> ('T list * 'T list)`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L551).
  Splits the collection into two collections, containing the elements for which the given predicate returns true and false respectively.
* (Except Seq) [`splitAt: index:int -> list:'T list -> ('T list * 'T list)`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L688).
  Splits a collection into two collections at the given index.
* [`splitInto: count:int -> list:'T list -> 'T list list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L137).
  Splits the input collection into at most count chunks.
* [`windowed : windowSize:int -> list:'T list -> 'T list list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L875).
  Returns a list of sliding windows containing elements drawn from the input collection. Each window is returned as a fresh collection.  Unlike `pairwise` the windows are collections,
  not tuples.
  
### Usage examples
  
```fsharp
[1..10] |> List.chunkBySize 3
// [[1; 2; 3]; [4; 5; 6]; [7; 8; 9]; [10]]  
// note that the last chunk has one element

[1..10] |> List.splitInto 3
// [[1; 2; 3; 4]; [5; 6; 7]; [8; 9; 10]]
// note that the first chunk has four elements

['a'..'i'] |> List.splitAt 3
// (['a'; 'b'; 'c'], ['d'; 'e'; 'f'; 'g'; 'h'; 'i'])

['a'..'e'] |> List.pairwise
// [('a', 'b'); ('b', 'c'); ('c', 'd'); ('d', 'e')]

['a'..'e'] |> List.windowed 3
// [['a'; 'b'; 'c']; ['b'; 'c'; 'd']; ['c'; 'd'; 'e']]

let isEven i = (i%2 = 0)
[1..10] |> List.partition isEven 
// ([2; 4; 6; 8; 10], [1; 3; 5; 7; 9])

let firstLetter (str:string) = str.[0]
["apple"; "alice"; "bob"; "carrot"] |> List.groupBy firstLetter 
// [('a', ["apple"; "alice"]); ('b', ["bob"]); ('c', ["carrot"])]  
```

All the functions other than `splitAt` and `pairwise` handle edge cases gracefully:

```fsharp
[1] |> List.chunkBySize 3
// [[1]]

[1] |> List.splitInto 3
// [[1]]

['a'; 'b'] |> List.splitAt 3
// InvalidOperationException: The input sequence has an insufficient number of elements.

['a'] |> List.pairwise
// InvalidOperationException: The input sequence has an insufficient number of elements.

['a'] |> List.windowed 3
// []

[1] |> List.partition isEven 
// ([], [1])

[] |> List.groupBy firstLetter 
//  []
```


<a id="14"></a> 
<hr>  
## 14.	Aggregating or summarizing a collection

The most generic way to aggregate the elements in a collection is to use `reduce`:

* [`reduce : reduction:('T -> 'T -> 'T) -> list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L584).
  Apply a function to each element of the collection, threading an accumulator argument through the computation.
* [`reduceBack : reduction:('T -> 'T -> 'T) -> list:'T list -> 'T`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L595).
  Applies a function to each element of the collection, starting from the end, threading an accumulator argument through the computation.

and there are specific versions of `reduce` for frequently used aggregations:
  
* [`max : list:'T list -> 'T when 'T : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L482).
  Return the greatest of all elements of the collection, compared via Operators.max.
* [`maxBy : projection:('T -> 'U) -> list:'T list -> 'T when 'U : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L492).
  Returns the greatest of all elements of the collection, compared via Operators.max on the function result.
* [`min : list:'T list -> 'T when 'T : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L501).
  Returns the lowest of all elements of the collection, compared via Operators.min.
* [`minBy : projection:('T -> 'U) -> list:'T list -> 'T when 'U : comparison `](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L511).
  Returns the lowest of all elements of the collection, compared via Operators.min on the function result.
* [`sum : list:'T list -> 'T when 'T has static members (+) and Zero`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L711).
  Returns the sum of the elements in the collection.
* [`sumBy : projection:('T -> 'U) -> list:'T list -> 'U when 'U has static members (+) and Zero`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L720).
  Returns the sum of the results generated by applying the function to each element of the collection.
* [`average : list:'T list -> 'T when 'T has static members (+) and Zero and DivideByInt`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L30).
  Returns the average of the elements in the collection.
  Note that a list of ints cannot be averaged -- they must be cast to floats or decimals.
* [`averageBy : projection:('T -> 'U) -> list:'T list -> 'U when 'U has static members (+) and Zero and DivideByInt`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L43).
  Returns the average of the results generated by applying the function to each element of the collection.<br>
  
Finally there are some counting functions:

* [`length: list:'T list -> int`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L404).
  Returns the length of the collection.
* [`countBy : projection:('T -> 'Key) -> list:'T list -> ('Key * int) list when 'Key : equality`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L129).
  Applies a key-generating function to each element and returns a collection yielding unique keys and their number of occurrences in the original collection.

### Usage examples

`reduce` is a variant of `fold` without an initial state -- see [section 19](#19) for more on `fold`.  One way to think of it is just inserting a operator between
each element.

```fsharp
["a";"b";"c"] |> List.reduce (+)     
// "abc"
```

is the same as 

```fsharp
"a" + "b" + "c"
```

Here's another example:

```fsharp
[2;3;4] |> List.reduce (*)     
// is same as
2 * 3 * 4
// Result is 24
```

Some ways of combining elements depend on the order of combining, and so there are two variants of "reduce":

* `reduce` moves forward through the list.
* `reduceBack`, not surprisingly, moves backwards through the list.

Here's a demonstration of the difference. First `reduce`:

```fsharp
[1;2;3;4] |> List.reduce (fun state x -> (state)*10 + x)

// built up from                // state at each step
1                               // 1
(1)*10 + 2                      // 12 
((1)*10 + 2)*10 + 3             // 123 
(((1)*10 + 2)*10 + 3)*10 + 4    // 1234

// Final result is 1234   
```
  
Using the *same* combining function with `reduceBack` produces a different result! It looks like this:
  
```fsharp
[1;2;3;4] |> List.reduceBack (fun x state -> x + 10*(state))

// built up from                // state at each step
4                               // 4
3 + 10*(4)                      // 43  
2 + 10*(3 + 10*(4))             // 432  
1 + 10*(2 + 10*(3 + 10*(4)))    // 4321  

// Final result is 4321   
```
  
Again, see [section 19](#19) for a more detailed discussion of the related functions `fold` and `foldBack`.  

The other aggregation functions are much more straightforward.
  
```fsharp
type Suit = Club | Diamond | Spade | Heart 
type Rank = Two | Three | King | Ace
let cards = [ (Club,King); (Diamond,Ace); (Spade,Two); (Heart,Three); ]

cards |> List.max        // (Heart, Three)
cards |> List.maxBy snd  // (Diamond, Ace)
cards |> List.min        // (Club, King)
cards |> List.minBy snd  // (Spade, Two)

[1..10] |> List.sum
// 55

[ (1,"a"); (2,"b") ] |> List.sumBy fst
// 3

[1..10] |> List.average
// The type 'int' does not support the operator 'DivideByInt'

[1..10] |> List.averageBy float
// 5.5

[ (1,"a"); (2,"b") ] |> List.averageBy (fst >> float)
// 1.5

[1..10] |> List.length
// 10

[ ("a","A"); ("b","B"); ("a","C") ]  |> List.countBy fst
// [("a", 2); ("b", 1)]
  
[ ("a","A"); ("b","B"); ("a","C") ]  |> List.countBy snd
// [("A", 1); ("B", 1); ("C", 1)]
```

Most of the aggregation functions do not like empty lists!  You might consider using one of the `fold` functions to be safe -- see [section 19](#19).

```fsharp
let emptyListOfInts : int list = []

emptyListOfInts |> List.reduce (+)     
// ArgumentException: The input list was empty.

emptyListOfInts |> List.max
// ArgumentException: The input sequence was empty.

emptyListOfInts |> List.min
// ArgumentException: The input sequence was empty.

emptyListOfInts |> List.sum      
// 0

emptyListOfInts |> List.averageBy float
// ArgumentException: The input sequence was empty.

let emptyListOfTuples : (int*int) list = []
emptyListOfTuples |> List.countBy fst
// (int * int) list = []
```
  
<a id="15"></a> 
<hr>  
## 15.	Changing the order of the elements

You can change the order of the elements using reversing, sorting and permuting. All of the following return *new* collections:

* [`rev: list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L608).
  Returns a new collection with the elements in reverse order.
* [`sort: list:'T list -> 'T list when 'T : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L678).
  Sorts the given collection using Operators.compare.
* [`sortDescending: list:'T list -> 'T list when 'T : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L705).
  Sorts the given collection in descending order using Operators.compare.
* [`sortBy: projection:('T -> 'Key) -> list:'T list -> 'T list when 'Key : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L670).
  Sorts the given collection using keys given by the given projection. Keys are compared using Operators.compare.
* [`sortByDescending: projection:('T -> 'Key) -> list:'T list -> 'T list when 'Key : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L697).
  Sorts the given collection in descending order using keys given by the given projection. Keys are compared using Operators.compare.
* [`sortWith: comparer:('T -> 'T -> int) -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L661).
  Sorts the given collection using the given comparison function.
* [`permute : indexMap:(int -> int) -> list:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L570).
  Returns a collection with all elements permuted according to the specified permutation.
  
And there are also some array-only functions that sort in place:
  
* (Array only) [`sortInPlace: array:'T[] -> unit when 'T : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/array.fsi#L874).
  Sorts the elements of an array by mutating the array in-place. Elements are compared using Operators.compare.
* (Array only) [`sortInPlaceBy: projection:('T -> 'Key) -> array:'T[] -> unit when 'Key : comparison`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/array.fsi#L858).
  Sorts the elements of an array by mutating the array in-place, using the given projection for the keys. Keys are compared using Operators.compare.
* (Array only) [`sortInPlaceWith: comparer:('T -> 'T -> int) -> array:'T[] -> unit`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/array.fsi#L867).
  Sorts the elements of an array by mutating the array in-place, using the given comparison function as the order.

### Usage examples
  
```fsharp
[1..5] |> List.rev
// [5; 4; 3; 2; 1]

[2;4;1;3;5] |> List.sort
// [1; 2; 3; 4; 5]

[2;4;1;3;5] |> List.sortDescending
// [5; 4; 3; 2; 1]

[ ("b","2"); ("a","3"); ("c","1") ]  |> List.sortBy fst
// [("a", "3"); ("b", "2"); ("c", "1")]

[ ("b","2"); ("a","3"); ("c","1") ]  |> List.sortBy snd
// [("c", "1"); ("b", "2"); ("a", "3")]

// example of a comparer
let tupleComparer tuple1 tuple2  =
    if tuple1 < tuple2 then 
        -1 
    elif tuple1 > tuple2 then 
        1 
    else
        0

[ ("b","2"); ("a","3"); ("c","1") ]  |> List.sortWith tupleComparer
// [("a", "3"); ("b", "2"); ("c", "1")]

[1..10] |> List.permute (fun i -> (i + 3) % 10)
// [8; 9; 10; 1; 2; 3; 4; 5; 6; 7]

[1..10] |> List.permute (fun i -> 9 - i)
// [10; 9; 8; 7; 6; 5; 4; 3; 2; 1]
```

<a id="16"></a> 
<hr>  
## 16.	Testing the elements of a collection

These set of functions all return true or false.

* [`contains: value:'T -> source:'T list -> bool when 'T : equality`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L97).
  Tests if the collection contains the specified element.
* [`exists: predicate:('T -> bool) -> list:'T list -> bool`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L176).
  Tests if any element of the collection satisfies the given predicate.
* [`forall: predicate:('T -> bool) -> list:'T list -> bool`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L299).
  Tests if all elements of the collection satisfy the given predicate.
* [`isEmpty: list:'T list -> bool`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L353).
  Returns true if the collection contains no elements, false otherwise.

### Usage examples
  
```fsharp
[1..10] |> List.contains 5
// true

[1..10] |> List.contains 42
// false

[1..10] |> List.exists (fun i -> i > 3 && i < 5)
// true

[1..10] |> List.exists (fun i -> i > 5 && i < 3)
// false

[1..10] |> List.forall (fun i -> i > 0)
// true

[1..10] |> List.forall (fun i -> i > 5)
// false

[1..10] |> List.isEmpty
// false
```

<a id="17"></a> 
<hr>  
## 17.	Transforming each element to something different

I sometimes like to think of functional programming as "transformation-oriented programming", and `map` (aka `Select` in LINQ) is one of the most fundamental ingredients for this approach.
In fact, I have devoted a whole series to it [here](/posts/elevated-world/).

* [`map: mapping:('T -> 'U) -> list:'T list -> 'U list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L419).
  Builds a new collection whose elements are the results of applying the given function to each of the elements of the collection.

Sometimes each element maps to a list, and you want to flatten out all the lists. For this case, use `collect` (aka `SelectMany` in LINQ).
  
* [`collect: mapping:('T -> 'U list) -> list:'T list -> 'U list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L70).
  For each element of the list, applies the given function. Concatenates all the results and return the combined list.
  
Other transformation functions include:
  
* `choose` in [section 12](#12) is a map and option filter combined.
* (Seq only) [`cast: source:IEnumerable -> seq<'T>`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/seq.fsi#L599).
  Wraps a loosely-typed `System.Collections` sequence as a typed sequence.

### Usage examples
  
Here are some examples of using `map` in the conventional way, as a function that accepts a list and a mapping function and returns a new transformed list:  

```fsharp
let add1 x = x + 1

// map as a list transformer
[1..5] |> List.map add1
// [2; 3; 4; 5; 6]

// the list being mapped over can contain anything!
let times2 x = x * 2
[ add1; times2] |> List.map (fun f -> f 5)
// [6; 10]
```

You can also think of `map` as a *function transformer*. It turns an element-to-element function into a list-to-list function.

```fsharp
let add1ToEachElement = List.map add1
// "add1ToEachElement" transforms lists to lists rather than ints to ints
// val add1ToEachElement : (int list -> int list)

// now use it
[1..5] |> add1ToEachElement 
// [2; 3; 4; 5; 6]
```

`collect` works to flatten lists. If you already have a list of lists, you can use `collect` with `id` to flatten them.
 
```fsharp
[2..5] |> List.collect (fun x -> [x; x*x; x*x*x] )
// [2; 4; 8; 3; 9; 27; 4; 16; 64; 5; 25; 125]

// using "id" with collect
let list1 = [1..3]
let list2 = [4..6]
[list1; list2] |> List.collect id
// [1; 2; 3; 4; 5; 6]
```

### Seq.cast

Finally, `Seq.cast` is useful when working with older parts of the BCL that have specialized collection classes rather than generics.

For example, the Regex library has this problem, and so the code below won't compile because `MatchCollection` is not an `IEnumerable<T>`

```fsharp
open System.Text.RegularExpressions

let matches = 
    let pattern = "\d\d\d"
    let matchCollection = Regex.Matches("123 456 789",pattern)
    matchCollection
    |> Seq.map (fun m -> m.Value)     // ERROR
    // ERROR: The type 'MatchCollection' is not compatible with the type 'seq<'a>'
    |> Seq.toList
```

The fix is to cast `MatchCollection` to a `Seq<Match>` and then the code will work nicely:

```fsharp
let matches = 
    let pattern = "\d\d\d"
    let matchCollection = Regex.Matches("123 456 789",pattern)
    matchCollection
    |> Seq.cast<Match> 
    |> Seq.map (fun m -> m.Value)
    |> Seq.toList
// output = ["123"; "456"; "789"]
```

<a id="18"></a> 
<hr>  
## 18.	Iterating over each element 

Normally, when processing a collection, we transform each element to a new value using `map`. But occasionally we need to process all the elements with a function which *doesn't*
produce a useful value (a "unit function").

* [`iter: action:('T -> unit) -> list:'T list -> unit`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L367).
  Applies the given function to each element of the collection.
* Alternatively, you can use a for-loop. The expression inside a for-loop *must* return `unit`.

### Usage examples
  
The most common examples of unit functions are all about side-effects: printing to the console, updating a database, putting a message on a queue, etc.
For the examples below, I will just use `printfn` as my unit function.

```fsharp
[1..3] |> List.iter (fun i -> printfn "i is %i" i)
(*
i is 1
i is 2
i is 3
*)

// or using partial application
[1..3] |> List.iter (printfn "i is %i")

// or using a for loop
for i = 1 to 3 do
    printfn "i is %i" i

// or using a for-in loop
for i in [1..3] do
    printfn "i is %i" i
```

As noted above, the expression inside an `iter` or for-loop must return unit.  In the following examples, we try to add 1 to the element, and get a compiler error:

```fsharp
[1..3] |> List.iter (fun i -> i + 1)
//                               ~~~
// ERROR error FS0001: The type 'unit' does not match the type 'int'

// a for-loop expression *must* return unit
for i in [1..3] do
     i + 1  // ERROR
     // This expression should have type 'unit', 
     // but has type 'int'. Use 'ignore' ...
```

If you are sure that this is not a logic bug in your code, and you want to get rid of this error, you can pipe the results into `ignore`:

```fsharp
[1..3] |> List.iter (fun i -> i + 1 |> ignore)

for i in [1..3] do
     i + 1 |> ignore
```

<a id="19"></a> 
<hr>  
## 19.	Threading state through an iteration

The `fold` function is the most basic and powerful function in the collection arsenal. All other functions (other than generators like `unfold`) can be written in terms of it. See the examples below.

* [`fold<'T,'State> : folder:('State -> 'T -> 'State) -> state:'State -> list:'T list -> 'State`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L254).
  Applies a function to each element of the collection, threading an accumulator argument through the computation.
* [`foldBack<'T,'State> : folder:('T -> 'State -> 'State) -> list:'T list -> state:'State -> 'State`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L276).
  Applies a function to each element of the collection, starting from the end, threading an accumulator argument through the computation.
  WARNING: Watch out for using `Seq.foldBack` on infinite lists! The runtime will laugh at you ha ha ha and then go very quiet.
  
The `fold` function is often called "fold left" and `foldBack` is often called "fold right".

The `scan` function is like `fold` but returns the intermediate results and thus can be used to trace or monitor the iteration.

* [`scan<'T,'State>  : folder:('State -> 'T -> 'State) -> state:'State -> list:'T list -> 'State list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L619).
  Like `fold`, but returns both the intermediary and final results.
* [`scanBack<'T,'State> : folder:('T -> 'State -> 'State) -> list:'T list -> state:'State -> 'State list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L627).
  Like `foldBack`, but returns both the intermediary and final results.

Just like the fold twins, `scan` is often called "scan left" and `scanBack` is often called "scan right".
  
Finally, `mapFold` combines `map` and `fold` into one awesome superpower. More complicated than using `map` and `fold` separately but also more efficient.

* [`mapFold<'T,'State,'Result> : mapping:('State -> 'T -> 'Result * 'State) -> state:'State -> list:'T list -> 'Result list * 'State`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L447).
  Combines map and fold. Builds a new collection whose elements are the results of applying the given function to each of the elements of the input collection. The function is also used to accumulate a final value.
* [`mapFoldBack<'T,'State,'Result> : mapping:('T -> 'State -> 'Result * 'State) -> list:'T list -> state:'State -> 'Result list * 'State`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L456).
  Combines map and foldBack. Builds a new collection whose elements are the results of applying the given function to each of the elements of the input collection. The function is also used to accumulate a final value.

### `fold` examples

One way of thinking about `fold` is that it is like `reduce` but with an extra parameter for the initial state:

```fsharp
["a";"b";"c"] |> List.fold (+) "hello: "    
// "hello: abc"
// "hello: " + "a" + "b" + "c"

[1;2;3] |> List.fold (+) 10    
// 16
// 10 + 1 + 2 + 3
```

As with `reduce`, `fold` and `foldBack` can give very different answers.

```fsharp
[1;2;3;4] |> List.fold (fun state x -> (state)*10 + x) 0
                                // state at each step
1                               // 1
(1)*10 + 2                      // 12 
((1)*10 + 2)*10 + 3             // 123 
(((1)*10 + 2)*10 + 3)*10 + 4    // 1234
// Final result is 1234   
```

And here's the `foldBack` version:

```fsharp
List.foldBack (fun x state -> x + 10*(state)) [1;2;3;4] 0
                                // state at each step  
4                               // 4
3 + 10*(4)                      // 43  
2 + 10*(3 + 10*(4))             // 432  
1 + 10*(2 + 10*(3 + 10*(4)))    // 4321  
// Final result is 4321   
```

Note that `foldBack` has a different parameter order to `fold`: the list is second last, and the initial state is last, which means that piping is not as convenient.

### Recursing vs iterating

It's easy to get confused between `fold` vs. `foldBack`. I find it helpful to think of `fold` as being about *iteration* while `foldBack` is about *recursion*.

Let's say we want to calculate the sum of a list. The iterative way would be to use a for-loop.
You start with a (mutable) accumulator and thread it through each iteration, updating it as you go.

```fsharp
let iterativeSum list = 
    let mutable total = 0
    for e in list do
        total <- total + e
    total // return sum
```

On the other hand, the recursive approach says that
if the list has a head and tail, calculate the sum of the tail (a smaller list) first, and then add the head to it.

Each time the tail gets smaller and smaller until it is empty, at which point you're done.

```fsharp
let rec recursiveSum list = 
    match list with
    | [] -> 
        0
    | head::tail -> 
        head + (recursiveSum tail)
```

Which approach is better?

For aggregation, the iterative way is (`fold`) often easiest to understand. 
But for things like constructing new lists, the recursive way is (`foldBack`) is easier to understand. 

For example, if we were going to going to create a function from scratch that turned each element into the corresponding string,
we might write something like this:

```fsharp
let rec mapToString list = 
    match list with
    | [] -> 
        []
    | head::tail -> 
        head.ToString() :: (mapToString tail)

[1..3] |> mapToString 
// ["1"; "2"; "3"]
```

Using `foldBack` we can transfer that same logic "as is":

* action for empty list = `[]`
* action for non-empty list = `head.ToString() :: state`

Here is the resulting function:

```fsharp
let foldToString list = 
    let folder head state = 
        head.ToString() :: state
    List.foldBack folder list []

[1..3] |> foldToString 
// ["1"; "2"; "3"]
```

On the other hand, a big advantage of `fold` is that it is easier to use "inline" because it plays better with piping.

Luckily, you can use `fold` (for list construction at least) just like `foldBack` as long as you reverse the list at the end.

```fsharp
// inline version of "foldToString"
[1..3] 
|> List.fold (fun state head -> head.ToString() :: state) []
|> List.rev
// ["1"; "2"; "3"]
```

### Using `fold` to implement other functions

As I mentioned above, `fold` is the core function for operating on lists and can emulate most other functions,
although perhaps not as efficiently as a custom implementation.

For example, here is `map` implemented using `fold`:

```fsharp
/// map a function "f" over all elements
let myMap f list = 
    // helper function
    let folder state head =
        f head :: state

    // main flow
    list
    |> List.fold folder []
    |> List.rev

[1..3] |> myMap (fun x -> x + 2)
// [3; 4; 5]
```

And here is `filter` implemented using `fold`:

```fsharp
/// return a new list of elements for which "pred" is true
let myFilter pred list = 
    // helper function
    let folder state head =
        if pred head then 
            head :: state
        else
            state

    // main flow
    list
    |> List.fold folder []
    |> List.rev

let isOdd n = (n%2=1)
[1..5] |> myFilter isOdd 
// [1; 3; 5]
```

And of course, you can emulate the other functions in a similar way.

### `scan` examples

Earlier, I showed an example of the intermediate steps of `fold`:

```fsharp
[1;2;3;4] |> List.fold (fun state x -> (state)*10 + x) 0
                                // state at each step
1                               // 1
(1)*10 + 2                      // 12 
((1)*10 + 2)*10 + 3             // 123 
(((1)*10 + 2)*10 + 3)*10 + 4    // 1234
// Final result is 1234   
```

For that example, I had to manually calculate the intermediate states,

Well, if I had used `scan`, I would have got those intermediate states for free!

```fsharp
[1;2;3;4] |> List.scan (fun state x -> (state)*10 + x) 0
// accumulates from left ===> [0; 1; 12; 123; 1234]
```

`scanBack` works the same way, but backwards of course:

```fsharp
List.scanBack (fun x state -> (state)*10 + x) [1;2;3;4] 0
// [4321; 432; 43; 4; 0]  <=== accumulates from right
```

Just as with `foldBack` the parameter order for "scan right" is inverted compared with "scan left".

### Truncating a string with `scan` 

Here's an example where `scan` is useful. Say that you have a news site, and you need to make sure headlines fit into 50 chars.

You could just truncate the string at 50, but that would look ugly. Instead you want to have the truncation end at a word boundary.

Here's one way of doing it using `scan`:

* Split the headline into words.
* Use `scan` to concat the words back together, generating a list of fragments, each with an extra word added.
* Get the longest fragment under 50 chars.

```fsharp
// start by splitting the text into words
let text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor."
let words = text.Split(' ')
// [|"Lorem"; "ipsum"; "dolor"; "sit"; ... ]

// accumulate a series of fragments
let fragments = words |> Seq.scan (fun frag word -> frag + " " + word) ""
(*
" Lorem" 
" Lorem ipsum" 
" Lorem ipsum dolor"
" Lorem ipsum dolor sit" 
" Lorem ipsum dolor sit amet,"
etc
*)

// get the longest fragment under 50
let longestFragUnder50 = 
    fragments 
    |> Seq.takeWhile (fun s -> s.Length <= 50) 
    |> Seq.last 

// trim off the first blank
let longestFragUnder50Trimmed = 
    longestFragUnder50 |> (fun s -> s.[1..])

// The result is:
//   "Lorem ipsum dolor sit amet, consectetur"
```

Note that I'm using `Seq.scan` rather than `Array.scan`. This does a lazy scan and avoids having to create fragments that are not needed.

Finally, here is the complete logic as a utility function:

```fsharp
// the whole thing as a function
let truncText max (text:string) = 
    if text.Length <= max then
        text
    else
        text.Split(' ')
        |> Seq.scan (fun frag word -> frag + " " + word) ""
        |> Seq.takeWhile (fun s -> s.Length <= max-3) 
        |> Seq.last 
        |> (fun s -> s.[1..] + "...")
    
"a small headline" |> truncText 50
// "a small headline"

text |> truncText 50
// "Lorem ipsum dolor sit amet, consectetur..."
```

Yes, I know that there is a more efficient implementation than this, but I hope that this little example shows off the power of `scan`.

### `mapFold` examples

The `mapFold` function can do a map and a fold in one step, which can be convenient on occasion. 

Here's an example of combining an addition and a sum in one step using `mapFold`:

```fsharp
let add1 x = x + 1

// add1 using map
[1..5] |> List.map (add1)   
// Result => [2; 3; 4; 5; 6]

// sum using fold
[1..5] |> List.fold (fun state x -> state + x) 0   
// Result => 15

// map and sum using mapFold
[1..5] |> List.mapFold (fun state x -> add1 x, (state + x)) 0   
// Result => ([2; 3; 4; 5; 6], 15)
```


<a id="20"></a> 
<hr>  
## 20. Working with the index of each element 

Often, you need the index of the element as you do an iteration. You could use a mutable counter, but why not sit back and let the library do the work for you?

* [`mapi: mapping:(int -> 'T -> 'U) -> list:'T list -> 'U list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L465).
  Like `map`, but with the integer index passed to the function as well. See [section 17](#17) for more on `map`.
* [`iteri: action:(int -> 'T -> unit) -> list:'T list -> unit`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L382).
  Like `iter`, but with the integer index passed to the function as well. See [section 18](#18) for more on `iter`.
* [`indexed: list:'T list -> (int * 'T) list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L340).
  Returns a new list whose elements are the corresponding elements of the input list paired with the index (from 0) of each element.

  
### Usage examples
  
```fsharp
['a'..'c'] |> List.mapi (fun index ch -> sprintf "the %ith element is '%c'" index ch)
// ["the 0th element is 'a'"; "the 1th element is 'b'"; "the 2th element is 'c'"]

// with partial application
['a'..'c'] |> List.mapi (sprintf "the %ith element is '%c'")
// ["the 0th element is 'a'"; "the 1th element is 'b'"; "the 2th element is 'c'"]

['a'..'c'] |> List.iteri (printfn "the %ith element is '%c'")
(*
the 0th element is 'a'
the 1th element is 'b'
the 2th element is 'c'
*)
```

`indexed` generates a tuple with the index -- a shortcut for a specific use of `mapi`:

```fsharp
['a'..'c'] |> List.mapi (fun index ch -> (index, ch) )
// [(0, 'a'); (1, 'b'); (2, 'c')]

// "indexed" is a shorter version of above
['a'..'c'] |> List.indexed
// [(0, 'a'); (1, 'b'); (2, 'c')]
```


<a id="21"></a> 
<hr>  
## 21. Transforming the whole collection to a different collection type 

You often need to convert from one kind of collection to another. These functions do this.

The `ofXXX` functions are used to convert from `XXX` to the module type. For example, `List.ofArray` will turn an array into a list.

* (Except Array) [`ofArray : array:'T[] -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L526).
  Builds a new collection from the given array.
* (Except Seq) [`ofSeq: source:seq<'T> -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L532).
  Builds a new collection from the given enumerable object.
* (Except List) [`ofList: source:'T list -> seq<'T>`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/seq.fsi#L864).
  Builds a new collection from the given list.

The `toXXX` are used to convert from the module type to the type `XXX`. For example, `List.toArray` will turn an list into an array.
  
* (Except Array) [`toArray: list:'T list -> 'T[]`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L762).
  Builds an array from the given collection.
* (Except Seq) [`toSeq: list:'T list -> seq<'T>`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L768).
  Views the given collection as a sequence.
* (Except List) [`toList: source:seq<'T> -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/seq.fsi#L1189).
  Builds a list from the given collection.

### Usage examples
  
```fsharp
[1..5] |> List.toArray      // [|1; 2; 3; 4; 5|]
[1..5] |> Array.ofList      // [|1; 2; 3; 4; 5|]
// etc
```

### Using sequences with disposables

One important use of these conversion functions is to convert a lazy enumeration (`seq`) to a fully evaluated collection such as `list`. This is particularly
important when there is a disposable resource involved, such as file handle or database connection. If the sequence is not converted into a list
you may encounter errors accessing the elements.  See [section 28](#28) for more.



<a id="22"></a> 
<hr>  
## 22. Changing the behavior of the collection as a whole

There are some special functions (for Seq only) that change the behavior of the collection as a whole.

* (Seq only) [`cache: source:seq<'T> -> seq<'T>`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/seq.fsi#L98).
  Returns a sequence that corresponds to a cached version of the input sequence. This result sequence will have the same elements as the input sequence. The result 
  can be enumerated multiple times. The input sequence will be enumerated at most once and only as far as is necessary.
* (Seq only) [`readonly : source:seq<'T> -> seq<'T>`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/seq.fsi#L919).
  Builds a new sequence object that delegates to the given sequence object. This ensures the original sequence cannot be rediscovered and mutated by a type cast.
* (Seq only) [`delay : generator:(unit -> seq<'T>) -> seq<'T>`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/seq.fsi#L221).
  Returns a sequence that is built from the given delayed specification of a sequence.

### `cache` example
  
Here's an example of `cache` in use:
  
```fsharp
let uncachedSeq = seq {
    for i = 1 to 3 do
        printfn "Calculating %i" i
        yield i
    }

// iterate twice    
uncachedSeq |> Seq.iter ignore
uncachedSeq |> Seq.iter ignore
```

The result of iterating over the sequence twice is as you would expect:

```text
Calculating 1
Calculating 2
Calculating 3
Calculating 1
Calculating 2
Calculating 3
```

But if we cache the sequence...

```fsharp
let cachedSeq = uncachedSeq |> Seq.cache

// iterate twice    
cachedSeq |> Seq.iter ignore
cachedSeq |> Seq.iter ignore
```

... then each item is only printed once:

```text
Calculating 1
Calculating 2
Calculating 3
```

### `readonly` example

Here's an example of `readonly` being used to hide the underlying type of the sequence:

```fsharp
// print the underlying type of the sequence
let printUnderlyingType (s:seq<_>) =
    let typeName = s.GetType().Name 
    printfn "%s" typeName 

[|1;2;3|] |> printUnderlyingType 
// Int32[]

[|1;2;3|] |> Seq.readonly |> printUnderlyingType 
// mkSeq@589   // a temporary type
```

### `delay` example

Here's an example of `delay`.

```fsharp
let makeNumbers max =
    [ for i = 1 to max do
        printfn "Evaluating %d." i
        yield i ]

let eagerList = 
    printfn "Started creating eagerList" 
    let list = makeNumbers 5
    printfn "Finished creating eagerList" 
    list

let delayedSeq = 
    printfn "Started creating delayedSeq" 
    let list = Seq.delay (fun () -> makeNumbers 5 |> Seq.ofList)
    printfn "Finished creating delayedSeq" 
    list
```

If we run the code above, we find that just by creating `eagerList`, we print all the "Evaluating" messages. But creating `delayedSeq` does not trigger the list iteration.

```text
Started creating eagerList
Evaluating 1.
Evaluating 2.
Evaluating 3.
Evaluating 4.
Evaluating 5.
Finished creating eagerList

Started creating delayedSeq
Finished creating delayedSeq
```

Only when the sequence is iterated over does the list creation happen:

```fsharp
eagerList |> Seq.take 3  // list already created
delayedSeq |> Seq.take 3 // list creation triggered
```

An alternative to using delay is just to embed the list in a `seq` like this:

```fsharp
let embeddedList = seq {
    printfn "Started creating embeddedList" 
    yield! makeNumbers 5 
    printfn "Finished creating embeddedList" 
    }
```

As with `delayedSeq`, the `makeNumbers` function will not be called until the sequence is iterated over.

<a id="23"></a> 
<hr>  
## 23. Working with two lists

If you have two lists, there are analogues of most of the common functions like map and fold. 

* [`map2: mapping:('T1 -> 'T2 -> 'U) -> list1:'T1 list -> list2:'T2 list -> 'U list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L428).
  Builds a new collection whose elements are the results of applying the given function to the corresponding elements of the two collections pairwise.
* [`mapi2: mapping:(int -> 'T1 -> 'T2 -> 'U) -> list1:'T1 list -> list2:'T2 list -> 'U list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L473).
  Like `mapi`, but mapping corresponding elements from two lists of equal length.
* [`iter2: action:('T1 -> 'T2 -> unit) -> list1:'T1 list -> list2:'T2 list -> unit`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L375).
  Applies the given function to two collections simultaneously. The collections must have identical size.
* [`iteri2: action:(int -> 'T1 -> 'T2 -> unit) -> list1:'T1 list -> list2:'T2 list -> unit`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L391).
  Like `iteri`, but mapping corresponding elements from two lists of equal length.
* [`forall2: predicate:('T1 -> 'T2 -> bool) -> list1:'T1 list -> list2:'T2 list -> bool`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L314).
  The predicate is applied to matching elements in the two collections up to the lesser of the two lengths of the collections. If any application returns false then the overall result is false, else true. 
* [`exists2: predicate:('T1 -> 'T2 -> bool) -> list1:'T1 list -> list2:'T2 list -> bool`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L191).
  The predicate is applied to matching elements in the two collections up to the lesser of the two lengths of the collections. If any application returns true then the overall result is true, else false. 
* [`fold2<'T1,'T2,'State> : folder:('State -> 'T1 -> 'T2 -> 'State) -> state:'State -> list1:'T1 list -> list2:'T2 list -> 'State`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L266).
  Applies a function to corresponding elements of two collections, threading an accumulator argument through the computation.
* [`foldBack2<'T1,'T2,'State> : folder:('T1 -> 'T2 -> 'State -> 'State) -> list1:'T1 list -> list2:'T2 list -> state:'State -> 'State`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L288).
  Applies a function to corresponding elements of two collections, threading an accumulator argument through the computation.
* [`compareWith: comparer:('T -> 'T -> int) -> list1:'T list -> list2:'T list -> int`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L84).
  Compares two collections using the given comparison function, element by element. Returns the first non-zero result from the comparison function.  If the end of a collection
  is reached it returns a -1 if the first collection is shorter and a 1 if the second collection is shorter.
* See also `append`, `concat`, and `zip` in [section 26: combining and uncombining collections](#26).

### Usage examples
  
These functions are straightforward to use:

```fsharp
let intList1 = [2;3;4]
let intList2 = [5;6;7]

List.map2 (fun i1 i2 -> i1 + i2) intList1 intList2 
//  [7; 9; 11]

// TIP use the ||> operator to pipe a tuple as two arguments
(intList1,intList2) ||> List.map2 (fun i1 i2 -> i1 + i2) 
//  [7; 9; 11]

(intList1,intList2) ||> List.mapi2 (fun index i1 i2 -> index,i1 + i2) 
 // [(0, 7); (1, 9); (2, 11)]

(intList1,intList2) ||> List.iter2 (printf "i1=%i i2=%i; ") 
// i1=2 i2=5; i1=3 i2=6; i1=4 i2=7;

(intList1,intList2) ||> List.iteri2 (printf "index=%i i1=%i i2=%i; ") 
// index=0 i1=2 i2=5; index=1 i1=3 i2=6; index=2 i1=4 i2=7;

(intList1,intList2) ||> List.forall2 (fun i1 i2 -> i1 < i2)  
// true

(intList1,intList2) ||> List.exists2 (fun i1 i2 -> i1+10 > i2)  
// true

(intList1,intList2) ||> List.fold2 (fun state i1 i2 -> (10*state) + i1 + i2) 0 
// 801 = 234 + 567

List.foldBack2 (fun i1 i2 state -> i1 + i2 + (10*state)) intList1 intList2 0 
// 1197 = 432 + 765

(intList1,intList2) ||> List.compareWith (fun i1 i2 -> i1.CompareTo(i2))  
// -1

(intList1,intList2) ||> List.append
// [2; 3; 4; 5; 6; 7]

[intList1;intList2] |> List.concat
// [2; 3; 4; 5; 6; 7]

(intList1,intList2) ||> List.zip
// [(2, 5); (3, 6); (4, 7)]
```

### Need a function that's not here?

By using `fold2` and `foldBack2` you can easily create your own functions. For example, some `filter2` functions can be defined like this:

```fsharp
/// Apply a function to each element in a pair
/// If either result passes, include that pair in the result
let filterOr2 filterPredicate list1 list2 =
    let pass e = filterPredicate e 
    let folder e1 e2 state =    
        if (pass e1) || (pass e2) then
            (e1,e2)::state
        else
            state
    List.foldBack2 folder list1 list2 ([])

/// Apply a function to each element in a pair
/// Only if both results pass, include that pair in the result
let filterAnd2 filterPredicate list1 list2 =
    let pass e = filterPredicate e 
    let folder e1 e2 state =     
        if (pass e1) && (pass e2) then
            (e1,e2)::state
        else
            state
    List.foldBack2 folder list1 list2 []

// test it
let startsWithA (s:string) = (s.[0] = 'A')
let strList1 = ["A1"; "A3"]
let strList2 = ["A2"; "B1"]

(strList1, strList2) ||> filterOr2 startsWithA 
// [("A1", "A2"); ("A3", "B1")]
(strList1, strList2) ||> filterAnd2 startsWithA 
// [("A1", "A2")]
```

See also [section 25](#25).

<a id="24"></a> 
<hr>  
## 24. Working with three lists

If you have three lists, you only have one built-in function available. But see [section 25](#25) for an example of how you can build your own three-list functions. 

* [`map3: mapping:('T1 -> 'T2 -> 'T3 -> 'U) -> list1:'T1 list -> list2:'T2 list -> list3:'T3 list -> 'U list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L438).
  Builds a new collection whose elements are the results of applying the given function to the corresponding elements of the three collections simultaneously.
* See also `append`, `concat`, and `zip3` in [section 26: combining and uncombining collections](#26).
  
<a id="25"></a> 
<hr>  
## 25. Working with more than three lists

If you are working with more than three lists, there are no built in functions for you. 

If this happens infrequently, then you could just collapse the lists into a single tuple using `zip2` and/or `zip3` in succession, and then process that tuple using `map`.

Alternatively you can "lift" your function to the world of "zip lists" using applicatives. 

```fsharp
let (<*>) fList xList = 
    List.map2 (fun f x -> f x) fList xList 

let (<!>) = List.map

let addFourParams x y z w = 
    x + y + z + w

// lift "addFourParams" to List world and pass lists as parameters rather than ints
addFourParams <!> [1;2;3] <*> [1;2;3] <*> [1;2;3] <*> [1;2;3] 
// Result = [4; 8; 12]
```

If that seems like magic, see [this series](/posts/elevated-world/#lift) for a explanation of what this code is doing.


<a id="26"></a> 
<hr>  
## 26. Combining and uncombining collections

Finally, there are a number of functions that combine and uncombine collections.

* [`append: list1:'T list -> list2:'T list -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L21).
  Returns a new collection that contains the elements of the first collection followed by elements of the second.
* `@` is an infix version of `append` for lists.
* [`concat: lists:seq<'T list> -> 'T list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L90).
  Builds a new collection whose elements are the results of applying the given function to the corresponding elements of the collections simultaneously.
* [`zip: list1:'T1 list -> list2:'T2 list -> ('T1 * 'T2) list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L882).
  Combines two collections into a list of pairs. The two collections must have equal lengths.
* [`zip3: list1:'T1 list -> list2:'T2 list -> list3:'T3 list -> ('T1 * 'T2 * 'T3) list`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L890).
  Combines three collections into a list of triples. The collections must have equal lengths.
* (Except Seq) [`unzip: list:('T1 * 'T2) list -> ('T1 list * 'T2 list)`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L852).
  Splits a collection of pairs into two collections.
* (Except Seq) [`unzip3: list:('T1 * 'T2 * 'T3) list -> ('T1 list * 'T2 list * 'T3 list)`](https://github.com/fsharp/fsharp/blob/4331dca3648598223204eed6bfad2b41096eec8a/src/fsharp/FSharp.Core/list.fsi#L858).
  Splits a collection of triples into three collections.


### Usage examples
  
These functions are straightforward to use:

```fsharp
List.append [1;2;3] [4;5;6]
// [1; 2; 3; 4; 5; 6]

[1;2;3] @ [4;5;6]
// [1; 2; 3; 4; 5; 6]

List.concat [ [1]; [2;3]; [4;5;6] ]
// [1; 2; 3; 4; 5; 6]

List.zip [1;2] [10;20] 
// [(1, 10); (2, 20)]

List.zip3 [1;2] [10;20] [100;200]
// [(1, 10, 100); (2, 20, 200)]

List.unzip [(1, 10); (2, 20)]
// ([1; 2], [10; 20])

List.unzip3 [(1, 10, 100); (2, 20, 200)]
// ([1; 2], [10; 20], [100; 200])
```

Note that the `zip` functions require the lengths to be the same.

```fsharp
List.zip [1;2] [10] 
// ArgumentException: The lists had different lengths.
```

<a id="27"></a> 
<hr>  
## 27. Other array-only functions

Arrays are mutable, and therefore have some functions that are not applicable to lists and sequences.

* See the "sort in place" functions in [section 15](#15)
* `Array.blit: source:'T[] -> sourceIndex:int -> target:'T[] -> targetIndex:int -> count:int -> unit`.
   Reads a range of elements from the first array and write them into the second.
* `Array.copy: array:'T[] -> 'T[]`.
   Builds a new array that contains the elements of the given array.
* `Array.fill: target:'T[] -> targetIndex:int -> count:int -> value:'T -> unit`.
   Fills a range of elements of the array with the given value.
* `Array.set: array:'T[] -> index:int -> value:'T -> unit`.
   Sets an element of an array.
* In addition to these, all the other [BCL array functions](https://msdn.microsoft.com/en-us/library/system.array.aspx) are available as well.

I won't give examples. See the [MSDN documentation](https://msdn.microsoft.com/en-us/library/ee370273.aspx).

<a id="28"></a> 
<hr>  

## 28. Using sequences with disposables

One important use of conversion functions like `List.ofSeq` is to convert a lazy enumeration (`seq`) to a fully evaluated collection such as `list`. This is particularly
important when there is a disposable resource involved such as file handle or database connection. If the sequence is not converted into a list
while the resource is available you may encounter errors accessing the elements later, after the resource has been disposed.

This will be an extended example, so let's start with some helper functions that emulate a database and a UI:

```fsharp
// a disposable database connection
let DbConnection() = 
    printfn "Opening connection"
    { new System.IDisposable with
        member this.Dispose() =
            printfn "Disposing connection" }

// read some records from the database
let readNCustomersFromDb dbConnection n =
    let makeCustomer i = 
        sprintf "Customer %i" i

    seq {
        for i = 1 to n do
            let customer = makeCustomer i
            printfn "Loading %s from db" customer 
            yield customer 
        } 

// show some records on the screen
let showCustomersinUI customers = 
    customers |> Seq.iter (printfn "Showing %s in UI")
```

A naive implementation will cause the sequence to be evaluated *after* the connection is closed:

```fsharp
let readCustomersFromDb() =
    use dbConnection = DbConnection()
    let results = readNCustomersFromDb dbConnection 2
    results

let customers = readCustomersFromDb()
customers |> showCustomersinUI
```

The output is below. You can see that the connection is closed and only then is the sequence evaluated.

```text
Opening connection
Disposing connection
Loading Customer 1 from db  // error! connection closed!
Showing Customer 1 in UI
Loading Customer 2 from db
Showing Customer 2 in UI
```

A better implementation will convert the sequence to a list while the connection is open, causing the sequence to be evaluated immediately:

```fsharp
let readCustomersFromDb() =
    use dbConnection = DbConnection()
    let results = readNCustomersFromDb dbConnection 2
    results |> List.ofSeq
    // Convert to list while connection is open

let customers = readCustomersFromDb()
customers |> showCustomersinUI
```

The result is much better. All the records are loaded before the connection is disposed:

```text
Opening connection
Loading Customer 1 from db
Loading Customer 2 from db
Disposing connection
Showing Customer 1 in UI
Showing Customer 2 in UI
```

A third alternative is to embed the disposable in the sequence itself:

```fsharp
let readCustomersFromDb() =
    seq {
        // put disposable inside the sequence
        use dbConnection = DbConnection()
        yield! readNCustomersFromDb dbConnection 2
        } 

let customers = readCustomersFromDb()
customers |> showCustomersinUI
```

The output shows that now the UI display is also done while the connection is open:

```text
Opening connection
Loading Customer 1 from db
Showing Customer 1 in UI
Loading Customer 2 from db
Showing Customer 2 in UI
Disposing connection
```

This may be a bad thing (longer time for the connection to stay open) or a good thing (minimal memory use), depending on the context.

<a id="29"></a> 
<hr>  

## 29. The end of the adventure

You made it to the end -- well done! Not really much of an adventure, though, was it? No dragons or anything. Nevertheless, I hope it was helpful.

