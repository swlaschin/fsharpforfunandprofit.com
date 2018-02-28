---
layout: post
title: "The Option type"
description: "And why it is not null or nullable"
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 7
categories: [Types]
---

Now let's look at a particular union type, the Option type. It is so common and so useful that it is actually built into the language.

You have already seen the option type discussed in passing, but let's go back to basics and understand how it fits into the type system.

A very common situation is when you want to represent missing or invalid values. Using a diagram, the domain would look like this:

![int option](/assets/img/int_option.png)
 
Obviously this calls for some kind of union type!  

In F#, it is called the `Option` type, and is defined as union type with two cases: `Some` and `None`.  A similar type is common in functional languages: OCaml and Scala also call it `Option`, while Haskell calls it `Maybe`.

Here is a definition:

```fsharp
type Option<'a> =       // use a generic definition  
   | Some of 'a           // valid value
   | None                 // missing
```

<div class="alert alert-error">
IMPORTANT: if you evaluate this in the interactive window, be sure to reset the session afterwards, so that the built-in type is restored.
</div>

The option type is used in the same way as any union type in construction, by specifying one of the two cases, the `Some` case or the `None` case:

```fsharp
let validInt = Some 1
let invalidInt = None
```

and when pattern matching, as with any union type, you must always match all the cases:

```fsharp
match validInt with 
| Some x -> printfn "the valid value is %A" x
| None -> printfn "the value is None" 
```

When defining a type that references the Option type, you must specify the generic type to use.  You can do this in an explicit way, with angle brackets, or use the built-in "`option`" keyword which comes after the type. The following examples are identical:

```fsharp
type SearchResult1 = Option<string>  // Explicit C#-style generics 
type SearchResult2 = string option   // built-in postfix keyword
```




## Using the Option type 

The option type is widely used in the F# libraries for values that might be missing or otherwise invalid.

For example, the `List.tryFind` function returns an option, with the `None` case used indicate that nothing matches the search predicate.

```fsharp
[1;2;3;4]  |> List.tryFind (fun x-> x = 3)  // Some 3
[1;2;3;4]  |> List.tryFind (fun x-> x = 10) // None
```

Let's revisit the same example we used for tuples and records, and see how options might be used instead:

```fsharp
// the tuple version of TryParse
let tryParseTuple intStr = 
   try
      let i = System.Int32.Parse intStr
      (true,i)
   with _ -> (false,0)  // any exception

// for the record version, create a type to hold the return result
type TryParseResult = {success:bool; value:int} 

// the record version of TryParse
let tryParseRecord intStr = 
   try
      let i = System.Int32.Parse intStr
      {success=true;value=i}
   with _ -> {success=false;value=0}  

// the option version of TryParse
let tryParseOption intStr = 
   try
      let i = System.Int32.Parse intStr
      Some i
   with _ -> None

//test it
tryParseTuple "99"
tryParseRecord "99"
tryParseOption "99"
tryParseTuple "abc"
tryParseRecord "abc"
tryParseOption "abc"
```

Of these three approaches, the "option" version is generally preferred; no new types need to be defined and for simple cases, the meaning of `None` is obvious from the context.

*NOTE: The `tryParseOption` code is just an example. A similar function `tryParse` is built into the .NET core libraries and should be used instead.*

### Option equality 

Like other union types, option types have an automatically defined equality operation

```fsharp
let o1 = Some 42
let o2 = Some 42

let areEqual = (o1=o2)
```

{% include book_page_pdf.inc %}

### Option representation

Option types have a nice default string representation, and unlike other union types, the `ToString()` representation is also nice.

```fsharp
let o = Some 42
printfn "%A" o   // nice
printfn "%O" o   // nice
```

### Options are not just for primitive types

The F# option is a true first class type (it's just a normal union type, after all). You can use it with *any* type.  For example, you can have an option of a complex type like Person,
or a tuple type like `int*int`, or a function type like `int->bool`, or even an option of an option type.

```fsharp
type OptionalString = string option 
type OptionalPerson = Person option       // optional complex type
type OptionalTuple = (int*int) option       
type OptionalFunc = (int -> bool) option  // optional function
type NestedOptionalString = OptionalString option //nested options!
type StrangeOption = string option option option
```

## How the Option type should not be used

The option type has functions such as `IsSome`, `IsNone` and `Value`, which allow you to access the "wrapped" value without doing pattern matching. Don't use them! Not only it is not idiomatic, but it is dangerous and can cause exceptions.

Here is how not to do it:

```fsharp
let x = Some 99

// testing using IsSome
if x.IsSome then printfn "x is %i" x.Value   // ugly!!

// no matching at all
printfn "x is %i" x.Value   // ugly and dangerous!!
```

Here is how to do it properly:

```fsharp
let x = Some 99
match x with 
| Some i -> printfn "x is %i" i
| None -> () // what to do here?
```

The pattern matching approach also forces you to think about and document what happens in the `None` case, which you might easily overlook when using `IsSome`.

## The Option module

If you are doing a lot of pattern matching on options, look into the `Option` module, as it has some useful helper functions like `map`, `bind`, `iter` and so on.

For example, say that I want to multiply the value of an option by 2 if it is valid. Here's the pattern matching way:
```fsharp
let x = Some 99
let result = match x with 
| Some i -> Some(i * 2)
| None -> None
```

And here's a more compact version written using `Option.map`:

```fsharp
let x = Some 99
x |> Option.map (fun v -> v * 2)
```

Or perhaps I want to multiply the value of an option by 2 if it is valid but return 0 if it is `None`. Here's the pattern matching way:

```fsharp
let x = Some 99
let result = match x with 
| Some i -> i * 2
| None -> 0
```

And here's the same thing as a one-liner using `Option.fold`:

```fsharp
let x = Some 99
x |> Option.fold (fun _ v -> v * 2) 0 
```

In simple cases like the one above, the `defaultArg` function can be used as well.

```fsharp
let x = Some 99
defaultArg x 0 
```

  
<a id="option-is-not-null"></a>
## Option vs. Null vs. Nullable

The option type often causes confusion to people who are used to dealing with nulls and nullables in C# and other languages. This section will try to clarify the differences.

### Type safety of Option vs. null 

In a language like C# or Java, "null" means a reference or pointer to an object that doesn't exist.  The "null" has *exactly the same type* as the object, so you can't tell from the type system that you have a null. 

For example, in the C# code below we create two string variables, one with a valid string and one with a null string. 

```csharp
string s1 = "abc";
var len1 = s1.Length;

string s2 = null;
var len2 = s2.Length;
```

This compiles perfectly, of course. The compiler cannot tell the difference between the two variables.
The `null` is exactly the same type as the valid string, so all the `System.String` methods and properties can be used on it, including the `Length` property. 

Now, we know that this code will fail by just looking at it, but the compiler can't help us.  Instead, as we all know, you have to tediously test for nulls constantly.

Now let's look at the nearest F# equivalent of the C# example above. In F#, to indicate missing data, you would use an option type and set it to `None`. (In this artificial example we have to use an ugly explicitly typed `None` -- normally this would not be necessary.)

```fsharp
let s1 = "abc"
var len1 = s1.Length

// create a string option with value None
let s2 = Option<string>.None
let len2 = s2.Length
```

In the F# version, we get a *compile-time* error immediately.  The `None` is *not* a string, it's a different type altogether, so you can't call `Length` on it directly.
And to be clear, `Some [string]` is *also* not the same type as `string`, so you can't call `Length` on it either!  

So if `Option<string>` is not a string, but you want to do something with the string it (might) contain, you are forced to have to pattern match on it (assuming you don't do bad things as described earlier).

```fsharp
let s2 = Option<string>.None

//which one is it?
let len2 = match s2 with
| Some s -> s.Length
| None -> 0
```

You always have to pattern match, because given a value of type `Option<string>`, you can't tell whether it is Some or None.

In just the same way `Option<int>` is not the same type as `int`, `Option<bool>` is not the same type as `bool`, and so on. 

To summarize the critical points:

* The type "`string option`" is not at all the same type as "`string`". You cannot cast from `string option` to `string` -- they do not have the same properties.
  A function that works with `string` will not work with `string option`, and vice versa.  So the type system will prevent any errors.
* On the other hand, a "null string" in C# is exactly the same type as "string". You cannot tell them apart at compile time, only at run time. A "null string" appears to have all the same properties and functions as a valid string, except that your code will blow up when you try to use it!

### Nulls vs. missing data

A "null" as used in C# is completely different from the concept of "missing" data, which is a valid part of modeling any system in any language.

In a true functional language there can be a concept of missing data, but there can be no such thing as "null", because the concepts of "pointers" or "uninitialized variables" do not exist in the functional way of thinking.  

For example, consider a value bound to the result of an expression like this:

```fsharp
let x = "hello world"
```

How can that value ever be uninitialized, or become null, or even become any other value at all? 

Unfortunately, additional confusion has been caused because in some cases API designers have used null to indicate the concept of "missing" data as well!  For example, the .NET library method `StreamReader.ReadLine` returns null to indicate that there is no more data in a file.


### F# and null ###

F# is not a pure functional language, and has to interact with the .NET languages that *do* have the concept of null. Therefore, F# does include a `null` keyword in its design, but makes it hard to use and treats it as an abnormal value.

As a general rule, nulls are never created in "pure" F#, but only by interacting with the .NET libraries or other external systems. 

Here are some examples:

```fsharp
// pure F# type is not allowed to be null (in general)
type Person = {first:string; last:string}  
let p : Person = null                      // error! 

// type defined in CLR, so is allowed to be null
let s : string = null                      // no error! 
let line = streamReader.ReadLine()         // no error if null 
```

In these cases, it is good practice to immediately check for nulls and convert them into an option type!  

```fsharp
// streamReader example
let line = match streamReader.ReadLine()  with
           | null -> None
           | line -> Some line

// environment example
let GetEnvVar var = 
    match System.Environment.GetEnvironmentVariable(var) with
    | null -> None
    | value -> Some value

// try it
GetEnvVar "PATH"
GetEnvVar "TEST"
```

And on occasion, you may need to pass a null to an external library. You can do this using the `null` keyword as well.

### Option vs. Nullable

In addition to null, C# has the concept of a Nullable type, such as `Nullable<int>`, which seems similar to the option type. So what's the difference?

The basic idea is the same, but Nullable is much weaker.  It only works on value types such as `Int` and `DateTime`, not on reference types such as strings or classes or functions. You can't nest Nullables, and they don't have much special behavior. 

On the other hand, the F# option is a true first class type and can be used consistently across all types in the same way. (See the examples above in the "Options are not just for primitive types" section.)

