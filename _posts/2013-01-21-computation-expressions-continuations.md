---
layout: post
title: "Understanding continuations"
description: "How 'let' works behind the scenes"
nav: thinking-functionally
seriesId: "Computation Expressions"
seriesOrder: 2
---

In the previous post we saw how some complex code could be condensed using computation expressions.

Here's the code before using a computation expression:

```fsharp

let log p = printfn "expression is %A" p

let loggedWorkflow = 
    let x = 42
    log x
    let y = 43
    log y
    let z = x + y
    log z
    //return
    z
```

And here's the same code after using a computation expression:

```fsharp
let loggedWorkflow = 
    logger
        {
        let! x = 42
        let! y = 43
        let! z = x + y
        return z
        }
```

The use of `let!` rather than a normal `let` is important.  Can we emulate this ourselves so we can understand what is going on?  Yes, but we need to understand continuations first.

## Continuations

In imperative programming, we have the concept of "returning" from a function. When you call a function, you "go in", and then you "come out", just like pushing and popping a stack.  

Here is some typical C# code which works like this. Notice the use of the `return` keyword.

```csharp
public int Divide(int top, int bottom)
{
    if (bottom==0)
    {
        throw new InvalidOperationException("div by 0");
    }
    else
    {
        return top/bottom;
    }
}

public bool IsEven(int aNumber)
{
    var isEven = (aNumber % 2 == 0);
    return isEven;
}
```

You've seen this a million times, but there is a subtle point about this approach that you might not have considered: *the called function always decides what to do*.

For example, the implemention of `Divide` has decided that it is going to throw an exception.  But what if I don't want an exception? Maybe I want a `nullable<int>`, or maybe I am going to display it on a screen as "#DIV/0". Why throw an exception that I am immediately going to have to catch?  In other words, why not let the *caller* decide what should happen, rather the callee.

Similarly in the `IsEven` example, what am I going to do with the boolean return value? Branch on it? Or maybe print it in a report? I don't know, but again, rather than returning a boolean that the caller has to deal with, why not let the caller tell the callee what to do next?

So this is what continuations are.  A **continuation** is simply a function that you pass into another function to tell it what to do next.

Here's the same C# code rewritten to allow the caller to pass in functions which the callee uses to handle each case. If it helps, you can think of this as somewhat analogous to a visitor pattern. Or maybe not.

```csharp
public T Divide<T>(int top, int bottom, Func<T> ifZero, Func<int,T> ifSuccess)
{
    if (bottom==0)
    {
        return ifZero();
    }
    else
    {
        return ifSuccess( top/bottom );
    }
}

public T IsEven<T>(int aNumber, Func<int,T> ifOdd, Func<int,T> ifEven)
{
    if (aNumber % 2 == 0)
    {
        return ifEven(aNumber);
    }
    else
    {   return ifOdd(aNumber);
    }
}
```

Note that the C# functions have been changed to return a generic `T` now, and both continuations are a `Func` that returns a `T`.

Well, passing in lots of `Func` parameters always looks pretty ugly in C#, so it is not done very often.  But passing functions is easy in F#, so let's see how this code ports over.

Here's the "before" code:

```fsharp
let divide top bottom = 
    if (bottom=0) 
    then invalidOp "div by 0"
    else (top/bottom)
    
let isEven aNumber = 
    aNumber % 2 = 0     
```

and here's the "after" code:

```fsharp
let divide ifZero ifSuccess top bottom = 
    if (bottom=0) 
    then ifZero()
    else ifSuccess (top/bottom)
    
let isEven ifOdd ifEven aNumber = 
    if (aNumber % 2 = 0)
    then aNumber |> ifEven 
    else aNumber |> ifOdd 
```

A few things to note. First, you can see that I have put the extra functions (`ifZero`, etc) *first* in the parameter list, rather than last, as in the C# example. Why? Because I am probably going to want to use [partial application](/posts/partial-application/).

And also, in the `isEven` example, I wrote `aNumber |> ifEven` and `aNumber |> ifOdd`. This makes it clear that we are piping the current value into the continuation and the continuation is always the very last step to be evaluated.  *We will be using this exact same pattern later in this post, so make sure you understand what is going on here.*

### Continuation examples

With the power of continuations at our disposal, we can use the same `divide` function in three completely different ways, depending on what the caller wants.

Here are three scenarios we can create quickly:

* pipe the result into a message and print it,
* convert the result to an option using `None` for the bad case and `Some` for the good case,
* or throw an exception in the bad case and just return the result in the good case.

```fsharp
// Scenario 1: pipe the result into a message
// ----------------------------------------
// setup the functions to print a message
let ifZero1 () = printfn "bad"
let ifSuccess1 x = printfn "good %i" x

// use partial application
let divide1  = divide ifZero1 ifSuccess1

//test
let good1 = divide1 6 3
let bad1 = divide1 6 0

// Scenario 2: convert the result to an option
// ----------------------------------------
// setup the functions to return an Option
let ifZero2() = None
let ifSuccess2 x = Some x
let divide2  = divide ifZero2 ifSuccess2

//test
let good2 = divide2 6 3
let bad2 = divide2 6 0

// Scenario 3: throw an exception in the bad case
// ----------------------------------------
// setup the functions to throw exception
let ifZero3() = failwith "div by 0"
let ifSuccess3 x = x
let divide3  = divide ifZero3 ifSuccess3

//test
let good3 = divide3 6 3
let bad3 = divide3 6 0
```

Notice that with this approach, the caller *never* has to catch an exception from `divide` anywhere. The caller decides whether an exception will be thrown, not the callee. So not only has the `divide` function become much more reusable in different contexts,  but the cyclomatic complexity has just dropped a level as well.

The same three scenarios can be applied to the `isEven` implementation:

```fsharp
// Scenario 1: pipe the result into a message
// ----------------------------------------
// setup the functions to print a message
let ifOdd1 x = printfn "isOdd %i" x
let ifEven1 x = printfn "isEven %i" x

// use partial application
let isEven1  = isEven ifOdd1 ifEven1

//test
let good1 = isEven1 6 
let bad1 = isEven1 5

// Scenario 2: convert the result to an option
// ----------------------------------------
// setup the functions to return an Option
let ifOdd2 _ = None
let ifEven2 x = Some x
let isEven2  = isEven ifOdd2 ifEven2

//test
let good2 = isEven2 6 
let bad2 = isEven2 5

// Scenario 3: throw an exception in the bad case
// ----------------------------------------
// setup the functions to throw exception
let ifOdd3 _ = failwith "assert failed"
let ifEven3 x = x
let isEven3  = isEven ifOdd3 ifEven3

//test
let good3 = isEven3 6 
let bad3 = isEven3 5 
```

In this case, the benefits are subtler, but the same: the caller never had to handle booleans with an `if/then/else` anywhere.  There is less complexity and less chance of error.

It might seem like a trivial difference, but by passing functions around like this, we can use all our favorite functional techniques such as composition, partial application, and so on.

We have also met continuations before, in the series on [designing with types](/posts/designing-with-types-single-case-dus/). We saw that their use enabled the caller to decide what would happen in case of possible validation errors in a constructor, rather than just throwing an exception.

```fsharp
type EmailAddress = EmailAddress of string

let CreateEmailAddressWithContinuations success failure (s:string) = 
    if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then success (EmailAddress s)
        else failure "Email address must contain an @ sign"
```
        
The success function takes the email as a parameter and the error function takes a string. Both functions must return the same type, but the type is up to you.

And here is a simple example of the continuations in use. Both functions do a printf, and return nothing (i.e. unit).

```fsharp
// setup the functions 
let success (EmailAddress s) = printfn "success creating email %s" s        
let failure  msg = printfn "error creating email: %s" msg
let createEmail = CreateEmailAddressWithContinuations success failure

// test
let goodEmail = createEmail "x@example.com"
let badEmail = createEmail "example.com"
```

### Continuation passing style

Using continuations like this leads to a style of programming called "[continuation passing style](http://en.wikipedia.org/wiki/Continuation-passing_style)" (or CPS), whereby *every* function is called with an extra "what to do next" function parameter.

To see the difference, let's look at the standard, direct style of programming.

When you use the direct style, you go "in" and "out" of functions, like this

```text
call a function ->
   <- return from the function
call another function ->
   <- return from the function
call yet another function ->
   <- return from the function
```
 
In continuation passing style, on the other hand, you end up with a chain of functions, like this: 
 
```text
evaluate something and pass it into ->
   a function that evaluates something and passes it into ->
      another function that evaluates something and passes it into ->
         yet another function that evaluates something and passes it into ->
            ...etc...
```

There is obviously a big difference between the two styles.

In the direct style, there is a hierarchy of functions. The top level function is a sort of "master controller" who calls one subroutine, and then another, deciding when to branch, when to loop, and generally coordinating the control flow explicitly. 

In the contination passing style, though, there is no "master controller". Instead there is a sort of "pipeline", not of data but of control flow, where the "function in charge" changes as the execution logic flows through the pipe.  

If you have ever attached a event handler to a button click in a GUI, or used a callback with [BeginInvoke](http://msdn.microsoft.com/en-us/library/2e08f6yc.aspx), then you have used this style without being aware of it. And in fact, this style will be key to understanding the `async` workflow, which I'll discuss later in this series.

## Continuations and 'let' ##

So how does all this fit in with `let`?

Let's go back and [revisit](/posts/let-use-do/) what 'let` actually does.

Remember that a (non-top-level) "let" can never be used in isolation -- it must always be part of a larger code block.

That is:

```fsharp
let x = someExpression
```

really means:

```fsharp
let x = someExpression in [an expression involving x]
```

And then every time you see the `x` in the second expression (the body expression), substitute it with the first expression (`someExpression`).

So for example, the expression:

```fsharp
let x = 42
let y = 43
let z = x + y          
```
  
really means (using the verbose `in` keyword):

```fsharp
let x = 42 in   
  let y = 43 in 
    let z = x + y in
       z    // the result
```

Now funnily enough, a lambda looks very similar to a `let`:

```fsharp
fun x -> [an expression involving x]
```

and if we pipe in the value of `x` as well, we get the following:

```fsharp
someExpression |> (fun x -> [an expression involving x] )
```

Doesn't this look awfully like a `let` to you? Here is a let and a lambda side by side:

```fsharp
// let
let x = someExpression in [an expression involving x]

// pipe a value into a lambda
someExpression |> (fun x -> [an expression involving x] )
```

They both have an `x`, and a `someExpression`, and everywhere you see `x` in the body of the lambda you replace it with  `someExpression`.
Yes, the `x` and the `someExpression` are reversed in the lambda case, but otherwise it is basically the same thing as a `let`.

So, using this technique, we can rewrite the original example in this style:

```fsharp
42 |> (fun x ->
  43 |> (fun y -> 
     x + y |> (fun z -> 
       z)))
```

When it is written this way, you can see that we have transformed the `let` style into a continuation passing style! 

* In the first line we have a value `42` -- what do we want to do with it? Let's pass it into a continuation, just as we did with the `isEven` function earlier. And in the context of the continuation, we will relabel `42` as `x`. 
* In the second line we have a value `43` -- what do we want to do with it? Let's pass it too into a continuation, calling it `y` in that context.
* In the third line we add the x and y together to create a new value. And what do we want to do with it? Another continuation, another label (`z`).
* Finally in the last line we are done and the whole expression evaluates to `z`.

### Wrapping the continuation in a function

Let's get rid of the explicit pipe and write a little function to wrap this logic. We can't call it "let" because that is a reserved word, and more importantly, the parameters are backwards from 'let'. 
The "x" is on the right hand side, and the "someExpression" is on the left hand side. So we'll call it `pipeInto` for now.

The definition of `pipeInto` is really obvious:

```fsharp
let pipeInto (someExpression,lambda) =
    someExpression |> lambda 
```

*Note that we are passing both parameters in at once using a tuple rather than as two distinct parameters separated by whitespace. They will always come as a pair.*

So, with this `pipeInto` function we can then rewrite the example once more as:
      
```fsharp
pipeInto (42, fun x ->
  pipeInto (43, fun y -> 
    pipeInto (x + y, fun z -> 
       z)))
```

or we can eliminate the indents and write it like this:
      
```fsharp
pipeInto (42, fun x ->
pipeInto (43, fun y -> 
pipeInto (x + y, fun z -> 
z)))
```

You might be thinking: so what? Why bother to wrap the pipe into a function?

The answer is that we can add *extra code* in the `pipeInto` function to do stuff "behine the scenes", just as in a computation expression.

### The "logging" example revisited ###

Let's redefine `pipeInto` to add a little bit of logging, like this:

```fsharp
let pipeInto (someExpression,lambda) =
   printfn "expression is %A" someExpression 
   someExpression |> lambda 
```

Now... run that code again.

```fsharp
pipeInto (42, fun x ->
pipeInto (43, fun y -> 
pipeInto (x + y, fun z -> 
z
)))
```

What is the output?

```text
expression is 42
expression is 43
expression is 85
```

This is exactly the same output as we had in the earlier implementations.  We have created our own little computation expression workflow!

If we compare this side by side with the computation expression version, we can see that our homebrew version is very similar to the `let!`, except that we have the parameters reversed, and we have the explicit arrow for the continuation.

![computation expression: logging](/assets/img/compexpr_logging.png)

### The "safe divide" example revisited ###

Let's do the same thing with the "safe divide" example. Here was the original code:

```fsharp
let divideBy bottom top =
    if bottom = 0
    then None
    else Some(top/bottom)

let divideByWorkflow x y w z = 
    let a = x |> divideBy y 
    match a with
    | None -> None  // give up
    | Some a' ->    // keep going
        let b = a' |> divideBy w
        match b with
        | None -> None  // give up
        | Some b' ->    // keep going
            let c = b' |> divideBy z
            match c with
            | None -> None  // give up
            | Some c' ->    // keep going
                //return 
                Some c'
```

You should see now that this "stepped" style is an obvious clue that we really should be using continuations.

Let's see if we can add extra code to `pipeInto` to do the matching for us. The logic we want is:

* If the `someExpression` parameter is `None`, then don't call the continuation lambda.
* If the `someExpression` parameter is `Some`, then do call the continuation lambda, passing in the contents of the `Some`.

Here it is:

```fsharp
let pipeInto (someExpression,lambda) =
   match someExpression with
   | None -> 
       None
   | Some x -> 
       x |> lambda 
```

With this new version of `pipeInto` we can rewrite the original code like this:

```fsharp
let divideByWorkflow x y w z = 
    let a = x |> divideBy y 
    pipeInto (a, fun a' ->
        let b = a' |> divideBy w
        pipeInto (b, fun b' ->
            let c = b' |> divideBy z
            pipeInto (c, fun c' ->
                Some c' //return 
                )))
``` 

We can clean this up quite a bit. 

First we can eliminate the `a`, `b` and `c`, and replace them with the `divideBy` expression directly. So that this:

```fsharp
let a = x |> divideBy y 
pipeInto (a, fun a' ->
``` 

becomes just this:

```fsharp
pipeInto (x |> divideBy y, fun a' ->
``` 

Now we can relabel `a'` as just `a`, and so on, and we can also remove the stepped indentation, so that we get this:

```fsharp
let divideByResult x y w z = 
    pipeInto (x |> divideBy y, fun a ->
    pipeInto (a |> divideBy w, fun b ->
    pipeInto (b |> divideBy z, fun c ->
    Some c //return 
    )))
```

Finally, we'll create a little helper function called `return'` to wrap the result in an option. Putting it all together, the code looks like this:

```fsharp
let divideBy bottom top =
    if bottom = 0
    then None
    else Some(top/bottom)

let pipeInto (someExpression,lambda) =
   match someExpression with
   | None -> 
       None
   | Some x -> 
       x |> lambda 

let return' c = Some c

let divideByWorkflow x y w z = 
    pipeInto (x |> divideBy y, fun a ->
    pipeInto (a |> divideBy w, fun b ->
    pipeInto (b |> divideBy z, fun c ->
    return' c 
    )))

let good = divideByWorkflow 12 3 2 1
let bad = divideByWorkflow 12 3 0 1
```

Again, if we compare this side by side with the computation expression version, we can see that our homebrew version is identical in meaning. Only the syntax is different.

![computation expression: logging](/assets/img/compexpr_safedivide.png)

### Summary

In this post, we talked about continuations and continuation passing style, and how we can think of `let` as a nice syntax for doing continuations behind scenes.

So now we have everything we need to start creating our *own* version of `let`. In the next post, we'll put this knowledge into practice. 

