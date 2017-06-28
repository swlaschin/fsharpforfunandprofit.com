---
layout: page
title: "The C# Light project"
description: 
hasComments: 1
---

**Welcome to the "C# light" project page.**

As you have probably guessed, "C# light" is not a real project.  It was inspired by [this post by Phil Trelford](http://www.trelford.com/blog/post/LighterCSharp.aspx).

However, the clean syntax and features of "C# light" are based closely on the F# language, which this site is devoted to.

Many people think that F# has a strange syntax, and are put off by the name.
I thought that if I created a ["C# light" slideshow](http://www.slideshare.net/ScottWlaschin/c-light) that demonstrated the benefits of the F# syntax under another name,
skeptical C# developers might be more open-minded!

So, if you are a skeptical C# developer, let me show you that F# is almost identical to "C# light", with only a few minor tweaks.

## "C# light" compared with F# ##

Here is the "C# light" example that I used throughout the slides:

```csharp
class Person(string name, DateTime birthday) =

    /// Full name
    member Name = name 

    /// Birthday
    member Birthday = birthday
```

And here is the equivalent F# code:

```fsharp
type Person(name :string, birthday :DateTime) =

    /// Full name
    member this.Name = name 

    /// Birthday
    member this.Birthday = birthday
```

There are a few minor syntax changes:

* The keyword `class` is replaced with `type`
* The type annotations are "backwards". Rather than `string name`, the parameter is declared as `name:string`
* The `this` keyword is added to the member declarations.

But other than that, the code is very similar.

## The DTO example 

Here is the example in the C# light slide deck which uses a syntax similar to anonymous types to declare a simple DTO class:

```csharp
class Person = {string name, DateTime birthday}

var person = {name="Alice", birthday=Today}
```

And here is the F# equivalent:

```fsharp
type Person = {name :string; birthday :DateTime}

let person = {name="Alice"; birthday=DateTime.Today}
```

There are two more changes here:

* The `var` keyword is replaced by `let` in F#.
* Commas are replaced by semicolons in both the definition and usage of the class.

## Automatically generate code for equality

As promised by the slides, the F# compiler *does* automatically generate equality code for most types.

```fsharp
type Person = {name :string; birthday :DateTime}

let alice1 = {name="Alice"; birthday=DateTime.Today}
let alice2 = {name="Alice"; birthday=DateTime.Today}

if alice1 = alice2 then
    Console.WriteLine("Alice1 and Alice2 are equal")  // true!
```


## Immutability

In F#, user-defined types are immutable by default.  If you want to change them, you have to use the `mutable` keyword.

```fsharp
type Person = {name :string; birthday :DateTime}

//define an immutable Alice 
let alice3 = {name="Alice"; birthday=DateTime.Today}

//changing immutable Alice to Bob gives an error
alice3 <- {name="Bob"; birthday=DateTime.Today}  // This value is not mutable

//define a mutable Alice 
let mutable alice4 = {name="Alice"; birthday=DateTime.Today}

//changing mutable Alice to Bob is ok
alice4 <- {name="Bob"; birthday=DateTime.Today}
Console.WriteLine("Alice's name is " + alice4.name)
```

## Non-null reference classes

In F#, user-defined types are not allowed to be null.

```fsharp
type Person = {name :string; birthday :DateTime}

let mutable alice4 = {name="Alice"; birthday=DateTime.Today}
alice4 <- null  // error
   // The type 'Person' does not have 'null' as a proper value
```


## Allow anonymous types to implement interfaces

Here's how an object instance can implement an interface in F#, in this case `IDisposable`.

```fsharp
do 

    // create it with a "using" block
    use tempDisposable = 
      {new IDisposable with 
        member this.Dispose() = Console.WriteLine("Disposed") }

    // do something             
    Console.WriteLine("Doing something") 
    
    // tempDisposable goes out of scope and is disposed
```

The console output is:

```text
Doing something 
Disposed
```
    
In F#, these are actually called ["object expressions"](/posts/object-expressions/).
    
## Allow subclasses to be merged into a single "case" class

In F#, these are called ["discriminated unions"](/posts/discriminated-unions/) and they are one of the 
best things about F#.

Here's the C# light version:

```csharp
class PaymentMethod = 
| Cash
| Check(int checkNo)
| Card(string cardType, string cardNo)
```

And here's the F# version:

```fsharp
type CheckNo = int
type CardType = Visa | Mastercard
type CardNo = string

type PaymentMethod = 
| Cash
| Check of CheckNo
| Card of CardType * CardNo
```

Here's the C# way of constructing an object:

```csharp
PaymentMethod cash = Cash();
PaymentMethod check = Check(123);
PaymentMethod card = Card("Visa", "4012888888881881");
```

And here's the F# way:

```fsharp
let cash = Cash
let check = Check 123
let card = Card (Visa, "4012888888881881")
```

Finally, here's the C# way of deconstructing a payment:

```csharp
void PrintPayment(payment) =   
   switch (payment) 
   {
     case Cash : // print cash
     case Check(checkNo) : // print check info
     case Card(cardType,cardNo) // print card info
   }
```

And here's the F# way:

```fsharp
let printPayment payment =   
   match payment with
   | Cash -> printfn "Cash"
   | Check checkNo -> printfn "Check %i" checkNo
   | Card (cardType,cardNo) -> printfn "Card %A %s" cardType cardNo
   
printPayment cash
printPayment check
printPayment card   
```

##  Interested in learning more?

The F# code for these examples is available on [.Net Fiddle here](https://dotnetfiddle.net/K1zs3W).
Do play with them to convince yourself that F# really does fulfill all the promises of C# Light!

If you want to see more about F#, [this page](/site-contents/) is a good starting point for browsing this site.
Or visit [fsharp.org](http://fsharp.org) for more about F# in general.

And for more about the power of F# types, check out my slide show on "Domain Driven Design with F# types", below. And there is more DDD stuff [here](/ddd/).

<script async class="speakerdeck-embed" data-id="96b632008eb6013146041a945ae20cc0" data-ratio="1.33333333333333" src="//speakerdeck.com/assets/embed.js"></script>

 
 