---
layout: post
title: "Is your programming language unreasonable?"
description: "or, why predictability is important"
categories: ["F# vs C#","Correctness"]
image: "/assets/img/safety_first.jpg"
---

As should be obvious, one of the goals of this site is to persuade people to take F# seriously as a general purpose development language.

But as functional idioms have become more mainstream, and C# has added functional capabilities such as lambdas and LINQ, it seems like C# is "catching up" with F# more and more. 

So, ironically, I've now started to hear people say things like this:

* "C# already has most of the features of F#, so why should I bother to switch?"*
* "There is no need to change. All we have do is wait a couple of years and C# will get many of the F# features that provide the most benefits."
* "F# is slightly better than C#, but not so much that it's really worth the effort to move towards it." 
* "F# seems really nice, even if it's a bit intimidating. But I can't see a practical purpose to use it over C#." 

No doubt, the same comments are being made in the JVM ecosystem about Scala and Clojure vs. Java, now that Java has lambdas too.

So for this post, I'm going to stray away from F#, and focus on C# (and by proxy, other mainstream languages),
and try to demonstrate that, even with all the functional features in the world, programming in C# will never be the same as programming in F#.

Before I start, I want to make it clear that I am *not* hating on C#. As it happens I like C# very much; it is one of my favorite mainstream languages,
and it has evolved to be very powerful while being consistent and backwards compatible, which is a hard thing to pull off.

But C# is not perfect. Like most mainstream OO languages, it contains some design decisions which no amount of LINQ or lambda goodness can compensate for.

In this post, I'll show you some of the issues that these design decisions cause, and suggest some ways to improve the language to avoid them.

*(I'm now going to don my flameproof suit. I think I might need it!)*

----

UPDATE: Many people have [seriously misread](http://www.washingtonpost.com/local/serious-reading-takes-a-hit-from-online-scanning-and-skimming-researchers-say/2014/04/06/088028d2-b5d2-11e3-b899-20667de76985_story.html) this post, it seems. So let me be clear:

* I am *not* saying that statically typed languages are "better" than dynamic languages.
* I am *not* saying that FP languages are "better" than OO languages.
* I am *not* saying that being able to reason about code is the most important aspect of a language.
 
What I *am* saying is:

* Not being able to reason about code has costs that many developers might not be aware of.
* Therefore, being "reasonable" should be one of the (many) factors under consideration when choosing a programming language, not just ignored due to lack of awareness.
* *IF* you want to be able to reason about your code, *THEN* it will be much easier if your language supports the features that I mention.
* The fundamental paradigm of OO (object-identity, behavior-based) is not compatible with "reasonability", and so it will be hard to retrofit existing OO languages to add this quality.

That's it. Thank you!




----

## What is a "reasonable" programming language, anyway?

If you hang around functional programmers, you will often hear the phrase "reason about", as in "we want to reason about our programs".

What does that mean? Why use the word "reason" rather than just "understand"?

The use of "reasoning" goes back to mathematics and logic, but I'm going to use a simple and pragmatic definition:

* "reasoning about the code" means that you can draw conclusions using only the information that you have *right in front of you*, rather than having to delve into other parts of the codebase.

In other words, you can predict the behavior of some code just by looking at it.  You may need to understand the interfaces to other components, but you shouldn't need to look inside them
to see what they do.

Since, as developers, we spend most of our time looking at code, this is a pretty important aspect of programming!

Of course, there is a huge amount of advice out there on how to do just this: naming guidelines, formatting rules, design patterns, etc., etc.

But can your programming language *by itself* help your code to be more reasonable, more predictable?  I think the answer is yes, but I'll let you judge for yourself.

Below, I'll present a series of code fragments. After each snippet, I'm going to ask you what you think the code does. I've deliberately not shown my own comments so that you can
think about it and do your own reasoning. After you have thought about it, scroll down to read my opinion.

-----

## Example 1 

Let's start off by looking at the following code.

* We start with a variable `x` that is assigned the integer `2`.
* Then `DoSomething` is called with `x` as a parameter.
* Then `y` is assigned to `x - 1`.

The question I would ask you is simple: What is the value of `y`?

```csharp
var x = 2;
DoSomething(x);

// What value is y? 
var y = x - 1;
```

(scroll down for answer)

<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>


The answer is `-1`.  Did you get that answer?  No? If you can't figure it out, scroll down again.

<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>

Trick question!  This code is actually JavaScript! 

Here's the whole thing:

```csharp
function DoSomething (foo) { x = false}

var x = 2;
DoSomething(x);
var y = x - 1;
```

Yes, it's horrible! `DoSomething` accesses `x` directly rather than through the parameter, and then turns it into a boolean of all things!
Then, subtracting 1 from `x` casts it from `false` to `0`, so that `y` is `-1`.

Don't you totally hate this?  Sorry to mislead you about the language, but I just wanted to demonstrate how annoying it is when the language behaves in unpredictable ways.

JavaScript is a very useful and important language. But no one would claim that [reasonableness](http://stackoverflow.com/a/1995298/1136133) was one of its [strengths](/assets/img/javascript-the-good-parts.jpg).
In fact, most dynamically-typed languages have [quirks that make them hard to reason about](https://www.destroyallsoftware.com/talks/wat) in this way.  

Thanks to static typing and sensible scoping rules, this kind of thing could never happen in C# (unless you tried really hard!)
In C#, if you don't match up the types properly, you get a *compile-time* error rather than a *run-time* error. 

In other words, C# is much more predictable than JavaScript. Score one for static typing!

So now we have our first requirement for making a language predictable:


*__How to make your language predictable__*: 

1. Variables should not be allowed to change their type.

C# is looking good compared to JavaScript. But we're not done yet...

<br><br><br>

*UPDATE: This is an admittedly silly example. In retrospect, I could have picked a better one.
Yes, I know that no one sensible would ever do this. The point still stands: the JavaScript language does not prevent you from doing stupid things with implicit typecasts.*

-----

## Example 2 

In this next example, we're going to create two instances of the same `Customer` class, with exactly the same data in them.

The question is: Are they equal?

```csharp
// create two customers
var cust1 = new Customer(99, "J Smith");
var cust2 = new Customer(99, "J Smith");

// true or false?
cust1.Equals(cust2);
```

(scroll down for answer)

<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>


```csharp
// true or false?
cust1.Equals(cust2);
```

Who knows? It depends on how the `Customer` class has been implemented. This code is *not* predictable.

You'll have to look at whether the class implements `IEquatable` at least,
and you'll probably have to look at the internals of the class as well to see exactly what is going on.

*But why is this even an issue?*

Let me ask you this:

* How often would you NOT want the instances to be equal?  
* How often have you had to override the `Equals` method?
* How often have you had a bug caused by *forgetting* to override the `Equals` method?
* How often have you had a bug caused by mis-implementing `GetHashCode` (such as forgetting to change it when the fields that you compare on change)?

Why not make the objects equal by default, and make reference equality testing the special case?

So let's add another item to our list. 

*__How to make your language predictable__*: 

1. Variables should not be allowed to change their type.
1. **Objects containing the same values should be equal by default.**

-----

## Example 3

In this next example, I've got two objects containing exactly the same data, but which are instances of different classes.

The question again is: Are they equal?

```csharp
// create a customer and an order
var cust = new Customer(99, "J Smith");
var order = new Order(99, "J Smith");

// true or false?
cust.Equals(order);
```

(scroll down for answer)

<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>


```csharp
// true or false?
cust.Equals(order);
```

Who cares! This is almost certainly a bug! Why are you even comparing two different classes like this in the first place?

Compare their names or ids, certainly, but not the objects themselves.  This should be a compiler error.

If it isn't, why not? You probably just used the wrong variable name by mistake but now you have a subtle bug in your code. Why does your language let you do this?

So let's add another item to our list. 

*__How to make your language predictable__*: 

1. Variables should not be allowed to change their type.
1. Objects containing the same values should be equal by default.
1. **Comparing objects of different types is a compile-time error.**
 
<br><br><br>
 
*UPDATE: Many people have pointed out that you need this when comparing classes related by inheritance. This is true, of course.
But what is the cost of this feature? You get the ability to compare subclasses, but you lose the ability to detect accidental errors.*

*Which is more important in practice? That's for you to decide, I just wanted to make it clear that there are costs associated with the status quo, not just benefits.*

-----
 
## Example 4

In this snippet, we're just going to create a `Customer` instance. That's all. Can't get much more basic than that.

```csharp
// create a customer
var cust = new Customer();

// what is the expected output?
Console.WriteLine(cust.Address.Country);
```

Now the question is: what is the expected output of `WriteLine`?

(scroll down for answer)

<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>



```csharp
// what is the expected output?
Console.WriteLine(cust.Address.Country);
```

Who knows?  

It depends on whether the `Address` property is null or not. And that is something you can't tell without looking at the internals of the `Customer` class again.

Yes, we know that it is a best practice that constructors should initialize all fields at construction time,
but why doesn't the language enforce it?

If the address is required, then make it be required in the constructor.
And if the address is *not* always required, then make it clear that the `Address` property is optional and might be missing.

So let's add another item to our list of improvements. 

*__How to make your language predictable__*: 

1. Variables should not be allowed to change their type.
1. Objects containing the same values should be equal by default.
1. Comparing objects of different types is a compile-time error.
1. **Objects must *always* be initialized to a valid state. Not doing so is a compile-time error.**

-----

## Example 5

In this next example, we're going to:

* Create a customer.
* Add it to a set that uses hashing.
* Do something with the customer object.
* See if the customer is still in the set.

What could possibly go wrong?

```csharp
// create a customer
var cust = new Customer(99, "J Smith");

// add it to a set
var processedCustomers = new HashSet<Customer>();
processedCustomers.Add(cust);

// process it
ProcessCustomer(cust);

// Does the set contain the customer? true or false?
processedCustomers.Contains(cust);
```

So, does the set still contain the customer at the end of this code?

(scroll down for answer)

<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>


```csharp
// Does the set contain the customer?
processedCustomers.Contains(cust);
```

Maybe. Maybe not.  

It depends on two things: 

* First, does the hash code of the customer depend on a *mutable* field, such as an id. 
* Second, does `ProcessCustomer` change this field? 

If both are true, then the hash will have been changed, and the customer will not longer *appear* to exist in the set (even though it is still in there somewhere!).

This might well cause subtle performance and memory problems (e.g. if the set is a cache).

How could the language prevent this?

One way would be to say that any field or property used in `GetHashCode` must be immutable, while allowing other properties to be mutable. But that is really impractical.

Better to just make the entire `Customer` class immutable instead!

Now if the `Customer` class was immutable, and `ProcessCustomer` wanted to make changes, it would have to return a *new version* of the customer, and the code would look like this:

```csharp
// create a customer
var cust = new ImmutableCustomer(99, "J Smith");

// add it to a set
var processedCustomers = new HashSet<ImmutableCustomer>();
processedCustomers.Add(cust);

// process it and return the changes
var changedCustomer = ProcessCustomer(cust);

// true or false?
processedCustomers.Contains(cust);
```

Notice that the `ProcessCustomer` line has changed to:

```csharp
var changedCustomer = ProcessCustomer(cust);
```

It's clear that `ProcessCustomer` has changed something just by looking at this code.
If `ProcessCustomer` *hadn't* changed anything, it wouldn't have needed to return an object at all.

Going back to the question, it's clear that in this implementation the original version of the customer is guaranteed to still be in the set, no matter what `ProcessCustomer` does.

Of course, that doesn't solve the issue of whether the new one or the old one (or both) should be in the set.
But unlike the implementation using the mutable customer, this issue is now staring you in the face and won't go unnoticed accidentally.

So [immutability FTW](http://stackoverflow.com/a/4763485/1136133)!

So that's another item for our list. 

*__How to make your language predictable__*: 

1. Variables should not be allowed to change their type.
1. Objects containing the same values should be equal by default.
1. Comparing objects of different types is a compile-time error.
1. Objects must *always* be initialized to a valid state. Not doing so is a compile-time error.
1. **Once created,  objects and collections *must* be immutable.**

Time for a quick joke about immutability:

> "How many Haskell programmers does it take to change a lightbulb?"

> "Haskell programmers don't "change" lightbulbs, they "replace" them. And you must also replace the whole house at the same time."

Almost done now -- just one more!

-----

## Example 6

In this final example, we'll try to fetch a customer from a `CustomerRepository`.

```csharp
// create a repository
var repo = new CustomerRepository();

// find a customer by id
var customer = repo.GetById(42);

// what is the expected output?
Console.WriteLine(customer.Id);
```

The question is: after we do `customer = repo.GetById(42)`, what is the value of `customer.Id`?

(scroll down for answer)

<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>


```csharp
var customer = repo.GetById(42);

// what is the expected output?
Console.WriteLine(customer.Id);
```

It all depends, of course. 

If I look at the method signature of `GetById`, it tells me it always returns a `Customer`. But does it *really*?

What happens if the customer is missing? Does `repo.GetById` return `null`? Does it throw an exception? You can't tell just by looking at the code that we've got. 

In particular, `null` is a terrible thing to return. It's a turncoat that pretends to be a `Customer` and can be assigned to `Customer` variables with nary a complaint from the compiler,
but when you actually ask it to do something, it blows up in your face with an evil cackle.  Unfortunately, I can't tell by looking at this code whether a null is returned or not.

Exceptions are a little better, because at least they are typed and contain information about the context. But it's not apparent from the method signature which exceptions might be thrown.
The only way that you can know for sure is by looking at the internal source code (and maybe the documentation, if you're lucky and it is up to date).

But now imagine that your language did not allow `null` and did not allow exceptions. What could you do instead?

The answer is, you would be forced to return a special class that might contain *either* a customer *or* an error, like this:

```csharp
// create a repository
var repo = new CustomerRepository();

// find a customer by id and
// return a CustomerOrError result
var customerOrError = repo.GetById(42);
```

The code that processed this "customerOrError" result would then have to test what kind of result it was, and handle each case separately, like this:

```csharp
// handle both cases
if (customerOrError.IsCustomer)
    Console.WriteLine(customerOrError.Customer.Id);

if (customerOrError.IsError)
    Console.WriteLine(customerOrError.ErrorMessage);
```
    
This is exactly the approach taken by most functional languages. It does help if the language provides conveniences to make this technique easier, such as sum types,
but even without that, this approach is still the only way to go if you want to make it obvious what your code is doing. (You can read more about this technique [here](/rop/).)
    
So that's the last two items to add to our list, at least for now. 

*__How to make your language predictable__*: 

1. Variables should not be allowed to change their type.
1. Objects containing the same values should be equal by default.
1. Comparing objects of different types is a compile-time error.
1. Objects must *always* be initialized to a valid state. Not doing so is a compile-time error.
1. Once created,  objects and collections *must* be immutable.
1. **No nulls allowed.**
1. **Missing data or errors must be made explicit in the function signature.**

I could go on, with snippets demonstrating the misuse of globals, side-effects, casting, and so on. But I think I'll stop here -- you've probably got the idea by now!

## Can your programming language do *this*?

I hope that it is obvious that making these additions to a programming language will help to make it more reasonable.

Unfortunately, mainstream OO languages like C# are very unlikely to add these features. 

First of all, it would be a major breaking change to all existing code.

Second, many of these changes go deeply against the grain of the object-oriented programming model itself. 

For example, in the OO model, object identity is paramount, so *of course* equality by reference is the default.

Also, from an OO point of view, how two objects are compared is entirely up to the objects themselves -- OO is all about polymorphic behavior and the compiler needs to stay out of it!
Similarly, how objects are constructed and initialized is again entirely up to the object itself. There are no rules to say what should or should not be allowed.

Finally, it is very hard to add non-nullable reference types to a statically typed OO language without also implementing the initialization constraints in point 4.
As Eric Lippert himself has said ["Non-nullability is the sort of thing you want baked into a type system from day one, not something you want to retrofit 12 years later"](http://blog.coverity.com/2013/11/20/c-non-nullable-reference-types/).

In contrast, most functional programming languages have these "high-predictability" features as a core part of the language. 

For example, in F#, all but one of the items on that list are built into the language:

1. Values are not allowed to change their type. (And this even includes implicit casts from int to float, say).
1. Records with the same internal data *ARE* equal by default.
1. Comparing values of different types *IS* a compile-time error.
1. Values *MUST* be initialized to a valid state. Not doing so is a compile-time error.
1. Once created, values *ARE* immutable by default.
1. Nulls are *NOT* allowed, in general.

Item #7 is not enforced by the compiler, but discriminated unions (sum types) are generally used to return errors rather than using exceptions, so
that the function signature indicates exactly what the possible errors are.

It's true that when working with F# there are still many caveats. You *can* have mutable values, you *can* create and throw exceptions, and you may indeed have to deal with nulls that come from non-F# code.

But these things are considered code smells and are unusual, rather than being the general default.

Other languages such as Haskell are even purer (and hence even more reasonable) than F#, but even Haskell programs will not be perfect. 

In fact, no language can be reasoned about *perfectly* and still be practical. But still, some languages are certainly more reasonable than others.

I think that one of the reasons why many people have become so enthusiastic about functional-style code (and call it "simple" even though it's full of [strange symbols](https://gist.github.com/folone/6089236)!) is exactly this:
immutability, and lack of side effects, and all the other functional principles, act together to enforce this reasonability and predictability,
which in turn helps to reduce your cognitive burden so that you need only focus on the code in front of you. 


## Lambdas aren't the solution

So now it should be clear that this list of proposed improvements has nothing to do with language enhancements such as lambdas or clever functional libraries.

In other words, when I focus on reasonability, **I don't care what my language *will* let me do, I care more about what my language *won't* let me do.**
I want a language that stops me doing stupid things by mistake.

That is, if I had to choose between language A that didn't allow nulls, or language B that had higher-kinded types but still allowed objects to be null easily,
I would pick language A without hesitation.

## Questions 

Let me see if I can prempt some questions...

**Question: These examples are very contrived! If you code carefully and follow good practices, you can write safe code without these features!**

Yes, you can. I'm not claiming you can't. But this post is not about writing safe code, it's about *reasoning* about the code. There is a difference.

And it's not about what you can do if you are careful. It's about what can happen if you are not careful!  
That is, does your *programming language* (not your coding guidelines, or tests, or IDE, or development practices) give you support for reasoning about your code?

**Question: You're telling me that a language *should* have these features. Isn't that very arrogant of you?**

Please read carefully. I am not saying that at all. What I *am* saying is that: 

* *IF* you want to be able to reason about your code, *THEN* it will be much easier if your language supports the features that I mention.

If reasoning about your code is not that important to you, then please do feel free to ignore everything I've said!

**Question: Focusing on just one aspect of a programming language is too limiting. Surely other qualities are just as important?**

Yes, or course they are. I am not a absolutist on this topic.
I think that factors such as comprehensive libraries, good tooling, a welcoming community, and the strength of the ecosystem are very important too.

But the purpose of this post was to address the specific comments I mentioned at the beginning, such as: "C# already has most of the features of F#, so why should I bother to switch?".

**Question: Why are you dismissing dynamic languages so quickly?**

First, my apologies to JavaScript developers for the dig earlier! 

I like dynamic languages a lot, and one of my favorite languages, Smalltalk, is completely unreasonable by the standards I've talked about. Luckily,
this post is not trying to persuade you which languages are "best" in general, but rather just discussing one aspect of that choice. 

**Question: Immutable data structures are slow, and there will be lots of extra allocation going on. Won't this affect performance? **

This post is not attempting to address the performance impact (or any other aspect) of these features. 

But it is indeed a valid question to ask which should have a higher priority: code quality or performance? That's for you to decide, and it depends on the context.

Personally, I would go for safety and quality first, unless there was a compelling reason not to. Here's a sign I like:

![Safety, Quality, Quantity, in that order](/assets/img/safety_first.jpg)

## Summary

I said just above that this post is not trying to persuade you to pick a language based on "reasonability" alone. But that's not quite true.

If you have already picked a statically typed, high-level language such as C# or Java,
then it's clear that reasonability or something like it was an important criterion in your language decision.

In that case, I hope that the examples in this post might have made you more willing to consider using an even more "reasonable" language on your platform of choice (.NET or JVM).

The argument for staying put -- that your current language will eventually "catch up" -- may be true purely in terms of features, 
but no amount of future enhancements can really change the core design decisions in an OO language.
You'll never get rid of nulls, or mutability, or having to override equality all the time.

What's nice about F#, or Scala/Clojure, is that these functional alternatives don't require you to change your ecosystem, but they do immediately improve your code quality. 

In my opinion, it's quite a low risk compared with the cost of business as usual. 

*(I'll leave the issue of finding skilled people, training, support, etc, for another post.
But see [this](http://www.paulgraham.com/pypar.html),
[this](https://twitter.com/panesofglass/status/559431579328475136),
[this](https://twitter.com/foxyjackfox/status/559415445594206208), 
and [this](http://wesmorgan.svbtle.com/recruiting-software-developers-language-matters) if you're worried about hiring)*

