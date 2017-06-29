---
layout: post
title: "Parameter and value naming conventions"
description: "a, f, x and friends"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 6
---

If you are coming to F# from an imperative language such as C#, then you might find a lot of the names shorter and more cryptic than you are used to.  

In C# and Java, the best practice is to have long descriptive identifiers.  In functional languages, the function names themselves can be descriptive, but the local identifiers inside a function tend to be quite short, and piping and composition is used a lot to get everything on a minimal number of lines.

For example, here is a crude implementation of a prime number sieve with very descriptive names for the local values.

```fsharp
let primesUpTo n = 
    // create a recursive intermediate function
    let rec sieve listOfNumbers  = 
        match listOfNumbers with 
        | [] -> []
        | primeP::sievedNumbersBiggerThanP-> 
            let sievedNumbersNotDivisibleByP = 
                sievedNumbersBiggerThanP
                |> List.filter (fun i-> i % primeP > 0)
            //recursive part
            let newPrimes = sieve sievedNumbersNotDivisibleByP
            primeP :: newPrimes
    // use the sieve
    let listOfNumbers = [2..n]
    sieve listOfNumbers     // return

//test
primesUpTo 100
```

Here is the same implementation, with terser, idiomatic names and more compact code:

```fsharp
let primesUpTo n = 
   let rec sieve l  = 
      match l with 
      | [] -> []
      | p::xs -> 
            p :: sieve [for x in xs do if (x % p) > 0 then yield x]
   [2..n] |> sieve 
```

The cryptic names are not always better, of course, but if the function is kept to a few lines and the operations used are standard, then this is a fairly common idiom.

The common naming conventions are as follows:

* "a", "b", "c" etc., are types
* "f", "g", "h" etc., are functions
* "x", "y", "z" etc., are arguments to the functions 
* Lists are indicated by adding an "s" suffix, so that "`xs`" is a list of `x`'s, "`fs`" is a list of functions, and so on.  It is extremely common to see "`x::xs`" meaning the head (first element) and tail (the remaining elements) of a list.
* "_" is used whenever you don't care about the value. So "`x::_`" means that you don't care about the rest of the list, and "`let f _ = something`" means you don't care about the argument to `f`.

Another reason for the short names is that often, they cannot be assigned to anything meaningful.  For example, the definition of the pipe operator is:

```fsharp
let (|>) x f = f x
```

We don't know what `f` and `x` are going to be, `f` could be any function and `x` could be any value. Making this explicit does not make the code any more understandable.

```fsharp
let (|>) aValue aFunction = aFunction aValue // any better?
```

### The style used on this site 

On this site I will use both styles.  For the introductory series, when most of the concepts are new, I will use a very descriptive style, with intermediate values and long names.  But in more advanced series, the style will become terser.
