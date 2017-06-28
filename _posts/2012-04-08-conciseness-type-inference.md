---
layout: post
title: "Type inference"
description: "How to avoid getting distracted by complex type syntax"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 8
categories: [Conciseness,Types]
---


As you have already seen, F# uses a technique called "type inference" to greatly reduce the number of type annotations that need to be explicitly specified in normal code. And even when types do need to be specified, the syntax is less longwinded compared to C#.

To see this, here are some C# methods that wrap two standard LINQ functions. The implementations are trivial, but the method signatures are extremely complex:

```csharp
public IEnumerable<TSource> Where<TSource>(
    IEnumerable<TSource> source,
    Func<TSource, bool> predicate
    )
{
    //use the standard LINQ implementation
    return source.Where(predicate);
}

public IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
    IEnumerable<TSource> source,
    Func<TSource, TKey> keySelector
    )
{
    //use the standard LINQ implementation
    return source.GroupBy(keySelector);
}
```

And here are the exact F# equivalents, showing that no type annotations are needed at all!

```fsharp
let Where source predicate = 
    //use the standard F# implementation
    Seq.filter predicate source

let GroupBy source keySelector = 
    //use the standard F# implementation
    Seq.groupBy keySelector source
```
	
<div class="alert alert-info">	
You might notice that the standard F# implementations for "filter" and "groupBy" have the parameters in exactly the opposite order from the LINQ implementations used in C#. The "source" parameter is placed last, rather than first. There is a reason for this, which will be explained in the <a href="/series/thinking-functionally.html">thinking functionally</a> series.
</div>

The type inference algorithm is excellent at gathering information from many sources to determine the types. In the following example, it correctly deduces that the `list` value is a list of strings.

```fsharp
let i  = 1
let s = "hello"
let tuple  = s,i      // pack into tuple   
let s2,i2  = tuple    // unpack
let list = [s2]       // type is string list
```

And in this example, it correctly deduces that the `sumLengths` function takes a list of strings and returns an int.

```fsharp
let sumLengths strList = 
    strList |> List.map String.length |> List.sum

// function type is: string list -> int
```


