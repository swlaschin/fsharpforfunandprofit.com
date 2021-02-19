---
layout: post
title: "Understanding FsCheck"
description: "Generators, shrinkers and more"
date: 2014-12-02
updated: 2021-02-17
categories: ["TDD", "Testing"]
seriesId: "Property Based Testing"
seriesOrder: 2
---


*UPDATE: I did a talk on property-based testing based on these posts. [Slides and video here.](/pbt/)*


In [the previous post](/posts/property-based-testing/), I described the basics of property-based testing, and showed how it could save a lot of time by generating random tests.

But how does it actually work in detail? That's the topic of this post.

## Understanding FsCheck: Generators

The first thing that FsCheck does is generate random inputs for you. This is called "generation", and for each type, there is an associated "generator".

```fsharp {src=#intGenerator}
// get the generator for ints
let intGenerator = Arb.generate<int>
```

`Arb` is short for "arbitrary" and `Arb.generator<T>` will return a generator for any type `T`.

To get some sample data from the generator, we can use the `Gen.sample` function. You will need to pass in a generator along with two parameters: the number of elements in the list and a "size".

The precise meaning of "size" depends on the type being generated and the context. Examples of things "size" is used for are: the maximum value of an int; the length of a list; the depth of a tree; etc.


```fsharp {src=#intGenerator1}
// generate three ints with a maximum size of 1
Gen.sample 1 3 intGenerator    // e.g. [0; 0; -1]

// generate three ints with a maximum size of 10
Gen.sample 10 3 intGenerator   // e.g. [-4; 8; 5]

// generate three ints with a maximum size of 100
Gen.sample 100 3 intGenerator  // e.g. [-37; 24; -62]
```

In this example, the ints are not generated uniformly, but clustered around zero.
You can see this for yourself with a little code:

```fsharp {src=#intGenerator2}
// see how the values are clustered around the center point
intGenerator
|> Gen.sample 10 1000
|> Seq.groupBy id  // use the generated number as key
|> Seq.map (fun (k,v) -> (k,Seq.length v)) // count the occurences
|> Seq.sortBy fst  // sort by key
|> Seq.toList
```

The result is something like this:

```fsharp {src=#intGenerator3}
// the (key, count) pairs
// see how the values are clustered around the center point of 0
[(-10, 3); (-9, 14); (-8, 18); (-7, 10); (-6, 27);
  (-5, 42); (-4, 49); (-3, 56); (-2, 76); (-1, 119);
  (0, 181); (1, 104); (2, 77); (3, 62); (4, 47); (5, 44);
  (6, 26); (7, 16); (8, 14); (9, 12); (10, 3)]
```

You can see that most of the values are in the center (0 is generated 181 times, 1 is generated 104 times), and the outlying values are rare (10 is generated only 3 times).

You can repeat with larger samples too. This one generates 10000 elements in the range [-30,30]

```fsharp {src=#intGenerator4}
intGenerator
|> Gen.sample 30 10000
|> Seq.groupBy id
|> Seq.map (fun (k,v) -> (k,Seq.length v))
|> Seq.sortBy (fun (k,v) -> k)
|> Seq.toList
```

Again, most of the numbers will be around zero.

There are plenty of other generator functions available in addition to `Gen.sample` ([more documentation here](https://fscheck.github.io/FsCheck//TestData.html)).

## Understanding FsCheck: Generating all sorts of types automatically

What's great about the generator logic is that it will automatically generate compound values as well.

For example, here is a generator for a tuple of three ints:

```fsharp {src=#tupleGenerator}
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

```fsharp {src=#intOptionGenerator}
let intOptionGenerator = Arb.generate<int option>
// generate 10 int options with a maximum size of 5
Gen.sample 5 10 intOptionGenerator
// result:  [Some 0; Some -1; Some 2; Some 0; Some 0;
//           Some -4; null; Some 2; Some -2; Some 0]
```

And here is a generator for `int list`s:

```fsharp {src=#intListGenerator}
let intListGenerator = Arb.generate<int list>
// generate 10 int lists with a maximum size of 5
Gen.sample 5 10 intListGenerator
// result:  [ []; []; [-4]; [0; 3; -1; 2]; [1];
//            [1]; []; [0; 1; -2]; []; [-1; -2]]
```

And of course you can generate random strings.

```fsharp {src=#stringGenerator}
let stringGenerator = Arb.generate<string>

// generate 3 strings with a maximum size of 1
Gen.sample 1 3 stringGenerator
// result: [""; "!"; "I"]

// generate 3 strings with a maximum size of 10
Gen.sample 10 3 stringGenerator
// result: [""; "eiX$a^"; "U%0Ika&r"]
```

You can generate random values from a user-defined types as well, like this:

```fsharp {src=#udtGenerator}
type Color = Red | Green of int | Blue of bool

let colorGenerator = Arb.generate<Color>

// generate 10 colors with a maximum size of 50
Gen.sample 50 10 colorGenerator

// result:  [Green -47; Red; Red; Red; Blue true;
//           Green 2; Blue false; Red; Blue true; Green -12]
```

Here's one that generates random values for a user-defined record type which contains another user-defined type.

```fsharp {src=#udtGenerator2}
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

There are ways to have more fine-grained control over how your types are generated, but that will have to wait for another post.

## Understanding FsCheck: Shrinking

One of the cool things about a property-based testing tool like FsCheck is that it will try to create *minimum* counter-examples for properties -- this is called "shrinking".

So how does shrinking work?

There are two parts to the process that FsCheck uses:

First it generates a sequence of random inputs, starting small and getting bigger. This is the "generator" phase as described above.

If any inputs cause the property to fail, it starts "shrinking" the first parameter to find a smaller number.
The exact process for shrinking varies depending on the type (and you can override it too), but let's say that for numbers, they get smaller in a sensible way.

For example, let's say that you have a silly property `isSmallerThan80`:

```fsharp {src=#shrink1}
let isSmallerThan80 x = x < 80
```

You have generated random numbers and found that then property fails for `100`, and you want to try a smaller number. `Arb.shrink` will generate a sequence of ints, all of which are smaller than 100.
Each one of these is tried with the property in turn until the property fails again.

```fsharp {src=#shrink2}
isSmallerThan80 100 // false, so start shrinking

Arb.shrink 100 |> Seq.toList
//  [0; 50; 75; 88; 94; 97; 99]
```

For each element in the list, test the property against it until you find another failure:

```fsharp {src=#shrink3}
isSmallerThan80 0 // true
isSmallerThan80 50 // true
isSmallerThan80 75 // true
isSmallerThan80 88 // false, so shrink again
```

The property failed with `88`, so shrink again using that as a starting point:

```fsharp {src=#shrink4}
Arb.shrink 88 |> Seq.toList
//  [0; 44; 66; 77; 83; 86; 87]
isSmallerThan80 0 // true
isSmallerThan80 44 // true
isSmallerThan80 66 // true
isSmallerThan80 77 // true
isSmallerThan80 83 // false, so shrink again
```

The property failed with `83` now, so shrink again using that as a starting point:

```fsharp {src=#shrink5}
Arb.shrink 83 |> Seq.toList
//  [0; 42; 63; 73; 78; 81; 82]
// smallest failure is 81, so shrink again
```

The property failed with `81`, so shrink again using that as a starting point:

```fsharp {src=#shrink6}
Arb.shrink 81 |> Seq.toList
//  [0; 41; 61; 71; 76; 79; 80]
// smallest failure is 80
```

After this point, shrinking on 80 doesn't work -- no smaller value will be found.

In this case then, FsCheck will report that `80` is the smallest value that falsifies the property and that 4 shrinks were needed.

Just as with generators, FsCheck will generate shrink sequences for almost any type:


```fsharp {src=#shrinkTuple}
Arb.shrink (1,2,3) |> Seq.toList
//  [(0, 2, 3); (1, 0, 3); (1, 1, 3);
//   (1, 2, 0); (1, 2, 2)]

Arb.shrink "abcd" |> Seq.toList
//  ["bcd"; "acd"; "abd"; "abc"; "abca";
//   "abcb"; "abcc"; "abad"; "abbd"; "aacd"]

Arb.shrink [1;2;3] |> Seq.toList
//  [[2; 3]; [1; 3]; [1; 2]; [1; 2; 0]; [1; 2; 2];
//  [1; 0; 3]; [1; 1; 3]; [0; 2; 3]]
```

And, as with generators, there are ways to customize how shrinking works if needed.

## Configuring FsCheck: Changing the number of tests

I mentioned a silly property `isSmallerThan80` above. Let's actually try it out and see how FsCheck does with it.

```fsharp {src=#testCount1}
// silly property to test
let isSmallerThan80 x = x < 80

Check.Quick isSmallerThan80
// result: Ok, passed 100 tests.
```

Oh dear! FsCheck didn't find a counter-example! We know that the property should fail, but we also know that most integers will be generated around zero. Maybe we should tell FsCheck to generate more numbers?

We do this by changing the default ("Quick") configuration. There is a field called `MaxTest` that we can set. The default is 100, so let's increase it to 1000.

To use a specific config, we'll need to use `Check.One(config,property)` rather than just `Check.Quick(property)`.

```fsharp {src=#testCount2}
let config = {
  Config.Quick with
    MaxTest = 1000
  }
Check.One(config,isSmallerThan80 )
// result: Ok, passed 1000 tests.
```

Oops! FsCheck didn't find a counter-example with 1000 tests either! Let's try once more with 10000 tests:

```fsharp {src=#testCount3}
let config = {
  Config.Quick with
    MaxTest = 10000
  }
Check.One(config,isSmallerThan80 )
// result: Falsifiable, after 8660 tests (1 shrink):
//         80
```

Ok, so we finally got it to work. But why did it take so many tests?

The answer lies in some other configuration settings: `StartSize` and `EndSize`.

Remember that the generators start with small numbers and gradually increase them.  This is controlled by the `StartSize` and `EndSize` settings.
By default, `StartSize` is 1 and `EndSize` is 100. So at the end of the test, the "size" parameter to the generator will be 100.

But, as we saw, even if the size is 100, very few numbers are generated at the extremes. In this case it means that numbers greater than 80 are unlikely to be generated.

So let's change the `EndSize` to something larger and see what happens!

```fsharp {src=#testCount4}
let config = {
  Config.Quick with
    EndSize = 1000
  }
Check.One(config,isSmallerThan80 )
// result: Falsifiable, after 21 tests (4 shrinks):
//         80
```

That's more like it! Only 21 tests needed now rather than 8660 tests!

The moral of the story is: understand the domain of your properties and configure the generator appropriately, otherwise you may never even generate inputs that are relevant.

## Configuring FsCheck: Verbose mode and logging

I mentioned that one of the benefits of FsCheck over a home-grown solution is the logging and reproducibility, so let's have a look at that.

Let's say that the EDFH has implemented the  `add` function with a "boundary" of `25`.
Within this limit, `add` will work correctly, but outside it, `add` will have a malicious implementation.

Let's see how FsCheck detects this boundary via logging.

```fsharp {src=#logging1}
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

```text {src=#logging2}
Falsifiable, after 66 tests (12 shrinks):
1
24
25
```

Again, FsCheck has found that the inputs `1`, `24`, and `25` fail. It has discovered that `25` is the exact boundary point quite quickly.  But how did it do it?

First, the simplest way to see what FsCheck is doing is to use "verbose" mode. That is, use `Check.Verbose` rather than `Check.Quick`:

```fsharp {src=#logging3}
// check the property interactively
Check.Quick associativeProperty

// with tracing/logging
Check.Verbose associativeProperty
```

When do this, you'll see an output like that shown below. I've added all the comments to explain the various elements.

```text {src=#logging4}
0:    // test #0
-1    // generated parameter #1 ("x")
-1    // generated parameter #2 ("y")
0     // generated parameter #3 ("z")
//       associativeProperty(-1,-1,0) => true, keep going
1:    // test #1
0
0
0     // associativeProperty 0 0 0  => true, keep going
2:    // test #2
-2
0
-3    // associativeProperty -2 0 -3  => true, keep going
3:    // test #3
1
2
0     // associativeProperty 1 2 0  => true, keep going
// etc
49:   // test #49
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

This is a different run, so the final answer is different than before -- 25,1,26 -- but it still detects the boundary at 25.

This display takes up a lot of space! Can we make it more compact?

Yes -- you can control how each test and shrink is displayed by writing your own custom functions, and telling FsCheck to use them via its `Config` structure.

These functions are generic, and the list of parameters is represented by a list of unknown length (`obj list`).
But since I know I am testing a three parameter property I can hard-code a three-element list parameter and print them all on one line.

```fsharp {src=#logging5}
// create a function for displaying a test
let printTest testNum [x;y;z] =
  sprintf "#%-3i %3O %3O %3O\n" testNum x y z

// create a function for displaying a shrink
let printShrink [x;y;z] =
  sprintf "shrink %3O %3O %3O\n" x y z
```

The configuration also has a slot called `Replay` which is normally `None`, which means that each run will be different. However, if you set `Replay` to `Some seed`, then the test will be replayed exactly the same way.
The seed looks like `StdGen (someInt,someInt)` and is printed on each run, so if you want to preserve a run all you need to do is paste that seed into the config.

And again, to use a specific config, you'll need to use `Check.One(config,property)` rather than just `Check.Quick(property)`.

Here's the code with the default tracing functions changed, and the replay seed set explicitly.


```fsharp {src=#logging5b}
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

```text {src=#logging6}
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

### A real world example of shrinking

In the previous example, we saw a real world example of shrinking.
So, let's look at how that shrinking was done in detail.

The last set of inputs from test #49 (`46,-4,50`) was false, so that triggered shrinking to start.  We start by shrinking the first number `46`.

```fsharp {src=#rwshrink1}
// The last set of inputs (46,-4,50) was false, so shrinking started
associativeProperty 46 -4 50  // false, so shrink

// list of possible shrinks starting at 46
Arb.shrink 46 |> Seq.toList
// result [0; 23; 35; 41; 44; 45]
```

We'll loop through the list `[0; 23; 35; 41; 44; 45]` stopping at the first element that causes the property to fail:

```fsharp {src=#rwshrink2}
// find the next test that fails when shrinking the x parameter
let x,y,z = (46,-4,50)
Arb.shrink x
|> Seq.tryPick (fun x ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (35, -4, 50)
```

The first element in the shrink list that caused a failure was `x=35`, as part of the inputs `(35, -4, 50)`.

So now we start at 35 and shrink that:

```fsharp {src=#rwshrink3}
// find the next test that fails when shrinking the x parameter
let x,y,z = (35,-4,50)
Arb.shrink x
|> Seq.tryPick (fun x ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (27, -4, 50)
```

The first element that caused a failure was now `x=27`, as part of the inputs `(27, -4, 50)`.

So now we start at 27 and keep going:

```fsharp {src=#rwshrink4}
// find the next test that fails when shrinking the x parameter
let x,y,z = (27,-4,50)
Arb.shrink x
|> Seq.tryPick (fun x ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (26, -4, 50)

// find the next test that fails when shrinking the x parameter
let x,y,z = (26,-4,50)
Arb.shrink x
|> Seq.tryPick (fun x ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, -4, 50)

// find the next test that fails when shrinking the x parameter
let x,y,z = (25,-4,50)
Arb.shrink x
|> Seq.tryPick (fun x ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer None
```

At this point, `x=25` is as low as you can go. None of its shrink sequence caused a failure.
So we're finished with the `x` parameter!

Now we just repeat this process with the `y` parameter, in the same way, starting at `-4`.

```fsharp {src=#rwshrink5}
// find the next test that fails when shrinking the y parameter
let x,y,z = (25,-4,50)
Arb.shrink y
|> Seq.tryPick (fun y ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 4, 50)

// find the next test that fails when shrinking the y parameter
let x,y,z = (25,4,50)
Arb.shrink y
|> Seq.tryPick (fun y ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 2, 50)

// find the next test that fails when shrinking the y parameter
let x,y,z = (25,2,50)
Arb.shrink y
|> Seq.tryPick (fun y ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 50)

// find the next test that fails when shrinking the y parameter
let x,y,z = (25,1,50)
Arb.shrink y
|> Seq.tryPick (fun y ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer None
```

At this point, `y=1` is as low as you can go. None of its shrink sequence caused a failure.
So we're finished with the `y` parameter!

Finally, we repeat this process with the `z` parameter.

```fsharp {src=#rwshrink6}
// find the next test that fails when shrinking the z parameter
let x,y,z = (25,1,50)
Arb.shrink z
|> Seq.tryPick (fun z ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 38)

// find the next test that fails when shrinking the z parameter
let x,y,z = (25,1,38)
Arb.shrink z
|> Seq.tryPick (fun z ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 29)

// find the next test that fails when shrinking the z parameter
let x,y,z = (25,1,29)
Arb.shrink z
|> Seq.tryPick (fun z ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer (25, 1, 26)

// find the next test that fails when shrinking the z parameter
let x,y,z = (25,1,26)
Arb.shrink z
|> Seq.tryPick (fun z ->
  if associativeProperty x y z then None else Some (x,y,z) )
// answer None
```

And now we're finished with all the parameters!

The final counter-example after shrinking is `(25,1,26)`.

## Adding pre-conditions

Let's say that we have a new idea for a property to check. We'll create a property called `addition is not multiplication` which will help to stop any malicious (or even accidental) mix-up in the implementations.

Here's our first attempt:

```fsharp {src=#precond1}
let additionIsNotMultiplication x y =
  x + y <> x * y
```

But when we run this test, we get a failure!

```fsharp {src=#precond1check}
Check.Quick additionIsNotMultiplication
// Falsifiable, after 3 tests (0 shrinks):
// 0
// 0
```

Well duh, obviously `0+0` and `0*0` are equal. But how can we tell FsCheck to ignore just those inputs and leave all the other ones alone?

This is done via a "condition" or filter expression that is prepended to the property function using `==>` (an operator defined by FsCheck).

Here's an example:

```fsharp {src=#precond2}
let additionIsNotMultiplication x y =
  x + y <> x * y

let preCondition x y =
  (x,y) <> (0,0)

let additionIsNotMultiplication_withPreCondition x y =
  preCondition x y ==> additionIsNotMultiplication x y
```

The new property is `additionIsNotMultiplication_withPreCondition` and can be passed to `Check.Quick` just like any other property.

```fsharp {src=#precond2check}
Check.Quick additionIsNotMultiplication_withPreCondition
// Falsifiable, after 38 tests (0 shrinks):
// 2
// 2
```

Oops! We forgot another case! `2+2` is the same as `2*2`. Let's fix up our precondition again:

```fsharp {src=#precond3}
let preCondition x y =
  (x,y) <> (0,0)
  && (x,y) <> (2,2)

let additionIsNotMultiplication_withPreCondition x y =
  preCondition x y ==> additionIsNotMultiplication x y
```

And now this works.

```fsharp {src=#precond3check}
Check.Quick additionIsNotMultiplication_withPreCondition
// Ok, passed 100 tests.
```

This kind of precondition should only be used if you want to filter out a small number of cases.

If most of the inputs will be invalid, then this filtering will be expensive. In this case there is a better way to do it, which will be discussed in a future post.

The FsCheck documentation has more on how you can [tweak properties here](https://fscheck.github.io/FsCheck//Properties.html).

## Naming convention for properties

These properties functions have a different purpose from "normal" functions, so how should we name them?

In the Haskell and Erlang world, properties are given a `prop_` prefix by convention. In the .NET world, it is more common to use a suffix like `AbcProperty`.

Also, in F# we have namespaces, modules, and attributes (like `[<Test>]`) that we can use to organize properties and distinguish them from other functions.

## Combining multiple properties

Once you have a set of properties, you can combine them into a group (or even, gasp, a *specification*!), by adding them as static members of a class type.

You can then do `Check.QuickAll` and pass in the name of the class.

For example, here are our three addition properties:

```fsharp {src=#combine1}
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

```fsharp {src=#combine2}
type AdditionSpecification =
  static member ``Commutative`` x y =
    commutativeProperty x y
  static member ``Associative`` x y z =
    associativeProperty x y z
  static member ``Left Identity`` x =
    leftIdentityProperty x
  static member ``Right Identity`` x =
    rightIdentityProperty x

Check.QuickAll<AdditionSpecification>()
```

The result of running `QuickAll<AdditionSpecification>` is:

```fsharp {src=#combine2_check}
--- Checking AdditionSpecification ---
AdditionSpecification.Commutative-Ok, passed 100 tests.
AdditionSpecification.Associative-Ok, passed 100 tests.
AdditionSpecification.Left Identity-Ok, passed 100 tests.
AdditionSpecification.Right Identity-Ok, passed 100 tests.
```

As you can see, all the tests pass. Try changing the implementation of `add` and rerunning the tests!

## Combining property-based tests with example-based tests

In the previous post, we showed that example-based tests had a weakness in that they only tested a very small part of the input space, and could be bypassed by the malicious EDFH, or more typically, by overlooking unusual inputs.

However, I do think that example-based tests have a role that complements property-based tests.

An example-based test is often easier to understand because it is less abstract, and so provides a good entry point and documentation in conjunction with the properties.

Here's an example of mixing properties and example-based tests in the same chunk of code:

```fsharp {src=#combine3}
type AdditionSpecification =

  // some properties
  static member ``Commutative`` x y =
    commutativeProperty x y
  static member ``Associative`` x y z =
    associativeProperty x y z
  static member ``Left Identity`` x =
    leftIdentityProperty x
  static member ``Right Identity`` x =
    rightIdentityProperty x

  // some example-based tests as well
  static member ``1 + 2 = 3``() =
    add 1 2 = 3

  static member ``1 + 2 = 2 + 1``() =
    add 1 2 = add 2 1

  static member ``42 + 0 = 0 + 42``() =
    add 42 0 = add 0 42
```

## Using FsCheck from NUnit

You can use FsCheck from NUnit and other test frameworks, with an extra plugin (e.g. `FsCheck.NUnit` for NUnit).

```fsharp {src=#nugetFsCheckNUnit}
#r "nuget:FsCheck.NUnit"
open FsCheck.NUnit
```

Rather than marking a test with `Test` or `Fact`, you use the `Property` attribute.
And unlike normal tests, these tests can have parameters!

Here's an example of some tests written to work within NUnit:

```fsharp {src=#fscheck_nunit}
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

Until next time -- happy testing!

{{<ghsource "/posts/property-based-testing-1">}}


