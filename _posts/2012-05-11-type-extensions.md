---
layout: post
title: "Attaching functions to types"
description: "Creating methods the F# way"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 11
---

Although we have focused on the pure functional style so far, sometimes it is convenient to switch to an object oriented style.
And one of the key features of the OO style is the ability to attach functions to a class and "dot into" the class to get the desired behavior.

In F#, this is done using a feature called "type extensions".  And any F# type, not just classes, can have functions attached to them.

Here's an example of attaching a function to a record type.

```fsharp
module Person = 
    type T = {First:string; Last:string} with
        // member defined with type declaration
        member this.FullName = 
            this.First + " " + this.Last

    // constructor
    let create first last = 
        {First=first; Last=last}
       
// test
let person = Person.create "John" "Doe"
let fullname = person.FullName
```

The key things to note are:

* The `with` keyword indicates the start of the list of members
* The `member` keyword shows that this is a member function (i.e. a method)
* The word `this` is a placeholder for the object that is being dotted into (called a "self-identifier"). The placeholder prefixes the function name, and then the function body then uses the same placeholder when it needs to refer to the current instance.
There is no requirement to use a particular word, just as long as it is consistent. You could use `this` or `self` or `me` or any other word that commonly indicates a self reference.

You don't have to add a member at the same time that you declare the type, you can always add it later in the same module:

```fsharp
module Person = 
    type T = {First:string; Last:string} with
       // member defined with type declaration
        member this.FullName = 
            this.First + " " + this.Last

    // constructor
    let create first last = 
        {First=first; Last=last}

    // another member added later
    type T with 
        member this.SortableName = 
            this.Last + ", " + this.First        
// test
let person = Person.create "John" "Doe"
let fullname = person.FullName
let sortableName = person.SortableName
```


These examples demonstrate what are called "intrinsic extensions". They are compiled into the type itself and are always available whenever the type is used. They also show up when you use reflection.

With intrinsic extensions, it is even possible to have a type definition that divided across several files, as long as all the components use the same namespace and are all compiled into the same assembly.
Just as with partial classes in C#, this can be useful to separate generated code from authored code.

## Optional extensions 

Another alternative is that you can add an extra member from a completely different module.
These are called "optional extensions". They are not compiled into the type itself, and require some other module to be in scope for them to work (this behavior is just like C# extension methods).

For example, let's say we have a `Person` type defined:

```fsharp
module Person = 
    type T = {First:string; Last:string} with
       // member defined with type declaration
        member this.FullName = 
            this.First + " " + this.Last

    // constructor
    let create first last = 
        {First=first; Last=last}

    // another member added later
    type T with 
        member this.SortableName = 
            this.Last + ", " + this.First        
```    

The example below demonstrates how to add an `UppercaseName` extension to it in a different module:
            
```fsharp            
// in a different module
module PersonExtensions = 

    type Person.T with 
    member this.UppercaseName = 
        this.FullName.ToUpper()
```        

So now let's test this extension:

```fsharp
let person = Person.create "John" "Doe"
let uppercaseName = person.UppercaseName 
```

Uh-oh, we have an error. What's wrong is that the `PersonExtensions` is not in scope. 
Just as for C#, any extensions have to be brought into scope in order to be used.

Once we do that, everything is fine:

```fsharp
// bring the extension into scope first!
open PersonExtensions

let person = Person.create "John" "Doe"
let uppercaseName = person.UppercaseName 
```


## Extending system types

You can extend types that are in the .NET libraries as well. But be aware that when extending a type, you must use the actual type name, not a type abbreviation. 

For example, if you try to extend `int`, you will fail, because `int` is not the true name of the type:

```fsharp
type int with
    member this.IsEven = this % 2 = 0
```

You must use `System.Int32` instead:

```fsharp
type System.Int32 with
    member this.IsEven = this % 2 = 0

//test
let i = 20
if i.IsEven then printfn "'%i' is even" i
```

## Static members

You can make the member functions static by:

* adding the keyword `static` 
* dropping the `this` placeholder

```fsharp
module Person = 
    type T = {First:string; Last:string} with
        // member defined with type declaration
        member this.FullName = 
            this.First + " " + this.Last

        // static constructor
        static member Create first last = 
            {First=first; Last=last}
      
// test
let person = Person.T.Create "John" "Doe"
let fullname = person.FullName
```

And you can create static members for system types as well:

```fsharp
type System.Int32 with
    static member IsOdd x = x % 2 = 1
    
type System.Double with
    static member Pi = 3.141

//test
let result = System.Int32.IsOdd 20 
let pi = System.Double.Pi
```

<a name="attaching-existing-functions" ></a>
## Attaching existing functions

A very common pattern is to attach pre-existing standalone functions to a type.  This has a couple of benefits:

* While developing, you can create standalone functions that refer to other standalone functions. This makes programming easier because type inference works much better with functional-style code than with OO-style ("dotting into") code.
* But for certain key functions, you can attach them to the type as well. This gives clients the choice of whether to use functional or object-oriented style.

One example of this in the F# libraries is the function that calculates a list's length. It is available as a standalone function in the `List` module, but also as a method on a list instance.

```fsharp
let list = [1..10]

// functional style
let len1 = List.length list

// OO style
let len2 = list.Length
```

In the following example, we start with a type with no members initially, then define some functions, then finally attach the `fullName` function to the type.

```fsharp
module Person = 
    // type with no members initially
    type T = {First:string; Last:string} 

    // constructor
    let create first last = 
        {First=first; Last=last}

    // standalone function            
    let fullName {First=first; Last=last} = 
        first + " " + last

    // attach preexisting function as a member 
    type T with 
        member this.FullName = fullName this
        
// test
let person = Person.create "John" "Doe"
let fullname = Person.fullName person  // functional style
let fullname2 = person.FullName        // OO style
```

The standalone `fullName` function has one parameter, the person. In the attached member, the parameter comes from the `this` self-reference.

### Attaching existing functions with multiple parameters

One nice thing is that when the previously defined function has multiple parameters, you don't have to respecify them all when doing the attachment, as long as the `this` parameter is first.

In the example below, the `hasSameFirstAndLastName` function has three parameters. Yet when we attach it, we only need to specify one! 

```fsharp
module Person = 
    // type with no members initially
    type T = {First:string; Last:string} 

    // constructor
    let create first last = 
        {First=first; Last=last}

    // standalone function            
    let hasSameFirstAndLastName (person:T) otherFirst otherLast = 
        person.First = otherFirst && person.Last = otherLast

    // attach preexisting function as a member 
    type T with 
        member this.HasSameFirstAndLastName = hasSameFirstAndLastName this
        
// test
let person = Person.create "John" "Doe"
let result1 = Person.hasSameFirstAndLastName person "bob" "smith" // functional style
let result2 = person.HasSameFirstAndLastName "bob" "smith" // OO style
```


Why does this work? Hint: think about currying and partial application!

<a name="tuple-form" ></a>
## Tuple-form methods

When we start having methods with more than one parameter, we have to make a decision:

* we could use the standard (curried) form, where parameters are separated with spaces, and partial application is supported.
* we could pass in *all* the parameters at once, comma-separated, in a single tuple.

The "curried" form is more functional, and the "tuple" form is more object-oriented.

The tuple form is also how F# interacts with the standard .NET libraries, so let's examine this approach in more detail.

As a testbed, here is a Product type with two methods, each implemented using one of the approaches.
The `CurriedTotal` and `TupleTotal` methods each do the same thing: work out the total price for a given quantity and discount.

```fsharp
type Product = {SKU:string; Price: float} with

    // curried style
    member this.CurriedTotal qty discount = 
        (this.Price * float qty) - discount

    // tuple style
    member this.TupleTotal(qty,discount) = 
        (this.Price * float qty) - discount
```

And here's some test code:

```fsharp
let product = {SKU="ABC"; Price=2.0}
let total1 = product.CurriedTotal 10 1.0 
let total2 = product.TupleTotal(10,1.0)
```

No difference so far.

We know that curried version can be partially applied:

```fsharp
let totalFor10 = product.CurriedTotal 10
let discounts = [1.0..5.0] 
let totalForDifferentDiscounts 
    = discounts |> List.map totalFor10 
```

But the tuple approach can do a few things that that the curried one can't, namely:

* Named parameters
* Optional parameters
* Overloading

### Named parameters with tuple-style parameters

The tuple-style approach supports named parameters:

```fsharp
let product = {SKU="ABC"; Price=2.0}
let total3 = product.TupleTotal(qty=10,discount=1.0)
let total4 = product.TupleTotal(discount=1.0, qty=10)
```

As you can see, when names are used, the parameter order can be changed.  

Note: if some parameters are named and some are not, the named ones must always be last.

### Optional parameters with tuple-style parameters

For tuple-style methods, you can specify an optional parameter by prefixing the parameter name with a question mark.

* If the parameter is set, it comes through as `Some value`
* If the parameter is not set, it comes through as `None`

Here's an example:

```fsharp
type Product = {SKU:string; Price: float} with

    // optional discount
    member this.TupleTotal2(qty,?discount) = 
        let extPrice = this.Price * float qty
        match discount with
        | None -> extPrice
        | Some discount -> extPrice - discount
```

And here's a test:

```fsharp
let product = {SKU="ABC"; Price=2.0}

// discount not specified
let total1 = product.TupleTotal2(10)

// discount specified
let total2 = product.TupleTotal2(10,1.0) 
```

This explicit matching of the `None` and `Some` can be tedious, and there is a slightly more elegant solution for handling optional parameters.

There is a function `defaultArg` which takes the parameter as the first argument and a default for the second argument. If the parameter is set, the value is returned.
And if not, the default value is returned.

Let's see the same code rewritten to use `defaultArg` 

```fsharp
type Product = {SKU:string; Price: float} with

    // optional discount
    member this.TupleTotal2(qty,?discount) = 
        let extPrice = this.Price * float qty
        let discount = defaultArg discount 0.0
        //return
        extPrice - discount
```

<a id="method-overloading"></a>

### Method overloading

In C#, you can have multiple methods with the same name that differ only in their function signature (e.g. different parameter types and/or number of parameters)

In the pure functional model, that does not make sense -- a function works with a particular domain type and a particular range type. 
The same function cannot work with different domains and ranges.  

However, F# *does* support method overloading, but only for methods (that is functions attached to types) and of these, only those using tuple-style parameter passing.

Here's an example, with yet another variant on the `TupleTotal` method!

```fsharp
type Product = {SKU:string; Price: float} with

    // no discount
    member this.TupleTotal3(qty) = 
        printfn "using non-discount method"
        this.Price * float qty

    // with discount
    member this.TupleTotal3(qty, discount) = 
        printfn "using discount method"
        (this.Price * float qty) - discount
```

Normally, the F# compiler would complain that there are two methods with the same name, but in this case, because they are tuple based and because their signatures are different, it is acceptable.
(To make it obvious which one is being called, I have added a small debugging message.)

And here's a test:


```fsharp
let product = {SKU="ABC"; Price=2.0}

// discount not specified
let total1 = product.TupleTotal3(10) 

// discount specified
let total2 = product.TupleTotal3(10,1.0) 
```

<a id="downsides-of-methods"></a>

## Hey! Not so fast... The downsides of using methods

If you are coming from an object-oriented background, you might be tempted to use methods everywhere, because that is what you are familiar with.
But be aware that there some major downsides to using methods as well:

* Methods don't play well with type inference
* Methods don't play well with higher order functions

In fact, by overusing methods you would be needlessly bypassing the most powerful and useful aspects of programming in F#.

Let's see what I mean.

### Methods don't play well with type inference

Let's go back to our Person example again, the one that had the same logic implemented both as a standalone function and as a method:

```fsharp
module Person = 
    // type with no members initially
    type T = {First:string; Last:string} 

    // constructor
    let create first last = 
        {First=first; Last=last}

    // standalone function            
    let fullName {First=first; Last=last} = 
        first + " " + last

    // function as a member 
    type T with 
        member this.FullName = fullName this
```

Now let's see how well each one works with type inference.  Say that I want to print the full name of a person, so I will define a function `printFullName` that takes a person as a parameter.

Here's the code using the module level standalone function.

```fsharp
open Person

// using standalone function            
let printFullName person = 
    printfn "Name is %s" (fullName person) 
    
// type inference worked:
//    val printFullName : Person.T -> unit    
```

This compiles without problems, and the type inference has correctly deduced that parameter was a person

Now let's try the "dotted" version:

```fsharp
open Person

// using method with "dotting into"
let printFullName2 person = 
    printfn "Name is %s" (person.FullName) 
```

This does not compile at all, because the type inference does not have enough information to deduce the parameter. *Any* object might implement `.FullName` -- there is just not enough to go on.

Yes, we could annotate the function with the parameter type, but that defeats the whole purpose of type inference.

### Methods don't play well with higher order functions

A similar problem happens with higher order functions. For example, let's say that, given a list of people, we want to get all their full names.

With a standalone function, this is trivial:

```fsharp
open Person

let list = [
    Person.create "Andy" "Anderson";
    Person.create "John" "Johnson"; 
    Person.create "Jack" "Jackson"]

//get all the full names at once
list |> List.map fullName
```

With object methods, we have to create special lambdas everywhere:

```fsharp
open Person

let list = [
    Person.create "Andy" "Anderson";
    Person.create "John" "Johnson"; 
    Person.create "Jack" "Jackson"]

//get all the full names at once
list |> List.map (fun p -> p.FullName)
```

And this is just a simple example. Object methods don't compose well, are hard to pipe, and so on.

So, a plea for those of you new to functionally programming. Don't use methods at all if you can, especially when you are learning.
They are a crutch that will stop you getting the full benefit from functional programming.