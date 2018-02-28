---
layout: post
title: "Designing with types: Conclusion"
description: "A before and after comparison"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 8
categories: [Types, DDD]
---

In this series, we've looked at some of the ways we can use types as part of the design process, including:

* Breaking large structures down into small "atomic" components.
* Using single case unions to add semantic meaning and validation to key domain types such `EmailAddress` and `ZipCode`.
* Ensuring that the type system can only represent valid data ("making illegal states unrepresentable").
* Using types as an analysis tool to uncover hidden requirements 
* Replacing flags and enums with simple state machines 
* Replacing primitive strings with types that guarantee various constraints 

For this final post, let's see them all applied together. 

## The "before" code ##

Here's the original example we started off with in the [first post](/posts/designing-with-types-intro/) in the series:

```fsharp
type Contact = 
    {
    FirstName: string;
    MiddleInitial: string;
    LastName: string;

    EmailAddress: string;
    //true if ownership of email address is confirmed
    IsEmailVerified: bool;

    Address1: string;
    Address2: string;
    City: string;
    State: string;
    Zip: string;
    //true if validated against address service
    IsAddressValid: bool; 
    }
```

And how does that compare to the final result after applying all the techniques above?

## The "after" code ##

First, let's start with the types that are not application specific.  These types could probably be reused in many applications.

```fsharp
// ========================================
// WrappedString 
// ========================================

/// Common code for wrapped strings
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

    /// Equality 
    let equals left right = 
        (value left) = (value right)

    /// Comparison
    let compareTo left right = 
        (value left).CompareTo (value right)

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

    /// map helpers
    let mapAdd k v map = 
        Map.add (value k) v map    

    let mapContainsKey k map =  
        Map.containsKey (value k) map    

    let mapTryFind k map =  
        Map.tryFind (value k) map    

// ========================================
// Email address (not application specific)
// ========================================

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

// ========================================
// ZipCode (not application specific)
// ========================================

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

// ========================================
// StateCode (not application specific)
// ========================================

module StateCode = 

    type T = StateCode  of string with
        interface WrappedString.IWrappedString with
            member this.Value = let (StateCode  s) = this in s

    let create = 
        let canonicalize = WrappedString.singleLineTrimmed 
        let stateCodes = ["AZ";"CA";"NY"] //etc
        let isValid s = 
            stateCodes |> List.exists ((=) s)

        WrappedString.create canonicalize isValid StateCode

    /// Converts any wrapped string to a StateCode
    let convert s = WrappedString.apply create s

// ========================================
// PostalAddress (not application specific)
// ========================================

module PostalAddress = 

    type USPostalAddress = 
        {
        Address1: WrappedString.String50;
        Address2: WrappedString.String50;
        City: WrappedString.String50;
        State: StateCode.T;
        Zip: ZipCode.T;
        }

    type UKPostalAddress = 
        {
        Address1: WrappedString.String50;
        Address2: WrappedString.String50;
        Town: WrappedString.String50;
        PostCode: WrappedString.String50;   // todo
        }

    type GenericPostalAddress = 
        {
        Address1: WrappedString.String50;
        Address2: WrappedString.String50;
        Address3: WrappedString.String50;
        Address4: WrappedString.String50;
        Address5: WrappedString.String50;
        }

    type T = 
        | USPostalAddress of USPostalAddress 
        | UKPostalAddress of UKPostalAddress 
        | GenericPostalAddress of GenericPostalAddress 

// ========================================
// PersonalName (not application specific)
// ========================================

module PersonalName = 
    open WrappedString

    type T = 
        {
        FirstName: String50;
        MiddleName: String50 option;
        LastName: String100;
        }

    /// create a new value
    let create first middle last = 
        match (string50 first),(string100 last) with
        | Some f, Some l ->
            Some {
                FirstName = f;
                MiddleName = (string50 middle)
                LastName = l;
                }
        | _ -> 
            None

    /// concat the names together        
    /// and return a raw string
    let fullNameRaw personalName = 
        let f = personalName.FirstName |> value 
        let l = personalName.LastName |> value 
        let names = 
            match personalName.MiddleName with
            | None -> [| f; l |]
            | Some middle -> [| f; (value middle); l |]
        System.String.Join(" ", names)

    /// concat the names together        
    /// and return None if too long
    let fullNameOption personalName = 
        personalName |> fullNameRaw |> string100

    /// concat the names together        
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

And now the application specific types.  
 
```fsharp

// ========================================
// EmailContactInfo -- state machine
// ========================================

module EmailContactInfo = 
    open System

    // UnverifiedData = just the EmailAddress
    type UnverifiedData = EmailAddress.T

    // VerifiedData = EmailAddress plus the time it was verified
    type VerifiedData = EmailAddress.T * DateTime 

    // set of states
    type T = 
        | UnverifiedState of UnverifiedData
        | VerifiedState of VerifiedData

    let create email = 
        // unverified on creation
        UnverifiedState email

    // handle the "verified" event
    let verified emailContactInfo dateVerified = 
        match emailContactInfo with
        | UnverifiedState email ->
            // construct a new info in the verified state
            VerifiedState (email, dateVerified) 
        | VerifiedState _ ->
            // ignore
            emailContactInfo

    let sendVerificationEmail emailContactInfo = 
        match emailContactInfo with
        | UnverifiedState email ->
            // send email
            printfn "sending email"
        | VerifiedState _ ->
            // do nothing
            ()

    let sendPasswordReset emailContactInfo = 
        match emailContactInfo with
        | UnverifiedState email ->
            // ignore
            ()
        | VerifiedState _ ->
            // ignore
            printfn "sending password reset"

// ========================================
// PostalContactInfo -- state machine
// ========================================

module PostalContactInfo = 
    open System

    // InvalidData = just the PostalAddress
    type InvalidData = PostalAddress.T

    // ValidData = PostalAddress plus the time it was verified
    type ValidData = PostalAddress.T * DateTime 

    // set of states
    type T = 
        | InvalidState of InvalidData
        | ValidState of ValidData

    let create address = 
        // invalid on creation
        InvalidState address

    // handle the "validated" event
    let validated postalContactInfo dateValidated = 
        match postalContactInfo with
        | InvalidState address ->
            // construct a new info in the valid state
            ValidState (address, dateValidated) 
        | ValidState _ ->
            // ignore
            postalContactInfo 

    let contactValidationService postalContactInfo = 
        let dateIsTooLongAgo (d:DateTime) =
            d < DateTime.Today.AddYears(-1)

        match postalContactInfo with
        | InvalidState address ->
            printfn "contacting the address validation service"
        | ValidState (address,date) when date |> dateIsTooLongAgo  ->
            printfn "last checked a long time ago."
            printfn "contacting the address validation service again"
        | ValidState  _ ->
            printfn "recently checked. Doing nothing."

// ========================================
// ContactMethod and Contact
// ========================================

type ContactMethod = 
    | Email of EmailContactInfo.T 
    | PostalAddress of PostalContactInfo.T

type Contact = 
    {
    Name: PersonalName.T;
    PrimaryContactMethod: ContactMethod;
    SecondaryContactMethods: ContactMethod list;
    }

```

{% include book_page_ddd_img.inc %}


## Conclusion ##

Phew!  The new code is much, much longer than the original code. Granted, it has a lot of supporting functions that were not needed in the original version, but even so it seems like a lot of extra work. So was it worth it?

I think the answer is yes. Here are some of the reasons why:

**The new code is more explicit**

If we look at the original example, there was no atomicity between fields, no validation rules, no length constraints, nothing to stop you updating flags in the wrong order, and so on.

The data structure was "dumb" and all the business rules were implicit in the application code.
Chances are that the application would have lots of subtle bugs that might not even show up in unit tests.  (*Are you sure the application reset the `IsEmailVerified` flag to false in every place the email address was updated?*)

On the other hand, the new code is extremely explicit about every little detail. If I stripped away everything but the types themselves, you would have a very good idea of what the business rules and domain constraints were.

**The new code won't let you postpone error handling**

Writing code that works with the new types means that you are forced to handle every possible thing that could go wrong, from dealing with a name that is too long, to failing to supply a contact method.
And you have to do this up front at construction time. You can't postpone it till later. 

Writing such error handling code can be annoying and tedious, but on the other hand, it pretty much writes itself. There is really only one way to write code that actually compiles with these types.

**The new code is more likely to be correct**

The *huge* benefit of the new code is that it is probably bug free. Without even writing any unit tests, I can be quite confident that a first name will never be truncated when written to a `varchar(50)` in a database, and that I can never accidentally send out a verification email twice.  

And in terms of the code itself, many of the things that you as a developer have to remember to deal with (or forget to deal with) are completely absent. No null checks, no casting, no worrying about what the default should be in a `switch` statement. And if you like to use cyclomatic complexity as a code quality metric, you might note that there are only three `if` statements in the entire 350 odd lines. 

**A word of warning...**

Finally, beware! Getting comfortable with this style of type-based design will have an insidious effect on you. You will start to develop paranoia whenever you see code that isn't typed strictly enough. (*How long should an email address be, exactly?*) and you will be unable to write the simplest python script without getting anxious. When this happens, you will have been fully inducted into the cult. Welcome!
 

*If you liked this series, here is a slide deck that covers many of the same topics. There is [a video as well (here)](/ddd/)*

<iframe src="//www.slideshare.net/slideshow/embed_code/32418451" width="627" height="556" frameborder="0" marginwidth="0" marginheight="0" scrolling="no" style="border:1px solid #CCC; border-width:1px 1px 0; margin-bottom:5px; max-width: 100%;" allowfullscreen> </iframe> <div style="margin-bottom:5px"> <strong> <a href="https://www.slideshare.net/ScottWlaschin/domain-driven-design-with-the-f-type-system-functional-londoners-2014" title="Domain Driven Design with the F# type System -- F#unctional Londoners 2014" target="_blank">Domain Driven Design with the F# type System -- F#unctional Londoners 2014</a> </strong> from <strong><a href="http://www.slideshare.net/ScottWlaschin" target="_blank">my slideshare page</a></strong> </div>

