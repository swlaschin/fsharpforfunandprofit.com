---
layout: post
title: "Understanding type inference"
description: "Behind the magic curtain"
nav: fsharp-types
seriesId: "Understanding F# types"
seriesOrder: 12
categories: [Types]
---

Before we finish with types, let's revisit type inference: the magic that allows the F# compiler to deduce what types are used and where. We have seen this happen through all the examples so far, but how does it work and what can you do if it goes wrong?

## How does type inference work?

It does seem to be magic, but the rules are mostly straightforward. The fundamental logic is based on an algorithm often called "Hindley-Milner" or "HM" (more accurately it should be called "Damas-Milner's Algorithm W"). If you want to know the details, go ahead and Google it.

I do recommend that you take some time to understand this algorithm so that you can "think like the compiler" and troubleshoot effectively when you need to.

Here are some of the rules for determine the types of simple and function values:

* Look at the literals
* Look at the functions and other values something interacts with
* Look at any explicit type constraints
* If there are no constraints anywhere, automatically generalize to generic types

Let's look at each of these in turn.

### Look at the literals

The literals give the compiler a clue to the context. As we have seen, the type checking is very strict; ints and floats are not automatically cast to the other. The benefit of this is that the compiler can deduce types by looking at the literals. If the literal is an `int` and you are adding "x" to it, then "x" must be an int as well. But if the literal is a `float` and you are adding "x" to it, then "x" must be a float as well.   

Here are some examples. Run them and see their signatures in the interactive window:

```fsharp
let inferInt x = x + 1
let inferFloat x = x + 1.0
let inferDecimal x = x + 1m     // m suffix means decimal
let inferSByte x = x + 1y       // y suffix means signed byte
let inferChar x = x + 'a'       // a char
let inferString x = x + "my string"
```

### Look at the functions and other values it interacts with

If there are no literals anywhere, the compiler tries to work out the types by analyzing the functions and other values that they interact with. In the cases below, the "`indirect`" function calls a function that we do know the types for, which gives us the information to deduce the types for the "`indirect`" function itself.

```fsharp
let inferInt x = x + 1
let inferIndirectInt x = inferInt x       //deduce that x is an int

let inferFloat x = x + 1.0
let inferIndirectFloat x = inferFloat x   //deduce that x is a float
```

And of course assignment counts as an interaction too.  If x is a certain type, and y is bound (assigned) to x, then y must be the same type as x.

```fsharp
let x = 1
let y = x     //deduce that y is also an int
```

Other interactions might be control structures, or external libraries

```fsharp
// if..else implies a bool 
let inferBool x = if x then false else true      
// for..do implies a sequence
let inferStringList x = for y in x do printfn "%s" y  
// :: implies a list
let inferIntList x = 99::x                      
// .NET library method is strongly typed
let inferStringAndBool x = System.String.IsNullOrEmpty(x)
```

### Look at any explicit type constraints or annotations

If there are any explicit type constraints or annotations specified, then the compiler will use them. In the case below, we are explicitly telling the compiler that "`inferInt2`" takes an `int` parameter. It can then deduce that the return value for "`inferInt2`" is also an `int`, which in turn implies that "`inferIndirectInt2`" is of type int->int.

```fsharp
let inferInt2 (x:int) = x 
let inferIndirectInt2 x = inferInt2 x 

let inferFloat2 (x:float) = x 
let inferIndirectFloat2 x = inferFloat2 x 
```

Note that the formatting codes in `printf` statements count as explicit type constraints too!

```fsharp
let inferIntPrint x = printf "x is %i" x 
let inferFloatPrint x = printf "x is %f" x 
let inferGenericPrint x = printf "x is %A" x 
```

### Automatic generalization

If after all this, there are no constraints found, the compiler just makes the types generic.

```fsharp
let inferGeneric x = x 
let inferIndirectGeneric x = inferGeneric x 
let inferIndirectGenericAgain x = (inferIndirectGeneric x).ToString() 
```

### It works in all directions!

The type inference works top-down, bottom-up, front-to-back, back-to-front, middle-out, anywhere there is type information, it will be used.

Consider the following example. The inner function has a literal, so we know that it returns an `int`. And the outer function has been explicitly told that it returns a `string`. But what is the type of the passed in "`action`" function in the middle?

```fsharp
let outerFn action : string =  
   let innerFn x = x + 1 // define a sub fn that returns an int
   action (innerFn 2)    // result of applying action to innerFn
```

The type inference would work something like this:

* `1` is an `int`
* Therefore `x+1` must be an `int`, therefore `x` must be an `int`
* Therefore `innerFn` must be `int->int`
* Next, `(innerFn 2)` returns an `int`, therefore "`action`" takes an `int` as input.
* The output of `action` is the return value for `outerFn`, and therefore the output type of `action` is the same as the output type of `outerFn`.
* The output type of `outerFn` has been explicitly constrained to `string`, therefore the output type of `action` is also `string`.
* Putting this together, we now know that the `action` function has signature `int->string`
* And finally, therefore, the compiler deduces the type of `outerFn` as:

```fsharp
val outerFn: (int -> string) -> string
```

### Elementary, my dear Watson!

The compiler can do deductions worthy of Sherlock Holmes. Here's a tricky example that will test how well you have understood everything so far.

Let's say we have a `doItTwice` function that takes any input function (call it "`f`") and generates a new function that simply does the original function twice in a row. Here's the code for it:

```fsharp
let doItTwice f  = (f >> f)
```

As you can see, it composes `f` with itself. So in other words, it means: "do f", then "do f" on the result of that.

Now, what could the compiler possibly deduce about the signature of `doItTwice`? 

Well, let's look at the signature of "`f`" first. The output of the first call to "`f`" is also the input to the second call to "`f`". So therefore the output and input of "`f`" must be the same type. So the signature of `f` must be `'a -> 'a`. The type is generic (written as 'a) because we have no other information about it. 

So going back to `doItTwice` itself, we now know it takes a function parameter of `'a -> 'a`. But what does it return?  Well, here's how we deduce it, step by step:

* First, note that `doItTwice` generates a function, so must return a function type. 
* The input to the generated function is the same type as the input to first call to "`f`"
* The output of the generated function is the same type as the output of the second call to "`f`"
* So the generated function must also have type `'a -> 'a`
* Putting it all together, `doItTwice` has a domain of `'a -> 'a` and a range of `'a -> 'a`, so therefore its signature must be `('a -> 'a) -> ('a -> 'a)`. 

Is your head spinning yet? You might want to read it again until it sinks in.

Quite a sophisticated deduction for one line of code. Luckily the compiler does all this for us. But you will need to understand this kind of thing if you have problems and you have to determine what the compiler is doing.

Let's test it! It's actually much simpler to understand in practice than it is in theory.

```fsharp
let doItTwice f  = (f >> f)

let add3 x = x + 3
let add6 = doItTwice add3
// test 
add6 5             // result = 11

let square x = x * x
let fourthPower = doItTwice square
// test 
fourthPower 3      // result = 81

let chittyBang x = "Chitty " + x + " Bang"
let chittyChittyBangBang = doItTwice chittyBang
// test 
chittyChittyBangBang "&"      // result = "Chitty Chitty & Bang Bang"
```

Hopefully, that makes more sense now.

## Things that can go wrong with type inference

The type inference isn't perfect, alas. Sometimes the compiler just doesn't have a clue what to do. Again, understanding what is happening will really help you stay calm instead of wanting to kill the compiler. Here are some of the main reasons for type errors:

* Declarations out of order
* Not enough information
* Overloaded methods
* Quirks of generic numeric functions

### Declarations out of order

A basic rule is that you must declare functions before they are used. 

This code fails:

```fsharp
let square2 x = square x   // fails: square not defined 
let square x = x * x
```

But this is ok:

```fsharp
let square x = x * x       
let square2 x = square x   // square already defined earlier
```

And unlike C#, in F# the order of file compilation is important, so do make sure the files are being compiled in the right order. (In Visual Studio, you can change the order from the context menu).

### Recursive or simultaneous declarations 

A variant of the "out of order" problem occurs with recursive functions or definitions that have to refer to each other. No amount of reordering will help in this case -- we need to use additional keywords to help the compiler.

When a function is being compiled, the function identifier is not available to the body. So if you define a simple recursive function, you will get a compiler error. The fix is to add the "rec" keyword as part of the function definition. For example:

```fsharp
// the compiler does not know what "fib" means
let fib n =
   if n <= 2 then 1
   else fib (n - 1) + fib (n - 2)
   // error FS0039: The value or constructor 'fib' is not defined
```

Here's the fixed version with "rec fib" added to indicate it is recursive:

```fsharp    
let rec fib n =              // LET REC rather than LET 
   if n <= 2 then 1
   else fib (n - 1) + fib (n - 2)
```

A similar "`let rec ... and`" syntax is used for two functions that refer to each other. Here is a very contrived example that fails if you do not have the "`rec`" keyword.

```fsharp
let rec showPositiveNumber x =               // LET REC rather than LET
   match x with 
   | x when x >= 0 -> printfn "%i is positive" x 
   | _ -> showNegativeNumber x

and showNegativeNumber x =                   // AND rather than LET

   match x with 
   | x when x < 0 -> printfn "%i is negative" x 
   | _ -> showPositiveNumber x
```

The "`and`" keyword can also be used to declare simultaneous types in a similar way.

```fsharp
type A = None | AUsesB of B
   // error FS0039: The type 'B' is not defined
type B = None | BUsesA of A
```

Fixed version:

```fsharp
type A = None | AUsesB of B
and B = None | BUsesA of A    // use AND instead of TYPE
```

### Not enough information

Sometimes, the compiler just doesn't have enough information to determine a type. In the following example, the compiler doesn't know what type the `Length` method is supposed to work on. But it can't make it generic either, so it complains.

```fsharp
let stringLength s = s.Length
  // error FS0072: Lookup on object of indeterminate type 
  // based on information prior to this program point. 
  // A type annotation may be needed ...
```

These kinds of error can be fixed with explicit annotations.

```fsharp
let stringLength (s:string) = s.Length
```

Occasionally there does appear to be enough information, but still the compiler doesn't seem to recognize it. For example, it's obvious to a human that the `List.map` function (below) is being applied to a list of strings, so why does `x.Length` cause an error?

```fsharp
List.map (fun x -> x.Length) ["hello"; "world"]       //not ok
```

The reason is that the F# compiler is currently a one-pass compiler, and so information later in the program is ignored if it hasn't been parsed yet. (The F# team have said that it is possible to make the compiler more sophisticated, but it would work less well with Intellisense and might produce more unfriendly and obscure error messages. So for now, we will have to live with this limitation.)

So in cases like this, you can always explicitly annotate:

```fsharp
List.map (fun (x:string) -> x.Length) ["hello"; "world"]       // ok
```

But another, more elegant way that will often fix the problem is to rearrange things so the known types come first, and the compiler can digest them before it moves to the next clause.

```fsharp
["hello"; "world"] |> List.map (fun s -> s.Length)   //ok
```

Functional programmers strive to avoid explicit type annotations, so this makes them much happier!

This technique can be used more generally in other areas as well; a rule of thumb is to try to put the things that have "known types" earlier than things that have "unknown types". 

### Overloaded methods

When calling an external class or method in .NET, you will often get errors due to overloading.

In many cases, such as the concat example below, you will have to explicitly annotate the parameters of the external function so that the compiler knows which overloaded method to call. 

```fsharp
let concat x = System.String.Concat(x)           //fails
let concat (x:string) = System.String.Concat(x)  //works 
let concat x = System.String.Concat(x:string)    //works
```

Sometimes the overloaded methods have different argument names, in which case you can also give the compiler a clue by naming the arguments. Here is an example for the `StreamReader` constructor.

```fsharp
let makeStreamReader x = new System.IO.StreamReader(x)        //fails
let makeStreamReader x = new System.IO.StreamReader(path=x)   //works
```

### Quirks of generic numeric functions

Numeric functions can be somewhat confusing. There often appear generic, but once they are bound to a particular numeric type, they are fixed, and using them with a different numeric type will cause an error. The following example demonstrates this:

```fsharp
let myNumericFn x = x * x
myNumericFn 10
myNumericFn 10.0             //fails
  // error FS0001: This expression was expected to have 
  // type int but has type float

let myNumericFn2 x = x * x
myNumericFn2 10.0     
myNumericFn2 10               //fails
  // error FS0001: This expression was expected to have 
  // type float but has type int    
```

There is a way round this for numeric types using the "inline" keyword and "static type parameters". I won't discuss these concepts here, but you can look them up in the F# reference at MSDN.

<a name="troubleshooting-summary"></a>
## "Not enough information" troubleshooting summary 

So to summarize, the things that you can do if the compiler is complaining about missing types, or not enough information, are:

* Define things before they are used (this includes making sure the files are compiled in the right order)
* Put the things that have "known types" earlier than things that have "unknown types". In particular, you might be able reorder pipes and similar chained functions so that the typed objects come first.
* Annotate as needed. One common trick is to add annotations until everything works, and then take them away one by one until you have the minimum needed. 
Do try to avoid annotating if possible. Not only is it not aesthetically pleasing, but it makes the code more brittle. It is a lot easier to change types if there are no explicit dependencies on them.

## Debugging type inference issues

Once you have ordered and annotated everything, you will probably still get type errors, or find that functions are less generic than expected. With what you have learned so far, you should have the tools to determine why this happened (although it can still be painful). 

For example:

```fsharp
let myBottomLevelFn x = x

let myMidLevelFn x = 
   let y = myBottomLevelFn x
   // some stuff 
   let z= y
   // some stuff 
   printf "%s" z         // this will kill your generic types!
   // some more stuff
   x

let myTopLevelFn x =
   // some stuff 
   myMidLevelFn x 
   // some more stuff 
   x
```

In this example, we have a chain of functions. The bottom level function is definitely generic, but what about the top level one?  Well often, we might expect it be generic but instead it is not. In this case we have:

```fsharp
val myTopLevelFn : string -> string
```

What went wrong? The answer is in the midlevel function. The `%s` on z forced it be a string, which forced y and then x to be strings too.

Now this is a pretty obvious example, but with thousands of lines of code, a single line might be buried away that causes an issue. One thing that can help is to look at all the signatures; in this case the signatures are:

```fsharp
val myBottomLevelFn : 'a -> 'a       // generic as expected
val myMidLevelFn : string -> string  // here's the clue! Should be generic
val myTopLevelFn : string -> string
```

When you find a signature that is unexpected you know that it is the guilty party. You can then drill down into it and repeat the process until you find the problem.
