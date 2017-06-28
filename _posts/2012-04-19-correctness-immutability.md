---
layout: post
title: "Immutability"
description: "Making your code predictable"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 19
categories: [Correctness, Immutability]
---

To see why immutability is important, let's start with a small example.

Here's some simple C# code that processes a list of numbers.

```csharp
public List<int> MakeList() 
{
   return new List<int> {1,2,3,4,5,6,7,8,9,10};
}

public List<int> OddNumbers(List<int> list) 
{ 
   // some code
}

public List<int> EvenNumbers(List<int> list) 
{ 
   // some code
}
```

Now let me test it:

```csharp
public void Test() 
{ 
   var odds = OddNumbers(MakeList()); 
   var evens = EvenNumbers(MakeList());
   // assert odds = 1,3,5,7,9 -- OK!
   // assert evens = 2,4,6,8,10 -- OK!
}
```

Everything works great, and the test passes, but I notice that I am creating the list twice -- surely I should refactor this out?  So I do the refactoring, and here's the new improved version:

```csharp
public void RefactoredTest() 
{ 
   var list = MakeList();
   var odds = OddNumbers(list); 
   var evens = EvenNumbers(list);
   // assert odds = 1,3,5,7,9 -- OK!
   // assert evens = 2,4,6,8,10 -- FAIL!
}
```

But now the test suddenly fails! Why would a refactoring break the test? Can you tell just by looking at the code?

The answer is, of course, that the list is mutable, and it is probable that the `OddNumbers` function is making destructive changes to the list as part of its filtering logic. Of course, in order to be sure, we would have to examine the code inside the `OddNumbers` function.

In other words, when I call the `OddNumbers` function, I am unintentionally creating undesirable side effects.  

Is there a way to ensure that this cannot happen?  Yes -- if the functions had used `IEnumerable` instead:

```csharp
public IEnumerable<int> MakeList() {}
public List<int> OddNumbers(IEnumerable<int> list) {} 
public List<int> EvenNumbers(IEnumerable <int> list) {}
```

In this case we can be confident that calling the `OddNumbers` function could not possibly have any effect on the list, and `EvenNumbers` would work correctly. What's more, we can know this *just by looking at the signatures*, without having to examine the internals of the functions.  And if you try to make one of the functions misbehave by assigning to the list then you will get an error straight away, at compile time. 

So `IEnumerable` can help in this case, but what if I had used a type such as `IEnumerable<Person>` instead of `IEnumerable<int>`? Could I still be as confident that the functions wouldn't have unintentional side effects?

## Reasons why immutability is important ##

The example above shows why immutability is helpful. In fact, this is just the tip of the iceberg. There are a number of reasons why immutability is important:

* Immutable data makes the code predictable
* Immutable data is easier to work with
* Immutable data forces you to use a "transformational" approach 

First, immutability makes the code **predictable**. If data is immutable, there can be no side-effects. If there are no side-effects, it is much, much, easier to reason about the correctness of the code. 

And when you have two functions that work on immutable data, you don't have to worry about which order to call them in, or whether one function will mess with the input of the other function.  And you have peace of mind when passing data around (for example, you don't have to worry about using an object as a key in a hashtable and having its hash code change).

In fact, immutability is a good idea for the same reasons that global variables are a bad idea: data should be kept as local as possible and side-effects should be avoided. 

Second, immutability is **easier to work with**.  If data is immutable, many common tasks become much easier.  Code is easier to write and easier to maintain. Fewer unit tests are needed (you only have to check that a function works in isolation), and mocking is much easier. Concurrency is much simpler, as you don't have to worry about using locks to avoid update conflicts (because there are no updates). 

Finally, using immutability by default means that you start thinking differently about programming. You tend to think about **transforming** the data rather than mutating it in place. 

SQL queries and LINQ queries are good examples of this "transformational" approach.  In both cases, you always transform the original data through various functions (selects, filters, sorts) rather than modifying the original data.  

When a program is designed using a transformation approach, the result tends to be more elegant, more modular, and more scalable. And as it happens, the transformation approach is also a perfect fit with a function-oriented paradigm.

## How F# does immutability ##

We saw earlier that immutable values and types are the default in F#:

```fsharp
// immutable list
let list = [1;2;3;4]    

type PersonalName = {FirstName:string; LastName:string}
// immutable person
let john = {FirstName="John"; LastName="Doe"}
```

Because of this, F# has a number of tricks to make life easier and to optimize the underlying code.

First, since you can't modify a data structure, you must copy it when you want to change it. F# makes it easy to copy another data structure with only the changes you want:

```fsharp
let alice = {john with FirstName="Alice"}
```

And complex data structures are implemented as linked lists or similar, so that common parts of the structure are shared. 

```fsharp
// create an immutable list
let list1 = [1;2;3;4]   

// prepend to make a new list
let list2 = 0::list1    

// get the last 4 of the second list 
let list3 = list2.Tail

// the two lists are the identical object in memory!
System.Object.ReferenceEquals(list1,list3)
```

This technique ensures that, while you might appear to have hundreds of copies of a list in your code, they are all sharing the same memory behind the scenes.

## Mutable data ##

F# is not dogmatic about immutability; it does support mutable data with the `mutable` keyword. But turning on mutability is an explicit decision, a deviation from the default, and it is generally only needed for special cases such as optimization, caching, etc, or when dealing with the .NET libraries.  

In practice, a serious application is bound to have some mutable state if it deals with messy world of user interfaces, databases, networks and so on.  But F# encourages the minimization of such mutable state. You can generally still design your core business logic to use immutable data, with all the corresponding benefits. 

