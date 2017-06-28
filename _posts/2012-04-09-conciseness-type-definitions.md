---
layout: post
title: "Low overhead type definitions"
description: "No penalty for making new types"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 9
categories: [Conciseness,Types]
---

In C#, there is a disincentive for creating new types -- the lack of type inference means you need to explicitly specify types in most places, resulting in brittleness and more visual clutter. As a result, there is always a temptation to create monolithic classes rather than modularizing them.

In F# there is no penalty for making new types, so it is quite common to have hundreds if not thousands of them.  Every time you need to define a structure, you can create a special type, rather than reusing (and overloading) existing types such as strings and lists. 

This means that your programs will be more type-safe, more self documenting, and more maintainable (because when the types change you will immediately get compile-time errors rather than runtime errors).

Here are some examples of one-liner types in F#: 

```fsharp
open System

// some "record" types
type Person = {FirstName:string; LastName:string; Dob:DateTime}
type Coord = {Lat:float; Long:float}

// some "union" (choice) types
type TimePeriod = Hour | Day | Week | Year
type Temperature = C of int | F of int
type Appointment = OneTime of DateTime 
                   | Recurring of DateTime list
```


## F# types and domain driven design

The conciseness of the type system in F# is particularly useful when doing domain driven design (DDD).  In DDD, for each real world entity and value object, you ideally want to have a corresponding type. This can mean creating hundreds of "little" types, which can be tedious in C#.  

Furthermore, "value" objects in DDD should have structural equality, meaning that two objects containing the same data should always be equal.  In C# this can mean more tedium in overriding `IEquatable<T>`, but in F#, you get this for free by default.

To show how easy it is to create DDD types in F#, here are some example types that might be created for a simple "customer" domain. 

```fsharp
type PersonalName = {FirstName:string; LastName:string}

// Addresses
type StreetAddress = {Line1:string; Line2:string; Line3:string }

type ZipCode =  ZipCode of string   
type StateAbbrev =  StateAbbrev of string
type ZipAndState =  {State:StateAbbrev; Zip:ZipCode }
type USAddress = {Street:StreetAddress; Region:ZipAndState}

type UKPostCode =  PostCode of string
type UKAddress = {Street:StreetAddress; Region:UKPostCode}

type InternationalAddress = {
    Street:StreetAddress; Region:string; CountryName:string}

// choice type  -- must be one of these three specific types
type Address = USAddress | UKAddress | InternationalAddress

// Email
type Email = Email of string

// Phone
type CountryPrefix = Prefix of int
type Phone = {CountryPrefix:CountryPrefix; LocalNumber:string}

type Contact = 
    {
    PersonalName: PersonalName;
    // "option" means it might be missing
    Address: Address option;
    Email: Email option;
    Phone: Phone option;
    }

// Put it all together into a CustomerAccount type
type CustomerAccountId  = AccountId of string
type CustomerType  = Prospect | Active | Inactive

// override equality and deny comparison
[<CustomEquality; NoComparison>]
type CustomerAccount = 
    {
    CustomerAccountId: CustomerAccountId;
    CustomerType: CustomerType;
    ContactInfo: Contact;
    }

    override this.Equals(other) =
        match other with
        | :? CustomerAccount as otherCust -> 
          (this.CustomerAccountId = otherCust.CustomerAccountId)
        | _ -> false

    override this.GetHashCode() = hash this.CustomerAccountId 
```

This code fragment contains 17 type definitions in just a few lines, but with minimal complexity. How many lines of C# code would you need to do the same thing?

Obviously, this is a simplified version with just the basic types -- in a real system, constraints and other methods would be added.  But note how easy it is to create lots of DDD value objects, especially wrapper types for strings, such as "`ZipCode`" and "`Email`". By using these wrapper types, we can enforce certain constraints at creation time, and also ensure that these types don't get confused with unconstrained strings in normal code. The only "entity" type is the `CustomerAccount`, which is clearly indicated as having special treatment for equality and comparison.

For a more in-depth discussion, see the series called ["Domain driven design in F#"](/series/domain-driven-design-in-fsharp.html).

