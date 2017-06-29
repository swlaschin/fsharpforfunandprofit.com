---
layout: post
title: "Pattern matching for conciseness"
description: "Pattern matching can match and bind in a single step"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 12
categories: [Conciseness, Patterns]
---

So far we have seen the pattern matching logic in the `match..with` expression, where it seems to be just a switch/case statement. But in fact pattern matching is much more general -- it can compare expressions in a number of ways, matching on values, conditions, and types, and then assign or extract values, all at the same time.

Pattern matching will be discussed in depth in later posts, but to start with, here is a little taster of one way that it aids conciseness. 
We'll look at the way pattern matching is used for binding values to expressions (the functional equivalent of assigning to variables). 

In the following examples, we are binding to the internal members of tuples and lists directly:

```fsharp
//matching tuples directly
let firstPart, secondPart, _ =  (1,2,3)  // underscore means ignore

//matching lists directly
let elem1::elem2::rest = [1..10]       // ignore the warning for now

//matching lists inside a match..with
let listMatcher aList = 
    match aList with
    | [] -> printfn "the list is empty" 
    | [firstElement] -> printfn "the list has one element %A " firstElement 
    | [first; second] -> printfn "list is %A and %A" first second 
    | _ -> printfn "the list has more than two elements"

listMatcher [1;2;3;4]
listMatcher [1;2]
listMatcher [1]
listMatcher []
```

You can also bind values to the inside of complex structures such as records. In the following example, we will create an "`Address`" type, and then a "`Customer`" type which contains an address. Next, we will create a customer value, and then match various properties against it. 

```fsharp
// create some types
type Address = { Street: string; City: string; }   
type Customer = { ID: int; Name: string; Address: Address}   

// create a customer 
let customer1 = { ID = 1; Name = "Bob"; 
      Address = {Street="123 Main"; City="NY" } }     

// extract name only
let { Name=name1 } =  customer1 
printfn "The customer is called %s" name1

// extract name and id 
let { ID=id2; Name=name2; } =  customer1 
printfn "The customer called %s has id %i" name2 id2

// extract name and address
let { Name=name3;  Address={Street=street3}  } =  customer1   
printfn "The customer is called %s and lives on %s" name3 street3
```

In the last example, note how we could reach right into the `Address` substructure and pull out the street as well as the customer name.  

This ability to process a nested structure, extract only the fields you want, and assign them to values, all in a single step, is very useful.  It removes quite a bit of coding drudgery, and is another factor in the conciseness of typical F# code.
