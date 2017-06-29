---
layout: post
title: "Swapping type-safety for high performance using compiler directives"
description: "An experiment in how to have your cake and eat it too"
categories: []
---

*TL;DR; An experiment: you can use lots of domain modelling types at development time and swap them out for a more performant implementation later using compiler directives.*

## Domain Modelling vs. Performance

I am a big fan of using [types for domain modelling](/ddd/) -- lots and lots and *lots* of types!

These types act both as documentation and as a compile time constraint to ensure that only valid data is used.

For example, if I have two types `CustomerId` and `OrderId`, I can represent them as separate types:

```fsharp
type CustomerId = CustomerId of int
type OrderId = OrderId of int
```

and by doing this, I guarantee that I can't use an `OrderId` where I need an `CustomerId`.

The problem is that adding a layer of indirection like this can affect performance:

* the extra indirection can cause data access to be much slower.
* the wrapper class needs extra memory, creating memory pressure.
* this in turn triggers the garbage collector more often, which can often be the cause of performance problems in managed code.

In general, I don't generally worry about micro-performance like this at design-time.
Many many things will have a *much* bigger impact on performance, including any kind of I/O, and the algorithms you choose.

As a result, I am very much *against* doing micro-benchmarks out of context. You should always profile a real app in a real context,
rather than worrying too much over things that might not be important.

Having said that, I am now going to do some micro-benchmarks!

### Micro-benchmarking a wrapper type

Let's see how a wrapper type fares when used in large numbers. Let's say we want to:

* create ten million customer ids 
* then, map over them twice
* then, filter them

Admittedly, it's a bit silly adding 1 to a customer id -- we'll look at a better example later.

Anyway, here's the code:

```fsharp
// type is an wrapper for a primitive type
type CustomerId = CustomerId of int

// create two silly functions for mapping and filtering 
let add1ToCustomerId (CustomerId i) = 
    CustomerId (i+1)

let isCustomerIdSmall (CustomerId i) = 
    i < 100000

// ---------------------------------
// timed with a 1 million element array
// ---------------------------------
#time
Array.init 1000000 CustomerId
// map it 
|> Array.map add1ToCustomerId 
// map it again
|> Array.map add1ToCustomerId 
// filter it 
|> Array.filter isCustomerIdSmall 
|> ignore
#time
```

*The code sample above is [available on GitHub](https://gist.github.com/swlaschin/348b6b9e64d4b150cf86#file-typesafe-performance-with-compiler-directives-1-fsx)*.

*(Again, let me stress that this is a terrible way to profile code!)*

A typical timed result looks like this:

```text
Real: 00:00:00.296, CPU: 00:00:00.296, GC gen0: 6, gen1: 4, gen2: 0
```

That is, it takes about 0.3 seconds to do those steps, and it creates quite a bit of garbage, triggering four gen1 collections.
If you are not sure what "gen0", "gen1", and "gen2" mean, then [this is a good place to start](https://msdn.microsoft.com/en-us/library/ms973837.aspx).

*DISCLAIMER: I'm going to be doing all my benchmarking in F# interactive. Compiled code with optimizations might have a completely different performance profile.
Past performance is no guarantee of future results. Draw conclusions at your own risk. Etc., etc.*

If we increase the array size to 10 million, we get a more than 10x slower result:

```text
Real: 00:00:03.489, CPU: 00:00:03.541, GC gen0: 68, gen1: 46, gen2: 2
```

That is, it takes about 3.5 seconds to do those steps, and it creates *a lot* of garbage, including a few gen2 GC's, which are really bad. 
In fact, you might even get an "out of memory" exception, in which case, you'll have to restart F# Interactive! 

So what are the alternatives to using a wrapper?  There are two common approaches:

* Using type aliases
* Using units of measure

Let's start with type aliases.

## Using type aliases

In the type alias approach, I would simply dispense with the wrapper, but keep the type around as documentation.

```fsharp
type CustomerId = int
type OrderId = int
```

If I want to use the type as documentation, I must then annotate the functions appropriately.

For example, in the `add1ToCustomerId` below
both the parameter and the return value have been annotated so that it has the type `CustomerId -> CustomerId` rather than `int -> int`.

```fsharp
let add1ToCustomerId (id:CustomerId) :CustomerId = 
    id+1
```

### Micro-benchmarking a type alias

Let's create another micro-benchmark:

```fsharp
type CustomerId = int

// create two silly functions for mapping and filtering 
let add1ToCustomerId (id:CustomerId) :CustomerId = 
    id+1
// val add1ToCustomerId : id:CustomerId -> CustomerId

let isCustomerIdSmall (id:CustomerId) = 
    id < 100000
// val isCustomerIdSmall : id:CustomerId -> bool

// ---------------------------------
// timed with a 1 million element array
// ---------------------------------
#time
Array.init 1000000 (fun i -> i)
// map it 
|> Array.map add1ToCustomerId 
// map it again
|> Array.map add1ToCustomerId 
// filter it 
|> Array.filter isCustomerIdSmall 
|> Array.length
#time
```

*The code sample above is [available on GitHub](https://gist.github.com/swlaschin/348b6b9e64d4b150cf86#file-typesafe-performance-with-compiler-directives-2-fsx)*.

The results are spectacularly better!

```text
Real: 00:00:00.017, CPU: 00:00:00.015, GC gen0: 0, gen1: 0, gen2: 0
```

It takes about 17 milliseconds to do those steps, and more importantly, very little garbage was generated. 

If we increase the array size to 10 million, we get a 10x slower result, but still no garbage:

```text
Real: 00:00:00.166, CPU: 00:00:00.156, GC gen0: 0, gen1: 0, gen2: 0
```

Compared with the earlier version at over three seconds, that's excellent.

### Problems with type aliases

Alas, the problem with type aliases is that we have completely lost type safety now! 

To demonstrate, here's some code that creates a `CustomerId` and an `OrderId`:

```fsharp
type CustomerId = int
type OrderId = int

// create two
let cid : CustomerId = 12
let oid : OrderId = 12
```

And sadly, the two ids compare equal, and we can pass an `OrderId` to function expecting a `CustomerId` without any complaint from the compiler.

```fsharp
cid = oid              // true

// pass OrderId to function expecting a CustomerId 
add1ToCustomerId oid   // CustomerId = 13
```

Ok, so that doesn't look promising! What next?

## Using units of measure

The other common option is to use units of measure to distinguish the two types, like this:

```fsharp
type [<Measure>] CustomerIdUOM 
type [<Measure>] OrderIdUOM 

type CustomerId = int<CustomerIdUOM>
type OrderId = int<OrderIdUOM>
```

`CustomerId` and `OrderId` are still two different types, but the unit of measure is erased, so by the time the JIT sees it the type looks like an primitive int.

We can see that this is true when we time the same steps as before:

```fsharp
// create two silly functions for mapping and filtering 
let add1ToCustomerId id  = 
    id+1<CustomerIdUOM>

let isCustomerIdSmall i = 
    i < 100000<CustomerIdUOM>

// ---------------------------------
// timed with a 1 million element array
// ---------------------------------
#time
Array.init 1000000 (fun i -> LanguagePrimitives.Int32WithMeasure<CustomerIdUOM> i)
// map it 
|> Array.map add1ToCustomerId 
// map it again
|> Array.map add1ToCustomerId 
// filter it 
|> Array.filter isCustomerIdSmall 
|> ignore
#time
```

*The code sample above is [available on GitHub](https://gist.github.com/swlaschin/348b6b9e64d4b150cf86#file-typesafe-performance-with-compiler-directives-3-fsx)*.

A typical timed result looks like this:

```text
Real: 00:00:00.022, CPU: 00:00:00.031, GC gen0: 0, gen1: 0, gen2: 0
```

Again, the code is very fast (22 milliseconds), and just as importantly, very little garbage was generated again. 

If we increase the array size to 10 million, we maintain the high performance (just as with the type alias approach) and still no garbage:

```text
Real: 00:00:00.157, CPU: 00:00:00.156, GC gen0: 0, gen1: 0, gen2: 0
```

### Problems with units of measure

The advantage of units of measure is that the `CustomerId` and `OrderId` types are incompatible, so we get the type safety that we want.

But I find them unsatisfactory from an esthetic point of view. I like my wrapper types! 

And also, units of measure are really meant to be used with numeric values. For example, I can create a customer id and order id:

```fsharp
let cid = 12<CustomerIdUOM>
let oid = 4<OrderIdUOM>
```

and then I can divide CustomerId(12) by OrderId(4) to get three...

```fsharp
let ratio = cid / oid
// val ratio : int<CustomerIdUOM/OrderIdUOM> = 3
```

Three what though? Three customer ids per order id?  What does that even mean?

Yes, surely this will never happen in practice, but still it bothers me!

## Using compiler directives to get the best of both worlds

Did I mention that I really like wrapper types? I really like them up until I get a call saying that production systems are having performance hiccups because of too many big GCs.

So, can we get the best of both worlds? Type-safe wrapper types AND fast performance?

I think so, if you are willing to put up with some extra work during development and build.

The trick is to have *both* the "wrapper type" implemention and the "type alias" implementation available to you, and then switch between them based on a compiler directive.

For this to work:

* you will need to tweak your code to not access the type directly, but only via functions and pattern matching.
* you will need to create a "type alias" implementation that implements a "constructor", various "getters" and for pattern matching, active patterns.

Here's an example, using the `COMPILED` and `INTERACTIVE` directives so that you can play with it interactively.
Obviously, in real code, you would use your own directive such as `FASTTYPES` or similar.

```fsharp
#if COMPILED  // uncomment to use aliased version   
//#if INTERACTIVE // uncomment to use wrapped version

// type is an wrapper for a primitive type
type CustomerId = CustomerId of int

// constructor
let createCustomerId i = CustomerId i

// get data
let customerIdValue (CustomerId i) = i

// pattern matching
// not needed

#else
// type is an alias for a primitive type
type CustomerId = int

// constructor
let inline createCustomerId i :CustomerId = i

// get data
let inline customerIdValue (id:CustomerId) = id

// pattern matching
let inline (|CustomerId|) (id:CustomerId) :int = id

#endif
```

You can see that for both versions I've created a constructor `createCustomerId` and a getter `customerIdValue` and, for the type alias version, an active pattern that looks just like `CustomerId`.

With this code in place, we can use `CustomerId` without caring about the implementation:

```fsharp
// test the getter
let testGetter c1 c2 =
    let i1 = customerIdValue c1
    let i2 = customerIdValue c2
    printfn "Get inner value from customers %i %i" i1 i2
// Note that the signature is as expected:
// c1:CustomerId -> c2:CustomerId -> unit

// test pattern matching
let testPatternMatching c1 =
    let (CustomerId i) = c1
    printfn "Get inner value from Customers via pattern match: %i" i

    match c1 with
    | CustomerId i2 -> printfn "match/with %i" i
// Note that the signature is as expected:
// c1:CustomerId -> unit

let test() = 
    // create two ids
    let c1 = createCustomerId 1
    let c2 = createCustomerId 2
    let custArray : CustomerId [] = [| c1; c2 |]
    
    // test them
    testGetter c1 c2 
    testPatternMatching c1 
```

And now we can run the *same* micro-benchmark with both implementations:

```fsharp
// create two silly functions for mapping and filtering 
let add1ToCustomerId (CustomerId i) = 
    createCustomerId (i+1)

let isCustomerIdSmall (CustomerId i) = 
    i < 100000

// ---------------------------------
// timed with a 1 million element array
// ---------------------------------
#time
Array.init 1000000 createCustomerId
// map it 
|> Array.map add1ToCustomerId 
// map it again
|> Array.map add1ToCustomerId 
// filter it 
|> Array.filter isCustomerIdSmall 
|> Array.length
#time
```

*The code sample above is [available on GitHub](https://gist.github.com/swlaschin/348b6b9e64d4b150cf86#file-typesafe-performance-with-compiler-directives-4-fsx)*.

The results are similar to the previous examples. The aliased version is much faster and does not create GC pressure:

```text
// results using wrapped version
Real: 00:00:00.408, CPU: 00:00:00.405, GC gen0: 7, gen1: 4, gen2: 1

// results using aliased version
Real: 00:00:00.022, CPU: 00:00:00.031, GC gen0: 0, gen1: 0, gen2: 0
```

and for the 10 million element version:

```text
// results using wrapped version
Real: 00:00:03.199, CPU: 00:00:03.354, GC gen0: 67, gen1: 45, gen2: 2

// results using aliased version
Real: 00:00:00.239, CPU: 00:00:00.202, GC gen0: 0, gen1: 0, gen2: 0
```

## A more complex example

In practice, we might want something more complex than a simple wrapper.

For example, here is an `EmailAddress` (a simple wrapper type, but constrained to be non-empty and containing a "@") and
some sort of `Activity` record that stores an email and the number of visits, say.

```fsharp
module EmailAddress =
    // type with private constructor 
    type EmailAddress = private EmailAddress of string

    // safe constructor
    let create s = 
        if System.String.IsNullOrWhiteSpace(s) then 
            None
        else if s.Contains("@") then
            Some (EmailAddress s)
        else
            None

    // get data
    let value (EmailAddress s) = s

module ActivityHistory =
    open EmailAddress
    
    // type with private constructor 
    type ActivityHistory = private {
        emailAddress : EmailAddress
        visits : int
        }

    // safe constructor
    let create email visits = 
        {emailAddress = email; visits = visits }

    // get data
    let email {emailAddress=e} = e
    let visits {visits=a} = a
```

As before, for each type there is a constructor and a getter for each field.

*NOTE: Normally I would define a type outside a module, but because the real constructor needs to be private,
I've put the type inside the module and given the module and the type the same name. If this is too awkward, you can rename the module to be different
from the type, or use the OCaml convention of calling the main type in a module just "T", so you get `EmailAddress.T` as the type name.*

To make a more performant version, we replace `EmailAddress` with a type alias, and `Activity` with a struct, like this:

```fsharp
module EmailAddress =

    // aliased type 
    type EmailAddress = string

    // safe constructor
    let inline create s :EmailAddress option = 
        if System.String.IsNullOrWhiteSpace(s) then 
            None
        else if s.Contains("@") then
            Some s
        else
            None

    // get data
    let inline value (e:EmailAddress) :string = e

module ActivityHistory =
    open EmailAddress
    
    [<Struct>]
    type ActivityHistory(emailAddress : EmailAddress, visits : int) = 
        member this.EmailAddress = emailAddress 
        member this.Visits = visits 

    // safe constructor
    let create email visits = 
        ActivityHistory(email,visits)

    // get data
    let email (act:ActivityHistory) = act.EmailAddress
    let visits (act:ActivityHistory) = act.Visits

```

This version reimplements the constructor and a getter for each field.
I could have made the field names for `ActivityHistory` be the same in both cases too, but. in the struct case, type inference would not work.
By making them different, the user is forced to use the getter functions rather than dotting in.

Both implementations have the same "API", so we can create code that works with both:

```fsharp
let rand = new System.Random()

let createCustomerWithRandomActivityHistory() = 
    let emailOpt = EmailAddress.create "abc@example.com"
    match emailOpt with
    | Some email  -> 
        let visits = rand.Next(0,100) 
        ActivityHistory.create email visits 
    | None -> 
        failwith "should not happen"

let add1ToVisits activity = 
    let email = ActivityHistory.email activity
    let visits = ActivityHistory.visits activity 
    ActivityHistory.create email (visits+1)

let isCustomerInactive activity = 
    let visits = ActivityHistory.visits activity 
    visits < 3

    
// execute creation and iteration for a large number of records
let mapAndFilter noOfRecords = 
    Array.init noOfRecords (fun _ -> createCustomerWithRandomActivity() )
    // map it 
    |> Array.map add1ToVisits 
    // map it again
    |> Array.map add1ToVisits 
    // filter it 
    |> Array.filter isCustomerInactive 
    |> ignore  // we don't actually care!
```

### Pros and cons of this approach

One nice thing about this approach is that it is self-correcting -- it forces you to use the "API" properly.  

For example, if I started accessing fields directly by dotting into
the `ActivityHistory` record, then that code would break when the compiler directive was turned on and the struct implementation was used.

Of course, you could also create a signature file to enforce the API.

On the negative side, we do lose some of the nice syntax such as `{rec with ...}`.
But you should really only be using this technique with small records (2-3 fields), so not having `with` is not a big burden.

### Timing the two implementations

Rather than using `#time`, this time I wrote a custom timer that runs a function 10 times and prints out the GC and memory used on each run.

```fsharp
/// Do countN repetitions of the function f and print the 
/// time elapsed, number of GCs and change in total memory
let time countN label f  = 

    let stopwatch = System.Diagnostics.Stopwatch()
    
    // do a full GC at the start but NOT thereafter
    // allow garbage to collect for each iteration
    System.GC.Collect()  
    printfn "Started"         

    let getGcStats() = 
        let gen0 = System.GC.CollectionCount(0)
        let gen1 = System.GC.CollectionCount(1)
        let gen2 = System.GC.CollectionCount(2)
        let mem = System.GC.GetTotalMemory(false)
        gen0,gen1,gen2,mem


    printfn "======================="         
    printfn "%s (%s)" label WrappedOrAliased
    printfn "======================="         
    for iteration in [1..countN] do
        let gen0,gen1,gen2,mem = getGcStats()
        stopwatch.Restart() 
        f()
        stopwatch.Stop() 
        let gen0',gen1',gen2',mem' = getGcStats()
        // convert memory used to K
        let changeInMem = (mem'-mem) / 1000L
        printfn "#%2i elapsed:%6ims gen0:%3i gen1:%3i gen2:%3i mem:%6iK" iteration stopwatch.ElapsedMilliseconds (gen0'-gen0) (gen1'-gen1) (gen2'-gen2) changeInMem 
```

*The code sample above is [available on GitHub](https://gist.github.com/swlaschin/348b6b9e64d4b150cf86#file-typesafe-performance-with-compiler-directives-5-fsx)*.

Let's now run `mapAndFilter` with a million records in the array:

```fsharp
let size = 1000000
let label = sprintf "mapAndFilter: %i records" size 
time 10 label (fun () -> mapAndFilter size)
```
 
The results are shown below:
 
```text
=======================
mapAndFilter: 1000000 records (Wrapped)
=======================
# 1 elapsed:   820ms gen0: 13 gen1:  8 gen2:  1 mem: 72159K
# 2 elapsed:   878ms gen0: 12 gen1:  7 gen2:  0 mem: 71997K
# 3 elapsed:   850ms gen0: 12 gen1:  6 gen2:  0 mem: 72005K
# 4 elapsed:   885ms gen0: 12 gen1:  7 gen2:  0 mem: 72000K
# 5 elapsed:  6690ms gen0: 16 gen1: 10 gen2:  1 mem:-216005K
# 6 elapsed:   714ms gen0: 12 gen1:  7 gen2:  0 mem: 72003K
# 7 elapsed:   668ms gen0: 12 gen1:  7 gen2:  0 mem: 71995K
# 8 elapsed:   670ms gen0: 12 gen1:  7 gen2:  0 mem: 72001K
# 9 elapsed:  6676ms gen0: 16 gen1: 11 gen2:  2 mem:-215998K
#10 elapsed:   712ms gen0: 13 gen1:  7 gen2:  0 mem: 71998K

=======================
mapAndFilter: 1000000 records (Aliased)
=======================
# 1 elapsed:   193ms gen0:  7 gen1:  0 gen2:  0 mem: 25325K
# 2 elapsed:   142ms gen0:  8 gen1:  0 gen2:  0 mem: 23779K
# 3 elapsed:   143ms gen0:  8 gen1:  0 gen2:  0 mem: 23761K
# 4 elapsed:   138ms gen0:  8 gen1:  0 gen2:  0 mem: 23745K
# 5 elapsed:   135ms gen0:  7 gen1:  0 gen2:  0 mem: 25327K
# 6 elapsed:   135ms gen0:  8 gen1:  0 gen2:  0 mem: 23762K
# 7 elapsed:   137ms gen0:  8 gen1:  0 gen2:  0 mem: 23755K
# 8 elapsed:   140ms gen0:  8 gen1:  0 gen2:  0 mem: 23777K
# 9 elapsed:   174ms gen0:  7 gen1:  0 gen2:  0 mem: 25327K
#10 elapsed:   180ms gen0:  8 gen1:  0 gen2:  0 mem: 23762K
```

Now this code no longer consists of only value types, so the profiling is getting muddier now!
The `mapAndFilter` function uses `createCustomerWithRandomActivity` which in turn uses `Option`, a reference type,
so there will be a large number of reference types being allocated. Just as in real life, it's hard to keep things pure!

Even so, you can see that the wrapped version is slower than the aliased version (approx 800ms vs. 150ms) and creates more garbage on each iteration (approx 72Mb vs 24Mb)
and most importantly has two big GC pauses (in the 5th and 9th iterations), while the aliased version never even does a gen1 GC, let alone a gen2.

*NOTE: The fact that aliased version is using up memory and yet there are no gen1s makes me a bit suspicious of these figures. I think they might be different if run outside of 
F# interactive.*

## What about non-record types?

What if the type we want to optimise is a discriminated union rather than a record?  

My suggestion is to turn the DU into a struct with a tag for each case, and fields for all possible data.

For example, let's say that we have DU that classifies an `Activity` into `Active` and `Inactive`, and for the `Active` case we store the email and visits and for
the inactive case we only store the email:
 
```fsharp
module Classification =
    open EmailAddress
    open ActivityHistory

    type Classification = 
        | Active of EmailAddress * int
        | Inactive of EmailAddress 

    // constructor
    let createActive email visits = 
        Active (email,visits)
    let createInactive email = 
        Inactive email

    // pattern matching
    // not needed
```

To turn this into a struct, I would do something like this:

```fsharp
module Classification =
    open EmailAddress
    open ActivityHistory
    open System

    [<Struct>]
    type Classification(isActive : bool, email: EmailAddress, visits: int) = 
        member this.IsActive = isActive 
        member this.Email = email
        member this.Visits = visits

    // constructor
    let inline createActive email visits = 
        Classification(true,email,visits)
    let inline createInactive email = 
        Classification(false,email,0)

    // pattern matching
    let inline (|Active|Inactive|) (c:Classification) = 
        if c.IsActive then 
            Active (c.Email,c.Visits)
        else
            Inactive (c.Email)
```

Note that `Visits` is not used in the `Inactive` case, so is set to a default value.

Now let's create a function that classifies the activity history, creates a `Classification` and then filters and extracts the email only for active customers.

```fsharp
open Classification

let createClassifiedCustomer activity = 
    let email = ActivityHistory.email activity
    let visits = ActivityHistory.visits activity 

    if isCustomerInactive activity then 
        Classification.createInactive email 
    else
        Classification.createActive email visits 

// execute creation and iteration for a large number of records
let extractActiveEmails noOfRecords =
    Array.init noOfRecords (fun _ -> createCustomerWithRandomActivityHistory() )
    // map to a classification
    |> Array.map createClassifiedCustomer
    // extract emails for active customers
    |> Array.choose (function
        | Active (email,visits) -> email |> Some
        | Inactive _ -> None )
    |> ignore
```

*The code sample above is [available on GitHub](https://gist.github.com/swlaschin/348b6b9e64d4b150cf86#file-typesafe-performance-with-compiler-directives-5-fsx)*.

The results of profiling this function with the two different implementations are shown below:
 
```text
=======================
extractActiveEmails: 1000000 records (Wrapped)
=======================
# 1 elapsed:   664ms gen0: 12 gen1:  6 gen2:  0 mem: 64542K
# 2 elapsed:   584ms gen0: 14 gen1:  7 gen2:  0 mem: 64590K
# 3 elapsed:   589ms gen0: 13 gen1:  7 gen2:  0 mem: 63616K
# 4 elapsed:   573ms gen0: 11 gen1:  5 gen2:  0 mem: 69438K
# 5 elapsed:   640ms gen0: 15 gen1:  7 gen2:  0 mem: 58464K
# 6 elapsed:  4297ms gen0: 13 gen1:  7 gen2:  1 mem:-256192K
# 7 elapsed:   593ms gen0: 14 gen1:  7 gen2:  0 mem: 64623K
# 8 elapsed:   621ms gen0: 13 gen1:  7 gen2:  0 mem: 63689K
# 9 elapsed:   577ms gen0: 11 gen1:  5 gen2:  0 mem: 69415K
#10 elapsed:   609ms gen0: 15 gen1:  7 gen2:  0 mem: 58480K

=======================
extractActiveEmails: 1000000 records (Aliased)
=======================
# 1 elapsed:   254ms gen0: 32 gen1:  1 gen2:  0 mem: 33162K
# 2 elapsed:   221ms gen0: 33 gen1:  0 gen2:  0 mem: 31532K
# 3 elapsed:   196ms gen0: 32 gen1:  0 gen2:  0 mem: 33113K
# 4 elapsed:   185ms gen0: 33 gen1:  0 gen2:  0 mem: 31523K
# 5 elapsed:   187ms gen0: 33 gen1:  0 gen2:  0 mem: 31532K
# 6 elapsed:   186ms gen0: 32 gen1:  0 gen2:  0 mem: 33095K
# 7 elapsed:   191ms gen0: 33 gen1:  0 gen2:  0 mem: 31514K
# 8 elapsed:   200ms gen0: 32 gen1:  0 gen2:  0 mem: 33096K
# 9 elapsed:   189ms gen0: 33 gen1:  0 gen2:  0 mem: 31531K
#10 elapsed:  3732ms gen0: 33 gen1:  1 gen2:  1 mem:-256432K
```

As before, the aliased/struct version is more performant, being faster and generating less garbage (although there was a GC pause at the end, oh dear).

## Questions

### Isn't this a lot of work, creating two implementations?

Yes! *I don't think you should do this in general.* This is just an experiment on my part.

I suggest that turning records and DUs into structs is a last resort, only done after you have eliminated all other bottlenecks first.

However, there may be a few special cases where speed and memory are critical, and then, perhaps, it might be worth doing something like this.

### What are the downsides?

In addition to all the extra work and maintenance, you mean?

Well, because the types are essentially private, we do lose some of the nice syntax available when you have access to the internals of the type,
such as `{rec with ...}`, but as I said, you should really only be using this technique with small records anyway.

More importantly, value types like structs are not a silver bullet. They have their own problems.

For example, they can be slower when passed as arguments (because of copy-by-value) and you must be careful not to [box them implicitly](http://theburningmonk.com/2015/07/beware-of-implicit-boxing-of-value-types/),
otherwise you end up doing allocations and creating garbage.  Microsoft has [guidelines on using classes vs structs](https://msdn.microsoft.com/en-us/library/ms229017.aspx),
but see also [this commentary on breaking these guidelines](http://stackoverflow.com/a/6973171/1136133) and [these rules](http://stackoverflow.com/a/598268/1136133).


### What about using shadowing?

Shadowing is used when the client wants to use a different implementation. For example, you can
switch from unchecked to checked arithmetic by opening the [Checked module](https://msdn.microsoft.com/en-us/library/ee340296.aspx).
[More details here](http://theburningmonk.com/2012/01/checked-context-in-c-and-f/).

But that would not work here -- I don't want each client to decide which version of the type they will use. That would lead to all sorts of incompatibility problems.
Also, it's not a per-module decision, it's a decision based on deployment context.

### What about more performant collection types?

I am using `array` everywhere as the collection type.  If you want other high performing collections,
check out [FSharpx.Collections](https://fsprojects.github.io/FSharpx.Collections/) or [Funq collections](https://github.com/GregRos/Funq).

### You've mixed up allocations, mapping, filtering. What about a more fine-grained analysis?

I'm trying to keep some semblage of dignity after I said that micro-benchmarking was bad!

So, yes, I deliberately created a case with mixed usage and measured it as a whole rather than benchmarking each part separately.
Your usage scenarios will obviously be different, so I don't think there's any need to go deeper.

Also, I'm doing all my benchmarking in F# interactive. Compiled code with optimizations might have a completely different performance profile.

### What other ways are there to increase performance?

Since F# is a .NET language, the performance tips for C# work for F# as well, standard stuff like:

* Make all I/O async. Use streaming IO over random access IO if possible. Batch up your requests.
* Check your algorithms. Anything worse than O(n log(n)) should be looked at.
* Don't do things twice. Cache as needed.
* Keep things in the CPU cache by keeping objects in contiguous memory and avoiding too many deep reference (pointer) chains. Things that help with this are using arrays instead of lists, value types instead of reference types, etc.
* Avoid pressure on the garbage collector by minimizing allocations. Avoid creating long-lived objects that survive gen0 collections.

To be clear, I don't claim to be an expert on .NET performance and garbage collection.  In fact, if you see something wrong with this analysis, please let me know!

Here are some sources that helped me:

* The book [Writing High-Performance .NET Code](http://www.writinghighperf.net/) by Ben Watson.
* Martin Thompson has a great [blog](http://mechanical-sympathy.blogspot.jp/2012/08/memory-access-patterns-are-important.html)
  on performance and some excellent videos, such as [Top 10 Performance Folklore](http://www.infoq.com/presentations/top-10-performance-myths).
  ([Good summary here](http://weronikalabaj.com/performance-myths-and-facts/).)
* [Understanding Latency](https://www.youtube.com/watch?v=9MKY4KypBzg), a video by Gil Tene.
* [Essential Truths Everyone Should Know about Performance in a Large Managed Codebase](https://channel9.msdn.com/Events/TechEd/NorthAmerica/2013/DEV-B333), a video by Dustin Cambell at Microsoft.
* For F# in particular:
  * Yan Cui has some blog posts on [records vs structs](http://theburningmonk.com/2011/10/fsharp-performance-test-structs-vs-records/) and [memory layout](http://theburningmonk.com/2015/07/smallest-net-ref-type-is-12-bytes-or-why-you-should-consider-using-value-types).
  * Jon Harrop has a number of good articles such as [this one](http://flyingfrogblog.blogspot.co.uk/2012/06/are-functional-languages-inherently.html) but some of it is behind a paywall.
  * Video: [High Performance F# in .NET and on the GPU](https://vimeo.com/33699102) with Jack Pappas. The sound is bad, but the slides and discussion are good!
  * [Resources for Math and Statistics](http://fsharp.org/guides/math-and-statistics/) on fsharp.org

## Summary

> "Keep it clean; keep it simple; aim to be elegant."
> -- *Martin Thompson*

This was a little experiment to see if I could have my cake and eat it too. Domain modelling using lots of types, but with the ability to get performance when needed in an elegant way.

I think that this is quite a nice solution, but as I said earlier, this optimization (and uglification)
should only ever be needed for a small number of heavily used core types that are allocated many millions of times.

Finally, I have not used this approach myself in a large production system (I've never needed to),
so I would be interested in getting feedback from people in the trenches on what they do.

*The code samples used in this post are [available on GitHub](https://gist.github.com/swlaschin/348b6b9e64d4b150cf86)*.