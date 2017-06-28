---
layout: post
title: "Enum types"
description: "Not the same as a union type"
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 8
categories: [Types]
---

The enum type in F# is the same as the enum type in C#.  Its definition is superficially just like that of a union type, but there are many non-obvious differences to be aware of.

## Defining enums

To define an enum you use exactly the same syntax as a union type with empty cases, except that you must specify a constant value for each case, and the constants must all be of the same type.

{% highlight fsharp %}
type SizeUnion = Small | Medium | Large         // union
type ColorEnum = Red=0 | Yellow=1 | Blue=2      // enum 
{% endhighlight %}

Strings are not allowed, only ints or compatible types such bytes and chars:

{% highlight fsharp %}
type MyEnum = Yes = "Y" | No ="N"  // Error. Strings not allowed.
type MyEnum = Yes = 'Y' | No ='N'  // Ok because char was used.
{% endhighlight %}

Union types require that their cases start with an uppercase letter. This is not required for enums.

{% highlight fsharp %}
type SizeUnion = Small | Medium | large      // Error - "large" is invalid.
type ColorEnum = Red=0 | Yellow=1 | blue=2      // Ok
{% endhighlight %}

Just as with C#, you can use the FlagsAttribute for bit flags:

{% highlight fsharp %}
[<System.FlagsAttribute>]
type PermissionFlags = Read = 1 | Write = 2 | Execute = 4 
let permission = PermissionFlags.Read ||| PermissionFlags.Write
{% endhighlight %}

## Constructing enums

Unlike union types, to construct an enum you *must always* use a qualified name:

{% highlight fsharp %}
let red = Red            // Error. Enums must be qualified
let red = ColorEnum.Red  // Ok 
let small = Small        // Ok.  Unions do not need to be qualified
{% endhighlight %}

You can also cast to and from the underlying int type:

{% highlight fsharp %}
let redInt = int ColorEnum.Red  
let redAgain:ColorEnum = enum redInt // cast to a specified enum type 
let yellowAgain = enum<ColorEnum>(1) // or create directly
{% endhighlight %}

You can even create values that are not on the enumerated list at all.

{% highlight fsharp %}
let unknownColor = enum<ColorEnum>(99)   // valid
{% endhighlight %}

And, unlike unions, you can use the BCL Enum functions to enumerate and parse values, just as with C#. For example:

{% highlight fsharp %}
let values = System.Enum.GetValues(typeof<ColorEnum>)
let redFromString =  
    System.Enum.Parse(typeof<ColorEnum>,"Red") 
    :?> ColorEnum  // downcast needed
{% endhighlight %}

## Matching enums

To match an enum you must again *always* use a qualified name:

{% highlight fsharp %}
let unqualifiedMatch x = 
    match x with
    | Red -> printfn "red"             // warning FS0049
    | _ -> printfn "something else" 

let qualifiedMatch x = 
    match x with
    | ColorEnum.Red -> printfn "red"   //OK. qualified name used.
    | _ -> printfn "something else"
{% endhighlight %}

Both unions and enums will warn if you have not covered all known cases when pattern matching:

{% highlight fsharp %}
let matchUnionIncomplete x = 
    match x with
    | Small -> printfn "small"   
    | Medium -> printfn "medium"   
    // Warning: Incomplete pattern matches
    
let matchEnumIncomplete x = 
    match x with
    | ColorEnum.Red -> printfn "red"   
    | ColorEnum.Yellow -> printfn "yellow"   
    // Warning: Incomplete pattern matches
{% endhighlight %}

One important difference between unions and enums is that can you make the compiler happy about exhaustive pattern matching by listing all the union types.
    
Not so for enums. It is possible to create an enum not on the predeclared list, and try to match with it, and get a runtime exception, so the compiler will warn you even if you have explicitly
listed all the known enums:

{% highlight fsharp %}
// the compiler is still not happy
let matchEnumIncomplete2 x = 
    match x with
    | ColorEnum.Red -> printfn "red"   
    | ColorEnum.Yellow -> printfn "yellow"   
    | ColorEnum.Blue -> printfn "blue"   
    // the value '3' may indicate a case not covered by the pattern(s).
{% endhighlight %}

The only way to fix this is to add a wildcard to the bottom of the cases, to handle enums outside the predeclared range.

{% highlight fsharp %}
// the compiler is finally happy
let matchEnumComplete x = 
    match x with
    | ColorEnum.Red -> printfn "red"   
    | ColorEnum.Yellow -> printfn "yellow"   
    | ColorEnum.Blue -> printfn "blue"   
    | _ -> printfn "something else"   

// test with unknown case    
let unknownColor = enum<ColorEnum>(99)   // valid
matchEnumComplete unknownColor
{% endhighlight %}

## Summary

In general, you should prefer discriminated union types over enums, unless you really need to have an `int` value associated with them,
or you are writing types that need to be exposed to other .NET languages.