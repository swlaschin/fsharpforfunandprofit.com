---
layout: post
title: "Designing with types: Constrained strings"
description: "Adding more semantic information to a primitive type"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 6
categories: [Types, DDD]
---

In a [previous post](/posts/designing-with-types-single-case-dus/), I talked about avoiding using plain primitive strings for email addresses, zip codes, states, etc.
By wrapping them in a single case union, we could (a) force the types to be distinct and (b) add validation rules.

In this post, we'll look at whether we can extend that concept to an even more fine grained level.

## When is a string not a string?

Let's look a simple `PersonalName` type.

```fsharp
type PersonalName = 
    {
    FirstName: string;
    LastName: string;
    }
```

The type says that the first name is a `string`. But really, is that all it is?  Are there any other constraints that we might need to add to it?

Well, OK, it must not be null. But that is assumed in F#.

What about the length of the string? Is it acceptable to have a name which is 64K characters long? If not, then is there some maximum length allowed?

And can a name contain linefeed characters or tabs?  Can it start or end with whitespace?

Once you put it this way, there are quite a lot of constraints even for a "generic" string. Here are some of the obvious ones:

* What is its maximum length?
* Can it cross over multiple lines?
* Can it have leading or trailing whitespace?
* Can it contain non-printing characters?

## Should these constraints be part of the domain model?

So we might acknowledge that some constraints exist, but should they really be part of the domain model (and the corresponding types derived from it)? 
For example, the constraint that a last name is limited to 100 characters -- surely that is specific to a particular implementation and not part of the domain at all.

I would answer that there is a difference between a logical model and a physical model.  In a logical model some of these constraints might not be relevant, but in a physical model they most certainly are. And when we are writing code, we are always dealing with a physical model anyway.

Another reason for incorporating the constraints into the model is that often the model is shared across many separate applications. For example, a personal name may be created in a e-commerce application, which writes it into a database table and then puts it on a message queue to be picked up by a CRM application, which in turn calls an email templating service, and so on.

It is important that all these applications and services have the *same* idea of what a personal name is, including the length and other constraints. If the model does not make the constraints explicit, then it is easy to have a mismatch when moving across service boundaries.

For example, have you ever written code that checks the length of a string before writing it to a database? 

```csharp
void SaveToDatabase(PersonalName personalName)
{ 
   var first = personalName.First;
   if (first.Length > 50)
   {    
        // ensure string is not too long
        first = first.Substring(0,50);
   }
   
   //save to database
}
```

If the string *is* too long at this point, what should you do? Silently truncate it? Throw an exception?

A better answer is to avoid the problem altogether if you can. By the time the string gets to the database layer it is too late -- the database layer should not be making these kinds of decisions.

The problem should be dealt with when the string was *first created*, not when it is *used*.  In other words, it should have been part of the validation of the string.

But how can we trust that the validation has been done correctly for all possible paths?  I think you can guess the answer...

## Modeling constrained strings with types

The answer, of course, is to create wrapper types which have the constraints built into the type.  

So let's knock up a quick prototype using the single case union technique we used [before](/posts/designing-with-types-single-case-dus/).

```fsharp
module String100 = 
    type T = String100 of string
    let create (s:string) = 
        if s <> null && s.Length <= 100 
        then Some (String100 s) 
        else None
    let apply f (String100 s) = f s
    let value s = apply id s

module String50 = 
    type T = String50 of string
    let create (s:string) = 
        if s <> null && s.Length <= 50 
        then Some (String50 s) 
        else None
    let apply f (String50 s) = f s
    let value s = apply id s

module String2 = 
    type T = String2 of string
    let create (s:string) = 
        if s <> null && s.Length <= 2 
        then Some (String2 s) 
        else None
    let apply f (String2 s) = f s
    let value s = apply id s
```

Note that we immediately have to deal with the case when the validation fails by using an option type as the result.  It makes creation more painful, but we can't avoid it if we want the benefits later.

For example, here is a good string and a bad string of length 2.

```fsharp
let s2good = String2.create "CA"
let s2bad = String2.create "California"

match s2bad with
| Some s2 -> // update domain object
| None -> // handle error
```

In order to use the `String2` value we are forced to check whether it is `Some` or `None` at the time of creation.

### Problems with this design 

One problem is that we have a lot of duplicated code. In practice a typical domain only has a few dozen string types, so there won't be that much wasted code. But still, we can probably do better.

Another more serious problem is that comparisons become harder. A `String50` is a different type from a `String100` so that they cannot be compared directly. 

```fsharp
let s50 = String50.create "John"
let s100 = String100.create "Smith"

let s50' = s50.Value
let s100' = s100.Value

let areEqual = (s50' = s100')  // compiler error
```

This kind of thing will make working with dictionaries and lists harder.

{% include book_page_pdf.inc %}

### Refactoring

At this point we can exploit F#'s support for interfaces, and create a common interface that all wrapped strings have to support, and also some standard functions:

```fsharp
module WrappedString = 

    /// An interface that all wrapped strings support
    type IWrappedString = 
        abstract Value : string

    /// Create a wrapped value option
    /// 1) canonicalize the input first
    /// 2) If the validation succeeds, return Some of the given constructor
    /// 3) If the validation fails, return None
    /// Null values are never valid.
    let create canonicalize isValid ctor (s:string) = 
        if s = null 
        then None
        else
            let s' = canonicalize s
            if isValid s'
            then Some (ctor s') 
            else None

    /// Apply the given function to the wrapped value
    let apply f (s:IWrappedString) = 
        s.Value |> f 

    /// Get the wrapped value
    let value s = apply id s

    /// Equality test
    let equals left right = 
        (value left) = (value right)
        
    /// Comparison
    let compareTo left right = 
        (value left).CompareTo (value right)
```

The key function is `create`, which takes a constructor function and creates new values using it only when the validation passes.

With this in place it is a lot easier to define new types:

```fsharp
module WrappedString = 

    // ... code from above ...

    /// Canonicalizes a string before construction
    /// * converts all whitespace to a space char
    /// * trims both ends
    let singleLineTrimmed s =
        System.Text.RegularExpressions.Regex.Replace(s,"\s"," ").Trim()

    /// A validation function based on length
    let lengthValidator len (s:string) =
        s.Length <= len 

    /// A string of length 100
    type String100 = String100 of string with
        interface IWrappedString with
            member this.Value = let (String100 s) = this in s

    /// A constructor for strings of length 100
    let string100 = create singleLineTrimmed (lengthValidator 100) String100 

    /// Converts a wrapped string to a string of length 100
    let convertTo100 s = apply string100 s

    /// A string of length 50
    type String50 = String50 of string with
        interface IWrappedString with
            member this.Value = let (String50 s) = this in s

    /// A constructor for strings of length 50
    let string50 = create singleLineTrimmed (lengthValidator 50)  String50

    /// Converts a wrapped string to a string of length 50
    let convertTo50 s = apply string50 s
```

For each type of string now, we just have to:

* create a type (e.g. `String100`) 
* an implementation of `IWrappedString` for that type
* and a public constructor (e.g. `string100`) for that type.  

(In the sample above I have also thrown in a useful `convertTo` to convert from one type to another.)

The type is a simple wrapped type as we have seen before.

The implementation of the `Value` method of the IWrappedString could have been written using multiple lines, like this:

```fsharp
member this.Value = 
    let (String100 s) = this 
    s
```

But I chose to use a one liner shortcut:

```fsharp
member this.Value = let (String100 s) = this in s
```

The constructor function is also very simple. The canonicalize function is `singleLineTrimmed`, the validator function checks the length, and the constructor is the `String100` function (the function associated with the single case, not to be confused with the type of the same name). 

```fsharp
let string100 = create singleLineTrimmed (lengthValidator 100) String100
```

If you want to have other types with different constraints, you can easily add them. For example you might want to have a `Text1000` type that supports multiple lines and embedded tabs and is not trimmed.

```fsharp
module WrappedString = 

    // ... code from above ...

    /// A multiline text of length 1000
    type Text1000 = Text1000 of string with
        interface IWrappedString with
            member this.Value = let (Text1000 s) = this in s

    /// A constructor for multiline strings of length 1000
    let text1000 = create id (lengthValidator 1000) Text1000 
```

### Playing with the WrappedString module 

We can now play with the module interactively to see how it works:

```fsharp
let s50 = WrappedString.string50 "abc" |> Option.get
printfn "s50 is %A" s50
let bad = WrappedString.string50 null
printfn "bad is %A" bad
let s100 = WrappedString.string100 "abc" |> Option.get
printfn "s100 is %A" s100

// equality using module function is true
printfn "s50 is equal to s100 using module equals? %b" (WrappedString.equals s50 s100)

// equality using Object method is false
printfn "s50 is equal to s100 using Object.Equals? %b" (s50.Equals s100)

// direct equality does not compile
printfn "s50 is equal to s100? %b" (s50 = s100) // compiler error
```

When we need to interact with types such as maps that use raw strings, it is easy to compose new helper functions.

For example, here are some helpers to work with maps:

```fsharp
module WrappedString = 

    // ... code from above ...

    /// map helpers
    let mapAdd k v map = 
        Map.add (value k) v map    

    let mapContainsKey k map =  
        Map.containsKey (value k) map    

    let mapTryFind k map =  
        Map.tryFind (value k) map    
```

And here is how these helpers might be used in practice:

```fsharp
let abc = WrappedString.string50 "abc" |> Option.get
let def = WrappedString.string100 "def" |> Option.get
let map = 
    Map.empty
    |> WrappedString.mapAdd abc "value for abc"
    |> WrappedString.mapAdd def "value for def"

printfn "Found abc in map? %A" (WrappedString.mapTryFind abc map)

let xyz = WrappedString.string100 "xyz" |> Option.get
printfn "Found xyz in map? %A" (WrappedString.mapTryFind xyz map)
```

So overall, this "WrappedString" module allows us to create nicely typed strings without interfering too much. Now let's use it in a real situation.

## Using the new string types in the domain

Now we have our types, we can change the definition of the `PersonalName` type to use them.

```fsharp
module PersonalName = 
    open WrappedString

    type T = 
        {
        FirstName: String50;
        LastName: String100;
        }

    /// create a new value
    let create first last = 
        match (string50 first),(string100 last) with
        | Some f, Some l ->
            Some {
                FirstName = f;
                LastName = l;
                }
        | _ -> 
            None
```

We have created a module for the type and added a creation function that converts a pair of strings into a `PersonalName`. 

Note that we have to decide what to do if *either* of the input strings are invalid. Again, we cannot postpone the issue till later, we have to deal with it at construction time.

In this case we use the simple approach of creating an option type with None to indicate failure.

Here it is in use:

```fsharp
let name = PersonalName.create "John" "Smith"
```

We can also provide additional helper functions in the module. 

Let's say, for example, that we want to create a `fullname` function that will return the first and last names joined together.

Again, more decisions to make.  

* Should we return a raw string or a wrapped string?
  The advantage of the latter is that the callers know exactly how long the string will be, and it will be compatible with other similar types.

* If we do return a wrapped string (say a `String100`), then how do we handle the the case when the combined length is too long? (It could be up to 151 chars, based on the length of the first and last name types.). We could either return an option, or force a truncation if the combined length is too long.

Here's code that demonstrates all three options.

```fsharp
module PersonalName = 

    // ... code from above ...

    /// concat the first and last names together        
    /// and return a raw string
    let fullNameRaw personalName = 
        let f = personalName.FirstName |> value 
        let l = personalName.LastName |> value 
        f + " " + l 

    /// concat the first and last names together        
    /// and return None if too long
    let fullNameOption personalName = 
        personalName |> fullNameRaw |> string100

    /// concat the first and last names together        
    /// and truncate if too long
    let fullNameTruncated personalName = 
        // helper function
        let left n (s:string) = 
            if (s.Length > n) 
            then s.Substring(0,n)
            else s

        personalName 
        |> fullNameRaw  // concat
        |> left 100     // truncate
        |> string100    // wrap
        |> Option.get   // this will always be ok

```

Which particular approach you take to implementing `fullName` is up to you.  But it demonstrates a key point about this style of type-oriented design: these decisions have to be taken *up front*, when creating the code.  You cannot postpone them till later.

This can be very annoying at times, but overall I think it is a good thing.

## Revisiting the email address and zip code types

We can use this WrappedString module to reimplement the `EmailAddress` and `ZipCode` types.

```fsharp
module EmailAddress = 

    type T = EmailAddress of string with 
        interface WrappedString.IWrappedString with
            member this.Value = let (EmailAddress s) = this in s

    let create = 
        let canonicalize = WrappedString.singleLineTrimmed 
        let isValid s = 
            (WrappedString.lengthValidator 100 s) &&
            System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        WrappedString.create canonicalize isValid EmailAddress

    /// Converts any wrapped string to an EmailAddress
    let convert s = WrappedString.apply create s

module ZipCode = 

    type T = ZipCode of string with
        interface WrappedString.IWrappedString with
            member this.Value = let (ZipCode s) = this in s

    let create = 
        let canonicalize = WrappedString.singleLineTrimmed 
        let isValid s = 
            System.Text.RegularExpressions.Regex.IsMatch(s,@"^\d{5}$") 
        WrappedString.create canonicalize isValid ZipCode

    /// Converts any wrapped string to a ZipCode
    let convert s = WrappedString.apply create s
```

## Other uses of wrapped strings

This approach to wrapping strings can also be used for other scenarios where you don't want to mix string types together accidentally. 

One case that leaps to mind is ensuring safe quoting and unquoting of strings in web applications. 

For example, let's say that you want to output a string to HTML.  Should the string be escaped or not?  
If it is already escaped, you want to leave it alone but if it is not, you do want to escape it.

This can be a tricky problem. Joel Spolsky discusses using a naming convention [here](http://www.joelonsoftware.com/articles/Wrong.html), but of course, in F#, we want a type-based solution instead.

A type-based solution will probably use a type for "safe" (already escaped) HTML strings (`HtmlString` say), and one for safe Javascript strings (`JsString`), one for safe SQL strings (`SqlString`), etc.
Then these strings can be mixed and matched safely without accidentally causing security issues.

I won't create a solution here (and you will probably be using something like Razor anyway), but if you are interested you can read about a [Haskell approach here](http://blog.moertel.com/articles/2006/10/18/a-type-based-solution-to-the-strings-problem) and a [port of that to F#](http://stevegilham.blogspot.co.uk/2011/12/approximate-type-based-solution-to.html).


## Update ##

Many people have asked for more information on how to ensure that constrained types such as `EmailAddress` are only created through a special constructor that does the validation.
So I have created a [gist here](https://gist.github.com/swlaschin/54cfff886669ccab895a) that has some detailed examples of other ways of doing it.

{% include book_page_ddd_img.inc %}