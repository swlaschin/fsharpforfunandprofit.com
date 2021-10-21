---
layout: post
title: "Anonymous Records"
description: ""
date: 2021-07-21
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 6
categories: [Types]
draft: true
---

In the [previous post on records](../records) we discussed how to define and use standard record types. But in some situations you may not know in advance what the structure of the record will be, and so you will not be able to explictly define a record type.

Enter "anonymous records". An anonymous record is a record that does not have a explicit type definition. It is defined "on-the-fly" as a value is created. Anonymous records are defined similarly to normal ("named") records, with curly braces, but have a vertical bar in addition.

Here's a named record along with an anonymous record:


```fsharp {src=#construct1}
// a named record with an explicit type definition
type Person = {First:string; Last:string}
let person = {First="Alice"; Last="Jones"}
// output:
//   val person : Person = ...

// an anonymous record without a type definition
let contact = {|Name="Alice"; Email="a@example.com"|}
// output:
//   val contact : {| Email: string; Name: string |} = ...
```

If you hover over `person` and `contact` or execute the code interactively, you can see the types of the two values.
The type of `person` has a name (`Person`), but the type of `contact` does not have a name, so the structure is shown instead.


## Operating on anonymous records

In many cases, anonymous records can be used just like named records, for example, you can dot into the fields, but you cannot pattern match with "let":

```fsharp {src=#deconstruct1}
let myGeoCoord = {| Lat = 1.1; Long = 2.2 |}
// dotting works
let lat = myGeoCoord.Lat
let long = myGeoCoord.Long

// pattern matching does NOT work
let {| Lat=myLat; Long=myLong |} = myGeoCoord
//   ^--- ERROR Unexpected symbol '{|' in binding
```

You can copy-and-update using `with`:

```fsharp {src=#copywith1}
let c1 = {| Name="Alice"; Email="a@example.com" |}
let c2 = {| c1 with Name="Bob" |}
```


You can also add new fields to named records by using `with` in conjunction with anonymous record syntax. For example, we can add an `Email` field to the original `Person` from above.

```fsharp {src=#copywith1}
let c1 = {| Name="Alice"; Email="a@example.com" |}
let c2 = {| c1 with Name="Bob" |}
```

You can also serialize anonymous records, and they are designed to interop cleanly with C# anonymous types.

## The "type" of anonymous records

Behind the scenes, anonymous records *do* have a type, based on their structure. This means that two anonymous records with the same structure are the same type so they can be compared, etc. And just like named record types, anonymous records are equal if all their fields are equal.

```fsharp {src=#type1}
// Define an anonymous record
let a = {| Id="A"; Email="a@example.com" |}

// Define another anonymous record
// It is the same type as `a`
let b = {| Id="B"; Email="b@example.com" |}
printfn "a=b is %b" (a=b)  // a=b is true
```

In fact, the "name" of the type *is* the structure definition, and this "name" (the structure definition) can be used in type annotations, function parameters, etc. Anywhere you would use a named record type, you can use an anonymous type "name", as you can see the example below.

```fsharp {src=#type2}
let a1 = {| Id="A"; Email="a@example.com" |}
// use the structure definition as the "name" of the type

// here's the type "name" used to annotate a value
let a2 : {| Id:string; Email:string |} = a1 // a2 is same type as a1

// here's the type "name" used to annotate a function parameter
let myFunc (x:{| Id:string; Email:string |}) =
  printfn "x is %A" x

// this function can be called with any value of the same type
myFunc a2
```

Note that anonymous records are not related even if they have some fields in common, or one is created from another. An anonymous record with a new field is not a superset or subset of the original. They are different types.

```fsharp {src=#type3}
// Define an anonymous record
let a = {| Id="A" |}

// Define another anonymous record
// It is NOT the same type as `a`
let b : {| Id:string; Email:string |} = a
  //  ERROR: This anonymous record does not have enough fields.
  //  Add the missing fields [Email].

// Define another anonymous record based on `a`
// It is NOT the same type as `a`
let c = {| a with Email="a@example.com"|}
printfn "a=c is %b" (a=c)  // error
  // ERROR: This anonymous record has too many fields.
  // Remove the extra fields [Email].
```

## What are anonymous records good for?

How can we use records? Let us count the ways...

### Using records for function results

Just like tuples, records are useful for passing back multiple values from a function. Let's revisit the tuple examples described earlier, rewritten to use records instead:

```fsharp {src=#practice1}
// the tuple version of TryParse
let tryParseTuple intStr =
  try
    let i = System.Int32.Parse intStr
    (true,i)
  with _ ->
    (false,0)  // any exception

// for the record version, create a type to hold the return result
type TryParseResult = {Success:bool; Value:int}

// the record version of TryParse
let tryParseRecord intStr =
  try
    let i = System.Int32.Parse intStr
    {Success=true;Value=i}
  with _ ->
    {Success=false;Value=0}

//test it
tryParseTuple "99"   // (true, 99)
tryParseRecord "99"  // { Success = true; Value = 99 }
tryParseTuple "abc"  // (false, 0)
tryParseRecord "abc" // { Success = false; Value = 0 }
```

You can see that having explicit labels in the return value makes it much easier to understand (of course, in practice we would probably use an `Option` type, discussed in later post).

And here's the word and letter count example using records rather than tuples:

```fsharp {src=#practice2}
//define return type
type WordAndLetterCountResult = {WordCount:int; LetterCount:int}

let wordAndLetterCount (s:string) =
  let words = s.Split [|' '|]
  let letterCount = words |> Array.sumBy (fun word -> word.Length )
  {WordCount=words.Length; LetterCount=letterCount}

//test
wordAndLetterCount "to be or not to be"
  // { WordCount = 6; LetterCount = 13 }
```

### Creating records from other records

Again, as with most F# values, records are immutable and the elements within them cannot be assigned to.  So how do you change a record? Again the answer is that you can't -- you must always create a new one.

Say that you need to write a function that, given a `GeoCoord` record, adds one to each element. Here it is:

```fsharp {src=#practice3}
let addOneToGeoCoord aGeoCoord =
  let {Lat=x; Long=y} = aGeoCoord
  {Lat = x + 1.0; Long = y + 1.0}   // create a new one

// try it
addOneToGeoCoord {Lat=1.1; Long=2.2}
```

But again you can simplify by deconstructing directly in the parameters of a function, so that the function becomes a one liner:

```fsharp {src=#practice4}
let addOneToGeoCoord {Lat=x; Long=y} = {Lat=x+1.0; Long=y+1.0}

// try it
addOneToGeoCoord {Lat=1.0; Long=2.0}
```

or depending on your taste, you can also use dot notation to get the properties:

```fsharp {src=#practice5}
let addOneToGeoCoord aGeoCoord =
  {Lat=aGeoCoord.Lat + 1.0; Long= aGeoCoord.Long + 1.0}
```

In many cases, you just need to tweak one or two fields and leave all the others alone. To make life easier, there is a special syntax for this common case, the "`with`" keyword. You start with the original value, followed by "with" and then the fields you want to change. Here are some examples:

```fsharp {src=#practice6}
let g1 = {Lat=1.1; Long=2.2}
// create a new record based on g1
let g2 = {g1 with Lat=99.9}

let p1 = {First="Alice"; Last="Jones"}
// create a new record based on p1
let p2 = {p1 with Last="Smith"}
```

The technical term for "with" is a copy-and-update record expression.

### Record equality

Like tuples, records have an automatically defined equality operation: two records are equal if they have the same type and the values in each slot are equal.

```fsharp {src=#equality1}
let p1 = {First="Alice"; Last="Jones"}
let p2 = {First="Alice"; Last="Jones"}
printfn "p1=p2 is %b" (p1=p2)  // p1=p2 is true
```

And records also have an automatically defined hash value based on the values in the record, so that records can be used in a hashed collection without any problems.

```fsharp {src=#equality2}
let h1 = {First="Alice"; Last="Jones"}.GetHashCode()
let h2 = {First="Alice"; Last="Jones"}.GetHashCode()
printfn "h1=h2 is %b" (h1=h2)  // h1=h2 is true
```

### Record representation

As noted in a [previous post](/posts/convenience-types/), records have a nice default string representation, and can be serialized easily. The default `ToString()` implementation uses this same representation.

```fsharp {src=#print1}
let p = {First="Alice"; Last="Jones"}
printfn "%A" p
// output:
//   { First = "Alice"
//     Last = "Jones" }
printfn "%O" p   // same as above
```

## Sidebar: %A vs. %O in print format strings

We just saw that print format specifiers `%A` and `%O` produce the same results. So why the difference?

`%A` prints the value using the same pretty printer that is used for interactive output. But `%O` uses `ToString()`, which means that if the `ToString` method is not overridden, `%O` will give the default (sometimes unhelpful) output.  So in general, you should try to use `%A` instead of `%O` for user-defined types unless you want to override ToString().

```fsharp {src=#print2}
type Person = {First:string; Last:string}
  with
  override this.ToString() = sprintf "%s %s" this.First this.Last

printfn "%A" {First="Alice"; Last="Jones"}
// output:
//   { First = "Alice"
//     Last = "Jones" }
printfn "%O" {First="Alice"; Last="Jones"}
// output:
//   "Alice Jones"
```

But note that the F# "class" types do *not* have a standard pretty printed format, so `%A` and `%O` are equally unhelpful unless you override `ToString()`.
