---
layout: post
title: "Match expressions"
description: "The workhorse of F#"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 9
categories: [Patterns,Folds]
---

Pattern matching is ubiquitous in F#. It is used for binding values to expressions with `let`, and in function parameters, and for branching using the `match..with` syntax.

We have briefly covered binding values to expressions in a [post in the "why use F#?" series](/posts/conciseness-pattern-matching), and it will be covered many times as we [investigate types](/posts/overview-of-types-in-fsharp). 

So in this post, we'll cover the `match..with` syntax and its use for control flow.

## What is a match expression?

We have already seen `match..with` expressions a number of times. And we know that it has the form: 

    match [something] with 
    | pattern1 -> expression1
    | pattern2 -> expression2
    | pattern3 -> expression3
    
If you squint at it just right, it looks a bit like a series of lambda expressions:
    
    match [something] with 
    | lambda-expression-1
    | lambda-expression-2
    | lambda-expression-3

Where each lambda expression has exactly one parameter:

    param -> expression
    
So one way of thinking about `match..with` is that it is a choice between a set of lambda expressions. But how to make the choice?

This is where the patterns come in.  The choice is made based on whether the "match with" value can be matched with the parameter of the lambda expression.
The first lambda whose parameter can be made to match the input value "wins"!

So for example, if the param is the wildcard `_`, it will always match, and if first, always win.

    _ -> expression  

### Order is important!
    
Looking at the following example:

```fsharp
let x = 
    match 1 with 
    | 1 -> "a"
    | 2 -> "b"  
    | _ -> "z" 
```

We can see that there are three lambda expressions to match, in this order:

    fun 1 -> "a"
    fun 2 -> "b"
    fun _ -> "z"

So, the `1` pattern gets tried first, then then the `2` pattern, and finally, the `_` pattern.
    
On the other hand, if we changed the order to put the wildcard first, it would be tried first and always win immediately:

```fsharp
let x = 
    match 1 with 
    | _ -> "z" 
    | 1 -> "a"
    | 2 -> "b"  
```

In this case, the F# compiler helpfully warns us that the other rules will never be matched.

So this is one major difference between a "`switch`" or "`case`" statement compared with a `match..with`. In a `match..with`, **the order is important**.

## Formatting a match expression

Since F# is sensitive to indentation, you might be wondering how best to format this expression, as there are quite a few moving parts.

The [post on F# syntax](/posts/fsharp-syntax) gives an overview of how alignment works, but for `match..with` expressions, here are some specific guidelines.

**Guideline 1: The alignment of the `| expression` clauses should be directly under the `match`**

This guideline is straightforward.

```fsharp
let f x =   match x with 
            // aligned
            | 1 -> "pattern 1" 
            // aligned
            | 2 -> "pattern 2" 
            // aligned
            | _ -> "anything" 
```


**Guideline 2: The `match..with` should be on a new line**

The `match..with` can be on the same line or a new line, but using a new line keeps the indenting consistent, independent of the lengths of the names:

```fsharp
                                              // ugly alignment!  
let myVeryLongNameForAFunction myParameter =  match myParameter with 
                                              | 1 -> "something" 
                                              | _ -> "anything" 

// much better
let myVeryLongNameForAFunction myParameter =  
    match myParameter with 
    | 1 -> "something" 
    | _ -> "anything" 
```

**Guideline 3: The expression after the arrow `->` should be on a new line**

Again, the result expression can be on the same line as the arrow, but using a new line again keeps the indenting consistent and helps to
separate the match pattern from the result expression.

```fsharp
let f x =  
    match x with 
    | "a very long pattern that breaks up the flow" -> "something" 
    | _ -> "anything" 

let f x =  
    match x with 
    | "a very long pattern that breaks up the flow" -> 
        "something" 
    | _ -> 
        "anything" 
```

Of course, when all the patterns are very compact, a common sense exception can be made:

```fsharp
let f list =  
    match list with 
    | [] -> "something" 
    | x::xs -> "something else" 
```
        
    
## match..with is an expression

It is important to realize that `match..with` is not really a "control flow" construct.  The "control" does not "flow" down the branches, but instead, the whole thing is an expression that gets evaluated at some point, just like any other expression.  The end result in practice might be the same, but it is a conceptual difference that can be important.

One consequence of it being an expression is that all branches *must* evaluate to the *same* type -- we have already seen this same behavior with if-then-else expressions and for loops.

```fsharp
let x = 
    match 1 with 
    | 1 -> 42
    | 2 -> true  // error wrong type
    | _ -> "hello" // error wrong type
```

You cannot mix and match the types in the expression.

{% include book_page_pdf.inc %}

### You can use match expressions anywhere

Since they are normal expressions, match expressions can appear anywhere an expression can be used.

For example, here's a nested match expression:

```fsharp
// nested match..withs are ok
let f aValue = 
    match aValue with 
    | x -> 
        match x with 
        | _ -> "something" 
```

And here's a match expression embedded in a lambda:

```fsharp
[2..10]
|> List.map (fun i ->
        match i with 
        | 2 | 3 | 5 | 7 -> sprintf "%i is prime" i
        | _ -> sprintf "%i is not prime" i
        )
```



## Exhaustive matching

Another consequence of being an expression is that there must always be *some* branch that matches. The expression as a whole must evaluate to *something*!

That is, the valuable concept of "exhaustive matching" comes from the "everything-is-an-expression" nature of F#. In a statement oriented language, there would be no requirement for this to happen.

Here's an example of an incomplete match:

```fsharp
let x = 
    match 42 with 
    | 1 -> "a"
    | 2 -> "b"
```

The compiler will warn you if it thinks there is a missing branch.
And if you deliberately ignore the warning, then you will get a nasty runtime error (`MatchFailureException`) when none of the patterns match.

### Exhaustive matching is not perfect

The algorithm for checking that all possible matches are listed is good but not always perfect. Occasionally it will complain that you have not matched every possible case, when you know that you have.
In this case, you may need to add an extra case just to keep the compiler happy.

### Using (and avoiding) the wildcard match

One way to guarantee that you always match all cases is to put the wildcard parameter as the last match:

```fsharp
let x = 
    match 42 with 
    | 1 -> "a"
    | 2 -> "b"
    | _ -> "z"
```

You see this pattern frequently, and I have used it a lot in these examples. It's the equivalent of having a catch-all `default` in a switch statement.

But if you want to get the full benefits of exhaustive pattern matching, I would encourage you *not* to use wildcards,
and try to match all the cases explicitly if you can.  This is particularly true if you are matching on
the cases of a union type:

```fsharp
type Choices = A | B | C
let x = 
    match A with 
    | A -> "a"
    | B -> "b"
    | C -> "c"
    //NO default match
```

By being always explicit in this way, you can trap any error caused by adding a new case to the union. If you had a wildcard match, you would never know.

If you can't have *every* case be explicit, you might try to document your boundary conditions as much as possible, and assert an runtime error for the wildcard case.

```fsharp
let x = 
    match -1 with 
    | 1 -> "a"
    | 2 -> "b"
    | i when i >= 0 && i<=100 -> "ok"
    // the last case will always match
    | x -> failwithf "%i is out of range" x
```


## Types of patterns

There are lots of different ways of matching patterns, which we'll look at next.

For more details on the various patterns, see the [MSDN documentation](http://msdn.microsoft.com/en-us/library/dd547125%28v=vs.110%29).

### Binding to values

The most basic pattern is to bind to a value as part of the match:

```fsharp
let y = 
    match (1,0) with 
    // binding to a named value
    | (1,x) -> printfn "x=%A" x
```

*By the way, I have deliberately left this pattern (and others in this post) as incomplete. As an exercise, make them complete without using the wildcard.*

It is important to note that the values that are bound *must* be distinct for each pattern. So you can't do something like this:

```fsharp
let elementsAreEqual aTuple = 
    match aTuple with 
    | (x,x) -> 
        printfn "both parts are the same" 
    | (_,_) -> 
        printfn "both parts are different" 
```

Instead, you have to do something like this:

```fsharp
let elementsAreEqual aTuple = 
    match aTuple with 
    | (x,y) -> 
        if (x=y) then printfn "both parts are the same" 
        else printfn "both parts are different" 
```

This second option can also be rewritten using "guards" (`when` clauses) instead. Guards will be discussed shortly.

### AND and OR

You can combine multiple patterns on one line, with OR logic and AND logic:

```fsharp
let y = 
    match (1,0) with 
    // OR  -- same as multiple cases on one line
    | (2,x) | (3,x) | (4,x) -> printfn "x=%A" x 

    // AND  -- must match both patterns at once
	// Note only a single "&" is used
    | (2,x) & (_,1) -> printfn "x=%A" x 
```

The OR logic is particularly common when matching a large number of union cases:

```fsharp
type Choices = A | B | C | D
let x = 
    match A with 
    | A | B | C -> "a or b or c"
    | D -> "d"
```


### Matching on lists

Lists can be matched explicitly in the form `[x;y;z]` or in the "cons" form `head::tail`:

```fsharp
let y = 
    match [1;2;3] with 
    // binding to explicit positions
    // square brackets used!
    | [1;x;y] -> printfn "x=%A y=%A" x y

    // binding to head::tail. 
    // no square brackets used!
    | 1::tail -> printfn "tail=%A" tail 

    // empty list
    | [] -> printfn "empty" 
```

A similar syntax is available for matching arrays exactly `[|x;y;z|]`.

It is important to understand that sequences (aka `IEnumerables`) can *not* be matched on this way directly, because they are "lazy" and meant to be accessed one element at a time.
Lists and arrays, on the other hand, are fully available to be matched on.

Of these patterns, the most common one is the "cons" pattern, often used in conjunction with recursion to loop through the elements of the list.

Here are some examples of looping through lists using recursion:

```fsharp
// loop through a list and print the values
let rec loopAndPrint aList = 
    match aList with 
    // empty list means we're done.
    | [] -> 
        printfn "empty" 

    // binding to head::tail. 
    | x::xs -> 
        printfn "element=%A," x
        // do all over again with the 
        // rest of the list
        loopAndPrint xs 

//test
loopAndPrint [1..5]

// ------------------------
// loop through a list and sum the values
let rec loopAndSum aList sumSoFar = 
    match aList with 
    // empty list means we're done.
    | [] -> 
        sumSoFar  

    // binding to head::tail. 
    | x::xs -> 
        let newSumSoFar = sumSoFar + x
        // do all over again with the 
        // rest of the list and the new sum
        loopAndSum xs newSumSoFar 

//test
loopAndSum [1..5] 0
```

The second example shows how we can carry state from one iteration of the loop to the next using a special "accumulator" parameter (called `sumSoFar` in this example). This is a very common pattern.

### Matching on tuples, records and unions

Pattern matching is available for all the built-in F# types.  More details in the [series on types](/posts/overview-of-types-in-fsharp).

```fsharp
// -----------------------
// Tuple pattern matching
let aTuple = (1,2)
match aTuple with 
| (1,_) -> printfn "first part is 1"
| (_,2) -> printfn "second part is 2"


// -----------------------
// Record pattern matching
type Person = {First:string; Last:string}
let person = {First="john"; Last="doe"}
match person with 
| {First="john"}  -> printfn "Matched John" 
| _  -> printfn "Not John" 

// -----------------------
// Union pattern matching
type IntOrBool= I of int | B of bool
let intOrBool = I 42
match intOrBool with 
| I i  -> printfn "Int=%i" i
| B b  -> printfn "Bool=%b" b
```


### Matching the whole and the part with the "as" keyword

Sometimes you want to match the individual components of the value *and* also the whole thing. You can use the `as` keyword for this.

```fsharp
let y = 
    match (1,0) with 
    // binding to three values
    | (x,y) as t -> 
        printfn "x=%A and y=%A" x y
        printfn "The whole tuple is %A" t
```


### Matching on subtypes

You can match on subtypes, using the `:?` operator, which gives you a crude polymorphism:

```fsharp
let x = new Object()
let y = 
    match x with 
    | :? System.Int32 -> 
        printfn "matched an int"
    | :? System.DateTime -> 
        printfn "matched a datetime"
    | _ -> 
        printfn "another type"
```

This only works to find subclasses of a parent class (in this case, Object). The overall type of the expression has the parent class as input.

Note that in some cases, you may need to "box" the value.

```fsharp
let detectType v =
    match v with
        | :? int -> printfn "this is an int"
        | _ -> printfn "something else"
// error FS0008: This runtime coercion or type test from type 'a to int    
// involves an indeterminate type based on information prior to this program point. 
// Runtime type tests are not allowed on some types. Further type annotations are needed.
```

The message tells you the problem: "runtime type tests are not allowed on some types". 
The answer is to "box" the value which forces it into a reference type, and then you can type check it:

```fsharp
let detectTypeBoxed v =
    match box v with      // used "box v" 
        | :? int -> printfn "this is an int"
        | _ -> printfn "something else"

//test
detectTypeBoxed 1
detectTypeBoxed 3.14
```

In my opinion, matching and dispatching on types is a code smell, just as it is in object-oriented programming.
It is occasionally necessary, but used carelessly is an indication of poor design.

In a good object oriented design, the correct approach would be to use [polymorphism to replace the subtype tests](http://sourcemaking.com/refactoring/replace-conditional-with-polymorphism), along with techniques such as [double dispatch](http://www.c2.com/cgi/wiki?DoubleDispatchExample). So if you are doing this kind of OO in F#, you should probably use those same techniques.

## Matching on multiple values

All the patterns we've looked at so far do pattern matching on a *single* value. How can you do it for two or more?

The short answer is: you can't. Matches are only allowed on single values.

But wait a minute -- could we combine two values into a *single* tuple on the fly and match on that? Yes, we can!

```fsharp
let matchOnTwoParameters x y = 
    match (x,y) with 
    | (1,y) -> 
        printfn "x=1 and y=%A" y
    | (x,1) -> 
        printfn "x=%A and y=1" x
```

And indeed, this trick will work whenever you want to match on a set of values -- just group them all into a single tuple.

```fsharp
let matchOnTwoTuples x y = 
    match (x,y) with 
    | (1,_),(1,_) -> "both start with 1"
    | (_,2),(_,2) -> "both end with 2"
    | _ -> "something else"

// test
matchOnTwoTuples (1,3) (1,2)
matchOnTwoTuples (3,2) (1,2)
```

## Guards, or the "when" clause

Sometimes pattern matching is just not enough, as we saw in this example:

```fsharp
let elementsAreEqual aTuple = 
    match aTuple with 
    | (x,y) -> 
        if (x=y) then printfn "both parts are the same" 
        else printfn "both parts are different" 
```

Pattern matching is based on patterns only -- it can't use functions or other kinds of conditional tests.

But there *is* a way to do the equality test as part of the pattern match -- using an additional `when` clause to the left of the function arrow.
These clauses are known as "guards".

Here's the same logic written using a guard instead:

```fsharp
let elementsAreEqual aTuple = 
    match aTuple with 
    | (x,y) when x=y -> 
        printfn "both parts are the same" 
    | _ ->
        printfn "both parts are different" 
```

This is nicer, because we have integrated the test into the pattern proper, rather than using a test after the match has been done.

Guards can be used for all sorts of things that pure patterns can't be used for, such as:

* comparing the bound values 
* testing object properties
* doing other kinds of matching, such as regular expressions
* conditionals derived from functions 

Let's look at some examples of these:

```fsharp
// --------------------------------
// comparing values in a when clause
let makeOrdered aTuple = 
    match aTuple with 
    // swap if x is bigger than y
    | (x,y) when x > y -> (y,x)
        
    // otherwise leave alone
    | _ -> aTuple

//test        
makeOrdered (1,2)        
makeOrdered (2,1)

// --------------------------------
// testing properties in a when clause        
let isAM aDate = 
    match aDate:System.DateTime with 
    | x when x.Hour <= 12-> 
        printfn "AM"
        
    // otherwise leave alone
    | _ -> 
        printfn "PM"

//test
isAM System.DateTime.Now

// --------------------------------
// pattern matching using regular expressions
open System.Text.RegularExpressions

let classifyString aString = 
    match aString with 
    | x when Regex.Match(x,@".+@.+").Success-> 
        printfn "%s is an email" aString
        
    // otherwise leave alone
    | _ -> 
        printfn "%s is something else" aString


//test
classifyString "alice@example.com"
classifyString "google.com"

// --------------------------------
// pattern matching using arbitrary conditionals
let fizzBuzz x = 
    match x with 
    | i when i % 15 = 0 -> 
        printfn "fizzbuzz" 
    | i when i % 3 = 0 -> 
        printfn "fizz" 
    | i when i % 5 = 0 -> 
        printfn "buzz" 
    | i  -> 
        printfn "%i" i

//test
[1..30] |> List.iter fizzBuzz
```

### Using active patterns instead of guards

Guards are great for one-off matches. But if there are certain guards that you use over and over, consider using active patterns instead.

For example, the email example above could be rewritten as follows:

```fsharp
open System.Text.RegularExpressions

// create an active pattern to match an email address
let (|EmailAddress|_|) input =
   let m = Regex.Match(input,@".+@.+") 
   if (m.Success) then Some input else None  

// use the active pattern in the match   
let classifyString aString = 
    match aString with 
    | EmailAddress x -> 
        printfn "%s is an email" x
        
    // otherwise leave alone
    | _ -> 
        printfn "%s is something else" aString

//test
classifyString "alice@example.com"
classifyString "google.com"
```

You can see other examples of active patterns in a [previous post](/posts/convenience-active-patterns).

## The "function" keyword

In the examples so far, we've seen a lot of this:

```fsharp
let f aValue = 
    match aValue with 
    | _ -> "something" 
```

In the special case of function definitions we can simplify this dramatically by using the `function` keyword.

```fsharp
let f = 
    function 
    | _ -> "something" 
```

As you can see, the `aValue` parameter has completely disappeared, along with the `match..with`.

This keyword is *not* the same as the `fun` keyword for standard lambdas, rather it combines `fun` and `match..with` in a single step.

The `function` keyword works anywhere a function definition or lambda can be used, such as nested matches:

```fsharp
// using match..with
let f aValue = 
    match aValue with 
    | x -> 
        match x with 
        | _ -> "something" 

// using function keyword
let f = 
    function 
    | x -> 
        function 
        | _ -> "something" 
```

or lambdas passed to a higher order function:

```fsharp
// using match..with
[2..10] |> List.map (fun i ->
        match i with 
        | 2 | 3 | 5 | 7 -> sprintf "%i is prime" i
        | _ -> sprintf "%i is not prime" i
        )

// using function keyword
[2..10] |> List.map (function 
        | 2 | 3 | 5 | 7 -> sprintf "prime"
        | _ -> sprintf "not prime"
        )
```

A minor drawback of `function` compared with `match..with` is that you can't see the original input value and have to rely on value bindings in the pattern.

## Exception handling with try..with

In the [previous post](/posts/exceptions), we looked at catching exceptions with the `try..with` expression.

```fsharp
try
    failwith "fail"
with
    | Failure msg -> "caught: " + msg
    | :? System.InvalidOperationException as ex -> "unexpected"
```
    
The `try..with` expression implements pattern matching in the same way as `match..with`.

So in the above example we see the use of matching on a custom pattern 

* `| Failure msg` is an example of matching on (what looks like) an active pattern 
* `| :? System.InvalidOperationException as ex` is an example of matching on the subtype (with the use of `as` as well).

Because the `try..with` expression implements full pattern matching, we can also use guards as well, if needed to add extra conditional logic:

```fsharp
let debugMode = false
try
    failwith "fail"
with
    | Failure msg when debugMode  -> 
        reraise()
    | Failure msg when not debugMode -> 
        printfn "silently logged in production: %s" msg
```

    
## Wrapping match expressions with functions

Match expressions are very useful, but can lead to complex code if not used carefully.

The main problem is that match expressions doesn't compose very well. That is, it is hard to chain `match..with` expressions and build simple ones into complex ones.

The best way of avoiding this is to wrap `match..with` expressions into functions, which can then be composed nicely.

Here's a simple example. The `match x with 42` is wrapped in a `isAnswerToEverything` function.

```fsharp
let times6 x = x * 6

let isAnswerToEverything x = 
    match x with 
    | 42 -> (x,true)
    | _ -> (x,false)

// the function can be used for chaining or composition
[1..10] |> List.map (times6 >> isAnswerToEverything)
```

### Library functions to replace explicit matching

Most built-in F# types have such functions already available.

For example, instead of using recursion to loop through lists, you should try to use the functions in the `List` module, which will do almost everything you need.

In particular, the function we wrote earlier:

```fsharp
let rec loopAndSum aList sumSoFar = 
    match aList with 
    | [] -> 
        sumSoFar  
    | x::xs -> 
        let newSumSoFar = sumSoFar + x
        loopAndSum xs newSumSoFar 
```

can be rewritten using the `List` module in at least three different ways!

```fsharp
// simplest
let loopAndSum1 aList = List.sum aList 
[1..10] |> loopAndSum1 

// reduce is very powerful    
let loopAndSum2 aList = List.reduce (+) aList 
[1..10] |> loopAndSum2 

// fold is most powerful of all
let loopAndSum3 aList = List.fold (fun sum i -> sum+i) 0 aList 
[1..10] |> loopAndSum3 
```

Similarly, the Option type (discussed at length in [this post](/posts/the-option-type)) has an associated `Option` module with many useful functions.

For example, a function that does a match on `Some` vs `None` can be replaced with `Option.map`:

```fsharp
// unnecessary to implement this explicitly
let addOneIfValid optionalInt = 
    match optionalInt with 
    | Some i -> Some (i + 1)
    | None -> None

Some 42 |> addOneIfValid

// much easier to use the built in function
let addOneIfValid2 optionalInt = 
    optionalInt |> Option.map (fun i->i+1)

Some 42 |> addOneIfValid2
```

<a name="folds" ></a>
### Creating "fold" functions to hide matching logic

Finally, if you create your own types which need to be frequently matched,
it is good practice to create a corresponding generic "fold" function that wraps it
nicely.

For example, here is a type for defining temperature. 

```fsharp
type TemperatureType  = F of float | C of float
```

Chances are, we will matching these cases a lot, so let's create a generic function that will do the matching for us.

```fsharp
module Temperature =
    let fold fahrenheitFunction celsiusFunction aTemp =
        match aTemp with
        | F f -> fahrenheitFunction f
        | C c -> celsiusFunction c
```

All `fold` functions follow this same general pattern:

* there is one function for each case in the union structure (or clause in the match pattern)
* finally, the actual value to match on comes last. (Why? See the post on ["designing functions for partial application"](/posts/partial-application))

Now we have our fold function, we can use it in different contexts.

Let's start by testing for a fever. We need a function for testing degrees F for fever and another one for testing degrees C for fever.

And then we combine them both using the fold function.

```fsharp
let fFever tempF =
    if tempF > 100.0 then "Fever!" else "OK"

let cFever tempC =
    if tempC > 38.0 then "Fever!" else "OK"

// combine using the fold
let isFever aTemp = Temperature.fold fFever cFever aTemp
```

And now we can test.

```fsharp
let normalTemp = C 37.0
let result1 = isFever normalTemp 

let highTemp = F 103.1
let result2 = isFever highTemp 
```

For a completely different use, let's write a temperature conversion utility.

Again we start by writing the functions for each case, and then combine them.

```fsharp
let fConversion tempF =
    let convertedValue = (tempF - 32.0) / 1.8
    TemperatureType.C convertedValue    //wrapped in type

let cConversion tempC =
    let convertedValue = (tempC * 1.8) + 32.0
    TemperatureType.F convertedValue    //wrapped in type

// combine using the fold
let convert aTemp = Temperature.fold fConversion cConversion aTemp
```

Note that the conversion functions wrap the converted values in a new `TemperatureType`, so the `convert` function has the signature: 

```fsharp
val convert : TemperatureType -> TemperatureType
```

And now we can test.

```fsharp
let c20 = C 20.0
let resultInF = convert c20

let f75 = F 75.0
let resultInC = convert f75 
```

We can even call convert twice in a row, and we should get back the same temperature that we started with!

```fsharp
let resultInC = C 20.0 |> convert |> convert
```


There will be much more discussion on folds in the upcoming series on recursion and recursive types.