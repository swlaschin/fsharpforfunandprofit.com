---
layout: post
title: "Designing with types: Introduction"
description: "Making design more transparent and improving correctness"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 1
categories: [Types, DDD]
---

In this series, we'll look at some of the ways we can use types as part of the design process. 
In particular, the thoughtful use of types can make a design more transparent and improve correctness at the same time.

This series will be focused on the "micro level" of design. That is, working at the lowest level of individual types and functions. 
Higher level design approaches, and the associated decisions about using functional or object-oriented style, will be discussed in another series.

Many of the suggestions are also feasable in C# or Java, but the lightweight nature of F# types means that it is much more likely that we will do this kind of refactoring.

## A basic example ##

For demonstration of the various uses of types, I'll work with a very straightforward example, namely a `Contact` type, such as the one below. 

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

This seems very obvious -- I'm sure we have all seen something like this many times. So what can we do with it?  How can we refactor this to make the most of the type system?

## Creating "atomic" types ##

The first thing to do is to look at the usage pattern of data access and updates.  For example, would be it be likely that `Zip` is updated without also updating `Address1` at the same time? On the other hand, it might be common that a transaction updates `EmailAddress` but not `FirstName`.  

This leads to the first guideline:  

* *Guideline: Use records or tuples to group together data that are required to be consistent (that is "atomic") but don't needlessly group together data that is not related.* 

In this case, it is fairly obvious that the three name values are a set, the address values are a set, and the email is also a set.

We have also some extra flags here, such as `IsAddressValid` and `IsEmailVerified`. Should these be part of the related set or not?  Certainly yes for now, because the flags are dependent on the related values. 

For example, if the `EmailAddress` changes, then `IsEmailVerified` probably needs to be reset to false at the same time.

For `PostalAddress`, it seems clear that the core "address" part is a useful common type, without the `IsAddressValid` flag. On the other hand, the `IsAddressValid` is associated with the address, and will be updated when it changes.

So it seems that we should create *two* types. One is a generic `PostalAddress` and the other is an address in the context of a contact, which we can call `PostalContactInfo`, say.

```fsharp
type PostalAddress = 
    {
    Address1: string;
    Address2: string;
    City: string;
    State: string;
    Zip: string;
    }

type PostalContactInfo = 
    {
    Address: PostalAddress;
    IsAddressValid: bool;
    }
```
 
 
Finally, we can use the option type to signal that certain values, such as `MiddleInitial`, are indeed optional.

```fsharp
type PersonalName = 
    {
    FirstName: string;
    // use "option" to signal optionality
    MiddleInitial: string option;
    LastName: string;
    }
```

## Summary
 
With all these changes, we now have the following code:

```fsharp
type PersonalName = 
    {
    FirstName: string;
    // use "option" to signal optionality
    MiddleInitial: string option;
    LastName: string;
    }

type EmailContactInfo = 
    {
    EmailAddress: string;
    IsEmailVerified: bool;
    }

type PostalAddress = 
    {
    Address1: string;
    Address2: string;
    City: string;
    State: string;
    Zip: string;
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

We haven't written a single function yet, but already the code represents the domain better. However, this is just the beginning of what we can do.

Next up, using single case unions to add semantic meaning to primitive types.
 
