---
layout: post
title: "Using functions to extract boilerplate code"
description: "The functional approach to the DRY principle"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 10
categories: [Conciseness, Functions, Folds]
---

In the very first example in this series, we saw a simple function that calculated the sum of squares, implemented in both F# and C#. 
Now let's say we want some new functions which are similar, such as:

* Calculating the product of all the numbers up to N
* Counting the sum of odd numbers up to N
* The alternating sum of the numbers up to N

Obviously, all these requirements are similar, but how would you extract any common functionality?

Let's start with some straightforward implementations in C# first:

```csharp
public static int Product(int n)
{
    int product = 1;
    for (int i = 1; i <= n; i++)
    {
        product *= i;
    }
    return product;
}

public static int SumOfOdds(int n)
{
    int sum = 0;
    for (int i = 1; i <= n; i++)
    {
        if (i % 2 != 0) { sum += i; }
    }
    return sum;
}

public static int AlternatingSum(int n)
{
    int sum = 0;
    bool isNeg = true;
    for (int i = 1; i <= n; i++)
    {
        if (isNeg)
        {
            sum -= i;
            isNeg = false;
        }
        else
        {
            sum += i;
            isNeg = true;
        }
    }
    return sum;
}
```

What do all these implementations have in common?  The looping logic!  As programmers, we are told to remember the DRY principle ("don't repeat yourself"), yet here we have repeated almost exactly the same loop logic each time. Let's see if we can extract just the differences between these three methods:

<table class="table">
<thead>
  <tr>
	<th>Function</th>
	<th>Initial value</th>
	<th>Inner loop logic</th>
  </tr>
</thead>
<tbody>
  <tr>
	<td>Product</td>
	<td>product=1</td>
	<td>Multiply the i'th value with the running total</td>
  </tr>
  <tr>
	<td>SumOfOdds</td>
	<td>sum=0</td>
	<td>Add the i'th value to the running total if not even</td>
  </tr>
  <tr>
	<td>AlternatingSum</td>
	<td>int sum = 0<br>bool isNeg = true</td>
	<td>Use the isNeg flag to decide whether to add or subtract, and flip the flag for the next pass.</td>
  </tr>
</tbody>
</table>

Is there a way to strip the duplicate code and focus on the just the setup and inner loop logic?  Yes there is. Here are the same three functions in F#:

```fsharp
let product n = 
    let initialValue = 1
    let action productSoFar x = productSoFar * x
    [1..n] |> List.fold action initialValue

//test
product 10

let sumOfOdds n = 
    let initialValue = 0
    let action sumSoFar x = if x%2=0 then sumSoFar else sumSoFar+x 
    [1..n] |> List.fold action initialValue

//test
sumOfOdds 10

let alternatingSum n = 
    let initialValue = (true,0)
    let action (isNeg,sumSoFar) x = if isNeg then (false,sumSoFar-x)
                                             else (true ,sumSoFar+x)
    [1..n] |> List.fold action initialValue |> snd

//test
alternatingSum 100
```

All three of these functions have the same pattern:

1. Set up the initial value
2. Set up an action function that will be performed on each element inside the loop. 
3. Call the library function `List.fold`. This is a powerful, general purpose function which starts with the initial value and then runs the action function for each element in the list in turn.

The action function always has two parameters: a running total (or state) and the list element to act on (called "x" in the above examples).

In the last function, `alternatingSum`, you will notice that it used a tuple (pair of values) for the initial value and the result of the action.  This is because both the running total and the `isNeg` flag must be passed to the next iteration of the loop -- there are no "global" values that can be used.  The final result of the fold is also a tuple so we have to use the "snd" (second) function to extract the final total that we want.

By using `List.fold` and avoiding any loop logic at all, the F# code gains a number of benefits:

* **The key program logic is emphasized and made explicit**. The important differences between the functions become very clear, while the commonalities are pushed to the background.
* **The boilerplate loop code has been eliminated**, and as a result the code is more condensed than the C# version (4-5 lines of F# code vs. at least 9 lines of C# code)
* **There can never be a error in the loop logic** (such as off-by-one) because that logic is not exposed to us.

By the way, the sum of squares example could also be written using `fold` as well:

```fsharp
let sumOfSquaresWithFold n = 
    let initialValue = 0
    let action sumSoFar x = sumSoFar + (x*x)
    [1..n] |> List.fold action initialValue 

//test
sumOfSquaresWithFold 100
```

## "Fold" in C# ##

Can you use the "fold" approach in C#? Yes. LINQ does have an equivalent to `fold`, called `Aggregate`. And here is the C# code rewritten to use it:

```csharp
public static int ProductWithAggregate(int n)
{
    var initialValue = 1;
    Func<int, int, int> action = (productSoFar, x) => 
        productSoFar * x;
    return Enumerable.Range(1, n)
            .Aggregate(initialValue, action);
}

public static int SumOfOddsWithAggregate(int n)
{
    var initialValue = 0;
    Func<int, int, int> action = (sumSoFar, x) =>
        (x % 2 == 0) ? sumSoFar : sumSoFar + x;
    return Enumerable.Range(1, n)
        .Aggregate(initialValue, action);
}

public static int AlternatingSumsWithAggregate(int n)
{
    var initialValue = Tuple.Create(true, 0);
    Func<Tuple<bool, int>, int, Tuple<bool, int>> action =
        (t, x) => t.Item1
            ? Tuple.Create(false, t.Item2 - x)
            : Tuple.Create(true, t.Item2 + x);
    return Enumerable.Range(1, n)
        .Aggregate(initialValue, action)
        .Item2;
}
```

Well, in some sense these implementations are simpler and safer than the original C# versions, but all the extra noise from the generic types makes this approach much less elegant than the equivalent code in F#.  You can see why most C# programmers prefer to stick with explicit loops.

## A more relevant example ##

A slightly more relevant example that crops up frequently in the real world is how to get the "maximum" element of a list when the elements are classes or structs.
The LINQ method 'max' only returns the maximum value, not the whole element that contains the maximum value.

Here's a solution using an explicit loop:

```csharp
public class NameAndSize
{
    public string Name;
    public int Size;
}

public static NameAndSize MaxNameAndSize(IList<NameAndSize> list)
{
    if (list.Count() == 0)
    {
        return default(NameAndSize);
    }

    var maxSoFar = list[0];
    foreach (var item in list)
    {
        if (item.Size > maxSoFar.Size)
        {
            maxSoFar = item;
        }
    }
    return maxSoFar;
}

```

Doing this in LINQ seems hard to do efficiently (that is, in one pass), and has come up as a [Stack Overflow question](http://stackoverflow.com/questions/1101841/linq-how-to-perform-max-on-a-property-of-all-objects-in-a-collection-and-ret). Jon Skeet even wrote an [article about it](http://codeblog.jonskeet.uk/2005/10/02/a-short-case-study-in-linq-efficiency/).

Again, fold to the rescue!

And here's the C# code using `Aggregate`:

```csharp
public class NameAndSize
{
    public string Name;
    public int Size;
}

public static NameAndSize MaxNameAndSize(IList<NameAndSize> list)
{
    if (!list.Any())
    {
        return default(NameAndSize);
    }

    var initialValue = list[0];
    Func<NameAndSize, NameAndSize, NameAndSize> action =
        (maxSoFar, x) => x.Size > maxSoFar.Size ? x : maxSoFar;
    return list.Aggregate(initialValue, action);
}
``` 

Note that this C# version returns null for an empty list.  That seems dangerous -- so what should happen instead? Throwing an exception? That doesn't seem right either.

Here's the F# code using fold:

```fsharp
type NameAndSize= {Name:string;Size:int}
 
let maxNameAndSize list = 
    
    let innerMaxNameAndSize initialValue rest = 
        let action maxSoFar x = if maxSoFar.Size < x.Size then x else maxSoFar
        rest |> List.fold action initialValue 

    // handle empty lists
    match list with
    | [] -> 
        None
    | first::rest -> 
        let max = innerMaxNameAndSize first rest
        Some max
``` 

The F# code has two parts:

* the `innerMaxNameAndSize` function is similar to what we have seen before.
* the second bit, `match list with`, branches on whether the list is empty or not.
With an empty list, it returns  a `None`, and in the non-empty case, it returns a `Some`.
Doing this guarantees that the caller of the function has to handle both cases.

And a test:

```fsharp
//test
let list = [
    {Name="Alice"; Size=10}
    {Name="Bob"; Size=1}
    {Name="Carol"; Size=12}
    {Name="David"; Size=5}
    ]    
maxNameAndSize list
maxNameAndSize []
``` 

Actually, I didn't need to write this at all, because F# already has a `maxBy` function!

```fsharp
// use the built in function
list |> List.maxBy (fun item -> item.Size)
[] |> List.maxBy (fun item -> item.Size)
``` 

But as you can see, it doesn't handle empty lists well. Here's a version that wraps the `maxBy` safely.

```fsharp
let maxNameAndSize list = 
    match list with
    | [] -> 
        None
    | _ -> 
        let max = list |> List.maxBy (fun item -> item.Size)
        Some max
``` 
