---
layout: post
title: "Defining functions"
description: "Lambdas and more"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 8
categories: [Functions, Combinators]
---


We have seen how to create typical functions using the "let" syntax, below:

```fsharp
let add x y = x + y
```

In this section, we'll look at some other ways of creating functions, and tips for defining functions.

## Anonymous functions (a.k.a. lambdas) ##

If you are familiar with lambdas in other languages, this will not be new to you. An anonymous function (or "lambda expression") is defined using the form:

```fsharp
fun parameter1 parameter2 etc -> expression
```

If you are used to lambdas in C# there are a couple of differences:

* the lambda must have the special keyword `fun`, which is not needed in the C# version
* the arrow symbol is a single arrow `->` rather than the double arrow (`=>`) in C#.

Here is a lambda that defines addition:

```fsharp
let add = fun x y -> x + y
```

This is exactly the same as a more conventional function definition:

```fsharp
let add x y = x + y
```

Lambdas are often used when you have a short expression and you don't want to define a function just for that expression. This is particularly common with list operations, as we have seen already.

```fsharp
// with separately defined function
let add1 i = i + 1
[1..10] |> List.map add1

// inlined without separately defined function
[1..10] |> List.map (fun i -> i + 1)
```

Note that you must use parentheses around the lambda.

Lambdas are also used when you want to make it clear that you are returning a function from another function. For example, the "`adderGenerator`" function that we talked about earlier could be rewritten with a lambda.

```fsharp
// original definition
let adderGenerator x = (+) x

// definition using lambda
let adderGenerator x = fun y -> x + y
```

The lambda version is slightly longer, but makes it clear that an intermediate function is being returned.

You can nest lambdas as well. Here is yet another definition of `adderGenerator`, this time using lambdas only. 

```fsharp
let adderGenerator = fun x -> (fun y -> x + y)
```

Can you see that all three of the following definitions are the same thing?

```fsharp
let adderGenerator1 x y = x + y 
let adderGenerator2 x   = fun y -> x + y
let adderGenerator3     = fun x -> (fun y -> x + y)
```

If you can't see it, then do reread the [post on currying](/posts/currying/). This is important stuff to understand!

## Pattern matching on parameters ##

When defining a function, you can pass an explicit parameter, as we have seen, but you can also pattern match directly in the parameter section. In other words, the parameter section can contain *patterns*, not just identifiers!

The following example demonstrates how to use patterns in a function definition:

```fsharp
type Name = {first:string; last:string} // define a new type
let bob = {first="bob"; last="smith"}   // define a value 

// single parameter style
let f1 name =                       // pass in single parameter   
   let {first=f; last=l} = name     // extract in body of function 
   printfn "first=%s; last=%s" f l

// match in the parameter itself
let f2 {first=f; last=l} =          // direct pattern matching 
   printfn "first=%s; last=%s" f l 

// test
f1 bob
f2 bob
```

This kind of matching can only occur when the matching is always possible. For example, you cannot match on union types or lists this way, because some cases might not be matched.

```fsharp
let f3 (x::xs) =            // use pattern matching on a list
   printfn "first element is=%A" x
```

You will get a warning about incomplete pattern matches.

<a name="tuples"></a>

## A common mistake: tuples vs. multiple parameters ##

If you come from a C-like language, a tuple used as a single function parameter can look awfully like multiple parameters. They are not the same thing at all!   As I noted earlier, if you see a comma, it is probably part of a tuple. Parameters are separated by spaces.

Here is an example of the confusion:

```fsharp
// a function that takes two distinct parameters
let addTwoParams x y = x + y

// a function that takes a single tuple parameter
let addTuple aTuple = 
   let (x,y) = aTuple
   x + y

// another function that takes a single tuple parameter 
// but looks like it takes two ints
let addConfusingTuple (x,y) = x + y
```

* The first definition, "`addTwoParams`", takes two parameters, separated with spaces.
* The second definition, "`addTuple`", takes a single parameter. It then binds "x" and "y" to the inside of the tuple and does the addition.
* The third definition, "`addConfusingTuple`", takes a single parameter just like "`addTuple`", but the tricky thing is that the tuple is unpacked and bound as part of the parameter definition using pattern matching. Behind the scenes, it is exactly the same as "`addTuple`".

Let's look at the signatures (it is always a good idea to look at the signatures if you are unsure)

```fsharp
val addTwoParams : int -> int -> int        // two params
val addTuple : int * int -> int             // tuple->int
val addConfusingTuple : int * int -> int    // tuple->int
```

Now let's use them:

```fsharp
//test
addTwoParams 1 2      // ok - uses spaces to separate args
addTwoParams (1,2)    // error trying to pass a single tuple 
//   => error FS0001: This expression was expected to have type
//                    int but here has type 'a * 'b
```

Here we can see an error occur in the second case above. 

First, the compiler treats `(1,2)` as a generic tuple of type `('a * 'b)`, which it attempts to pass as the first parameter to "`addTwoParams`".
Then it complains that the first parameter of `addTwoParams` is an `int`, and we're trying to pass a tuple.

To make a tuple, use a comma!  Here's how to do it correctly:

```fsharp
addTuple (1,2)           // ok
addConfusingTuple (1,2)  // ok

let x = (1,2)                 
addTuple x               // ok

let y = 1,2              // it's the comma you need, 
                         // not the parentheses!      
addTuple y               // ok
addConfusingTuple y      // ok
```

Conversely, if you attempt to pass multiple arguments to a function expecting a tuple, you will also get an obscure error.

```fsharp
addConfusingTuple 1 2    // error trying to pass two args 
// => error FS0003: This value is not a function and 
//                  cannot be applied
```

In this case, the compiler thinks that, since you are passing two arguments, `addConfusingTuple` must be curryable. So then "`addConfusingTuple 1`" would be a partial application that returns another intermediate function. Trying to apply that intermediate function with "2" gives an error, because there is no intermediate function! We saw this exact same error in the post on currying, when we discussed the issues that can occur from having too many parameters.

### Why not use tuples as parameters? ###

The discussion of the issues with tuples above shows that there's another way to define functions with more than one parameter: rather than passing them in separately, all the parameters can be combined into a single composite data structure. In the example below, the function takes a single parameter, which is a tuple containing three items. 

```fsharp
let f (x,y,z) = x + y * z
// type is int * int * int -> int

// test
f (1,2,3)
```

Note that the function signature is different from a true three parameter function. There is only one arrow, so only one parameter, and the stars indicate that this is a tuple of `(int*int*int)`. 

When would we want to use tuple parameters instead of individual ones?  

* When the tuples are meaningful in themselves. For example, if we are working with three dimensional coordinates, a three-tuple might well be more convenient than three separate dimensions.
* Tuples are occasionally used to bundle data together in a single structure that should be kept together. For example, the `TryParse` functions in .NET library return the result and a Boolean as a tuple.  But if you have a lot of data that is kept together as a bundle, then you will probably want to define a record or class type to store it.

### A special case: tuples and .NET library functions ###

One area where commas are seen a lot is when calling .NET library functions! 

These all take tuple-like arguments, and so these calls look just the same as they would from C#:  

```fsharp
// correct
System.String.Compare("a","b")

// incorrect
System.String.Compare "a" "b"
```

The reason is that .NET library functions are not curried and cannot be partially applied. *All* the parameters must *always* be passed in, and using a tuple-like approach is the obvious way to do this.

But do note that although these calls look like tuples, they are actually a special case. Real tuples cannot be used, so the following code is invalid:

```fsharp
let tuple = ("a","b")
System.String.Compare tuple   // error  

System.String.Compare "a","b" // error  
```

If you do want to partially apply .NET library functions, it is normally trivial to write wrapper functions for them, as we have [seen earlier](/posts/partial-application/), and as shown below:

```fsharp
// create a wrapper function
let strCompare x y = System.String.Compare(x,y)

// partially apply it
let strCompareWithB = strCompare "B"

// use it with a higher order function
["A";"B";"C"]
|> List.map strCompareWithB 
```


## Guidelines for separate vs. grouped parameters ##

The discussion on tuples leads us to a more general topic: when should function parameters be separate and when should they be grouped?

Note that F# is different from C# in this respect. In C# *all* the parameters are *always* provided, so the question does not even arise!  In F#, due to partial application, only some parameters might be provided, so you need to distinguish between those that are required to be grouped together vs. those that are independent. 

Here are some general guidelines of how to structure parameters when you are designing your own functions.

* In general, it is always better to use separate parameters rather than passing them as a single structure such as a tuple or record. This allows for more flexible behavior such as partial application.
* But, when a group of parameters *must* all be set at once, then *do* use some sort of grouping mechanism.  

In other words, when designing a function, ask yourself "could I provide this parameter in isolation?" If the answer is no, the parameters should be grouped.

Let's look at some examples:

```fsharp
// Pass in two numbers for addition. 
// The numbers are independent, so use two parameters
let add x y = x + y

// Pass in two numbers as a geographical co-ordinate. 
// The numbers are dependent, so group them into a tuple or record
let locateOnMap (xCoord,yCoord) = // do something

// Set first and last name for a customer.
// The values are dependent, so group them into a record.
type CustomerName = {First:string; Last:string}
let setCustomerName aCustomerName = // good
let setCustomerName first last = // not recommended

// Set first and last name and and pass the 
// authorizing credentials as well.
// The name and credentials are independent, keep them separate
let setCustomerName myCredentials aName = //good
```

Finally, do be sure to order the parameters appropriately to assist with partial application (see the guidelines in the earlier [post](/posts/partial-application/)). For example, in the last function above, why did I put the `myCredentials` parameter ahead of the `aName` parameter?

## Parameter-less functions ##

Sometimes we may want functions that don't take any parameters at all. For example, we may want a "hello world" function that we can call repeatedly. As we saw in a previous section, the naive definition will not work.

```fsharp
let sayHello = printfn "Hello World!"     // not what we want
```

The fix is to add a unit parameter to the function, or use a lambda. 

```fsharp
let sayHello() = printfn "Hello World!"           // good
let sayHello = fun () -> printfn "Hello World!"   // good
```

And then the function must always be called with a unit argument:

```fsharp
// call it
sayHello()
```

This is particularly common with the .NET libraries. Some examples are:

```fsharp
Console.ReadLine()
System.Environment.GetCommandLineArgs()
System.IO.Directory.GetCurrentDirectory()
```

Do remember to call them with the unit parameter!

## Defining new operators ##

You can define functions named using one or more of the operator symbols (see the [F# documentation](http://msdn.microsoft.com/en-us/library/dd233204) for the exact list of symbols that you can use):

```fsharp
// define
let (.*%) x y = x + y + 1
```

You must use parentheses around the symbols when defining them. 

Note that for custom operators that begin with `*`, a space is required; otherwise the `(*` is interpreted as the start of a comment: 

```fsharp
let ( *+* ) x y = x + y + 1
```

Once defined, the new function can be used in the normal way, again with parens around the symbols:

```fsharp
let result = (.*%) 2 3
```

If the function has exactly two parameters, you can use it as an infix operator without parentheses.

```fsharp
let result = 2 .*% 3
```

You can also define prefix operators that start with `!` or `~` (with some restrictions -- see the [F# documentation on operator overloading](http://msdn.microsoft.com/en-us/library/dd233204#prefix))

```fsharp
let (~%%) (s:string) = s.ToCharArray()

//use
let result = %% "hello"
```

In F# it is quite common to create your own operators, and many libraries will export operators with names such as `>=>` and `<*>`.

## Point-free style ##

We have already seen many examples of leaving off the last parameter of functions to reduce clutter. This style is referred to as **point-free style** or **tacit programming**.

Here are some examples:

```fsharp
let add x y = x + y   // explicit
let add x = (+) x     // point free

let add1Times2 x = (x + 1) * 2    // explicit
let add1Times2 = (+) 1 >> (*) 2   // point free

let sum list = List.reduce (fun sum e -> sum+e) list // explicit
let sum = List.reduce (+)                            // point free
```

There are pros and cons to this style. 

On the plus side, it focuses attention on the high level function composition rather than the low level objects. For example "`(+) 1 >> (*) 2`" is clearly an addition operation followed by a multiplication. And "`List.reduce (+)`" makes it clear that the plus operation is key, without needing to know about the list it is actually applied to. 

Point-free helps to clarify the underlying algorithm and reveal commonalities between code -- the "`reduce`" function used above is a good example of this -- it will be discussed in a planned series on list processing.

On the other hand, too much point-free style can make for confusing code. Explicit parameters can act as a form of documentation, and their names (such as "list") make it clear what the function is acting on. 

As with anything in programming, the best guideline is to use the approach that provides the most clarity.

## Combinators ##

The word "**combinator**" is used to describe functions whose result depends only on their parameters.  That means there is no dependency on the outside world, and in particular no other functions or global value can be accessed at all.

In practice, this means that a combinator function is limited to combining its parameters in various ways.

We have already seen some combinators already: the "pipe" operator and the "compose" operator.  If you look at their definitions, it is clear that all they do is reorder the parameters in various ways

```fsharp
let (|>) x f = f x             // forward pipe
let (<|) f x = f x             // reverse pipe
let (>>) f g x = g (f x)       // forward composition
let (<<) g f x = g (f x)       // reverse composition
```

On the other hand, a function like "printf", although primitive, is not a combinator, because it has a dependency on the outside world (I/O).

### Combinator birds ###

Combinators are the basis of a whole branch of logic (naturally called "combinatory logic") that was invented many years before computers and programming languages. Combinatory logic has had a very large influence on functional programming.

To read more about combinators and combinatory logic, I recommend the book "To Mock a Mockingbird" by Raymond Smullyan.  In it, he describes many other combinators and whimsically gives them names of birds.  Here are some examples of some standard combinators and their bird names:

```fsharp
let I x = x                // identity function, or the Idiot bird
let K x y = x              // the Kestrel
let M x = x >> x           // the Mockingbird
let T x y = y x            // the Thrush (this looks familiar!)
let Q x y z = y (x z)      // the Queer bird (also familiar!)
let S x y z = x z (y z)    // The Starling
// and the infamous...
let rec Y f x = f (Y f) x  // Y-combinator, or Sage bird
```

The letter names are quite standard, so if you refer to "the K combinator", everyone will be familiar with that terminology.

It turns out that many common programming patterns can be represented using these standard combinators. For example, the Kestrel is a common pattern in fluent interfaces where you do something but then return the original object. The Thrush is the pipe operation, the Queer bird is forward composition, and the Y-combinator is famously used to make functions recursive.

Indeed, there is a well-known theorem that states that any computable function whatsoever can be built from just two basic combinators, the Kestrel and the Starling. 

### Combinator libraries ###

A combinator library is a code library that exports a set of combinator functions that are designed to work together. The user of the library can then easily combine simple functions together to make bigger and more complex functions, like building with Lego.  

A well designed combinator library allows you to focus on the high level operations, and push the low level "noise" to the background. We've already seen some examples of this power in the examples in ["why use F#"](/series/why-use-fsharp.html) series, and the `List` module is full of them -- the "`fold`" and "`map`" functions are also combinators, if you think about it.

Another advantage of combinators is that they are the safest type of function. As they have no dependency on the outside world they cannot change if the global environment changes.  A function that reads a global value or uses a library function can break or alter between calls if the context is different. This can never happen with combinators. 

In F#, combinator libraries are available for parsing (the FParsec library), HTML construction, testing frameworks, and more.  We'll discuss and use combinators further in later series.

## Recursive functions ##

Often, a function will need to refer to itself in its body.  The classic example is the Fibonacci function:

```fsharp
let fib i = 
   match i with
   | 1 -> 1
   | 2 -> 1
   | n -> fib(n-1) + fib(n-2)
```

Unfortunately, this will not compile: 

	error FS0039: The value or constructor 'fib' is not defined

You have to tell the compiler that this is a recursive function using the rec keyword.

```fsharp
let rec fib i = 
   match i with
   | 1 -> 1
   | 2 -> 1
   | n -> fib(n-1) + fib(n-2)
```

Recursive functions and data structures are extremely common in functional programming, and I hope to devote a whole later series to this topic.
