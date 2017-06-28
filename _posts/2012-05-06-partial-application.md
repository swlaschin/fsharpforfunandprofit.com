---
layout: post
title: "Partial application"
description: "Baking-in some of the parameters of a function"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 6
categories: [Currying, Partial Application]
---


In the previous post on currying, we looked at breaking multiple parameter functions into smaller one parameter functions. It is the mathematically correct way of doing it, but that is not the only reason it is done -- it also leads to a very powerful technique called **partial function application**. This is a very widely used style in functional programming, and it is important to understand it.

The idea of partial application is that if you fix the first N parameters of the function, you get a function of the remaining parameters. From the discussion on currying, you can probably see how this comes about naturally.

Here are some simple examples that demonstrate this:

```fsharp
// create an "adder" by partial application of add
let add42 = (+) 42    // partial application
add42 1
add42 3

// create a new list by applying the add42 function 
// to each element
[1;2;3] |> List.map add42 

// create a "tester" by partial application of "less than"
let twoIsLessThan = (<) 2   // partial application
twoIsLessThan 1
twoIsLessThan 3

// filter each element with the twoIsLessThan function
[1;2;3] |> List.filter twoIsLessThan 

// create a "printer" by partial application of printfn
let printer = printfn "printing param=%i" 

// loop over each element and call the printer function
[1;2;3] |> List.iter printer   
```

In each case, we create a partially applied function that we can then reuse in multiple contexts.

The partial application can just as easily involve fixing function parameters, of course. Here are some examples:

```fsharp
// an example using List.map
let add1 = (+) 1
let add1ToEach = List.map add1   // fix the "add1" function

// test
add1ToEach [1;2;3;4]

// an example using List.filter
let filterEvens = 
   List.filter (fun i -> i%2 = 0) // fix the filter function

// test
filterEvens [1;2;3;4]
```

The following more complex example shows how the same approach can be used to create "plug in" behavior that is transparent.

* We create a function that adds two numbers, but in addition takes a logging function that will log the two numbers and the result. 
* The logging function has two parameters: (string) "name" and (generic) "value", so it has signature `string->'a->unit`.
* We then create various implementations of the logging function, such as a console logger or a popup logger.
* And finally we partially apply the main function to create new functions that have a particular logger baked into them. 

```fsharp
// create an adder that supports a pluggable logging function
let adderWithPluggableLogger logger x y = 
    logger "x" x
    logger "y" y
    let result = x + y
    logger "x+y"  result 
    result 

// create a logging function that writes to the console
let consoleLogger argName argValue = 
    printfn "%s=%A" argName argValue 

//create an adder with the console logger partially applied
let addWithConsoleLogger = adderWithPluggableLogger consoleLogger 
addWithConsoleLogger  1 2 
addWithConsoleLogger  42 99

// create a logging function that creates popup windows
let popupLogger argName argValue = 
    let message = sprintf "%s=%A" argName argValue 
    System.Windows.Forms.MessageBox.Show(
                                 text=message,caption="Logger") 
      |> ignore

//create an adder with the popup logger partially applied
let addWithPopupLogger  = adderWithPluggableLogger popupLogger 
addWithPopupLogger  1 2 
addWithPopupLogger  42 99
```

These functions with the logger baked in can in turn be used like any other function. For example, we can create a partial application to add 42, and then pass that into a list function, just like we did for the simple "`add42`" function.

```fsharp
// create a another adder with 42 baked in
let add42WithConsoleLogger = addWithConsoleLogger 42 
[1;2;3] |> List.map add42WithConsoleLogger  
[1;2;3] |> List.map add42               //compare without logger 
```

These partially applied functions are a very useful tool. We can create library functions which are flexible (but complicated), yet make it easy to create reusable defaults so that callers don't have to be exposed to the complexity all the time.

## Designing functions for partial application ##

You can see that the order of the parameters can make a big difference in the ease of use for partial application. For example, most of the functions in the `List` library such as `List.map` and `List.filter` have a similar form, namely:

	List-function [function parameter(s)] [list]

The list is always the last parameter. Here are some examples of the full form:

```fsharp
List.map    (fun i -> i+1) [0;1;2;3]
List.filter (fun i -> i>1) [0;1;2;3]
List.sortBy (fun i -> -i ) [0;1;2;3]
```

And the same examples using partial application:

```fsharp
let eachAdd1 = List.map (fun i -> i+1) 
eachAdd1 [0;1;2;3]

let excludeOneOrLess = List.filter (fun i -> i>1) 
excludeOneOrLess [0;1;2;3]

let sortDesc = List.sortBy (fun i -> -i) 
sortDesc [0;1;2;3]
```

If the library functions were written with the parameters in a different order, it would be much more inconvenient to use them with partial application.

As you write your own multi-parameter functions, you might wonder what the best parameter order is. As with all design questions, there is no "right" answer to this question, but here are some commonly accepted guidelines:

1.	Put earlier: parameters more likely to be static 
2.	Put last: the data structure or collection (or most varying argument)
3.	For well-known operations such as "subtract", put in the expected order 

Guideline 1 is straightforward. The parameters that are most likely to be "fixed" with partial application should be first. We saw this with the logger example earlier.

Guideline 2 makes it easier to pipe a structure or collection from function to function. We have seen this many times already with list functions.

```fsharp
// piping using list functions
let result = 
   [1..10]
   |> List.map (fun i -> i+1)
   |> List.filter (fun i -> i>5)
```

Similarly, partially applied list functions are easy to compose, because the list parameter itself can be easily elided:

```fsharp
let compositeOp = List.map (fun i -> i+1) 
                  >> List.filter (fun i -> i>5)
let result = compositeOp [1..10]
```

### Wrapping BCL functions for partial application ###

The .NET base class library functions are easy to access in F#, but are not really designed for use with a functional language like F#. For example, most functions have the data parameter first, while with F#, as we have seen, the data parameter should normally come last.

However, it is easy enough to create wrappers for them that are more idiomatic. For example, in the snippet below, the .NET string functions are rewritten to have the string target be the last parameter rather than the first:

```fsharp
// create wrappers for .NET string functions
let replace oldStr newStr (s:string) = 
  s.Replace(oldValue=oldStr, newValue=newStr)

let startsWith lookFor (s:string) = 
  s.StartsWith(lookFor)
```

Once the string becomes the last parameter, we can then use them with pipes in the expected way:

```fsharp
let result = 
     "hello" 
     |> replace "h" "j" 
     |> startsWith "j"

["the"; "quick"; "brown"; "fox"] 
     |> List.filter (startsWith "f")
```

or with function composition:

```fsharp
let compositeOp = replace "h" "j" >> startsWith "j"
let result = compositeOp "hello"
```

### Understanding the "pipe" function ###

Now that you have seen how partial application works, you should be able to understand how the "pipe" function works.

The pipe function is defined as:

```fsharp
let (|>) x f = f x
```

All it does is allow you to put the function argument in front of the function rather than after. That's all. 

```fsharp
let doSomething x y z = x+y+z
doSomething 1 2 3       // all parameters after function
```

If the function has multiple parameters, then it appears that the input is the final parameter. Actually what is happening is that the function is partially applied, returning a function that has a single parameter: the input 

Here's the same example rewritten to use partial application

```fsharp
let doSomething x y  = 
   let intermediateFn z = x+y+z
   intermediateFn        // return intermediateFn

let doSomethingPartial = doSomething 1 2
doSomethingPartial 3     // only one parameter after function now
3 |> doSomethingPartial  // same as above - last parameter piped in
```

As you have already seen, the pipe operator is extremely common in F#, and used all the time to preserve a natural flow. Here are some more usages that you might see:

```fsharp
"12" |> int               // parses string "12" to an int
1 |> (+) 2 |> (*) 3       // chain of arithmetic
```

### The reverse pipe function ###

You might occasionally see the reverse pipe function "<|" being used.  

```fsharp
let (<|) f x = f x
```

It seems that this function doesn't really do anything different from normal, so why does it exist? 

The reason is that, when used in the infix style as a binary operator, it reduces the need for parentheses and can make the code cleaner.

```fsharp
printf "%i" 1+2          // error
printf "%i" (1+2)        // using parens
printf "%i" <| 1+2       // using reverse pipe
```

You can also use piping in both directions at once to get a pseudo infix notation.

```fsharp
let add x y = x + y
(1+2) add (3+4)          // error
1+2 |> add <| 3+4        // pseudo infix
```
