---
layout: post
title: "Railway oriented programming: Carbonated edition"
description: "Three ways to implement FizzBuzz"
categories: []
---

As a follow up to the [Railway Oriented Programming](/posts/recipe-part2/) post, I thought I'd apply the same technique to the [FizzBuzz](http://imranontech.com/2007/01/24/using-fizzbuzz-to-find-developers-who-grok-coding/) problem,
and compare it with other implementations.

A large part of this post was directly <s>stolen from</s> inspired by [Dave Fayram's post on FizzBuzz](http://dave.fayr.am/posts/2012-10-4-finding-fizzbuzz.html), with some additional ideas from
[raganwald](http://weblog.raganwald.com/2007/01/dont-overthink-fizzbuzz.html).

## FizzBuzz: The imperative version

As a reminder, here are the requirements for the FizzBuzz problem:

```text
Write a program that prints the numbers from 1 to 100. 
* For multiples of three print "Fizz" instead of the number.
* For multiples of five print "Buzz". 
* For numbers which are multiples of both three and five print "FizzBuzz".
```

And here is a basic F# solution:

```fsharp
module FizzBuzz_Match = 

    let fizzBuzz i = 
        match i with
        | _ when i % 15 = 0 -> 
            printf "FizzBuzz"
        | _ when i % 3 = 0 -> 
            printf "Fizz"
        | _ when i % 5 = 0 -> 
            printf "Buzz"
        | _ -> 
            printf "%i" i

        printf "; "
   
    // do the fizzbuzz
    [1..100] |> List.iter fizzBuzz
```

I have defined a function `fizzBuzz` that, given an integer `i`, uses `match` with `when` clauses to do the various tests, and then prints the appropriate value.

Simple and straightforward, and fine for a quick hack, but there are a number of problems with this implementation.

First, note that we had to have a special case for "fifteen". We couldn't just reuse the code from the "three" and "five" cases.  And this means that if we want to add another case, such as "seven",
we also need to add special cases for all the combinations as well (that is "21", "35" and "105").  And of course, adding more numbers would lead to a combinatorial explosion of cases.

Second, the order of matching is important. If the "fifteen" case had come last in the list of patterns, the code would have run correctly, but not actually met the requirements.
And again, if we need to add new cases, we must always remember to put the largest ones first to ensure correctness. This is just the kind of thing that causes subtle bugs.

Let's try another implementation, where we reuse the code for the "three" and "five" case, and eliminate the need for a "fifteen" case altogether:

```fsharp
module FizzBuzz_IfPrime = 

    let fizzBuzz i = 
        let mutable printed = false

        if i % 3 = 0 then
            printed <- true
            printf "Fizz"

        if i % 5 = 0 then
            printed <- true
            printf "Buzz"

        if not printed then
            printf "%i" i
        
        printf "; "
    
    // do the fizzbuzz
    [1..100] |> List.iter fizzBuzz
```

In this implementation, the printed value for "15" will be correct, because both the "3" and "5" cases will be used.  And also we don't have to worry about order -- as much, anyway.

But -- these branches are no longer independent, so we have to track whether *any* branch has been used at all, so that we can handle the default case. And that has led
to the mutable variable. Mutables are a code smell in F#, so this implementation is not ideal.

However, this version *does* have the advantage that it can be easily refactored to support multiple factors, not just 3 and 5. 

Below is a version that does just this. We pass in a list of "rules" to `fizzBuzz`. Each rule consists of a factor and a corresponding label to print out.
The `fizzBuzz` function then just iterates through these rules, processing each in turn.

```fsharp
module FizzBuzz_UsingFactorRules = 

    let fizzBuzz rules i  = 
        let mutable printed = false

        for factor,label in rules do
            if i % factor = 0 then
                printed <- true
                printf "%s" label

        if not printed then
            printf "%i" i
        
        printf "; "
    
    // do the fizzbuzz
    let rules = [ (3,"Fizz"); (5,"Buzz") ]
    [1..100] |> List.iter (fizzBuzz rules)
```

If we want additional numbers to be processed, we just add them to the list of rules, like this:

```fsharp
module FizzBuzz_UsingFactorRules = 

    // existing code as above
    
    let rules2 = [ (3,"Fizz"); (5,"Buzz"); (7,"Baz") ]
    [1..105] |> List.iter (fizzBuzz rules2)
```

To sum up, we have created a very imperative implementation that would be almost the same in C#. It's flexible, but the mutable variable is a bit of a code smell. Is there another way?
 
## FizzBuzz: The pipeline version

In this next version, we'll look at using the "pipeline" model, where data is fed through a series of functions to arrive at a final result.

In this design, I envision a pipeline of functions, one to handle the "three" case, one to handle the "five" case, and so on. And at the end, the appropriate label is spat out, ready to be printed.

Here is some pseudocode to demonstrate the concept:

```fsharp
data |> handleThreeCase |> handleFiveCase |> handleAllOtherCases |> printResult
```

As an additional requirement, we want the pipeline to have *no* side effects. This means that the intermediate functions must *not* print anything.
They must instead pass any generated labels down the pipe to the end, and only at that point print the results.

### Designing the pipeline

As a first step, we need to define what data will be fed down the pipe.  

Let's start with the first function, called `handleThreeCase` in the pseudocode above. What is its input, and what is its output?

Obviously, the input is the integer being processed. But the output could be the string "Fizz" if we're lucky. Or the original integer if we're not.

So now let's think about the input to the second function, `handleFiveCase`. It needs the integer as well.
But in the case of "15" it *also* needs the string "Fizz" as well, so it can append "Buzz" to it.

Finally, the `handleAllOtherCases` function converts the int to a string, but *only* if "Fizz" or "Buzz" have not been generated yet.

It's quite obvious then, that the data structure needs to contain both the integer being processed *and* the "label so far". 

The next question is: how do we know if an upstream function has created a label?
The `handleAllOtherCases` needs to know this in order to determine whether it needs to do anything.

One way would be to use an empty string (or, horrors, a null string), but let's be good and use a `string option`.

So, here's the final data type that we will be using:

```fsharp
type Data = {i:int; label:string option}
```

### Pipeline version 1

With this data structure, we can define how `handleThreeCase` and `handleFiveCase` will work.

* First, test the input int `i` to see if it is divisible by the factor.
* If it is divisible, look at the `label` -- if it is `None`, then replace it with `Some "Fizz"` or `Some "Buzz"`.
* If the label already has a value, then append "Buzz" (or whatever) to it.
* If the input is not divisible by the factor, just pass on the data unchanged.

Given this design, here's the implementation. It's a generic function that I will call `carbonate` (after [raganwald](http://weblog.raganwald.com/2007/01/dont-overthink-fizzbuzz.html))
because it works with both "Fizz" and "Buzz":

```fsharp
let carbonate factor label data = 
    let {i=i; label=labelSoFar} = data
    if i % factor = 0 then
        // pass on a new data record
        let newLabel = 
            match labelSoFar with
            | Some s -> s + label 
            | None -> label 
        {data with label=Some newLabel}
    else
        // pass on the unchanged data
        data
```

The design for the `handleAllOtherCases` function is slightly different:

* Look at the label -- if it is not `None`, then a previous function has created a label, so do nothing.
* But if the label is `None`, replace it with the string representation of the integer.

Here's the code -- I will call it `labelOrDefault`:

```fsharp
let labelOrDefault data = 
    let {i=i; label=labelSoFar} = data
    match labelSoFar with
    | Some s -> s
    | None -> sprintf "%i" i
```

Now that we have the components, we can assemble the pipeline:

```fsharp
let fizzBuzz i = 
    {i=i; label=None}
    |> carbonate 3 "Fizz"
    |> carbonate 5 "Buzz"
    |> labelOrDefault     // convert to string
    |> printf "%s; "      // print
```

Note that we have to create an initial record using `{i=i; label=None}` for passing into the first function (`carbonate 3 "Fizz"`).

Finally, here is all the code put together:

```fsharp
module FizzBuzz_Pipeline_WithRecord = 

    type Data = {i:int; label:string option}

    let carbonate factor label data = 
        let {i=i; label=labelSoFar} = data
        if i % factor = 0 then
            // pass on a new data record
            let newLabel = 
                match labelSoFar with
                | Some s -> s + label 
                | None -> label 
            {data with label=Some newLabel}
        else
            // pass on the unchanged data
            data

    let labelOrDefault data = 
        let {i=i; label=labelSoFar} = data
        match labelSoFar with
        | Some s -> s
        | None -> sprintf "%i" i

    let fizzBuzz i = 
        {i=i; label=None}
        |> carbonate 3 "Fizz"
        |> carbonate 5 "Buzz"
        |> labelOrDefault     // convert to string
        |> printf "%s; "      // print

    [1..100] |> List.iter fizzBuzz
```

### Pipeline version 2

Creating a new record type can be useful as a form of documentation, but in a case like this, it would probably be more idiomatic
to just use a tuple rather than creating a special data structure.

So here's a modified implementation that uses tuples. 

```fsharp
module FizzBuzz_Pipeline_WithTuple = 

    // type Data = int * string option

    let carbonate factor label data = 
        let (i,labelSoFar) = data
        if i % factor = 0 then
            // pass on a new data record
            let newLabel = 
                labelSoFar 
                |> Option.map (fun s -> s + label)
                |> defaultArg <| label 
            (i,Some newLabel)
        else
            // pass on the unchanged data
            data

    let labelOrDefault data = 
        let (i,labelSoFar) = data
        labelSoFar 
        |> defaultArg <| sprintf "%i" i

    let fizzBuzz i = 
        (i,None)   // use tuple instead of record
        |> carbonate 3 "Fizz"
        |> carbonate 5 "Buzz"
        |> labelOrDefault     // convert to string
        |> printf "%s; "      // print

    [1..100] |> List.iter fizzBuzz
```

As an exercise, try to find all the code that had to be changed.

### Eliminating explicit tests for Some and None

In the tuple code above, I have also replaced the explicit Option matching code `match .. Some .. None` with some built-in Option functions, `map` and `defaultArg`.

Here are the changes in `carbonate` :

```fsharp
// before
let newLabel = 
    match labelSoFar with
    | Some s -> s + label 
    | None -> label 

// after
let newLabel = 
    labelSoFar 
    |> Option.map (fun s -> s + label)
    |> defaultArg <| label 
```

and in `labelOrDefault`:

```fsharp
// before
match labelSoFar with
| Some s -> s
| None -> sprintf "%i" i

// after
labelSoFar 
|> defaultArg <| sprintf "%i" i
```

You might be wondering about the strange looking `|> defaultArg <|` idiom.

I am using it because the option is the *first* parameter to `defaultArg`, not the *second*, so a normal partial application won't work. But "bi-directional" piping does work, hence the strange looking code.

Here's what I mean:

```fsharp
// OK - normal usage
defaultArg myOption defaultValue

// ERROR: piping doesn't work
myOption |> defaultArg defaultValue

// OK - bi-directional piping does work
myOption |> defaultArg <| defaultValue
```

### Pipeline version 3

Our `carbonate` function is generic for any factor, so we can easily extend the code to support "rules" just as in the earlier imperative version.

But one issue seems to be that we have hard-coded the "3" and "5" cases into the pipeline, like this:

```fsharp
|> carbonate 3 "Fizz"
|> carbonate 5 "Buzz"
```

How can we dynamically add new functions into the pipeline?

The answer is quite simple. We dynamically create a function for each rule, and then combine all these functions into one using composition.

Here's a snippet to demonstrate:

```fsharp
let allRules = 
    rules
    |> List.map (fun (factor,label) -> carbonate factor label)
    |> List.reduce (>>)
```

Each rule is mapped into a function. And then the list of functions is combined into one single function using `>>`.

Putting it all together, we have this final implementation: 

```fsharp
module FizzBuzz_Pipeline_WithRules = 

    let carbonate factor label data = 
        let (i,labelSoFar) = data
        if i % factor = 0 then
            // pass on a new data record
            let newLabel = 
                labelSoFar 
                |> Option.map (fun s -> s + label)
                |> defaultArg <| label 
            (i,Some newLabel)
        else
            // pass on the unchanged data
            data

    let labelOrDefault data = 
        let (i,labelSoFar) = data
        labelSoFar 
        |> defaultArg <| sprintf "%i" i

    let fizzBuzz rules i = 

        // create a single function from all the rules
        let allRules = 
            rules
            |> List.map (fun (factor,label) -> carbonate factor label)
            |> List.reduce (>>)

        (i,None)   
        |> allRules
        |> labelOrDefault     // convert to string
        |> printf "%s; "      // print

    // test
    let rules = [ (3,"Fizz"); (5,"Buzz"); (7,"Baz") ]
    [1..105] |> List.iter (fizzBuzz rules)
```

Comparing this "pipeline" version with the previous imperative version, the design is much more functional. There are no mutables and there are no side-effects anywhere (except in the final
`printf` statement).

There is a subtle bug in the use of `List.reduce`, however. Can you see what it is?**  For a discussion of the problem and the fix, see the postscript at the bottom of this page. 

<sub>** Hint: try an empty list of rules.</sub>


## FizzBuzz: The railway oriented version

The pipeline version is a perfectly adequate functional implementation of FizzBuzz, but for fun, let's see if we can use the "two-track" design described
in the [railway oriented programming](/posts/recipe-part2/) post.

As a quick reminder, in "railway oriented programming" (a.k.a the "Either" monad), we define a union type with two cases: "Success" and "Failure", each representing a different "track".
We then connect a set of "two-track" functions together to make the railway.

Most of the functions we actually use are what I called "switch" or "points" functions, with a *one* track input, but a two-track output, one for the Success case, and one for the Failure case.
These switch functions are converted into two-track functions using a glue function called "bind".

Here is a module containing definitions of the functions we will need.

```fsharp
module RailwayCombinatorModule = 

    let (|Success|Failure|) =
        function 
        | Choice1Of2 s -> Success s
        | Choice2Of2 f -> Failure f

    /// convert a single value into a two-track result
    let succeed x = Choice1Of2 x

    /// convert a single value into a two-track result
    let fail x = Choice2Of2 x

    // appy either a success function or failure function
    let either successFunc failureFunc twoTrackInput =
        match twoTrackInput with
        | Success s -> successFunc s
        | Failure f -> failureFunc f

    // convert a switch function into a two-track function
    let bind f = 
        either f fail
```

I am using the `Choice` type here, which is built into the F# core library. But I have created some helpers to make it look like the Success/Failure type: an active pattern and two constructors.

Now, how will we adapt FizzBuzz to this?

Let's start by doing the obvious thing: defining "carbonation" as success, and an unmatched integer as a failure.

In other words, the Success track contains the labels, and the Failure track contains the ints.

Our `carbonate` "switch" function will therefore look like this:

```fsharp
let carbonate factor label i = 
    if i % factor = 0 then
        succeed label
    else
        fail i
```

This implementation is similar to the one used in the pipeline design discussed above, but it is cleaner because the input is just an int, not a record or a tuple.

Next, we need to connect the components together. The logic will be:

* if the int is already carbonated, ignore it
* if the int is not carbonated, connect it to the input of the next switch function

Here is the implementation:

```fsharp
let connect f = 
    function
    | Success x -> succeed x 
    | Failure i -> f i
```

Another way of writing this is to use the `either` function we defined in the library module:

```fsharp
let connect' f = 
    either succeed f
```

Make sure you understand that both of these implementations do exactly the same thing!

Next, we can create our "two-track" pipeline, like this:  

```fsharp
let fizzBuzz = 
    carbonate 15 "FizzBuzz"      // need the 15-FizzBuzz rule because of short-circuit
    >> connect (carbonate 3 "Fizz")
    >> connect (carbonate 5 "Buzz")
    >> either (printf "%s; ") (printf "%i; ")
```

This is superficially similar to the "one-track" pipeline, but actually uses a different technique.
The switches are connected together through composition (`>>`) rather than piping (`|>`).

As a result, the `fizzBuzz` function does not have an int parameter -- we are defining a function by combining other functions. There is no data anywhere.

A few other things have changed as well:

* We have had to reintroduce the explicit test for the "15" case. This is because we have only two tracks (success or failure).
  There is no "half-finished track" that allows the "5" case to add to the output of the "3" case.
* The `labelOrDefault` function from the previous example has been replaced with `either`. In the Success case, a string is printed. In the Failure case, an int is printed.

Here is the complete implementation:

```fsharp
module FizzBuzz_RailwayOriented_CarbonationIsSuccess = 

    open RailwayCombinatorModule 

    // carbonate a value
    let carbonate factor label i = 
        if i % factor = 0 then
            succeed label
        else
            fail i

    let connect f = 
        function
        | Success x -> succeed x 
        | Failure i -> f i

    let connect' f = 
        either succeed f

    let fizzBuzz = 
        carbonate 15 "FizzBuzz"      // need the 15-FizzBuzz rule because of short-circuit
        >> connect (carbonate 3 "Fizz")
        >> connect (carbonate 5 "Buzz")
        >> either (printf "%s; ") (printf "%i; ")

    // test
    [1..100] |> List.iter fizzBuzz
```

### Carbonation as failure?

We defined carbonation as "success"in the example above -- it seems the natural thing to do, surely.  But if you recall, in the railway oriented programming model,
"success" means that data should be passed on to the next function, while "failure" means to bypass all the intermediate functions and go straight to the end.

For FizzBuzz, the "bypass all the intermediate functions" track is the track with the carbonated labels, while the "pass on to next function" track is the one with integers.

So we should really reverse the tracks: "Failure" now means carbonation, while "Success" means no carbonation. 

By doing this, we also get to reuse the pre-defined `bind` function, rather than having to write our own `connect` function.

Here's the code with the tracks switched around:

```fsharp
module FizzBuzz_RailwayOriented_CarbonationIsFailure = 

    open RailwayCombinatorModule 

    // carbonate a value
    let carbonate factor label i = 
        if i % factor = 0 then
            fail label
        else
            succeed i

    let fizzBuzz = 
        carbonate 15 "FizzBuzz"
        >> bind (carbonate 3 "Fizz")
        >> bind (carbonate 5 "Buzz")
        >> either (printf "%i; ") (printf "%s; ") 

    // test
    [1..100] |> List.iter fizzBuzz
```

### What are the two tracks anyway?

The fact that we can swap the tracks so easily implies that that maybe there is a weakness in the design. Are we trying to use a design that doesn't fit?  

Why does one track have to be "Success" and another track "Failure" anyway? It doesn't seem to make much difference.

So, why don't we *keep* the two-track idea, but get rid of the "Success" and "Failure" labels.

Instead, we can call one track "Carbonated" and the other "Uncarbonated".

To make this work, we can define an active pattern and constructor methods, just as we did for "Success/Failure".

```fsharp
let (|Uncarbonated|Carbonated|) =
    function 
    | Choice1Of2 u -> Uncarbonated u
    | Choice2Of2 c -> Carbonated c

/// convert a single value into a two-track result
let uncarbonated x = Choice1Of2 x
let carbonated x = Choice2Of2 x
```

If you are doing domain driven design, it is good practice to write code that uses the appropriate [Ubiquitous Language](http://martinfowler.com/bliki/UbiquitousLanguage.html)
rather than language that is not applicable. 

In this case, if FizzBuzz was our domain, then our functions could now use the domain-friendly terminology of `carbonated` and `uncarbonated` rather than "success" or "failure".

```fsharp
let carbonate factor label i = 
    if i % factor = 0 then
        carbonated label
    else
        uncarbonated i

let connect f = 
    function
    | Uncarbonated i -> f i
    | Carbonated x -> carbonated x 
```

Note that, as before, the `connect` function can be rewritten using `either` (or we can just use the predefined `bind` as before):

```fsharp
let connect' f = 
    either f carbonated 
```

Here's all the code in one module:

```fsharp
module FizzBuzz_RailwayOriented_UsingCustomChoice = 

    open RailwayCombinatorModule 

    let (|Uncarbonated|Carbonated|) =
        function 
        | Choice1Of2 u -> Uncarbonated u
        | Choice2Of2 c -> Carbonated c

    /// convert a single value into a two-track result
    let uncarbonated x = Choice1Of2 x
    let carbonated x = Choice2Of2 x

    // carbonate a value
    let carbonate factor label i = 
        if i % factor = 0 then
            carbonated label
        else
            uncarbonated i

    let connect f = 
        function
        | Uncarbonated i -> f i
        | Carbonated x -> carbonated x 

    let connect' f = 
        either f carbonated 
        
    let fizzBuzz = 
        carbonate 15 "FizzBuzz"
        >> connect (carbonate 3 "Fizz")
        >> connect (carbonate 5 "Buzz")
        >> either (printf "%i; ") (printf "%s; ") 

    // test
    [1..100] |> List.iter fizzBuzz
```

### Adding rules

There are some problems with the version we have so far:

* The "15" test is ugly. Can we get rid of it and reuse the "3" and "5" cases?
* The "3" and "5" cases are hard-coded. Can we make this more dynamic?

As it happens, we can kill two birds with one stone and address both of these issues at once. 

Instead of combining all the "switch" functions in *series*, we can "add" them together in *parallel*.
In the [railway oriented programming](/posts/recipe-part2/) post, we used this technique for combining validation functions.
For FizzBuzz we are going to use it for doing all the factors at once.

The trick is to define a "append" or "concat" function for combining two functions. Once we can add two functions this way, we can continue and add as many as we like.

So given that we have two carbonation functions, how do we concat them?

Well, here are the possible cases:

* If they both have carbonated outputs, we concatenate the labels into a new carbonated label.
* If one has a carbonated output and the other doesn't, then we use the carbonated one.
* If neither has a carbonated output, then we use either uncarbonated one (they will be the same).

Here's the code:

```fsharp
// concat two carbonation functions
let (<+>) switch1 switch2 x = 
    match (switch1 x),(switch2 x) with
    | Carbonated s1,Carbonated s2 -> carbonated (s1 + s2)
    | Uncarbonated f1,Carbonated s2  -> carbonated s2
    | Carbonated s1,Uncarbonated f2 -> carbonated s1
    | Uncarbonated f1,Uncarbonated f2 -> uncarbonated f1
```

As an aside, notice that this code is almost like math, with `uncarbonated` playing the role of "zero", like this:

```text
something + something = combined somethings
zero + something = something
something + zero = something
zero + zero = zero
```

This is not a coincidence! We will see this kind of thing pop up over and over in functional code. I'll be talking about this in a future post.

Anyway, with this "concat" function in place, we can rewrite the main `fizzBuzz` like this:

```fsharp
let fizzBuzz = 
    let carbonateAll = 
        carbonate 3 "Fizz" <+> carbonate 5 "Buzz"

    carbonateAll 
    >> either (printf "%i; ") (printf "%s; ") 
```

The two `carbonate` functions are added and then passed to `either` as before.

Here is the complete code:

```fsharp
module FizzBuzz_RailwayOriented_UsingAppend = 

    open RailwayCombinatorModule 

    let (|Uncarbonated|Carbonated|) =
        function 
        | Choice1Of2 u -> Uncarbonated u
        | Choice2Of2 c -> Carbonated c

    /// convert a single value into a two-track result
    let uncarbonated x = Choice1Of2 x
    let carbonated x = Choice2Of2 x

    // concat two carbonation functions
    let (<+>) switch1 switch2 x = 
        match (switch1 x),(switch2 x) with
        | Carbonated s1,Carbonated s2 -> carbonated (s1 + s2)
        | Uncarbonated f1,Carbonated s2  -> carbonated s2
        | Carbonated s1,Uncarbonated f2 -> carbonated s1
        | Uncarbonated f1,Uncarbonated f2 -> uncarbonated f1

    // carbonate a value
    let carbonate factor label i = 
        if i % factor = 0 then
            carbonated label
        else
            uncarbonated i

    let fizzBuzz = 
        let carbonateAll = 
            carbonate 3 "Fizz" <+> carbonate 5 "Buzz"

        carbonateAll 
        >> either (printf "%i; ") (printf "%s; ") 

    // test
    [1..100] |> List.iter fizzBuzz
```

With this addition logic available, we can easily refactor the code to use rules.
Just as with the earlier "pipeline" implementation, we can use `reduce` to add all the rules together at once.

Here's the version with rules:

```fsharp
module FizzBuzz_RailwayOriented_UsingAddition = 

    // code as above
        
    let fizzBuzzPrimes rules = 
        let carbonateAll  = 
            rules
            |> List.map (fun (factor,label) -> carbonate factor label)
            |> List.reduce (<+>)
        
        carbonateAll 
        >> either (printf "%i; ") (printf "%s; ") 

    // test
    let rules = [ (3,"Fizz"); (5,"Buzz"); (7,"Baz") ]
    [1..105] |> List.iter (fizzBuzzPrimes rules)
```


## Summary

In this post, we've seen three different implementations:

* An imperative version that used mutable values and mixed side-effects with logic.
* A "pipeline" version that passed a data structure through a series of functions.
* A "railway oriented" version that had two separate tracks, and used "addition" to combine functions in parallel.

In my opinion, the imperative version is the worst design. Even though it was easy to hack out quickly, it has a number of problems that make it brittle and error-prone.

Of the two functional versions, I think the "railway oriented" version is cleaner, for this problem at least.

By using the `Choice` type rather than a tuple or special record, we made the code more elegant thoughout.
You can see the difference if you compare the pipeline version of `carbonate` with the railway oriented version of `carbonate`.

In other situations, of course, the railway oriented approach might not work, and the pipeline approach might be better. I hope this post has given some useful insight into both.

*If you're a FizzBuzz fan, check out the [Functional Reactive Programming](/posts/concurrency-reactive/) page, which has yet another variant of the problem.*

## Postscript: Be careful when using List.reduce

Be careful with `List.reduce` -- it will fail with empty lists. So if you have an empty rule set, the code will throw a `System.ArgumentException`.

In the pipeline case, you can see this by adding the following snippet to the module:

```fsharp
module FizzBuzz_Pipeline_WithRules = 

    // code as before
    
    // bug
    let emptyRules = []
    [1..105] |> List.iter (fizzBuzz emptyRules)
```

The fix is to replace `List.reduce` with `List.fold`. `List.fold` requires an additional parameter: the initial (or "zero") value.
In this case, we can use the identity function, `id`, as the initial value.

Here is the fixed version of the code:

```fsharp
let allRules = 
    rules
    |> List.map (fun (factor,label) -> carbonate factor label)
    |> List.fold (>>) id
```

Similarly, in the railway oriented example, we had:

```fsharp
let allRules = 
    rules
    |> List.map (fun (factor,label) -> carbonate factor label)
    |> List.reduce (<+>)
```    

which should be corrected to:

```fsharp
let allRules = 
    rules
    |> List.map (fun (factor,label) -> carbonate factor label)
    |> List.fold (<+>) zero
```

where `zero` is the "default" function to use if the list is empty. 

As an exercise, define the zero function for this case. (Hint: we have actually already defined it under another name).