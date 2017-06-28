---
layout: post
title: "Organizing functions"
description: "Nested functions and modules"
nav: thinking-functionally
seriesId: "Thinking functionally"
seriesOrder: 10
categories: [Functions, Modules]
---

Now that you know how to define functions, how can you organize them?

In F#, there are three options:

* functions can be nested inside other functions.
* at an application level, the top level functions are grouped into "modules".
* alternatively, you can also use the object-oriented approach and attach functions to types as methods.

We'll look at the first two options in this post, and the third in the next post.

## Nested Functions ##

In F#, you can define functions inside other functions. This is a great way to encapsulate "helper" functions that are needed for the main function but shouldn't be exposed outside.

In the example below `add` is nested inside `addThreeNumbers`:

```fsharp
let addThreeNumbers x y z  = 

    //create a nested helper function
    let add n = 
       fun x -> x + n
       
    // use the helper function       
    x |> add y |> add z

// test
addThreeNumbers 2 3 4
```

A nested function can access its parent function parameters directly, because they are in scope. 
So, in the example below, the `printError` nested function does not need to have any parameters of its own -- it can access the `n` and `max` values directly.

```fsharp
let validateSize max n  = 

    //create a nested helper function with no params
    let printError() = 
        printfn "Oops: '%i' is bigger than max: '%i'" n max

    // use the helper function               
    if n > max then printError()

// test
validateSize 10 9
validateSize 10 11
```

A very common pattern is that the main function defines a nested recursive helper function, and then calls it with the appropriate initial values.
The code below is an example of this:

```fsharp
let sumNumbersUpTo max = 

    // recursive helper function with accumulator    
    let rec recursiveSum n sumSoFar = 
        match n with
        | 0 -> sumSoFar
        | _ -> recursiveSum (n-1) (n+sumSoFar)

    // call helper function with initial values
    recursiveSum max 0
            
// test
sumNumbersUpTo 10
```


When nesting functions, do try to avoid very deeply nested functions, especially if the nested functions directly access the variables in their parent scopes rather than having parameters passed to them.
A badly nested function will be just as confusing as the worst kind of deeply nested imperative branching.

Here's how *not* to do it:

```fsharp
// wtf does this function do?
let f x = 
    let f2 y = 
        let f3 z = 
            x * z
        let f4 z = 
            let f5 z = 
                y * z
            let f6 () = 
                y * x
            f6()
        f4 y
    x * f2 x
```


## Modules ##

A module is just a set of functions that are grouped together, typically because they work on the same data type or types.

A module definition looks very like a function definition. It starts with the `module` keyword, then an `=` sign, and then the contents of the module are listed.
The contents of the module *must* be indented, just as expressions in a function definition must be indented.

Here's a module that contains two functions:

```fsharp
module MathStuff = 

    let add x y  = x + y
    let subtract x y  = x - y
```

Now if you try this in Visual Studio, and you hover over the `add` function, you will see that the full name of the `add` function is actually `MathStuff.add`, just as if `MathStuff` was a class and `add` was a method.

Actually, that's exactly what is going on. Behind the scenes, the F# compiler creates a static class with static methods. So the C# equivalent would be:

```csharp
static class MathStuff
{
    static public int add(int x, int y)
    {
        return x + y;
    }

    static public int subtract(int x, int y)
    {
        return x - y;
    }
}
```

If you realize that modules are just static classes, and that functions are static methods, then you will already have a head-start on understanding how modules work in F#,
as most of the rules that apply to static classes also apply to modules.

And, just as in C# every standalone function must be part of a class, in F# every standalone function *must* be part of a module.

### Accessing functions across module boundaries

If you want to access a function in another module, you can refer to it by its qualified name.

```fsharp
module MathStuff = 

    let add x y  = x + y
    let subtract x y  = x - y

module OtherStuff = 

    // use a function from the MathStuff module
    let add1 x = MathStuff.add x 1  
```

You can also import all the functions in another module with the `open` directive, 
after which you can use the short name, rather than having to specify the qualified name.

```fsharp
module OtherStuff = 
    open MathStuff  // make all functions accessible

    let add1 x = add x 1
```

The rules for using qualified names are exactly as you would expect. That is, you can always use a fully qualified name to access a function, 
and you can use relative names or unqualified names based on what other modules are in scope.

### Nested modules

Just like static classes, modules can contain child modules nested within them, as shown below:

```fsharp
module MathStuff = 

    let add x y  = x + y
    let subtract x y  = x - y

    // nested module    
    module FloatLib = 

        let add x y :float = x + y
        let subtract x y :float  = x - y
```
        
And other modules can reference functions in the nested modules using either a full name or a relative name as appropriate:

```fsharp
module OtherStuff = 
    open MathStuff

    let add1 x = add x 1

    // fully qualified
    let add1Float x = MathStuff.FloatLib.add x 1.0
    
    //with a relative path
    let sub1Float x = FloatLib.subtract x 1.0
```

### Top level modules 

So if there can be nested child modules, that implies that, going back up the chain, there must always be some *top-level* parent module.  This is indeed true.

Top level modules are defined slightly differently than the modules we have seen so far. 

* The `module MyModuleName` line *must* be the first declaration in the file 
* There is no `=` sign
* The contents of the module are *not* indented

In general, there must be a top level module declaration present in every `.FS` source file. There some exceptions, but it is good practice anyway.
The module name does not have to be the same as the name of the file, but two files cannot share the same module name.

For `.FSX` script files, the module declaration is not needed, in which case the module name is automatically set to the filename of the script.

Here is an example of `MathStuff` declared as a top level module:

```fsharp
// top level module
module MathStuff

let add x y  = x + y
let subtract x y  = x - y

// nested module    
module FloatLib = 

    let add x y :float = x + y
    let subtract x y :float  = x - y
```

Note the lack of indentation for the top level code (the contents of `module MathStuff`), but that the content of a nested module like `FloatLib` does still need to be indented.

### Other module content

A module can contain other declarations as well as functions, including type declarations, simple values and initialization code (like static constructors)

```fsharp
module MathStuff = 

    // functions
    let add x y  = x + y
    let subtract x y  = x - y

    // type definitions
    type Complex = {r:float; i:float}
    type IntegerFunction = int -> int -> int
    type DegreesOrRadians = Deg | Rad

    // "constant"
    let PI = 3.141

    // "variable"
    let mutable TrigType = Deg

    // initialization / static constructor
    do printfn "module initialized"

```

<div class="alert alert-info">By the way, if you are playing with these examples in the interactive window, you might want to right-click and do "Reset Session" every so often, so that the code is fresh and doesn't get contaminated with previous evaluations</div>

### Shadowing

Here's our example module again. Notice that `MathStuff` has an `add` function and `FloatLib` *also* has an `add` function.

```fsharp
module MathStuff = 

    let add x y  = x + y
    let subtract x y  = x - y

    // nested module    
    module FloatLib = 

        let add x y :float = x + y
        let subtract x y :float  = x - y
```

Now what happens if I bring *both* of them into scope, and then use `add`?

```fsharp
open  MathStuff
open  MathStuff.FloatLib

let result = add 1 2  // Compiler error: This expression was expected to 
                      // have type float but here has type int    
```

What happened was that the `MathStuff.FloatLib` module has masked or overridden the original `MathStuff` module, which has been "shadowed" by `FloatLib`.

As a result you now get a [FS0001 compiler error](/troubleshooting-fsharp/#FS0001) because the first parameter `1` is expected to be a float. You would have to change `1` to `1.0` to fix this.

Unfortunately, this is invisible and easy to overlook. Sometimes you can do cool tricks with this, almost like subclassing, but more often, it can be annoying if you have functions with the same name (such as the very common `map`).

If you don't want this to happen, there is a way to stop it by using the `RequireQualifiedAccess` attribute. Here's the same example where both modules are decorated with it.

```fsharp
[<RequireQualifiedAccess>]
module MathStuff = 

    let add x y  = x + y
    let subtract x y  = x - y

    // nested module    
    [<RequireQualifiedAccess>]    
    module FloatLib = 

        let add x y :float = x + y
        let subtract x y :float  = x - y
```

Now the `open` isn't allowed:
        
```fsharp
open  MathStuff   // error
open  MathStuff.FloatLib // error
```

But we can still access the functions (without any ambiguity) via their qualified name:

```fsharp
let result = MathStuff.add 1 2  
let result = MathStuff.FloatLib.add 1.0 2.0
```


### Access Control

F# supports the use of standard .NET access control keywords such as `public`, `private`, and `internal`.
The [MSDN documentation](http://msdn.microsoft.com/en-us/library/dd233188) has the complete details.

* These access specifiers can be put on the top-level ("let bound") functions, values, types and other declarations in a module. They can also be specified for the modules themselves (you might want a private nested module, for example).
* Everything is public by default (with a few exceptions) so you will need to use `private` or `internal` if you want to protect them.

These access specifiers are just one way of doing access control in F#. Another completely different way is to use module "signature" files, which are a bit like C header files. They describe the content of the module in an abstract way. Signatures are very useful for doing serious encapsulation, but that discussion will have to wait for the planned series on encapsulation and capability based security.


## Namespaces

Namespaces in F# are similar to namespaces in C#.  They can be used to organize modules and types to avoid name collisions.

A namespace is declared with a `namespace` keyword, as shown below.

```fsharp
namespace Utilities

module MathStuff = 

    // functions
    let add x y  = x + y
    let subtract x y  = x - y
```

Because of this namespace, the fully qualified name of the `MathStuff` module now becomes `Utilities.MathStuff` and
the fully qualified name of the `add` function now becomes `Utilities.MathStuff.add`.

With the namespace, the indentation rules apply, so that the module defined above must have its content indented, as it it were a nested module.

You can also declare a namespace implicitly by adding dots to the module name. That is, the code above could also be written as:

```fsharp
module Utilities.MathStuff  

// functions
let add x y  = x + y
let subtract x y  = x - y
```

The fully qualified name of the `MathStuff` module is still `Utilities.MathStuff`, but
in this case, the module is a top-level module and the contents do not need to be indented.

Some additional things to be aware of when using namespaces:

* Namespaces are optional for modules. And unlike C#, there is no default namespace for an F# project, so a top level module without a namespace will be at the global level.
If you are planning to create reusable libraries, be sure to add some sort of namespace to avoid naming collisions with code in other libraries. 
* Namespaces can directly contain type declarations, but not function declarations. As noted earlier, all function and value declarations must be part of a module.
* Finally, be aware that namespaces don't work well in scripts.  For example, if you try to to send a namespace declaration such as `namespace Utilities` below to the interactive window, you will get an error.


### Namespace hierarchies

You can create a namespace hierarchy by simply separating the names with periods:

```fsharp
namespace Core.Utilities

module MathStuff = 
    let add x y  = x + y
```

And if you want to put *two* namespaces in the same file, you can. Note that all namespaces *must* be fully qualified -- there is no nesting.

```fsharp
namespace Core.Utilities

module MathStuff = 
    let add x y  = x + y
    
namespace Core.Extra

module MoreMathStuff = 
    let add x y  = x + y
```

One thing you can't do is have a naming collision between a namespace and a module.


```fsharp
namespace Core.Utilities

module MathStuff = 
    let add x y  = x + y
    
namespace Core

// fully qualified name of module
// is Core.Utilities  
// Collision with namespace above!
module Utilities = 
    let add x y  = x + y
```


## Mixing types and functions in modules ##

We've seen that a module typically consists of a set of related functions that act on a data type.  

In an object oriented program, the data structure and the functions that act on it would be combined in a class.
However in functional-style F#, a data structure and the functions that act on it are combined in a module instead.

There are two common patterns for mixing types and functions together:

* having the type declared separately from the functions 
* having the type declared in the same module as the functions 

In the first approach, the type is declared *outside* any module (but in a namespace) and then the functions that work on the type
are put in a module with a similar name.

```fsharp
// top-level module
namespace Example

// declare the type outside the module
type PersonType = {First:string; Last:string}

// declare a module for functions that work on the type
module Person = 

    // constructor
    let create first last = 
        {First=first; Last=last}

    // method that works on the type
    let fullName {First=first; Last=last} = 
        first + " " + last

// test
let person = Person.create "john" "doe" 
Person.fullName person |> printfn "Fullname=%s"
```

In the alternative approach, the type is declared *inside* the module and given a simple name such as "`T`" or the name of the module. 
So the functions are accessed with names like `MyModule.Func1` and `MyModule.Func2` while the type itself is
accessed with a name like `MyModule.T`. Here's an example:

```fsharp
module Customer = 

    // Customer.T is the primary type for this module
    type T = {AccountId:int; Name:string}

    // constructor
    let create id name = 
        {T.AccountId=id; T.Name=name}

    // method that works on the type
    let isValid {T.AccountId=id; } = 
        id > 0

// test
let customer = Customer.create 42 "bob" 
Customer.isValid customer |> printfn "Is valid?=%b"
```

Note that in both cases, you should have a constructor function that creates new instances of the type (a factory method, if you will),
Doing this means that you will rarely have to explicitly name the type in your client code, and therefore, you should not care whether it lives in the module or not!

So which approach should you choose?

* The former approach is more .NET like, and much better if you want to share your libraries with other non-F# code, as the exported class names are what you would expect.
* The latter approach is more common for those used to other functional languages. The type inside a module compiles into nested classes, which is not so nice for interop.  

For yourself, you might want to experiment with both. And in a team programming situation, you should choose one style and be consistent. 


### Modules containing types only

If you have a set of types that you need to declare without any associated functions, don't bother to use a module. You can declare types directly in a namespace and avoid nested classes.

For example, here is how you might think to do it:

```fsharp
// top-level module
module Example

// declare the type inside a module
type PersonType = {First:string; Last:string}

// no functions in the module, just types...
```

And here is a alternative way to do it. The `module` keyword has simply been replaced with `namespace`.

```fsharp
// use a namespace 
namespace Example

// declare the type outside any module
type PersonType = {First:string; Last:string}
```

In both cases, `PersonType` will have the same fully qualified name.

Note that this only works with types. Functions must always live in a module.
