---
layout: post
title: "The Enterprise Developer from Hell"
description: "An introduction to property-based testing"
date: 2014-12-01
updated: 2021-02-17
categories: ["TDD", "Testing"]
seriesId: "Property Based Testing"
seriesOrder: 1
---

> This post is part of the [F# Advent Calendar in English 2014](https://sergeytihon.wordpress.com/2014/11/24/f-advent-calendar-in-english-2014/) project.
> Check out all the other great posts there! And special thanks to Sergey Tihon for organizing this.

*UPDATE: I did a talk on property-based testing based on these posts. [Slides and video here.](/pbt/)*

*Also, there is now a post on [how to choose properties for property-based testing](/posts/property-based-testing-2/)*

Let's start with a discussion that I might have had once (topic changed to protect the guilty):

{{<alertwell>}}
Me to co-worker: "We need a function that will add two numbers together, would you mind implementing it?"

(a short time later)

Co-worker: "I just finished implementing the 'add' function"

Me: "Great, have you written unit tests for it?"

Co-worker: "You want tests as well?" (rolls eyes) "Ok."

(a short time later)

Co-worker: "I just wrote a test. Look! 'Given 1 + 2, I expect output is 3'. "

Co-worker: "So can we call it done now?"

Me: "Well that's only *one* test. How do you know that it doesn't fail for other inputs?"

Co-worker: "Ok, let me do another one."

(a short time later)

Co-worker: "I just wrote a another awesome test. 'Given 2 + 2, I expect output is 4'"

Me: "Yes, but you're still only testing for special cases. How do you know that it doesn't fail for other inputs you haven't thought of?"

Co-worker: "You want even *more* tests?"
(mutters "slave driver" under breath and walks away)
{{</alertwell>}}

But seriously, my imaginary co-worker's complaint has some validity: *How many tests are enough?*

So now imagine that rather than being a developer, you are a test engineer who is responsible for testing that the "add" function is implemented correctly.

Unfortunately for you, the implementation is being written by a burned-out, always lazy and often malicious programmer who I will call *The Enterprise Developer From Hell*, or "EDFH".
(The EDFH has a [cousin who you might have heard of](https://en.wikipedia.org/wiki/Bastard_Operator_From_Hell)).

You are practicing test-driven-development, enterprise-style, which means that you write a test, and then the EDFH implements code that passes the test.

So you start with a test like this (using vanilla NUnit style):

```fsharp {src=#test1}
[<Test>]
let ``When I add 1 + 2, I expect 3``() =
  let result = add 1 2
  Assert.AreEqual(3,result)
```

The EDFH then implements the `add` function like this:

```fsharp {src=#test1_edfh}
let add x y =
  if x=1 && y=2 then
    3
  else
    0
```

And your test passes!

When you complain to the EDFH, they say that they are doing TDD properly, and only writing the minimal code that will make the test pass.

Fair enough. So you write another test:

```fsharp {src=#test2}
[<Test>]
let ``When I add 2 + 2, I expect 4``() =
  let result = add 2 2
  Assert.AreEqual(4,result)
```

The EDFH then changes the implementation of the `add` function to this:

```fsharp {src=#test2_edfh}
let add x y =
  if x=1 && y=2 then
    3
  else if x=2 && y=2 then
    4
  else
    0 // all other cases
```

When you again complain to the EDFH, they point out that this approach is actually a best practice. Apparently it's called ["The Transformation Priority Premise"](http://blog.8thlight.com/uncle-bob/2013/05/27/TheTransformationPriorityPremise.html).

At this point, you start thinking that the EDFH is being malicious, and that this back-and-forth could go on forever!

## Beating the malicious programmer

So the question is, what kind of test could you write so that a malicious programmer could not create an incorrect implementation, even if they wanted to?

Well, you could start with a much larger list of known results, and mix them up a bit.

```fsharp {src=#test3}
[<Test>]
let ``Add two numbers, expect their sum``() =
  let testData = [ (1,2,3); (2,2,4); (3,5,8); (27,15,42) ]
  for (x,y,expected) in testData do
    let actual = add x y
    Assert.AreEqual(expected,actual)
```

But the EDFH is tireless, and will update the implementation to include all of these cases as well.

```fsharp {src=#test3_edfh}
let add x y =
  match x,y with
  | 1,2 -> 3
  | 2,2 -> 4
  | 3,5 -> 8
  | 27,15 -> 42
  | _ -> 0 // all other cases
```

A much better approach is to generate random numbers and use those for inputs, so that a malicious programmer could not possibly know what to do in advance.

```fsharp {src=#test4}
let rand = System.Random()
let randInt() = rand.Next()

[<Test>]
let ``Add two random numbers, expect their sum``() =
  let x = randInt()
  let y = randInt()
  let expected = x + y
  let actual = add x y
  Assert.AreEqual(expected,actual)
```

If the test looks like this, then the EDFH will be *forced* to implement the `add` function correctly!

One final improvement -- the EDFH might just get lucky and have picked numbers that work by chance, so let's repeat the random number test a number of times, say 100 times.

```fsharp {src=#test5}
[<Test>]
let ``Add two random numbers 100 times, expect their sum``() =
  for _ in [1..100] do
    let x = randInt()
    let y = randInt()
    let expected = x + y
    let actual = add x y
    Assert.AreEqual(expected,actual)
```

So now we're done!

Or are we?

## Property based testing

There's just one problem. In order to test the `add` function, you're making use of the `+` function. In other words, you are using one implementation to test another.

In some cases that is acceptable (see the use of "test oracles" in a following post), but in general, it's a bad idea to have your tests duplicate the code that you are testing!
It's a waste of time and effort, and now you have two implementations to build and keep up to date.

So if you can't test by using `+`, how *can* you test?

The answer is to create tests that focus on the *properties* of the function -- the "requirements".
These properties should be things that are true for *any* correct implementation.

So let's think about what the properties of an `add` function are.

One way of getting started is to think about how `add` differs from other similar functions.

So for example, what is the difference between `add` and `subtract`? Well, for `subtract`, the order of the parameters makes a difference, while for `add` it doesn't.

So there's a good property to start with. It doesn't depend on addition itself, but it does eliminate a whole class of incorrect implementations.

```fsharp {src=#paramorder}
[<Test>]
let addDoesNotDependOnParameterOrder() =
  for _ in [1..100] do
    let x = randInt()
    let y = randInt()
    let result1 = add x y
    let result2 = add y x // reversed params
    Assert.AreEqual(result1,result2)
```

That's a good start, but it doesn't stop the EDFH. The EDFH could still implement `add` using `x * y` and this test would pass!

So now what about the difference between `add` and `multiply`? What does addition really mean?

We could start by testing with something like this, which says that `x + x` should be the same as `x * 2`:

```fsharp {src=#paramorder2}
let result1 = add x x
let result2 = x * 2
Assert.AreEqual(result1,result2)
```

But now we are assuming the existence of multiplication!  Can we define a property that *only* depends on `add` itself?

One very useful approach is to see what happens when the function is repeated more than once. That is, what if you `add` and then `add` to the result of that?

That leads to the idea that two `add 1`s is the same as one `add 2`. Here's the test:

```fsharp {src=#add1twice}
[<Test>]
let addOneTwiceIsSameAsAddTwo() =
  for _ in [1..100] do
    let x = randInt()
    let y = randInt()
    let result1 = x |> add 1 |> add 1
    let result2 = x |> add 2
    Assert.AreEqual(result1,result2)
```

That's great! `add` works perfectly with this test, while `multiply` doesn't.

However, note that the EDFH could still implement `add` using subtraction and this test would pass!

Luckily, we have the "parameter order" test above as well.
Combining both the "parameter order" and "add 1 twice" tests should narrow it down so that there is only one correct implementation, surely?

After submitting this test suite we find out the EDFH has written an implementation that passes both these tests. Let's have a look:

```fsharp {src=#add1twice_edfh}
let add x y = 0  // malicious implementation
```

Aarrghh! What happened? Where did our approach go wrong?

Well, we forgot to force the implementation to actually use the random numbers we were generating!

So we need to ensure that the implementation does indeed *do* something with the parameters that are passed into it.
We're going to have to check that the result is somehow connected to the input in a specific way.

Is there a trivial property of `add` that we know the answer to without reimplementing our own version?

Yes!

What happens when you add zero to a number? You always get the same number back.

```fsharp {src=#addzero}
[<Test>]
let addZeroIsSameAsDoingNothing() =
  for _ in [1..100] do
    let x = randInt()
    let result1 = x |> add 0
    let result2 = x
    Assert.AreEqual(result1,result2)
```

So now we have a set of properties that can be used to test any implementation of `add`, and that force the EDFH to create a correct implementation:

## Refactoring the common code

There's quite a bit of duplicated code in these three tests. Let's do some refactoring.

First, we'll write a function called `propertyCheck` that does the work of generating 100 pairs of random ints.

`propertyCheck` will also need a parameter for the property to test. In this example, the `property` parameter will be a function that takes two ints and returns a bool:

```fsharp {src=#propertyCheck}
let propertyCheck property =
  // property has type: int -> int -> bool
  for _ in [1..100] do
    let x = randInt()
    let y = randInt()
    let result = property x y
    Assert.IsTrue(result)
```

With this in place, we can redefine one of the tests by pulling out the property into a separate function, like this:

```fsharp {src=#commutativeProperty}
let commutativeProperty x y =
  let result1 = add x y
  let result2 = add y x // reversed params
  result1 = result2

[<Test>]
let addDoesNotDependOnParameterOrder() =
  propertyCheck commutativeProperty
```

We can also do the same thing for the other two properties.

After the refactoring, the complete code looks like this:

```fsharp  {src=#propertyCheckAll}
let rand = System.Random()
let randInt() = rand.Next()

let add x y = x + y  // correct implementation

let propertyCheck property =
  // property has type: int -> int -> bool
  for _ in [1..100] do
    let x = randInt()
    let y = randInt()
    let result = property x y
    Assert.IsTrue(result)

let commutativeProperty x y =
  let result1 = add x y
  let result2 = add y x // reversed params
  result1 = result2

[<Test>]
let addDoesNotDependOnParameterOrder() =
  propertyCheck commutativeProperty

let add1TwiceIsAdd2Property x _ =
  let result1 = x |> add 1 |> add 1
  let result2 = x |> add 2
  result1 = result2

[<Test>]
let addOneTwiceIsSameAsAddTwo() =
  propertyCheck add1TwiceIsAdd2Property

let identityProperty x _ =
  let result1 = x |> add 0
  result1 = x

[<Test>]
let addZeroIsSameAsDoingNothing() =
  propertyCheck identityProperty
```


## Reviewing what we have done so far

We have defined a set of properties that any implementation of `add` should satisfy:

* The parameter order doesn't matter ("commutativity" property)
* Doing `add` twice with 1 is the same as doing `add` once with 2
* Adding zero does nothing  ("identity" property)

What's nice about these properties is that they work with *all* inputs, not just special magic numbers. But more importantly, they show us the core essence of addition.

In fact, you can take this approach to the logical conclusion and actually *define* addition as anything that has these properties.

This is exactly what mathematicians do. If you look up [addition on Wikipedia](https://en.wikipedia.org/wiki/Addition#Properties), you'll see that it is defined entirely
in terms of commutativity, associativity, identity, and so on.

You'll note that in our experiment, we missed defining "associativity", but instead created a weaker property (`x+1+1 = x+2`).
We'll see later that the EDFH can indeed write a malicious implementation that satisfies this property, and that associativity is better.

Alas, it's hard to get properties perfect on the first attempt, but even so, by using the three properties we came up with,
we have got a much higher confidence that the implementation is correct, and in fact, we have learned something too -- we have understood the requirements in a deeper way.

## Specification by properties

A collection of properties like this can be considered a *specification*.

Historically, unit tests, as well as being functional tests, have been [used as a sort of specification](https://en.wikipedia.org/wiki/Unit_testing#Documentation) as well.
But an approach to specification using properties instead of tests with "magic" data is an alternative which I think is often shorter and less ambiguous.

You might be thinking that only mathematical kinds of functions can be specified this way, but in future posts, we'll see how this approach can be used to test web services and databases too.

Of course, not every business requirement can be expressed as properties like this, and we must not neglect the social component of software development.
[Specification by example](https://en.wikipedia.org/wiki/Specification_by_example) and domain driven design can play a valuable role when working with non-technical customers.

You also might be thinking that designing all these properties is a lot of work -- and you'd be right! It is the hardest part.
In a follow-up post, I'll present some tips for coming up with properties which might reduce the effort somewhat.

But even with the extra effort involved upfront (the technical term for this activity is called "thinking about the problem", by the way) the overall time
saved by having automated tests and unambiguous specifications will more than pay for the upfront cost later.

In fact, the arguments that are used to promote the benefits of unit testing can equally well be applied to property-based testing!
So if a TDD fan tells you that they don't have the time to come up with property-based tests, then they might not be looking at the big picture.

## Introducing FsCheck

We have implemented our own property checking system, but there are quite a few problems with it:

* It only works with integer functions.
  It would be nice if we could use the same approach for functions that had string parameters, or in fact any type of parameter, including ones we defined ourselves.
* It only works with two parameter functions (and we had to ignore one of them for the `adding1TwiceIsAdding2OnceProperty` and `identity` properties).
  It would be nice if we could use the same approach for functions with any number of parameters.
* When there is a counter-example to the property, we don't know what it is! Not very helpful when the tests fail!
* There's no logging of the random numbers that we generated, and there's no way to set the seed, which means that we can't debug and reproduce errors easily.
* It's not configurable. For example, we can't easily change the number of loops from 100 to something else.

It would be nice if there was a framework that did all that for us!

Thankfully there is! The ["QuickCheck"](https://en.wikipedia.org/wiki/QuickCheck) library was originally developed for Haskell by Koen Claessen and John Hughes, and has been ported to many other languages.

The version of QuickCheck used in F# (and C# too) is the excellent ["FsCheck"](https://fscheck.github.io/FsCheck/) library created by Kurt Schelfthout.
Although based on the Haskell QuickCheck, it has some nice additional features, including integration with test frameworks such as NUnit and xUnit.

So let's look at how FsCheck would do the same thing as our homemade property-testing system.

## Using FsCheck to test the addition properties

First, you need to install FsCheck and load the DLL

If you are using F# 5 or newer, you can reference the package directly in a script, like this:

```fsharp {src=#nugetFsCheck}
#r "nuget:NUnit"
open FsCheck
```

For older versions of F#, you should download the nuget package manually, and then reference the DLL in your script:

```fsharp {src=#nugetFsCheckOld}
// 1) use "nuget install FsCheck" or similar to download
// 2) include your nuget path here
#I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
// 3) reference the DLL
#r "FsCheck.dll"
open FsCheck
```

Once FsCheck is loaded, you can use `Check.Quick` and pass in any "property" function. For now, let's just say that a "property" function is any function (with any parameters) that returns a boolean.

Here's an example using a property function called `commutativeProperty`

```fsharp {src=#quickCheckCommutativeProperty}
let add x y = x + y  // correct implementation

let commutativeProperty (x,y) =
  let result1 = add x y
  let result2 = add y x // reversed params
  result1 = result2

// check the property interactively
Check.Quick commutativeProperty
```

And here's a check of a property function called `adding1TwiceIsAdding2OnceProperty`

```fsharp {src=#quickCheckAdditionProperty}
let add1TwiceIsAdd2Property x =
  let result1 = x |> add 1 |> add 1
  let result2 = x |> add 2
  result1 = result2

// check the property interactively
Check.Quick add1TwiceIsAdd2Property
```

And the identity property

```fsharp {src=#quickCheckIdentityProperty}
let identityProperty x =
  let result1 = x |> add 0
  result1 = x

// check the property interactively
Check.Quick identityProperty
```

If you check one of the properties interactively, say with `Check.Quick commutativeProperty`,  you'll see the message:

```text {src=#none}
Ok, passed 100 tests.
```

## Using FsCheck to find unsatisfied properties

Let's see what happens when we have a malicious implementation of `add`. In the code below, the EDFH implements `add` as multiplication!

That implementation *will* satisfy the commutative property, but what about the `adding1TwiceIsAdding2OnceProperty`?

```fsharp  {src=#quickCheckAdditionProperty_edfh}
let add x y =
  x * y // malicious implementation

let add1TwiceIsAdd2Property x =
  let result1 = x |> add 1 |> add 1
  let result2 = x |> add 2
  result1 = result2

// check the property interactively
Check.Quick add1TwiceIsAdd2Property
```

The result from FsCheck is:

```text {src=#quickCheckAdditionProperty_edfh_result}
Falsifiable, after 1 test (1 shrink) (StdGen (1657127138,295941511)):
1
```

That means that using `1` as the input to `adding1TwiceIsAdding2OnceProperty` will result in `false`, which you can easily see that it does.

## The return of the malicious EDFH

By using random testing, we have made it harder for a malicious implementer. They will have to change tactics now!

The EDFH notes that we are still using some magic numbers in the `adding1TwiceIsAdding2OnceProperty` -- namely 1 and 2,
and decides to create an implementation that exploits this. They'll use a correct implementation for low input values and an incorrect implementation for high input values:

```fsharp {src=#quickCheckAssoc_edfh}
let add x y =
  if (x < 10) || (y < 10) then
    x + y  // correct for low values
  else
    x * y  // incorrect for high values
```

Oh no! If we retest all our properties, they all pass now!

That'll teach us to use magic numbers in our tests!

What's the alternative? Well, let's steal from the mathematicians and create an associative property test.

```fsharp {src=#quickCheckAssoc}
let associativeProperty x y z =
  let result1 = add x (add y z)    // x + (y + z)
  let result2 = add (add x y) z    // (x + y) + z
  result1 = result2

// check the property interactively
Check.Quick associativeProperty
```

Aha! Now we get a falsification:

```text {src=#quickCheckAssoc_edfh_result}
Falsifiable, after 38 tests (4 shrinks) (StdGen (127898154,295941554)):
8
2
10
```

That means that using `(8+2)+10` is not the same as `8+(2+10)`.

Note that not only has FsCheck found some inputs that break the property, but it has found a lowest example.
It knows that the inputs `8,2,9` pass but going one higher (`8,2,10`) fails. That's very nice!

## Summary

In this post I've introduced you to the basics of property-based testing and how it differs from the familiar example-based testing.

I've also introduced the notion of the EDFH, an evil malicious programmer. You might think that such a malicious programmer is unrealistic and over-the-top.

But in many cases *you* act like an unintentionally malicious programmer. You happily create a implementation that works for some special cases,
but doesn't work more generally, not out of evil intent, but out of unawareness and blindness.

Like fish unaware of water, we are often unaware of the assumptions we make. Property-based testing can force us to become aware of them.

But how does a property-based testing library like FsCheck actually work in detail? That's the topic of the [next post](/posts/property-based-testing-1/).

{{<ghsource "/posts/property-based-testing">}}


