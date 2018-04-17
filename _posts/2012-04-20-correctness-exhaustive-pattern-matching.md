---
layout: post
title: "Exhaustive pattern matching"
description: "A powerful technique to ensure correctness"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 20
categories: [Correctness, Patterns]
---

We briefly noted earlier that when pattern matching there is a requirement to match all possible cases.  This turns out be a very powerful technique to ensure correctness.

Let's compare some C# to F# again. Here's some C# code that uses a switch statement to handle different types of state.

```csharp
enum State { New, Draft, Published, Inactive, Discontinued }
void HandleState(State state)
{
    switch (state)
    {
    case State.Inactive: 
        ... 
    case State.Draft:  
        ... 
    case State.New: 
        ... 
    case State.Discontinued:  
        ... 
    } 
}
```

This code will compile, but there is an obvious bug! The compiler couldn't see it -- can you? If you can, and you fixed it, would it stay fixed if I added another `State` to the list?

Here's the F# equivalent:

```fsharp
type State = New | Draft | Published | Inactive | Discontinued
let handleState state = 
   match state with
   | Inactive -> 
      ... 
   | Draft -> 
      ... 
   | New -> 
      ... 
   | Discontinued -> 
      ... 
```
   
Now try running this code. What does the compiler tell you? It will say something like:

```
Incomplete pattern matches on this expression. 
For example, the value 'Published' may indicate a case not covered by the pattern(s)
```

The fact that exhaustive matching is always done means that certain common errors will be detected by the compiler immediately:

* A missing case (often caused when a new choice has been added due to changed requirements or refactoring).
* An impossible case (when an existing choice has been removed).
* A redundant case that could never be reached (the case has been subsumed in a previous case -- this can sometimes be non-obvious). 

Now let's look at some real examples of how exhaustive matching can help you write correct code.

## Avoiding nulls with the Option type ##

We'll start with an extremely common scenario where the caller should always check for an invalid case, namely testing for nulls. A typical C# program is littered with code like this:

```csharp
if (myObject != null)
{
  // do something 
}
```

Unfortunately, this test is not required by the compiler. All it takes is for one piece of code to forget to do this, and the program can crash.
Over the years, a huge amount of programming effort has been devoted to handling nulls -- the invention of nulls has even been called a [billion dollar mistake](http://www.infoq.com/presentations/Null-References-The-Billion-Dollar-Mistake-Tony-Hoare)! 

In pure F#, nulls cannot exist accidentally. A string or object must always be assigned to something at creation, and is immutable thereafter. 

However, there are many situations where the *design intent* is to distinguish between valid and invalid values,
and you require the caller to handle both cases. 

In C#, this can be managed in certain situations by using nullable value types (such as `Nullable<int>`) to make the design decision clear.
When a nullable is encountered the compiler will force you to be aware of it. You can then test the validity of the value before using it.
But nullables do not work for standard classes (i.e. reference types), and it is easy to accidentally bypass the tests too and just call `Value` directly.

In F# there is a similar but more powerful concept to convey the design intent: the generic wrapper type called `Option`, with two choices: `Some` or `None`.
The `Some` choice wraps a valid value, and `None` represents a missing value.

Here's an example where `Some` is returned if a file exists, but a missing file returns `None`.

```fsharp
let getFileInfo filePath =
   let fi = new System.IO.FileInfo(filePath)
   if fi.Exists then Some(fi) else None

let goodFileName = "good.txt"
let badFileName = "bad.txt"

let goodFileInfo = getFileInfo goodFileName // Some(fileinfo)
let badFileInfo = getFileInfo badFileName   // None
```

If we want to do anything with these values, we must always handle both possible cases.

```fsharp
match goodFileInfo with
  | Some fileInfo -> 
      printfn "the file %s exists" fileInfo.FullName
  | None -> 
      printfn "the file doesn't exist" 

match badFileInfo with
  | Some fileInfo -> 
      printfn "the file %s exists" fileInfo.FullName
  | None -> 
      printfn "the file doesn't exist" 
```
	  
We have no choice about this. Not handling a case is a compile-time error, not a run-time error.
By avoiding nulls and by using `Option` types in this way, F# completely eliminates a large class of null reference exceptions.
 
<sub>Caveat: F# does allow you to access the value without testing, just like C#, but that is considered extremely bad practice.</sub> 


## Exhaustive pattern matching for edge cases ##

Here's some C# code that creates a list by averaging pairs of numbers from an input list:

```csharp
public IList<float> MovingAverages(IList<int> list)
{
    var averages = new List<float>();
    for (int i = 0; i < list.Count; i++)
    {
        var avg = (list[i] + list[i+1]) / 2;
        averages.Add(avg);
    }
    return averages;
}
```

It compiles correctly, but it actually has a couple of issues. Can you find them quickly? If you're lucky, your unit tests will find them for you, assuming you have thought of all the edge cases.

Now let's try the same thing in F#:

```fsharp
let rec movingAverages list = 
    match list with
    // if input is empty, return an empty list
    | [] -> []
    // otherwise process pairs of items from the input 
    | x::y::rest -> 
        let avg = (x+y)/2.0 
        //build the result by recursing the rest of the list
        avg :: movingAverages (y::rest)
```

This code also has a bug. But unlike C#, this code will not even compile until I fix it.  The compiler will tell me that I haven't handled the case when I have a single item in my list.
Not only has it found a bug, it has revealed a gap in the requirements: what should happen when there is only one item?

Here's the fixed up version:

```fsharp
let rec movingAverages list = 
    match list with
    // if input is empty, return an empty list
    | [] -> []
    // otherwise process pairs of items from the input 
    | x::y::rest -> 
        let avg = (x+y)/2.0 
        //build the result by recursing the rest of the list
        avg :: movingAverages (y::rest)
    // for one item, return an empty list
    | [_] -> []

// test
movingAverages [1.0]
movingAverages [1.0; 2.0]
movingAverages [1.0; 2.0; 3.0]
```

As an additional benefit, the F# code is also much more self-documenting. It explicitly describes the consequences of each case.
In the C# code, it is not at all obvious what happens if a list is empty or only has one item.  You would have to read the code carefully to find out.

## Exhaustive pattern matching as an error handling technique ##

The fact that all choices must be matched can also be used as a useful alternative to throwing exceptions. For example consider the following common scenario:

* There is a utility function in the lowest tier of your app that opens a file and performs an arbitrary operation on it (that you pass in as a callback function)
* The result is then passed back up through to tiers to the top level.
* A client calls the top level code, and the result is processed and any error handling done. 

In a procedural or OO language, propagating and handling exceptions across layers of code is a common problem. Top level functions are not easily able to tell the difference between an exception that they should recover from (`FileNotFound` say) vs. an exception that they needn't handle (`OutOfMemory` say). In Java, there has been an attempt to do this with checked exceptions, but with mixed results.

In the functional world, a common technique is to create a new structure to hold both the good and bad possibilities, rather than throwing an exception if the file is missing.

```fsharp
// define a "union" of two different alternatives
type Result<'a, 'b> = 
    | Success of 'a  // 'a means generic type. The actual type
                     // will be determined when it is used.
    | Failure of 'b  // generic failure type as well

// define all possible errors
type FileErrorReason = 
    | FileNotFound of string
    | UnauthorizedAccess of string * System.Exception

// define a low level function in the bottom layer
let performActionOnFile action filePath =
   try
      //open file, do the action and return the result
      use sr = new System.IO.StreamReader(filePath:string)
      let result = action sr  //do the action to the reader
      sr.Close()
      Success (result)        // return a Success
   with      // catch some exceptions and convert them to errors
      | :? System.IO.FileNotFoundException as ex 
          -> Failure (FileNotFound filePath)      
      | :? System.Security.SecurityException as ex 
          -> Failure (UnauthorizedAccess (filePath,ex))  
      // other exceptions are unhandled
```
      
The code demonstrates how `performActionOnFile` returns a `Result` object which has two alternatives: `Success` and `Failure`.  The `Failure` alternative in turn has two alternatives as well: `FileNotFound` and `UnauthorizedAccess`.

Now the intermediate layers can call each other, passing around the result type without worrying what its structure is, as long as they don't access it:

```fsharp
// a function in the middle layer
let middleLayerDo action filePath = 
    let fileResult = performActionOnFile action filePath
    // do some stuff
    fileResult //return

// a function in the top layer
let topLayerDo action filePath = 
    let fileResult = middleLayerDo action filePath
    // do some stuff
    fileResult //return
```
	
Because of type inference, the middle and top layers do not need to specify the exact types returned. If the lower layer changes the type definition at all, the intermediate layers will not be affected.

Obviously at some point, a client of the top layer does want to access the result. And here is where the requirement to match all patterns is enforced. The client must handle the case with a `Failure` or else the compiler will complain. And furthermore, when handling the `Failure` branch, it must handle the possible reasons as well. In other words, special case handling of this sort can be enforced at compile time, not at runtime!   And in addition the possible reasons are explicitly documented by examining the reason type. 

Here is an example of a client function that accesses the top layer:

```fsharp
/// get the first line of the file
let printFirstLineOfFile filePath = 
    let fileResult = topLayerDo (fun fs->fs.ReadLine()) filePath

    match fileResult with
    | Success result -> 
        // note type-safe string printing with %s
        printfn "first line is: '%s'" result   
    | Failure reason -> 
       match reason with  // must match EVERY reason
       | FileNotFound file -> 
           printfn "File not found: %s" file
       | UnauthorizedAccess (file,_) -> 
           printfn "You do not have access to the file: %s" file
```

		   
You can see that this code must explicitly handle the `Success` and `Failure` cases, and then for the failure case, it explicitly handles the different reasons. If you want to see what happens if it does not handle one of the cases, try commenting out the line that handles `UnauthorizedAccess` and see what the compiler says.

Now it is not required that you always handle all possible cases explicitly. In the example below, the function uses the underscore wildcard to treat all the failure reasons as one. This can be considered bad practice if we want to get the benefits of the strictness, but at least it is clearly done.

```fsharp
/// get the length of the text in the file
let printLengthOfFile filePath = 
   let fileResult = 
     topLayerDo (fun fs->fs.ReadToEnd().Length) filePath

   match fileResult with
   | Success result -> 
      // note type-safe int printing with %i
      printfn "length is: %i" result       
   | Failure _ -> 
      printfn "An error happened but I don't want to be specific"
```

Now let's see all this code work in practice with some interactive tests. 

First set up a good file and a bad file.

```fsharp
/// write some text to a file
let writeSomeText filePath someText = 
    use writer = new System.IO.StreamWriter(filePath:string)
    writer.WriteLine(someText:string)
    writer.Close()

let goodFileName = "good.txt"
let badFileName = "bad.txt"

writeSomeText goodFileName "hello"
```

And now test interactively:

```fsharp
printFirstLineOfFile goodFileName 
printLengthOfFile goodFileName 

printFirstLineOfFile badFileName 
printLengthOfFile badFileName 
```

I think you can see that this approach is very attractive:

* Functions return error types for each expected case (such as `FileNotFound`), but the handling of these types does not need to make the calling code ugly.
* Functions continue to throw exceptions for unexpected cases (such as `OutOfMemory`), which will generally be caught and logged at the top level of the program.

This technique is simple and convenient. Similar (and more generic) approaches are standard in functional programming.

It is feasible to use this approach in C# too, but it is normally impractical, due to the lack of union types and the lack of type inference (we would have to specify generic types everywhere). 

## Exhaustive pattern matching as a change management tool ##

Finally, exhaustive pattern matching is a valuable tool for ensuring that code stays correct as requirements change, or during refactoring.

Let's say that the requirements change and we need to handle a third type of error: "Indeterminate". To implement this new requirement, change the first `Result` type as follows, and re-evaluate all the code. What happens?

```fsharp
type Result<'a, 'b> = 
    | Success of 'a 
    | Failure of 'b
    | Indeterminate
```

Or sometimes a requirements change will remove a possible choice. To emulate this, change the first `Result` type to eliminate all but one of the choices. 

```fsharp
type Result<'a> = 
    | Success of 'a 
```

Now re-evaluate the rest of the code. What happens now?

This is very powerful!  When we adjust the choices, we immediately know all the places which need to be fixed to handle the change. This is another example of the power of statically checked type errors. It is often said about functional languages like F# that "if it compiles, it must be correct".



