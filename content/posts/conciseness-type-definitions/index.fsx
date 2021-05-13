//>oneliner
open System

// some "record" types
type Person = {FirstName:string; LastName:string; Dob:DateTime}
type Coord = {Lat:float; Long:float}

// some "union" (choice) types
type TimePeriod = Hour | Day | Week | Year
type Temperature = C of int | F of int
type Appointment =
    OneTime of DateTime | Recurring of DateTime list
//<


//>concisecustomer
type PersonalName =
    {FirstName:string; LastName:string}

// Addresses
type StreetAddress = {
    Line1:string;
    Line2:string;
    Line3:string
    }

type ZipCode = ZipCode of string
type StateAbbrev = StateAbbrev of string
type ZipAndState =
    {State:StateAbbrev; Zip:ZipCode }
type USAddress =
    {Street:StreetAddress; Region:ZipAndState}

type UKPostCode = PostCode of string
type UKAddress =
    {Street:StreetAddress; Region:UKPostCode}

type InternationalAddress = {
    Street:StreetAddress;
    Region:string;
    CountryName:string
    }

// choice type -- must be one of these three specific types
type Address =
    | USAddress of USAddress
    | UKAddress of UKAddress
    | InternationalAddress of InternationalAddress

// Email
type Email = Email of string

// Phone
type CountryPrefix = Prefix of int
type Phone =
    CountryPrefix:CountryPrefix; LocalNumber:string}

type Contact =
    {
    PersonalName: PersonalName;
    // "option" means it might be missing
    Address: Address option;
    Email: Email option;
    Phone: Phone option;
    }

// Put it all together into a CustomerAccount type
type CustomerAccountId = AccountId of string
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
//<