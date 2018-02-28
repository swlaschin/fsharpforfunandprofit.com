---
layout: post
title: "Designing with types: Making state explicit"
description: "Using state machines to ensure correctness"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 5
categories: [Types, DDD]
---

In this post we will look at making implicit states explicit by using state machines, and then modelling these state machines with union types.

## Background ##

In an [earlier post](/posts/designing-with-types-single-case-dus/) in this series, we looked at single case unions as a wrapper for types such as email addresses.

```fsharp
module EmailAddress = 

    type T = EmailAddress of string

    let create (s:string) = 
        if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
            then Some (EmailAddress s)
            else None
```     

This code assumes that either an address is valid or it is not. If it is not, we reject it altogether and return `None` instead of a valid value.

But there are degrees of validity. For example, what happens if we want to keep an invalid email address around rather than just rejecting it?  In this case, as usual, we want to use the type system to make sure that we don't get a valid address mixed up with an invalid address.

The obvious way to do this is with a union type:
```fsharp
module EmailAddress = 

    type T = 
        | ValidEmailAddress of string
        | InvalidEmailAddress of string

    let create (s:string) = 
        if System.Text.RegularExpressions.Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
            then ValidEmailAddress s    // change result type 
            else InvalidEmailAddress s  // change result type

    // test
    let valid = create "abc@example.com"
    let invalid = create "example.com"
```     

and with these types we can ensure that only valid emails get sent:

```fsharp
let sendMessageTo t = 
    match t with 
    | ValidEmailAddress email ->
         // send email
    | InvalidEmailAddress _ -> 
         // ignore
```     

So far, so good. This kind of design should be obvious to you by now.  

But this approach is more widely applicable than you might think.  In many situations, there are similar "states" that are not made explicit, and handled with flags, enums, or conditional logic in code.

## State machines ##

In the example above, the "valid" and "invalid" cases are mutually incompatible. That is, a valid email can never become invalid, and vice versa. 

But in many cases, it is possible to go from one case to another, triggered by some kind of event. At which point we have a ["state machine"](http://en.wikipedia.org/wiki/Finite-state_machine), where each case represents a "state", and moving from one state to another is a "transition".

Some examples:

* A email address might have states "Unverified" and "Verified", where you can transition from the "Unverified" state to the "Verified" state by asking the user to click on a link in a confirmation email.
![State transition diagram: Verified Email](/assets/img/State_VerifiedEmail.png)

* A shopping cart might have states "Empty", "Active" and "Paid", where you can transition from the "Empty" state to the "Active" state by adding an item to the cart, and to the "Paid" state by paying.
![State transition diagram: Shopping Cart](/assets/img/State_ShoppingCart.png)

* A game such as chess might have states "WhiteToPlay", "BlackToPlay" and "GameOver", where you can transition from the "WhiteToPlay" state to the "BlackToPlay" state by White making a non-game-ending move, or transition to the "GameOver" state by playing a checkmate move. 
![State transition diagram: Chess game](/assets/img/State_Chess.png)

In each of these cases, we have a set of states, a set of transitions, and events that can trigger a transition.  
State machines are often represented by a table, like this one for a shopping cart:

<table class="table table-condensed">
<thead>
<tr>
<th>Current State</th>
<th>Event-></th>
<th>Add Item</th>
<th>Remove Item</th>
<th>Pay</th>
</tr>
</thead>
<tbody>
<tr>
<th>Empty</th>
<td></td>
<td>new state = Active</td>
<td>n/a</td>
<td>n/a</td>
</tr>
<tr>
<th>Active</th>
<td></td>
<td>new state = Active</td>
<td>new state = Active or Empty,<br> depending on the number of items</td>
<td>new state = Paid</td>
</tr>
<tr>
<th>Paid</th>
<td></td>
<td>n/a</td>
<td>n/a</td>
<td>n/a</td>
</tr>
</tbody>
</table>
  
With a table like this, you can quickly see exactly what should happen for each event when the system is in a given state.  

<a name="why-use"></a>  

## Why use state machines? 

There are a number of benefits to using state machines in these cases:

**Each state can have different allowable behavior.**

In the verified email example, there is probably a business rule that says that you can only send password resets to verified email addresses, not to unverified addresses. 
And in the shopping cart example, only an active cart can be paid for, and a paid cart cannot be added to. 

**All the states are explicitly documented.**

It is all too easy to have important states that are implicit but never documented.

For example, the "empty cart" has different behavior from the "active cart" but it would be rare to see this documented explicitly in code.

**It is a design tool that forces you to think about every possibility that could occur.**

A common cause of errors is that certain edge cases are not handled, but a state machine forces all cases to be thought about. 

For example, what should happen if we try to verify an already verified email? 
What happens if we try to remove an item from an empty shopping cart? 
What happens if white tries to play when the state is "BlackToPlay"? And so on.


## How to implement simple state machines in F# ##

You are probably familiar with complex state machines, such as those used in language parsers and regular expressions.  Those kinds of state machines are generated from rule sets or grammars, and are quite complicated.

The kinds of state machines that I'm talking about are much, much simpler. Just a few cases at the most, with a small number of transitions, so we don't need to use complex generators.

So what is the best way implement these simple state machines?

Typically, each state will have its own type, to store the data that is relevant to that state (if any), and then the entire set of states will be represented by a union class. 

Here's an example using the shopping cart state machine:

```fsharp
type ActiveCartData = { UnpaidItems: string list }
type PaidCartData = { PaidItems: string list; Payment: float }

type ShoppingCart = 
    | EmptyCart  // no data
    | ActiveCart of ActiveCartData
    | PaidCart of PaidCartData
```     

Note that the `EmptyCart` state has no data, so no special type is needed.

Each event is then represented by a function that accepts the entire state machine (the union type) and returns a new version of the state machine (again, the union type).

Here's an example using two of the shopping cart events:

```fsharp
let addItem cart item = 
    match cart with
    | EmptyCart -> 
        // create a new active cart with one item
        ActiveCart {UnpaidItems=[item]}
    | ActiveCart {UnpaidItems=existingItems} -> 
        // create a new ActiveCart with the item added
        ActiveCart {UnpaidItems = item :: existingItems}
    | PaidCart _ ->  
        // ignore
        cart

let makePayment cart payment = 
    match cart with
    | EmptyCart -> 
        // ignore
        cart
    | ActiveCart {UnpaidItems=existingItems} -> 
        // create a new PaidCart with the payment
        PaidCart {PaidItems = existingItems; Payment=payment}
    | PaidCart _ ->  
        // ignore
        cart
```     

You can see that from the caller's point of view, the set of states is treated as "one thing" for general manipulation (the `ShoppingCart` type), but when processing the events internally, each state is treated separately.

### Designing event handling functions 

Guideline: *Event handling functions should always accept and return the entire state machine*

You might ask: why do we have to pass in the whole shopping cart to the event-handling functions? For example, the `makePayment` event only has relevance when the cart is in the Active state, so why not just explicitly pass it the ActiveCart type, like this:

```fsharp
let makePayment2 activeCart payment = 
    let {UnpaidItems=existingItems} = activeCart
    {PaidItems = existingItems; Payment=payment}
```     

Let's compare the function signatures:

```fsharp
// the original function 
val makePayment : ShoppingCart -> float -> ShoppingCart

// the new more specific function
val makePayment2 :  ActiveCartData -> float -> PaidCartData
```     

You will see that the original `makePayment` function takes a cart and results in a cart, while the new function takes an `ActiveCartData` and results in a `PaidCartData`, which seems to be much more relevant.

But if you did this, how would you handle the same event when the cart was in a different state, such as empty or paid?  Someone has to handle the event for all three possible states somewhere, and it is much better to encapsulate this business logic inside the function than to be at the mercy of the caller.

### Working with "raw" states

Occasionally you do genuinely need to treat one of the states as a separate entity in its own right and use it independently. Because each state is a type as well, this is normally straightforward.  

For example, if I need to report on all paid carts, I can pass it a list of `PaidCartData`.

```fsharp
let paymentReport paidCarts = 
    let printOneLine {Payment=payment} = 
        printfn "Paid %f for items" payment
    paidCarts |> List.iter printOneLine
```     

By using a list of `PaidCartData` as the parameter rather than `ShoppingCart` itself, I ensure that I cannot accidentally report on unpaid carts.

If you do this, it should be in a supporting function to the event handlers, never the event handlers themselves.

<a name="replace-flags"></a>  


## Using explicit states to replace boolean flags ##

Let's look at how we can apply this approach to a real example now.  

In the `Contact` example from an [earlier post](/posts/designing-with-types-intro/) we had a flag that was used to indicate whether a customer had verified their email address. 
The type looked like this:

```fsharp
type EmailContactInfo = 
    {
    EmailAddress: EmailAddress.T;
    IsEmailVerified: bool;
    }
```     

Any time you see a flag like this, chances are you are dealing with state. In this case, the boolean is used to indicate that we have two states: "Unverified" and "Verified". 

As mentioned above, there will probably be various business rules associated with what is permissible in each state. For example, here are two:

* Business rule: *"Verification emails should only be sent to customers who have unverified email addresses"*
* Business rule: *"Password reset emails should only be sent to customers who have verified email addresses"*

As before, we can use types to ensure that code conforms to these rules.

Let's rewrite the `EmailContactInfo` type using a state machine. We'll put it in an module as well.

We'll start by defining the two states. 

* For the "Unverified" state, the only data we need to keep is the email address. 
* For the "Verified" state, we might want to keep some extra data in addition to the email address, such as the date it was verified, the number of recent password resets, on so on. This data is not relevant (and should not even be visible) to the "Unverified" state.

```fsharp
module EmailContactInfo = 
    open System

    // placeholder
    type EmailAddress = string

    // UnverifiedData = just the email
    type UnverifiedData = EmailAddress

    // VerifiedData = email plus the time it was verified
    type VerifiedData = EmailAddress * DateTime 

    // set of states
    type T = 
        | UnverifiedState of UnverifiedData
        | VerifiedState of VerifiedData

```

Note that for the `UnverifiedData` type I just used a type alias. No need for anything more complicated right now, but using a type alias makes the purpose explicit and helps with refactoring.

Now let's handle the construction of a new state machine, and then the events.

* Construction *always* results in an unverified email, so that is easy.
* There is only one event that transitions from one state to another: the "verified" event.

```fsharp
module EmailContactInfo = 

    // types as above
    
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
```

Note that, as [discussed here](/posts/match-expression/), every branch of the match must return the same type, so when ignoring the verified state we must still return something, such as the object that was passed in.

Finally, we can write the two utility functions `sendVerificationEmail` and `sendPasswordReset`.

```fsharp
module EmailContactInfo = 

    // types and functions as above
    
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
```

{% include book_page_ddd.inc %}


## Using explicit cases to replace case/switch statements ##

Sometimes it is not just a simple boolean flag that is used to indicate state.  In C# and Java it is common to use a `int` or an `enum` to represent a set of states.

For example, here's a simple state diagram of a package status for a delivery system, where a package has three possible states:

![State transition diagram: Package Delivery](/assets/img/State_Delivery.png)

There are some obvious business rules that come out of this diagram:

* *Rule: "You can't put a package on a truck if it is already out for delivery"*
* *Rule: "You can't sign for a package that is already delivered"*

and so on.

Now, without using union types, we might represent this design by using an enum to represent the state, like this:

```fsharp
open System

type PackageStatus = 
    | Undelivered
    | OutForDelivery
    | Delivered

type Package = 
    {
    PackageId: int;
    PackageStatus: PackageStatus;
    DeliveryDate: DateTime;
    DeliverySignature: string;
    }
```

And then the code to handle the "putOnTruck" and "signedFor" events might look like this:

```fsharp
let putOnTruck package = 
    {package with PackageStatus=OutForDelivery}

let signedFor package signature = 
    let {PackageStatus=packageStatus} = package 
    if (packageStatus = Undelivered) 
    then 
        failwith "package not out for delivery"
    else if (packageStatus = OutForDelivery) 
    then 
        {package with 
            PackageStatus=OutForDelivery;
            DeliveryDate = DateTime.UtcNow;
            DeliverySignature=signature;
            }
    else
        failwith "package already delivered"
```

This code has some subtle bugs in it.

* When handling the "putOnTruck" event, what should happen in the case that the status is *already* `OutForDelivery` or `Delivered`. The code is not explicit about it.
* When handling the "signedFor" event, we do handle the other states, but the last else branch assumes that we only have three states, and therefore doesn't bother to be explicit about testing for it. This code would be incorrect if we ever added a new status.
* Finally, because the `DeliveryDate` and `DeliverySignature` are in the basic structure, it would be possible to set them accidentally, even though the status was not `Delivered`.

But as usual, the idiomatic and more type-safe F# approach is to use an overall union type rather than embed a status value inside a data structure.

```fsharp
open System

type UndeliveredData = 
    {
    PackageId: int;
    }

type OutForDeliveryData = 
    {
    PackageId: int;
    }

type DeliveredData = 
    {
    PackageId: int;
    DeliveryDate: DateTime;
    DeliverySignature: string;
    }

type Package = 
    | Undelivered of UndeliveredData 
    | OutForDelivery of OutForDeliveryData
    | Delivered of DeliveredData 
```

And then the event handlers *must* handle every case.

```fsharp
let putOnTruck package = 
    match package with
    | Undelivered {PackageId=id} ->
        OutForDelivery {PackageId=id}
    | OutForDelivery _ ->
        failwith "package already out"
    | Delivered _ ->
        failwith "package already delivered"

let signedFor package signature = 
    match package with
    | Undelivered _ ->
        failwith "package not out"
    | OutForDelivery {PackageId=id} ->
        Delivered {
            PackageId=id; 
            DeliveryDate = DateTime.UtcNow;
            DeliverySignature=signature;
            }
    | Delivered _ ->
        failwith "package already delivered"
```

*Note: I am using `failWith` to handle the errors. In a production system, this code should be replaced by client driven error handlers.
See the discussion of handling constructor errors in the [post about single case DUs](/posts/designing-with-types-single-case-dus/) for some ideas.*

## Using explicit cases to replace implicit conditional code ##

Finally, there are often cases where a system has states, but they are implicit in conditional code. 

For example, here is a type that represents an order. 

```fsharp
open System

type Order = 
    {
    OrderId: int;
    PlacedDate: DateTime;
    PaidDate: DateTime option;
    PaidAmount: float option;
    ShippedDate: DateTime option;
    ShippingMethod: string option;
    ReturnedDate: DateTime option;
    ReturnedReason: string option;
    }
```

You can guess that Orders can be "new", "paid", "shipped" or "returned", and have timestamps and extra information for each transition, but this is not made explicit in the structure.

The option types are a clue that this type is trying to do too much.  At least F# forces you to use options -- in C# or Java these might just be nulls, and you would have no idea from the type definition whether they were required or not. 

And now let's look at the kind of ugly code that might test these option types to see what state the order is in.

Again, there is some important business logic that depends on the state of the order, but nowhere is it explicitly documented what the various states and transitions are.

```fsharp
let makePayment order payment = 
    if (order.PaidDate.IsSome)
    then failwith "order is already paid"
    //return an updated order with payment info
    {order with 
        PaidDate=Some DateTime.UtcNow
        PaidAmount=Some payment
        }

let shipOrder order shippingMethod = 
    if (order.ShippedDate.IsSome)
    then failwith "order is already shipped"
    //return an updated order with shipping info
    {order with 
        ShippedDate=Some DateTime.UtcNow
        ShippingMethod=Some shippingMethod
        }
```

*Note: I added `IsSome` to test for option values being present as a direct port of the way that a C# program would test for `null`. But `IsSome` is both ugly and dangerous.  Don't use it!*

Here is a better approach using types that makes the states explicit.

```fsharp
open System

type InitialOrderData = 
    {
    OrderId: int;
    PlacedDate: DateTime;
    }
type PaidOrderData = 
    {
    Date: DateTime;
    Amount: float;
    }
type ShippedOrderData = 
    {
    Date: DateTime;
    Method: string;
    }
type ReturnedOrderData = 
    {
    Date: DateTime;
    Reason: string;
    }

type Order = 
    | Unpaid of InitialOrderData 
    | Paid of InitialOrderData * PaidOrderData
    | Shipped of InitialOrderData * PaidOrderData * ShippedOrderData
    | Returned of InitialOrderData * PaidOrderData * ShippedOrderData * ReturnedOrderData
```

And here are the event handling methods:

```fsharp
let makePayment order payment = 
    match order with
    | Unpaid i -> 
        let p = {Date=DateTime.UtcNow; Amount=payment}
        // return the Paid order
        Paid (i,p)
    | _ ->
        printfn "order is already paid"
        order

let shipOrder order shippingMethod = 
    match order with
    | Paid (i,p) -> 
        let s = {Date=DateTime.UtcNow; Method=shippingMethod}
        // return the Shipped order
        Shipped (i,p,s)
    | Unpaid _ ->
        printfn "order is not paid for"
        order
    | _ ->
        printfn "order is already shipped"
        order
```

*Note: Here I am using `printfn` to handle the errors. In a production system, do use a different approach.*


## When not to use this approach  

As with any technique we learn, we have to be careful of treating it like a [golden hammer](http://en.wikipedia.org/wiki/Law_of_the_instrument).

This approach does add complexity, so before you start using it, be sure that benefits will outweigh the costs.

To recap, here are the conditions where using simple state machines might be benficial:

* You have a set of mutually exclusive states with transitions between them.
* The transitions are triggered by external events. 
* The states are exhaustive. That is, there are no other choices and you must always handle all cases.
* Each state might have associated data that should not be accessable when the system is in another state.
* There are static business rules that apply to the states.

Let's look at some examples where these guidelines *don't* apply.

**States are not important in the domain.**

Consider a blog authoring application. Typically, each blog post can be in a state such as "Draft", "Published", etc. And there are obviously transitions between these states driven by events (such as clicking a "publish" button).

But is it worth creating a state machine for this? Generally, I would say not.

Yes, there are state transitions, but is there really any change in logic because of this?  From the authoring point of view, most blogging apps don't have any restrictions based on the state.
You can author a draft post in exactly the same way as you author a published post.

The only part of the system that *does* care about the state is the display engine, and that filters out the drafts in the database layer before it ever gets to the domain. 

Since there is no special domain logic that cares about the state, it is probably unnecessary.

**State transitions occur outside the application**

In a customer management application, it is common to classify customers as "prospects", "active", "inactive", etc.

![State transition diagram: Customer states](/assets/img/State_Customer.png)

In the application, these states have business meaning and should be represented by the type system (such as a union type).  But the state *transitions* generally do not occur within the application itself. For example, we might classify a customer as inactive if they haven't ordered anything for 6 months. And then this rule might be applied to customer records in a database by a nightly batch job, or when the customer record is loaded from the database.  But from our application's point of view, the transitions do not happen *within* the application, and so we do not need to create a special state machine.

**Dynamic business rules**

The last bullet point in the list above refers to "static" business rules. By this I mean that the rules change slowly enough that they should be embedded into the code itself. 

On the other hand, if the rules are dynamic and change frequently, it is probably not worth going to the trouble of creating static types.

In these situations, you should consider using active patterns, or even a proper rules engine.

## Summary

In this post, we've seen that if you have data structures with explicit flags ("IsVerified") or status fields ("OrderStatus"), or implicit state (clued by an excessive number of nullable or option types), it is worth considering using a simple state machine to model the domain objects.  In most cases the extra complexity is compensated for by the explicit documention of the states and the elimination of errors due to not handling all possible cases.