---
layout: post
title: "F# decompiled into C#"
description: "Or, what C# code do you have to write to get the same functionality as F#?"
categories: ["F# vs C#", "Convenience"]
---

*The complete code for this post [is available on GitHub](https://github.com/swlaschin/fsharp-decompiled)*

A favorite trick of F# proponents is to take some F# code, compile it, and then decompile the result to C# using a tool such as [ILSpy](http://ilspy.net/).
This shows you the C# code you would have to write to get the same functionality as the F# code.

Generally, the F# code is *much* shorter than the equivalent C# code -- due to things like one-line type definitions, lack of curly braces, and so on. 

<blockquote class="twitter-tweet" lang="en"><p lang="en" dir="ltr">Just typed a 15 lines <a href="https://twitter.com/hashtag/FSharp?src=hash">#FSharp</a> program of <a href="https://twitter.com/dsyme">@dsyme</a>&#39;s book, compiled it, decompiled it using <a href="https://twitter.com/ilpsy">@ilpsy</a>, got 720 lines of <a href="https://twitter.com/hashtag/CSharp?src=hash">#CSharp</a> code. Woot!</p>&mdash; Axel Heer (@axelheer) <a href="https://twitter.com/axelheer/status/320487786597732352">April 6, 2013</a></blockquote>
<script async src="//platform.twitter.com/widgets.js" charset="utf-8"></script>

<blockquote class="twitter-tweet" lang="en"><p lang="en" dir="ltr">Looking at <a href="https://twitter.com/hashtag/fsharp?src=hash">#fsharp</a> code decompiled to <a href="https://twitter.com/hashtag/csharp?src=hash">#csharp</a> really shows how powerful the language, and not to mention the CLR, really is.</p>&mdash; Christian Palmstie~1 (@cpx86) <a href="https://twitter.com/cpx86/status/619603027750748160">July 10, 2015</a></blockquote>
<script async src="//platform.twitter.com/widgets.js" charset="utf-8"></script>

However, I couldn't find many concrete examples of this kind of decompilation on the web, so I thought I would put together some demonstrations for reference.

## Contents

This post will *not* cover all possible F# code. For example, I won't show any examples of lambdas or partial application,
which end up looking messy when decompiled. Rather, I'll just focus on two core things: basic type definitions and stand-alone functions in modules.

So I have grouped the examples as follows:

* **[Record Types](#records)**, with examples of both immutable and mutable record types, and a type with extra properties and methods.
* **[Classes](#classes)**, with examples of a basic class, then one with custom equality, then an interface, an abstract base class, and some concrete subclasses.
* **[Discriminated Union Types](#unions)**, with examples of a single-case union used as wrapper, an "enum" set of choices, and a more complex example with extra data for each choice.
* **[Modules](#modules)**, with examples of simple functions, and also a submodule.
* **[Pattern Matching](#pattern-matching)**, which shows the code that is generated for various kinds of pattern matching.

## How I generated the C# code

I did not write the C# code from scratch. Instead, I used the following process:

* I created some F# types, such as those in [this file](https://github.com/swlaschin/fsharp-decompiled/blob/master/FsExamples/RecordTypeExamples.fs).
* I compiled this to a DLL, opened the DLL in [ILSpy](http://ilspy.net/), and decompiled the code to C#.
* For each F# type, I created a corresponding C# class.
* I then tweaked the C# code to make it more idiomatic, using auto-properties, removing duplicate code, renaming variables, etc. I also added a few explanatory comments where needed.
* I next used Resharper and tweaked the C# code some more until R# was happy.
* The final C# code looks something [like this](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/RecordTypeExamples/FinalGameScore.cs).

NOTE: The goal was *not* to create perfect C# code, but to preserve the F# compiler output as much as possible, without giving C# devs too many conniptions.

<a name="records"></a>

## Record types

In F#, most types that contain fields or properties are defined as record types rather than classes, so let's start with these.

### A simple immutable record type

Here is an example of a simple immutable record in F#, with comments attached to the type and each property.

```fsharp
/// Example of a simple immutable record 
type FinalGameScore = { 
    /// Game property
    Game: string
    /// FinalScore property
    FinalScore : int
    }
```

In C# the equivalent of the generated code would look like this (I'll discuss the code in detail below).

```csharp
/// <summary>
///  Example of a simple immutable record 
/// </summary>
[Serializable]
public sealed class FinalGameScore :
    IEquatable<FinalGameScore>,
    IStructuralEquatable,
    IComparable<FinalGameScore>,
    IComparable,
    IStructuralComparable
{
    /// <summary>
    /// Game property
    /// </summary>
    public string Game { get; internal set; }

    /// <summary>
    /// FinalScore property
    /// </summary>
    public int FinalScore { get; internal set; }

    /// <summary>
    /// Constructor 
    /// </summary>
    public FinalGameScore(string game, int finalScore)
    {
        this.Game = game;
        this.FinalScore = finalScore;
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public int GetHashCode(IEqualityComparer comp)
    {
        var num = 0;
        const int offset = -1640531527;
        num = offset + (this.FinalScore + ((num << 6) + (num >> 2)));
        var game = this.Game;
        return offset + (((game == null) ? 0 : game.GetHashCode()) + ((num << 6) + (num >> 2)));
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public override int GetHashCode()
    {
        return this.GetHashCode(LanguagePrimitives.GenericEqualityComparer);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(FinalGameScore obj)
    {
        return obj != null
               && string.Equals(this.Game, obj.Game)
               && this.FinalScore == obj.FinalScore;
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public override bool Equals(object obj)
    {
        var finalGameScore = obj as FinalGameScore;
        return finalGameScore != null && this.Equals(finalGameScore);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(object obj, IEqualityComparer comp)
    {
        // ignore the IEqualityComparer as a simplification -- the generated F# code is more complex
        return Equals(obj);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(FinalGameScore obj)
    {
        if (obj == null)
        {
            return 1;
        }

        int num = string.CompareOrdinal(this.Game, obj.Game);
        if (num != 0)
        {
            return num;
        }

        return this.FinalScore.CompareTo(obj.FinalScore);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(object obj)
    {
        return this.CompareTo((FinalGameScore)obj);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(object obj, IComparer comp)
    {
        // ignore the IComparer as a simplification -- the generated F# code is more complex
        return this.CompareTo((FinalGameScore)obj);
    }

}
```

*[Source for FinalGameScore.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/RecordTypeExamples/FinalGameScore.cs)*

Let's go through the generated C# code in sections.

The first thing to notice is that the generated code implements a number of interfaces:

```csharp
public sealed class FinalGameScore :
    IEquatable<FinalGameScore>,
    IStructuralEquatable,
    IComparable<FinalGameScore>,
    IComparable,
    IStructuralComparable
```

In particular it overrides equality (both the generic one from `Object` and `IEquatable<T>`) and comparison (both `IComparable` and `IComparable<T>`) and also
`IStructuralEquatable` and `IStructuralComparable` (for details on what these are for, see
[this SO question](http://stackoverflow.com/questions/3609823/what-problem-does-istructuralequatable-and-istructuralcomparable-solve)).

The first part of the class body is similar to what we would write ourselves. Two get-only properties and a constructor.

```csharp
/// <summary>
/// Game property
/// </summary>
public string Game { get; internal set; }

/// <summary>
/// FinalScore property
/// </summary>
public int FinalScore { get; internal set; }

/// <summary>
/// Constructor 
/// </summary>
public FinalGameScore(string game, int finalScore)
{
    this.Game = game;
    this.FinalScore = finalScore;
}
```

Note that the F# code generates `internal set` properties rather than `private` ones. Since all the code in the assembly is F#,
and there is no special behavior in the getter or setter, this makes no practical difference.

The next section implements `GetHashCode` and three kinds of `Equals`. 

```csharp
/// <summary>
///  Needed for custom equality
/// </summary>
public int GetHashCode(IEqualityComparer comp)
{
    var num = 0;
    const int offset = -1640531527;
    num = offset + (this.FinalScore + ((num << 6) + (num >> 2)));
    var game = this.Game;
    return offset + (((game == null) ? 0 : game.GetHashCode()) + ((num << 6) + (num >> 2)));
}

/// <summary>
///  Needed for custom equality
/// </summary>
public override int GetHashCode()
{
    return this.GetHashCode(LanguagePrimitives.GenericEqualityComparer);
}

/// <summary>
///  Implement custom equality
/// </summary>
public bool Equals(FinalGameScore obj)
{
    return obj != null
           && string.Equals(this.Game, obj.Game)
           && this.FinalScore == obj.FinalScore;
}

/// <summary>
///  Implement custom equality
/// </summary>
public override bool Equals(object obj)
{
    var finalGameScore = obj as FinalGameScore;
    return finalGameScore != null && this.Equals(finalGameScore);
}

/// <summary>
///  Implement custom equality
/// </summary>
public bool Equals(object obj, IEqualityComparer comp)
{
    // ignore the IEqualityComparer as a simplification -- the generated F# code is more complex
    return Equals(obj);
}
```

The code that is generated for the third `Equals` method (using `IEqualityComparer`) is almost a duplicate of the primary `Equals` method, so to make the comparison fairer,
and to be more like the code that someone would write by hand, I have removed it and replaced with a call to the primary `Equals` method.

Finally, the various kinds of comparisons are implemented. Again I have removed the duplicated logic and tidied up the code to be a bit more idiomatic.

```csharp
/// <summary>
///  Implement custom comparison
/// </summary>
public int CompareTo(FinalGameScore obj)
{
    if (obj == null)
    {
        return 1;
    }

    int num = string.CompareOrdinal(this.Game, obj.Game);
    if (num != 0)
    {
        return num;
    }

    return this.FinalScore.CompareTo(obj.FinalScore);
}

/// <summary>
///  Implement custom comparison
/// </summary>
public int CompareTo(object obj)
{
    return this.CompareTo((FinalGameScore)obj);
}

/// <summary>
///  Implement custom comparison
/// </summary>
public int CompareTo(object obj, IComparer comp)
{
    // ignore the IComparer as a simplification -- the generated F# code is more complex
    return this.CompareTo((FinalGameScore)obj);
}
```

You might not always need special equality and comparison code to be generated, in which case you can mark a type with
`NoEqualityAttribute` and/or `NoComparisonAttribute`.

We'll see `NoComparisonAttribute` used in the next example.

### A mutable record

Let's have a look at how you would implement a record with one mutable property, such as a mutable `CurrentScore`, and without generating comparison code:

```fsharp
/// Example of a simple mutable record 
[<NoComparisonAttribute>]
type UpdatableGameScore = {
    /// Game property
    Game: string
    /// Mutable CurrentScore property
    mutable CurrentScore : int
    }
```

In the generated C# code, the `CurrentScore` property now has a setter as well as a getter, and the `IComparable` interfaces and the `CompareTo` implementations have been eliminated. 
Here is the relevant excerpt:

```csharp
[Serializable]
public sealed class UpdatableGameScore :
    IEquatable<UpdatableGameScore>,
    IStructuralEquatable
{
    /// <summary>
    /// Game property
    /// </summary>
    public string Game { get; internal set; }

    /// <summary>
    /// Mutable CurrentScore property
    /// </summary>
    public int CurrentScore { get; set; }

    /// <summary>
    /// Constructor 
    /// </summary>
    public UpdatableGameScore(string game, int currentScore)
    {
        this.Game = game;
        this.CurrentScore = currentScore;
    }
    
// remaining code snipped    
```

*[Source for UpdatableGameScore.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/RecordTypeExamples/UpdatableGameScore.cs)*

## Adding extra methods and properties

Finally, we often want to add extra properties or methods to a type. 

In the example below, I've defined a `Person` type that has a `FullName` property and a `IsBirthday` method
in addition to the core properties of `FirstName`, `LastName` and `DateOfBirth`.

```fsharp
/// Definition of a Person
type Person = {
    /// Stores first name
    FirstName: string
    /// Stores last name
    LastName: string
    /// Stores date of birth
    DateOfBirth: DateTime
    }
    with 
    
    /// FullName property
    member this.FullName = 
        this.FirstName + " " + this.LastName

    /// IsBirthday method
    member this.IsBirthday() = 
        DateTime.Today.Month = this.DateOfBirth.Month 
        && DateTime.Today.Day = this.DateOfBirth.Day
```

The first part of the generated C# code looks like this:

```csharp
/// <summary>
///  Definition of a Person
/// </summary>
[Serializable]
public sealed class Person :
    IEquatable<Person>,
    IStructuralEquatable,
    IComparable<Person>,
    IComparable,
    IStructuralComparable
{
    /// <summary>
    /// Stores first name
    /// </summary>
    public string FirstName { get; internal set; }

    /// <summary>
    /// Stores last name
    /// </summary>
    public string LastName { get; internal set; }

    /// <summary>
    /// Stores date of birth
    /// </summary>
    public DateTime DateOfBirth { get; internal set; }

    /// <summary>
    /// Constructor 
    /// </summary>
    public Person(string firstName, string lastName, DateTime dateOfBirth)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.DateOfBirth = dateOfBirth;
    }

    /// <summary>
    ///  FullName property
    /// </summary>
    public string FullName
    {
        get
        {
            return this.FirstName + " " + this.LastName;
        }
    }

    /// <summary>
    ///  IsBirthday method
    /// </summary>
    public bool IsBirthday()
    {
        return DateTime.Today.Month == this.DateOfBirth.Month && DateTime.Today.Day == this.DateOfBirth.Day;
    }

// remaining code snipped    
```

and of course there is the usual extra code for equality and comparison. Here is just one piece of that:

```csharp
/// <summary>
///  Implement custom equality
/// </summary>
public bool Equals(Person obj)
{
    return obj != null
           && string.Equals(this.FirstName, obj.FirstName)
           && string.Equals(this.LastName, obj.LastName)
           && LanguagePrimitives.HashCompare.GenericEqualityERIntrinsic(this.DateOfBirth, obj.DateOfBirth);
}
```

The `LanguagePrimitives` namespace comes from the F# core library, and contains helper code for hashing, equality testing, comparision, etc.  

*[Full source for Person.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/RecordTypeExamples/Person.cs)*

<a name="classes"></a>

## Classes

Sometimes you need inheritance or other OO features. So let's see what F# classes look like in C#.

### A simple class

Here's a simple class `Product` with one immutable property (`Id`), two mutable properties, a secondary constructor, two methods, and a static property containing a constant.

```fsharp
/// Example of a simple class
type Product(id, name, price) = 

    /// immutable Id property
    member this.Id = id

    /// mutable Name property
    member val Name = name with get,set

    /// mutable Price property
    member val Price = price with get,set

    /// secondary constructor
    new(id,name) = Product(id,name,Product.DefaultPrice)

    /// True if price > 10.00
    member this.IsExpensive = this.Price > 10.00

    /// Example of method
    member this.CanBeSoldTo(countryCode) = 
        match countryCode with
        | "US" 
        | "CA" 
        | "UK" -> true
        | "RU" -> false
        | _  -> false   //all others
    
    /// Example of static property
    static member DefaultPrice = 9.99
```

Here's what that looks like in the generated C#:

```csharp
/// <summary>
///  Example of a simple class
/// </summary>
[Serializable]
public class Product
{
    /// <summary>
    ///  immutable Id property
    /// </summary>
    public object Id { get; internal set; }

    /// <summary>
    ///  mutable Name property
    /// </summary>
    public object Name { get; set; }

    /// <summary>
    ///  mutable Price property
    /// </summary>
    public double Price { get; set; }

    /// <summary>
    ///  True if price &gt; 10.00
    /// </summary>
    public bool IsExpensive
    {
        get
        {
            return this.Price > 10.0;
        }
    }

    /// <summary>
    /// Example of static property
    /// </summary>
    public static double DefaultPrice
    {
        get
        {
            return 9.99;
        }
    }

    /// <summary>
    ///  primary constructor
    /// </summary>
    public Product(object id, object name, double price)
    {
        this.Id = id;
        this.Price = price;
        this.Name = name;
    }

    /// <summary>
    ///  secondary constructor
    /// </summary>
    public Product(object id, object name)
        : this(id, name, DefaultPrice)
    {
    }

    /// <summary>
    /// Example of method
    /// </summary>
    public bool CanBeSoldTo(string countryCode)
    {
        if (!string.Equals(countryCode, "US"))
        {
            if (!string.Equals(countryCode, "CA"))
            {
                if (!string.Equals(countryCode, "UK"))
                {
                    return string.Equals(countryCode, "RU") && false;
                }
            }
        }
        return true;
    }
}
```

*[Source for Product.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/ClassExamples/Product.cs)*

As you can see, the generated code no longer implements `IEquatable` and `IComparable`, which makes it much shorter. 

One thing to note is that the pattern matching in the F# version of `CanBeSoldTo` method has been unrolled into a series of `if` statements in C# (since C# doesn't have pattern matching).

Other than that, the generated code looks mostly as you would expect.

## A class with custom equality

Now what happens if we want a class with a custom implementation of equality, let's say an `Entity` class that compares using an `Id`?

```fsharp
/// Example of custom equality
type Entity(id:int, name:string) = 

    /// immutable Id property
    member this.Id = id

    /// mutable Name property
    member val Name = name with get,set

    /// Implement custom equality
    override this.Equals(obj) =
        match obj with
        | :? Entity as ent -> 
            this.Id = ent.Id   // no null checking needed
        | _ ->  false // all other cases

    /// Needed for custom equality
    override this.GetHashCode() =
        hash this.Id  

    /// Implement custom equality
    interface IEquatable<Entity> with
        member this.Equals(ent) =
            this.Id = ent.Id  // no null checking needed
```

Note that I have annotated the class constructor `type Entity(id:int, name:string)` to force the `id` to be an `int` and the `name` to be a `string`.

Here's the corresponding generated C#:

```csharp
/// <summary>
///  Example of custom equality
/// </summary>
[Serializable]
public class Entity : IEquatable<Entity>
{
    /// <summary>
    ///  immutable Id property
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    ///  mutable Name property
    /// </summary>
    public string Name { get; set; }

    public Entity(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public override bool Equals(object obj)
    {
        var entity = obj as Entity;
        if (entity == null) return false;
        return this.Id == entity.Id;
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public override int GetHashCode()
    {
        return this.Id;
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    bool IEquatable<Entity>.Equals(Entity ent)
    {
        return this.Id == ent.Id;
    }
}
```

Again, this code looks like standard C# code. 

*[Source for Entity.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/ClassExamples/Entity.cs)*

### A class hierarchy 

Finally, let's look at some OO code in F# and C#.  We'll start with an interface:

```fsharp
/// Interface
type IShape =
    abstract Name : string
    abstract Draw : unit -> unit
```

And the generated C# is similar:

```csharp
/// <summary>
///  Interface
/// </summary>
public interface IShape
{
    string Name { get; }
    void Draw();
}
```

Next, we'll define an abstract base class that implements `IShape`. The `Name` property is concrete, but the subclasses are expected to provide their own `Draw` method.

```fsharp
/// Abstract Base Class
[<AbstractClass>]
type ShapeBase(name) as self = 

    /// concrete implementation of Name property
    member this.Name = name

    /// abstract definition of Draw method
    abstract Draw : unit -> unit

    /// Explicit implementation of interface
    interface IShape with
        member this.Name = self.Name
        member this.Draw() = self.Draw()
```

And the generated C#:

```csharp
/// <summary>
///  Abstract Base Class
/// </summary>
[Serializable]
public abstract class ShapeBase : IShape
{
    /// <summary>
    ///  Explicit implementation of interface
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    ///  abstract definition of Draw method
    /// </summary>
    public abstract void Draw();

    protected ShapeBase(string name)
    {
        this.Name = name;
    }
```

And finally, here's a subclass of `ShapeBase` in F#:

```fsharp
/// Concrete class Square
type Square(name,size) =
    inherit ShapeBase(name)

    /// subclass specific property
    member this.Size = size

    /// concrete implementation of Draw method
    override this.Draw() =
        Console.Write("I am a square with size {0}",size)
```

And in C#:

```csharp
/// <summary>
///  Concrete class Square
/// </summary>
[Serializable]
public class Square : ShapeBase
{
    /// <summary>
    ///  subclass specific property
    /// </summary>
    public int Size { get; internal set; }

    public Square(string name, int size)
        : base(name)
    {
        this.Size = size;
    }

    /// <summary>
    ///  concrete implementation of Draw method
    /// </summary>
    public override void Draw()
    {
        Console.Write("I am a square with size {0}", this.Size);
    }
}
```

Source for all these examples:

* [Source for F# code](https://github.com/swlaschin/fsharp-decompiled/blob/master/FsExamples/ClassExamples.fs#L74)
* [Source for IShape.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/ClassExamples/IShape.cs)
* [Source for ShapeBase.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/ClassExamples/ShapeBase.cs)
* [Source for Square.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/ClassExamples/Square.cs)
* [Source for Circle.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/ClassExamples/Circle.cs)

<a name="unions"></a>

## Discriminated Unions

Now to discriminated unions, a feature that C# does not have. So how are they represented behind the scenes?

### Single-case union

We'll start with a single-case union, typically used to wrap a more primitive type to avoid [primitive obsession](http://blog.ploeh.dk/2015/01/19/from-primitive-obsession-to-domain-modelling/).

```fsharp
/// example of single-case union as a wrapper round a primitive
type ProductId = ProductId of int
```

One of the nice things about F# is that this type gets implementations of equality and comparison, just like record types.

This means that the generated C# code is going to be very long again, alas.  

```csharp
/// <summary>
///  example of single-case union as a wrapper round a primitive
/// </summary>
[Serializable]
public class ProductId :
    IEquatable<ProductId>,
    IStructuralEquatable,
    IComparable<ProductId>,
    IComparable,
    IStructuralComparable
{
    /// <summary>
    ///  Implemented for all F# union types. Not used in this case.
    /// </summary>
    public int Tag
    {
        get
        {
            return 0;
        }
    }

    /// <summary>
    ///  Property to access wrapped value
    /// </summary>
    public int Item { get; private set; }

    /// <summary>
    /// static public constructor 
    /// </summary>
    public static ProductId NewProductId(int item)
    {
        return new ProductId(item);
    }

    /// <summary>
    /// private constructor 
    /// </summary>
    internal ProductId(int item)
    {
        this.Item = item;
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public int GetHashCode(IEqualityComparer comp)
    {
        const int num = 0;
        return -1640531527 + (this.Item + ((num << 6) + (num >> 2)));
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public sealed override int GetHashCode()
    {
        return this.GetHashCode(LanguagePrimitives.GenericEqualityComparer);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(ProductId obj)
    {
        return obj != null && this.Item == obj.Item;
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public sealed override bool Equals(object obj)
    {
        var productId = obj as ProductId;
        return productId != null && this.Equals(productId);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(object obj, IEqualityComparer comp)
    {
        // ignore the IEqualityComparer as a simplification -- the generated F# code is more complex
        return Equals(obj);
    }


    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(ProductId obj)
    {
        if (obj == null)
        {
            return 1;
        }
        return this.Item.CompareTo(obj.Item);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(object obj)
    {
        return this.CompareTo((ProductId)obj);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(object obj, IComparer comp)
    {
        // ignore the IComparer as a simplification -- the generated F# code is more complex
        return this.CompareTo((ProductId)obj);
    }
}
```

*[Source for ProductId.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/UnionTypeExamples/ProductId.cs)*

The fact that the equivalent C# code is so long is one reason why primitive obsession is so common.

I know that, personally, I am *much* less likely to create a wrapper class in C# than in F#, just because of the effort involved.

Anyway, ignoring the equality and comparison code, we can see that the meat of the implementation is just a wrapper around a `Item` property, along with a static constructor `NewProductId`.

```csharp
public class ProductId 
{
    /// <summary>
    ///  Property to access wrapped value
    /// </summary>
    public int Item { get; private set; }

    /// <summary>
    /// static public constructor 
    /// </summary>
    public static ProductId NewProductId(int item)
    {
        return new ProductId(item);
    }

    /// <summary>
    /// private constructor 
    /// </summary>
    internal ProductId(int item)
    {
        this.Item = item;
    }
```


### "Enum" style unions

Another common use of discriminated unions is to emulate an Enum.

For example, here's one with three colors:

```fsharp
/// example of simple "enum"
type Color = Red | Green | Blue
```

This generates the following C# code:

```csharp
/// <summary>
///  example of simple "enum"
/// </summary>
[Serializable]
public class Color : 
    IEquatable<Color>, 
    IStructuralEquatable, 
    IComparable<Color>, 
    IComparable, 
    IStructuralComparable
{
    public static class Tags
    {
        public const int Red = 0;
        public const int Green = 1;
        public const int Blue = 2;
    }

    // singletons -- one for each "enum"
    internal static readonly Color _unique_Red = new Color(0);
    internal static readonly Color _unique_Green = new Color(1);
    internal static readonly Color _unique_Blue = new Color(2);

    /// <summary>
    ///  Implemented for all F# union types. Used in this case to distinguish between the singletons.
    /// </summary>
    public int Tag { get; private set; }

    /// <summary>
    ///  Static method to get one of the singletons
    /// </summary>
    public static Color Red
    {
        get
        {
            return _unique_Red;
        }
    }

    public bool IsRed
    {
        get
        {
            return Tag == 0;
        }
    }

    /// <summary>
    ///  Static method to get one of the singletons
    /// </summary>
    public static Color Green
    {
        get
        {
            return _unique_Green;
        }
    }

    public bool IsGreen
    {
        get
        {
            return Tag == 1;
        }
    }

    /// <summary>
    ///  Static method to get one of the singletons
    /// </summary>
    public static Color Blue
    {
        get
        {
            return _unique_Blue;
        }
    }

    public bool IsBlue
    {
        get
        {
            return Tag == 2;
        }
    }

    /// <summary>
    /// private constructor 
    /// </summary>
    internal Color(int tag)
    {
        Tag = tag;
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public int GetHashCode(IEqualityComparer comp)
    {
        return Tag;
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public sealed override int GetHashCode()
    {
        return GetHashCode(LanguagePrimitives.GenericEqualityComparer);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(Color obj)
    {
        if (obj != null)
        {
            return Tag == obj.Tag;
        }
        return false;
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public sealed override bool Equals(object obj)
    {
        var color = obj as Color;
        return color != null && Equals(color);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(object obj, IEqualityComparer comp)
    {
        // ignore the IEqualityComparer as a simplification -- the generated F# code is more complex
        return Equals(obj);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(Color obj)
    {
        if (obj == null)
        {
            return 1;
        }

        return Tag.CompareTo(obj.Tag);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(object obj)
    {
        return CompareTo((Color)obj);
    }

    /// <summary>
    ///  Implement custom comparison
    /// </summary>
    public int CompareTo(object obj, IComparer comp)
    {
        // ignore the IComparer as a simplification -- the generated F# code is more complex
        return CompareTo((Color)obj);
    }
}
```

*[Source for Color.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/UnionTypeExamples/Color.cs)*

The C# implementation creates a set of static singleton instances (`_unique_Red`, `_unique_Green`, etc.) which are then used everywhere else.  They are differentiated by their `Tag` number.

### A real Enum

In F#, you can also create a "real" enum by assigning int values to each case, like this:

```fsharp
/// example of a real C# enum
type ColorEnum = Red=1 | Green=2 | Blue=3
```

When decompiled into C# the definition is exactly an `enum`:

```csharp
[Serializable]
public enum ColorEnum
{
    Red = 1,
    Green,
    Blue
}
```

So, why not use this all the time in F#?  The main reason is that the `enum` style is not suitable for exhaustive pattern matching.

*Any* int can be cast to an enum, so the F# pattern matching will always need to have a wildcard case.
This is not needed for the discriminated union version that we defined in the previous section.


### An example of a complex union type

Finally, let's look at a more complex union type. 

Say that we have a `PaymentMethod` type that can be cash, check or credit card. We might model it like this:
    
```fsharp
type CheckNumber = CheckNumber of int
type CardType = MasterCard | Visa
type CardNumber = CardNumber of string

/// PaymentMethod is cash, check or card
[<NoComparisonAttribute>]
type PaymentMethod = 
    /// Cash needs no extra information
    | Cash
    /// Check needs a CheckNumber 
    | Check of CheckNumber 
    /// CreditCard needs a CardType and CardNumber 
    | CreditCard of CardType * CardNumber 
```

Note that I'm using the `[<NoComparisonAttribute>]` since I don't expect to be sorting the payment methods.

Decompiled to C#, this 12-line snippet results in four top-level classes and around 600 lines of code, as we'll see!

First we need to define classes for the three helper types `CheckNumber`, `CardType` and `CardNumber`.

For example, here's the first few lines of `CheckNumber`:

```csharp
[Serializable]
public class CheckNumber : 
    IEquatable<CheckNumber>, 
    IStructuralEquatable, 
    IComparable<CheckNumber>, 
    IComparable, 
    IStructuralComparable
{
    /// <summary>
    ///  Property to access wrapped value
    /// </summary>
    public int Item { get; private set; }

    /// <summary>
    /// static public constructor 
    /// </summary>
    public static CheckNumber NewCheckNumber(int item)
    {
        return new CheckNumber(item);
    }

    /// <summary>
    /// private constructor 
    /// </summary>
    internal CheckNumber(int item)
    {
        this.Item = item;
    }

// [snipped rest of file]
```

This is similar to the `ProductId` type, as is `CardNumber`. And `CardType` is very similar to `Color`.

Now for the main `PaymentMethod` type.

I'm going to ignore all the equality and comparison code, and just focus on how it is implemented.

* Each case is represented by an inner class that is a subclass of `PaymentMethod`. 
* Each inner class has properties to store the data associated with that case. 
* The `Cash` case has no associated data, so it only needs one instance, implemented as a static singleton.
* A `Tag` is used to differentiate the subclasses and to help with equality testing and comparison.

Here's the core code:

```csharp
/// <summary>
///  PaymentMethod is cash, check or card
/// </summary>
[Serializable]
public abstract class PaymentMethod :
    IEquatable<PaymentMethod>,
    IStructuralEquatable
{

    public static class Tags
    {
        public const int Cash = 0;
        public const int Check = 1;
        public const int CreditCard = 2;
    }

    /// <summary>
    ///  Private Subclass: Cash needs no extra information, so is represented by a singleton
    /// </summary>
    [Serializable]
    internal class _Cash : PaymentMethod
    {
    }

    /// <summary>
    ///  Public Subclass: Check needs a CheckNumber 
    /// </summary>
    [Serializable]
    public class Check : PaymentMethod
    {
        public CheckNumber Item { get; private set; }

        internal Check(CheckNumber item)
        {
            Item = item;
        }
    }

    /// <summary>
    ///  Public Subclass: CreditCard needs a CardType and CardNumber 
    /// </summary>
    [Serializable]
    public class CreditCard : PaymentMethod
    {
        public CardType Item1 { get; private set; }
        public CardNumber Item2 { get; private set; }

        internal CreditCard(CardType item1, CardNumber item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    /// <summary>
    ///  Implemented for all F# union types. Used in this case for equality and comparision
    /// </summary>
    public int Tag
    {
        get
        {
            return (!(this is CreditCard)) ? ((!(this is Check)) ? 0 : 1) : 2;
        }
    }

    // Cash has no extra data so can be implemented as singleton instance
    internal static readonly PaymentMethod _unique_Cash = new _Cash();


    /// <summary>
    /// static public "constructor"
    /// (just gets the singleton)
    /// </summary>
    public static PaymentMethod Cash
    {
        get
        {
            return _unique_Cash;
        }
    }

    public bool IsCash
    {
        get
        {
            return this is _Cash;
        }
    }

    /// <summary>
    /// static public constructor 
    /// </summary>
    public static PaymentMethod NewCheck(CheckNumber item)
    {
        return new Check(item);
    }

    public bool IsCheck
    {
        get
        {
            return this is Check;
        }
    }

    /// <summary>
    /// static public constructor 
    /// </summary>
    public static PaymentMethod NewCreditCard(CardType item1, CardNumber item2)
    {
        return new CreditCard(item1, item2);
    }

    public bool IsCreditCard
    {
        get
        {
            return this is CreditCard;
        }
    }

    /// <summary>
    /// private constructor 
    /// </summary>
    internal PaymentMethod()
    {
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public int GetHashCode(IEqualityComparer comp)
    {
        if (!(this is _Cash))
        {
            const int offset = -1640531527;
            var check = this as Check;
            if (check != null)
            {
                const int num = 1;
                return offset + (check.Item.GetHashCode(comp) + ((num << 6) + (num >> 2)));
            }
            var creditCard = this as CreditCard;
            if (creditCard != null)
            {
                var num = 2;
                num = offset + (creditCard.Item2.GetHashCode(comp) + ((num << 6) + (num >> 2)));
                return offset + (creditCard.Item1.GetHashCode(comp) + ((num << 6) + (num >> 2)));
            }
        }
        return 0;
    }

    /// <summary>
    ///  Needed for custom equality
    /// </summary>
    public sealed override int GetHashCode()
    {
        return GetHashCode(LanguagePrimitives.GenericEqualityComparer);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(PaymentMethod obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (Tag != obj.Tag)
        {
            return false;
        }

        var check1 = this as Check;
        if (check1 != null)
        {
            var check2 = (Check)obj;
            return check1.Item.Equals(check2.Item);
        }
        var creditCard1 = this as CreditCard;
        if (creditCard1 != null)
        {
            var creditCard2 = (CreditCard) obj;
            return creditCard1.Item1.Equals(creditCard2.Item1) && creditCard1.Item2.Equals(creditCard2.Item2);
        }
        return true;
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public sealed override bool Equals(object obj)
    {
        var paymentMethod = obj as PaymentMethod;
        return paymentMethod != null && Equals(paymentMethod);
    }

    /// <summary>
    ///  Implement custom equality
    /// </summary>
    public bool Equals(object obj, IEqualityComparer comp)
    {
        // ignore the IEqualityComparer as a simplification -- the generated F# code is more complex
        return Equals(obj);
    }
}
```

In comparison with the 12 lines of F# code, the generated C# looks like this:

* [Source for CheckNumber.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/UnionTypeExamples/CheckNumber.cs) (about 120 lines)
* [Source for CardType.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/UnionTypeExamples/CardType.cs) (about 150 lines)
* [Source for CardNumber.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/UnionTypeExamples/CardNumber.cs) (about 120 lines)
* [Source for PaymentMethod.cs](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/UnionTypeExamples/PaymentMethod.cs) (about 220 lines)

That's around 600 lines altogether. It's true that some of that may be methods that we might not need,
but even so, it's still quite a lot of code to write by hand.
    
<a name="modules"></a>

## Modules

Now let's look at modules.

In F#, a module is the standard technique for grouping standalone functions.

```fsharp
module ModuleExample

/// add two numbers
let Add x y = x + y

/// add 1 to a number
let Add1 x = x + 1
```

Behind the scenes, this code is implemented as static methods on a static class. Here's the generated C# code:

```csharp
public static class ModuleExample
{
    /// <summary>
    ///  add two numbers
    /// </summary>
    public static int Add(int x, int y)
    {
        return x + y;
    }

    /// <summary>
    ///  add 1 to a number
    /// </summary>
    public static int Add1(int x)
    {
        return x + 1;
    }
```

### Types defined in modules

In F#, types can also be defined in modules:

```fsharp
module ModuleExample

/// define a empty class inside a module
type Something() = class end 
```

This is represented in C# as a inner class -- defined inside the static module class:

```csharp
public static class ModuleExample
{
    // [snipped Add and Add1]

    [Serializable]
    public class Something
    {
        public Something()                
        {
        }
    }
```

### Submodules

In F#, a module can contain submodules. For example, I might want to group some functions that work with the `FinalGameScore` type defined at the top of this post.

```fsharp
module ModuleExample

// [snipped Add and Add1]

/// Create a submodule
module GameFunctions =
    open RecordTypeExamples

    /// Create a game with score=12
    let CreateGame name = {Game=name; FinalScore=12}

    /// Change the score for an existing game
    let ChangeScore newScore game = 
        {game with FinalScore=newScore}

    /// Example of a higher order function
    let MapScore f game = 
        {game with FinalScore=f game.FinalScore}
```

In the generated C#, this code becomes another inner static class:

```csharp
public static class ModuleExample
{
    // [snipped Add and Add1]

    /// <summary>
    ///  Create a submodule
    /// </summary>
    public static class GameFunctions
    {
        /// <summary>
        /// Create a game with score=12
        /// </summary>
        public static FinalGameScore CreateGame(string name)
        {
            return new FinalGameScore(name, 12);
        }

        /// <summary>
        /// Change the score for an existing game
        /// </summary>
        public static FinalGameScore ChangeScore(int newScore, FinalGameScore game)
        {
            return new FinalGameScore(game.Game, newScore);
        }

        /// <summary>
        ///  Example of a higher order function
        /// </summary>
        public static FinalGameScore MapScore(FSharpFunc<int, int> f, FinalGameScore game)
        {
            return new FinalGameScore(game.Game, f.Invoke(game.FinalScore));
        }
    }
}
```

A couple of things to note:

First, the F# code never mentions the type `FinalGameScore` -- it is inferred by the `{game with FinalScore=newScore}` code.
On the other hand, the generated C# code mentions the type `FinalGameScore` in many places -- that's why a tool like Resharper is so useful when you want to rename things.

Second, the `MapScore` function is a simple example of a higher order function.
It takes an `int->int` function to change the score.  In normal C#, this would be represented by a `Func<int,int>`, but F# has it's own func type `FSharpFunc`.
If you need to expose higher order functions from F# to C#, you will probably need to change the function type to a `Func`.

The source code for the two implementations is available here:

* [ModuleExample.fs source](https://github.com/swlaschin/fsharp-decompiled/blob/master/FsExamples/ModuleExample.fs)
* [ModuleExample.cs source](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/ModuleExample.cs)

<a name="pattern-matching"></a>

## Pattern matching

Finally, let's see what F# pattern matching code looks like when turned into C# code.

### Simple pattern matching

Let's start with a simple integer pattern match.  The code below has some integer pattern matching, plus a guard `when x%2 = 0`, and finally the wildcard.

```fsharp
/// demonstrates some simple pattern matching
let IntPatternMatching x = 
    match x with
    | 1 -> "1"
    | 2 -> "2"
    | 3 -> "3"
    | 4 -> "4"
    // example of guard 
    | e when x%2 = 0 -> "even" 
    // wildcard
    | _ -> "other"
```

The generated code is a straightforward switch statement.

```csharp
public static string IntPatternMatching(int x)
{
    switch (x)
    {
        case 1:
            return "1";
        case 2:
            return "2";
        case 3:
            return "3";
        case 4:
            return "4";
        default:
            if (x % 2 == 0)
            {
                return "even";
            }
            return "other";
    }
}
```

### Nested matching

But what if the value being matching on is not a primitive? In F# we can do nested pattern matching that accesses the internal of the value.

For example, let's say that we have a `Person` type that contains a `Name` type. Then in F# we can write:

```fsharp
type Name= {First:string; Last:string}
type Person = {Name:Name; Age:int}

/// demonstrates some nested pattern matching
let NestedPatternMatching person = 
    match person with
    | {Name={First="Jane";Last="Doe"}} -> "Jane Doe"
    | {Name={First="Jane"}} -> "Jane something"
    | {Name={Last="Doe"}} -> "something Doe"
    // example of guard 
    | {Age=age} when age > 18 -> "Adult" 
    // wildcard
    | _ -> "other"
```

The generated code looks like this in C#, which is quite clunky, but similar to what you would have to write by hand: 

```csharp
public static string NestedPatternMatching(Person person)
{
    if (string.Equals(person.Name.First, "Jane"))
    {
        if (string.Equals(person.Name.Last, "Doe"))
        {
            return "Jane Doe";
        }
        return "Jane something";
    }
    else
    {
        if (string.Equals(person.Name.Last, "Doe"))
        {
            return "something Doe";
        }
        if (person.Age > 18)
        {
            return "Adult";
        }
        return "other";
    }
}
```

Note that the static `string.Equals` is used to avoid an additional null check for `person.Name.First` and `person.Name.Last`.

### Pattern matching in a parameter

F# supports pattern matching in function parameters as well. This is a great way to specify the parameter type and to extract a value in one step.

For example, in the following code, we extract the inner string from the `Email` value, lowercase it, and return a new `Email` value.

```fsharp
/// demonstrates some in-parameter pattern matching
let LowercaseEmail (Email e) = 
    e.ToLowerInvariant() |> Email
```

The C# for this is very similar, except that the parameter type must be given explicitly:

```csharp
/// <summary>
/// demonstrates some in-parameter pattern matching
/// </summary>
public static Email LowercaseEmail(Email email)
{
    var lower = email.Item.ToLowerInvariant();
    return Email.NewEmail(lower);
}

```

### List pattern matching 

When it comes to pattern matching on lists, F# provides some nice syntax for fixed sized lists (e.g. `[a;b]`) or heads and tails (e.g. `a::b::rest`).

```fsharp
/// demonstrates some list-testing pattern matching
let ListTesting list = 
    match list with 
    | [] -> 
        sprintf "Empty list"
    | [a;b] -> 
        sprintf "Exactly two elements %A and %A" a b
    | a::b::rest -> 
        sprintf "Two or more elements starting with  %A and %A" a b
    | a::rest -> 
        sprintf "One or more elements starting with  %A" a
```

When we look at the generated code this pattern matching consists of a series of `if` statements as shown below.  

```csharp
public static string ListTesting<T>(FSharpList<T> list)
{
    // test for empty
    var tail1 = list.TailOrNull;
    if (tail1 == null)
    {
        return ExtraTopLevelOperators.PrintFormatToString(
            new PrintfFormat<string, Unit, string, string, Unit>("Empty list"));
    }

    // first element is valid
    var firstElem = list.HeadOrDefault;

    // test for one or more elements
    if (tail1.TailOrNull == null)
    {
        var print = ExtraTopLevelOperators.PrintFormatToString(
            new PrintfFormat<FSharpFunc<T, string>, Unit, string, string, T>("One or more elements starting with  %A"));
        return print.Invoke(firstElem);
    }

    // second element is valid
    var secondElem = tail1.HeadOrDefault;

    // test for exactly two elements
    var tail2 = tail1.TailOrNull;
    if (tail2.TailOrNull == null)
    {
        var print2 = ExtraTopLevelOperators.PrintFormatToString(
            new PrintfFormat<FSharpFunc<T, FSharpFunc<T, string>>, Unit, string, string, Tuple<T, T>>("Exactly two elements %A and %A"));
        return print2.Invoke(firstElem).Invoke(secondElem);
    }

    // test for two or more elements
    var print3 = ExtraTopLevelOperators.PrintFormatToString(
        new PrintfFormat<FSharpFunc<T, FSharpFunc<T, string>>, Unit, string, string, Tuple<T, T>>("Two or more elements starting with  %A and %A"));
    return print3.Invoke(firstElem).Invoke(secondElem);
}
```

Notes:

* I have simplified the real generated code somewhat while maintaining the core features.
* The long-winded `ExtraTopLevelOperators.PrintFormatToString` is just the external name of `sprintf`!
  Obviously, if I were writing this in C# myself, I would use `string.Format`. 
* `print2` and `print3` are in curried form and are invoked twice -- once for each parameter.

So, replacing the F# printing with `string.Format`, a more idiomatic C# version would look like this:

```csharp
public static string ListTesting<T>(FSharpList<T> list)
{
    // test for empty
    var tail1 = list.TailOrNull;
    if (tail1 == null)
    {
        return "Empty list";
    }

    // first element is valid
    var firstElem = list.HeadOrDefault;

    // test for one or more elements
    if (tail1.TailOrNull == null)
    {
        return string.Format("One or more elements starting with {0}", firstElem);
    }

    // second element is valid
    var secondElem = tail1.HeadOrDefault;

    // test for exactly two elements
    var tail2 = tail1.TailOrNull;
    if (tail2.TailOrNull == null)
    {
        return string.Format("Exactly two elements {0} and {1}", firstElem, secondElem);
    }

    // test for two or more elements
    return string.Format("Two or more elements starting with {0} and {1}", firstElem, secondElem);
}
```
        
### Type-testing pattern matching 

Finally, let's look at how pattern matching on types is done using the `:?` operator.

```fsharp
/// demonstrates some type-testing pattern matching
let TypeTesting obj = 
    match box obj with
    | :? string as s -> 
        sprintf "Obj is string with value %s" s
    | :? int as i -> 
        sprintf "Obj is int with value %i" i
    | :? Person as p -> 
        sprintf "Obj is Person with name %s %s" p.Name.First p.Name.Last
    | _ -> 
        sprintf "Obj is something else" 
```

The generated code looks like this:

```csharp
public static string TypeTesting<T>(T obj)
{
    // NOTE: This is a simplified version of the real generated code

    var str = obj as string;
    if (str != null)
    {
        var printString = ExtraTopLevelOperators.PrintFormatToString(
            new PrintfFormat<FSharpFunc<string, string>, Unit, string, string, string>("Obj is string with value %s"));
        return printString.Invoke(str);
    }

    if (LanguagePrimitives.IntrinsicFunctions.TypeTestGeneric<int>(obj))
    {
        var i = (int)(obj as object);
        var printInt = ExtraTopLevelOperators.PrintFormatToString(
            new PrintfFormat<FSharpFunc<int, string>, Unit, string, string, int>("Obj is int with value %i"));
        return printInt.Invoke(i);
    }

    var person = obj as Person;
    if (person != null)
    {
        var printPerson = ExtraTopLevelOperators.PrintFormatToString(
            new PrintfFormat<FSharpFunc<string, FSharpFunc<string, string>>, Unit, string, string, Tuple<string, string>>("Obj is Person with name %s %s"));
        return printPerson.Invoke(person.Name.First).Invoke(person.Name.Last);
    }

    return ExtraTopLevelOperators.PrintFormatToString(
        new PrintfFormat<string, Unit, string, string, Unit>("Obj is something else"));
}
```


Notes:

* Again, I have simplified the real generated code somewhat while maintaining the core features.
* Note that in the F# code I never had to test that a string or Person was null. The null case is handled by the wildcard branch. In the generated code, the null test is added for me.
* `printPerson` is in curried form and is invoked twice -- once for each parameter.

The source code for the pattern matching examples is available here:

* [PatternMatchingExamples.fs source](https://github.com/swlaschin/fsharp-decompiled/blob/master/FsExamples/PatternMatchingExamples.fs)
* [PatternMatchingExamples.cs source](https://github.com/swlaschin/fsharp-decompiled/blob/master/CsEquivalents/PatternMatchingExamples.cs)

## Summary

The title of this post is "F# decompiled into C#".
That was the primary purpose of this post -- to demonstrate the kind of boilerplate code that the F# compiler generates for you
"for free", as a service to people who don't have a tool like ILSpy handy.

The subtitle of this post is "What C# code do you have to write to get the same functionality as F#?"
That is, if I stopped using F#, and wanted to write equivalent code in C#, what would I have to do?

Now obviously, the code generated by the F# compiler has a lot of stuff in it that I would not bother to reproduce if I were writing the equivalent C# by hand.
For example, we might not need to implement `CompareTo` in many cases, and many of the methods in the generated code might be tweaked to be smaller.

Nevertheless, *even* with these caveats, and *even* writing the most compact and idiomatic C# code possible,
it is hard to create, say, an immutable wrapper type with equality in a very few lines of C# code.

Unfortunately, that overhead discourages me from creating small classes that model the domain and encourages the "primitive obsession" code smell.
I'm sacrificing good design and maybe even introducing bugs because the language makes it painful for me to do what I want. 

For those of you who think I am being "brutal" -- sorry. This is not meant to be a gloat or an attack on C#, just an example of what F# provides for free.

*EDIT: Based on the feedback in the comments, I have tweaked the C# code to be more idiomatic, using auto-properties, etc.
Also, the summary above and some of the body text has been updated.*

*The complete code for this post [is available on GitHub](https://github.com/swlaschin/fsharp-decompiled)*
