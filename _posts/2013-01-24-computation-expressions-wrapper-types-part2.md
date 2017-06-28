---
layout: post
title: "More on wrapper types"
description: "We discover that even lists can be wrapper types"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 5
---

In the previous post, we looked at the concept of "wrapper types" and their relation to computation expressions. In this post, we'll investigate what types are suitable for being wrapper types.

## What kinds of types can be wrapper types?

If every computation expression must have an associated wrapper type, then what kinds of type can be used as wrapper types? Are there any special constraints or limitations that apply?

There is one general rule, which is:

* **Any type with a generic parameter can be used as a wrapper type**

So for example, you can use `Option<T>`, `DbResult<T>`, etc., as wrapper types, as we have seen. And you can use wrapper types that restrict the type parameter, such as `Vector<int>`.

But what about other generic types like `List<T>` or `IEnumerable<T>`? Surely they can't be used?  Actually, yes, they *can* be used! We'll see how shortly.

## Can non-generic wrapper types work?

Is it possible to use a wrapper type that does *not* have a generic parameter?

For example, we saw in an earlier example an attempt to do addition on strings, like this: `"1" + "2"`. 
Can't we be clever and treat `string` as a wrapper type for `int` in this case? That would be cool, yes?

Let's try. We can use the signatures of `Bind` and `Return` to guide our implementation.

* `Bind` takes a tuple. The first part of the tuple is the wrapped type (`string` in this case), and the second part of the tuple is a function that takes an unwrapped type and converts it to a wrapped type.  In this case, that would be `int -> string`.
* `Return` takes an unwrapped type (`int` in this case) and converts it to a wrapped type.  So in this case, the signature of `Return` would be `int -> string`.

How does this guide the implementation?

* The implementation of the "rewrapping" function, `int -> string`, is easy. It is just "toString" on an int.
* The bind function has to unwrap a string to an int, and then pass it to the function. We can use `int.Parse` for that.
* But what happens if the bind function *can't* unwrap a string, because it is not a valid number? In this case, the bind function *must* still return a wrapped type (a string), so we can just return a string such as "error".

Here's the implementation of the builder class:

```fsharp
type StringIntBuilder() =

    member this.Bind(m, f) = 
        let b,i = System.Int32.TryParse(m)
        match b,i with
        | false,_ -> "error"
        | true,i -> f i

    member this.Return(x) = 
        sprintf "%i" x

let stringint = new StringIntBuilder()
```

Now we can try using it:

```fsharp
let good = 
    stringint {
        let! i = "42"
        let! j = "43"
        return i+j
        }
printfn "good=%s" good
```

And what happens if one of the strings is invalid?

```fsharp
let bad = 
    stringint {
        let! i = "42"
        let! j = "xxx"
        return i+j
        }
printfn "bad=%s" bad
```

That looks really good -- we can treat strings as ints inside our workflow!

But hold on, there is a problem.  

Let's say we give the workflow an input, unwrap it (with `let!`) and then immediately rewrap it (with `return`) without doing anything else. What should happen?

```fsharp
let g1 = "99"
let g2 = stringint {
            let! i = g1
            return i
            }
printfn "g1=%s g2=%s" g1 g2
```

No problem. The input `g1` and the output `g2` are the same value, as we would expect.

But what about the error case?

```fsharp
let b1 = "xxx"
let b2 = stringint {
            let! i = b1
            return i
            }
printfn "b1=%s b2=%s" b1 b2
```

In this case we have got some unexpected behavior. The input `b1` and the output `b2` are *not* the same value. We have introduced an inconsistency.

Would this be a problem in practice? I don't know. But I would avoid it and use a different approach, like options, that are consistent in all cases.


## Rules for workflows that use wrapper types 

Here's a question? What is the difference between these two code fragments, and should they behave differently?

```fsharp
// fragment before refactoring
myworkflow {
    let wrapped = // some wrapped value
    let! unwrapped = wrapped
    return unwrapped 
    } 
    
// refactored fragment 
myworkflow {
    let wrapped = // some wrapped value
    return! wrapped
    } 
```

The answer is no, they should not behave differently. The only difference is that in the second example, the `unwrapped` value has been refactored away and the `wrapped` value is returned directly.

But as we just saw in the previous section, you can get inconsistencies if you are not careful.  So, any implementation you create should be sure to follow some standard rules, which are:

**Rule 1: If you start with an unwrapped value, and then you wrap it (using `return`), then unwrap it (using `bind`), you should always get back the original unwrapped value.**

This rule and the next are about not losing information as you wrap and unwrap the values. Obviously, a sensible thing to ask, and required for refactoring to work as expected.

In code, this would be expressed as something like this:

```fsharp
myworkflow {
    let originalUnwrapped = something
    
    // wrap it
    let wrapped = myworkflow { return originalUnwrapped }

    // unwrap it
    let! newUnwrapped = wrapped

    // assert they are the same
    assertEqual newUnwrapped originalUnwrapped 
    } 
```

**Rule 2: If you start with a wrapped value, and then you unwrap it (using `bind`), then wrap it (using `return`), you should always get back the original wrapped value.**

This is the rule that the `stringInt` workflow broke above. As with rule 1, this should obviously be a requirement.

In code, this would be expressed as something like this:

```fsharp
myworkflow {
    let originalWrapped = something

    let newWrapped = myworkflow { 

        // unwrap it
        let! unwrapped = originalWrapped
        
        // wrap it
        return unwrapped
        }
        
    // assert they are the same
    assertEqual newWrapped originalWrapped
    }
```

**Rule 3: If you create a child workflow, it must produce the same result as if you had "inlined" the logic in the main workflow.**

This rule is required for composition to behave properly, and again, "extraction" refactoring will only work correctly if this is true.

In general, you will get this for free if you follow some guidelines (which will be explained in a later post).

In code, this would be expressed as something like this:

```fsharp
// inlined
let result1 = myworkflow { 
    let! x = originalWrapped
    let! y = f x  // some function on x
    return! g y   // some function on y
    }

// using a child workflow ("extraction" refactoring)
let result2 = myworkflow { 
    let! y = myworkflow { 
        let! x = originalWrapped
        return! f x // some function on x
        }
    return! g y     // some function on y
    }

// rule
assertEqual result1 result2
```


## Lists as wrapper types

I said earlier that types like `List<T>` or `IEnumerable<T>` can be used as wrapper types. But how can this be? There is no one-to-one correspondence between the wrapper type and the unwrapped type!

This is where the "wrapper type" analogy becomes a bit misleading. Instead, let's go back to thinking of `bind` as a way of connecting the output of one expression with the input of another.  

As we have seen, the `bind` function "unwraps" the type, and applies the continuation function to the unwrapped value.  But there is nothing in the definition that says that there has to be only *one* unwrapped value. There is no reason that we can't apply the continuation function to each item of the list in turn. 

In other words, we should be able to write a `bind` that takes a list and a continuation function, where the continuation function processes one element at a time, like this:

```fsharp
bind( [1;2;3], fun elem -> // expression using a single element )
```

And with this concept, we should be able to chain some binds together like this:

```fsharp
let add = 
    bind( [1;2;3], fun elem1 -> 
    bind( [10;11;12], fun elem2 -> 
        elem1 + elem2
    ))
```

But we've missed something important.  The continuation function passed into `bind` is required to have a certain signature. It takes an unwrapped type, but it produces a *wrapped* type.

In other words, the continuation function must *always create a new list* as its result.

```fsharp
bind( [1;2;3], fun elem -> // expression using a single element, returning a list )
```

And the chained example would have to be written like this, with the `elem1 + elem2` result turned into a list:

```fsharp
let add = 
    bind( [1;2;3], fun elem1 -> 
    bind( [10;11;12], fun elem2 -> 
        [elem1 + elem2] // a list!
    ))
```

So the logic for our bind method now looks like this:

```fsharp
let bind(list,f) =
    // 1) for each element in list, apply f
    // 2) f will return a list (as required by its signature)
    // 3) the result is a list of lists
```

We have another issue now. `Bind` itself must produce a wrapped type, which means that the "list of lists" is no good. We need to turn them back into a simple "one-level" list.

But that is easy enough -- there is a list module function that does just that, called `concat`.

So putting it together, we have this:

```fsharp
let bind(list,f) =
    list 
    |> List.map f 
    |> List.concat

let added = 
    bind( [1;2;3], fun elem1 -> 
    bind( [10;11;12], fun elem2 -> 
//       elem1 + elem2    // error. 
        [elem1 + elem2]   // correctly returns a list.
    ))
```

Now that we understand how the `bind` works on its own, we can create a "list workflow".

* `Bind` applies the continuation function to each element of the passed in list, and then flattens the resulting list of lists into a one-level list. `List.collect` is a library function that does exactly that.
* `Return` converts from unwrapped to wrapped. In this case, that just means wrapping a single element in a list.

```fsharp
type ListWorkflowBuilder() =

    member this.Bind(list, f) = 
        list |> List.collect f 
    
    member this.Return(x) = 
        [x]

let listWorkflow = new ListWorkflowBuilder()
```

Here is the workflow in use:

```fsharp
let added = 
    listWorkflow {
        let! i = [1;2;3]
        let! j = [10;11;12]
        return i+j
        }
printfn "added=%A" added

let multiplied = 
    listWorkflow {
        let! i = [1;2;3]
        let! j = [10;11;12]
        return i*j
        }
printfn "multiplied=%A" multiplied 
```

And the results show that every element in the first collection has been combined with every element in the second collection:

```fsharp
val added : int list = [11; 12; 13; 12; 13; 14; 13; 14; 15]
val multiplied : int list = [10; 11; 12; 20; 22; 24; 30; 33; 36]
```

That's quite amazing really.  We have completely hidden the list enumeration logic, leaving just the workflow itself.

### Syntactic sugar for "for"

If we treat lists and sequences as a special case, we can add some nice syntactic sugar to replace `let!` with something a bit more natural.

What we can do is replace the `let!` with a `for..in..do` expression:

```fsharp
// let version
let! i = [1;2;3] in [some expression]

// for..in..do version
for i in [1;2;3] do [some expression]
```

Both variants mean exactly the same thing, they just look different.

To enable the F# compiler to do this, we need to add a `For` method to our builder class. It generally has exactly the same implementation as the normal `Bind` method, but is required to accept a sequence type.

```fsharp
type ListWorkflowBuilder() =

    member this.Bind(list, f) = 
        list |> List.collect f 
    
    member this.Return(x) = 
        [x]

    member this.For(list, f) = 
        this.Bind(list, f)

let listWorkflow = new ListWorkflowBuilder()
```

And here is how it is used:

```fsharp
let multiplied = 
    listWorkflow {
        for i in [1;2;3] do
        for j in [10;11;12] do
        return i*j
        }
printfn "multiplied=%A" multiplied 
```

### LINQ and the "list workflow"

Does the `for element in collection do` look familiar? It is very close to the `from element in collection ...` syntax used by LINQ. 
And indeed LINQ uses basically the same technique to convert from a query expression syntax like `from element in collection ...` to actual method calls behine the scenes. 

In F#, as we saw, the `bind` uses the `List.collect` function. The equivalent of `List.collect` in LINQ is the `SelectMany` extension method.
And once you understand how `SelectMany`  works, you can implement the same kinds of queries yourself.  Jon Skeet has written a [helpful blog post](http://codeblog.jonskeet.uk/2010/12/27/reimplementing-linq-to-objects-part-9-selectmany/) explaining this.

## The identity "wrapper type"

So we've seen a number of wrapper types in this post, and have said that *every* computation expression *must* have an associated wrapper type. 

But what about the logging example in the previous post? There was no wrapper type there.  There was a `let!` that did things behind the scenes, but the input type was the same as the output type. The type was left unchanged.

The short answer to this is that you can treat any type as its own "wrapper".  But there is another, deeper way to understand this.

Let's step back and consider what a wrapper type definition like `List<T>` really means.

If you have a type such as `List<T>`, it is in fact not a "real" type at all. `List<int>` is a real type, and `List<string>` is a real type. But `List<T>` on its own is incomplete. It is  missing the parameter it needs to become a real type.

One way to think about `List<T>` is that it is a *function*, not a type.  It is a function in the abstract world of types, rather than the concrete world of normal values, but just like any function it maps values to other values, except in this case, the input values are types (say `int` or `string`) and the output values are other types (`List<int>` and `List<string>`). And like any function it takes a parameter, in this case a "type parameter".  Which is why the concept that .NET developers call "generics" is known as "[parametric polymorphism](http://en.wikipedia.org/wiki/Parametric_polymorphism)" in computer science terminology.

Once we grasp the concept of functions that generate one type from another type (called "type constructors"), we can see that what we really mean by a "wrapper type" is just a type constructor.  

But if a "wrapper type" is just a function that maps one type to another type, surely a function that maps a type to the *same* type fits into this category? And indeed it does. The "identity" function for types fits our definition and can be used as a wrapper type for computation expressions.

Going back to some real code then, we can define the "identity workflow" as the simplest possible implementation of a workflow builder.

```fsharp
type IdentityBuilder() =
    member this.Bind(m, f) = f m
    member this.Return(x) = x
    member this.ReturnFrom(x) = x

let identity = new IdentityBuilder()

let result = identity {
    let! x = 1
    let! y = 2
    return x + y
    } 
```

With this in place, you can see that the logging example discussed earlier is just the identity workflow with some logging added in.
    
## Summary

Another long post, and we covered a lot of topics, but I hope that the role of wrapper types is now clearer. We will see how the wrapper types can be used in practice when we come to look at common workflows such as the "writer workflow" and the "state workflow" later in this series.

Here's a summary of the points covered in this post:

* A major use of computation expressions is to unwrap and rewrap values that are stored in some sort of wrapper type.
* You can easily compose computation expressions, because the output of a `Return` can be fed to the input of a `Bind`.
* Every computation expression *must* have an associated wrapper type. 
* Any type with a generic parameter can be used as a wrapper type, even lists. 
* When creating workflows, you should ensure that your implementation conforms to the three sensible rules about wrapping and unwrapping and composition.

    