---
layout: post
title: "Implementing a builder: Adding laziness"
description: "Delaying a workflow externally"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 10
---

In a [previous post](/posts/computation-expressions-builder-part3/), we saw how to avoid unnecessary evaluation of expressions in a workflow until needed. 

But that approach was designed for expressions *inside* a workflow. What happens if we want to delay the *whole workflow itself* until needed.

## The problem

Here is the code from our "maybe" builder class. This code is based on the `trace` builder from the earlier post, but with all the tracing taken out, so that it is nice and clean.

{% highlight fsharp %}
type MaybeBuilder() =

    member this.Bind(m, f) = 
        Option.bind f m

    member this.Return(x) = 
        Some x

    member this.ReturnFrom(x) = 
        x

    member this.Zero() = 
        None

    member this.Combine (a,b) = 
        match a with
        | Some _ -> a  // if a is good, skip b
        | None -> b()  // if a is bad, run b

    member this.Delay(f) = 
        f

    member this.Run(f) = 
        f()

// make an instance of the workflow                
let maybe = new MaybeBuilder()
{% endhighlight fsharp %}

Before moving on, make sure that you understand how this works. If we analyze this using the terminology of the earlier post, we can see that the types used are:

* Wrapper type: `'a option`
* Internal type: `'a option`
* Delayed type: `unit -> 'a option`

Now let's check this code and make sure everything works as expected. 

{% highlight fsharp %}
maybe { 
    printfn "Part 1: about to return 1"
    return 1
    printfn "Part 2: after return has happened"
    } |> printfn "Result for Part1 but not Part2: %A" 

// result - second part is NOT evaluated    

maybe { 
    printfn "Part 1: about to return None"
    return! None
    printfn "Part 2: after None, keep going"
    } |> printfn "Result for Part1 and then Part2: %A" 

// result - second part IS evaluated    
{% endhighlight fsharp %}

But what happens if we refactor the code into a child workflow, like this:

{% highlight fsharp %}
let childWorkflow = 
    maybe {printfn "Child workflow"} 

maybe { 
    printfn "Part 1: about to return 1"
    return 1
    return! childWorkflow 
    } |> printfn "Result for Part1 but not childWorkflow: %A" 
{% endhighlight fsharp %}

The output shows that the child workflow was evaluated even though it wasn't needed in the end. This might not be a problem in this case, but in many cases, we may not want this to happen.

So, how to avoid it?

## Wrapping the inner type in a delay

The obvious approach is to wrap the *entire result of the builder* in a delay function, and then to "run" the result, we just evaluate the delay function.

So, here's our new wrapper type:

{% highlight fsharp %}
type Maybe<'a> = Maybe of (unit -> 'a option)
{% endhighlight fsharp %}

We've replaced a simple `option` with a function that evaluates to an option, and then wrapped that function in a [single case union](/posts/designing-with-types-single-case-dus/) for good measure.

And now we need to change the `Run` method as well. Previously, it evaluated the delay function that was passed in to it, but now it should leave it unevaluated and wrap it in our new wrapper type:

{% highlight fsharp %}
// before
member this.Run(f) = 
    f()

// after    
member this.Run(f) = 
    Maybe f
{% endhighlight fsharp %}

*I've forgotten to fix up another method -- do you know which one? We'll bump into it soon!*

One more thing -- we'll need a way to "run" the result now.

{% highlight fsharp %}
let run (Maybe f) = f()
{% endhighlight fsharp %}

Let's try out our new type on our previous examples:

{% highlight fsharp %}
let m1 = maybe { 
    printfn "Part 1: about to return 1"
    return 1
    printfn "Part 2: after return has happened"
    } 
{% endhighlight fsharp %}

Running this, we get something like this:

{% highlight fsharp %}
val m1 : Maybe<int> = Maybe <fun:m1@123-7>
{% endhighlight fsharp %}

That looks good; nothing else was printed.

And now run it:

{% highlight fsharp %}
run m1 |> printfn "Result for Part1 but not Part2: %A" 
{% endhighlight fsharp %}

and we get the output:

{% highlight text %}
Part 1: about to return 1
Result for Part1 but not Part2: Some 1
{% endhighlight text %}

Perfect. Part 2 did not run.

But we run into a problem with the next example:

{% highlight fsharp %}
let m2 = maybe { 
    printfn "Part 1: about to return None"
    return! None
    printfn "Part 2: after None, keep going"
    } 
{% endhighlight fsharp %}

Oops! We forgot to fix up `ReturnFrom`!  As we know, that method takes a *wrapped type*, and we have redefined the wrapped type now.

Here's the fix:

{% highlight fsharp %}
member this.ReturnFrom(Maybe f) = 
    f()
{% endhighlight fsharp %}

We are going to accept a `Maybe` from outside, and then immediately run it to get at the option.

But now we have another problem -- we can't return an explicit `None` anymore in `return! None`, we have to return a `Maybe` type instead.  How are we going to create one of these?

Well, we could create a helper function that constructs one for us.  But there is a much simpler answer:
you can create a new `Maybe` type by using a `maybe` expression!  

{% highlight fsharp %}
let m2 = maybe { 
    return! maybe {printfn "Part 1: about to return None"}
    printfn "Part 2: after None, keep going"
    } 
{% endhighlight fsharp %}

This is why the `Zero` method is useful. With `Zero` and the builder instance, you can create new instances of the type even if they don't do anything.

But now we have one more error -- the dreaded "value restriction":

{% highlight text %}
Value restriction. The value 'm2' has been inferred to have generic type
{% endhighlight text %}

The reason why this has happened is that *both* expressions are returning `None`. But the compiler does not know what type `None` is. The code is using `None` of type `Option<obj>` (presumably because of implicit boxing) yet the compiler knows that the type can be more generic than that.

There are two fixes. One is to make the type explicit:

{% highlight fsharp %}
let m2_int: Maybe<int> = maybe { 
    return! maybe {printfn "Part 1: about to return None"}
    printfn "Part 2: after None, keep going;"
    } 
{% endhighlight fsharp %}

Or we can just return some non-None value instead:

{% highlight fsharp %}
let m2 = maybe { 
    return! maybe {printfn "Part 1: about to return None"}
    printfn "Part 2: after None, keep going;"
    return 1
    } 
{% endhighlight fsharp %}

Both of these solutions will fix the problem.

Now if we run the example, we see that the result is as expected. The second part *is* run this time.

{% highlight fsharp %}
run m2 |> printfn "Result for Part1 and then Part2: %A" 
{% endhighlight fsharp %}

The trace output:

{% highlight text %}
Part 1: about to return None
Part 2: after None, keep going;
Result for Part1 and then Part2: Some 1
{% endhighlight text %}

Finally, we'll try the child workflow examples again:

{% highlight fsharp %}
let childWorkflow = 
    maybe {printfn "Child workflow"} 

let m3 = maybe { 
    printfn "Part 1: about to return 1"
    return 1
    return! childWorkflow 
    } 

run m3 |> printfn "Result for Part1 but not childWorkflow: %A" 
{% endhighlight fsharp %}

And now the child workflow is not evaluated, just as we wanted.

And if we *do* need the child workflow to be evaluated, this works too:

{% highlight fsharp %}
let m4 = maybe { 
    return! maybe {printfn "Part 1: about to return None"}
    return! childWorkflow 
    } 

run m4 |> printfn "Result for Part1 and then childWorkflow: %A" 
{% endhighlight fsharp %}

### Reviewing the builder class 

Let's look at all the code in the new builder class again:

{% highlight fsharp %}
type Maybe<'a> = Maybe of (unit -> 'a option)

type MaybeBuilder() =

    member this.Bind(m, f) = 
        Option.bind f m

    member this.Return(x) = 
        Some x

    member this.ReturnFrom(Maybe f) = 
        f()

    member this.Zero() = 
        None

    member this.Combine (a,b) = 
        match a with
        | Some _' -> a    // if a is good, skip b
        | None -> b()     // if a is bad, run b

    member this.Delay(f) = 
        f

    member this.Run(f) = 
        Maybe f

// make an instance of the workflow                
let maybe = new MaybeBuilder()

let run (Maybe f) = f()
{% endhighlight fsharp %}

If we analyze this new builder using the terminology of the earlier post, we can see that the types used are:

* Wrapper type: `Maybe<'a>`
* Internal type: `'a option`
* Delayed type: `unit -> 'a option`

Note that in this case it was convenient to use the standard `'a option` as the internal type, because we didn't need to modify `Bind` or `Return` at all.

An alternative design might use `Maybe<'a>` as the internal type as well, which would make things more consistent, but makes the code harder to read.

## True laziness

Let's look at a variant of the last example:

{% highlight fsharp %}
let child_twice: Maybe<unit> = maybe { 
    let workflow = maybe {printfn "Child workflow"} 

    return! maybe {printfn "Part 1: about to return None"}
    return! workflow 
    return! workflow 
    } 

run child_twice |> printfn "Result for childWorkflow twice: %A" 
{% endhighlight fsharp %}
  
What should happen? How many times should the child workflow be run?

The delayed implementation above does ensure that the child workflow is only be evaluated on demand, but it does not stop it being run twice.  

In some situations, you might require that the workflow is guaranteed to only run *at most once*, and then cached ("memoized"). This is easy enough to do using the `Lazy` type that is built into F#.

The changes we need to make are:

* Change `Maybe` to wrap a `Lazy` instead of a delay
* Change `ReturnFrom` and `run` to force the evaluation of the lazy value
* Change `Run` to run the delay from inside a `lazy`

Here is the new class with the changes:

{% highlight fsharp %}
type Maybe<'a> = Maybe of Lazy<'a option>

type MaybeBuilder() =

    member this.Bind(m, f) = 
        Option.bind f m

    member this.Return(x) = 
        Some x

    member this.ReturnFrom(Maybe f) = 
        f.Force()

    member this.Zero() = 
        None

    member this.Combine (a,b) = 
        match a with
        | Some _' -> a    // if a is good, skip b
        | None -> b()     // if a is bad, run b

    member this.Delay(f) = 
        f

    member this.Run(f) = 
        Maybe (lazy f())

// make an instance of the workflow                
let maybe = new MaybeBuilder()

let run (Maybe f) = f.Force()
{% endhighlight fsharp %}

And if we run the "child twice` code from above, we get:

{% highlight text %}
Part 1: about to return None
Child workflow
Result for childWorkflow twice: <null>
{% endhighlight text %}
  
from which it is clear that the child workflow only ran once.  
  
## Summary: Immediate vs. Delayed vs. Lazy 

On this page, we've seen three different implementations of the `maybe` workflow. One that is always evaluated immediately, one that uses a delay function, and one that uses laziness with memoization.

So... which approach should you use?

There is no single "right" answer. Your choice depends on a number of things:

* *Is the code in the expression cheap to execute, and without important side-effects?* If so, stick with the first, immediate version.  It's simple and easy to understand, and this is exactly what most implementations of the `maybe` workflow use.
* *Is the code in the expression expensive to execute, might the result vary with each call (e.g. non-deterministic), or are there important side-effects?* If so, use the second, delayed version. This is exactly what most other workflows do, especially those relating to I/O (such as `async`). 
* F# does not attempt to be a purely functional language, so almost all F# code will fall into one of these two categories. But, *if you need to code in a guaranteed side-effect free style, or you just want to ensure that expensive code is evaluated at most once*, then use the third, lazy option.  

Whatever your choice, do make it clear in the documentation. For example, the delayed vs. lazy implementations appear exactly the same to the client, but they have very different semantics, and the  client code must be written differently for each case.

Now that we have finished with delays and laziness, we can go back to the builder methods and finish them off.

