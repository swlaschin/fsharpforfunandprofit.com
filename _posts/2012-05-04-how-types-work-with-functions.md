---
layout: post
title: "How types work with functions"
description: "Understanding the type notation"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 4
categories: [Types, Functions]
---

Now that we have some understanding of functions, we'll look at how types work with functions, both as domains and ranges. This is just an overview; the series ["understanding F# types"](/series/understanding-fsharp-types.html) will cover types in detail. 

First, we need to understand the type notation a bit more. We've seen that the arrow notation "`->`" is used to show the domain and range. So that a function signature always looks like:

```fsharp
val functionName : domain -> range
```

Here are some example functions:

```fsharp
let intToString x = sprintf "x is %i" x  // format int to string
let stringToInt x = System.Int32.Parse(x)
```

If you evaluate that in the F# interactive window, you will see the signatures: 

```fsharp
val intToString : int -> string
val stringToInt : string -> int
```

This means:

* `intToString` has a domain of `int` which it maps onto the range `string`.
* `stringToInt` has a domain of `string` which it maps onto the range `int`. 

##  Primitive types ##

The possible primitive types are what you would expect:  string, int, float, bool, char, byte, etc., plus many more derived from the .NET type system.

Here are some more examples of functions using primitive types:

```fsharp
let intToFloat x = float x // "float" fn. converts ints to floats
let intToBool x = (x = 2)  // true if x equals 2
let stringToString x = x + " world"
```

and their signatures are:

```fsharp
val intToFloat : int -> float
val intToBool : int -> bool
val stringToString : string -> string
```

## Type annotations ##

In the previous examples, the F# compiler correctly determined the types of the parameters and results. But this is not always the case. If you try the following code, you will get a compiler error:

```fsharp
let stringLength x = x.Length         
   => error FS0072: Lookup on object of indeterminate type
```

The compiler does not know what type "x" is, and therefore does not know if "Length" is a valid method. In most cases, this can be fixed by giving the F# compiler a "type annotation" so that it knows which type to use. In the corrected version below, we indicate that the type of "x" is a string. 

```fsharp
let stringLength (x:string) = x.Length         
```

The parens around the `x:string` param are important. If they are missing, the compiler thinks that the return value is a string! That is, an "open" colon is used to indicate the type of the return value, as you can see in the example below.

```fsharp
let stringLengthAsInt (x:string) :int = x.Length         
```

We're indicating that the x param is a string and the return value is an int. 

## Function types as parameters ##

A function that takes other functions as parameters, or returns a function, is called a **higher-order function** (sometimes abbreviated as HOF). They are used as a way of abstracting out common behavior. These kinds of functions are extremely common in F#; most of the standard libraries use them. 

Consider a function `evalWith5ThenAdd2`, which takes a function as a parameter, then evaluates the function with the value 5, and adds 2 to the result:

```fsharp
let evalWith5ThenAdd2 fn = fn 5 + 2     // same as fn(5) + 2
```

The signature of this function looks like this:

```fsharp
val evalWith5ThenAdd2 : (int -> int) -> int
```

You can see that the domain is `(int->int)` and the range is `int`. What does that mean?  It means that the input parameter is not a simple value, but a function, and what's more is restricted only to functions that map `ints` to `ints`. The output is not a function, just an int.

Let's try it:

```fsharp
let add1 x = x + 1      // define a function of type (int -> int)
evalWith5ThenAdd2 add1  // test it
```

gives:

```fsharp
val add1 : int -> int
val it : int = 8
```

"`add1`" is a function that maps ints to ints, as we can see from its signature. So it is a valid parameter for the `evalWith5ThenAdd2` function. And the result is 8. 

By the way, the special word "`it`" is used for the last thing that was evaluated; in this case the result we want. It's not a keyword, just a convention.

Here's another one:

```fsharp
let times3 x = x * 3      // a function of type (int -> int)
evalWith5ThenAdd2 times3  // test it 
```

gives:

```fsharp
val times3 : int -> int
val it : int = 17
```

"`times3`" is also a function that maps ints to ints, as we can see from its signature. So it is also a valid parameter for the `evalWith5ThenAdd2` function. And the result is 17.

Note that the input is sensitive to the types. If our input function uses `floats` rather than `ints`, it will not work. For example, if we have:

```fsharp
let times3float x = x * 3.0  // a function of type (float->float)  
evalWith5ThenAdd2 times3float 
```

Evaluating this will give an error:

```fsharp
error FS0001: Type mismatch. Expecting a int -> int but 
              given a float -> float    
```

meaning that the input function should have been an `int->int` function.

### Functions as output ###

A function value can also be the output of a function. For example, the following function will generate an "adder" function that adds using the input value. 

```fsharp
let adderGenerator numberToAdd = (+) numberToAdd
```

The signature is:

```fsharp
val adderGenerator : int -> (int -> int)
```

which means that the generator takes an `int`, and creates a function (the "adder") that maps `ints` to `ints`. Let's see how it works:

```fsharp
let add1 = adderGenerator 1
let add2 = adderGenerator 2
```

This creates two adder functions. The first generated function adds 1 to its input, and the second adds 2. Note that the signatures are just as we would expect them to be.

```fsharp
val add1 : (int -> int)
val add2 : (int -> int)
```

And we can now use these generated functions in the normal way. They are indistinguishable from functions defined explicitly

```fsharp
add1 5    // val it : int = 6
add2 5    // val it : int = 7
```

### Using type annotations to constrain function types ###

In the first example, we had the function:

```fsharp
let evalWith5ThenAdd2 fn = fn 5 +2
    => val evalWith5ThenAdd2 : (int -> int) -> int
```

In this case F# could deduce that "`fn`" mapped `ints` to `ints`, so its signature would be `int->int`

But what is the signature of "fn" in this following case?

```fsharp
let evalWith5 fn = fn 5
```

Obviously, "`fn`" is some kind of function that takes an int, but what does it return? The compiler can't tell. If you do want to specify the type of the function, you can add a type annotation for function parameters in the same way as for a primitive type.

```fsharp
let evalWith5AsInt (fn:int->int) = fn 5
let evalWith5AsFloat (fn:int->float) = fn 5
```

Alternatively, you could also specify the return type instead.

```fsharp
let evalWith5AsString fn :string = fn 5
```

Because the main function returns a string, the "`fn`" function is also constrained to return a string, so no explicit typing is required for "fn". 

<a name="unit-type"></a>
## The "unit" type ##

When programming, we sometimes want a function to do something without returning a value. Consider the function "`printInt`", defined below. The function doesn't actually return anything. It just prints a string to the console as a side effect.

```fsharp
let printInt x = printf "x is %i" x        // print to console
```

So what is the signature for this function? 

```fsharp
val printInt : int -> unit
```

What is this "`unit`"?  

Well, even if a function returns no output, it still needs a range. There are no "void" functions in mathematics-land. Every function must have some output, because a function is a mapping, and a mapping has to have something to map to!
 
![](/assets/img/Functions_Unit.png)
 
So in F#, functions like this return a special range called "`unit`". This range has exactly one value in it, called "`()`". You can think of `unit` and `()` as somewhat like "void" (the type) and "null" (the value) in C#. But unlike void/null, `unit` is a real type and `()` is a real value. To see this, evaluate:

```fsharp
let whatIsThis = ()
```

and you will see the signature:

```fsharp
val whatIsThis : unit = ()
```

Which means that the value "`whatIsThis`" is of type `unit` and has been bound to the value `()`

So, going back to the signature of "`printInt`", we can now understand it:

```fsharp
val printInt : int -> unit
```

This signature says: `printInt` has a domain of `int` which it maps onto nothing that we care about.

<a name="parameterless-functions"></a>

### Parameterless functions

Now that we understand unit, can we predict its appearance in other contexts?  For example, let's try to create a reusable "hello world" function. Since there is no input and no output, we would expect it to have a signature `unit -> unit`. Let's see:

```fsharp
let printHello = printf "hello world"        // print to console
```

The result is:

```fsharp
hello world
val printHello : unit = ()
```

Not quite what we expected. "Hello world" is printed immediately and the result is not a function, but a simple value of type unit. As we saw earlier, we can tell that this is a simple value because it has a signature of the form:  

```fsharp
val aName: type = constant
```

So in this case, we see that `printHello` is actually a *simple value* with the value `()`. It's not a function that we can call again.

Why the difference between `printInt` and `printHello`?  In the `printInt` case, the value could not be determined until we knew the value of the x parameter, so the definition was of a function. In the `printHello` case, there were no parameters, so the right hand side could be determined immediately. Which it was, returning the `()` value, with the side effect of printing to the console. 

We can create a true reusable function that is parameterless by forcing the definition to have a unit argument, like this:

```fsharp
let printHelloFn () = printf "hello world"    // print to console
```

The signature is now:

```fsharp
val printHelloFn : unit -> unit
```

and to call it, we have to pass the `()` value as a parameter, like so:

```fsharp
printHelloFn ()
```

### Forcing unit types with the ignore function ###

In some cases the compiler requires a unit type and will complain. For example, both of the following will be compiler errors:

```fsharp
do 1+1     // => FS0020: This expression should have type 'unit'

let something = 
  2+2      // => FS0020: This expression should have type 'unit'
  "hello"
```

To help in these situations, there is a special function `ignore` that takes anything and returns the unit type. The correct version of this code would be:

```fsharp
do (1+1 |> ignore)  // ok

let something = 
  2+2 |> ignore     // ok
  "hello"
```

## Generic types ##

In many cases, the type of the function parameter can be any type, so we need a way to indicate this. F# uses the .NET generic type system for this situation. 

For example, the following function converts the parameter to a string and appends some text:

```fsharp
let onAStick x = x.ToString() + " on a stick"
```

It doesn't matter what type the parameter is, as all objects understand `ToString()`. 

The signature is:

```fsharp
val onAStick : 'a -> string
```

What is this type called `'a`?  That is F#'s way of indicating a generic type that is not known at compile time. The apostrophe in front of the "a" means that the type is generic. The signature for the C# equivalent of this would be:

```csharp
string onAStick<a>();   

//or more idiomatically 
string OnAStick<TObject>();   // F#'s use of 'a is like 
                              // C#'s "TObject" convention 
```

Note that the F# function is still strongly typed with a generic type. It does *not* take a parameter of type `Object`. This strong typing is desirable so that when functions are composed together, type safety is still maintained.

Here's the same function being used with an int, a float and a string

```fsharp
onAStick 22
onAStick 3.14159
onAStick "hello"
```

If there are two generic parameters, the compiler will give them different names: `'a` for the first generic, `'b` for the second generic, and so on. Here's an example:

```fsharp
let concatString x y = x.ToString() + y.ToString()
```

The type signature for this has two generics: `'a` and `'b`:

```fsharp
val concatString : 'a -> 'b -> string
```

On the other hand, the compiler will recognize when only one generic type is required. In the following example, the x and y parameters must be of the same type:

```fsharp
let isEqual x y = (x=y)
```

So the function signature has the same generic type for both of them:

```fsharp
val isEqual : 'a -> 'a -> bool 
```

Generic parameters are also very important when it comes to lists and more abstract structures, and we will be seeing them a lot in upcoming examples.

## Other types ##

The types discussed so far are just the basic types. These types can be combined in various ways to make much more complex types. A full discussion of these types will have to wait for [another series](/series/understanding-fsharp-types.html), but meanwhile, here is a brief introduction to them so that you can recognize them in function signatures.

* **The "tuple" types**. These are pairs, triples, etc., of other types. For example `("hello", 1)` is a tuple made from a string and an int. The comma is the distinguishing characteristic of a tuple -- if you see a comma in F#, it is almost certainly part of a tuple!

In function signatures, tuples are written as the "multiplication" of the two types involved. So in this case, the tuple would have type:

```fsharp
string * int      // ("hello", 1)
```

* **The collection types**. The most common of these are lists, sequences, and arrays. Lists and arrays are fixed size, while sequences are potentially infinite (behind the scenes, sequences are the same as `IEnumerable`). In function signatures, they have their own keywords: "`list`", "`seq`", and "`[]`" for arrays.

```fsharp
int list          // List type  e.g. [1;2;3]
string list       // List type  e.g. ["a";"b";"c"]
seq<int>          // Seq type   e.g. seq{1..10}
int []            // Array type e.g. [|1;2;3|]
```

* **The option type**. This is a simple wrapper for objects that might be missing. There are two cases: `Some` and `None`. In function signatures, they have their own "`option`" keyword:

```fsharp
int option        // Some(1)
```

* **The discriminated union type**. These are built from a set of choices of other types. We saw some examples of this in the ["why use F#?"](/series/why-use-fsharp.html) series. In function signatures, they are referred to by the name of the type, so there is no special keyword.
* **The record type**. These are like structures or database rows, a list of named slots. We saw some examples of this in the ["why use F#?"](/series/why-use-fsharp.html) series as well. In function signatures, they are referred to by the name of the type, so again there is no special keyword.

## Test your understanding of types ##

How well do you understand the types yet?  Here are some expressions for you -- see if you can guess their signatures. To see if you are correct, just run them in the interactive window!

```fsharp
let testA   = float 2
let testB x = float 2
let testC x = float 2 + x
let testD x = x.ToString().Length
let testE (x:float) = x.ToString().Length
let testF x = printfn "%s" x
let testG x = printfn "%f" x
let testH   = 2 * 2 |> ignore
let testI x = 2 * 2 |> ignore
let testJ (x:int) = 2 * 2 |> ignore
let testK   = "hello"
let testL() = "hello"
let testM x = x=x
let testN x = x 1          // hint: what kind of thing is x?
let testO x:string = x 1   // hint: what does :string modify? 
```