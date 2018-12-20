---
layout: post
title: "Designing with types: Making illegal states unrepresentable"
description: "Encoding business logic in types"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 3
categories: [Types, DDD]
---

In this post, we look at a key benefit of F#, which using the type system to "make illegal states unrepresentable" (a phrase borrowed from [Yaron Minsky](https://blog.janestreet.com/effective-ml-revisited/)).

Let's look at our `Contact` type. Thanks to the previous refactoring, it is quite simple:

```fsharp
type Contact = 
    {
    Name: Name;
    EmailContactInfo: EmailContactInfo;
    PostalContactInfo: PostalContactInfo;
    }
```     

Now let's say that we have the following simple business rule: *"A contact must have an email or a postal address"*. Does our type conform to this rule?

The answer is no. The business rule implies that a contact might have an email address but no postal address, or vice versa. But as it stands, our type requires that a contact must always have *both* pieces of information. 

The answer seems obvious -- make the addresses optional, like this:

```fsharp
type Contact = 
    {
    Name: PersonalName;
    EmailContactInfo: EmailContactInfo option;
    PostalContactInfo: PostalContactInfo option;
    }
```     

But now we have gone too far the other way. In this design, it would be possible for a contact to have neither type of address at all. But the business rule says that at least one piece of information *must* be present.

What's the solution?
   
## Making illegal states unrepresentable 

If we think about the business rule carefully, we realize that there are three possibilities:

* A contact only has an email address
* A contact only has a postal address
* A contact has both a email address and a postal address

Once it is put like this, the solution becomes obvious -- use a union type with a case for each possibility.

```fsharp
type ContactInfo = 
    | EmailOnly of EmailContactInfo
    | PostOnly of PostalContactInfo
    | EmailAndPost of EmailContactInfo * PostalContactInfo

type Contact = 
    {
    Name: Name;
    ContactInfo: ContactInfo;
    }
```     

This design meets the requirements perfectly. All three cases are explictly represented, and the fourth possible case (with no email or postal address at all) is not allowed.

Note that for the "email and post" case, I just used a tuple type for now. It's perfectly adequate for what we need.

### Constructing a ContactInfo

Now let's see how we might use this in practice. We'll start by creating a new contact:

```fsharp
let contactFromEmail name emailStr = 
    let emailOpt = EmailAddress.create emailStr
    // handle cases when email is valid or invalid
    match emailOpt with
    | Some email -> 
        let emailContactInfo = 
            {EmailAddress=email; IsEmailVerified=false}
        let contactInfo = EmailOnly emailContactInfo 
        Some {Name=name; ContactInfo=contactInfo}
    | None -> None

let name = {FirstName = "A"; MiddleInitial=None; LastName="Smith"}
let contactOpt = contactFromEmail name "abc@example.com"
```     

In this code, we have created a simple helper function `contactFromEmail` to create a new contact by passing in a name and email.
However, the email might not be valid, so the function has to handle both cases, which it does by returning a `Contact option`, not a `Contact`

### Updating a ContactInfo

Now if we need to add a postal address to an existing `ContactInfo`, we have no choice but to handle all three possible cases:

* If a contact previously only had an email address, it now has both an email address and a postal address, so return a contact using the `EmailAndPost` case.
* If a contact previously only had a postal address, return a contact using the `PostOnly` case, replacing the existing address.
* If a contact previously had both an email address and a postal address, return a contact with using the `EmailAndPost` case, replacing the existing address.

So here's a helper method that updates the postal address. You can see how it explicitly handles each case.

```fsharp
let updatePostalAddress contact newPostalAddress = 
    let {Name=name; ContactInfo=contactInfo} = contact
    let newContactInfo =
        match contactInfo with
        | EmailOnly email ->
            EmailAndPost (email,newPostalAddress) 
        | PostOnly _ -> // ignore existing address
            PostOnly newPostalAddress 
        | EmailAndPost (email,_) -> // ignore existing address
            EmailAndPost (email,newPostalAddress) 
    // make a new contact
    {Name=name; ContactInfo=newContactInfo}
```     

And here is the code in use:

```fsharp
let contact = contactOpt.Value   // see warning about option.Value below
let newPostalAddress = 
    let state = StateCode.create "CA"
    let zip = ZipCode.create "97210"
    {   
        Address = 
            {
            Address1= "123 Main";
            Address2="";
            City="Beverly Hills";
            State=state.Value; // see warning about option.Value below
            Zip=zip.Value;     // see warning about option.Value below
            }; 
        IsAddressValid=false
    }
let newContact = updatePostalAddress contact newPostalAddress
```     

*WARNING: I am using `option.Value` to extract the contents of an option in this code. 
This is ok when playing around interactively but is extremely bad practice in production code! You should always use matching to handle both cases of an option.*


## Why bother to make these complicated types? 

At this point, you might be saying that we have made things unnecessarily complicated. I would answer with these points:

First, the business logic *is* complicated. There is no easy way to avoid it. If your code is not this complicated, you are not handling all the cases properly.

Second, if the logic is represented by types, it is automatically self documenting. You can look at the union cases below and immediate see what the business rule is. You do not have to spend any time trying to analyze any other code.

```fsharp
type ContactInfo = 
    | EmailOnly of EmailContactInfo
    | PostOnly of PostalContactInfo
    | EmailAndPost of EmailContactInfo * PostalContactInfo
```     

Finally, if the logic is represented by a type, any changes to the business rules will immediately create breaking changes, which is a generally a good thing. 

In the next post, we'll dig deeper into the last point. As you try to represent business logic using types, you may suddenly find that can gain a whole new insight into the domain.

{% include book_page_ddd_img.inc %}
