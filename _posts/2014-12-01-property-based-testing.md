---
layout: post
title: "An introduction to property-based testing"
description: "Or, why you should be using FsCheck and QuickCheck"
categories: ["TDD"]
---

> This post is part of the [F# Advent Calendar in English 2014](https://sergeytihon.wordpress.com/2014/11/24/f-advent-calendar-in-english-2014/) project.
> Check out all the other great posts there! And special thanks to Sergey Tihon for organizing this.

*UPDATE: I did a talk on property-based testing based on these posts. [Slides and video here.](/pbt/)*

*[Part 2 of this post](/posts/property-based-testing-2/) discusses how to choose properties for property-based testing*

Let's start with a discussion that I hope never to have:

```text
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
(mutters "slavedriver" under breath and walks away) 
```

But seriously, my imaginary co-worker's complaint has some validity: **How many tests are enough? **

So now imagine that rather than being a developer, you are a test engineer who is responsible for testing that the "add" function is implemented correctly.

Unfortunately for you, the implementation is being written by a burned-out, always lazy and often malicious programmer,
who I will call *The Enterprise Developer From Hell*, or "EDFH".
(The EDFH has a [cousin who you might have heard of](https://en.wikipedia.org/wiki/Bastard_Operator_From_Hell)).

You are practising test-driven-development, enterprise-style, which means that you write a test, and then the EDFH implements code that passes the test.
 
So you start with a test like this (using vanilla NUnit style):

```fsharp
[<Test>]
let ``When I add 1 + 2, I expect 3``()=
    let result = add 1 2
    Assert.AreEqual(3,result)
```

The EDFH then implements the `add` function like this:

```fsharp
let add x y =
    if x=1 && y=2 then 
        3
    else
        0    
```

And your test passes!

When you complain to the EDFH, they say that they are doing TDD properly, and only [writing the minimal code that will make the test pass](http://www.typemock.com/test-driven-development-tdd/).

Fair enough. So you write another test:

```fsharp
[<Test>]
let ``When I add 2 + 2, I expect 4``()=
    let result = add 2 2
    Assert.AreEqual(4,result)
```

The EDFH then changes the implementation of the `add` function to this:

```fsharp
let add x y =
    if x=1 && y=2 then 
        3
    else if x=2 && y=2 then 
        4
    else
        0    
```

When you again complain to the EDFH, they point out that this approach is actually a best practice. Apparently it's called ["The Transformation Priority Premise"](http://blog.8thlight.com/uncle-bob/2013/05/27/TheTransformationPriorityPremise.html).

At this point, you start thinking that the EDFH is being malicious, and that this back-and-forth could go on forever!

## Beating the malicious programmer

So the question is, what kind of test could you write so that a malicious programmer could not create an incorrect implementation, even if they wanted to?

Well, you could start with a much larger list of known results, and mix them up a bit.

```fsharp
[<Test>]
let ``When I add two numbers, I expect to get their sum``()=
    for (x,y,expected) in [ (1,2,3); (2,2,4); (3,5,8); (27,15,42); ]
        let actual = add x y
        Assert.AreEqual(expected,actual)
```

But the EDFH is tireless, and will update the implementation to include all of these cases as well.

A much better approach is to generate random numbers and use those for inputs, so that a malicious programmer could not possibly know what to do in advance.

```fsharp
let rand = System.Random()
let randInt() = rand.Next()

[<Test>]
let ``When I add two random numbers, I expect their sum``()=
    let x = randInt()
    let y = randInt()
    let expected = x + y
    let actual = add x y
    Assert.AreEqual(expected,actual)
```


If the test looks like this, then the EDFH will be *forced* to implement the `add` function correctly! 

One final improvement -- the EDFH might just get lucky and have picked numbers that work by chance, so let's repeat the random number test a number of times, say 100 times.

```fsharp
[<Test>]
let ``When I add two random numbers (100 times), I expect their sum``()=
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

```fsharp
[<Test>]
let ``When I add two numbers, the result should not depend on parameter order``()=
    for _ in [1..100] do
        let x = randInt()
        let y = randInt()
        let result1 = add x y
        let result2 = add y x // reversed params
        Assert.AreEqual(result1,result2)
```

That's a good start, but it doesn't stop the EDFH. The EDFH could still implement `add` using `x * y` and this test would pass!

So now what about the difference between `add` and `multiply`? What does addition really mean?

We could start by testing with something like this, which says that `x + x` should the same as `x * 2`:

```fsharp
let result1 = add x x   
let result2 = x * 2     
Assert.AreEqual(result1,result2)
```

But now we are assuming the existence of multiplication!  Can we define a property that *only* depends on `add` itself?

One very useful approach is to see what happens when the function is repeated more than once. That is, what if you `add` and then `add` to the result of that?

That leads to the idea that two `add 1`s is the same as one `add 2`. Here's the test:

```fsharp
[<Test>]
let ``Adding 1 twice is the same as adding 2``()=
    for _ in [1..100] do
        let x = randInt()
        let y = randInt()
        let result1 = x |> add 1 |> add 1
        let result2 = x |> add 2 
        Assert.AreEqual(result1,result2)
```

That's great! `add` works perfectly with this test, while `multiply` doesn't. 

However, note that the EDFH could still implement `add` using `y - x` and this test would pass!

Luckily, we have the "parameter order" test above as well.
So the combination of both of these tests should narrow it down so that there is only one correct implementation, surely?

After submitting this test suite we find out the EDFH has written an implementation that passes both these tests. Let's have a look:

```fsharp
let add x y = 0  // malicious implementation
```

Aarrghh! What happened? Where did our approach go wrong?

Well, we forgot to force the implementation to actually use the random numbers we were generating! 

So we need to ensure that the implementation does indeed *do* something with the parameters that are passed into it.
We're going to have to check that the result is somehow connected to the input in a specific way.

Is there a trivial property of `add` that we know the answer to without reimplementing our own version?

Yes! 

What happens when you add zero to a number? You always get the same number back.

```fsharp
[<Test>]
let ``Adding zero is the same as doing nothing``()=
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

`propertyCheck` will also need a parameter for the property itself. This will be a function that takes two ints and returns a bool:

```fsharp
let propertyCheck property = 
    // property has type: int -> int -> bool
    for _ in [1..100] do
        let x = randInt()
        let y = randInt()
        let result = property x y
        Assert.IsTrue(result)
```

With this in place, we can redefine one of the tests by pulling out the property into a separate function, like this:

```fsharp
let commutativeProperty x y = 
    let result1 = add x y
    let result2 = add y x // reversed params
    result1 = result2

[<Test>]
let ``When I add two numbers, the result should not depend on parameter order``()=
    propertyCheck commutativeProperty 
```

We can also do the same thing for the other two properties.

After the refactoring, the complete code looks like this:

```fsharp
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
let ``When I add two numbers, the result should not depend on parameter order``()=
    propertyCheck commutativeProperty 

let adding1TwiceIsAdding2OnceProperty x _ = 
    let result1 = x |> add 1 |> add 1
    let result2 = x |> add 2 
    result1 = result2

[<Test>]
let ``Adding 1 twice is the same as adding 2``()=
    propertyCheck adding1TwiceIsAdding2OnceProperty 

let identityProperty x _ = 
    let result1 = x |> add 0
    result1 = x

[<Test>]
let ``Adding zero is the same as doing nothing``()=
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
saved by having automated tests and unambigous specifications will more than pay for the upfront cost later.

In fact, the arguments that are used to promote the benefits of unit testing can equally well be applied to property-based testing!
So if a TDD fan tells you that they don't have the time to come up with property-based tests, then they might not be looking at the big picture.

## Introducing QuickCheck and FsCheck

We have implemented our own property checking system, but there are quite a few problems with it:

* It only works with integer functions.
  It would be nice if we could use the same approach for functions that had string parameters, or in fact any type of parameter, including ones we defined ourselves.
* It only works with two parameter functions (and we had to ignore one of them for the `adding1TwiceIsAdding2OnceProperty` and `identity` properties).
  It would be nice if we could use the same approach for functions with any number of parameters. 
* When there is a counter-example to the property, we don't know what it is! Not very helpful when the tests fail!
* There's no logging of the random numbers that we generated, and there's no way to set the seed, which means that we can't debug and reproduce errors easily.
* It's not configurable. For example, we can't easily change the number of loops from 100 to something else.

It would be nice if there was a framework that did all that for us!

Thankfully there is! The ["QuickCheck"](https://en.wikipedia.org/wiki/QuickCheck) library was originally developed for Haskell
by Koen Claessen and John Hughes, and has been ported to many other languages.

The version of QuickCheck used in F# (and C# too) is the excellent ["FsCheck"](https://fsharp.github.io/FsCheck/) library created by Kurt Schelfthout.
Although based on the Haskell QuickCheck, it has some nice additional features, including integration with test frameworks such as NUnit and xUnit.

So let's look at how FsCheck would do the same thing as our homemade property-testing system.

## Using FsCheck to test the addition properties

First, you need to install FsCheck and load the DLL (FsCheck can be a bit finicky -- see the bottom of this page for instructions and troubleshooting).

The top of your script file should look something like this:

```fsharp
System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)
#I @"Packages\FsCheck.1.0.3\lib\net45"
//#I @"Packages\FsCheck.0.9.2.0\lib\net40-Client"  // use older version for VS2012
#I @"Packages\NUnit.2.6.3\lib"
#r @"FsCheck.dll"
#r @"nunit.framework.dll"

open System
open FsCheck
open NUnit.Framework
```

Once FsCheck is loaded, you can use `Check.Quick` and pass in any "property" function. For now, let's just say that a "property" function is any function (with any parameters) that returns a boolean.

```fsharp
let add x y = x + y  // correct implementation

let commutativeProperty (x,y) = 
	let result1 = add x y
	let result2 = add y x // reversed params
	result1 = result2

// check the property interactively            
Check.Quick commutativeProperty 

let adding1TwiceIsAdding2OnceProperty x = 
	let result1 = x |> add 1 |> add 1
	let result2 = x |> add 2 
	result1 = result2

// check the property interactively            
Check.Quick adding1TwiceIsAdding2OnceProperty 

let identityProperty x = 
	let result1 = x |> add 0
	result1 = x

// check the property interactively            
Check.Quick identityProperty 
```

If you check one of the properties interactively, say with `Check.Quick commutativeProperty`,  you'll see the message:

```text
Ok, passed 100 tests.
```

## Using FsCheck to find unsatified properties

Let's see what happens when we have a malicious implementation of `add`. In the code below, the EDFH implements `add` as multiplication!

That implementation *will* satisfy the commutative property, but what about the `adding1TwiceIsAdding2OnceProperty`?

```fsharp
let add x y =
	x * y // malicious implementation

let adding1TwiceIsAdding2OnceProperty x = 
	let result1 = x |> add 1 |> add 1
	let result2 = x |> add 2 
	result1 = result2

// check the property interactively            
Check.Quick adding1TwiceIsAdding2OnceProperty 
```

The result from FsCheck is:

```text
Falsifiable, after 1 test (1 shrink) (StdGen (1657127138,295941511)):
1
```

That means that using `1` as the input to `adding1TwiceIsAdding2OnceProperty` will result in `false`, which you can easily see that it does.

## The return of the malicious EDFH

By using random testing, we have made it harder for a malicious implementor. They will have to change tactics now!

The EDFH notes that we are still using some magic numbers in the `adding1TwiceIsAdding2OnceProperty` -- namely 1 and 2,
and decides to create an implementation that exploits this. They'll use a correct implementation for low input values and an incorrect implementation for high input values:

```fsharp
let add x y = 
	if (x < 10) || (y < 10) then
		x + y  // correct for low values
	else
		x * y  // incorrect for high values
```

Oh no! If we retest all our properties, they all pass now!  

That'll teach us to use magic numbers in our tests!

What's the alternative? Well, let's steal from the mathematicians and create an associative property test.

```fsharp
let associativeProperty x y z = 
	let result1 = add x (add y z)    // x + (y + z)
	let result2 = add (add x y) z    // (x + y) + z
	result1 = result2

// check the property interactively            
Check.Quick associativeProperty 
```

Aha! Now we get a falsification:

```text
Falsifiable, after 38 tests (4 shrinks) (StdGen (127898154,295941554)):
8
2
10
```

That means that using `(8+2)+10` is not the same as `8+(2+10)`. 

Note that not only has FsCheck found some inputs that break the property, but it has found a lowest example. 
It knows that the inputs `8,2,9` pass but going one higher (`8,2,10`) fails. That's very nice!

## Understanding FsCheck: Generators

Now that we have used FsCheck for real, let's pause and have a look at how it works.

The first thing that FsCheck does is generate random inputs for you. This is called "generation", and for each type, there is an associated generator.

For example, to generate a list of sample data, you use the generator along with two parameters: the number of elements in the list and a "size".
The precise meaning of "size" depends on the type being generated and the context. Examples of things "size" is used for are: the maximum value of an int; the length of a list; the depth of a tree; etc.

Here's some code that generates ints:

```fsharp
// get the generator for ints
let intGenerator = Arb.generate<int>

// generate three ints with a maximum size of 1
Gen.sample 1 3 intGenerator    // e.g. [0; 0; -1]

// generate three ints with a maximum size of 10
Gen.sample 10 3 intGenerator   // e.g. [-4; 8; 5]

// generate three ints with a maximum size of 100
Gen.sample 100 3 intGenerator  // e.g. [-37; 24; -62] 
```

In this example, the ints are not generated uniformly, but clustered around zero.
You can see this for yourself with a little code:

```fsharp
// see how the values are clustered around the center point
intGenerator 
|> Gen.sample 10 1000 
|> Seq.groupBy id 
|> Seq.map (fun (k,v) -> (k,Seq.length v))
|> Seq.sortBy (fun (k,v) -> k)
|> Seq.toList 
```

The result is something like this:

```fsharp
[(-10, 3); (-9, 14); (-8, 18); (-7, 10); (-6, 27); (-5, 42); (-4, 49);
   (-3, 56); (-2, 76); (-1, 119); (0, 181); (1, 104); (2, 77); (3, 62);
   (4, 47); (5, 44); (6, 26); (7, 16); (8, 14); (9, 12); (10, 3)]
```

You can see that most of the values are in the center (0 is generated 181 times, 1 is generated 104 times), and the outlying values are rare (10 is generated only 3 times).

You can repeat with larger samples too. This one generates 10000 elements in the range [-30,30]

```fsharp
intGenerator 
|> Gen.sample 30 10000 
|> Seq.groupBy id 
|> Seq.map (fun (k,v) -> (k,Seq.length v))
|> Seq.sortBy (fun (k,v) -> k)
|> Seq.toList 
```

There are plenty of other generator functions available as well as `Gen.sample` (more documentation [here](https://fsharp.github.io/FsCheck/TestData.html)).

## Understanding FsCheck: Generating all sorts of types automatically

What's great about the generator logic is that it will automatically generate compound values as well.

For example, here is a generator for a tuple of three ints: 

```fsharp
let tupleGenerator = Arb.generate<int*int*int>

// generate 3 tuples with a maximum size of 1
Gen.sample 1 3 tupleGenerator 
// result: [(0, 0, 0); (0, 0, 0); (0, 1, -1)]

// generate 3 tuples with a maximum size of 10
Gen.sample 10 3 tupleGenerator 
// result: [(-6, -4, 1); (2, -2, 8); (1, -4, 5)]

// generate 3 tuples with a maximum size of 100
Gen.sample 100 3 tupleGenerator 
// result: [(-2, -36, -51); (-5, 33, 29); (13, 22, -16)]
```


Once you have a generator for a base type, `option` and `list` generators follow.
Here is a generator for `int option`s:

```fsharp
let intOptionGenerator = Arb.generate<int option>
// generate 10 int options with a maximum size of 5
Gen.sample 5 10 intOptionGenerator 
// result:  [Some 0; Some -1; Some 2; Some 0; Some 0; 
//           Some -4; null; Some 2; Some -2; Some 0]
```

And here is a generator for `int list`s:

```fsharp
let intListGenerator = Arb.generate<int list>
// generate 10 int lists with a maximum size of 5
Gen.sample 5 10 intListGenerator 
// result:  [ []; []; [-4]; [0; 3; -1; 2]; [1]; 
//            [1]; []; [0; 1; -2]; []; [-1; -2]]
```

And of course you can generate random strings too!
	
```fsharp
let stringGenerator = Arb.generate<string>

// generate 3 strings with a maximum size of 1
Gen.sample 1 3 stringGenerator 
// result: [""; "!"; "I"]

// generate 3 strings with a maximum size of 10
Gen.sample 10 3 stringGenerator 
// result: [""; "eiX$a^"; "U%0Ika&r"]
```

The best thing is that the generator will work with your own user-defined types too!


```fsharp
type Color = Red | Green of int | Blue of bool

let colorGenerator = Arb.generate<Color>

// generate 10 colors with a maximum size of 50
Gen.sample 50 10 colorGenerator 

// result:  [Green -47; Red; Red; Red; Blue true; 
//           Green 2; Blue false; Red; Blue true; Green -12]
```

Here's one that generates a user-defined record type containing another user-defined type.

```fsharp
type Point = {x:int; y:int; color: Color}

let pointGenerator = Arb.generate<Point>

// generate 10 points with a maximum size of 50
Gen.sample 50 10 pointGenerator 

(* result
[{x = -8; y = 12; color = Green -4;}; 
 {x = 28; y = -31; color = Green -6;}; 
 {x = 11; y = 27; color = Red;}; 
 {x = -2; y = -13; color = Red;};
 {x = 6; y = 12; color = Red;};
 // etc
*)
```

There are ways to have more fine-grained control over how your types are generated, but that will have to wait for another post! 

## Understanding FsCheck: Shrinking

Creating minimum counter-examples is one of the cool things about QuickCheck-style testing. 

How does it do this?  

There are two parts to the process that FsCheck uses:

First it generates a sequence of random inputs, starting small and getting bigger. This is the "generator" phase as described above.

If any inputs cause the property to fail, it starts "shrinking" the first parameter to find a smaller number.
The exact process for shrinking varies depending on the type (and you can override it too), but let's say that for numbers, they get smaller in a sensible way.

For example, let's say that you have a silly property `isSmallerThan80`:

```fsharp
let isSmallerThan80 x = x < 80
```

You have generated random numbers and found that then property fails for `100`, and you want to try a smaller number. `Arb.shrink` will generate a sequence of ints, all of which are smaller than 100.
Each one of these is tried with the property in turn until the property fails again.

```fsharp
isSmallerThan80 100 // false, so start shrinking

Arb.shrink 100 |> Seq.toList 
//  [0; 50; 75; 88; 94; 97; 99]
```

For each element in the list, test the property against it until you find another failure:

```fsharp
isSmallerThan80 0 // true
isSmallerThan80 50 // true
isSmallerThan80 75 // true
isSmallerThan80 88 // false, so shrink again
```

The property failed with `88`, so shrink again using that as a starting point:

```fsharp
Arb.shrink 88 |> Seq.toList 
//  [0; 44; 66; 77; 83; 86; 87]
isSmallerThan80 0 // true
isSmallerThan80 44 // true
isSmallerThan80 66 // true
isSmallerThan80 77 // true
isSmallerThan80 83 // false, so shrink again
```

The property failed with `83` now, so shrink again using that as a starting point:

```fsharp
Arb.shrink 83 |> Seq.toList 
//  [0; 42; 63; 73; 78; 81; 82]
// smallest failure is 81, so shrink again
```

The property failed with `81`, so shrink again using that as a starting point:

```fsharp
Arb.shrink 81 |> Seq.toList 
//  [0; 41; 61; 71; 76; 79; 80]
// smallest failure is 80
```

After this point, shrinking on 80 doesn't work -- no smaller value will be found.

In this case then, FsCheck will report that `80` falsifies the property and that 4 shrinks were needed.

Just as with generators, FsCheck will generate shrink sequences for almost any type:


```fsharp
Arb.shrink (1,2,3) |> Seq.toList 
//  [(0, 2, 3); (1, 0, 3); (1, 1, 3); (1, 2, 0); (1, 2, 2)]

Arb.shrink "abcd" |> Seq.toList 
//  ["bcd"; "acd"; "abd"; "abc"; "abca"; "abcb"; "abcc"; "abad"; "abbd"; "aacd"]

Arb.shrink [1;2;3] |> Seq.toList 
//  [[2; 3]; [1; 3]; [1; 2]; [1; 2; 0]; [1; 2; 2]; [1; 0; 3]; [1; 1; 3]; [0; 2; 3]]
```

And, as with generators, there are ways to customize how shrinking works if needed.

## Configuring FsCheck: Changing the number of tests

I mentioned a silly property `isSmallerThan80` -- let's see how FsCheck does with it.

```fsharp
// silly property to test
let isSmallerThan80 x = x < 80

Check.Quick isSmallerThan80 
// result: Ok, passed 100 tests.
```

Oh dear! FsCheck didn't find a counter-example!

At this point, we can try a few things. First, we can try increasing the number of tests.

We do this by changing the default ("Quick") configuration. There is a field called `MaxTest` that we can set. The default is 100, so let's increase it to 1000.

Finally, to use a specific config, you'll need to use `Check.One(config,property)` rather than just `Check.Quick(property)`.

```fsharp
let config = {
	Config.Quick with 
		MaxTest = 1000
	}
Check.One(config,isSmallerThan80 )
// result: Ok, passed 1000 tests.
```

Oops! FsCheck didn't find a counter-example with 1000 tests either! Let's try once more with 10000 tests:

```fsharp
let config = {
	Config.Quick with 
		MaxTest = 10000
	}
Check.One(config,isSmallerThan80 )
// result: Falsifiable, after 8660 tests (1 shrink) (StdGen (539845487,295941658)):
//         80
```

Ok, so we finally got it to work. But why did it take so many tests?

The answer lies in some other configuration settings: `StartSize` and `EndSize`.

Remember that the generators start with small numbers and gradually increase them.  This is controlled by the `StartSize` and `EndSize` settings.
By default, `StartSize` is 1 and `EndSize` is 100. So at the end of the test, the "size" parameter to the generator will be 100.

But, as we saw, even if the size is 100, very few numbers are generated at the extremes. In this case it means that numbers greater than 80 are unlikely to be generated.

So let's change the `EndSize` to something larger and see what happens!

```fsharp
let config = {
	Config.Quick with 
		EndSize = 1000
	}
Check.One(config,isSmallerThan80 )
// result: Falsifiable, after 21 tests (4 shrinks) (StdGen (1033193705,295941658)):
//         80
```

That's more like it! Only 21 tests needed now rather than 8660 tests!

## Configuring FsCheck: Verbose mode and logging

I mentioned that one of the benefits of FsCheck over a home-grown solution is the logging and reproducibility, so let's have a look at that.

We'll tweak the malicious implementation to have a boundary of `25`. Let's see how FsCheck detects this boundary via logging.

```fsharp
let add x y = 
	if (x < 25) || (y < 25) then
		x + y  // correct for low values
	else
		x * y  // incorrect for high values

let associativeProperty x y z = 
	let result1 = add x (add y z)    // x + (y + z)
	let result2 = add (add x y) z    // (x + y) + z
	result1 = result2

// check the property interactively            
Check.Quick associativeProperty 
```

The result is:

```text
Falsifiable, after 66 tests (12 shrinks) (StdGen (1706196961,295941556)):
1
24
25
```

Again, FsCheck has found that `25` is the exact boundary point quite quickly.  But how did it do it?

First, the simplest way to see what FsCheck is doing is to use "verbose" mode. That is, use `Check.Verbose` rather than `Check.Quick`:

```fsharp
// check the property interactively            
Check.Quick associativeProperty 

// with tracing/logging
Check.Verbose associativeProperty 
```

When do this, you'll see an output like that shown below. I've added all the comments to explain the various elements.

```text
0:    // test 1
-1    // param 1
-1    // param 2 
0     // param 3 
      // associativeProperty -1 -1 0  => true, keep going
1:    // test 2
0
0
0     // associativeProperty 0 0 0  => true, keep going
2:    // test 3
-2
0
-3    // associativeProperty -2 0 -3  => true, keep going
3:    // test 4
1
2
0     // associativeProperty 1 2 0  => true, keep going
// etc
49:   // test 50
46
-4
50    // associativeProperty 46 -4 50  => false, start shrinking
// etc
shrink:
35
-4
50    // associativeProperty 35 -4 50  => false, keep shrinking
shrink:
27
-4
50    // associativeProperty 27 -4 50  => false, keep shrinking
// etc
shrink:
25
1
29    // associativeProperty 25 1 29  => false, keep shrinking
shrink:
25
1
26    // associativeProperty 25 1 26  => false, keep shrinking
// next shrink fails
Falsifiable, after 50 tests (10 shrinks) (StdGen (995282583,295941602)):
25
1
26
```

This display takes up a lot of space! Can we make it more compact?

Yes -- you can control how each test and shrink is displayed by writing your own custom functions, and telling FsCheck to use them via its `Config` structure.

These functions are generic, and the list of parameters is represented by a list of unknown length (`obj list`).
But since I know I am testing a three parameter property I can hard-code a three-element list parameter and print them all on one line.

The configuration also has a slot called `Replay` which is normally `None`, which means that each run will be different. 

If you set `Replay` to `Some seed`, then the test will be replayed exactly the same way.
The seed looks like `StdGen (someInt,someInt)` and is printed on each run, so if you want to preserve a run all you need to do is paste that seed into the config.

And again, to use a specific config, you'll need to use `Check.One(config,property)` rather than just `Check.Quick(property)`.

Here's the code with the default tracing functions changed, and the replay seed set explicitly.

```fsharp
// create a function for displaying a test
let printTest testNum [x;y;z] = 
	sprintf "#%-3i %3O %3O %3O\n" testNum x y z

// create a function for displaying a shrink
let printShrink [x;y;z] = 
	sprintf "shrink %3O %3O %3O\n" x y z

// create a new FsCheck configuration
let config = {
	Config.Quick with 
		Replay = Random.StdGen (995282583,295941602) |> Some 
		Every = printTest 
		EveryShrink = printShrink
	}

// check the given property with the new configuration
Check.One(config,associativeProperty)
```

The output is now much more compact, and looks like this:

```text
#0    -1  -1   0
#1     0   0   0
#2    -2   0  -3
#3     1   2   0
#4    -4   2  -3
#5     3   0  -3
#6    -1  -1  -1
// etc
#46  -21 -25  29
#47  -10  -7 -13
#48   -4 -19  23
#49   46  -4  50
// start shrinking first parameter
shrink  35  -4  50
shrink  27  -4  50
shrink  26  -4  50
shrink  25  -4  50
// start shrinking second parameter
shrink  25   4  50
shrink  25   2  50
shrink  25   1  50
// start shrinking third parameter
shrink  25   1  38
shrink  25   1  29
shrink  25   1  26
Falsifiable, after 50 tests (10 shrinks) (StdGen (995282583,295941602)):
25
1
26
```

So there you go -- it's quite easy to customize the FsCheck logging if you need to.

Let's look at how the shrinking was done in detail.
The last set of inputs (46,-4,50) was false, so shrinking started.
 
```fsharp
// The last set of inputs (46,-4,50) was false, so shrinking started
associativeProperty 46 -4 50  // false, so shrink

// list of possible shrinks starting at 46
Arb.shrink 46 |> Seq.toList 
// result [0; 23; 35; 41; 44; 45]
```

We'll loop through the list `[0; 23; 35; 41; 44; 45]` stopping at the first element that causes the property to fail:

```fsharp
// find the next test that fails when shrinking the x parameter 
let x,y,z = (46,-4,50) 
Arb.shrink x
|> Seq.tryPick (fun x -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (35, -4, 50)
```

The first element that caused a failure was `x=35`, as part of the inputs `(35, -4, 50)`.

So now we start at 35 and shrink that:

```fsharp
// find the next test that fails when shrinking the x parameter 
let x,y,z = (35,-4,50) 
Arb.shrink x
|> Seq.tryPick (fun x -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (27, -4, 50)
```

The first element that caused a failure was now `x=27`, as part of the inputs `(27, -4, 50)`.

So now we start at 27 and keep going:

```fsharp
// find the next test that fails when shrinking the x parameter 
let x,y,z = (27,-4,50) 
Arb.shrink x
|> Seq.tryPick (fun x -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (26, -4, 50)

// find the next test that fails when shrinking the x parameter 
let x,y,z = (26,-4,50) 
Arb.shrink x
|> Seq.tryPick (fun x -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, -4, 50)

// find the next test that fails when shrinking the x parameter 
let x,y,z = (25,-4,50) 
Arb.shrink x
|> Seq.tryPick (fun x -> if associativeProperty x y z then None else Some (x,y,z) )
// answer None
```

At this point, `x=25` is as low as you can go. None of its shrink sequence caused a failure.
So we're finished with the `x` parameter!

Now we just repeat this process with the `y` parameter

```fsharp
// find the next test that fails when shrinking the y parameter 
let x,y,z = (25,-4,50) 
Arb.shrink y
|> Seq.tryPick (fun y -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 4, 50)

// find the next test that fails when shrinking the y parameter 
let x,y,z = (25,4,50) 
Arb.shrink y
|> Seq.tryPick (fun y -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 2, 50)

// find the next test that fails when shrinking the y parameter 
let x,y,z = (25,2,50) 
Arb.shrink y
|> Seq.tryPick (fun y -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 50)

// find the next test that fails when shrinking the y parameter 
let x,y,z = (25,1,50) 
Arb.shrink y
|> Seq.tryPick (fun y -> if associativeProperty x y z then None else Some (x,y,z) )
// answer None
```

At this point, `y=1` is as low as you can go. None of its shrink sequence caused a failure.
So we're finished with the `y` parameter!

Finally, we repeat this process with the `z` parameter

```fsharp
// find the next test that fails when shrinking the z parameter 
let x,y,z = (25,1,50) 
Arb.shrink z
|> Seq.tryPick (fun z -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 38)

// find the next test that fails when shrinking the z parameter 
let x,y,z = (25,1,38) 
Arb.shrink z
|> Seq.tryPick (fun z -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 29)

// find the next test that fails when shrinking the z parameter 
let x,y,z = (25,1,29) 
Arb.shrink z
|> Seq.tryPick (fun z -> if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 26)

// find the next test that fails when shrinking the z parameter 
let x,y,z = (25,1,26) 
Arb.shrink z
|> Seq.tryPick (fun z -> if associativeProperty x y z then None else Some (x,y,z) )
// answer None
```

And now we're finished with all the parameters!

The final counter-example after shrinking is `(25,1,26)`.

## Adding pre-conditions

Let's say that we have a new idea for a property to check. We'll create a property called `addition is not multiplication` which will help to stop any malicious (or even accidental) mixup in the implementations.

Here's our first attempt:
```fsharp
let additionIsNotMultiplication x y = 
	x + y <> x * y
```

Bt when we run this test, we get a failure!

```fsharp
Check.Quick additionIsNotMultiplication 
// Falsifiable, after 3 tests (0 shrinks) (StdGen (2037191079,295941699)):
// 0
// 0
```

Well duh, obviously `0+0` and `0*0` are equal. But how can we tell FsCheck to ignore just those inputs and leave all the other ones alone?

This is done via a "condition" or filter expression that is prepended to the property function using `==>` (an operator defined by FsCheck).

Here's an example:

```fsharp
let additionIsNotMultiplication x y = 
	x + y <> x * y

let preCondition x y = 
	(x,y) <> (0,0)

let additionIsNotMultiplication_withPreCondition x y = 
	preCondition x y ==> additionIsNotMultiplication x y 
```

The new property is `additionIsNotMultiplication_withPreCondition` and can be passed to `Check.Quick` just like any other property.

```fsharp
Check.Quick additionIsNotMultiplication_withPreCondition
// Falsifiable, after 38 tests (0 shrinks) (StdGen (1870180794,295941700)):
// 2
// 2
```

Oops! We forgot another case! Let's fix up our precondition again:

```fsharp
let preCondition x y = 
	(x,y) <> (0,0)
	&& (x,y) <> (2,2)

let additionIsNotMultiplication_withPreCondition x y = 
	preCondition x y ==> additionIsNotMultiplication x y 
```

And now this works.

```fsharp
Check.Quick additionIsNotMultiplication_withPreCondition
// Ok, passed 100 tests.
```
	
This kind of precondition should only be used if you want to filter out a small number of cases.

If most of the inputs will be invalid, then this filtering will be expensive. In this case there is a better way to do it, which will be discussed in a future post.

The FsCheck documentation has more on how you can tweak properties [here](https://fsharp.github.io/FsCheck/Properties.html).
	
## Naming convention for properties

These properties functions have a different purpose from "normal" functions, so how should we name them?

In the Haskell and Erlang world, properties are given a `prop_` prefix by convention. In the .NET world, it is more common to use a suffix like `AbcProperty`.

Also, in F# we have namespaces, modules, and attributes (like `[<Test>]`) that we can use to organize properties and distinguish them from other functions.

## Combining multiple properties 

Once you have a set of properties, you can combine them into a group (or even, gasp, a *specification*!), by adding them as static members of a class type.

You can then do `Check.QuickAll` and pass in the name of the class. 

For example, here are our three addition properties:

```fsharp
let add x y = x + y // good implementation

let commutativeProperty x y = 
	add x y = add y x    

let associativeProperty x y z = 
	add x (add y z) = add (add x y) z    

let leftIdentityProperty x = 
	add x 0 = x

let rightIdentityProperty x = 
	add 0 x = x
```

And here's the corresponding static class to be used with `Check.QuickAll`:
 
```fsharp
type AdditionSpecification =
	static member ``Commutative`` x y = commutativeProperty x y
	static member ``Associative`` x y z = associativeProperty x y z 
	static member ``Left Identity`` x = leftIdentityProperty x 
	static member ``Right Identity`` x = rightIdentityProperty x 

Check.QuickAll<AdditionSpecification>()
```

## Combining property-based tests with example-based tests

At the beginning of this post, I was dismissive of tests that used "magic" numbers to test a very small part of the input space.

However, I do think that example-based tests have a role that complements property-based tests. 

An example-based test is often easier to understand because it is less abstract, and so provides a good entry point and documentation in conjuction with the properties.

Here's an example:

```fsharp
type AdditionSpecification =
	static member ``Commutative`` x y = commutativeProperty x y
	static member ``Associative`` x y z = associativeProperty x y z 
	static member ``Left Identity`` x = leftIdentityProperty x 
	static member ``Right Identity`` x = rightIdentityProperty x 

	// some examples as well
	static member ``1 + 2 = 3``() =  
		add 1 2 = 3

	static member ``1 + 2 = 2 + 1``() =  
		add 1 2 = add 2 1 

	static member ``42 + 0 = 0 + 42``() =  
		add 42 0 = add 0 42 
```

## Using FsCheck from NUnit

You can use FsCheck from NUnit and other test frameworks, with an extra plugin (e.g. `FsCheck.NUnit` for Nunit).

Rather than marking a test with `Test` or `Fact`, you use the `Property` attribute.
And unlike normal tests, these tests can have parameters!

Here's an example of some tests.

```fsharp
open NUnit.Framework
open FsCheck
open FsCheck.NUnit

[<Property(QuietOnSuccess = true)>]
let ``Commutative`` x y = 
    commutativeProperty x y

[<Property(Verbose= true)>]
let ``Associative`` x y z = 
    associativeProperty x y z 
    
[<Property(EndSize=300)>]
let ``Left Identity`` x = 
    leftIdentityProperty x 
```

As you can see, you can change the configuration for each test (such as `Verbose` and `EndSize`) via properties of the annotation.

And the `QuietOnSuccess` flag is available to make FsCheck compatible with standard test frameworks, which are silent on success and only show messages if something goes wrong.


## Summary

In this post I've introduced you to the basics of property-based checking. 

There's much more to cover though! In future posts I will cover topics such as:

* **[How to come up with properties that apply to your code](/posts/property-based-testing-2)**. The properties don't have to be mathematical. 
  We'll look at more general properties such as inverses (for testing serialization/deserialization), idempotence (for safe handling of multiple updates or duplicate messages),
  and also look at test oracles.
* **How to create your own generators and shrinkers**. We've seen that FsCheck can generate random values nicely.
  But what about values with constraints such as positive numbers, or valid email addresses, or phone numbers. FsCheck gives you the tools to build your own.
* **How to do model-based testing**, and in particular, how to test for concurrency issues.

I've also introduced the notion of an evil malicious programmer. You might think that such a malicious programmer is unrealistic and over-the-top. 

But in many cases *you* act like an unintentionally malicious programmer. You happily create a implementation that works for some special cases,
but doesn't work more generally, not out of evil intent, but out of unawareness and blindness. 

Like fish unaware of water, we are often unaware of the assumptions we make. Property-based testing can force us to become aware of them.

Until next time -- happy testing!

*The code samples used in this post are [available on GitHub](https://github.com/swlaschin/PropertyBasedTesting/blob/master/part1.fsx)*.

**Want more? I have written [a follow up post on choosing properties for property-based testing](http://fsharpforfunandprofit.com/posts/property-based-testing-2/)**

*UPDATE: I did a talk on property-based testing based on these posts. [Slides and video here.](/pbt/)*

## Appendix: Installing and troubleshooting FsCheck 

The easiest way to make FsCheck available to you is to create an F# project and add the NuGet package "FsCheck.NUnit". This will install both FsCheck and NUnit in the `packages` directory.

If you are using a FSX script file for interactive development, you'll need to load the DLLs from the appropriate package location, like this:

```fsharp
// sets the current directory to be same as the script directory
System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)

// assumes nuget install FsCheck.Nunit has been run 
// so that assemblies are available under the current directory
#I @"Packages\FsCheck.1.0.3\lib\net45"
//#I @"Packages\FsCheck.0.9.2.0\lib\net40-Client"  // use older version for VS2012
#I @"Packages\NUnit.2.6.3\lib"

#r @"FsCheck.dll"
#r @"nunit.framework.dll"

open System
open FsCheck
open NUnit.Framework
```

Next, test that FsCheck is working correctly by running the following:

```fsharp
let revRevIsOrig (xs:list<int>) = List.rev(List.rev xs) = xs

Check.Quick revRevIsOrig 
```

If you get no errors, then everything is good.

If you *do* get errors, it's probably because you are on an older version of Visual Studio. Upgrade to VS2013 or failing that, do the following:

* First make sure you have the latest F# core installed ([currently 3.1](https://stackoverflow.com/questions/20332046/correct-version-of-fsharp-core)).
* Make sure your that your `app.config` has the [appropriate binding redirects](http://blog.ploeh.dk/2014/01/30/how-to-use-fsharpcore-430-when-all-you-have-is-431/).
* Make sure that your NUnit assemblies are being referenced locally rather than from the GAC.

These steps should ensure that compiled code works. 

With F# interactive, it can be trickier. If you are not using VS2013, you might run into errors such as `System.InvalidCastException: Unable to cast object of type 'Arrow'`.

The best cure for this is to upgrade to VS2013!  Failing that, you can use an older version of FsCheck, such as 0.9.2 (which I have tested successfully with VS2012)

