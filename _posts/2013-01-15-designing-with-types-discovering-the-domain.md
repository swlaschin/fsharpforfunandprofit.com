---
layout: post
title: "Designing with types: Discovering new concepts"
description: "Gaining deeper insight into the domain"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 4
categories: [Types, DDD]
---

In the last post, we looked at how we could represent a business rule using types. 

The rule was: *"A contact must have an email or a postal address"*. 

And the type we designed was:

```fsharp
type ContactInfo = 
    | EmailOnly of EmailContactInfo
    | PostOnly of PostalContactInfo
    | EmailAndPost of EmailContactInfo * PostalContactInfo
```     

Now let's say that the business decides that phone numbers need to be supported as well.  The new business rule is: *"A contact must have at least one of the following: an email, a postal address, a home phone, or a work phone"*. 

How can we represent this now?  

A little thought reveals that there are 15 possible combinations of these four contact methods. Surely we don't want to create a union case with 15 choices? Is there a better way?

Let's hold that thought and look at a different but related problem.

## Forcing breaking changes when requirements change 

Here's the problem. Say that you have a contact structure which contains a list of email addresses and also a list of postal addresses, like so:

```fsharp
type ContactInformation = 
    {
    EmailAddresses : EmailContactInfo list;
    PostalAddresses : PostalContactInfo list
    }
```     

And, also let's say that you have created a `printReport` function that loops through the information and prints it out in a report:

```fsharp
// mock code            
let printEmail emailAddress = 
    printfn "Email Address is %s" emailAddress 

// mock code
let printPostalAddress postalAddress = 
    printfn "Postal Address is %s" postalAddress 

let printReport contactInfo = 
    let {
        EmailAddresses = emailAddresses; 
        PostalAddresses = postalAddresses; 
        } = contactInfo
    for email in emailAddresses do
         printEmail email
    for postalAddress in postalAddresses do
         printPostalAddress postalAddress 
```     

Crude, but simple and understandable.

Now if the new business rule comes into effect, we might decide to change the structure to have some new lists for the phone numbers.  The updated structure will now look something like this:

```fsharp
type PhoneContactInfo = string // dummy for now

type ContactInformation = 
    {
    EmailAddresses : EmailContactInfo list;
    PostalAddresses : PostalContactInfo list;
    HomePhones : PhoneContactInfo list;
    WorkPhones : PhoneContactInfo list;
    }
```     

If you make this change, you also want to make sure that all the functions that process the contact infomation are updated to handle the new phone cases as well.

Certainly, you will be forced to fix any pattern matches that break. But in many cases, you would *not* be forced to handle the new cases.

For example, here's `printReport` updated to work with the new lists:

```fsharp
let printReport contactInfo = 
    let {
        EmailAddresses = emailAddresses; 
        PostalAddresses = postalAddresses; 
        } = contactInfo
    for email in emailAddresses do
         printEmail email
    for postalAddress in postalAddresses do
         printPostalAddress postalAddress 
```     

Can you see the deliberate mistake? Yes, I forgot to change the function to handle the phones. The new fields in the record have not caused the code to break at all. There is no guarantee that you will remember to handle the new cases. It would be all too easy to forget.  

Again, we have the challenge: can we design types such that these situations cannot easily happen?

## Deeper insight into the domain

If you think about this example a bit more deeply, you will realize that we have missed the forest for the trees.

Our initial concept was: *"to contact a customer, there will be a list of possible emails, and a list of possible addresses, etc"*.

But really, this is all wrong. A much better concept is: *"To contact a customer, there will be a list of contact methods. Each contact method could be an email OR a postal address OR a phone number"*.

This is a key insight into how the domain should be modelled.  It creates a whole new type, a "ContactMethod", which resolves our problems in one stroke.

We can immediately refactor the types to use this new concept:

```fsharp
type ContactMethod = 
    | Email of EmailContactInfo 
    | PostalAddress of PostalContactInfo 
    | HomePhone of PhoneContactInfo 
    | WorkPhone of PhoneContactInfo 

type ContactInformation = 
    {
    ContactMethods  : ContactMethod list;
    }
```     

And the reporting code must now be changed to handle the new type as well:

```fsharp
// mock code            
let printContactMethod cm = 
    match cm with
    | Email emailAddress -> 
        printfn "Email Address is %s" emailAddress 
    | PostalAddress postalAddress -> 
         printfn "Postal Address is %s" postalAddress 
    | HomePhone phoneNumber -> 
        printfn "Home Phone is %s" phoneNumber 
    | WorkPhone phoneNumber -> 
        printfn "Work Phone is %s" phoneNumber 

let printReport contactInfo = 
    let {
        ContactMethods=methods; 
        } = contactInfo
    methods
    |> List.iter printContactMethod
```     

These changes have a number of benefits.

First, from a modelling point of view, the new types represent the domain much better, and are more adaptable to changing requirements.  

And from a development point of view, changing the type to be a union means that any new cases that we add (or remove) will break the code in a very obvious way, and it will be much harder to accidentally forget to handle all the cases.

{% include book_page_ddd.inc %}

## Back to the business rule with 15 possible combinations 

So now back to the original example. We left it thinking that, in order to encode the business rule, we might have to create 15 possible combinations of various contact methods.

But the new insight from the reporting problem also affects our understanding of the business rule.

With the "contact method" concept in our heads, we can rephase the requirement as: *"A customer must have at least one contact method. A contact method could be an email OR a postal addresses OR a phone number"*. 

So let's redesign the `Contact` type to have a list of contact methods:

```fsharp
type Contact = 
    {
    Name: PersonalName;
    ContactMethods: ContactMethod list;
    }
```

But this is still not quite right. The list could be empty.  How can we enforce the rule that there must be *at least* one contact method?

The simplest way is to create a new field that is required, like this:

```fsharp
type Contact = 
    {
    Name: PersonalName;
    PrimaryContactMethod: ContactMethod;
    SecondaryContactMethods: ContactMethod list;
    }
```

In this design, the `PrimaryContactMethod` is required, and the secondary contact methods are optional, which is exactly what the business rule requires!

And this refactoring too, has given us some insight.  It may be that the concepts of "primary" and "secondary" contact methods might, in turn, clarify code in other areas, creating a cascading change of insight and refactoring.

## Summary

In this post, we've seen how using types to model business rules can actually help you to understand the domain at a deeper level.

In the *Domain Driven Design* book, Eric Evans devotes a whole section and two chapters in particular (chapters 8 and 9) to discussing the importance of [refactoring towards deeper insight](http://dddcommunity.org/wp-content/uploads/files/books/evans_pt03.pdf).  The example in this post is simple in comparison, but I hope that it shows that how an insight like this can help improve both the model and the code correctness.  

In the next post, we'll see how types can help with representing fine-grained states. 











