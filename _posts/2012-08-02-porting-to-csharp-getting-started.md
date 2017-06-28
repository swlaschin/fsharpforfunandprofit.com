---
layout: post
title: "Getting started with direct porting"
description: "F# equivalents to C#"
nav: fsharp-types
seriesId: "Porting from C#"
seriesOrder: 2
---

Before we get started on the detailed examples, we'll go back to basics and do some simple porting of trivial examples.

In this post and the next, we'll look at the nearest F# equivalents to common C# statements and keywords, to guide you when doing direct ports.

## Basic syntax conversion guidelines ##

Before starting a port, you need to understand how F# syntax is different from C# syntax. This section presents some general guidelines for converting from one to another. (For a quick overview of F# syntax as a whole, see ["F# syntax in 60 seconds"](/posts/fsharp-in-60-seconds/))

### Curly braces and indentation ###

C# uses curly braces to indicate the start and end of a block of code. F# generally just uses indentation.  

Curly braces are used in F#, but not for blocks of code. Instead, you will see them used:

* 	For definitions and usage of "record" types. 
* 	In conjunction with computation expressions, such as `seq` and `async`. In general, you will not be using these expressions for basic ports anyway.

For details on the indentation rules, [see this post](/posts/fsharp-syntax).

### Semicolons

Unlike C#'s semicolon, F# does not require any kind of line or statement terminator.

### Commas

F# does not use commas for separating parameters or list elements, so remember not to use commas when porting!

*For separating list elements, use semicolons rather than commas.*

{% highlight csharp %}
// C# example
var list = new int[] { 1,2,3}
{% endhighlight %}

{% highlight fsharp %}
// F# example
let list = [1;2;3] // semicolons
{% endhighlight %}

*For separating parameters for native F# functions, use white space.*

{% highlight csharp %}
// C# example 
int myFunc(int x, int y, int z) { ... function body ...}
{% endhighlight %}

{% highlight fsharp %}
// F# example 
let myFunc (x:int) (y:int) (z:int) :int = ... function body ...
let myFunc x y z = ... function body ...
{% endhighlight %}

Commas are generally only used for tuples, or for separating parameters when calling .NET library functions. (See [this post](/posts/defining-functions/#tuples) for more on tuples vs multiple parameters)

### Defining variables, functions and types

In F#, definitions of both variables and functions use the form:

{% highlight fsharp %}
let someName = // the definition
{% endhighlight %}

Definitions for all types (classes, structures, interfaces, etc.) use the form:

{% highlight fsharp %}
type someName = // the definition
{% endhighlight %}

The use of the `=` sign is an important difference between F# and C#. Where C# uses curly braces, F# uses the `=` and then the following block of code must be indented.

### Mutable values

In F#, values are immutable by default. If you are doing a direct imperative port, you probably need to make some of the values mutable, using the `mutable` keyword.
Then to assign to the values, use the `<-` operator, not the equals sign.

{% highlight csharp %}
// C# example 
var variableName = 42
variableName = variableName + 1
{% endhighlight %}

{% highlight fsharp %}
// F# example 
let mutable variableName = 42
variableName <- variableName + 1
{% endhighlight %}

### Assignment vs. testing for equality 

In C#, the equals sign is used for assignment, and the double equals `==` is used for testing equality. 

However in F#, the equals sign is used for testing equality, and is also used to initially bind values to other values when declared,

{% highlight fsharp %}
let mutable variableName = 42     // Bound to 42 on declaration
variableName <- variableName + 1  // Mutated (reassigned)
variableName = variableName + 1   // Comparison not assignment! 
{% endhighlight %}

To test for inequality, use SQL-style `<>` rather than `!=`

{% highlight fsharp %}
let variableName = 42             // Bound to 42 on declaration
variableName <> 43                // Comparison will return true.
variableName != 43                // Error FS0020.
{% endhighlight %}

If you accidentally use `!=` you will probably get an [error FS0020](/troubleshooting-fsharp/#FS0020).

## Conversion example #1

With these basic guidelines in place, let's look at some real code examples, and do a direct port for them.

This first example has some very simple code, which we will port line by line. Here's the C# code.

{% highlight csharp %}
using System;
using System.Collections.Generic;

namespace PortingToFsharp
{
    public class Squarer
    {
        public int Square(int input)
        {
            var result = input * input;
            return result;
        }

        public void PrintSquare(int input)
        {
            var result = this.Square(input);
            Console.WriteLine("Input={0}. Result={1}", 
              input, result);
        }
    }
{% endhighlight %}
    
### Converting "using" and "namespace"

These keywords are straightforward:

* 	`using` becomes `open`
* 	`namespace` with curly braces becomes just `namespace`. 

Unlike C#, F# files do not generally declare namespaces unless they need to interop with other .NET code. The filename itself acts as a default namespace.

Note that the namespace, if used, must come before anything else, such as "open".  This the opposite order from most C# code.

### Converting the class

To declare a simple class, use:

{% highlight fsharp %}
type myClassName() = 
   ... code ...  
{% endhighlight %}

Note that there are parentheses after the class name. These are required for class definitions.

More complicated class definitions will be shown in the next example, and you read the [complete discussion of classes](/posts/classes/).

### Converting function/method signatures

For function/method signatures:

* Parentheses are not needed around the parameter list
* Whitespace is used to separate the parameters, not commas
* Rather than curly braces, an equals sign signals the start of the function body
* The parameters don't normally need types but if you do need them:
  *	The type name comes after the value or parameter
  *	The parameter name and type are separated by colons 
  *	When specifying types for parameters, you should probably wrap the pair in parentheses to avoid unexpected behavior.
  *	The return type for the function as a whole is prefixed by a colon, and comes after all the other parameters

Here's a C# function signature:

{% highlight csharp %}
int Square(int input) { ... code ...}
{% endhighlight %}

and here's the corresponding F# function signature with explicit types:

{% highlight fsharp %}
let Square (input:int) :int =  ... code ...
{% endhighlight %}

However, because F# can normally infer the parameter and return types, you rarely need to specify them explicitly.

Here's a more typical F# signature, with inferred types:

{% highlight fsharp %}
let Square input =  ... code ...
{% endhighlight %}

### void

The `void` keyword in C# is generally not needed, but if required, would be converted to `unit`

So the C# code:

{% highlight csharp %}
void PrintSquare(int input) { ... code ...}
{% endhighlight %}

could be converted to the F# code:

{% highlight fsharp %}
let PrintSquare (input:int) :unit =  ... code ...
{% endhighlight %}

but again, the specific types are rarely needed, and so the F# version is just:

{% highlight fsharp %}
let PrintSquare input =  ... code ...
{% endhighlight %}

### Converting function/method bodies

In a function body, you are likely to have a combination of:

* 	Variable declarations and assignments
* 	Function calls
* 	Control flow statements
* 	Return values

We'll have a quick look at porting each of these in turn, except for control flow, which we'll discuss later.

### Converting variable declarations

Almost always, you can use `let` on its own, just like `var` in C#:

{% highlight csharp %}
// C# variable declaration
var result = input * input;
{% endhighlight %}

{% highlight fsharp %}
// F# value declaration
let result = input * input
{% endhighlight %}

Unlike C#, you must always assign ("bind") something to an F# value as part of its declaration.

{% highlight csharp %}
// C# example 
int unassignedVariable; //valid
{% endhighlight %}

{% highlight fsharp %}
// F# example 
let unassignedVariable // not valid
{% endhighlight %}

As noted above, if you need to change the value after its declaration, you must use the "mutable" keyword.

If you need to specify a type for a value, the type name comes after the value or parameter, preceded by a colon.

{% highlight csharp %}
// C# example 
int variableName = 42;
{% endhighlight %}

{% highlight fsharp %}
// F# example 
let variableName:int = 42
{% endhighlight %}

### Converting function calls

When calling a native F# function, there is no need for parentheses or commas. In other words, the same rules apply for calling a function as when defining it. 

Here's C# code for defining a function, then calling it:

{% highlight csharp %}
// define a method/function 
int Square(int input) { ... code  ...}

// call it
var result = Square(input);
{% endhighlight %}

However, because F# can normally infer the parameter and return types, you rarely need to specify them explicitly
So here's typical F# code for defining a function and then calling it:

{% highlight fsharp %}
// define a function 
let Square input = ... code ...

// call it
let result = Square input
{% endhighlight %}

### Return values

In C#, you use the `return` keyword. But in F#, the last value in the block is automatically the "return" value. 

Here's the C# code returning the `result` variable.

{% highlight csharp %}
public int Square(int input)
{
    var result = input * input;
    return result;   //explicit "return" keyword
}
{% endhighlight %}

And here's the F# equivalent.

{% highlight fsharp %}
let Square input = 
    let result = input * input
    result        // implicit "return" value
{% endhighlight %}

This is because F# is expression-based. Everything is an expression, and the value of a block expression as a whole is just the value of the last expression in the block. 

For more details on expression-oriented code, see ["expressions vs statements"](/posts/expressions-vs-statements/).

### Printing to the console

To print output in C#, you generally use `Console.WriteLine` or similar. In F#, you generally use `printf` or similar, which is typesafe.  ([More details on using "printf" family](/posts/printf)).

### The complete port of example #1

Putting it all together, here is the complete direct port of example #1 to F#.

The C# code again:
{% highlight csharp %}
using System;
using System.Collections.Generic;

namespace PortingToFsharp
{
    public class Squarer
    {
        public int Square(int input)
        {
            var result = input * input;
            return result;
        }

        public void PrintSquare(int input)
        {
            var result = this.Square(input);
            Console.WriteLine("Input={0}. Result={1}", 
              input, result);
        }
    }
{% endhighlight %}

And the equivalent F# code:

{% highlight fsharp %}
namespace PortingToFsharp

open System
open System.Collections.Generic

type Squarer() =  

    let Square input = 
        let result = input * input
        result

    let PrintSquare input = 
        let result = Square input
        printf "Input=%i. Result=%i" input result
{% endhighlight %}        
    

