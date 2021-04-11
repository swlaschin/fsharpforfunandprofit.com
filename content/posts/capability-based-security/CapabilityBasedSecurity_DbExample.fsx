(*
CapabilityBasedSecurity_DbExample.fsx

Code snippets from the blog post: http://fsharpforfunandprofit.com/posts/capability-based-security-2/
*)

open System.Security.Principal
open System

// ==============================
// dummy definitions
// ==============================
type CustomerId = int
type CustomerData = string

let customerIdBelongsToPrincipal (id:CustomerId) (principle:IPrincipal) = true

type SuccessFailure<'a,'b> = 
    | Success of 'a
    | Failure of 'b

type DbErrors = 
    | AuthorizationFailed
    | OnlyAllowedOnce
    | OnlyAllowedNTimes of int
    | Revoked

// ==============================
// end dummy definitions
// ==============================


// ==============================
// Example 1 - inlined authorization
// ==============================

module Example1 = 
    let getCustomer id principal = 
        if customerIdBelongsToPrincipal id principal ||
           principal.IsInRole("CustomerAgent") 
        then
            // get database
            Success "CustomerData"
        else
            Failure AuthorizationFailed

    let updateCustomer id data principal = 
        if customerIdBelongsToPrincipal id principal ||
           principal.IsInRole("CustomerAgent") 
        then
            // update database
            Success "OK"
        else
            Failure AuthorizationFailed

// ==============================
// Example 2 - separate CapabilityProvider
// ==============================

module Example2 = 

    module internal CustomerDatabase = 
        let getCustomer (id:CustomerId) :CustomerData = 
            // get customer data
            "CustomerData"

        let updateCustomer (id:CustomerId) (data:CustomerData) = 
            // update database
            ()

    /// accessible to the business layer        
    module CustomerDatabaseCapabilityProvider =         

        // Get the capability to call getCustomer
        let getGetCustomerCapability (id:CustomerId) (principal:IPrincipal) = 
            if customerIdBelongsToPrincipal id principal ||
               principal.IsInRole("CustomerAgent") 
            then
                Some ( fun () -> CustomerDatabase.getCustomer id )
            else
                None
 
        // Get the capability to call UpdateCustomer
        let getUpdateCustomerCapability (id:CustomerId) (principal:IPrincipal) = 
            if customerIdBelongsToPrincipal id principal ||
               principal.IsInRole("CustomerAgent") 
            then
                Some ( fun () -> CustomerDatabase.updateCustomer id )
            else
                None


// ==============================
// Example 3 - separate CapabilityFilter
// ==============================

module Example3 = 

    module internal CustomerDatabase = 
        let getCustomer (id:CustomerId) :CustomerData = 
            // get customer data
            "data"
        // val getCustomer : CustomerId -> CustomerData
    

        let updateCustomer (id:CustomerId) (data:CustomerData) = 
            // update database
            ()
        // val updateCustomer : CustomerId -> CustomerData -> unit


    module CustomerCapabilityFilter =         
 
        // Get the capability to use any function that has a CustomerId parameter
        // but only if the caller has the same customer id or is a member of the 
        // CustomerAgent role.
        let onlyForSameIdOrAgents (id:CustomerId) (principal:IPrincipal) (f:CustomerId -> 'a) = 
            if customerIdBelongsToPrincipal id principal ||
               principal.IsInRole("CustomerAgent") 
            then
                Some (fun () -> f id)
            else
                None

    module Startup =
        open CustomerCapabilityFilter 
                 
        let principal = WindowsPrincipal.Current // from context
        let id = 0 // from context

        let getCustomerOnlyForSameIdOrAgents = 
            onlyForSameIdOrAgents id principal CustomerDatabase.getCustomer
        // val getCustomerOnlyForSameIdOrAgents : (CustomerId -> CustomerData) option

        let updateCustomerOnlyForSameIdOrAgents = 
            onlyForSameIdOrAgents id principal CustomerDatabase.updateCustomer
        // val updateCustomerOnlyForSameIdOrAgents : (CustomerId -> CustomerData -> unit) option

        match getCustomerOnlyForSameIdOrAgents with
        | Some cap -> () // create child component and pass in the capability
        | None -> () // return error saying that you don't have the capability to get the data


// ==============================
// Example 4 - composable filters
// ==============================

module Example4 = 

    module internal CustomerDatabase = 
        let getCustomer (id:CustomerId) : CustomerData = 
            // get customer data
            "data"
        // val getCustomer : CustomerId -> CustomerData
    

        let updateCustomer (id:CustomerId) (data:CustomerData) = 
            // update database
            ()
        // val updateCustomer : CustomerId -> CustomerData -> unit


    module CustomerCapabilityFilter =         

        let onlyForSameId (id:CustomerId) (principal:IPrincipal) (f:CustomerId -> 'a) = 
            if customerIdBelongsToPrincipal id principal then
                Some (fun () -> f id)
            else
                None
 
        let onlyForAgents (id:CustomerId) (principal:IPrincipal) (f:CustomerId -> 'a) = 
            if principal.IsInRole("CustomerAgent") then
                Some (fun () -> f id)
            else
                None

        let onlyIfDuringBusinessHours (time:DateTime) f = 
            if time.Hour >= 8 && time.Hour <= 17 then
                Some f
            else
                None

        // given a list of capability options, 
        // return the first good one, if any
        let first capabilityList = 
            capabilityList |> List.tryPick id


        // given a capability option, restrict it
        let restrict filter originalCap = 
            originalCap
            |> Option.bind filter 
            

    module Startup =
        open CustomerCapabilityFilter 
                 
        let principal = WindowsPrincipal.Current // from context
        let id = 0 // from context

        let getCustomerOnlyForSameId = 
            let f = CustomerDatabase.getCustomer
            onlyForSameId id principal f
        // val getCustomerOnlyForSameId : (unit -> CustomerData) option

        let getCustomerOnlyForSameIdOrAgents = 
            let f = CustomerDatabase.getCustomer
            let cap1 = onlyForSameId id principal f
            let cap2 = onlyForAgents id principal f 
            first [cap1; cap2]
        // val getCustomerOnlyForSameIdOrAgents : (unit -> CustomerData) option

        let updateCustomerOnlyForSameIdOrAgents = 
            let f = CustomerDatabase.updateCustomer
            let cap1 = onlyForSameId id principal f
            let cap2 = onlyForAgents id principal f 
            first [cap1; cap2]
        // val updateCustomerOnlyForSameIdOrAgents : (unit -> CustomerData -> unit) option

        match getCustomerOnlyForSameIdOrAgents with
        | Some cap -> () // create child component and pass in the capability
        | None -> () // return error saying that you don't have the capability to get the data

        let getCustomerOnlyForAgentsInBusinessHours = 
            let f = CustomerDatabase.getCustomer
            let cap1 = onlyForAgents id principal f 
            let restriction f = onlyIfDuringBusinessHours (DateTime.Now) f
            cap1 |> restrict restriction 
        // val getCustomerOnlyForAgentsInBusinessHours : (unit -> CustomerData) option

        let getCustomerOnlyForSameId_OrForAgentsInBusinessHours = 
            let cap1 = getCustomerOnlyForSameId
            let cap2 = getCustomerOnlyForAgentsInBusinessHours 
            first [cap1; cap2]

// ==============================
// Example 5 - more transforms
// ==============================

module Example5 = 

    module internal CustomerDatabase = 
        let updatePassword (id,password) = 
            Success "OK"

    module GenericCapabilityFilter =         

        /// Uses of the capability will be audited
        let auditable capabilityName f = 
            fun x -> 
                // simple audit log!
                printfn "AUDIT: calling %s with %A" capabilityName  x
                // use the capability
                f x

        /// Allow the function to be called once only
        let onlyOnce f = 
            let allow = ref true
            fun x -> 
                if !allow then   //! is dereferencing not negation!
                    allow := false
                    f x
                else
                    Failure OnlyAllowedOnce

        /// Return a pair of functions: the revokable capability, 
        /// and the revoker function
        let revokable f = 
            let allow = ref true
            let capability = fun x -> 
                if !allow then  //! is dereferencing not negation!
                    f x
                else
                    Failure Revoked
            let revoker() = 
                allow := false
            capability, revoker

    module Startup =
        open GenericCapabilityFilter 

        // ----------------------------------------
        let updatePasswordWithAudit x = 
            auditable "updatePassword" CustomerDatabase.updatePassword x

        // test
        updatePasswordWithAudit (1,"password") 
        updatePasswordWithAudit (1,"new password") 

        // AUDIT: calling updatePassword with (1, "password")
        // AUDIT: calling updatePassword with (1, "new password")

        // ----------------------------------------
        let updatePasswordOnce = 
            onlyOnce CustomerDatabase.updatePassword 

        // test
        updatePasswordOnce (1,"password") |> printfn "Result 1st time: %A"
        updatePasswordOnce (1,"password") |> printfn "Result 2nd time: %A"

        // Result 1st time: Success "OK"
        // Result 2nd time: Failure OnlyAllowedOnce

        // ----------------------------------------
        let revokableUpdatePassword, revoker = 
            revokable CustomerDatabase.updatePassword 

        // test
        revokableUpdatePassword (1,"password") |> printfn "Result 1st time before revoking: %A"
        revokableUpdatePassword (1,"password") |> printfn "Result 2nd time before revoking: %A"
        revoker()
        revokableUpdatePassword (1,"password") |> printfn "Result 3nd time after revoking: %A"

        // Result 1st time before revoking: Success "OK"
        // Result 2nd time before revoking: Success "OK"
        // Result 3nd time after revoking: Failure Revoked