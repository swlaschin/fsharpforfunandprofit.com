---
layout: post
title: "Units of measure"
description: "Type safety for numerics"
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 11
---

As we mentioned [earlier in the "why use F#?" series](/posts/correctness-type-checking/#units-of-measure), F# has a very cool feature which allows you to add extra unit-of-measure information to as metadata to numeric types. 

The F# compiler will then make sure that only numerics with the same unit-of-measure can be combined. This can be very useful to stop accidental mismatches and to make your code safer.

## Defining units of measure

A unit of measure definition consists of the attribute `[<Measure>]`, followed by the `type` keyword and then a name. For example:

```fsharp
[<Measure>] 
type cm

[<Measure>] 
type inch
``` 

Often you will see the whole definition written on one line instead:

```fsharp
[<Measure>] type cm
[<Measure>] type inch
``` 

Once you have a definition, you can associate a measure type with a numeric type by using angle brackets with measure name inside:

```fsharp
let x = 1<cm>    // int
let y = 1.0<cm>  // float
let z = 1.0m<cm> // decimal 
``` 

You can even combine measures within the angle brackets to create compound measures:

```fsharp
[<Measure>] type m
[<Measure>] type sec
[<Measure>] type kg

let distance = 1.0<m>    
let time = 2.0<sec>    
let speed = 2.0<m/sec>    
let acceleration = 2.0<m/sec^2>    
let force = 5.0<kg m/sec^2>    
``` 

### Derived units of measure

If you use certain combinations of units a lot, you can define a *derived* measure and use that instead.

```fsharp
[<Measure>] type N = kg m/sec^2

let force1 = 5.0<kg m/sec^2>    
let force2 = 5.0<N>

force1 = force2 // true
``` 

### SI units and constants

If you are using the units-of-measure for physics or other scientific applications, you will definitely want to use the SI units and related constants. You don't need to define all these yourself! These are predefined for you and available as follows:

* In F# 3.0 and higher (which shipped with Visual Studio 2012), these are built into the core F# libraries in the `Microsoft.FSharp.Data.UnitSystems.SI` namespace (see the [MSDN page](http://msdn.microsoft.com/en-us/library/hh289707.aspx)). 
* In F# 2.0 (which shipped with Visual Studio 2010), you will have to install the F# powerpack to get them. (The F# powerpack is on Codeplex at http://fsharppowerpack.codeplex.com).


## Type checking and type inference

The units-of-measure are just like proper types; you get static checking *and* type inference.

```fsharp
[<Measure>] type foot
[<Measure>] type inch

let distance = 3.0<foot>    

// type inference for result
let distance2 = distance * 2.0

// type inference for input and output
let addThreeFeet ft = 
    ft + 3.0<foot>    
``` 

And of course, when using them, the type checking is strict:

```fsharp
addThreeFeet 1.0        //error
addThreeFeet 1.0<inch>  //error
addThreeFeet 1.0<foot>  //OK
``` 



### Type annotations

If you want to be explicit in specifying a unit-of-measure type annotation, you can do so in the usual way. 
The numeric type must have angle brackets with the unit-of-measure.

```fsharp
let untypedTimesThree (ft:float) = 
    ft * 3.0

let footTimesThree (ft:float<foot>) = 
    ft * 3.0
``` 

    
### Combining units of measure with multiplication and division

The compiler understands how units of measure transform when individual values are multiplied or divided.  
For example, in the following, the `speed` value has been automatically given the measure `<m/sec>`.

```fsharp
[<Measure>] type m
[<Measure>] type sec
[<Measure>] type kg

let distance = 1.0<m>    
let time = 2.0<sec>    
let speed = distance/time 
let acceleration = speed/time
let mass = 5.0<kg>    
let force = mass * speed/time
``` 

Look at the types of the `acceleration` and `force` values above to see other examples of how this works.


## Dimensionless values

A numeric value without any specific unit of measure is called *dimensionless*. If you want to be explicit that a value is dimensionless, you can use the measure called `1`.

```fsharp
// dimensionless
let x = 42

// also dimensionless
let x = 42<1>
``` 

### Mixing units of measure with dimensionless values

Note that you cannot *add* a dimensionless value to a value with a unit of measure, but you can *multiply or divide* by dimensionless values.

```fsharp
// test addition
3.0<foot> + 2.0<foot>  // OK
3.0<foot> + 2.0        // error

// test multiplication
3.0<foot> * 2.0        // OK   
``` 

But see the section on "generics" below for an alternative approach.

## Conversion between units 

What if you need to convert between units?

It's straightforward. You first need to define a conversion value that uses *both* units, and then multiply the source value by the conversion factor.

Here's an example with feet and inches:

```fsharp
[<Measure>] type foot
[<Measure>] type inch

//conversion factor
let inchesPerFoot = 12.0<inch/foot>    

// test    
let distanceInFeet = 3.0<foot>    
let distanceInInches = distanceInFeet * inchesPerFoot 
``` 

And here's an example with temperature:

```fsharp
[<Measure>] type degC
[<Measure>] type degF

let convertDegCToF c = 
    c * 1.8<degF/degC> + 32.0<degF>

// test    
let f = convertDegCToF 0.0<degC>    
``` 

The compiler correctly inferred the signature of the conversion function.

```fsharp
val convertDegCToF : float<degC> -> float<degF>
``` 

Note that the constant `32.0<degF>` was explicitly annotated with the `degF` so that the result would be in `degF` as well. If you leave off this annotation, the result is a plain float, and the function signature changes to something much stranger! Try it and see:

```fsharp
let badConvertDegCToF c = 
    c * 1.8<degF/degC> + 32.0
``` 

### Conversion between dimensionless values and unit-of-measure values

To convert from a dimensionless numeric value to a value with a measure type, just multiply it by one, but with the one annotated with the appropriate unit.

```fsharp
[<Measure>] type foot

let ten = 10.0   // normal

//converting from non-measure to measure 
let tenFeet = ten * 1.0<foot>  // with measure
``` 

And to convert the other way, either divide by one, or multiply with the inverse unit.

```fsharp
//converting from measure to non-measure
let tenAgain = tenFeet / 1.0<foot>  // without measure
let tenAnotherWay = tenFeet * 1.0<1/foot>  // without measure
``` 

The above methods are type safe, and will cause errors if you try to convert the wrong type. 

If you don't care about type checking, you can do the conversion with the standard casting functions instead:

```fsharp
let tenFeet = 10.0<foot>  // with measure
let tenDimensionless = float tenFeet // without measure
``` 

## Generic units of measure

Often, we want to write functions that will work with any value, no matter what unit of measure is associated with it.

For example, here is our old friend `square`. But when we try to use it with a unit of measure, we get an error.

```fsharp
let square x = x * x

// test
square 10<foot>   // error
``` 

What can we do? We don't want to specify a particular unit of measure, but on the other hand we must specify *something*, because the simple definition above doesn't work.

The answer is to use *generic* units of measure, indicated with an underscore where the measure name normally is.

```fsharp
let square (x:int<_>) = x * x

// test
square 10<foot>   // OK
square 10<sec>   // OK
``` 

Now the `square` function works as desired, and you can see that the function signature has used the letter `'u` to indicate a generic unit of measure. 
And also note that the compiler has inferred that the return value is of type "unit squared".

```fsharp
val square : int<'u> -> int<'u ^ 2>
``` 


Indeed, you can specify the generic type using letters as well if you like:

```fsharp
// with underscores
let square (x:int<_>) = x * x

// with letters
let square (x:int<'u>) = x * x

// with underscores
let speed (distance:float<_>) (time:float<_>) = 
    distance / time

// with letters
let speed (distance:float<'u>) (time:float<'v>) = 
    distance / time
``` 

You may need to use letters sometimes to explicitly indicate that the units are the same:

```fsharp
let ratio (distance1:float<'u>) (distance2:float<'u>) = 
    distance1 / distance2
``` 


### Using generic measures with lists

You cannot always use a measure directly. For example, you cannot define a list of feet directly:

```fsharp
//error
[1.0<foot>..10.0<foot>]
``` 

Instead, you have to use the "multiply by one" trick mentioned above:

```fsharp
//converting using map -- OK
[1.0..10.0] |> List.map (fun i-> i * 1.0<foot>)

//using a generator -- OK
[ for i in [1.0..10.0] -> i * 1.0<foot> ]
``` 


### Using generic measures for constants

Multiplication by constants is OK (as we saw above), but if you try to do addition, you will get an error.

```fsharp
let x = 10<foot> + 1  // error
``` 

The fix is to add a generic type to the constant, like this:

```fsharp
let x = 10<foot> + 1<_>  // ok
``` 

A similar situation occurs when passing in constants to a higher order function such as `fold`.

```fsharp
let feet = [ for i in [1.0..10.0] -> i * 1.0<foot> ]

// OK
feet |> List.sum  

// Error
feet |> List.fold (+) 0.0   

// Fixed with generic 0
feet |> List.fold (+) 0.0<_>  
``` 

### Issues with generic measures with functions

There are some cases where type inference fails us. For example, let's try to create a simple `add1` function that uses units.

```fsharp
// try to define a generic function
let add1 n = n + 1.0<_>
// warning FS0064: This construct causes code to be less generic than 
// indicated by the type annotations. The unit-of-measure variable 'u 
// has been constrained to be measure '1'.
 
// test
add1 10.0<foot>   
// error FS0001: This expression was expected to have type float    
// but here has type float<foot>    
``` 

The warning message has the clue. The input parameter `n` has no measure, so the measure for `1<_>` will always be ignored. The `add1` function does not have a unit of measure so when you try to call it with a value that does have a measure, you get an error.

So maybe the solution is to explicitly annotate the measure type, like this:

```fsharp
// define a function with explicit type annotation
let add1 (n:float<'u>) : float<'u> =  n + 1.0<_>
``` 

But no, you get the same warning FS0064 again.

Maybe we can replace the underscore with something more explicit such as `1.0<'u>`?

```fsharp
let add1 (n:float<'u>) : float<'u> = n + 1.0<'u>  
// error FS0634: Non-zero constants cannot have generic units. 
``` 

But this time we get a compiler error!

The answer is to use one of the helpful utility functions in the LanguagePrimitives module: `FloatWithMeasure`, `Int32WithMeasure`, etc.

```fsharp
// define the function
let add1 n  = 
    n + (LanguagePrimitives.FloatWithMeasure 1.0)

// test
add1 10.0<foot>   // Yes!
``` 

And for generic ints, you can use the same approach:

```fsharp
open LanguagePrimitives

let add2Int n  = 
    n + (Int32WithMeasure 2)

add2Int 10<foot>   // OK
``` 

### Using generic measures with type definitions

That takes care of functions. What about when we need to use a unit of measure in a type definition?

Say we want to define a generic coordinate record that works with an unit of measure. Let's start with a naive approach:

```fsharp
type Coord = 
    { X: float<'u>; Y: float<'u>; }
// error FS0039: The type parameter 'u' is not defined
``` 

That didn't work, so what about adding the measure as a type parameter:

```fsharp
type Coord<'u> = 
    { X: float<'u>; Y: float<'u>; }
// error FS0702: Expected unit-of-measure parameter, not type parameter.
// Explicit unit-of-measure parameters must be marked with the [<Measure>] attribute.
``` 

That didn't work either, but the error message tells us what to do. Here is the final, correct version, using the `Measure` attribute:

```fsharp
type Coord<[<Measure>] 'u> = 
    { X: float<'u>; Y: float<'u>; }

// Test
let coord = {X=10.0<foot>; Y=2.0<foot>}
``` 

In some cases, you might need to define more than one measure. In the following example, the currency exchange rate is defined as the ratio of two currencies, and so needs two generic measures to be defined.
 
```fsharp
type CurrencyRate<[<Measure>]'u, [<Measure>]'v> = 
    { Rate: float<'u/'v>; Date: System.DateTime}

// test
[<Measure>] type EUR
[<Measure>] type USD
[<Measure>] type GBP

let mar1 = System.DateTime(2012,3,1)
let eurToUsdOnMar1 = {Rate= 1.2<USD/EUR>; Date=mar1 }
let eurToGbpOnMar1 = {Rate= 0.8<GBP/EUR>; Date=mar1 }

let tenEur = 10.0<EUR>
let tenEurInUsd = eurToUsdOnMar1.Rate * tenEur 
``` 

And of course, you can mix regular generic types with unit of measure types. 

For example, a product price might consist of a generic product type, plus a price with a currency:

```fsharp 
type ProductPrice<'product, [<Measure>] 'currency> = 
    { Product: 'product; Price: float<'currency>; }
``` 
    
### Units of measure at runtime

An issue that you may run into is that units of measure are not part of the .NET type system.  

F# does stores extra metadata about them in the assembly, but this metadata is only understood by F#.

This means that there is no (easy) way at runtime to determine what unit of measure a value has, nor any way to dynamically assign a unit of measure at runtime.

It also means that there is no way to expose units of measure as part of a public API to another .NET language (except other F# assemblies).


