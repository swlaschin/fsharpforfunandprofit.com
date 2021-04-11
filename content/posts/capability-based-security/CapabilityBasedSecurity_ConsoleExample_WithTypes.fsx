(*
CapabilityBasedSecurity_ConsoleExample_WithTypes.fsx

An example of a capability-based console application that also includes authorization and access tokens.

Related blog post: http://fsharpforfunandprofit.com/posts/capability-based-security-3/
*)

open System.Security.Principal
open System

// ================================================
// A complete console application demonstrating capabilities
// ================================================

module Rop = 
    type SuccessFailure<'a,'b> = 
        | Success of 'a
        | Failure of 'b

    let bind f = function
        | Success x -> f x
        | Failure e -> Failure e 

    let map f = function
        | Success x -> Success (f x)
        | Failure e -> Failure e 

    let orElse errValue = function
        | Success x -> x
        | Failure _ -> errValue

/// Core domain types shares across the application
module Domain = 
    open Rop

    type CustomerId = CustomerId of int
    type CustomerData = CustomerData of string
    type Password = Password of string

    type FailureCase = 
        | AuthenticationFailed of string
        | AuthorizationFailed
        | CustomerNameNotFound of string
        | CustomerIdNotFound of CustomerId
        | OnlyAllowedOnce
        | CapabilityRevoked

// ----------------------------------------------

/// Capabilities that are available in the application
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

                
// ----------------------------------------------

/// Functions related to authentication
module Authentication = 
    open Rop
    open Domain 

    let customerRole = "Customer"
    let customerAgentRole = "CustomerAgent"

    let makePrincipal name role = 
        let iden = GenericIdentity(name)
        let principal = GenericPrincipal(iden,[|role|])
        principal :> IPrincipal

    let authenticate name = 
        match name with
        | "Alice" | "Bob" -> 
            makePrincipal name customerRole  |> Success
        | "Zelda" -> 
            makePrincipal name customerAgentRole |> Success
        | _ -> 
            AuthenticationFailed name |> Failure 

    let customerIdForName name = 
        match name with
        | "Alice" -> CustomerId 1 |> Success
        | "Bob" -> CustomerId 2 |> Success
        | _ -> CustomerNameNotFound name |> Failure

    let customerIdOwnedByPrincipal customerId (principle:IPrincipal) = 
        principle.Identity.Name
        |> customerIdForName 
        |> Rop.map (fun principalId -> principalId = customerId)
        |> Rop.orElse false

// ----------------------------------------------

/// Functions related to authorization
module Authorization = 
    open Rop
    open Domain 
    open Capabilities

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

    // return the first good capability, if any
    let first capabilityList = 
        capabilityList |> List.tryPick id

    // given a capability option, restrict it
    let restrict filter originalCap = 
        originalCap
        |> Option.bind filter 

    /// Uses of the capability will be audited
    let auditable capabilityName principalName f = 
        fun x -> 
            // simple audit log!
            let timestamp = DateTime.UtcNow.ToString("u")
            printfn "AUDIT: User %s used capability %s at %s" principalName capabilityName timestamp 
            // use the capability
            f x

    /// Return a pair of functions: the revokable capability, 
    /// and the revoker function
    let revokable f = 
        let allow = ref true
        let capability = fun x -> 
            if !allow then  //! is dereferencing not negation!
                f x
            else
                Failure CapabilityRevoked
        let revoker() = 
            allow := false
        capability, revoker

// ----------------------------------------------

/// Functions related to database access
module CustomerDatabase = 
    open Rop
    open System.Collections.Generic
    open Domain 
    open Capabilities
    open Authorization

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

// ----------------------------------------------

module BusinessServices =
    open Rop
    open Domain

    // use the getCustomer capability
    let getCustomer capability =
        match capability() with
        | Success data -> printfn "%A" data
        | Failure err -> printfn ".. %A" err

    // use the updateCustomer capability
    let updateCustomer capability =
        printfn "Enter new data: "
        let customerData = Console.ReadLine() |> CustomerData
        match capability customerData  with
        | Success _ -> printfn "Data updated" 
        | Failure err -> printfn ".. %A" err

    // use the updatePassword capability
    let updatePassword capability =
        printfn "Enter new password: "
        let password = Console.ReadLine() |> Password
        match capability password  with
        | Success _ -> printfn "Password updated" 
        | Failure err -> printfn ".. %A" err

// ----------------------------------------------

module UserInterface =
    open Rop
    open Domain
    open Capabilities

    type CurrentState = 
        | LoggedOut
        | LoggedIn of IPrincipal
        | CustomerSelected of IPrincipal * CustomerId
        | Exit

    /// do the actions available while you are logged out. Return the new state
    let loggedOutActions originalState = 
        printfn "[Login] enter Alice, Bob, Zelda, or Exit: "
        let action = Console.ReadLine()
        match action with
        | "Exit"  -> 
            // Change state to Exit
            Exit
        | name -> 
            // otherwise try to authenticate the name
            match Authentication.authenticate name with
            | Success principal -> 
                LoggedIn principal
            | Failure err -> 
                printfn ".. %A" err
                originalState

    /// do the actions available while you are logged in. Return the new state
    let loggedInActions originalState (principal:IPrincipal) = 
        printfn "[%s] Pick a customer to work on. Enter Alice, Bob, or Logout: " principal.Identity.Name
        let action = Console.ReadLine()

        match action with
        | "Logout"  -> 
            // Change state to LoggedOut
            LoggedOut
        // otherwise treat it as a customer name
        | customerName -> 
            // Attempt to find customer            
            match Authentication.customerIdForName customerName with
            | Success customerId -> 
                // found -- change state
                CustomerSelected (principal,customerId)
            | Failure err -> 
                // not found -- stay in originalState 
                printfn ".. %A" err
                originalState 

    let getAvailableCapabilities capabilityProvider customerId principal = 
        let getCustomer = capabilityProvider.getCustomer customerId principal 
        let updateCustomer = capabilityProvider.updateCustomer customerId principal 
        let updatePassword = capabilityProvider.updatePassword customerId principal 
        getCustomer,updateCustomer,updatePassword  

    /// do the actions available when a selected customer is available. Return the new state
    let selectedCustomerActions originalState capabilityProvider customerId principal = 
        
        // get the individual component capabilities from the provider
        let getCustomerCap,updateCustomerCap,updatePasswordCap = 
            getAvailableCapabilities capabilityProvider customerId principal

        // get the text for menu options based on capabilities that are present
        let menuOptionTexts = 
            [
            getCustomerCap |> Option.map (fun _ -> "(G)et");
            updateCustomerCap |> Option.map (fun _ -> "(U)pdate");
            updatePasswordCap |> Option.map (fun _ -> "(P)assword");
            ] 
            |> List.choose id

        // show the menu        
        let actionText =
            match menuOptionTexts with
            | [] -> " (no other actions available)"
            | texts -> texts |> List.reduce (fun s t -> s + ", " + t) 
        printfn "[%s] (D)eselect customer, %s" principal.Identity.Name actionText 

        // process the user action
        let action = Console.ReadLine().ToUpper()
        match action with
        | "D" -> 
            // revert to logged in with no selected customer
            LoggedIn principal
        | "G" -> 
            // use Option.iter in case we don't have the capability
            getCustomerCap 
              |> Option.iter BusinessServices.getCustomer 
            originalState  // stay in same state
        | "U" -> 
            updateCustomerCap 
              |> Option.iter BusinessServices.updateCustomer 
            originalState  
        | "P" -> 
            updatePasswordCap 
              |> Option.iter BusinessServices.updatePassword
            originalState  
        | _ -> 
            // unknown option
            originalState  

    let rec mainUiLoop capabilityProvider state =
        match state with
        | LoggedOut -> 
            let newState = loggedOutActions state 
            mainUiLoop capabilityProvider newState 
        | LoggedIn principal -> 
            let newState = loggedInActions state principal
            mainUiLoop capabilityProvider newState 
        | CustomerSelected (principal,customerId) ->
            let newState = selectedCustomerActions state capabilityProvider customerId principal 
            mainUiLoop capabilityProvider newState 
        | Exit -> 
            () // done 

    let start capabilityProvider  = 
        mainUiLoop capabilityProvider LoggedOut

// ----------------------------------------------

/// Top level module
module Application=
    open Rop
    open Domain
    open CustomerDatabase 
    open Authentication
    open Authorization
    open Capabilities

    let capabilities = 
        
        // apply the token, if present,
        // to a function which has only the token as a parameter
        let tokenToCap f token =
            token 
            |> Option.map (fun token -> 
                fun () -> f token)

        // apply the token, if present,
        // to a function which has the token and other parameters
        let tokenToCap2 f token =
            token 
            |> Option.map (fun token -> 
                fun x -> f token x)

        let getCustomerOnlyForSameId id principal  = 
            let accessToken = Authorization.onlyForSameId id principal
            accessToken |> tokenToCap CustomerDatabase.getCustomer 

        let getCustomerOnlyForAgentsInBusinessHours id principal = 
            let accessToken = Authorization.onlyForAgents id principal
            let cap1 = accessToken |> tokenToCap CustomerDatabase.getCustomer 
            let restriction f = onlyIfDuringBusinessHours (DateTime.Now) f
            cap1 |> restrict restriction 

        let getCustomerOnlyForSameId_OrForAgentsInBusinessHours id principal = 
            let cap1 = getCustomerOnlyForSameId id principal 
            let cap2 = getCustomerOnlyForAgentsInBusinessHours id principal 
            first [cap1; cap2]

        let updateCustomerOnlyForSameId id principal = 
            let accessToken = Authorization.onlyForSameId id principal
            accessToken |> tokenToCap2 CustomerDatabase.updateCustomer

        let updateCustomerOnlyForAgentsInBusinessHours id principal = 
            let accessToken = Authorization.onlyForAgents id principal
            let cap1 = accessToken |> tokenToCap2 CustomerDatabase.updateCustomer
            // uncomment to get the restriction
            // let restriction f = onlyIfDuringBusinessHours (DateTime.Now) f
            let restriction = Some  // no restriction
            cap1 |> restrict restriction 

        let updateCustomerOnlyForSameId_OrForAgentsInBusinessHours id principal = 
            let cap1 = updateCustomerOnlyForSameId id principal 
            let cap2 = updateCustomerOnlyForAgentsInBusinessHours id principal 
            first [cap1; cap2]

        let updatePasswordOnlyForSameId id principal = 
            let accessToken = Authorization.passwordUpdate id principal
            let cap = accessToken |> tokenToCap2 CustomerDatabase.updatePassword
            cap 
            |> Option.map (auditable "UpdatePassword" principal.Identity.Name) 

        // create the record that contains the capabilities
        {
        getCustomer = getCustomerOnlyForSameId_OrForAgentsInBusinessHours 
        updateCustomer = updateCustomerOnlyForSameId_OrForAgentsInBusinessHours 
        updatePassword = updatePasswordOnlyForSameId 
        }         


    let start() = 
        // pass capabilities to UI
        UserInterface.start capabilities 
// compile all the code above

// and then run this separately to start the app
Application.start()