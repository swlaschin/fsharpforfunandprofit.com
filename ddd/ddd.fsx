// ======================================================
// This page contains code snippets for the talk
// "Domain-driven design with the F# type system"
// ======================================================

// To execute code in VisualStudio, 
//  (a) Open F# interactive (Menu->View->Other Windows->F# interactive)
//  (b) Highlight code, right click and choose "Send to Interactive"
// To execute code in trysharp.org, just highlight and click "Run"

// Try it! highlight from here ===>
let three = 1 + 2
let square x = x * x
printfn "Three is %i" three
printfn "Three squared is %i" (square three)
// <==== highlight to here and Run

// ======================================================
// demonstration of Value types in F#
// ======================================================

// highlight from here ===>
module FirstSlide = 

    type Contact = {

      FirstName: string
      MiddleInitial: string
      LastName: string

      EmailAddress: string
      IsEmailVerified: bool  // true if ownership of email address is confirmed

      }

    // The issues with this design are:
    // * Which values are optional?
    // * Whare the constraints?
    // * What groups of properties are linked?
    // * Is there any domain logic that we need to be aware of?
// <==== highlight to here and Run

// ======================================================
// demonstration of Value types in F#
// ======================================================

// highlight from here ===>
// define the type
module ValueTypeExample = 

    type PersonalName = {FirstName:string; LastName:string}
// <==== highlight to here and Run


// highlight from here ===>
// test the code 
module TestValueTypeExample = 
    open ValueTypeExample
        
    let alice = {FirstName="Alice"; LastName="Adams"}
    let aliceClone = {FirstName="Alice"; LastName="Adams"}
    printfn "Alice's name is %A" alice
    printfn "AliceClone's name is %A" aliceClone
    printfn "Are Alice and clone equal? %b" (alice = aliceClone)
// <==== highlight to here and Run 

// NOTE you may need to scroll up in the outpt window to see the print statements



// ======================================================
// demonstration of Entity types in F#
// ======================================================

// highlight from here ===>
// define the type
module EntityTypeExample = 

    // value type
    type PersonalName = {FirstName:string; LastName:string}
    
    // entity type
    [<CustomEquality; NoComparison>]    
    type Person = {Id:int; Name:PersonalName} with
        
        override this.Equals(other) =
            match other with
            | :? Person as p -> (this.Id = p.Id)
            | _ -> false
        
        override this.GetHashCode() = hash this.Id
// <==== highlight to here and Run


// highlight from here ===>
// test the code 
module TestEntityTypeExample = 
    open EntityTypeExample
    
    let alice = {Id=1; Name={FirstName="Alice"; LastName="Adams"}}
    let bilbo = {Id=1; Name={FirstName="Bilbo"; LastName="Baggins"}}
    printfn "Alice is %A" alice
    printfn "Bilbo is %A" bilbo
    printfn "Are Alice and Bilbo equal? %b" (alice = bilbo)
    printfn "Are Alice.Name and Bilbo.Name equal? %b" (alice.Name = bilbo.Name)
// <==== highlight to here and Run


// ======================================================
// Entity versioning in F#
// ======================================================

// highlight from here ===>
// define the type
module EntityVersioningExample = 

    open System
    
    //value type
    type PersonalName = {FirstName:string; LastName:string}
    
    // entity type
    [<NoEquality; NoComparison>]    
    type Person = {Id:int; Version:Guid; Name:PersonalName} 
// <==== highlight to here and Run


// highlight from here ===>
// test the code 
module TestEntityVersioningExample = 
    open EntityVersioningExample 
    open System

    let alice ={Id=1; Version=Guid.NewGuid(); 
                Name={FirstName="Alice"; LastName="Adams"}}
                
    let aliceV2 = {alice with 
                    Version=Guid.NewGuid(); 
                    Name={FirstName="Al"; LastName="Adamson"}}
    
    printfn "Alice is %A" alice
    printfn "AliceV2 is %A" aliceV2
    //printfn "Are Alice and AliceV2 equal? %b" (alice = aliceV2)   // uncomment this to get a compiler error
    printfn "Are Alice and AliceV2 same id? %b" (alice.Id = aliceV2.Id)
    printfn "Are Alice and AliceV2 same version? %b" (alice.Version = aliceV2.Version)
// <==== highlight to here and Run

// ======================================================
// Entity object definition in F# with mutability
// ======================================================

// highlight from here ===>
// define the type
module EntityMutabilityExample = 
    
    //value type
    type PersonalName = {FirstName:string; LastName:string}
    
    // entity type
    [<NoEquality; NoComparison>]    
    type Person = {Id:int; mutable Name:PersonalName} 
// <==== highlight to here and Run



// highlight from here ===>
// test the code 
module TestEntityMutabilityExample = 
    open EntityMutabilityExample 
    
    let alice ={Id=1; Name={FirstName="Alice"; LastName="Adams"}}
    printfn "Alice before change  is %A" alice
                
    alice.Name <- {FirstName="Al"; LastName="Adamson"}
    printfn "Alice after change  is %A" alice
// <==== highlight to here and Run

// ======================================================
// Review of code so far
// ======================================================

// highlight from here ===>
module ValueAndEntityReview = 

    [<StructuralEquality;NoComparison>]
    type PersonalName = {         // a Value Object
        FirstName : string; 
        LastName : string }
    
    [<NoEquality; NoComparison>]       
    type Person = {               // an Entity
        Id : int; 
        Name : PersonalName }  


    // try to put an Entity inside a Value
    //[<StructuralEquality;NoComparison>]      // uncomment to get a compiler error
    //type PersonWrapper = { Person: Person }  // uncomment to get a compiler error
// <==== highlight to here and Run


// ======================================================
// Ubiquitous language
// ======================================================

// highlight from here ===>
module CardGameBoundedContext = 

    type Suit = Club | Diamond | Spade | Heart
                // | means a choice -- pick one from the list
                
    type Rank = Two | Three | Four | Five | Six | Seven | Eight 
                | Nine | Ten | Jack | Queen | King | Ace

    type Card = Suit * Rank   // * means a pair -- one from each type
    
    type Hand = Card list
    type Deck = Card list
    
    type Player = {Name:string; Hand:Hand}
    type Game = {Deck:Deck; Players: Player list}
    
    type Deal = Deck -> (Deck * Card) // X -> Y means a function
                                      // input of type X
                                      // output of type Y

    type PickupCard = (Hand * Card)-> Hand
// <==== highlight to here and Run


// highlight from here ===>
module TestCardGameBoundedContext = 
    open CardGameBoundedContext

    let aceHearts  = (Heart, Ace)
    let aceSpades = (Spade, Ace)
    let twoClubs = (Club, Two)

    let myHand = [aceHearts; aceSpades; twoClubs]

    let deck = [aceHearts; aceSpades; twoClubs]

    let deal cards = 
        let head::tail = cards   // compiler has found a potential bug here!
        (tail, head)

// <==== highlight to here and Run



// ======================================================
// Understanding the F# type system
// ======================================================

// highlight from here ===>
module ProductTypeExamples = 

    let x = (1,2)    //  int * int
    let y = (true,false)    //  bool * bool 

    type Person = Person of string // dummy type    
    type Date = Date of string // dummy type    

    type Birthday = Person * Date
// <==== highlight to here and Run


// highlight from here ===>
module TestProductTypeExamples = 
    open ProductTypeExamples

    let alice = Person "Alice"
    let date1 = Date "Jan 12th"
    let aliceBDay = (alice,date1)
    let aliceBDay2 : Birthday = (alice,date1)  // with explicit typing
// <==== highlight to here and Run


// highlight from here ===>
module ChoiceTypeExamples = 
    
    type Temp = 
      | F of int
      | C of float


    type CardType = CardType of string
    type CardNumber = CardNumber of string

    type PaymentMethod = 
      | Cash
      | Cheque of int
      | Card of CardType * CardNumber

// <==== highlight to here and Run

// highlight from here ===>
module TestChoiceTypeExamples = 
    open ChoiceTypeExamples

    let isFever temp = 
        match temp with
        | F tempInF -> tempInF > 101
        | C tempInC -> tempInC > 38.0

    let temp1 = F 103 
    printfn "temp %A is fever? %b" temp1 (isFever temp1)

    let temp2 = C 37.0
    printfn "temp %A is fever? %b" temp2 (isFever temp2)

    let printPayment paymentMethod = 
        match paymentMethod with
        | Cash -> 
            printfn "Paid in cash"
        | Cheque checkNo ->
            printfn "Paid by cheque: %i" checkNo
        | Card (cardType,cardNo) ->
            printfn "Paid with %A %A" cardType cardNo

    let cashPayment = Cash
    let chequePayment  = Cheque 123
    let cardPayment  = Card (CardType "Visa",CardNumber "123")
    
    printPayment cashPayment
    printPayment chequePayment
    printPayment cardPayment
// <==== highlight to here and Run



// ======================================================
// Designing with types
// ======================================================

// highlight from here ===>
module OptionalType = 

    type OptionalString = 
        | SomeString of string
        | Nothing

    type OptionalInt = 
        | SomeInt of string
        | Nothing

    type OptionalBool = 
        | SomeBool of string
        | Nothing

    // built in!
//    type Option<'T> = 
//        | Some of 'T
//        | None

// <==== highlight to here and Run


// highlight from here ===>
module TestOptionalType = 

    type PersonalName1 = 
        {
        FirstName: string
        MiddleInitial: Option<string>
        LastName: string
        }

    type PersonalName2 = 
        {
        FirstName: string
        MiddleInitial: string option
        LastName: string
        }
// <==== highlight to here and Run



// highlight from here ===>
module SingleChoiceType =

    type EmailAddress = EmailAddress of string
    type PhoneNumber = PhoneNumber of string

    type CustomerId = CustomerId of int
    type OrderId = OrderId of int
// <==== highlight to here and Run



// highlight from here ===>
module TestSingleChoiceType = 
    open SingleChoiceType       

    let email1 = EmailAddress "abc"
    let email2 = EmailAddress "def"
    let phone1 = PhoneNumber "abc"

    printfn "%A = %A? %b" email1 email2 (email1=email2)
    //printfn "%A = %A? %b" email1 phone1 (email1=phone1)   // uncommento get compiler error
// <==== highlight to here and Run



// highlight from here ===>
module EmailAddressType =
    open System.Text.RegularExpressions 

    type EmailAddress = EmailAddress of string

    // createEmailAddress : string -> EmailAddress option
    let createEmailAddress (s:string) = 
        if Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
            then Some (EmailAddress s)
            else None
// <==== highlight to here and Run

// highlight from here ===>
module TestEmailAddressType =
    open EmailAddressType

    let email1 = createEmailAddress "a@example.com"
    let email2 = createEmailAddress "example.com"
// <==== highlight to here and Run


// highlight from here ===>
module ConstrainedString =

    type String50 = String50 of string

    let createString50 (s:string) = 
        if s = null
            then None
        else if s.Length <= 50
            then Some (String50 s)
            else None    
// <==== highlight to here and Run

// highlight from here ===>
module TestConstrainedString =
    open ConstrainedString

    let s1 = createString50 "12345"
    let s2 = createString50 (String.replicate 100 "a")
// <==== highlight to here and Run


// highlight from here ===>
module ConstrainedInteger =

    type OrderLineQty = OrderLineQty of int

    let createOrderLineQty qty = 
        if qty >0 && qty <= 99
            then Some (OrderLineQty qty)
            else None

    let increment (OrderLineQty i) =
        createOrderLineQty (i + 1)

    let decrement (OrderLineQty i) =
        createOrderLineQty (i - 1)
// <==== highlight to here and Run

// highlight from here ===>
module TestConstrainedInteger =
    open ConstrainedInteger

    let qty1 = createOrderLineQty 1
    let qty2 = createOrderLineQty 0

    let qty3 = 
        match qty1 with
        | Some i -> decrement i
        | None -> None
// <==== highlight to here and Run

// ======================================================
// Prologue revisited
// ======================================================

module PrologueVersion2 = 

    type String1 = String1 of string
    type String50 = String50 of string
    type EmailAddress = EmailAddress of string

    type PersonalName = {
        FirstName: String50
        MiddleInitial: String1 option
        LastName: String50 }

    type EmailContactInfo = {
        EmailAddress: EmailAddress
        IsEmailVerified: bool }

    type Contact = {
        Name: PersonalName 
        Email: EmailContactInfo }


// ======================================================
// Verified email
//
// "Rule 1: If the email is changed, the verified flag must be reset to false"
// "Rule 2: The verified flag can only be set by a special verification service"
// ======================================================

module VerifiedEmailExample = 
    type String1 = String1 of string
    type String50 = String50 of string
    type EmailAddress = EmailAddress of string

    type PersonalName = {
        FirstName: String50
        MiddleInitial: String1 option
        LastName: String50 }

    type VerifiedEmail = VerifiedEmail of EmailAddress
    type VerificationHash = VerificationHash of string
    type VerificationService = 
        (EmailAddress * VerificationHash) ->  VerifiedEmail option

    type EmailContactInfo = 
        | Unverified of EmailAddress
        | Verified of VerifiedEmail

    type Contact = {
        Name: PersonalName 
        Email: EmailContactInfo }


// ======================================================
// A contact must have an email or a postal address
// ======================================================

module EMailAndContactExample_Before = 


    type String1 = String1 of string
    type String50 = String50 of string
    type EmailAddress = EmailAddress of string

    type PersonalName = {
        FirstName: String50
        MiddleInitial: String1 option
        LastName: String50 }

    type VerifiedEmail = VerifiedEmail of EmailAddress
    type VerificationHash = VerificationHash of string
    type VerificationService = 
        (EmailAddress * VerificationHash) ->  VerifiedEmail option

    type EmailContactInfo = 
        | Unverified of EmailAddress
        | Verified of VerifiedEmail

    type PostalContactInfo = {
        Address1: String50
        Address2: String50 option
        Town: String50
        PostCode: String50 }

    type Contact = {
        Name: PersonalName 
        Email: EmailContactInfo 
        Address: PostalContactInfo 
        }


module EMailAndContactExample_After = 

    type String1 = String1 of string
    type String50 = String50 of string
    type EmailAddress = EmailAddress of string

    type PersonalName = {
        FirstName: String50
        MiddleInitial: String1 option
        LastName: String50 }

    type VerifiedEmail = VerifiedEmail of EmailAddress
    type VerificationHash = VerificationHash of string
    type VerificationService = 
        (EmailAddress * VerificationHash) ->  VerifiedEmail option

    type EmailContactInfo = 
        | Unverified of EmailAddress
        | Verified of VerifiedEmail

    type PostalContactInfo = {
        Address1: String50
        Address2: String50 option
        Town: String50
        PostCode: String50 }

    type ContactInfo = 
        | EmailOnly of EmailContactInfo
        | AddrOnly of PostalContactInfo
        | EmailAndAddr of EmailContactInfo * PostalContactInfo

    type Contact = {
        Name: PersonalName 
        ContactInfo: ContactInfo 
        }


// ======================================================
// A contact must have at least one way of being contacted
// ======================================================


module EMailAndContactExample_Alternative = 

    type String1 = String1 of string
    type String50 = String50 of string
    type EmailAddress = EmailAddress of string

    type PersonalName = {
        FirstName: String50
        MiddleInitial: String1 option
        LastName: String50 }

    type VerifiedEmail = VerifiedEmail of EmailAddress
    type VerificationHash = VerificationHash of string
    type VerificationService = 
        (EmailAddress * VerificationHash) ->  VerifiedEmail option

    type EmailContactInfo = 
        | Unverified of EmailAddress
        | Verified of VerifiedEmail

    type PostalContactInfo = {
        Address1: String50
        Address2: String50 option
        Town: String50
        PostCode: String50 }

    type ContactInfo = 
        | EmailOnly of EmailContactInfo
        | AddrOnly of PostalContactInfo

    type Contact = {
        Name: PersonalName 
        PrimaryContactInfo: ContactInfo
        SecondaryContactInfo: ContactInfo option
        }



