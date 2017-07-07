(*
Calculator_design.fsx

Related blog post: http://fsharpforfunandprofit.com/posts/calculator-design/
*)

// ================================================
// First version of domain 
// ================================================          
module CalculatorDomain_V1 =
    type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
    and CalculatorState = {
        display: CalculatorDisplay
        }
    and CalculatorDisplay = string
    and CalculatorInput = 
        | Zero | One | Two | Three | Four 
        | Five | Six | Seven | Eight | Nine
        | DecimalSeparator
        | Add | Subtract | Multiply | Divide
        | Equals | Clear

// ================================================
// Second attempt at CalculatorInput
// - move Digit to its own new type
// ================================================          
module CalculatorInput_V2 =

    type CalculatorDigit = 
        | Zero | One | Two | Three | Four 
        | Five | Six | Seven | Eight | Nine
        | DecimalSeparator

    type CalculatorInput = 
        | Digit of CalculatorDigit
        | Add | Subtract | Multiply | Divide
        | Equals | Clear

// ================================================
// Third attempt at CalculatorInput
// - move other inputs to special types as well
// ================================================          
module CalculatorInput_V3 =

    type CalculatorDigit = 
        | Zero | One | Two | Three | Four 
        | Five | Six | Seven | Eight | Nine
        | DecimalSeparator

    type CalculatorMathOp = 
        | Add | Subtract | Multiply | Divide

    type CalculatorAction = 
        | Equals | Clear

    type CalculatorInput = 
        | Digit of CalculatorDigit
        | Op of CalculatorMathOp
        | Action of CalculatorAction

// ================================================
// Second version of domain
//
// Added 
// * UpdateDisplayFromDigit 
// * DoMathOperation and related
// ================================================          
module CalculatorDomain_V2 =

    type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
    and CalculatorState = {
        display: CalculatorDisplay
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

    type UpdateDisplayFromDigit = CalculatorDigit * CalculatorDisplay -> CalculatorDisplay    

        
    type DoMathOperation = CalculatorMathOp * Number * Number -> MathOperationResult 
    and Number = float
    and MathOperationResult = 
        | Success of Number 
        | Failure of MathOperationError
    and MathOperationError = 
        | DivideByZero


// ================================================
// Third version of Domain
//
// Added 
// * pendingOp in CalculatorState 
// * GetDisplayNumber and related
// * Services record
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
    type SetDisplayError = MathOperationError -> CalculatorDisplay 

    type InitState = unit -> CalculatorState 

    type CalculatorServices = {
        updateDisplayFromDigit: UpdateDisplayFromDigit 
        doMathOperation: DoMathOperation 
        getDisplayNumber: GetDisplayNumber 
        setDisplayNumber: SetDisplayNumber 
        setDisplayError: SetDisplayError 
        initState: InitState 
        }


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