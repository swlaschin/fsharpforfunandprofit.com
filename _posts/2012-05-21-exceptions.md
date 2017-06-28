---
layout: post
title: "Exceptions"
description: "Syntax for throwing and catching"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 8
---

Just like other .NET languages, F# supports throwing and catching exceptions.  As with the control flow expressions, the syntax will feel familiar, but again there are a few catches that you should know about.

## Defining your own exceptions

When raising/throwing exceptions, you can use the standard system ones such as `InvalidOperationException`, or you can define your own exception types using the simple syntax shown below, where the "content" of the exception is any F# type:

```fsharp
exception MyFSharpError1 of string
exception MyFSharpError2 of string * int
```

That's it! Defining new exception classes is a lot easier than in C#!

## Throwing exceptions

There are three basic ways to throw an exception

* Using one of the built in functions, such as "invalidArg"
* Using one of the standard .NET exception classes
* Using your own custom exception types 

### Throwing exceptions, method 1: using one of the built in functions

There are four useful exception keywords built into F#: 

* `failwith` throws a generic `System.Exception`
* `invalidArg` throws an `ArgumentException`
* `nullArg` throws a `NullArgumentException`
* `invalidOp ` throws an `InvalidOperationException`

These four probably cover most of the exceptions you would regularly throw. Here is how they are used:

```fsharp
// throws a generic System.Exception
let f x = 
   if x then "ok"
   else failwith "message"
                    
// throws an ArgumentException
let f x = 
   if x then "ok"
   else invalidArg "paramName" "message" 
  
// throws a NullArgumentException
let f x = 
   if x then "ok"
   else nullArg "paramName" "message"   

// throws an InvalidOperationException
let f x = 
   if x then "ok"
   else invalidOp "message"   
```

By the way, there's a very useful variant of `failwith` called `failwithf` that includes `printf` style formatting, so that you can make custom messages easily:

```fsharp
open System
let f x = 
    if x = "bad" then
        failwithf "Operation '%s' failed at time %O" x DateTime.Now
    else
        printfn "Operation '%s' succeeded at time %O" x DateTime.Now

// test   
f "good"
f "bad"
```

### Throwing exceptions, method 2: using one of the standard .NET exception classes

You can `raise` any .NET exception explicitly:

```fsharp
// you control the exception type
let f x = 
   if x then "ok"
   else raise (new InvalidOperationException("message"))
```

### Throwing exceptions, method 3: using your own F# exception types

Finally, you can use your own types, as defined earlier.

```fsharp
// using your own F# exception types
let f x = 
   if x then "ok"
   else raise (MyFSharpError1 "message")
```

And that's pretty much it for throwing exceptions.

## What effect does raising an exception have on the function type?

We said earlier that both branches of an if-then-else expression must return the same type. But how can raising an exception work with this constraint?

The answer is that any code that raises exceptions is ignored for the purposes of determining expression types.  This means that the function signature will be based on the normal case only, not the exception case.

For example, in the code below, the exceptions are ignored, and the overall function has signature `bool->int`, as you would expect.

```fsharp
let f x = 
   if x then 42
   elif true then failwith "message"
   else invalidArg "paramName" "message"   
```

Question: what do you think the function signature will be if both branches raise exceptions?  

```fsharp
let f x = 
   if x then failwith "error in true branch"
   else failwith "error in false branch"
```

Try it and see!

## Catching exceptions

Exceptions are caught using a try-catch block, as in other languages. F# calls it `try-with` instead, and testing for each type of exception uses the standard pattern matching syntax.

```fsharp
try
    failwith "fail"
with
    | Failure msg -> "caught: " + msg
    | MyFSharpError1 msg -> " MyFSharpError1: " + msg
    | :? System.InvalidOperationException as ex -> "unexpected"
```

If the exception to catch was thrown with `failwith` (e.g. a System.Exception) or a custom F# exception, you can match using the simple tag approach shown above. 

On the other hand, to catch a specific .NET exception class, you have to match using the more complicated syntax:

```fsharp
:? (exception class) as ex 
```

Again, as with if-then-else and the loops, the try-with block is an expression that returns a value.  This means that all branches of the `try-with` expression *must* return the same type.

Consider this example:

```fsharp
let divide x y=
    try
        (x+1) / y                      // error here -- see below
    with
    | :? System.DivideByZeroException as ex -> 
          printfn "%s" ex.Message
```

When we try to evaluate it, we get an error:

    error FS0043: The type 'unit' does not match the type 'int'

The reason is that the "`with`" branch is of type `unit`, while the "`try`" branch is of type `int`. So the two branches are of incompatible types.

To fix this, we need to make the "`with`" branch also return type `int`. We can do this easily using the semicolon trick to chain expressions on one line.

```fsharp
let divide x y=
    try
        (x+1) / y                      
    with
    | :? System.DivideByZeroException as ex -> 
          printfn "%s" ex.Message; 0            // added 0 here!

//test
divide 1 1
divide 1 0
```

Now that the `try-with` expression has a defined type, the whole function can be assigned a type, namely `int -> int -> int`, as expected.

As before, if any branch throws an exception, it doesn't count when types are being determined.  

### Rethrowing exceptions

If needed, you can call the "`reraise()`" function in a catch handler to propagate the same exception up the call chain. This is the same as the C# `throw` keyword.

```fsharp
let divide x y=
    try
        (x+1) / y                      
    with
    | :? System.DivideByZeroException as ex -> 
          printfn "%s" ex.Message
          reraise()

//test
divide 1 1
divide 1 0
```

## Try-finally

Another familiar expression is `try-finally`.  As you might expect, the "finally" clause will be called no matter what.

```fsharp
let f x = 
    try
        if x then "ok" else failwith "fail"
    finally
        printf "this will always be printed"
```

The return type of the try-finally expression as a whole is always the same as return type of the "try" clause on its own. The "finally" clause has no effect on the type of the expression as a whole. So in the above example, the whole expression has type `string`.

The "finally" clause must always return unit, so any non-unit values will be flagged by the compiler.

```fsharp
let f x = 
    try
        if x then "ok" else failwith "fail"
    finally
        1+1  // This expression should have type 'unit
```

## Combining try-with and try-finally

The try-with and the try-finally expressions are distinct and cannot be combined directly into a single expression. Instead, you will have to nest them as circumstances require.

```fsharp
let divide x y=
   try
      try       
         (x+1) / y                      
      finally
         printf "this will always be printed"
   with
   | :? System.DivideByZeroException as ex -> 
           printfn "%s" ex.Message; 0            
```

## Should functions throw exceptions or return error structures?

When you are designing a function, should you throw exceptions, or return structures which encode the error? This section will discuss two different approaches.

### The pair of functions approach 

One approach is to provide two functions: one which assumes everything works and throws an exception otherwise and a second "tryXXX" function that returns a missing value if something goes wrong.

For example, we might want to design two distinct library functions for division, one that doesn't handle exceptions and one that does:

```fsharp
// library function that doesn't handle exceptions
let divideExn x y = x / y

// library function that converts exceptions to None
let tryDivide x y = 
   try
       Some (x / y)
   with
   | :? System.DivideByZeroException -> None // return missing
```

Note the use of `Some` and `None` Option types in the `tryDivide` code to signal to the client whether the value is valid.

With the first function, the client code must handle the exception explicitly. 

```fsharp
// client code must handle exceptions explicitly
try
    let n = divideExn 1 0
    printfn "result is %i" n
with
| :? System.DivideByZeroException as ex -> printfn "divide by zero"
```

Note that there is no constraint that forces the client to do this, so this approach can be a source of errors.

With the second function the client code is simpler, and the client is constrained to handle both the normal case and the error case.

```fsharp
// client code must test both cases
match tryDivide 1 0 with
| Some n -> printfn "result is %i" n
| None -> printfn "divide by zero"
```

This "normal vs. try" approach is very common in the .NET BCL, and also occurs in a few cases in the F# libraries too. For example, in the `List` module:

* `List.find` will throw a `KeyNotFoundException` if the key is not found
* But `List.tryFind` will return an Option type, with `None` if the key is not found

If you are going to use this approach, do have a naming convention. For example: 

* "doSomethingExn" for functions that expect clients to catch exceptions.
* "tryDoSomething" for functions that handle normal exceptions for you.

Note that I prefer to have an "Exn" suffix on "doSomething" rather than no suffix at all. It makes it clear that you expect clients to catch exceptions even in normal cases.

The overall problem with this approach is that you have to do extra work to create pairs of functions, and you reduce the safety of the system by relying on the client to catch exceptions if they use the unsafe version of the function.

### The error-code-based approach

> "Writing good error-code-based code is hard, but writing good exception-based code is really hard." 
> [*Raymond Chen*](http://blogs.msdn.com/b/oldnewthing/archive/2005/01/14/352949.aspx)

In the functional world, returning error codes (or rather error *types*) is generally preferred to throwing exceptions, and so a standard hybrid approach is to encode the common cases (the ones that you would expect a user to care about) into a error type, but leave the very unusual exceptions alone.

Often, the simplest approach is just to use the option type: `Some` for success and `None` for errors. If the error case is obvious, as in `tryDivide` or `tryParse`, there is no need to be explicit with more detailed error cases.

But sometimes there is more than one possible error, and each should be handled differently. In this case, a union type with a case for each error is useful.

In the following example, we want to execute a SqlCommand. Three very common error cases are login errors, constraint errors and foreign key errors, so we build them into the result structure. All other errors are raised as exceptions.

```fsharp
open System.Data.SqlClient

type NonQueryResult =
    | Success of int
    | LoginError of SqlException
    | ConstraintError of SqlException
    | ForeignKeyError of SqlException 

let executeNonQuery (sqlCommmand:SqlCommand) =
    try
       use sqlConnection = new SqlConnection("myconnection")
       sqlCommmand.Connection <- sqlConnection 
       let result = sqlCommmand.ExecuteNonQuery()
       Success result
    with    
    | :?SqlException as ex ->     // if a SqlException
        match ex.Number with      
        | 18456 ->                // login Failed
            LoginError ex     
        | 2601 | 2627 ->          // handle constraint error
            ConstraintError ex     
        | 547 ->                  // handle FK error
            ForeignKeyError ex     
        | _ ->                    // don't handle any other cases 
            reraise()          
       // all non SqlExceptions are thrown normally        
```

The client is then forced to handle the common cases, while uncommon exceptions will be caught by a handler higher up the call chain.

```fsharp
let myCmd = new SqlCommand("DELETE Product WHERE ProductId=1")
let result =  executeNonQuery myCmd
match result with
| Success n -> printfn "success"
| LoginError ex -> printfn "LoginError: %s" ex.Message
| ConstraintError ex -> printfn "ConstraintError: %s" ex.Message
| ForeignKeyError ex -> printfn "ForeignKeyError: %s" ex.Message
```

Unlike a traditional error code approach, the caller of the function does not have to handle any errors immediately, and can simply pass the structure around until it gets to someone who knows how to handle it, as shown below:

```fsharp
let lowLevelFunction commandString = 
  let myCmd = new SqlCommand(commandString)
  executeNonQuery myCmd          //returns result    

let deleteProduct id = 
  let commandString = sprintf "DELETE Product WHERE ProductId=%i" id
  lowLevelFunction commandString  //returns without handling errors

let presentationLayerFunction = 
  let result = deleteProduct 1
  match result with
  | Success n -> printfn "success"
  | errorCase -> printfn "error %A" errorCase 
```

On the other hand, unlike C#, the result of a expression cannot be accidentally thrown away. So if a function returns an error result, the caller must handle it (unless it really wants to be badly behaved and send it to `ignore`)

```fsharp
let presentationLayerFunction = 
  do deleteProduct 1    // error: throwing away a result code!
```

