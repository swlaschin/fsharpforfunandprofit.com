---
layout: post
title: "Discriminated Unions"
description: "Adding types together"
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 6
categories: [Types]
---


Tuples and records are examples of creating new types by "multiplying" existing types together.  At the beginning of the series, I mentioned that the other way of creating new types was by "summing" existing types. What does this mean?

Well, let's say that we want to define a function that works with integers OR booleans, maybe to convert them into strings.  But we want to be strict and not accept any other type (such as floats or strings). Here's a diagram of such as function:

![function from int union bool](/assets/img/fun_int_union_bool.png)
 
How could we represent the domain of this function?

What we need is a type that represents all possible integers PLUS all possible booleans. 
 
![int union bool](/assets/img/int_union_bool.png)
 
In other words, a "sum" type. In this case the new type is the "sum" of the integer type plus the boolean type.

In F#, a sum type is called a "discriminated union" type.  Each component type (called a *union case*) must be tagged with a label (called a *case identifier* or *tag*) so that they can be told apart ("discriminated"). The labels can be any identifier you like, but must start with an uppercase letter.

Here's how we might define the type above:

```fsharp
type IntOrBool = 
  | I of int
  | B of bool
```

The "I" and the "B" are just arbitrary labels; we could have used any other labels that were meaningful.

For small types, we can put the definition on one line:

```fsharp
type IntOrBool = I of int | B of bool
```

The component types can be any other type you like, including tuples, records, other union types, and so on.

```fsharp
type Person = {first:string; last:string}  // define a record type 
type IntOrBool = I of int | B of bool

type MixedType = 
  | Tup of int * int  // a tuple
  | P of Person       // use the record type defined above
  | L of int list     // a list of ints
  | U of IntOrBool    // use the union type defined above
```

You can even have types that are recursive, that is, they refer to themselves. This is typically how tree structures are defined. Recursive types will be discussed in more detail shortly.

### Sum types vs. C++ unions and VB variants

At first glance, a sum type might seem similar to a union type in C++ or a variant type in Visual Basic, but there is a key difference. The union type in C++ is not type-safe and the data stored in the type can be accessed using any of the possible tags.  An F# discriminated union type is safe, and the data can only be accessed one way.  It really is helpful to think of it as a sum of two types (as shown in the diagram), rather than as just an overlay of data.

## Key points about union types

Some key things to know about union types are:

* 	The vertical bar is optional before the first component, so that the following definitions are all equivalent, as you can see by examining the output of the interactive window:

```fsharp
type IntOrBool = I of int | B of bool     // without initial bar
type IntOrBool = | I of int | B of bool   // with initial bar
type IntOrBool = 
   | I of int 
   | B of bool      // with initial bar on separate lines
```

* 	The tags or labels must start with an uppercase letter. So the following will give an error:

```fsharp
type IntOrBool = int of int| bool of bool
//  error FS0053: Discriminated union cases 
//                must be uppercase identifiers
```

* 	Other named types (such as `Person` or `IntOrBool`) must be pre-defined outside the union type.  You can't define them "inline" and write something like this:

```fsharp
type MixedType = 
  | P of  {first:string; last:string}  // error
```

or

```fsharp
type MixedType = 
  | U of (I of int | B of bool)  // error
```

* 	The labels can be any identifier, including the names of the component type themselves, which can be quite confusing if you are not expecting it. For example, if the `Int32` and `Boolean` types (from the `System` namespace) were used instead, and the labels were named the same, we would have this perfectly valid definition:

```fsharp
open System
type IntOrBool = Int32 of Int32 | Boolean of Boolean
```

This "duplicate naming" style is actually quite common, because it documents exactly what the component types are.

{% include book_page_pdf.inc %}

## Constructing a value of a union type

To create a value of a union type, you use a "constructor" that refers to only one of the possible union cases. The constructor then follows the form of the definition, using the case label as if it were a function. In the `IntOrBool` example, you would write:

```fsharp
type IntOrBool = I of int | B of bool

let i  = I 99    // use the "I" constructor
// val i : IntOrBool = I 99

let b  = B true  // use the "B" constructor
// val b : IntOrBool = B true
```

The resulting value is printed out with the label along with the component type:

```fsharp
val [value name] : [type]    = [label] [print of component type]
val i            : IntOrBool = I       99
val b            : IntOrBool = B       true
```

If the case constructor has more than one "parameter", you construct it in the same way that you would call a function:

```fsharp
type Person = {first:string; last:string}

type MixedType = 
  | Tup of int * int
  | P of Person

let myTup  = Tup (2,99)    // use the "Tup" constructor
// val myTup : MixedType = Tup (2,99)

let myP  = P {first="Al"; last="Jones"} // use the "P" constructor
// val myP : MixedType = P {first = "Al";last = "Jones";}
```

The case constructors for union types are normal functions, so you can use them anywhere a function is expected. For example, in `List.map`:

```fsharp
type C = Circle of int | Rectangle of int * int

[1..10]
|> List.map Circle

[1..10]
|> List.zip [21..30]
|> List.map Rectangle
```

### Naming conflicts

If a particular case has a unique name, then the type to construct will be unambiguous. 

But what happens if you have two types which have cases with the same labels? 

```fsharp
type IntOrBool1 = I of int | B of bool
type IntOrBool2 = I of int | B of bool
```

In this case, the last one defined is generally used:

```fsharp
let x = I 99                // val x : IntOrBool2 = I 99
```

But it is much better to explicitly qualify the type, as shown:

```fsharp
let x1 = IntOrBool1.I 99    // val x1 : IntOrBool1 = I 99
let x2 = IntOrBool2.B true  // val x2 : IntOrBool2 = B true
```

And if the types come from different modules, you can use the module name as well:

```fsharp
module Module1 = 
  type IntOrBool = I of int | B of bool

module Module2 = 
  type IntOrBool = I of int | B of bool

module Module3 =
  let x = Module1.IntOrBool.I 99 // val x : Module1.IntOrBool = I 99
```


### Matching on union types

For tuples and records, we have seen that "deconstructing" a value uses the same model as constructing it.  This is also true for union types, but we have a complication: which case should we deconstruct?

This is exactly what the "match" expression is designed for. As you should now realize, the match expression syntax has parallels to how a union type is defined.

```fsharp
// definition of union type
type MixedType = 
  | Tup of int * int
  | P of Person

// "deconstruction" of union type
let matcher x = 
  match x with
  | Tup (x,y) -> 
        printfn "Tuple matched with %i %i" x y
  | P {first=f; last=l} -> 
        printfn "Person matched with %s %s" f l

let myTup = Tup (2,99)                 // use the "Tup" constructor
matcher myTup  

let myP = P {first="Al"; last="Jones"} // use the "P" constructor
matcher myP
```

Let's analyze what is going on here:  

* 	Each "branch" of the overall match expression is a pattern expression that is designed to match the corresponding case of the union type.
* 	The pattern starts with the tag for the particular case, and then the rest of the pattern deconstructs the type for that case in the usual way.
* 	The pattern is followed by an arrow "->" and then the code to execute.


## Empty cases

The label for a union case does not have to have to have any type after it. The following are all valid union types:

```fsharp
type Directory = 
  | Root                   // no need to name the root
  | Subdirectory of string // other directories need to be named 

type Result = 
  | Success                // no string needed for success state
  | ErrorMessage of string // error message needed 
```

If *all* the cases are empty, then we have an "enum style" union:

```fsharp
type Size = Small | Medium | Large
type Answer = Yes | No | Maybe
```

Note that this "enum style" union is *not* the same as a true C# enum type, discussed later.

To create an empty case, just use the label as a constructor without any parameters:

```fsharp
let myDir1 = Root
let myDir2 = Subdirectory "bin"

let myResult1 = Success
let myResult2 = ErrorMessage "not found"

let mySize1 = Small
let mySize2 = Medium
```

<a id="single-case"></a>

## Single cases

Sometimes it is useful to create union types with only one case. This might be seem useless, because you don't seem to be adding value. But in fact, this a very useful practice that can enforce type safety*.

<sub>* And in a future series we'll see that, in conjuction with module signatures, single case unions can also help with data hiding and capability based security.<sub>

For example, let's say that we have customer ids and order ids which are both represented by integers, but that they should never be assigned to each other.

As we saw before, a type alias approach will not work, because an alias is just a synonym and doesn't create a distinct type.  Here's how you might try to do it with aliases:

```fsharp
type CustomerId = int   // define a type alias
type OrderId = int      // define another type alias

let printOrderId (orderId:OrderId) = 
   printfn "The orderId is %i" orderId

//try it
let custId = 1          // create a customer id
printOrderId custId   // Uh-oh! 
```

But even though I explicitly annotated the `orderId` parameter to be of type `OrderId`, I can't ensure that customer ids are not accidentally passed in.

On the other hand, if we create simple union types, we can easily enforce the type distinctions.

```fsharp
type CustomerId = CustomerId of int   // define a union type 
type OrderId = OrderId of int         // define another union type 

let printOrderId (OrderId orderId) =  // deconstruct in the param
   printfn "The orderId is %i" orderId

//try it
let custId = CustomerId 1             // create a customer id
printOrderId custId                   // Good! A compiler error now.
```

This approach is feasible in C# and Java as well, but is rarely used because of the overhead of creating and managing the special classes for each type.  In F# this approach is lightweight and therefore quite common.

A convenient thing about single case union types is you can pattern match directly against a value without having to use a full `match-with` expression.

```fsharp
// deconstruct in the param
let printCustomerId (CustomerId customerIdInt) =     
   printfn "The CustomerId is %i" customerIdInt

// or deconstruct explicitly through let statement
let printCustomerId2 custId =     
   let (CustomerId customerIdInt) = custId  // deconstruct here
   printfn "The CustomerId is %i" customerIdInt

// try it
let custId = CustomerId 1             // create a customer id
printCustomerId custId                   
printCustomerId2 custId                   
```

But a common "gotcha" is that in some cases, the pattern match must have parens around it, otherwise the compiler will think you are defining a function!

```fsharp
let custId = CustomerId 1                
let (CustomerId customerIdInt) = custId  // Correct pattern matching
let CustomerId customerIdInt = custId    // Wrong! New function?
```

Similarly, if you ever do need to create an enum-style union type with a single case, you will have to start the case with a vertical bar in the type definition; otherwise the compiler will think you are creating an alias.

```fsharp
type TypeAlias = A     // type alias!
type SingleCase = | A   // single case union type
```


## Union equality ##

Like other core F# types, union types have an automatically defined equality operation: two unions are equal if they have the same type and the same case and the values for that case is equal.

```fsharp
type Contact = Email of string | Phone of int

let email1 = Email "bob@example.com"
let email2 = Email "bob@example.com"

let areEqual = (email1=email2)
```


## Union representation ##

Union types have a nice default string representation, and can be serialized easily. But unlike tuples, the ToString() representation is unhelpful.

```fsharp
type Contact = Email of string | Phone of int
let email = Email "bob@example.com"
printfn "%A" email    // nice
printfn "%O" email    // ugly!
```

