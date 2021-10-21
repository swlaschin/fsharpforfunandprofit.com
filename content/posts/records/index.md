---
layout: post
title: "Records"
description: "Extending tuples with labels"
date: 2012-06-05
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 5
categories: [Types]
---

As we noted in the previous post, plain tuples are useful in many cases. But they have some disadvantages too. Because all tuple types are pre-defined, you can't distinguish between a pair of floats used for geographic coordinates say, vs. a similar tuple used for complex numbers.  And when tuples have more than a few elements, it is easy to get confused about which element is in which place.

In these situations, what you would like to do is *label* each slot in the tuple, which will both document what each element is for and force a distinction between tuples made from the same types.

Enter the "record" type. A record type is exactly that, a tuple where each element is labeled.

```fsharp {src=#intro1}
type ComplexNumber = { Real: float; Imaginary: float }
type GeoCoord = { Lat: float; Long: float }
```

A record type has the standard preamble: `type [typename] =` followed by curly braces. Inside the curly braces is a list of `label: type` pairs, separated by semicolons (remember, all lists in F# use semicolon separators -- commas are for tuples).

Let's compare the "type syntax" for a record type with a tuple type:

```fsharp {src=#intro2}
type ComplexNumberRecord = { Real: float; Imaginary: float }
type ComplexNumberTuple = float * float
```

In the record type, there is no "multiplication", just a list of labeled types.

{{<alertinfo>}}
Relational database theory uses a similar "record type" concept. In the relational model, a relation is a (possibly empty) finite set of tuples all having the same finite set of attributes. This set of attributes is familiarly referred to as the set of column names.
{{</alertinfo>}}

## Making and matching records

To create a record value, use a similar format to the type definition, but using equals signs after the labels. This is called a "record expression."

```fsharp {src=#construct1}
type ComplexNumberRecord = { Real: float; Imaginary: float }
let myComplexNumber = { Real = 1.1; Imaginary = 2.2 } // use equals!

type GeoCoord = { Lat: float; Long: float } // use colon in type
let myGeoCoord = { Lat = 1.1; Long = 2.2 }  // use equals in let
```

And to "deconstruct" a record, use the same syntax:

```fsharp {src=#deconstruct1}
let myGeoCoord = { Lat = 1.1; Long = 2.2 }   // "construct"
let { Lat=myLat; Long=myLong } = myGeoCoord  // "deconstruct"
```

As always, if you don't need some of the values, you can use the underscore as a placeholder; or more cleanly, just leave off the unwanted label altogether.

```fsharp {src=#deconstruct2}
let { Lat=_; Long=myLong2 } = myGeoCoord  // "deconstruct"
let { Long=myLong3 } = myGeoCoord         // "deconstruct"
```

If you just need a single property, you can use dot notation rather than pattern matching.

```fsharp {src=#deconstruct3}
let x = myGeoCoord.Lat
let y = myGeoCoord.Long
```

Note that you can leave a label off when deconstructing, but not when constructing:

```fsharp {src=#construct2}
let myGeoCoord = { Lat = 1.1; }  // error FS0764: No assignment
  // given for field 'Long'
```

{{<alertinfo>}}
One of the most noticeable features of record types is use of curly braces. Unlike C-style languages, curly braces are rarely used in F# -- only for records, sequences, computation expressions (of which sequences are a special case), and object expressions (creating implementations of interfaces on the fly). These other uses will be discussed later.
{{</alertinfo>}}

### Label order

Unlike tuples, the order of the labels is not important. So the following two values are the same:

```fsharp {src=#labelOrder}
let myGeoCoordA = { Lat = 1.1; Long = 2.2 }
let myGeoCoordB = { Long = 2.2; Lat = 1.1 }   // same as above
```

### Naming conflicts

In the examples above, we could construct a record by just using the label names "`lat`" and "`long`". Magically, the compiler knew what record type to create. (Well, in truth, it was not really that magical, as only one record type had those exact labels.)

But what happens if there are two record types with the same labels? How can the compiler know which one you mean?  The answer is that it can't -- it will use the most recently defined type, and in some cases, issue a warning.  Try evaluating the following:

```fsharp {src=#namingConflicts1}
type Person1 = {First:string; Last:string}
type Person2 = {First:string; Last:string}
let p = {First="Alice"; Last="Jones"} //
```

What type is `p`?  Answer: `Person2`, which was the last type defined with those labels.

And if you try to deconstruct, you will get a warning about ambiguous field labels.

```fsharp {src=#namingConflicts2}
let {First=f; Last=l} = p
// warning FS0667: The labels of this record do not
//   uniquely determine a corresponding record type
```

How can you fix this? Simply by adding the type name as a qualifier to at least one of the labels.

```fsharp {src=#namingConflicts3}
let p = {Person1.First="Alice"; Last="Jones"}
//  ^Person1
```

If needed, you can even add a fully qualified name (with namespace). Here's an example using [modules](/posts/organizing-functions/).

```fsharp {src=#namingConflicts4}
module Module1 =
  type Person = {First:string; Last:string}

module Module2 =
  type Person = {First:string; Last:string}

let p =
    {Module1.Person.First="Alice"; Module1.Person.Last="Jones"}
```

Alternatively, you can add an explicit type annotation so that the compiler knows what type the record is:

```fsharp {src=#namingConflicts4b}
let p : Module1.Person =
  {First="Alice"; Last="Jones"}
```

Of course, if you can ensure there is only one version in the local namespace, you can avoid having to do this at all.

```fsharp {src=#namingConflicts5}
module Module3 =
  open Module1  // bring only one definition into scope
  let p = {First="Alice"; Last="Jones"} // will be Module1.Person
```

The moral of the story is that when defining record types, you should try to use unique labels if possible, otherwise you will get ugly code at best, and unexpected behavior at worst.

{{<alertinfo>}}
Note that in F#, unlike some other functional languages, two types with exactly the same structural definition are not the same type. This is called a "nominal" type system, where two types are only equal if they have the same name, as opposed to a "structural" type system, where definitions with identical structures will be the same type regardless of what they are called.
{{</alertinfo>}}

{{< book_page_ddd >}}

## Using records in practice

How can we use records? Let us count the ways...

### Using records for function results

Just like tuples, records are useful for passing back multiple values from a function. Let's revisit the tuple examples described earlier, rewritten to use records instead:

```fsharp {src=#practice1}
// the tuple version of TryParse
let tryParseTuple intStr =
  try
    let i = System.Int32.Parse intStr
    (true,i)
  with _ -> (false,0)  // any exception

// for the record version, create a type to hold the return result
type TryParseResult = {Success:bool; Value:int}

// the record version of TryParse
let tryParseRecord intStr =
  try
    let i = System.Int32.Parse intStr
    {Success=true;Value=i}
  with _ -> {Success=false;Value=0}

//test it
tryParseTuple "99"
tryParseRecord "99"
tryParseTuple "abc"
tryParseRecord "abc"
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
let g2 = {g1 with Lat=99.9}   // create a new one

let p1 = {First="Alice"; Last="Jones"}
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
