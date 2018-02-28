---
layout: post
title: "Using the type system to ensure correct code"
description: "In F# the type system is your friend, not your enemy"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 21
categories: [Correctness, Types]
---

You are familiar with static type checking through languages such as C# and Java. In these languages, the type checking is straightforward but rather crude, and can be seen as an annoyance compared with the freedom of dynamic languages such as Python and Ruby.

But in F# the type system is your friend, not your enemy. You can use static type checking almost as an instant unit test -- making sure that your code is correct at compile time.

In the earlier posts we have already seen some of the things that you can do with the type system in F#:

* The types and their associated functions provide an abstraction to model the problem domain. Because creating types is so easy, there is rarely an excuse to avoid designing them as needed for a given problem, and unlike C# classes it is hard to create "kitchen-sink" types that do everything.
* Well defined types aid in maintenance. Since F# uses type inference, you can normally rename or restructure types easily without using a refactoring tool. And if the type is changed in an incompatible way, this will almost certainly create compile-time errors that aid in tracking down any problems. 
* Well named types provide instant documentation about their roles in the program (and this documentation can never be out of date). 

In this post and the next we will focus on using the type system as an aid to writing correct code. I will demonstrate that you can create designs such that, if your code actually compiles, it will almost certainly work as designed.

## Using standard type checking ##

In C#, you use the compile-time checks to validate your code without even thinking about it. For example, would you give up `List<string>` for a plain `List`? Or give up `Nullable<int>` and be forced to used `object` with casting? Probably not. 

But what if you could have even more fine-grained types? You could have even better compile-time checks. And this is exactly what F# offers.

The F# type checker is not that much stricter than the C# type checker.  But because it is so easy to create new types without clutter, you can represent the domain better, and, as a useful side-effect, avoid many common errors.

Here is a simple example:

```fsharp
//define a "safe" email address type
type EmailAddress = EmailAddress of string

//define a function that uses it 
let sendEmail (EmailAddress email) = 
   printfn "sent an email to %s" email

//try to send one
let aliceEmail = EmailAddress "alice@example.com"
sendEmail aliceEmail

//try to send a plain string
sendEmail "bob@example.com"   //error
```

By wrapping the email address in a special type, we ensure that normal strings cannot be used as arguments to email specific functions. (In practice, we would also hide the constructor of the `EmailAddress` type as well, to ensure that only valid values could be created in the first place.)

There is nothing here that couldn't be done in C#, but it would be quite a lot of work to create a new value type just for this one purpose, so in C#, it is easy to be lazy and just pass strings around.

## Additional type safety features in F# ##

Before moving on to the major topic of "designing for correctness", let's see a few of the other minor, but cool, ways that F# is type-safe.

### Type-safe formatting with printf ###

Here is a minor feature that demonstrates one of the ways that F# is more type-safe than C#, and how the F# compiler can catch errors that would only be detected at runtime in C#.

Try evaluating the following and look at the errors generated:

```fsharp
let printingExample = 
   printf "an int %i" 2                        // ok
   printf "an int %i" 2.0                      // wrong type
   printf "an int %i" "hello"                  // wrong type
   printf "an int %i"                          // missing param

   printf "a string %s" "hello"                // ok
   printf "a string %s" 2                      // wrong type
   printf "a string %s"                        // missing param
   printf "a string %s" "he" "lo"              // too many params

   printf "an int %i and string %s" 2 "hello"  // ok
   printf "an int %i and string %s" "hello" 2  // wrong type
   printf "an int %i and string %s" 2          // missing param
```

Unlike C#, the compiler analyses the format string and determines what the number and types of the arguments are supposed to be. 

This can be used to constrain the types of parameters without explicitly having to specify them. So for example, in the code below, the compiler can deduce the types of the arguments automatically.

```fsharp
let printAString x = printf "%s" x
let printAnInt x = printf "%i" x

// the result is:
// val printAString : string -> unit  //takes a string parameter
// val printAnInt : int -> unit       //takes an int parameter
```

<a name="units-of-measure"></a>
### Units of measure ###

F# has the ability to define units of measure and associate them with floats. The unit of measure is then "attached" to the float as a type and prevents mixing different types. This is another feature that can be very handy if you need it.

```fsharp
// define some measures
[<Measure>] 
type cm

[<Measure>] 
type inches

[<Measure>] 
type feet =
   // add a conversion function
   static member toInches(feet : float<feet>) : float<inches> = 
      feet * 12.0<inches/feet>

// define some values
let meter = 100.0<cm>
let yard = 3.0<feet>

//convert to different measure
let yardInInches = feet.toInches(yard)

// can't mix and match!
yard + meter

// now define some currencies
[<Measure>] 
type GBP

[<Measure>] 
type USD

let gbp10 = 10.0<GBP>
let usd10 = 10.0<USD>
gbp10 + gbp10             // allowed: same currency
gbp10 + usd10             // not allowed: different currency
gbp10 + 1.0               // not allowed: didn't specify a currency
gbp10 + 1.0<_>            // allowed using wildcard
```

### Type-safe equality ###

One final example. In C# any class can be equated with any other class (using reference equality by default). In general, this is a bad idea! For example, you shouldn't really be able to compare a string with a person at all.  

Here is some C# code which is perfectly valid and compiles fine:

```csharp
using System;
var obj = new Object();
var ex = new Exception();
var b = (obj == ex);
```

If we write the identical code in F#, we get a compile-time error:

```fsharp
open System
let obj = new Object()
let ex = new Exception()
let b = (obj = ex)
```

Chances are, if you are testing equality between two different types, you are doing something wrong.

In F#, you can even stop a type being compared at all!  This is not as silly as it seems. For some types, there may not be a useful default, or you may want to force equality to be based on a specific field rather than the object as whole.

Here is an example of this:

```fsharp
// deny comparison
[<NoEquality; NoComparison>]
type CustomerAccount = {CustomerAccountId: int}

let x = {CustomerAccountId = 1}

x = x       // error!
x.CustomerAccountId = x.CustomerAccountId // no error
```

{% include book_page_ddd_img.inc %}
