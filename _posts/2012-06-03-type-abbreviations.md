---
layout: post
title: "Type abbreviations"
description: "Also known as aliases"
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 3
---

Let's start with the simplest type definition, a type abbreviation or alias.

It has the form:

```fsharp
type [typename] = [existingType]
```

where "existing type" can be any type: one of the basic types we have already seen, or one of the extended types we will be seeing soon.

Some examples:

```fsharp
type RealNumber = float
type ComplexNumber = float * float
type ProductCode = string
type CustomerId = int
type AdditionFunction = int->int->int
type ComplexAdditionFunction = 
       ComplexNumber-> ComplexNumber -> ComplexNumber
```

And so on -- pretty straightforward.  

Type abbreviations are useful to provide documentation and avoid writing a signature repeatedly.  In the above examples, `ComplexNumber` and `AdditionFunction` demonstrate this.  

Another use is to provide some degree of decoupling between the usage of a type and the actual implementation of a type. In the above examples, `ProductCode` and `CustomerId` demonstrate this.  I could easily change `CustomerId` to be a string without changing (most of) my code.

However, one thing is to note is that this really is just an alias or abbreviation; you are not actually creating a new type. So if I define a function that I explicitly say is an `AdditionFunction`:

```fsharp
type AdditionFunction = int->int->int
let f:AdditionFunction = fun a b -> a + b
```

the compiler will erase the alias and return a plain `int->int->int` as the function signature.

In particular, there is no true encapsulation. I could use an explicit `int` anywhere I used a `CustomerId` and the compiler would not complain. And if I had attempted to create safe versions of entity ids such as this:

```fsharp
type CustomerId = int
type OrderId = int
```

then I would be disappointed. There would be nothing preventing me from using an `OrderId` in place of a `CustomerId` and vice versa.  To get true encapsulated types like this, we will need to use single case union types, as described in a later post.
