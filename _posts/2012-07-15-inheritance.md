---
layout: post
title: "Inheritance and abstract classes"
description: ""
nav: fsharp-types
seriesId: "Object-oriented programming in F#"
seriesOrder: 3
categories: [Object-oriented, Classes]
---

This is a follow-on from the [previous post on classes](/posts/classes/). This post will focus on inheritance in F#, and how to define and use abstract classes and interfaces.

## Inheritance

To declare that a class inherits from another class, use the syntax:

```fsharp
type DerivedClass(param1, param2) =
   inherit BaseClass(param1)
```

The `inherit` keyword signals that `DerivedClass` inherits from `BaseClass`. In addition, some `BaseClass` constructor must be called at the same time.

It might be useful to compare F# with C# at this point. Here is some C# code for a very simple pair of classes. 

```csharp
public class MyBaseClass
{
    public MyBaseClass(int param1)
    {
        this.Param1 = param1;
    }
    public int Param1 { get; private set; }
}

public class MyDerivedClass: MyBaseClass
{
    public MyDerivedClass(int param1,int param2): base(param1)
    {
        this.Param2 = param2;
    }
    public int Param2 { get; private set; }
}
```

Note that the inheritance declaration `class MyDerivedClass: MyBaseClass` is distinct from the constructor which calls `base(param1)`.

Now here is the F# version:

```fsharp
type BaseClass(param1) =
   member this.Param1 = param1

type DerivedClass(param1, param2) =
   inherit BaseClass(param1)
   member this.Param2 = param2

// test
let derived = new DerivedClass(1,2)
printfn "param1=%O" derived.Param1
printfn "param2=%O" derived.Param2
```

Unlike C#, the inheritance part of the declaration, `inherit BaseClass(param1)`, contains both the class to inherit from *and* its constructor.

## Abstract and virtual methods

Obviously, part of the point of inheritance is to be able to have abstract methods, virtual methods, and so on.

### Defining abstract methods in the base class

In C#, an abstract method is indicated by the `abstract` keyword plus the method signature. In F#, it is the same concept, except that the way that function signatures are written in F# is quite different from C#.

```fsharp
// concrete function definition
let Add x y = x + y

// function signature
// val Add : int -> int -> int
```

So to define an abstract method, we use the signature syntax, along with the `abstract member` keywords:

```fsharp
type BaseClass() =
   abstract member Add: int -> int -> int
```

Notice that the equals sign has been replaced with a colon. This is what you would expect, as the equals sign is used for binding values, while the colon is used for type annotation.

Now, if you try to compile the code above, you will get an error! The compiler will complain that there is no implementation for the method. To fix this, you need to: 

* provide a default implementation of the method, or 
* tell the compiler that the class as whole is also abstract.

We'll look at both of these alternatives shortly.

### Defining abstract properties

An abstract immutable property is defined in a similar way. The signature is just like that of a simple value.

```fsharp
type BaseClass() =
   abstract member Pi : float
```

If the abstract property is read/write, you add the get/set keywords.

```fsharp
type BaseClass() =
   abstract Area : float with get,set
```

### Default implementations (but no virtual methods)

To provide a default implementation of an abstract method in the base class, use the `default` keyword instead of the `member` keyword:

```fsharp
// with default implementations
type BaseClass() =
   // abstract method
   abstract member Add: int -> int -> int
   // abstract property
   abstract member Pi : float 

   // defaults
   default this.Add x y = x + y
   default this.Pi = 3.14
```

You can see that the default method is defined in the usual way, except for the use of `default` instead of `member`.

One major difference between F# and C# is that in C# you can combine the abstract definition and the default implementation into a single method, using the `virtual` keyword. In F#, you cannot. You must declare the abstract method and the default implementation separately. The `abstract member` has the signature, and the `default` has the implementation.

### Abstract classes

If at least one abstract method does *not* have a default implementation, then the entire class is abstract, and you must indicate this by annotating it with the `AbstractClass` attribute. 

```fsharp
[<AbstractClass>]
type AbstractBaseClass() =
   // abstract method
   abstract member Add: int -> int -> int

   // abstract immutable property
   abstract member Pi : float 

   // abstract read/write property
   abstract member Area : float with get,set
```

If this is done, then the compiler will no longer complain about a missing implementation.

### Overriding methods in subclasses

To override an abstract method or property in a subclass, use the `override` keyword instead of the `member` keyword.  Other than that change, the overridden method is defined in the usual way.

```fsharp
[<AbstractClass>]
type Animal() =
   abstract member MakeNoise: unit -> unit 

type Dog() =
   inherit Animal() 
   override this.MakeNoise () = printfn "woof"

// test
// let animal = new Animal()  // error creating ABC
let dog = new Dog()
dog.MakeNoise()
```

And to call a base method, use the `base` keyword, just as in C#.

```fsharp
type Vehicle() =
   abstract member TopSpeed: unit -> int
   default this.TopSpeed() = 60

type Rocket() =
   inherit Vehicle() 
   override this.TopSpeed() = base.TopSpeed() * 10

// test
let vehicle = new Vehicle()
printfn "vehicle.TopSpeed = %i" <| vehicle.TopSpeed()
let rocket = new Rocket()
printfn "rocket.TopSpeed = %i" <| rocket.TopSpeed()
```

### Summary of abstract methods

Abstract methods are basically straightforward and similar to C#. There are only two areas that might be tricky if you are used to C#:

* You must understand how function signatures work and what their syntax is!  For a detailed discussion see the [post on function signatures](/posts/function-signatures/).
* There is no all-in-one virtual method. You must define the abstract method and the default implementation separately.

