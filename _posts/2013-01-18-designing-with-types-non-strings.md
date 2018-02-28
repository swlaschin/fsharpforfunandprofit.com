---
layout: post
title: "Designing with types: Non-string types"
description: "Working with integers and dates safely"
nav: thinking-functionally
seriesId: "Designing with types"
seriesOrder: 7
categories: [Types, DDD]
---

In this series we've seen a lot of uses of single case discriminated unions to wrap strings.

There is no reason why you cannot use this technique with other primitive types, such as numbers and dates.  Let's look a few examples.

## Single case unions

In many cases, we want to avoid accidentally mixing up different kinds of integers. Two domain objects may have the same representation (using integers) but they should never be confused.

For example, you may have an `OrderId` and a `CustomerId`, both of which are stored as ints. But they are not *really* ints. You cannot add 42 to a `CustomerId`, for example. 
And `CustomerId(42)` is not equal to `OrderId(42)`. In fact, they should not even be allowed to be compared at all.

Types to the rescue, of course.

```fsharp
type CustomerId = CustomerId of int
type OrderId = OrderId of int

let custId = CustomerId 42
let orderId = OrderId 42

// compiler error
printfn "cust is equal to order? %b" (custId = orderId) 
```

Similarly, you might want avoid mixing up semantically different date values by wrapping them in a type. (`DateTimeKind` is an attempt at this, but not always reliable.)

```fsharp
type LocalDttm = LocalDttm of System.DateTime
type UtcDttm = UtcDttm of System.DateTime
```

With these types we can ensure that we always pass the right kind of datetime as parameters. Plus, it acts as documentation as well.

```fsharp
let SetOrderDate (d:LocalDttm) = 
    () // do something

let SetAuditTimestamp (d:UtcDttm) = 
    () // do something
```

## Constraints on integers

Just as we had validation and constraints on types such as `String50` and `ZipCode`, we can use the same approach when we need to have constraints on integers.

For example, an inventory management system or a shopping cart may require that certain types of number are always positive.  You might ensure this by creating a `NonNegativeInt` type.

```fsharp
module NonNegativeInt = 
    type T = NonNegativeInt of int

    let create i = 
        if (i >= 0 )
        then Some (NonNegativeInt i)
        else None

module InventoryManager = 

    // example of NonNegativeInt in use
    let SetStockQuantity (i:NonNegativeInt.T) = 
        //set stock
        ()
```

## Embedding business rules in the type

Just as we wondered earlier whether first names could ever be 64K characters long, can you really add 999999 items to your shopping cart?  

![State transition diagram: Package Delivery](/assets/img/AddToCart.png)

Is it worth trying to avoid this issue by using constrained types? Let's look at some real code.  

Here is a very simple shopping cart manager using a standard `int` type for the quantity. The quantity is incremented or decremented when the related buttons are clicked. Can you find the obvious bug?

```fsharp
module ShoppingCartWithBug = 

    let mutable itemQty = 1  // don't do this at home!

    let incrementClicked() = 
        itemQty <- itemQty + 1

    let decrementClicked() = 
        itemQty <- itemQty - 1
```

If you can't quickly find the bug, perhaps you should consider making any constraints more explicit. 

Here is the same simple shopping cart manager using a typed quantity instead. Can you find the bug now?  (Tip: paste the code into a F# script file and run it) 

```fsharp
module ShoppingCartQty = 

    type T = ShoppingCartQty of int

    let initialValue = ShoppingCartQty 1

    let create i = 
        if (i > 0 && i < 100)
        then Some (ShoppingCartQty i)
        else None

    let increment t = create (t + 1)
    let decrement t = create (t - 1)

module ShoppingCartWithTypedQty = 

    let mutable itemQty = ShoppingCartQty.initialValue

    let incrementClicked() = 
        itemQty <- ShoppingCartQty.increment itemQty

    let decrementClicked() = 
        itemQty <- ShoppingCartQty.decrement itemQty
```

You might think this is overkill for such a trivial problem. But if you want to avoid being in the DailyWTF, it might be worth considering.

{% include book_page_ddd.inc %}

## Constraints on dates

Not all systems can handle all possible dates. Some systems can only store dates going back to 1/1/1980, and some systems can only go into the future up to 2038 (I like to use 1/1/2038 as a max date to avoid US/UK issues with month/day order).

As with integers, it might be useful to have constraints on the valid dates built into the type, so that any out of bound issues are dealt with at construction time rather than later on.

```fsharp
type SafeDate = SafeDate of System.DateTime

let create dttm = 
    let min = new System.DateTime(1980,1,1)
    let max = new System.DateTime(2038,1,1)
    if dttm < min || dttm > max
    then None
    else Some (SafeDate dttm)
```


## Union types vs. units of measure

You might be asking at this point: What about [units of measure](/posts/units-of-measure/)? Aren't they meant to be used for this purpose?

Yes and no.  Units of measure can indeed be used to avoid mixing up numeric values of different type, and are much more powerful than the single case unions we've been using.

On the other hand, units of measure are not encapsulated and cannot have constraints. Anyone can create a int with unit of measure `<kg>` say, and there is no min or max value.

In many cases, both approaches will work fine.  For example, there are many parts of the .NET library that use timeouts, but sometimes the timeouts are set in seconds, and sometimes in milliseconds.
I often have trouble remembering which is which. I definitely don't want to accidentally use a 1000 second timeout when I really meant a 1000 millisecond timeout.

To avoid this scenario, I often like to create separate types for seconds and milliseconds.

Here's a type based approach using single case unions:

```fsharp
type TimeoutSecs = TimeoutSecs of int
type TimeoutMs = TimeoutMs of int

let toMs (TimeoutSecs secs)  = 
    TimeoutMs (secs * 1000)

let toSecs (TimeoutMs ms) = 
    TimeoutSecs (ms / 1000)

/// sleep for a certain number of milliseconds
let sleep (TimeoutMs ms) = 
    System.Threading.Thread.Sleep ms

/// timeout after a certain number of seconds    
let commandTimeout (TimeoutSecs s) (cmd:System.Data.IDbCommand) = 
    cmd.CommandTimeout <- s
```

And here's the same thing using units of measure:

```fsharp
[<Measure>] type sec 
[<Measure>] type ms

let toMs (secs:int<sec>) = 
    secs * 1000<ms/sec>

let toSecs (ms:int<ms>) = 
    ms / 1000<ms/sec>

/// sleep for a certain number of milliseconds
let sleep (ms:int<ms>) = 
    System.Threading.Thread.Sleep (ms * 1<_>)

/// timeout after a certain number of seconds    
let commandTimeout (s:int<sec>) (cmd:System.Data.IDbCommand) = 
    cmd.CommandTimeout <- (s * 1<_>)
```

Which approach is better?

If you are doing lots of arithmetic on them (adding, multiplying, etc) then the units of measure approach is much more convenient, but otherwise there is not much to choose between them.  


