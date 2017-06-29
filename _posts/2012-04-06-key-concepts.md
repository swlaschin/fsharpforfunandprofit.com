---
layout: post
title: "Four Key Concepts"
description: "The concepts that differentiate F# from a standard imperative language"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 6
categories: 
image: "/assets/img/four-concepts2.png"
---

In the next few posts we'll move on to demonstrating the themes of this series: conciseness, convenience, correctness, concurrency and completeness.

But before that, let's look at some of the key concepts in F# that we will meet over and over again.  F# is different in many ways from a standard imperative language like C#, but there are a few major differences that are particularly important to understand:

* **Function-oriented** rather than object-oriented
* **Expressions** rather than statements 
* **Algebraic types** for creating domain models
* **Pattern matching** for flow of control 

In later posts, these will be dealt with in much greater depth -- this is just a taster to help you understand the rest of this series.

![four key concepts](/assets/img/four-concepts2.png)

### Function-oriented rather than object-oriented

As you might expect from the term "functional programming", functions are everywhere in F#.

Of course, functions are first class entities, and can be passed around like any other value:

```fsharp
let square x = x * x

// functions as values
let squareclone = square
let result = [1..10] |> List.map squareclone

// functions taking other functions as parameters
let execFunction aFunc aParam = aFunc aParam
let result2 = execFunction square 12
```

But C# has first-class functions too, so what's so special about functional programming?

The short answer is that the function-oriented nature of F# infiltrates every part of the language and type system in a way that it does not in C#, so that things 
that are awkward or clumsy in C# are very elegant in F#.

It's hard to explain this in a few paragraphs, but here are some of the benefits that we will see demonstrated over this series of posts:

* **Building with composition**. Composition is the 'glue' that allows us build larger systems from smaller ones. This is not an optional technique, but is at the very heart of the functional style. Almost every line of code is a composable expression (see below). Composition is used to build basic functions, and then functions that use those functions, and so on. And the composition principle doesn't just apply to functions, but also to types (the product and sum types discussed below). 
* **Factoring and refactoring**. The ability to factor a problem into parts depends how easily the parts can be glued back together. Methods and classes that might seem to be indivisible in an imperative language can often be broken down into surprisingly small pieces in a functional design. These fine-grained components typically consist of (a) a few very general functions that take other functions as parameters, and (b) other helper functions that specialize the general case for a particular data structure or application.
  Once factored out, the generalized functions allow many additional operations to be programmed very easily without having to write new code. You can see a good example of a general function like this (the fold function) in the [post on extracting duplicate code from loops](/posts/conciseness-extracting-boilerplate/).
* **Good design**. Many of the principles of good design, such as "separation of concerns", "single responsibility principle", ["program to an interface, not an implementation"](/posts/convenience-functions-as-interfaces/), arise naturally as a result of a functional approach. And functional code tends to be high level and declarative in general.

The following posts in this series will have examples of how functions can make code more 
concise and convenient, and then for a deeper understanding, there is a whole series on [thinking functionally](/series/thinking-functionally.html). 

### Expressions rather than statements 

In functional languages, there are no statements, only expressions. That is, every chunk of code always returns a value, 
and larger chunks are created by combining smaller chunks using composition rather than a serialized list of statements.

If you have used LINQ or SQL you will already be familiar with expression-based languages. For example, in pure SQL, 
you cannot have assignments. Instead, you must have subqueries within larger queries. 

```sql
SELECT EmployeeName 
FROM Employees
WHERE EmployeeID IN 
	(SELECT DISTINCT ManagerID FROM Employees)  -- subquery
```

F# works in the same way -- every function definition is a single expression, not a set of statements.

And it might not be obvious, but code built from expressions is both safer and more compact than using statements. 
To see this, let's compare some statement-based code in C# with the equivalent expression-based code.  

First, the statement-based code. Statements don't return values, so you have to use temporary variables that are assigned to from within statement bodies.  

```csharp
// statement-based code in C#
int result;     
if (aBool)
{
  result = 42; 
}
Console.WriteLine("result={0}", result);
```

Because the `if-then` block is a statement, the `result` variable must be defined *outside* the statement but assigned to from *inside* the statement, which leads to issues such as:

* What initial value should `result` be set to?
* What if I forget to assign to the `result` variable?  
* What is the value of the `result` variable in the "else" case?  

For comparison, here is the same code, rewritten in an expression-oriented style:

```csharp
// expression-based code in C#
int result = (aBool) ? 42 : 0;
Console.WriteLine("result={0}", result);
```

In the expression-oriented version, none of these issues apply:  

* The `result` variable is declared at the same time that it is assigned. No variables have to be set up "outside" the expression and there is no worry about what initial value they should be set to. 
* The "else" is explicitly handled. There is no chance of forgetting to do an assignment in one of the branches.
* It is not possible to forget to assign `result`, because then the variable would not even exist!

Expression-oriented style is not a choice in F#, and it is one of the things that requires a change of approach when coming from an imperative background.

### Algebraic Types

The type system in F# is based on the concept of **algebraic types**. That is, new compound types are built by combining existing types in two different ways:

* First, a combination of values, each picked from a set of types. These are called "product" types. 
* Of, alternately, as a disjoint union representing a choice between a set of types. These are called "sum" types.

For example, given existing types `int` and `bool`, we can create a new product type that must have one of each:

```fsharp
//declare it
type IntAndBool = {intPart: int; boolPart: bool}

//use it
let x = {intPart=1; boolPart=false}
```

Alternatively, we can create a new union/sum type that has a choice between each type:

```fsharp
//declare it
type IntOrBool = 
  | IntChoice of int
  | BoolChoice of bool

//use it
let y = IntChoice 42
let z = BoolChoice true
```

These "choice" types are not available in C#, but are incredibly useful for modeling many real-world cases, such as states in a state machine (which is a surprisingly common theme in many domains).

And by combining "product" and "sum" types in this way, it is easy to create a rich set of types that accurately models any business domain.
For examples of this in action, see the posts on [low overhead type definitions](/posts/conciseness-type-definitions/) and [using the type system to ensure correct code](/posts/correctness-type-checking).

 
### Pattern matching for flow of control 

Most imperative languages offer a variety of control flow statements for branching and looping: 

* `if-then-else` (and the ternary version `bool ? if-true : if-false`)
* `case` or `switch` statements
* `for` and `foreach` loops, with `break` and `continue` 
* `while` and `until` loops
* and even the dreaded `goto`

F# does support some of these, but F# also supports the most general form of conditional expression, which is **pattern-matching**.

A typical matching expression that replaces `if-then-else` looks like this:

```fsharp
match booleanExpression with
| true -> // true branch
| false -> // false branch
```

And the replacement of `switch` might look like this:

```fsharp
match aDigit with
| 1 -> // Case when digit=1
| 2 -> // Case when digit=2
| _ -> // Case otherwise
```

Finally, loops are generally done using recursion, and typically look something like this:

```fsharp
match aList with
| [] -> 
     // Empty case 
| first::rest -> 
     // Case with at least one element.
     // Process first element, and then call 
     // recursively with the rest of the list
```

Although the match expression seems unnecessarily complicated at first, you'll see that in practice it is both elegant and powerful.

For the benefits of pattern matching, see the post on [exhaustive pattern matching](/posts/correctness-exhaustive-pattern-matching), and for a worked example that uses pattern matching heavily, see the [roman numerals example](/posts/roman-numerals/).

### Pattern matching with union types ###

We mentioned above that F# supports a "union" or "choice" type. This is used instead of inheritance to work with different variants of an underlying type. Pattern matching works seamlessly with these types to create a flow of control for each choice.

In the following example, we create a `Shape` type representing four different shapes and then define a `draw` function with different behavior for each kind of shape.
This is similar to polymorphism in an object oriented language, but based on functions.

```fsharp
type Shape =        // define a "union" of alternative structures
    | Circle of radius:int 
    | Rectangle of height:int * width:int
    | Point of x:int * y:int 
    | Polygon of pointList:(int * int) list

let draw shape =    // define a function "draw" with a shape param
  match shape with
  | Circle radius -> 
      printfn "The circle has a radius of %d" radius
  | Rectangle (height,width) -> 
      printfn "The rectangle is %d high by %d wide" height width
  | Polygon points -> 
      printfn "The polygon is made of these points %A" points
  | _ -> printfn "I don't recognize this shape"

let circle = Circle(10)
let rect = Rectangle(4,5)
let point = Point(2,3)
let polygon = Polygon( [(1,1); (2,2); (3,3)])

[circle; rect; polygon; point] |> List.iter draw
```

A few things to note:

* As usual, we didn't have to specify any types. The compiler correctly determined that the shape parameter for the "draw" function was of type `Shape`.
* The `int * int` in the definition of the `Polygon` case is a tuple, a pair of ints. If you're wondering why the types are "multiplied", see this [post on tuples](/posts/tuples/).
* You can see that the `match..with` logic not only matches against the internal structure of the shape, but also assigns values based on what is appropriate for the shape. 
* The underscore is similar to the "default" branch in a switch statement, except that in F# it is required -- every possible case must always be handled. If you comment out the line 

```fsharp
  | _ -> printfn "I don't recognize this shape"
```

see what happens when you compile!

These kinds of choice types can be simulated somewhat in C# by using subclasses or interfaces,
but there is no built in support in the C# type system for this kind of exhaustive matching with error checking.

### Behaviour-oriented design vs data-oriented design 

You might be wondering if this kind of pattern matching is a good idea? 
In an object-oriented design, checking for a particular class is an anti-pattern because you should only care about *behavior*, not about the class that implements it.

But in a pure functional design there are no objects and no behavior. There are functions and there are "dumb" data types.
Data types do not have any behavior associated with them, and functions do not contain data -- they just transform data types into other data types.

In this case, `Circle` and `Rectangle` are not actually types. The only type is `Shape` -- a choice, a discriminated union -- and these are various cases of that type.
(More about discriminated unions [here](/posts/discriminated-unions/)).

In order to work with the `Shape` type, a function needs to handle each case of the `Shape`, which it does with [pattern matching](conciseness-pattern-matching.html).



