---
layout: post
title: "Using types as access tokens"
description: "A functional approach to authorization, part 3"
seriesId: "A functional approach to authorization"
seriesOrder: 3
categories: []
image: "/assets/img/auth_token.png"
---

*UPDATE: [Slides and video from my talk on this topic](/cap/)*

In the previous posts ([link](/posts/capability-based-security/), [link](/posts/capability-based-security-2/))
we looked at "capabilities" as the basis for locking down code.

But in most of the examples so far, we've been relying on self-discipline to avoid using the global capabilities,
or by trying to hide the "raw" capabilities using the `internal` keyword.

It's a bit ugly -- can we do better?

In this post, we'll show that we can by using types to emulate "access tokens".

## Real-world authorization

First, let's step back and look at how authorization works in the real world.

Here's a simplified diagram of a basic authorization system (such as [OAuth 2.0](https://developers.google.com/accounts/docs/OAuth2#basicsteps)).

![Simplified authentication](/assets/img/auth_token.png)

The steps, in their crudest form, are:

* The client presents some claims to the Authorization Service, including identity and the id and scope (capability) of the service it wants to access.
* The Authorization Service checks whether the client is authorized, and if so, creates an access token which is returned to the client.
* The client then presents this access token to the Resource Service (the service the client wants to use). 
* In general, the access token will only let the client do certain things. In our terminology, it has been granted a limited set of capabilities. 

Obviously, there's a lot more to it than that, but it will be enough to give us some ideas.

## Implementing an Access Token

If we want to emulate this in our design, it's clear that we need some sort of "access token". Since we're running in a single process, and the primary goal
is to stop accidental errors, we don't need to do cryptographic signatures and all that. All we need is some object that can *only* be created by an authorization service.

That's easy. We can just use a type with a private constructor!  

We'll set it up so that the type can only be created by an Authorization Service, but is required to be passed in to the database service. 

For example, here's an F# implementation of the `AccessToken` type. The constructor is private, and there's a static member that returns an instance if
authorization is allowed.

```fsharp
type AccessToken private() = 

    // create an AccessToken that allows access to a particular customer
    static member getAccessToCustomer id principal = 
        let principalId = GetIdForPrincipal(principal)
        if (principalId = id) || principal.IsInRole("CustomerAgent") then
            Some <| AccessToken() 
        else
            None   
```

Next, in the database module, we will add an extra parameter to each function, which is the AccessToken.

Because the AccessToken token is required, we can safely make the database module public now, as no unauthorized client can call the functions.

```fsharp
let getCustomer (accessToken:AccessToken) (id:CustomerId) = 
    // get customer data

let updateCustomer (accessToken:AccessToken) (id:CustomerId) (data:CustomerData) = 
    // update database
```

Note that the accessToken is not actually used in the implementation. It is just there to force callers to obtain a token at compile time.

So let's look at how this might be used in practice.

```fsharp
let principal = // from context
let id = // from context

// attempt to get an access token
let accessToken = AuthorizationService.AccessToken.getAccessToCustomer id principal
```

At this point we have an optional access token. Using `Option.map`, we can apply it to `CustomerDatabase.getCustomer` to get an optional capability.
And by partially applying the access token, the user of the capability is isolated from the authentication process.

```fsharp
let getCustomerCapability = 
    accessToken |> Option.map CustomerDatabase.getCustomer
```

And finally, we can attempt to use the capability, if present.

```fsharp
match getCustomerCapability with
| Some getCustomer -> getCustomer id
| None -> Failure AuthorizationFailed // error
```

So now we have a statically typed authorization system that will prevent us from accidentally getting too much access to the database.

## Oops! We have made a big mistake... 

This design looks fine on the surface, but we haven't actually made anything more secure.

The first problem is that the `AccessToken` type is too broad. If I can somehow get hold of an access token for innocently writing to a config file,
I might also be able to use it to maliciously update passwords as well.

The second problem is that the `AccessToken` throws away the context of the operation. For example, I might get an access token for updating `CustomerId 1`,
but when I actually *use* the capability, I could pass in `CustomerId 2` as the the customer id instead!

The answer to both these issues is to store information in the access token itself, at the point when the authorization is granted. 

For example, if the token stores the operation that was requested, the service can check that the token matches the operation being called,
which ensures that the token can only be used for that particular operation. 
In fact, as we'll see in a minute, we can be lazy and have the *compiler* do this checking for us!

And, if we also store any data (such as the customer id) that was part of the authorization request, then we don't need to ask for it again in the service.

What's more, we can trust that the information stored in the token is not forged or tampered with because only the Authorization Service can create the token.
In other words, this is the equivalent of the token being "signed". 

## Revisiting the Access Token design

So let's revisit the design and fix it up.

First we will define a *distinct type* for each capability. The type will also contain any data needed at authorization time, such as the customer id.

For example, here are two types that represent access to capabilities, one for accessing a customer (both read and update), and another one updating a password.
Both of these will store the `CustomerId` that was provided at authorization time.

```fsharp
type AccessCustomer = AccessCustomer of CustomerId
type UpdatePassword = UpdatePassword of CustomerId
```

Next, the `AccessToken` type is redefined to be a generic container with a `data` field.
The constructor is still private, but a public getter is added so clients can access the data field.

```fsharp
type AccessToken<'data> = private {data:'data} with 
    // but do allow read access to the data
    member this.Data = this.data
```

The authorization implementation is similar to the previous examples, except that this time the capability type and customer id are stored in the token.

```fsharp
// create an AccessToken that allows access to a particular customer
let getAccessCustomerToken id principal = 
    if customerIdBelongsToPrincipal id principal ||
        principal.IsInRole("CustomerAgent") 
    then
        Some {data=AccessCustomer id}
    else
        None   

// create an AccessToken that allows access to UpdatePassword 
let getUpdatePasswordToken id principal = 
    if customerIdBelongsToPrincipal id principal then
        Some {data=UpdatePassword id}
    else
        None
```

## Using Access Tokens in the database

With these access token types in place the database functions can be rewritten to require a token of a particular type.
The `customerId` is no longer needed as an explicit parameter, because it will be passed in as part of the access token's data.

Note also that both `getCustomer` and `updateCustomer` can use the same type of token (`AccessCustomer`), but `updatePassword` requires a different type (`UpdatePassword`).

```fsharp
let getCustomer (accessToken:AccessToken<AccessCustomer>) = 
    // get customer id
    let (AccessCustomer id) = accessToken.Data

    // now get customer data using the id
    match db.TryGetValue id with
    | true, value -> Success value 
    | false, _ -> Failure (CustomerIdNotFound id)

let updateCustomer (accessToken:AccessToken<AccessCustomer>) (data:CustomerData) = 
    // get customer id
    let (AccessCustomer id) = accessToken.Data

    // update database
    db.[id] <- data
    Success ()

let updatePassword (accessToken:AccessToken<UpdatePassword>) (password:Password) = 
    Success ()   // dummy implementation
```

## Putting it all together

So now let's see all this in action.

The steps to getting a customer are:

* Attempt to get the access token from the authorization service
* If you have the access token, get the `getCustomer` capability from the database
* Finally, if you have the capability, you can use it. 

Note that, as always, the `getCustomer` capability does not take a customer id parameter. It was baked in when the capability was created.

```fsharp
let principal =  // from context
let customerId = // from context

// attempt to get a capability
let getCustomerCap = 
    // attempt to get a token
    let accessToken = AuthorizationService.getAccessCustomerToken customerId principal
    match accessToken with
    // if token is present pass the token to CustomerDatabase.getCustomer, 
    // and return a unit->CustomerData 
    | Some token -> 
        Some (fun () -> CustomerDatabase.getCustomer token)
    | None -> None

// use the capability, if available               
match getCustomerCap with
| Some getCustomer -> getCustomer()
| None -> Failure AuthorizationFailed // error
```

Now what happens if we accidentally get the *wrong* type of access token? For example, let us try to access the `updatePassword` function with an `AccessCustomer` token.

```fsharp
// attempt to get a capability
let getUpdatePasswordCap = 
    let accessToken = AuthorizationService.getAccessCustomerToken customerId principal
    match accessToken with
    | Some token -> 
        Some (fun password -> CustomerDatabase.updatePassword token password)
    | None -> None

match getUpdatePasswordCap with
| Some updatePassword -> 
    let password = Password "p@ssw0rd"
    updatePassword password 
| None -> 
    Failure AuthorizationFailed // error
```

This code will not even compile!  The line `CustomerDatabase.updatePassword token password` has an error.

```text
error FS0001: Type mismatch. Expecting a
    AccessToken<Capabilities.UpdatePassword>    
but given a
    AccessToken<Capabilities.AccessCustomer>    
The type 'Capabilities.UpdatePassword' does not match the type 'Capabilities.AccessCustomer'
```

We have accidentally fetched the wrong kind of Access Token, but we have been stopped from accessing the wrong database method at *compile time*.

Using types in this way is a nice solution to the problem of global access to a potentially dangerous capability.

## A complete example in F# ##

In the last post, I showed a complete console application in F# that used capabilities to update a database.

Now let's update it to use access tokens as well. (The code is available as a [gist here](https://gist.github.com/swlaschin/909c5b24bf921e5baa8c#file-capabilitybasedsecurity_consoleexample_withtypes-fsx)).

Since this is an update of the example, I'll focus on just the changes.

### Defining the capabilities

The capabilities are as before except that we have defined the two new types (`AccessCustomer` and `UpdatePassword`) to be stored inside the access tokens.

```fsharp
module Capabilities = 
    open Rop
    open Domain

    // each access token gets its own type
    type AccessCustomer = AccessCustomer of CustomerId
    type UpdatePassword = UpdatePassword of CustomerId

    // capabilities
    type GetCustomerCap = unit -> SuccessFailure<CustomerData,FailureCase>
    type UpdateCustomerCap = CustomerData -> SuccessFailure<unit,FailureCase>
    type UpdatePasswordCap = Password -> SuccessFailure<unit,FailureCase>

    type CapabilityProvider = {
        /// given a customerId and IPrincipal, attempt to get the GetCustomer capability
        getCustomer : CustomerId -> IPrincipal -> GetCustomerCap option
        /// given a customerId and IPrincipal, attempt to get the UpdateCustomer capability
        updateCustomer : CustomerId -> IPrincipal -> UpdateCustomerCap option
        /// given a customerId and IPrincipal, attempt to get the UpdatePassword capability
        updatePassword : CustomerId -> IPrincipal -> UpdatePasswordCap option 
        }
```

### Implementing authorization

The authorization implementation must be changed to return `AccessTokens` now.  The `onlyIfDuringBusinessHours` restriction applies to capabilities, not access tokens, so it is unchanged.

```fsharp
// the constructor is protected
type AccessToken<'data> = private {data:'data} with 
    // but do allow read access to the data
    member this.Data = this.data

let onlyForSameId (id:CustomerId) (principal:IPrincipal) = 
    if Authentication.customerIdOwnedByPrincipal id principal then
        Some {data=AccessCustomer id}
    else
        None

let onlyForAgents (id:CustomerId) (principal:IPrincipal)  = 
    if principal.IsInRole(Authentication.customerAgentRole) then
        Some {data=AccessCustomer id}
    else
        None

let onlyIfDuringBusinessHours (time:DateTime) f = 
    if time.Hour >= 8 && time.Hour <= 17 then
        Some f
    else
        None

// constrain who can call a password update function
let passwordUpdate (id:CustomerId) (principal:IPrincipal) = 
    if Authentication.customerIdOwnedByPrincipal id principal then
        Some {data=UpdatePassword id}
    else
        None
```

### Implementing the database 

Compared with the example from the previous post, the database functions have the `CustomerId` parameter replaced with an `AccessToken` instead.

Here's what the database implementation looked like *before* using access tokens:

```fsharp
let getCustomer id = 
    // code

let updateCustomer id data = 
    // code

let updatePassword (id:CustomerId,password:Password) = 
    // code
```

And here's what the code looks like *after* using access tokens:

```fsharp
let getCustomer (accessToken:AccessToken<AccessCustomer>) = 
    // get customer id
    let (AccessCustomer id) = accessToken.Data

    // now get customer data using the id
    // as before

let updateCustomer (accessToken:AccessToken<AccessCustomer>) (data:CustomerData) = 
    // get customer id
    let (AccessCustomer id) = accessToken.Data

    // update database
    // as before

let updatePassword (accessToken:AccessToken<UpdatePassword>) (password:Password) = 
    // as before
```

### Implementing the business services and user interface

The code relating to the business services and UI is completely unchanged. 

Because these functions have been passed capabilities only, they are decoupled from both the lower levels and higher levels of the application,
so any change in the authorization logic has no effect on these layers.

### Implementing the top-level module

The major change in the top-level module is how the capabilities are fetched.  We now have an additional step of getting the access token first.

Here's what the code looked like *before* using access tokens:

```fsharp
let getCustomerOnlyForSameId id principal  = 
    onlyForSameId id principal CustomerDatabase.getCustomer

let getCustomerOnlyForAgentsInBusinessHours id principal = 
    let cap1 = onlyForAgents id principal CustomerDatabase.getCustomer
    let restriction f = onlyIfDuringBusinessHours (DateTime.Now) f
    cap1 |> restrict restriction 
```

And here's what the code looks like *after* using access tokens:

```fsharp
let getCustomerOnlyForSameId id principal  = 
    let accessToken = Authorization.onlyForSameId id principal
    accessToken |> tokenToCap CustomerDatabase.getCustomer 

let getCustomerOnlyForAgentsInBusinessHours id principal = 
    let accessToken = Authorization.onlyForAgents id principal
    let cap1 = accessToken |> tokenToCap CustomerDatabase.getCustomer 
    let restriction f = onlyIfDuringBusinessHours (DateTime.Now) f
    cap1 |> restrict restriction 
```

The `tokenToCap` function is a little utility that applies the (optional) token to a given function as the first parameter. The output is an (equally optional) capability.

```fsharp
let tokenToCap f token =
    token 
    |> Option.map (fun token -> 
        fun () -> f token)
```

And that's it for the changes needed to support access tokens. 
You can see all the code for this example [here](https://gist.github.com/swlaschin/909c5b24bf921e5baa8c#file-capabilitybasedsecurity_consoleexample_withtypes-fsx).

## Summary of Part 3

In this post, we used types to represent access tokens, as follows:

* The `AccessToken` type is the equivalent of a signed ticket in a distributed authorization system. It has a private constructor and can only be created by the Authorization Service (ignoring reflection, of course!).
* A specific type of `AccessToken` is needed to access a specific operation, which ensures that we can't accidentally do unauthorized activities.
* Each specific type of `AccessToken` can store custom data collected at authorization time, such as a `CustomerId`.
* Global functions, such as the database, are modified so that they cannot be accessed without an access token. This means that they can safely be made public.

**Question: Why not also store the caller in the access token, so that no other client can use it?**

This is not needed because of the authority-based approach we're using.
As discussed in the [first post](/posts/capability-based-security/#authority), once a client has a capability,
they can pass it around to other people to use, so there is no point limiting it to a specific caller.  

**Question: The authorization module needs to know about the capability and access token types now. Isn't that adding extra coupling?**

If the authorization service is going to do its job, it has to know *something* about what capabilities are available, so there is always some coupling, whether it
is implicit ("resources" and "actions" in XACML) or explicit via types, as in this model. 

So yes, the authorization service and database service both have a dependency on the set of capabilities, but they are not coupled to each other directly.

**Question: How do you use this model in a distributed system?**

This model is really only designed to be used in a single codebase, so that type checking can occur.

You could probably hack it so that types are turned into tickets at the boundary, and conversely, but I haven't looked at that at all.

**Question: Where can I read more on using types as access tokens?**

This type-oriented version of an access token is my own design, although I very much doubt that I'm the first person to think of using types this way.
There are some related things for Haskell [(example)](http://hackage.haskell.org/package/Capabilities) but I don't know of any directly
analogous work that's accessible to mainstream developers.

**I've got more questions...**

Some additional questions are answered at the end of [part 1](/posts/capability-based-security/#summary) and [part 2](/posts/capability-based-security-2/#summary), so read those answers first.
Otherwise please add your question in the comments below, and I'll try to address it.

## Conclusion

Thanks for making it all the way to the end!

As I said at the beginning, the goal is not to create an absolutely safe system, but instead encourage you to think about and integrate authorization constraints
into the design of your system from the beginning, rather than treating it as an afterthought. 

What's more, the point of doing this extra work is not just to improve *security*,
but also to *improve the general design* of your code. If you follow the principle of least authority, you get modularity, decoupling, explicit dependencies, etc., for free!

In my opinion, a capability-based system works very well for this:

* Functions map well to capabilities, and the need to pass capabilities around fits in very well with standard functional programming patterns.
* Once created, capabilities hide all the ugliness of authorization from the client,
and so the model succeeds in "making security user-friendly by making the security invisible".
* Finally, with the addition of type-checked access tokens, we can have high confidence that no part of our code can access global functions to do unauthorized operations.



I hope you found this series useful, and might inspire you to investigate some of these ideas more fully.

*NOTE: All the code for this post is available as a [gist here](https://gist.github.com/swlaschin/909c5b24bf921e5baa8c#file-capabilitybasedsecurity_typeexample-fsx)
and [here](https://gist.github.com/swlaschin/909c5b24bf921e5baa8c#file-capabilitybasedsecurity_consoleexample_withtypes-fsx).*
