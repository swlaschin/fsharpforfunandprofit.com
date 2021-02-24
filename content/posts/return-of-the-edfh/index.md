---
layout: post
title: "The Return of the Enterprise Developer From Hell"
description: "More malicious compliance, more property-based testing"
date: 2021-02-14
categories: [Testing]
seriesId: "The Return of the EDFH"
seriesOrder: 1
---

In a [previous series of posts](/pbt), I introduced you to the burned-out and lazy programmer known as the *Enterprise Developer From Hell*, or the *EDFH* for short. As we saw, the EDFH loves to practice [malicious compliance](https://www.reddit.com/r/MaliciousCompliance/top/?sort=top&t=all).

Recently, the EDFH's influence was apparent with [this viral answer to an interview problem](https://twitter.com/allenholub/status/1357115515672555520).

{{<alertwell>}}
Write a function that converts the input to the output.

* Input: "aaaabbbcca"
* Output: [('a',4), ('b',3), ('c',2), ('a',1)]
{{</alertwell>}}

Of course the EDFH answer is simple:

```fsharp {src=#efdh1}
let func inputStr =
  // hard code the answer
  [('a',4); ('b',3); ('c',2); ('a',1)]
```

Since this is the only specification we are given, this is a perfectly fine implementation!

It's funny though, because the interviewer was obviously asking for something a bit more complex.

But this raises two very important questions: what exactly was the interviewer asking for? And how would they know if they got it?


With only one input/output pair given, there are lots of potential specs that could work. However, the consensus of twitter was that this was meant to be a run-length encoding (RLE) problem. 😀

So now we have two specific challenges:

* What *should* the specification for RLE be? How can we define it in an unambiguous way?
* How can we check that a particular RLE implementation meets that spec?

So what does a spec for RLE actually look like? Interestingly, when I searched the internet, I didn't find much. Wikipedia has a [RLE page](https://en.wikipedia.org/wiki/Run-length_encoding) with an example but no spec. Rosetta Stone has [a RLE page](https://rosettacode.org/wiki/Run-length_encoding) with an informal spec.

## Testing in the presence of the EDFH

Let's put the spec on hold for a minute and turn our attention to testing. How can we check that an RLE implementation works?

One approach would be to do example-based testing:

* expect that the output of `rle("")` is `[]`
* expect that the output of `rle("a")` is `[(a,1)]`
* expect that the output of `rle("aab")` is `[(a,2); (b,1)]`

and so on.

But, if we recall our previous experience with the EDFH, they will surely find an implementation that passes all the tests, but is still wrong. For example, the EDFH's implementation for the examples above could look like this:

```fsharp {src=#efdh2}
let rle inputStr =
  match inputStr with
  | "" ->
    []
  | "a" ->
    [('a',1)]
  | "aab" ->
    [('a',2); ('b',1)]
  | "aaaabbbcca" ->
    [('a',4); ('b',3); ('c',2); ('a',1)]
  // everything else
  | _ -> []
```

And if we check this implementation, it looks pretty good!

```fsharp {src=#efdh2_test}
rle "a"           //=> [('a',1);]
rle "aab"         //=> [('a',2); ('b',1)]
rle "aaaabbbcca"  //=> [('a',4); ('b',3); ('c',2); ('a',1)]
```

The best way to beat the EFDH is to use random inputs, and in particular, property-based testing.

A nice thing about property-based testing is that, by doing it, you can often discover the specification. In [a previous post](/posts/property-based-testing), I discussed how we might test an implementation of addition. Eventually, we discovered the properties of commutativity, associativity, and identity. These not only defined the tests we needed, they also pretty much define what "addition" actually is.

Let's see if we can do the same for RLE.

### Using EDFH implementations to help us think of properties

Remember that in property based testing, we're not allowed to reimplement the logic, but instead we have to come up with general properties that work for all inputs.

But this is the hard part -- thinking of properties. However, we can use the EDFH to guide us! For each implementation that the EDFH creates, we figure out why it's wrong, and then create a property to capture that requirement.

For example, the EDFH might implement the RLE function as an empty list, no matter what the input was:

```fsharp {src=#rle_empty}
let rle_empty (inputStr:string) : (char*int) list =
  []
```

Why would that be wrong? Because the output must have some connection to the input. It should contain every character from the input, in fact.

Ok then, the EDFH will retaliate by returning each character with a count of one.

```fsharp {src=#rle_allChars}
let rle_allChars inputStr =
  inputStr
  |> Seq.toList
  |> List.map (fun ch -> (ch,1))
```

If we run this, we get

```fsharp {src=#rle_allChars_test}
rle_allChars ""      //=> []
rle_allChars "a"     //=> [('a',1)]
rle_allChars "abc"   //=> [('a',1); ('b',1); ('c',1)]
rle_allChars "aab"   //=> [('a',1); ('a',1); ('b',1)]
```

These output do indeed contain every character from the input.

Why is that wrong? Well, we want to collect "runs", which means that we should not have two 'a's together. Each character in the output list must be different from the adjacent characters.

That's an easy fix for the EFDH, just add `distinct` into the pipeline!

```fsharp {src=#rle_distinct}
let rle_distinct inputStr =
  inputStr
  |> Seq.distinct // added
  |> Seq.toList
  |> List.map (fun ch -> (ch,1))
```

And now the output satisfies the "runs" property -- the duplicates have disappeared.

```fsharp {src=#rle_distinct_test}
rle_distinct "a"     //=> [('a',1)]
rle_distinct "aab"   //=> [('a',1); ('b',1))]
rle_distinct "aaabb" //=> [('a',1); ('b',1))]
```

Why is this wrong? Well what about those counts? They're all just 1. What should they be?

Without reimplementing the algorithm, we don't know what the individual counts should be, but we *do* know what they should add up to: the number of characters in the string. If there are 5 characters in the source string, the sum of the run lengths should be 5 as well.

Unfortunately, the EDFH has an answer for this as well. Their implementation can just use `groupBy` or `countBy` to get the counts.

```fsharp {src=#rle_groupedCount}
let rle_groupedCount inputStr =
  inputStr
  |> Seq.countBy id
  |> Seq.toList
```

The output looks good at first glance

```fsharp {src=#rle_groupedCount_test}
rle_groupedCount "aab"         //=> [('a',2); ('b',1))]
rle_groupedCount "aaabb"       //=> [('a',3); ('b',3))]
rle_groupedCount "aaaabbbcca"  //=> [('a',5); ('b',3); ('c',2))]
```

But there's a subtle problem. In the third example, there are two distinct runs of `'a'` but the `rle_groupedCount` implementation merges them together.

What we wanted:

```fsharp {src=#rle_groupedCount_1}
[('a',4); ('b',3); ('c',2); ('a',1)]
```

What we got:

```fsharp {src=#rle_groupedCount_2}
[('a',5); ('b',3); ('c',2)]
//    ^ wrong number      ^ another entry needed here
```

The problem with the `groupedCount` approach is that it doesn't take the *order* of the characters into account. What property could we come up with that would catch that?

The simplest way to check for ordering is just to reverse something! In this case we could have a property: "A reversed *input* should give a reversed *output*". The `rle_groupedCount` implementation would fail this -- just what we want.

So, already, with just a few minutes thinking (and some help from the EDFH) we have some properties that can be used to check an RLE implementation:

* The output must contain all the characters from the input
* No two adjacent characters in the output can be the same
* The sum of the run lengths in the output must equal the total length of the input
* If the input is reversed, the output must also be reversed

{{<alertinfo>}}
Is that enough to correctly check a RLE implementation? Can you think of any malicious EDFH implementations that would satisfy these properties and yet be wrong? We'll revisit this in a [later post](/posts/return-of-the-edfh-3).
{{</alertinfo>}}

## Property checking in practice

Let's put these concepts into practice. We'll use `FsCheck`, the F# library, to test these properties against both bad and good implementations.

As of F# 5, it's really easy to load FsCheck into the interactive workspace. You can just reference it directly like this:

```fsharp {src=#nugetFsCheck}
#r "nuget:FsCheck"
```

*NOTE: For the code used in these examples, see the link at the bottom of this post*

Now we can write our first property: "the result must contain all the characters from the input"

```fsharp {src=#propUsesAllCharacters}
// An RLE implementation has this signature
type RleImpl = string -> (char*int) list

let propUsesAllCharacters (impl:RleImpl) inputStr =
  let output = impl inputStr
  let expected =
    inputStr
    |> Seq.distinct
    |> Seq.toList
  let actual =
    output
    |> Seq.map fst
    |> Seq.distinct
    |> Seq.toList
  expected = actual
```

Normally the only parameters for a property would be the ones under test, but in this case we
will also pass in an implementation parameter so we can test with the EDFH implementations, as well
as our (hopefully) correct ones

### Checking the rle_empty implementation

Let's try it out with the first EDFH implementation, that always returned an empty list

```fsharp {src=#rle_empty_proptest}
let impl = rle_empty
let prop = propUsesAllCharacters impl
FsCheck.Check.Quick prop
```

The response from FsCheck is

```text {src=#rle_empty_proptest_result}
Falsifiable, after 1 test (1 shrink) (StdGen (777291017, 296855223)):
Original:
"#"
Shrunk:
"a"
```

In other words, the using the string "a" as input will break the property.

### Checking the rle_allChars implementation

If we try with the `rle_allChars` implementation...

```fsharp {src=#rle_allChars_proptest}
let impl = rle_allChars
let prop = propUsesAllCharacters impl
FsCheck.Check.Quick prop
```

...we immediately get a `ArgumentNullException` because we completely forgot to handle null inputs in the implementation! Thank you, property based testing!

Let's fix up the implementation to handle nulls...

```fsharp {src=#rle_allChars_fixed}
let rle_allChars inputStr =
  // add null check
  if System.String.IsNullOrEmpty inputStr then
    []
  else
    inputStr
    |> Seq.toList
    |> List.map (fun ch -> (ch,1))
```

... and then try again -- oops -- we get another null issue, this time in our property. Let's fix that up too.

```fsharp {src=#propUsesAllCharacters_fixed}
let propUsesAllCharacters (impl:RleImpl) inputStr =
  let output = impl inputStr
  let expected =
    if System.String.IsNullOrEmpty inputStr then
      []
    else
      inputStr
      |> Seq.distinct
      |> Seq.toList
  let actual =
    output
    |> Seq.map fst
    |> Seq.distinct
    |> Seq.toList
  expected = actual
```

Now if we try one more time, the property passes.

```
Ok, passed 100 tests.
```

So the incorrect `rle_allChars` implementation does pass, as we expected. Which is why we need the next property: "adjacent characters in the output cannot be the same"

## The "adjacent characters are not the same" property

In order to define this property, we will first define a helper function `removeDupAdjacentChars` that strips duplicates.

```fsharp {src=#removeDupAdjacentChars}
/// Given a list of elements, remove elements that have the
/// same char as the preceding element.
/// Example:
///   removeDupAdjacentChars ['a';'a';'b';'b';'a'] => ['a'; 'b'; 'a']
let removeDupAdjacentChars charList =
  let folder stack element =
    match stack with
    | [] ->
      // First time? Create the stack
      [element]
    | top::_ ->
      // New element? add it to the stack
      if top <> element then
        element::stack
      // else leave stack alone
      else
        stack

  // Loop over the input, generating a list of non-dup items.
  // These are in reverse order. so reverse the result
  charList |> List.fold folder [] |> List.rev
```

With this in hand, our property will get the characters from the output and then remove duplicates. If the implementation is correct, removing duplicates should not have any effect.

```fsharp {src=#propAdjacentCharactersAreNotSame}
/// Property: "Adjacent characters in the output cannot be the same"
let propAdjacentCharactersAreNotSame (impl:RleImpl) inputStr =
  let output = impl inputStr
  let actual =
    output
    |> Seq.map fst
    |> Seq.toList
  let expected =
    actual
    |> removeDupAdjacentChars // should have no effect
  expected = actual // should be the same
```

Now lets check this new property against the EDFH's `rle_allChars` implementation:

```fsharp {src=#propAdjacentCharactersAreNotSame_rle_allChars}
let impl = rle_allChars
let prop = propAdjacentCharactersAreNotSame impl
FsCheck.Check.Quick prop
```

And...

```
Ok, passed 100 tests.
```

That was unexpected. Maybe we were just unlucky? Let's change the default configuration to be 10000 runs rather than 100.

```fsharp {src=#propAdjacentCharactersAreNotSame_rle_allChars_10000}
let impl = rle_allChars
let prop = propAdjacentCharactersAreNotSame impl
let config = {FsCheck.Config.Default with MaxTest = 10000}
FsCheck.Check.One(config,prop)
```

And...

```
Ok, passed 10000 tests.
```

...it still passes. That's not good.


Hmmm. Let's add a quick `printf` to print the strings being generated by FsCheck, so we can see what is going on.

```fsharp {src=#propAdjacentCharactersAreNotSame_debug}
let propAdjacentCharactersAreNotSame (impl:RleImpl) inputStr =
  let output = impl inputStr
  printfn "%s" inputStr
  // etc
```

Here's what the input strings being generated by FsCheck look like:

```text {src=#none}
v$D
%q6,NDUwm9~ 8I?a-ruc(@6Gi_+pT;1SdZ|H
E`Vxc(1daN
t/vLH$".5m8RjMrlCUb1J1'
Y[Q?zh^#ELn:0u
```

We can see that the strings are very random, and almost never have a series of repeated characters. From the point of view of testing an RLE algorithm these inputs are completely useless!

{{<alertinfo>}}
The moral of this story is that, just as for regular TDD, make sure you start with failing tests. Only then can you be sure that your correct implementation is passing for the right reasons.
{{</alertinfo>}}

So what we need to do now is generate *interesting* inputs rather than random strings.

How can we do that? And then how can we monitor what the inputs are without resorting to crude `print` debugging?

That will be the topic of the [next installment!](/posts/return-of-the-edfh-2)

{{<ghsource "/posts/return-of-the-edfh">}}
