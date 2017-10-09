---
layout: post
title: "Monoids without tears"
description: "A mostly mathless discussion of a common functional pattern"
categories: ["Patterns","Math","Folds"]
seriesId: "Understanding monoids"
seriesOrder: 1
---

If you are coming from an OO background, one of the more challenging aspects of learning functional programming is the lack of obvious design patterns.
There are plenty of idioms such as [partial application](/posts/partial-application/), and [error handling techniques](/posts/recipe-part2/), but no apparent patterns in the [GoF sense](http://en.wikipedia.org/wiki/Design_Patterns).

In this post, we'll look at a very common "pattern" known as a *monoid*. Monoids are not really a design pattern; more an approach to working with many different types of values in a common way.
In fact, once you understand monoids, you will start seeing them everywhere!

Unfortunately the term "monoid" itself is a bit off-putting. It originally comes from [mathematics](http://en.wikipedia.org/wiki/Monoid)
but the concept as applied to programming is easy to grasp without any math at all, as I hope to demonstrate.  In fact, if we were to name the concept today in a programming context,
we might call it something like `ICombinable` instead, which is not nearly as scary. 

Finally, you might be wondering if a "monoid" has any connection with a "monad". Yes, there is a mathematical connection between them, but in programming terms, they are very different things, despite having similar names.

## Uh-oh... some equations

On this site, I generally don't use any math, but in this case I'm going to break my self-imposed rule and show you some equations. 

Ready? Here's the first one:

```text
1 + 2 = 3
```

Could you handle that? How about another one?

```text
1 + (2 + 3) = (1 + 2) + 3
```

And finally one more...

```text
1 + 0 = 1 and 0 + 1 = 1
```

Ok! We're done! If you can understand these equations, then you have all the math you need to understand monoids.

## Thinking like a mathematician

> *"A mathematician, like a painter or poet, is a maker of patterns. If his patterns are more permanent than theirs, it is because they are made with ideas" -- G H Hardy*
 
Most people imagine that mathematicians work with numbers, doing complicated arithmetic and calculus. 

This is a misconception. For example, if you look at [typical high-level](http://terrytao.wordpress.com/2013/07/27/an-improved-type-i-estimate/) [math discussions](http://books.google.co.uk/books?id=VOCQUC_uiWgC&pg=PA102),
you will see lots of strange words, and lots of letter and symbols, but not a lot of arithmetic.
 
One of the things that mathematicians *do* do though, is try to find patterns in things. "What do these things have in common?" and "How can we generalize these concepts?" are typical mathematical questions.


So let's look at these three equations through a mathematician's eyes.

### Generalizing the first equation

A mathematician would look at `1 + 2 = 3` and think something like:

* We've got a bunch of things (integers in this case)
* We've got some way of combining two of them (addition in this case)
* And the result is another one of these things (that is, we get another integer)

And then a mathematician might try to see if this pattern could be generalized to other kinds of things and operations. 

Let's start by staying with integers as the "things". What other ways are there of combining integers? And do they fit the pattern?

Let's try multiplication, does that fit this pattern?

The answer is yes, multiplication does fit this pattern because multiplying any two integers results in another integer.

What about division? Does that fit the pattern? The answer is no, because in most cases, dividing two integers results in a fraction, which is *not* an integer (I'm ignoring integer division).

What about the `max` function? Does that fit the pattern? It combines two integers and returns one of them, so the answer is yes.

What about the `equals` function? It combines two integers but returns a boolean, not an integer, so the answer is no.

Enough of integers! What other kinds of things can we think of?

Floats are similar to integers, but unlike integers, using division with floats does result in another float, so the division operation fits the pattern.

How about booleans? They can be combined using operators such as AND, OR and so on. Does `aBool AND aBool` result in another bool? Yes! And `OR` too fits the pattern.

Strings next. How can they be combined? One way is string concatenation, which returns another string, which is what we want. But something like the equality operation doesn't fit, because it returns a boolean.

Finally, let's consider lists. As for strings, the obvious way to combine them is with list concatenation, which returns another list and fits the pattern.

We can continue on like this for all sorts of objects and combining operations, but you should see how it works now.

You might ask: why is it so important that the operation return another thing of the same type? The answer is that **you can chain together multiple objects using the operation**.


For example, because `1 + 2` is another integer, you can add 3 to it. And then because `1 + 2 + 3` is an integer as well, you can keep going and add say, 4, to the result.
In other words, it is only because integer addition fits the pattern that you can write a sequence of additions like this: `1 + 2 + 3 + 4`.  You couldn't write `1 = 2 = 3 = 4` in the same way,
because integer equality doesn't fit the pattern.

And of course, the chain of combined items can be as long as we like. In other words, this pattern allows us to extend a pairwise operation into **an operation that works on lists**.

Mathematicians call the requirement that "the result is another one of these things" the *closure* requirement.

### Generalizing the second equation

Ok, what about the next equation, `1 + (2 + 3) = (1 + 2) + 3`? Why is that important?

Well, if you think about the first pattern, it says we can build up a chain of operations such as `1 + 2 + 3`. But we have only got a pairwise operation. So what order should we do the combining in? Should we combine 1 and 2 first,
then combine the result with 3? Or should we combine 2 and 3 first and then combine 1 with that result? Does it make a difference?

That's where this second equation is useful. It says that, for addition, the order of combination doesn't matter. You get the same result either way. 

So for a chain of four items like this: `1 + 2 + 3 + 4`, we could start working from the left hand side: `((1+2) + 3) + 4` or from the right hand side: `1 + (2 + (3+4))` or even do it in two parts
and then combine them like this: `(1+2) + (3+4)`.

Let's see if this pattern applies to the examples we've already looked at.

Again, let's start with other ways of combining integers. 

We'll start with multiplication again. Does `1 * (2 * 3)` give the same result as `(1 * 2) * 3`? Yes. Just as with addition, the order doesn't matter.

Now let's try subtraction. Does `1 - (2 - 3)` give the same result as `(1 - 2) - 3`?  No. For subtraction, the order *does* matter. 

What about division? Does `12 / (2 / 3)` give the same result as `(12 / 2) / 3`?  No. For division also, the order matters. 

But the `max` function does work. `max( max(12,2), 3)` gives the same result as `max(12, max(2,3)`.

What about strings and lists? Does concatenation meet the requirement? What do you think?

Here's a question... Can we come up with an operation for strings that *is* order dependent? 

Well, how about a function like "subtractChars" which removes all characters in the right string from the left string. So `subtractChars("abc","ab")` is just `"c"`.
`subtractChars` is indeed order dependent, as you can see with a simple example: `subtractChars("abc", subtractChars("abc","abc"))` is not the same string  as `subtractChars(subtractChars("abc","abc"),"abc")`.

Mathematicians call the requirement that "the order doesn't matter" the *associativity* requirement.

**Important Note:**  When I say the "order of combining", I am talking about the order in which you do the pairwise combining steps -- combining one pair, and then combining the result with the next item. 

But it is critical that the overall sequence of the items be left unchanged. This is because for certain operations, if you change the sequencing of the items,
then you get a completely different result! `1 - 2` does not mean the same as `2 - 1` and `2 / 3` does not mean the same as `3 / 2`.

Of course, in many common cases, the sequence order doesn't matter. After all, `1+2` is the same as `2+1`. In this case, the operation is said to be *commutative*.

### The third equation

Now let's look at the third equation, `1 + 0 = 1`.

A mathematician would say something like: that's interesting -- there is a special kind of thing ("zero") that, when you combine it with something,
just gives you back the original something, as if nothing had happened.

So once more, let's revisit our examples and see if we can extend this "zero" concept to other operations and other things.

Again, let's start with multiplication. Is there some value, such that when you multiply a number with it, you get back the original number?

Yes, of course! The number one. So for multiplication, the number `1` is the "zero".

What about `max`? Is there a "zero" for that?  For 32 bit ints, yes. Combining `System.Int32.MinValue` with any other 32 bit integer using `max` will return the other integer.
That fits the definition of "zero" perfectly.

What about booleans combined using AND? Is there a zero for that? Yes. It is the value `True`. Why? Because `True AND False` is `False`, and `True AND True` is `True`. In both cases the other value is returned untouched.

What about booleans combined using OR? Is there a zero for that as well? I'll let you decide.

Moving on, what about string concatenation? Is there a "zero" for this?  Yes, indeed -- it is just the empty string.

```text
"" + "hello" = "hello"
"hello" + "" = "hello"
```

Finally, for list concatenation, the "zero" is just the empty list.  

```text
[] @ [1;2;3] = [1;2;3]
[1;2;3] @ [] = [1;2;3]
```

You can see that the "zero" value depends very much on the operation, not just on the set of things. The zero for integer addition is different from the "zero" for integer multiplication,
which is different again from the from "zero" for `Max`.

Mathematicians call the "zero" the *identity element*.

### The equations revisited

So now let's revisit the equations with our new generalizations in mind.

Before, we had:

```text
1 + 2 = 3
1 + (2 + 3) = (1 + 2) + 3
1 + 0 = 1 and 0 + 1 = 1
```

But now we have something much more abstract, a set of generalized requirements that can apply to all sorts of things:

* You start with a bunch of things, *and* some way of combining them two at a time. 
* **Rule 1 (Closure)**: The result of combining two things is always another one of the things.
* **Rule 2 (Associativity)**: When combining more than two things, which pairwise combination you do first doesn't matter.
* **Rule 3 (Identity element)**: There is a special thing called "zero" such that when you combine any thing with "zero" you get the original thing back.

With these rules in place, we can come back to the definition of a monoid. A "monoid" is just a system that obeys all three rules. Simple!

As I said at the beginning, don't let the mathematical background put you off. If programmers had named this pattern, it probably would been called something like "the combinable pattern" rather than "monoid".
But that's life.  The terminology is already well-established, so we have to use it.

Note there are *two* parts to the definition of a monoid -- the things plus the associated operation.
A monoid is not just "a bunch of things", but "a bunch of things" *and* "some way of combining them".
So, for example, "the integers" is not a monoid, but "the integers under addition" is a monoid.

### Semigroups

In certain cases, you have a system that only follows the first two rules, and there is no candidate for a "zero" value. 

For example, if your domain consists only of strictly positive numbers, then under addition they are closed and associative, but there is no positive number that can be "zero".

Another example might be the intersection of finite lists. It is closed and associative, but there is no (finite) list that when intersected with any other finite list, leaves it untouched.

This kind of system still quite useful, and is called a "semigroup" by mathematicians, rather than a monoid.
Luckily, there is a trick that can convert any semigroup into a monoid (which I'll describe later).


### A table of classifications

Let's put all our examples into a table, so you can see them all together.

<table class="table table-condensed table-striped">
<colgroup>
<col width="5em">
</colgroup>
<tr>
<th>Things</th>
<th>Operation</th>
<th>Closed?</th>
<th>Associative?</th>
<th>Identity?</th>
<th>Classification</th>
</tr>

<tr>
<td>Int32</td>
<td>Addition</td>
<td>Yes</td>
<td>Yes</td>
<td>0</td>
<td>Monoid</td>
</tr>

<tr>
<td>Int32</td>
<td>Multiplication</td>
<td>Yes</td>
<td>Yes</td>
<td>1</td>
<td>Monoid</td>
</tr>

<tr>
<td>Int32</td>
<td>Subtraction</td>
<td>Yes</td>
<td>No</td>
<td>0</td>
<td>Other</td>
</tr>


<tr>
<td>Int32</td>
<td>Max</td>
<td>Yes</td>
<td>Yes</td>
<td>Int32.MinValue</td>
<td>Monoid</td>
</tr>

<tr>
<td>Int32</td>
<td>Equality</td>
<td>No</td>
<td></td>
<td></td>
<td>Other</td>
</tr>


<tr>
<td>Int32</td>
<td>Less than</td>
<td>No</td>
<td></td>
<td></td>
<td>Other</td>
</tr>

<tr>
<td colspan="6"></td>
</tr>

<tr>
<td>Float</td>
<td>Multiplication</td>
<td>Yes</td>
<td>No (See note 1)</td>
<td>1</td>
<td>Other</td>
</tr>

<tr>
<td>Float</td>
<td>Division</td>
<td>Yes (See note 2)</td>
<td>No</td>
<td>1</td>
<td>Other</td>
</tr>

<tr>
<td colspan="6"></td>
</tr>

<tr>
<td>Positive Numbers</td>
<td>Addition</td>
<td>Yes</td>
<td>Yes</td>
<td>No identity</td>
<td>Semigroup</td>
</tr>

<tr>
<td>Positive Numbers</td>
<td>Multiplication</td>
<td>Yes</td>
<td>Yes</td>
<td>1</td>
<td>Monoid</td>
</tr>


<tr>
<td colspan="6"></td>
</tr>

<tr>
<td>Boolean</td>
<td>AND</td>
<td>Yes</td>
<td>Yes</td>
<td>true</td>
<td>Monoid</td>
</tr>

<tr>
<td>Boolean</td>
<td>OR</td>
<td>Yes</td>
<td>Yes</td>
<td>false</td>
<td>Monoid</td>
</tr>


<tr>
<td colspan="6"></td>
</tr>

<tr>
<td>String</td>
<td>Concatenation</td>
<td>Yes</td>
<td>Yes</td>
<td>Empty string ""</td>
<td>Monoid</td>
</tr>

<tr>
<td>String</td>
<td>Equality</td>
<td>No</td>
<td></td>
<td></td>
<td>Other</td>
</tr>

<tr>
<td>String</td>
<td>"subtractChars"</td>
<td>Yes</td>
<td>No</td>
<td>Empty string ""</td>
<td>Other</td>
</tr>

<tr>
<td colspan="6"></td>
</tr>

<tr>
<td>List</td>
<td>Concatenation</td>
<td>Yes</td>
<td>Yes</td>
<td>Empty list []</td>
<td>Monoid</td>
</tr>

<tr>
<td>List</td>
<td>Intersection</td>
<td>Yes</td>
<td>Yes</td>
<td>No identity</td>
<td>Semigroup</td>
</tr>

</table>

There are many other kinds of things you can add to this list; polynomials, matrices, probability distributions, and so on.
This post won't discuss them, but once you get the idea of monoids, you will see that the concept can be applied to all sorts of things.

*[Note 1]* As Doug points out in the comments, [floats are not associative](http://forums.udacity.com/questions/100055360/why-floating-point-arithematic-non-associative).
Replace 'float' with 'real number' to get associativity.

*[Note 2]* Mathematical real numbers are not closed under division, because you cannot divide by zero and get another real number. However, with IEEE floating point numbers you
[<i>can</i> divide by zero](http://stackoverflow.com/questions/14682005/why-does-division-by-zero-in-ieee754-standard-results-in-infinite-value) and get a valid value. So floats are indeed closed under
division! Here's a demonstration:

```fsharp
let x = 1.0/0.0 // infinity
let y = x * 2.0 // two times infinity 
let z = 2.0 / x // two divided by infinity 
```

## What use are monoids to a programmer?

So far, we have described some abstract concepts, but what good are they for real-world programming problems?

### The benefit of closure

As we've seen, the closure rule has the benefit that you can convert pairwise operations into operations that work on lists or sequences. 

In other words, if we can define a pairwise operation, we can extend it to list operations "for free".

The function that does this is typically called "reduce". Here are some examples:

<table class="table table-condensed table-striped">

<tr>
<th>Explicit</th>
<th>Using reduce</th>
</tr>

<tr>
<td><code>1 + 2 + 3 + 4</code></td>
<td><code>[ 1; 2; 3; 4 ] |> List.reduce (+)</code></td>
</tr>

<tr>
<td><code>1 * 2 * 3 * 4</code></td>
<td><code>[ 1; 2; 3; 4 ] |> List.reduce (*)</code></td>
</tr>

<tr>
<td><code>"a" + "b" + "c" + "d"</code></td>
<td><code>[ "a"; "b"; "c"; "d" ] |> List.reduce (+)</code></td>
</tr>

<tr>
<td><code>[1] @ [2] @ [3] @ [4]</code></td>
<td><code>[ [1]; [2]; [3]; [4] ] |> List.reduce (@)</code></td>
</tr>

</table>

You can see that `reduce` can be thought of as inserting the specified operation between each element of the list.

Note that in the last example, the input to `reduce` is a list of lists, and the output is a single list. Make sure you understand why this is.

### The benefit of associativity

If the pairwise combinations can be done in any order, that opens up some interesting implementation techniques, such as:

* Divide and conquer algorithms
* Parallelization
* Incrementalism

These are deep topics, but let's have a quick look!

**Divide and conquer algorithms**

Consider the task of summing the first 8 integers; how could we implement this?

One way would be a crude step-by-step sum, as follows:

```fsharp
let sumUpTo2 = 1 + 2
let sumUpTo3 = sumUpTo2 + 3
let sumUpTo4 = sumUpTo3 + 4
// etc
let result = sumUpTo7 + 8
```

But because the sums can be done in any order, we could also implement the requirement by splitting the sum into two halves, like this

```fsharp
let sum1To4 = 1 + 2 + 3 + 4
let sum5To8 = 5 + 6 + 7 + 8
let result = sum1To4 + sum5To8
```

and then we can recursively split the sums into sub-sums in the same way until we get down to the basic pairwise operation:

```fsharp
let sum1To2 = 1 + 2 
let sum3To4 = 3 + 4
let sum1To4 = sum1To2 + sum3To4
```

This "divide and conquer" approach may seem like overkill for something like a simple sum, but we'll see in a future post that, in conjunction with a `map`, it is the basis for some well known aggregation algorithms.

**Parallelization**

Once we have a divide and conquer strategy, it can be easily converted into a parallel algorithm. 

For example, to sum the first 8 integers on a four-core CPU, we might do something like this:

<table class="table table-condensed table-striped">

<tr>
<th></th>
<th>Core 1</th>
<th>Core 2</th>
<th>Core 3</th>
<th>Core 4</th>
</tr>

<tr>
<td>Step 1</td>
<td><code>sum12 = 1 + 2</code></td>
<td><code>sum34 = 3 + 4</code></td>
<td><code>sum56 = 5 + 6</code></td>
<td><code>sum78 = 7 + 8</code></td>
</tr>

<tr>
<td>Step 2</td>
<td><code>sum1234 = sum12 + sum34</code></td>
<td><code>sum5678 = sum56 + sum78</code></td>
<td>(idle)</td>
<td>(idle)</td>
</tr>

<tr>
<td>Step 3</td>
<td><code>sum1234 + sum5678</code></td>
<td>(idle)</td>
<td>(idle)</td>
<td>(idle)</td>
</tr>

</table>

There are still seven calculations that need to be done, but because we are doing it parallel, we can do them all in three steps.

Again, this might seem like a trivial example, but big data systems such as Hadoop are all about aggregating large amounts of data,
and if the aggregation operation is a monoid, then you can, in theory, easily scale these aggregations by using multiple machines*.

*<sub>In practice, of course, the devil is in the details, and real-world systems don't work exactly this way.</sub>


**Incrementalism**

Even if you do not need parallelism, a nice property of monoids is that they support incremental calculations.

For example, let's say you have asked me to calculate the sum of one to five. Then of course I give you back the answer fifteen. 

But now you say that you have changed your mind, and you want the sum of one to *six* instead. Do I have to add up all the numbers again, starting from scratch?
No, I can use the previous sum, and just add six to it incrementally.  This is possible because integer addition is a monoid.

That is, when faced with a sum like `1 + 2 + 3 + 4 + 5 + 6`, I can group the numbers any way I like.
In particular, I can make an incremental sum like this: `(1 + 2 + 3 + 4 + 5) + 6`, which then reduces to `15 + 6`.

In this case, recalculating the entire sum from scratch might not be a big deal, but consider a real-world example like web analytics, counting the number of visitors over the last 30 days, say.
A naive implementation might be to calculate the numbers by parsing the logs of the last 30 days data.  A more efficient approach would be to recognize that the previous 29 days have not changed,
and to only process the incremental changes for one day. As a result, the parsing effort is greatly reduced.

Similarly, if you had a word count of a 100 page book, and you added another page, you shouldn't need to parse all 101 pages again. You just need to count the words on the last page and add that to the previous total.*

* <sub>Technically, these are scary sounding *monoid homomorphisms*. I will explain what this is in the next post.</sub>


### The benefit of identity

Having an identity element is not always required. Having a closed, associative operation (i.e. a semigroup) is sufficient to do many useful things.

But in some cases, it is not enough.  For example, here are some cases that might crop up:

* How can I use `reduce` on an empty list? 
* If I am designing a divide and conquer algorithm, what should I do if one of the "divide" steps has nothing in it?
* When using an incremental algorithm, what value should I start with when I have no data?

In all cases we need a "zero" value.  This allows us to say, for example, that the sum of an empty list is `0`. 

Regarding the first point above, if we are concerned that the list might be empty, then we must replace `reduce` with `fold`, which allows an initial value to be passed in.
(Of course, `fold` can be used for more things than just monoid operations.)

Here are `reduce` and `fold` in action:

```fsharp
// ok
[1..10] |> List.reduce (+)

// error
[] |> List.reduce (+)  

// ok with explicit zero
[1..10] |> List.fold (+) 0 

// ok with explicit zero
[] |> List.fold (+) 0 
```

Using a "zero" can result in counter-intuitive results sometimes. For example, what is the *product* of an empty list of integers? 

The answer is `1`, not `0` as you might expect!  Here's the code to prove it:

```fsharp
[1..4] |> List.fold (*) 1  // result is 24
[] |> List.fold (*) 1      // result is 1
```

### Summary of the benefits

To sum up, a monoid is basically a way to describe an aggregation pattern -- we have a list of things, we have some way of combining them, and we get a single aggregated object back at the end.

Or in F# terms:

```text
Monoid Aggregation : 'T list -> 'T
```

So when you are designing code, and you start using terms like "sum", "product", "composition", or "concatenation", these are clues that you are dealing with a monoid.

## Next steps

Now that we understand what a monoid is, let's see how they can be used in practice.

In the next post in this series, we'll look at how you might write real code that implements the monoid "pattern".
