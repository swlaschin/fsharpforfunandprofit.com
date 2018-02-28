---
layout: post
title: "Designing with types: Single case union types"
description: "Adding meaning to primitive types"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 2
categories: [Types, DDD]
---

At the end of the previous post, we had values for email addresses, zip codes, etc., defined like this:

```fsharp

EmailAddress: string;
State: string;
Zip: string;

```

These are all defined as simple strings.  But really, are they just strings?  Is an email address interchangable with a zip code or a state abbreviation?

In a domain driven design, they are indeed distinct things, not just strings. So we would ideally like to have lots of separate types for them so that they cannot accidentally be mixed up.

This has been [known as good practice](http://codemonkeyism.com/never-never-never-use-string-in-java-or-at-least-less-often/) for a long time, 
but in languages like C# and Java it can be painful to create hundred of tiny types like this, leading to the so called ["primitive obsession"](http://sourcemaking.com/refactoring/primitive-obsession) code smell.

But F# there is no excuse! It is trivial to create simple wrapper types.

## Wrapping primitive types

The simplest way to create a separate type is to wrap the underlying string type inside another type. 

We can do it using single case union types, like so:

```fsharp
type EmailAddress = EmailAddress of string
type ZipCode = ZipCode of string
type StateCode = StateCode of string
```

or alternatively, we could use record types with one field, like this:

```fsharp
type EmailAddress = { EmailAddress: string }
type ZipCode = { ZipCode: string }
type StateCode = { StateCode: string}
```

Both approaches can be used to create wrapper types around a string or other primitive type, so which way is better?

The answer is generally the single case discriminated union.  It is much easier to "wrap" and "unwrap", as the "union case" is actually a proper constructor function in its own right. Unwrapping can be done using inline pattern matching.

Here's some examples of how an `EmailAddress` type might be constructed and deconstructed:

```fsharp
type EmailAddress = EmailAddress of string

// using the constructor as a function
"a" |> EmailAddress
["a"; "b"; "c"] |> List.map EmailAddress

// inline deconstruction
let a' = "a" |> EmailAddress
let (EmailAddress a'') = a'

let addresses = 
    ["a"; "b"; "c"] 
    |> List.map EmailAddress

let addresses' = 
    addresses
    |> List.map (fun (EmailAddress e) -> e)
```

You can't do this as easily using record types.

So, let's refactor the code again to use these union types.  It now looks like this:

```fsharp
type PersonalName = 
    {
    FirstName: string;
    MiddleInitial: string option;
    LastName: string;
    }

type EmailAddress = EmailAddress of string

type EmailContactInfo = 
    {
    EmailAddress: EmailAddress;
    IsEmailVerified: bool;
    }

type ZipCode = ZipCode of string
type StateCode = StateCode of string

type PostalAddress = 
    {
    Address1: string;
    Address2: string;
    City: string;
    State: StateCode;
    Zip: ZipCode;
    }

type PostalContactInfo = 
    {
    Address: PostalAddress;
    IsAddressValid: bool;
    }

type Contact = 
    {
    Name: PersonalName;
    EmailContactInfo: EmailContactInfo;
    PostalContactInfo: PostalContactInfo;
    }
```

Another nice thing about the union type is that the implementation can be encapsulated with module signatures, as we'll discuss below.


## Naming the "case" of a single case union

In the examples above we used the same name for the case as we did for the type:

```fsharp
type EmailAddress = EmailAddress of string
type ZipCode = ZipCode of string
type StateCode = StateCode of string
```

This might seem confusing initially, but really they are in different scopes, so there is no naming collision. One is a type, and one is a constructor function with the same name.

So if you see a function signature like this:

```fsharp
val f: string -> EmailAddress
```

this refers to things in the world of types, so `EmailAddress` refers to the type.

On the other hand, if you see some code like this:

```fsharp
let x = EmailAddress y
```

this refers to things in the world of values, so `EmailAddress` refers to the constructor function.

## Constructing single case unions

For values that have special meaning, such as email addresses and zip codes, generally only certain values are allowed.  Not every string is an acceptable email or zip code.

This implies that we will need to do validation at some point, and what better point than at construction time? After all, once the value is constructed, it is immutable, so there is no worry that someone might modify it later.

Here's how we might extend the above module with some constructor functions:

```fsharp

... types as above ...

let CreateEmailAddress (s:string) = 
    if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Some (EmailAddress s)
        else None

let CreateStateCode (s:string) = 
    let s' = s.ToUpper()
    let stateCodes = ["AZ";"CA";"NY"] //etc
    if stateCodes |> List.exists ((=) s')
        then Some (StateCode s')
        else None
```

We can test the constructors now:

```fsharp
CreateStateCode "CA"
CreateStateCode "XX"

CreateEmailAddress "a@example.com"
CreateEmailAddress "example.com"
```

## Handling invalid input in a constructor ###

With these kinds of constructor functions, one immediate challenge is the question of how to handle invalid input.
For example, what should happen if I pass in "abc" to the email address constructor?

There are a number of ways to deal with it.

First, you could throw an exception. I find this ugly and unimaginative, so I'm rejecting this one out of hand!

Next, you could return an option type, with `None` meaning that the input wasn't valid.  This is what the constructor functions above do.

This is generally the easiest approach. It has the advantage that the caller has to explicitly handle the case when the value is not valid. 

For example, the caller's code for the example above might look like:
```fsharp
match (CreateEmailAddress "a@example.com") with
| Some email -> ... do something with email
| None -> ... ignore?
```

The disadvantage is that with complex validations, it might not be obvious what went wrong. Was the email too long, or missing a '@' sign, or an invalid domain? We can't tell.

If you do need more detail, you might want to return a type which contains a more detailed explanation in the error case.

The following example uses a `CreationResult` type to indicate the error in the failure case.

```fsharp
type EmailAddress = EmailAddress of string
type CreationResult<'T> = Success of 'T | Error of string            

let CreateEmailAddress2 (s:string) = 
    if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Success (EmailAddress s)
        else Error "Email address must contain an @ sign"

// test
CreateEmailAddress2 "example.com"
```

Finally, the most general approach uses continuations. That is, you pass in two functions, one for the success case (that takes the newly constructed email as parameter), and another for the failure case (that takes the error string as parameter).

```fsharp
type EmailAddress = EmailAddress of string

let CreateEmailAddressWithContinuations success failure (s:string) = 
    if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then success (EmailAddress s)
        else failure "Email address must contain an @ sign"
```

The success function takes the email as a parameter and the error function takes a string. Both functions must return the same type, but the type is up to you. 

Here is a simple example -- both functions do a printf, and return nothing (i.e. unit).

```fsharp
let success (EmailAddress s) = printfn "success creating email %s" s        
let failure  msg = printfn "error creating email: %s" msg
CreateEmailAddressWithContinuations success failure "example.com"
CreateEmailAddressWithContinuations success failure "x@example.com"
```

With continuations, you can easily reproduce any of the other approaches. Here's the way to create options, for example. In this case both functions return an `EmailAddress option`.

```fsharp
let success e = Some e
let failure _  = None
CreateEmailAddressWithContinuations success failure "example.com"
CreateEmailAddressWithContinuations success failure "x@example.com"
```

And here is the way to throw exceptions in the error case:

```fsharp
let success e = e
let failure _  = failwith "bad email address"
CreateEmailAddressWithContinuations success failure "example.com"
CreateEmailAddressWithContinuations success failure "x@example.com"
```

This code seems quite cumbersome, but in practice you would probably create a local partially applied function that you use instead of the long-winded one.

```fsharp
// setup a partially applied function
let success e = Some e
let failure _  = None
let createEmail = CreateEmailAddressWithContinuations success failure

// use the partially applied function
createEmail "x@example.com"
createEmail "example.com"
```

{% include book_page_ddd.inc %}


## Creating modules for wrapper types ###

These simple wrapper types are starting to get more complicated now that we are adding validations, and we will probably discover other functions that we want to associate with the type.

So it is probably a good idea to create a module for each wrapper type, and put the type and its associated functions there.

```fsharp
module EmailAddress = 

    type T = EmailAddress of string

    // wrap
    let create (s:string) = 
        if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
            then Some (EmailAddress s)
            else None
    
    // unwrap
    let value (EmailAddress e) = e
```

The users of the type would then use the module functions to create and unwrap the type. For example:

```fsharp

// create email addresses
let address1 = EmailAddress.create "x@example.com"
let address2 = EmailAddress.create "example.com"

// unwrap an email address
match address1 with
| Some e -> EmailAddress.value e |> printfn "the value is %s"
| None -> ()
```

## Forcing use of the constructor ###

One issue is that you cannot force callers to use the constructor. Someone could just bypass the validation and create the type directly.

In practice, that tends not to be a problem.  One simple techinique is to use naming conventions to indicate 
a "private" type, and provide "wrap" and "unwrap" functions so that the clients never need to interact with the type directly.

Here's an example:

```fsharp

module EmailAddress = 

    // private type
    type _T = EmailAddress of string

    // wrap
    let create (s:string) = 
        if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
            then Some (EmailAddress s)
            else None
    
    // unwrap
    let value (EmailAddress e) = e
```

Of course the type is not really private in this case, but you are encouraging the callers to always use the "published" functions.

If you really want to encapsulate the internals of the type and force callers to use a constructor function, you can use module signatures. 

Here's a signature file for the email address example:

```fsharp
// FILE: EmailAddress.fsi

module EmailAddress  

// encapsulated type
type T

// wrap
val create : string -> T option
    
// unwrap
val value : T -> string
```

(Note that module signatures only work in compiled projects, not in interactive scripts, so to test this, you will need to create three files in an F# project, with the filenames as shown here.)

Here's the implementation file:

```fsharp
// FILE: EmailAddress.fs

module EmailAddress  

// encapsulated type
type T = EmailAddress of string

// wrap
let create (s:string) = 
    if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Some (EmailAddress s)
        else None
    
// unwrap
let value (EmailAddress e) = e

```

And here's a client:

```fsharp
// FILE: EmailAddressClient.fs

module EmailAddressClient

open EmailAddress

// code works when using the published functions
let address1 = EmailAddress.create "x@example.com"
let address2 = EmailAddress.create "example.com"

// code that uses the internals of the type fails to compile
let address3 = T.EmailAddress "bad email"

```

The type `EmailAddress.T` exported by the module signature is opaque, so clients cannot access the internals. 

As you can see, this approach enforces the use of the constructor. Trying to create the type directly (`T.EmailAddress "bad email"`) causes a compile error.


## When to "wrap" single case unions ###
   
Now that we have the wrapper type, when should we construct them?

Generally you only need to at service boundaries (for example, boundaries in a [hexagonal architecture](http://alistair.cockburn.us/Hexagonal+architecture))

In this approach, wrapping is done in the UI layer, or when loading from a persistence layer, and once the wrapped type is created, it is passed in to the domain layer and manipulated "whole", as an opaque type. 
It is suprisingly uncommon that you actually need the wrapped contents directly when working in the domain itself.

As part of the construction, it is critical that the caller uses the provided constructor rather than doing its own validation logic. This ensures that "bad" values can never enter the domain.

For example, here is some code that shows the UI doing its own validation:

```fsharp
let processFormSubmit () = 
    let s = uiTextBox.Text
    if (s.Length < 50) 
        then // set email on domain object
        else // show validation error message        
```

A better way is to let the constructor do it, as shown earlier.

```fsharp
let processFormSubmit () = 
    let emailOpt = uiTextBox.Text |> EmailAddress.create 
    match emailOpt with
    | Some email -> // set email on domain object
    | None -> // show validation error message
```

## When to "unwrap" single case unions ###

And when is unwrapping needed? Again, generally only at service boundaries. For example, when you are persisting an email to a database, or binding to a UI element or view model.

One tip to avoid explicit unwrapping is to use the continuation approach again, passing in a function that will be applied to the wrapped value.

That is, rather than calling the "unwrap" function explicitly:

```fsharp
address |> EmailAddress.value |> printfn "the value is %s" 
```

You would pass in a function which gets applied to the inner value, like this:

```fsharp
address |> EmailAddress.apply (printfn "the value is %s")
```

Putting this together, we now have the complete `EmailAddress` module.

```fsharp
module EmailAddress = 

    type _T = EmailAddress of string

    // create with continuation
    let createWithCont success failure (s:string) = 
        if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
            then success (EmailAddress s)
            else failure "Email address must contain an @ sign"

    // create directly
    let create s = 
        let success e = Some e
        let failure _  = None
        createWithCont success failure s

    // unwrap with continuation
    let apply f (EmailAddress e) = f e

    // unwrap directly
    let value e = apply id e

```       

The `create` and `value` functions are not strictly necessary, but are added for the convenience of callers.

## The code so far ###

Let's refactor the `Contact` code now, with the new wrapper types and modules added.

```fsharp
module EmailAddress = 

    type T = EmailAddress of string

    // create with continuation
    let createWithCont success failure (s:string) = 
        if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
            then success (EmailAddress s)
            else failure "Email address must contain an @ sign"

    // create directly
    let create s = 
        let success e = Some e
        let failure _  = None
        createWithCont success failure s

    // unwrap with continuation
    let apply f (EmailAddress e) = f e

    // unwrap directly
    let value e = apply id e

module ZipCode = 

    type T = ZipCode of string

    // create with continuation
    let createWithCont success failure  (s:string) = 
        if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\d{5}$") 
            then success (ZipCode s) 
            else failure "Zip code must be 5 digits"
    
    // create directly
    let create s = 
        let success e = Some e
        let failure _  = None
        createWithCont success failure s

    // unwrap with continuation
    let apply f (ZipCode e) = f e

    // unwrap directly
    let value e = apply id e

module StateCode = 

    type T = StateCode of string

    // create with continuation
    let createWithCont success failure  (s:string) = 
        let s' = s.ToUpper()
        let stateCodes = ["AZ";"CA";"NY"] //etc
        if stateCodes |> List.exists ((=) s')
            then success (StateCode s') 
            else failure "State is not in list"
    
    // create directly
    let create s = 
        let success e = Some e
        let failure _  = None
        createWithCont success failure s

    // unwrap with continuation
    let apply f (StateCode e) = f e

    // unwrap directly
    let value e = apply id e

type PersonalName = 
    {
    FirstName: string;
    MiddleInitial: string option;
    LastName: string;
    }

type EmailContactInfo = 
    {
    EmailAddress: EmailAddress.T;
    IsEmailVerified: bool;
    }

type PostalAddress = 
    {
    Address1: string;
    Address2: string;
    City: string;
    State: StateCode.T;
    Zip: ZipCode.T;
    }

type PostalContactInfo = 
    {
    Address: PostalAddress;
    IsAddressValid: bool;
    }

type Contact = 
    {
    Name: PersonalName;
    EmailContactInfo: EmailContactInfo;
    PostalContactInfo: PostalContactInfo;
    }

```       

By the way, notice that we now have quite a lot of duplicate code in the three wrapper type modules. What would be a good way of getting rid of it, or at least making it cleaner?

## Summary ###

To sum up the use of discriminated unions, here are some guidelines:

* Do use single case discriminated unions to create types that represent the domain accurately.
* If the wrapped value needs validation, then provide constructors that do the validation and enforce their use.
* Be clear what happens when validation fails. In simple cases, return option types. In more complex cases, let the caller pass in handlers for success and failure.
* If the wrapped value has many associated functions, consider moving it into its own module.
* If you need to enforce encapsulation, use signature files.

We're still not done with refactoring.  We can alter the design of types to enforce business rules at compile time -- making illegal states unrepresentable.
 
<a name="update"></a> 

## Update ##

Many people have asked for more information on how to ensure that constrained types such as `EmailAddress` are only created through a special constructor that does the validation.
So I have created a [gist here](https://gist.github.com/swlaschin/54cfff886669ccab895a) that has some detailed examples of other ways of doing it.