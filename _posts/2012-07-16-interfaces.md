---
layout: post
title: "Interfaces"
description: ""
nav: fsharp-types
seriesId: "Object-oriented programming in F#"
seriesOrder: 4
categories: [Object-oriented, Interfaces]
---

Interfaces are available and fully supported in F#, but there are number of important ways in which their usage differs from what you might be used to in C#. 

### Defining interfaces

Defining an interface is similar to defining an abstract class. So similar, in fact, that you might easily get them confused.

Here's an interface definition:

```fsharp
type MyInterface =
   // abstract method
   abstract member Add: int -> int -> int

   // abstract immutable property
   abstract member Pi : float 

   // abstract read/write property
   abstract member Area : float with get,set
```

And here's the definition for the equivalent abstract base class:

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

So what's the difference? As usual, all abstract members are defined by signatures only. The only difference seems to be the lack of the `[<AbstractClass>]` attribute.

But in the earlier discussion on abstract methods, we stressed that the `[<AbstractClass>]` attribute was required; the compiler would complain that the methods have no implementation otherwise. So how does the interface definition get away with it?  

The answer is trivial, but subtle. *The interface has no constructor*. That is, it does not have any parentheses after the interface name:

```fsharp
type MyInterface =   // <- no parens!
```

That's it.  Removing the parens will convert a class definition into an interface!

### Explicit and implicit interface implementations 

When it comes time to implement an interface in a class, F# is quite different from C#.  In C#, you can add a list of interfaces to the class definition and implement the interfaces implicitly. 

Not so in F#. In F#, all interfaces must be *explicitly* implemented. 

In an explicit interface implementation, the interface members can only be accessed through an interface instance (e.g. by casting the class to the interface type). The interface members are not visible as part of the class itself.

C# has support for both explicit and implicit interface implementations, but almost always, the implicit approach is used, and many programmers are not even aware of [explicit interfaces in C#](http://msdn.microsoft.com/en-us/library/ms173157.aspx).


### Implementing interfaces in F# ###

So, how do you implement an interface in F#?  You cannot just "inherit" from it, as you would an abstract base class.  You have to provide an explicit implementation for each interface member using the syntax `interface XXX with`, as shown below:

```fsharp
type IAddingService =
    abstract member Add: int -> int -> int

type MyAddingService() =
    
    interface IAddingService with 
        member this.Add x y = 
            x + y

    interface System.IDisposable with 
        member this.Dispose() = 
            printfn "disposed"
```

The above code shows how the class `MyAddingService` explicitly implements the `IAddingService` and the `IDisposable` interfaces. After the required `interface XXX with` section, the members are implemented in the normal way.

(As an aside, note again that `MyAddingService()` has a constructor, while `IAddingService` does not.)

### Using interfaces

So now let's try to use the adding service interface:

```fsharp
let mas = new MyAddingService()
mas.Add 1 2    // error 
```

Immediately, we run into an error. It appears that the instance does not implement the `Add` method at all. Of course, what this really means is that we must cast it to the interface first using the `:>` operator:

```fsharp
// cast to the interface
let mas = new MyAddingService()
let adder = mas :> IAddingService
adder.Add 1 2  // ok
```

This might seem incredibly awkward, but in practice it is not a problem as in most cases the casting is done implicitly for you. 

For example, you will typically be passing an instance to a function that specifies an interface parameter. In this case, the casting is done automatically:

```fsharp
// function that requires an interface
let testAddingService (adder:IAddingService) = 
    printfn "1+2=%i" <| adder.Add 1 2  // ok

let mas = new MyAddingService()
testAddingService mas // cast automatically
```

And in the special case of `IDisposable`, the `use` keyword will also automatically cast the instance as needed:

```fsharp
let testDispose = 
    use mas = new MyAddingService()
    printfn "testing"
    // Dispose() is called here
```

