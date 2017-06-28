---
layout: post
title: "Currying"
description: "Breaking multi-parameter functions into smaller one-parameter functions"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 5
categories: [Currying]
---

After that little digression on basic types, we can turn back to functions again, and in particular the puzzle we mentioned earlier: if a mathematical function can only have one parameter, then how is it possible that an F# function can have more than one? 

The answer is quite simple: a function with multiple parameters is rewritten as a series of new functions, each with only one parameter. And this is done automatically by the compiler for you. It is called "**currying**", after Haskell Curry, a mathematician who was an important influence on the development of functional programming.

To see how this works in practice, let's use a very basic example that prints two numbers: 

```fsharp
//normal version
let printTwoParameters x y = 
   printfn "x=%i y=%i" x y
```

Internally, the compiler rewrites it as something more like:

```fsharp
//explicitly curried version
let printTwoParameters x  =    // only one parameter!
   let subFunction y = 
      printfn "x=%i y=%i" x y  // new function with one param
   subFunction                 // return the subfunction
```

Let's examine this in more detail:

1.	Construct the function called "`printTwoParameters`" but with only *one* parameter: "x"
2.	Inside that, construct a subfunction that has only *one* parameter: "y". Note that this inner function uses the "x" parameter but x is not passed to it explicitly as a parameter. The "x" parameter is in scope, so the inner function can see it and use it without needing it to be passed in. 
3.	Finally, return the newly created subfunction.
4.	This returned function is then later used against "y".  The "x" parameter is baked into it, so the returned function only needs the y param to finish off the function logic.

By rewriting it this way, the compiler has ensured that every function has only one parameter, as required. So when you use "`printTwoParameters`", you might think that you are using a two parameter function, but it is actually only a one parameter function!  You can see for yourself by passing only one argument instead of two:

```fsharp
// eval with one argument
printTwoParameters 1 

// get back a function!
val it : (int -> unit) = <fun:printTwoParameters@286-3>
```

If you evaluate it with one argument, you don't get an error, you get back a function. 

So what you are really doing when you call `printTwoParameters` with two arguments is:

* You call `printTwoParameters` with the first argument (x)
* `printTwoParameters` returns a new function that has "x" baked into it.
* You then call the new function with the second argument (y)

Here is an example of the step by step version, and then the normal version again.

```fsharp
// step by step version
let x = 6
let y = 99
let intermediateFn = printTwoParameters x  // return fn with 
                                           // x "baked in"
let result  = intermediateFn y 

// inline version of above
let result  = (printTwoParameters x) y

// normal version
let result  = printTwoParameters x y
```

Here is another example:

```fsharp
//normal version
let addTwoParameters x y = 
   x + y

//explicitly curried version
let addTwoParameters x  =      // only one parameter!
   let subFunction y = 
      x + y                    // new function with one param
   subFunction                 // return the subfunction

// now use it step by step 
let x = 6
let y = 99
let intermediateFn = addTwoParameters x  // return fn with 
                                         // x "baked in"
let result  = intermediateFn y 

// normal version
let result  = addTwoParameters x y
```

Again, the "two parameter function" is actually a one parameter function that returns an intermediate function.

But wait a minute -- what about the "`+`" operation itself? It's a binary operation that must take two parameters, surely? No, it is curried like every other function. There is a function called "`+`" that takes one parameter and returns a new intermediate function, exactly like `addTwoParameters` above. 

When we write the statement `x+y`, the compiler reorders the code to remove the infix and turns it into `(+) x y`, which is the function named `+` called with two parameters.  Note that the function named "+" needs to have parentheses around it to indicate that it is being used as a normal function name rather than as an infix operator.

Finally, the two parameter function named `+` is treated as any other two parameter function would be. 

```fsharp
// using plus as a single value function 
let x = 6
let y = 99
let intermediateFn = (+) x     // return add with x baked in
let result  = intermediateFn y 

// using plus as a function with two parameters
let result  = (+) x y          

// normal version of plus as infix operator
let result  = x + y
```

And yes, this works for all other operators and built in functions like printf.

```fsharp
// normal version of multiply
let result  = 3 * 5

// multiply as a one parameter function
let intermediateFn = (*) 3   // return multiply with "3" baked in
let result  = intermediateFn 5

// normal version of printfn
let result  = printfn "x=%i y=%i" 3 5  

// printfn as a one parameter function
let intermediateFn = printfn "x=%i y=%i" 3  // "3" is baked in
let result  = intermediateFn 5
```

## Signatures of curried functions ##

Now that we know how curried functions work, what should we expect their signatures to look like?

Going back to the first example, "`printTwoParameters`", we saw that it took one argument and returned an intermediate function. The intermediate function also took one argument and returned nothing (that is, unit). So the intermediate function has type `int->unit`. In other words, the domain of `printTwoParameters` is `int` and the range is `int->unit`. Putting this together we see that the final signature is:   

```fsharp
val printTwoParameters : int -> (int -> unit)
```

If you evaluate the explicitly curried implementation, you will see the parentheses in the signature, as written above, but if you evaluate the normal implementation, which is implicitly curried, the parentheses are left off, like so:

```fsharp
val printTwoParameters : int -> int -> unit
```

The parentheses are optional. If you are trying to make sense of function signatures it might be helpful to add them back in mentally.

At this point you might be wondering, what is the difference between a function that returns an intermediate function and a regular two parameter function?

Here's a one parameter function that returns a function:

```fsharp
let add1Param x = (+) x    
// signature is = int -> (int -> int)
```

Here's a two  parameter function that returns a simple value:

```fsharp
let add2Params x y = (+) x y    
// signature is = int -> int -> int
```

The signatures are slightly different, but in practical terms, there *is* no difference*, only that the second function is automatically curried for you.

## Functions with more than two parameters ##

How does currying work for functions with more than two parameters? Exactly the same way: for each parameter except the last one, the function returns an intermediate function with the previous parameters baked in.

Consider this contrived example. I have explicitly specified the types of the parameters, but the function itself does nothing.

```fsharp
let multiParamFn (p1:int)(p2:bool)(p3:string)(p4:float)=
   ()   //do nothing

let intermediateFn1 = multiParamFn 42    
   // intermediateFn1 takes a bool 
   // and returns a new function (string -> float -> unit)
let intermediateFn2 = intermediateFn1 false    
   // intermediateFn2 takes a string 
   // and returns a new function (float -> unit)
let intermediateFn3 = intermediateFn2 "hello"  
   // intermediateFn3 takes a float 
   // and returns a simple value (unit)
let finalResult = intermediateFn3 3.141
```

The signature of the overall function is:

```fsharp
val multiParamFn : int -> bool -> string -> float -> unit
```

and the signatures of the intermediate functions are:

```fsharp
val intermediateFn1 : (bool -> string -> float -> unit)
val intermediateFn2 : (string -> float -> unit)
val intermediateFn3 : (float -> unit)
val finalResult : unit = ()
```

A function signature can tell you how many parameters the function takes: just count the number of arrows outside of parentheses. If the function takes or returns other function parameters, there will be other arrows in parentheses, but these can be ignored. Here are some examples:

```fsharp
int->int->int      // two int parameters and returns an int

string->bool->int  // first param is a string, second is a bool,  
                   // returns an int

int->string->bool->unit // three params (int,string,bool) 
                        // returns nothing (unit)

(int->string)->int      // has only one parameter, a function
                        // value (from int to string)
                        // and returns a int

(int->string)->(int->bool) // takes a function (int to string) 
                           // returns a function (int to bool) 
```


## Issues with multiple parameters ##

The logic behind currying can produce some unexpected results until you understand it. Remember that you will not get an error if you evaluate a function with fewer arguments than it is expecting. Instead you will get back a partially applied function. If you then go on to use this partially applied function in a context where you expect a value, you will get obscure error messages from the compiler.

Here's an innocuous looking function: 

```fsharp
// create a function
let printHello() = printfn "hello"
```

What would you expect to happen when we call it as shown below? Will it print "hello" to the console?  Try to guess before evaluating it, and here's a hint: be sure to take a look at the function signature. 

```fsharp
// call it
printHello
```

It will *not* be called as expected. The original function expects a unit argument that was not supplied, so you are getting a partially applied function (in this case with no arguments).

How about this? Will it compile?

```fsharp
let addXY x y = 
    printfn "x=%i y=%i" x     
    x + y 
```

If you evaluate it, you will see that the compiler complains about the printfn line.

```fsharp
printfn "x=%i y=%i" x
//^^^^^^^^^^^^^^^^^^^^^
//warning FS0193: This expression is a function value, i.e. is missing
//arguments. Its type is  ^a -> unit.
```

If you didn't understand currying, this message would be very cryptic! All expressions that are evaluated standalone like this (i.e. not used as a return value or bound to something with "let") *must* evaluate to the unit value. And in this case, it is does *not* evaluate to the unit value, but instead evaluates to a function. This is a long winded way of saying that `printfn` is missing an argument. 

A common case of errors like this is when interfacing with the .NET library. For example, the `ReadLine` method of a `TextReader` must take a unit parameter. It is often easy to forget this and leave off the parens, in which case you do not get a compiler error immediately, but only when you try to treat the result as a string.

```fsharp
let reader = new System.IO.StringReader("hello");

let line1 = reader.ReadLine        // wrong but compiler doesn't 
                                   // complain
printfn "The line is %s" line1     //compiler error here!
// ==> error FS0001: This expression was expected to have 
// type string but here has type unit -> string    

let line2 = reader.ReadLine()      //correct
printfn "The line is %s" line2     //no compiler error 
```

In the code above, `line1` is just a pointer or delegate to the `Readline` method, not the string that we expected. The use of `()` in `reader.ReadLine()` actually executes the function.

## Too many parameters ##

You can get similar cryptic messages when you have too many parameters as well. Here are some examples of passing too many parameters to printf.

```fsharp
printfn "hello" 42
// ==> error FS0001: This expression was expected to have 
//                   type 'a -> 'b but here has type unit    

printfn "hello %i" 42 43
// ==> Error FS0001: Type mismatch. Expecting a 'a -> 'b -> 'c    
//                   but given a 'a -> unit    

printfn "hello %i %i" 42 43 44
// ==> Error FS0001: Type mismatch. Expecting a 'a->'b->'c->'d    
//                   but given a 'a -> 'b -> unit   
```

For example, in the last case, the compiler is saying that it expects the format argument to have three parameters (the signature `'a -> 'b -> 'c -> 'd`  has three parameters) but it is given only two (the signature `'a -> 'b -> unit`  has two parameters).

In cases not using `printf`, passing too many parameters will often mean that you end up with a simple value that you then try to pass a parameter to. The compiler will complain that the simple value is not a function.

```fsharp
let add1 x = x + 1
let x = add1 2 3
// ==>   error FS0003: This value is not a function 
//                     and cannot be applied
```

If you break the call into a series of explicit intermediate functions, as we did earlier, you can see exactly what is going wrong.

```fsharp
let add1 x = x + 1
let intermediateFn = add1 2   //returns a simple value
let x = intermediateFn 3      //intermediateFn is not a function!
// ==>   error FS0003: This value is not a function 
//                     and cannot be applied
```
