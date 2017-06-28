---
layout: post
title: "Understanding F# types: Introduction"
description: "A new world of types"
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 1
---

*NOTE: Before reading this series, I suggest that you read the ["thinking functionally"](/series/thinking-functionally.html) and ["expressions and syntax"](/series/expressions-and-syntax.html) series as a prerequisite.* 


F# is not just about functions; the powerful type system is another key ingredient.  And just as with functions, understanding the type system is critical to being fluent and comfortable in the language.

Now, so far we have seen some basic types that can be used as input and output to functions:

* 	Primitive types such as `int`, `float`, `string`, and `bool`
* 	Simple function types such as `int->int`
* 	The `unit` type
* 	Generic types.

None of these types should be unfamiliar. Analogues of these are available in C# and other imperative languages. 

But in this series we are going to introduce some new kinds of types that are very common in functional languages but uncommon in imperative languages. 

The extended types we will look at in this series are:

* 	Tuples
* 	Records
* 	Unions
* 	The Option type
* 	Enum types

For all these types, we will discuss both the abstract principles and the details of how to use them in practice. 

Lists and other recursive data types are also very important types, but there is so much to say about them that they will need their own series!
