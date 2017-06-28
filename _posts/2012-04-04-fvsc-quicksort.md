---
layout: post
title: "Comparing F# with C#: Sorting"
description: "In which we see that F# is more declarative than C#, and we are introduced to pattern matching."
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 4
categories: [F# vs C#]
---

In this next example, we will implement a quicksort-like algorithm for sorting lists and compare an F# implementation to a C# implementation. 

Here is the logic for a simplified quicksort-like algorithm:

<pre>
If the list is empty, there is nothing to do.
Otherwise: 
  1. Take the first element of the list
  2. Find all elements in the rest of the list that 
      are less than the first element, and sort them. 
  3. Find all elements in the rest of the list that 
      are >= than the first element, and sort them
  4. Combine the three parts together to get the final result: 
      (sorted smaller elements + firstElement + 
       sorted larger elements)
</pre>	   

Note that this is a simplified algorithm and is not optimized (and it does not sort in place, like a true quicksort); we want to focus on clarity for now.

Here is the code in F#:

```fsharp
let rec quicksort list =
   match list with
   | [] ->                            // If the list is empty
        []                            // return an empty list
   | firstElem::otherElements ->      // If the list is not empty     
        let smallerElements =         // extract the smaller ones    
            otherElements             
            |> List.filter (fun e -> e < firstElem) 
            |> quicksort              // and sort them
        let largerElements =          // extract the large ones
            otherElements 
            |> List.filter (fun e -> e >= firstElem)
            |> quicksort              // and sort them
        // Combine the 3 parts into a new list and return it
        List.concat [smallerElements; [firstElem]; largerElements]

//test
printfn "%A" (quicksort [1;5;23;18;9;1;3])
```

Again note that this is not an optimized implementation, but is designed to mirror the algorithm closely.

Let's go through this code:

* There are no type declarations anywhere. This function will work on any list that has comparable items (which is almost all F# types, because they automatically have a default comparison function).
* The whole function is recursive -- this is signaled to the compiler using the `rec` keyword in "`let rec quicksort list =`".
* The `match..with` is sort of like a switch/case statement. Each branch to test is signaled with a vertical bar, like so:

```fsharp
match x with
| caseA -> something
| caseB -> somethingElse
```
* The "`match`" with `[]` matches an empty list, and returns an empty list. 
* The "`match`" with `firstElem::otherElements` does two things. 
  * First, it only matches a non-empty list. 
  * Second, it creates two new values automatically. One for the first element called "`firstElem`", and one for the rest of the list, called "`otherElements`".
    In C# terms, this is like having a "switch" statement that not only branches, but does variable declaration and assignment *at the same time*.
* The `->` is sort of like a lambda (`=>`) in C#. The equivalent C# lambda would look something like `(firstElem, otherElements) => do something`.
* The "`smallerElements`" section takes the rest of the list, filters it against the first element using an inline lambda expression with the "`<`" operator and then pipes the result into the quicksort function recursively.
* The "`largerElements`" line does the same thing, except using the "`>=`" operator 
* Finally the resulting list is constructed using the list concatenation function "`List.concat`". For this to work, the first element needs to be put into a list, which is what the square brackets are for. 
* Again note there is no "return" keyword; the last value will be returned. In the "`[]`" branch the return value is the empty list, and in the main branch, it is the newly constructed list.

For comparison here is an old-style C# implementation (without using LINQ).

```csharp
public class QuickSortHelper
{
   public static List<T> QuickSort<T>(List<T> values) 
      where T : IComparable
   {
      if (values.Count == 0)
      {
         return new List<T>();
      }

      //get the first element
      T firstElement = values[0];

      //get the smaller and larger elements
      var smallerElements = new List<T>();
      var largerElements = new List<T>();
      for (int i = 1; i < values.Count; i++)  // i starts at 1
      {                                       // not 0!
         var elem = values[i];
         if (elem.CompareTo(firstElement) < 0)
         {
            smallerElements.Add(elem);
         }
         else
         {
            largerElements.Add(elem);
         }
      }

      //return the result
      var result = new List<T>();
      result.AddRange(QuickSort(smallerElements.ToList()));
      result.Add(firstElement);
      result.AddRange(QuickSort(largerElements.ToList()));
      return result;
   }
}
```

Comparing the two sets of code, again we can see that the F# code is much more compact, with less noise and no need for type declarations. 

Furthermore, the F# code reads almost exactly like the actual algorithm, unlike the C# code.  This is another key advantage of F# -- the code is generally more declarative ("what to do") and less imperative ("how to do it") than C#, and is therefore much more self-documenting. 

 
## A functional implementation in C# ##

Here's a more modern "functional-style" implementation using LINQ and an extension method:

```csharp
public static class QuickSortExtension
{
    /// <summary>
    /// Implement as an extension method for IEnumerable
    /// </summary>
    public static IEnumerable<T> QuickSort<T>(
        this IEnumerable<T> values) where T : IComparable
    {
        if (values == null || !values.Any())
        {
            return new List<T>();
        }

        //split the list into the first element and the rest
        var firstElement = values.First();
        var rest = values.Skip(1);

        //get the smaller and larger elements
        var smallerElements = rest
                .Where(i => i.CompareTo(firstElement) < 0)
                .QuickSort();

        var largerElements = rest
                .Where(i => i.CompareTo(firstElement) >= 0)
                .QuickSort();

        //return the result
        return smallerElements
            .Concat(new List<T>{firstElement})
            .Concat(largerElements);
    }
}
```

This is much cleaner, and reads almost the same as the F# version.  But unfortunately there is no way of avoiding the extra noise in the function signature.

## Correctness

Finally, a beneficial side-effect of this compactness is that F# code often works the first time, while the C# code may require more debugging. 

Indeed, when coding these samples, the old-style C# code was incorrect initially, and required some debugging to get it right. Particularly tricky areas were the `for` loop (starting at 1 not zero) and the `CompareTo` comparison (which I got the wrong way round), and it would also be very easy to accidentally modify the inbound list. The functional style in the second C# example is not only cleaner but was easier to code correctly.

But even the functional C# version has drawbacks compared to the F# version. For example, because F# uses pattern matching, it is not possible to branch to the "non-empty list" case with an empty list. On the other hand, in the C# code, if we forgot the test:

```csharp
if (values == null || !values.Any()) ...
```

then the extraction of the first element:

```csharp
var firstElement = values.First();
```

would fail with an exception. The compiler cannot enforce this for you.  In your own code, how often have you used `FirstOrDefault` rather than `First` because you are writing "defensive" code. Here is an example of a code pattern that is very common in C# but is rare in F#:

```csharp
var item = values.FirstOrDefault();  // instead of .First()
if (item != null) 
{ 
   // do something if item is valid 
}
```

The one-step "pattern match and branch" in F# allows you to avoid this in many cases.

## Postscript

The example implementation in F# above is actually pretty verbose by F# standards!  

For fun, here is what a more typically concise version would look like:

```fsharp
let rec quicksort2 = function
   | [] -> []                         
   | first::rest -> 
        let smaller,larger = List.partition ((>=) first) rest 
        List.concat [quicksort2 smaller; [first]; quicksort2 larger]
        
// test code        
printfn "%A" (quicksort2 [1;5;23;18;9;1;3])
```

Not bad for 4 lines of code, and when you get used to the syntax, still quite readable.
