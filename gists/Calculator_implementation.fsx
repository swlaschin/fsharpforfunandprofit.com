(*
Calculator_implementation.fsx

Related blog post: http://fsharpforfunandprofit.com/posts/calculator-implementation/
*)

// ================================================
// Draft of Domain from previous file
// ================================================          
module CalculatorDomain_V3 =

    type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
    and CalculatorState = {
        display: CalculatorDisplay
        pendingOp: (CalculatorMathOp * Number) option
        }
    and CalculatorDisplay = string
    and CalculatorInput = 
        | Digit of CalculatorDigit
        | Op of CalculatorMathOp
        | Action of CalculatorAction
    and CalculatorDigit = 
        | Zero | One | Two | Three | Four 
        | Five | Six | Seven | Eight | Nine
        | DecimalSeparator
    and CalculatorMathOp = 
        | Add | Subtract | Multiply | Divide
    and CalculatorAction = 
        | Equals | Clear
    and UpdateDisplayFromDigit = CalculatorDigit * CalculatorDisplay -> CalculatorDisplay
    and DoMathOperation = CalculatorMathOp * Number * Number -> MathOperationResult 
    and Number = float
    and MathOperationResult = 
        | Success of Number 
        | Failure of MathOperationError
    and MathOperationError = 
        | DivideByZero

    type GetDisplayNumber = CalculatorDisplay -> Number option
    type SetDisplayNumber = Number -> CalculatorDisplay 

    // added when missing requirement for error display needed
    type SetDisplayError = MathOperationError -> CalculatorDisplay 

    type InitState = unit -> CalculatorState 

    type CalculatorServices = {
        updateDisplayFromDigit: UpdateDisplayFromDigit 
        doMathOperation: DoMathOperation 
        getDisplayNumber: GetDisplayNumber 
        setDisplayNumber: SetDisplayNumber 
        setDisplayError: SetDisplayError // added for missing requirement 
        initState: InitState 
        }

// ================================================
// Utilities
// ================================================          
[<AutoOpen>]
module CommonComputationExpressions  =
    
    type MaybeBuilder() =
        member this.Bind(x, f) = Option.bind f x
        member this.Return(x) = Some x
   
    let maybe = new MaybeBuilder()
        
// ================================================
// First implementation of Calculator
// ================================================          
module CalculatorImplementation_V1 =
    open CalculatorDomain_V3 

    let updateDisplayFromDigit services digit state =
        let newDisplay = services.updateDisplayFromDigit (digit,state.display)
        let newState = {state with display=newDisplay}
        newState //return

    // First version of updateDisplayFromPendingOp 
    // * very imperative and ugly
    let updateDisplayFromPendingOp services state =
        if state.pendingOp.IsSome then
            let op,pendingNumber = state.pendingOp.Value
            let currentNumberOpt = services.getDisplayNumber state.display
            if currentNumberOpt.IsSome then
                let currentNumber = currentNumberOpt.Value 
                let result = services.doMathOperation (op,pendingNumber,currentNumber)
                match result with
                | Success resultNumber ->
                    let newDisplay = services.setDisplayNumber resultNumber 
                    let newState = {display=newDisplay;pendingOp=None}
                    newState //return
                | Failure error -> 
                    state // original state is untouched
            else
                state // original state is untouched
        else
            state // original state is untouched

    // Second version of updateDisplayFromPendingOp 
    // * Uses "bind"
    // * Doesn't show errors on display in Failure case
    let updateDisplayFromPendingOp_v2 services state =
        // helper to extract CurrentNumber
        let getCurrentNumber (op,pendingNumber) = 
            state.display
            |> services.getDisplayNumber 
            |> Option.map (fun currentNumber -> (op,pendingNumber,currentNumber))

        // helper to do the math op
        let doMathOp (op,pendingNumber,currentNumber) = 
            let result = services.doMathOperation (op,pendingNumber,currentNumber)
            match result with
            | Success resultNumber ->
                let newDisplay = services.setDisplayNumber resultNumber 
                let newState = {display=newDisplay;pendingOp=None}
                Some newState //return something
            | Failure error -> 
                None // failed
    
        // connect all the helpers
        state.pendingOp
        |> Option.bind getCurrentNumber
        |> Option.bind doMathOp 
        |> defaultArg <| state


    // helper to make defaultArg better for piping
    let ifNone defaultValue input = 
        // just reverse the parameters!
        defaultArg input defaultValue 

    // Third version of updateDisplayFromPendingOp 
    // * Updated to show errors on display in Failure case
    // * replaces awkward defaultArg syntax
    let updateDisplayFromPendingOp_v3 services state =
        // helper to extract CurrentNumber
        let getCurrentNumber (op,pendingNumber) = 
            state.display
            |> services.getDisplayNumber 
            |> Option.map (fun currentNumber -> (op,pendingNumber,currentNumber))

        // helper to do the math op
        let doMathOp (op,pendingNumber,currentNumber) = 
            let result = services.doMathOperation (op,pendingNumber,currentNumber)
            let newDisplay = 
                match result with
                | Success resultNumber ->
                    services.setDisplayNumber resultNumber 
                | Failure error -> 
                    services.setDisplayError error
            let newState = {display=newDisplay;pendingOp=None}
            Some newState //return something
    
        // connect all the helpers
        state.pendingOp
        |> Option.bind getCurrentNumber
        |> Option.bind doMathOp 
        |> ifNone state // return original state if anything fails


    // Fourth version of updateDisplayFromPendingOp 
    // * Changed to use "maybe" computation expression
    let updateDisplayFromPendingOp_v4 services state =

        // helper to do the math op
        let doMathOp (op,pendingNumber,currentNumber) = 
            let result = services.doMathOperation (op,pendingNumber,currentNumber)
            let newDisplay = 
                match result with
                | Success resultNumber ->
                    services.setDisplayNumber resultNumber 
                | Failure error -> 
                    services.setDisplayError error
            {display=newDisplay;pendingOp=None}
            
        // fetch the two options and combine them
        let newState = maybe {
            let! (op,pendingNumber) = state.pendingOp
            let! currentNumber = services.getDisplayNumber state.display
            return doMathOp (op,pendingNumber,currentNumber)
            }
        newState |> ifNone state

    // First version of addPendingMathOp 
    // * very imperative and ugly
    let addPendingMathOp services op state = 
        let currentNumberOpt = services.getDisplayNumber state.display
        if currentNumberOpt.IsSome then 
            let currentNumber = currentNumberOpt.Value 
            let pendingOp = Some (op,currentNumber)
            let newState = {state with pendingOp=pendingOp}
            newState //return
        else                
            state // original state is untouched

    // Second version of addPendingMathOp 
    // * Uses "map" and helper function
    let addPendingMathOp_v2 services op state = 
        let newStateWithPending currentNumber =
            let pendingOp = Some (op,currentNumber)
            {state with pendingOp=pendingOp}
            
        state.display
        |> services.getDisplayNumber 
        |> Option.map newStateWithPending 
        |> ifNone state

    // Third version of addPendingMathOp 
    // * Uses "maybe"
    let addPendingMathOp_v3 services op state = 
        maybe {            
            let! currentNumber = 
                state.display |> services.getDisplayNumber 
            let pendingOp = Some (op,currentNumber)
            return {state with pendingOp=pendingOp}
            }
        |> ifNone state // return original state if anything fails


    // creates a calculate function
    let createCalculate (services:CalculatorServices) :Calculate = 
        fun (input,state) -> 
            match input with
            | Digit d ->
                let newState = updateDisplayFromDigit services d state
                newState //return
            | Op op ->
                let newState1 = updateDisplayFromPendingOp services state
                let newState2 = addPendingMathOp services op newState1 
                newState2 //return
            | Action Clear ->
                let newState = services.initState()
                newState //return
            | Action Equals ->
                let newState = updateDisplayFromPendingOp services state
                newState //return

    /// Alternate version of createCalculate that uses an inner function rather than a lambda
    let createCalculate_V2 (services:CalculatorServices) :Calculate = 
        let innerCalculate (input,state) = 
            match input with
            | Digit d -> state // not implemented
            | Op op -> state // not implemented
            | Action Clear -> state // not implemented
            | Action Equals -> state // not implemented
        innerCalculate // return the inner function



// ================================================
// Example of how bootstrapper code would work
// with services
// ================================================          
(*
// assemble everything
open CalculatorDomain
open System

let services = CalculatorServices.createServices()
let initState = services.initState
let calculate = CalculatorImplementation.createCalculate services

let form = new CalculatorUI.CalculatorForm(initState,calculate)
form.Show()
*)


