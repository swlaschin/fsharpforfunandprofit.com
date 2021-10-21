// ======================================
// Companion file for index.md
//
// Use ../scripts/process_code_snippets.fsx to update the text
// ======================================

let dotdotdot() = failwith "..."

module UseWith =

    //>UseWith1
    type Config = {
        Host : string
        Port : int
        Db : string
        }
    //<

    //>UseWith2
    module Config =

        let Default = {
            Host = "localhost"
            Port = 80
            Db = "sqllite"
            }
    //<

    module Setup =
        open Config

        //>UseWith3
        let setup() =
            { Config.Default with Port=9000; Db="mysql" }
        //<

module UseWith_CsharpFriendly =

    //>UseWithCsharp1
    type Config = {
        Host : string
        Port : int
        Db : string
        }
        with
        static member Default = {
            Host = "localhost"
            Port = 80
            Db = "sqllite"
            }
    //<


module PrivateValidation =

    //>PrivateValidation1
    type Config = private {
        Host : string
        Port : int
        Db : string
        }
    //<

    module Config =

        let Default = {
            Host = "localhost"
            Port = 80
            Db = "sqllite"
            }

        //>PrivateValidation2
        let withPort port config =
            if port >= 0 || port <= 65535 then
                Some {config with Port=port}
            else
                None

        let withHost host config =
            if System.String.IsNullOrWhiteSpace host then
                None
            else
                Some {config with Host=host}
        //<

        //>PrivateValidation3
        let port config = config.Port
        let host config = config.Host
        //<


module TestPrivateValidation =
    open PrivateValidation

    Config.Default
    |> Config.withPort 80
    |> Option.bind (Config.withHost "example.com")



module TypedValidation =

    // wrapper types for Host and Port
    type Host = private Host of string
    type Port = private Port of int
    type Db = SqlLite | MySql | Postgres

    type Config = private {
        Host : Host
        Port : Port
        Db : Db
        }

    // companion module for Host type
    module Host =
        let Default = Host "localhost"

        let create str =
            if System.String.IsNullOrWhiteSpace str then
                None
            else
                Some (Host str)

        // get the data inside the wrapper
        let value (Host str) = str

    // companion module for Port type
    module Port =
        let Default = Port 80

        let create port =
            if port >= 0 || port <= 65535 then
                Some (Port port)
            else
                None

        // get the data inside the wrapper
        let value (Port port) = port

    // companion module for Config type
    module Config =

        let Default = {
            Host = Host.Default
            Port = Port.Default
            Db = SqlLite
            }

// ======================================
// Test builder from http://www.natpryce.com/articles/000714.html
// ======================================

//>TestBuilder1
type PostCode = {
    Outward : string
    Inward : string
    }
    with
    static member Default = {
        Outward = "NW1"
        Inward = ""
        }

type Address = {
    Street : string
    City : string
    PostCode : PostCode
    }
    with
    static member Default = {
        Street = "221b Baker Street"
        City = "London"
        PostCode = PostCode.Default
        }

type Recipient = {
    Name : string
    Address : Address
    }
    with
    static member Default = {
        Name = "Sherlock Holmes"
        Address = Address.Default
        }

type PoundsShillingsPence = {
    L: int; S: int; D: int
    }
    with
    static member Default = {
        L=1; S=1; D=1
        }

type InvoiceLine = {
    Description : string
    Cost : PoundsShillingsPence
    }

type Invoice = {
    Recipient : Recipient
    InvoiceLines : InvoiceLine list
    }
    with
    static member Default = {
        Recipient = Recipient.Default
        InvoiceLines = []
        }
//<

module InvoiceExample =

    //>TestBuilder2
    let invoice1 = {
        Recipient = {
            Name ="Sherlock Holmes"
            Address = {
                Street = "221b Baker Street"
                City = "London"
                PostCode = {Outward="NW1"; Inward="3RX"}
            }
        }
        InvoiceLines = [
            { Description="Deerstalker Hat"; Cost={L=0; S=3; D=10} }
            { Description="Tweed Cape"; Cost={L=0; S=4; D=12} }  // bug!
        ]
    }
    //<

    //>TestBuilder3
    let invoiceWithNoPostcode = {
        Invoice.Default with
            Recipient = { Recipient.Default with
                Name ="Sherlock Holmes"
                Address = { Address.Default with
                    PostCode = { PostCode.Default with
                        Outward=""}}}
        }
    //<

// ======================================
// Separate builder object
// ======================================

module Builder<'a> = {
    PartialValue : 'a
    Errors : string list
}