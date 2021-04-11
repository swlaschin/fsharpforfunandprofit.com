(*
CapabilityBasedSecurity_TypeExample.fsx

Code snippets from the blog post: http://fsharpforfunandprofit.com/posts/capability-based-security-3/
*)

open System.Security.Principal
open System

// ==============================
// dummy definitions
// ==============================
type CustomerId = int
type CustomerData = CustomerData of string
type Password = Password of string

let myCustomerData = CustomerData "data"

let customerIdBelongsToPrincipal customerId (principle:IPrincipal) = true

type SuccessFailure<'a,'b> = 
    | Success of 'a
    | Failure of 'b

type DbErrors = 
    | AuthorizationFailed
    | CustomerIdNotFound of CustomerId

// ==============================
// end dummy definitions
// ==============================
     
// ==============================
// Example 1 - access token in same service
// ==============================

module Example1 = 

    /// Public database module
    module CustomerDatabase = 

        type DbAccessToken private() = 
            // create a DbAccessToken that allows access to a particular customer
            static member getAccessToCustomer id principal = 
                if customerIdBelongsToPrincipal id principal ||
                   principal.IsInRole("CustomerAgent") 
                then
                    Some <| DbAccessToken() 
                else
                    None   

        let getCustomer (accessToken:DbAccessToken) (id:CustomerId) = 
            // get customer data
            Success myCustomerData 

        let updateCustomer (accessToken:DbAccessToken) (id:CustomerId) (data:CustomerData) = 
            // update database
            Success "OK"


    /// Usage example
    module Startup = 
        let principal = WindowsPrincipal.Current // from context
        let id = 0 // from context

        // attempt to get an access token
        let accessToken = CustomerDatabase.DbAccessToken.getAccessToCustomer id principal

        // get the (optional) capabilities
        let getCustomerCap = 
            accessToken |> Option.map CustomerDatabase.getCustomer
        let updateCustomerCap = 
            accessToken |> Option.map CustomerDatabase.updateCustomer 

        // use the capabilities, if available               
        match getCustomerCap with
        | Some getCustomer -> getCustomer id
        | None -> Failure AuthorizationFailed // error

        match updateCustomerCap with
        | Some updateCustomer -> updateCustomer id myCustomerData
        | None -> Failure AuthorizationFailed // error


// ==============================
// Example 2 - access token from separate service
//
// Dangerous because (a) the access token can be reused
// and (b) the access token doesn't store the customer id
// ==============================

module Example2 = 

    /// OO version of AccessToken
    module AuthorizationService = 

        // the constructor is hidden using "private"
        type AccessToken private() = 

            // create a AccessToken that allows access to a particular customer
            static member getAccessToCustomer id principal = 
                if customerIdBelongsToPrincipal id principal ||
                   principal.IsInRole("CustomerAgent") 
                then
                    Some <| AccessToken() 
                else
                    None   

    /// Public database module
    module CustomerDatabase = 
        open AuthorizationService 

        let getCustomer (accessToken:AccessToken) (id:CustomerId) = 
            // get customer data
            Success myCustomerData 

        let updateCustomer (accessToken:AccessToken) (id:CustomerId) (data:CustomerData) = 
            // update database
            Success "OK"

    /// Usage example
    module Startup = 
        let principal = WindowsPrincipal.Current // from context
        let id = 0 // from context

        // attempt to get an access token
        let accessToken = AuthorizationService.AccessToken.getAccessToCustomer id principal

        // get the (optional) capabilities
        let getCustomerCap = 
            accessToken |> Option.map CustomerDatabase.getCustomer
        let updateCustomerCap = 
            accessToken |> Option.map CustomerDatabase.updateCustomer 

        // use the capabilities, if available               
        match getCustomerCap with
        | Some getCustomer -> getCustomer id
        | None -> Failure AuthorizationFailed // error

        match updateCustomerCap with
        | Some updateCustomer -> updateCustomer id myCustomerData 
        | None -> Failure AuthorizationFailed // error


// ==============================
// Example 3 - access token stores information
// ==============================

module Example3 = 

    module Capabilities = 

        // each capability gets a type
        type AccessCustomer = AccessCustomer of CustomerId
        type UpdatePassword = UpdatePassword of CustomerId

    // functional version of AccessToken
    module AuthorizationService = 
        open Capabilities

        // the constructor is protected
        type AccessToken<'data> = private {data:'data} with 
            // but do allow read access to the data
            member this.Data = this.data

        // create a AccessToken that allows access to a particular customer
        let getAccessCustomerToken id principal = 
            if customerIdBelongsToPrincipal id principal ||
                principal.IsInRole("CustomerAgent") 
            then
                Some {data=AccessCustomer id}
            else
                None   

        // create a AccessToken that allows access to UpdatePassword 
        let getUpdatePasswordToken id principal = 
            if customerIdBelongsToPrincipal id principal then
                Some {data=UpdatePassword id}
            else
                None

    /// Public database module
    module CustomerDatabase = 
        open Capabilities
        open AuthorizationService
        open System.Collections.Generic

        let private db = Dictionary<CustomerId,CustomerData>()

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


    /// Usage example
    module Startup = 
        open AuthorizationService

        let principal = WindowsPrincipal.Current // from context
        let customerId = 0 // from context

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