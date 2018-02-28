---
layout: post
title: "We don't need no stinking UML diagrams"
description: "A comparison of code vs UML"
categories: ["DDD"]
---

In my talk on [functional DDD](/ddd/), I often use this slide (*[in context](http://www.slideshare.net/ScottWlaschin/ddd-with-fsharptypesystemlondonndc2013/45)*):

![We don't need no stinking UML diagrams](/assets/img/no-uml-diagrams.jpg)

Which is of course is a misquote of [this famous scene](https://www.youtube.com/watch?v=gx6TBrfCW54). Oops, I mean [this one](https://www.youtube.com/watch?v=VqomZQMZQCQ).

Ok, I might be exaggerating a bit. Some UML diagrams are useful (I like sequence diagrams for example) and in general, I do think a good picture or diagram can be worth 1000 words.

But I believe that, in many cases, using UML for class diagrams is not necessary.

Instead, a concise language like F# (or OCaml or Haskell) can convey the same meaning
in a way that is easier to read, easier to write, and most important, easier to turn into *working code*! 

With UML diagrams, you need to translate them to code, with the possibility of losing something in translation.
But if the design is documented in your programming language itself, there is no translation phase, and so the design must always be in sync with the implementation.

To demonstrate this in practice, I decided to scour the internet for some good (and not-so-good) UML class diagrams, and convert them into F# code.  You can compare them for yourselves.

## Regular expressions

Let's start with a classic one: regular expressions (*[source](http://zenit.senecac.on.ca/wiki/index.php/Interpreter)*)

Here's the UML diagram:

![](/assets/img/uml-regex.png)

And here's the F# equivalent:

```fsharp
type RegularExpression =
    | Literal of string
    | Sequence of RegularExpression list
    | Alternation of RegularExpression * RegularExpression
    | Repetition of RegularExpression 

// An interpreter takes a string input and a RegularExpression
// and returns a value of some kind    
type Interpret<'a> =  string -> RegularExpression -> 'a
```    

That's quite straightforward.

## Student enrollment

Here's another classic one: enrollment (*[source](http://www.agilemodeling.com/artifacts/classDiagram.htm)*).

Here's the UML diagram:

![](/assets/img/uml-enrollment.png)

And here's the F# equivalent:

```fsharp
type Student = {
    Name: string
    Address: string
    PhoneNumber: string
    EmailAddress: string
    AverageMark: float
    }

type Professor= {
    Name: string
    Address: string
    PhoneNumber: string
    EmailAddress: string
    Salary: int
    }

type Seminar = {
    Name: string
    Number: string
    Fees: float
    TaughtBy: Professor option
    WaitingList: Student list
    }

type Enrollment = {
    Student : Student 
    Seminar : Seminar 
    Marks: float list
    }

type EnrollmentRepository = Enrollment list

// ==================================
// activities / use-cases / scenarios
// ==================================

type IsElegibleToEnroll = Student -> Seminar -> bool
type GetSeminarsTaken = Student -> EnrollmentRepository -> Seminar list
type AddStudentToWaitingList = Student -> Seminar -> Seminar 
```

The F# mirrors the UML diagram, but I find that by writing functions for all the activities rather than drawing pictures, holes in the original requirements are revealed.

For example, in the `GetSeminarsTaken` method in the UML diagram, where is the list of seminars stored?
If it is in the `Student` class (as implied by the diagram) then we have a mutual recursion between `Student` and `Seminar` 
and the whole tree of every student and seminar is interconnected and must be loaded at the same time unless [hacks are used](https://stackoverflow.com/questions/19371214/entity-framework-code-first-circular-dependices).

Instead, for the functional version, I created an `EnrollmentRepository` to decouple the two classes.

Similarly, it's not clear how enrollment actually works, so I created an `EnrollStudent` function to make it clear what inputs are needed.

```fsharp
type EnrollStudent = Student -> Seminar -> Enrollment option
```

Because the function returns an `option`, it is immediately clear that enrollment might fail (e.g student is not eligible to enroll, or is enrolling twice by mistake).

{% include book_page_ddd.inc %}

## Order and customer

Here's another one (*[source](http://www.tutorialspoint.com/uml/uml_class_diagram.htm)*).

![](/assets/img/uml-order.png)

And here's the F# equivalent:

```fsharp
type Customer = {name:string; location:string}

type NormalOrder = {date: DateTime; number: string; customer: Customer}
type SpecialOrder = {date: DateTime; number: string; customer: Customer}
type Order = 
    | Normal of NormalOrder
    | Special of SpecialOrder 

// these three operations work on all orders
type Confirm =  Order -> Order 
type Close =  Order -> Order 
type Dispatch =  Order -> Order 

// this operation only works on Special orders
type Receive =  SpecialOrder -> SpecialOrder
```    

I'm just copying the UML diagram, but I have to say that I hate this design. It's crying out to have more fine grained states. 

In particular, the `Confirm` and `Dispatch` functions are horrible -- they give no idea of what else is needed as input or what the effects will be.
This is where writing real code can force you to think a bit more deeply about the requirements.

## Order and customer, version 2

Here's a much better version of orders and customers (*[source](http://edn.embarcadero.com/article/31863)*).

![](/assets/img/uml-order2.png)

And here's the F# equivalent:

```fsharp
type Date = System.DateTime

// == Customer related ==

type Customer = {
    name:string
    address:string
    }

// == Item related ==

type [<Measure>] grams

type Item = {
    shippingWeight: int<grams>
    description: string
    }

type Qty = int
type Price = decimal


// == Payment related ==

type PaymentMethod = 
    | Cash
    | Credit of number:string * cardType:string * expDate:Date
    | Check of name:string * bankID: string

type Payment = {
    amount: decimal
    paymentMethod : PaymentMethod 
    }

// == Order related ==

type TaxStatus = Taxable | NonTaxable
type Tax = decimal

type OrderDetail = {
    item: Item
    qty: int
    taxStatus : TaxStatus
    }
    
type OrderStatus = Open | Completed

type Order = {
    date: DateTime; 
    customer: Customer
    status: OrderStatus
    lines: OrderDetail list
    payments: Payment list
    }

// ==================================
// activities / use-cases / scenarios
// ==================================
type GetPriceForQuantity = Item -> Qty -> Price

type CalcTax = Order -> Tax
type CalcTotal = Order -> Price
type CalcTotalWeight = Order -> int<grams>
```

I've done some minor tweaking, adding units of measure for the weight, creating types to represent `Qty` and `Price`.

Again, this design might be improved with more fine grained states, 
such as creating a separate `AuthorizedPayment` type (to ensure that an order can only be paid with authorized payments)
and a separate `PaidOrder` type (e.g. to stop you paying for the same order twice).

Here's the kind of thing I mean:

```fsharp
// Try to authorize a payment. Note that it might fail
type Authorize =  UnauthorizedPayment -> AuthorizedPayment option

// Pay an unpaid order with an authorized payment.
type PayOrder = UnpaidOrder -> AuthorizedPayment -> PaidOrder
```


## Hotel Booking

Here's one from the JetBrains IntelliJ documentation (*[source](https://www.jetbrains.com/idea/help/viewing-diagram.html)*).

![](/assets/img/uml-hotel.png)

Here's the F# equivalent:

```fsharp
type Date = System.DateTime

type User = {
    username: string
    password: string
    name: string
    }

type Hotel = {
    id: int
    name: string
    address: string
    city: string
    state: string
    zip: string
    country: string
    price: decimal
    }

type CreditCardInfo = {
    card: string
    name: string
    expiryMonth: int
    expiryYear: int
    }

type Booking = {
    id: int
    user: User
    hotel: Hotel
    checkinDate: Date
    checkoutDate: Date
    creditCardInfo: CreditCardInfo
    smoking: bool
    beds: int
    }

// What are these?!? And why are they in the domain?
type EntityManager = unit
type FacesMessages = unit
type Events = unit
type Log = unit

type BookingAction = {
    em: EntityManager
    user: User
    hotel: Booking
    booking: Booking
    facesMessages : FacesMessages
    events: Events 
    log: Log
    bookingValid: bool
    }

type ChangePasswordAction = {
    user: User
    em: EntityManager
    verify: string
    booking: Booking
    changed: bool
    facesMessages : FacesMessages
    }

type RegisterAction = {
    user: User
    em: EntityManager
    facesMessages : FacesMessages
    verify: string
    registered: bool
    }
```    

I have to stop there, sorry. The design is driving me crazy. I can't even.

What are these `EntityManager` and `FacesMessages` fields? And logging is important of course, but why is `Log` a field in the domain object?

By the way, in case you think that I am deliberately picking bad examples of UML design, all these diagrams come from the top results in an image search for ["uml class diagram"](https://www.google.com/search?q=uml+class+diagram&tbm=isch).

## Library

This one is better, a library domain (*[source](http://www.uml-diagrams.org/library-domain-uml-class-diagram-example.html)*).

![](/assets/img/uml-library.png)

Here's the F# equivalent. Note that because it is code, I can add comments to specific types and fields, which is doable but awkward with UML.

Note also that I can say `ISBN: string option` to indicate an optional ISBN rather that the awkward `[0..1]` syntax.

```fsharp
type Author = {
    name: string
    biography: string
    }

type Book = {
    ISBN: string option
    title: string
    author: Author
    summary: string
    publisher: string
    publicationDate: Date
    numberOfPages: int
    language: string
    }

type Library = {
    name: string
    address: string
    }

// Each physical library item - book, tape cassette, CD, DVD, etc. could have its own item number. 
// To support it, the items may be barcoded. The purpose of barcoding is 
// to provide a unique and scannable identifier that links the barcoded physical item 
// to the electronic record in the catalog. 
// Barcode must be physically attached to the item, and barcode number is entered into 
// the corresponding field in the electronic item record.
// Barcodes on library items could be replaced by RFID tags. 
// The RFID tag can contain item's identifier, title, material type, etc. 
// It is read by an RFID reader, without the need to open a book cover or CD/DVD case 
// to scan it with barcode reader.
type BookItem = {
    barcode: string option
    RFID: string option
    book: Book
    /// Library has some rules on what could be borrowed and what is for reference only. 
    isReferenceOnly: bool
    belongsTo: Library
    }

type Catalogue = {
    belongsTo: Library
    records : BookItem list
    }

type Patron = {
    name: string
    address: string
    }

type AccountState = Active | Frozen | Closed

type Account = {
    patron: Patron
    library: Library
    number: int
    opened: Date
    
    /// Rules are also defined on how many books could be borrowed 
    /// by patrons and how many could be reserved.
    history: History list
    
    state: AccountState
    }

and History = {
    book : BookItem
    account: Account
    borrowedOn: Date
    returnedOn: Date option
    }
```

Since the Search and Manage interfaces are undefined, we can just use placeholders (`unit`) for the inputs and outputs.

```fsharp
type Librarian = {
    name: string
    address: string
    position: string
    }

/// Either a patron or a librarian can do a search
type SearchInterfaceOperator =
    | Patron of Patron
    | Librarian of Librarian

type SearchRequest = unit // to do
type SearchResult = unit // to do
type SearchInterface = SearchInterfaceOperator -> Catalogue -> SearchRequest -> SearchResult

type ManageRequest = unit // to do
type ManageResult = unit // to do

/// Only librarians can do management
type ManageInterface = Librarian -> Catalogue -> ManageRequest -> ManageResult   
```    

Again, this might not be the perfect design. For example, it's not clear that only `Active` accounts could borrow a book, which I might represent in F# as: 

```fsharp
type Account = 
    | Active of ActiveAccount
    | Closed of ClosedAccount
    
/// Only ActiveAccounts can borrow
type Borrow = ActiveAccount -> BookItem -> History
```    

If you want to see a more modern approach to modelling this domain using CQRS and event sourcing, see [this post](http://thinkbeforecoding.com/post/2009/11/02/Event-Sourcing-and-CQRS-Lets-use-it).


## Software licensing

The final example is from a software licensing domain (*[source](http://www.uml-diagrams.org/software-licensing-domain-diagram-example.html?context=cls-examples)*).

![](/assets/img/uml-hasp.png)

Here's the F# equivalent. 

```fsharp
open System
type Date = System.DateTime
type String50 = string
type String5 = string

// ==========================
// Customer-related
// ==========================

type AddressDetails = {
    street : string option
    city : string option
    postalCode : string option
    state : string option
    country : string option
    }

type CustomerIdDescription = {
    CRM_ID : string
    description : string
    }

type IndividualCustomer = {
    idAndDescription : CustomerIdDescription
    firstName : string
    lastName : string
    middleName : string option
    email : string
    phone : string option
    locale : string option // default :  "English"
    billing : AddressDetails
    shipping : AddressDetails
    }

type Contact = {
    firstName : string
    lastName : string
    middleName : string option
    email : string
    locale : string option // default :  "English"
    }

type Company = {
    idAndDescription : CustomerIdDescription
    name : string
    phone : string option
    fax : string option
    contact: Contact
    billing : AddressDetails
    shipping : AddressDetails
    }

type Customer = 
    | Individual of IndividualCustomer
    | Company of Company 

// ==========================
// Product-related
// ==========================

/// Flags can be ORed together
[<Flags>] 
type LockingType =
    | HL 
    | SL_AdminMode 
    | SL_UserMode

type Rehost =
    | Enable
    | Disable
    | LeaveAsIs
    | SpecifyAtEntitlementTime

type BatchCode = {
    id : String5
    }
    
type Feature = {
    id : int
    name : String50
    description : string option
    }

type ProductInfo = {
    id : int
    name : String50
    lockingType : LockingType
    rehost : Rehost
    description : string option
    features: Feature list
    bactchCode: BatchCode
    }

type Product = 
    | BaseProduct of ProductInfo
    | ProvisionalProduct of ProductInfo * baseProduct:Product 

// ==========================
// Entitlement-related
// ==========================

type EntitlementType = 
    | HardwareKey
    | ProductKey
    | ProtectionKeyUpdate

type Entitlement = {
    EID : string
    entitlementType : EntitlementType 
    startDate : Date
    endDate : Date option
    neverExpires: bool
    comments: string option
    customer: Customer
    products: Product list
    }
```    

This diagram is just pure data and no methods, so there are no function types.  I have a feeling that there are some important business rules that have not been captured.

For example, if you read the comments in the source, you'll see that there are some interesting constraints around `EntitlementType` and `LockingType`.
Only certain locking types can be used with certain entitlement types.

That might be something that we could consider modelling in the type system, but I haven't bothered. I've just tried to reproduct the UML as is.

## Summary

I think that's enough to get the idea. 

My general feeling about UML class diagrams is that they are OK for a sketch, if a bit heavyweight compared to a few lines of code.

For detailed designs, though, they are not nearly detailed enough. Critical things like context and dependencies are not at all obvious.
In my opinion, none of the UML diagrams I've shown have been good enough to write code from, even as a basic design.

Even more seriously, a UML diagram can be very misleading to non-developers. It looks "official" and can give the impression that the design has been thought about deeply,
when in fact the design is actually shallow and unusable in practice.

Disagree? Let me know in the comments!  


