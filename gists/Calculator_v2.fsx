(*
Calculator_v2.fsx

Related blog post: http://fsharpforfunandprofit.com/posts/calculator-complete-v2/
*)

// ================================================
// Domain using a state machine
// ================================================          
module CalculatorDomain =

    type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
    // five states        
    and CalculatorState = 
        | ZeroState of ZeroStateData 
        | AccumulatorState of AccumulatorStateData 
        | AccumulatorWithDecimalState of AccumulatorStateData 
        | ComputedState of ComputedStateData 
        | ErrorState of ErrorStateData 
    // six inputs
    and CalculatorInput = 
        | Zero 
        | Digit of NonZeroDigit
        | DecimalSeparator
        | MathOp of CalculatorMathOp
        | Equals 
        | Clear
    // data associated with each state
    and ZeroStateData = 
        PendingOp option
    and AccumulatorStateData = 
        {digits:DigitAccumulator; pendingOp:PendingOp option}
    and ComputedStateData = 
        {displayNumber:Number; pendingOp:PendingOp option}
    and ErrorStateData = 
        MathOperationError
    // other types referenced from above 
    and DigitAccumulator = string
    and PendingOp = (CalculatorMathOp * Number)
    and Number = float
    and NonZeroDigit= 
        | One | Two | Three | Four 
        | Five | Six | Seven | Eight | Nine
    and CalculatorMathOp = 
        | Add | Subtract | Multiply | Divide
    and MathOperationResult = 
        | Success of Number 
        | Failure of MathOperationError
    and MathOperationError = 
        | DivideByZero

    // services used by the calculator itself
    type AccumulateNonZeroDigit = NonZeroDigit * DigitAccumulator -> DigitAccumulator 
    type AccumulateZero = DigitAccumulator -> DigitAccumulator 
    type AccumulateSeparator = DigitAccumulator -> DigitAccumulator 
    type DoMathOperation = CalculatorMathOp * Number * Number -> MathOperationResult 
    type GetNumberFromAccumulator = AccumulatorStateData -> Number

    // services used by the UI or testing
    type GetDisplayFromState = CalculatorState -> string
    type GetPendingOpFromState = CalculatorState -> string

    type CalculatorServices = {
        accumulateNonZeroDigit :AccumulateNonZeroDigit 
        accumulateZero :AccumulateZero 
        accumulateSeparator :AccumulateSeparator
        doMathOperation :DoMathOperation 
        getNumberFromAccumulator :GetNumberFromAccumulator 
        getDisplayFromState :GetDisplayFromState 
        getPendingOpFromState :GetPendingOpFromState 
        }

// ================================================
// Utilities
// ================================================          
[<AutoOpen>]
module CommonComputationExpressions =
    
    type MaybeBuilder() =
        member this.Bind(x, f) = Option.bind f x
        member this.Return(x) = Some x
   
    let maybe = new MaybeBuilder()

// ================================================
// Implementation of Calculator
// ================================================          
module CalculatorImplementation =
    open CalculatorDomain

    // helper to make defaultArg better for piping
    let ifNone defaultValue input = 
        // just reverse the parameters!
        defaultArg input defaultValue 

    let accumulateNonZeroDigit services digit accumulatorData =
        let digits = accumulatorData.digits
        let newDigits = services.accumulateNonZeroDigit (digit,digits)
        let newAccumulatorData = {accumulatorData with digits=newDigits}
        newAccumulatorData // return

    let accumulateZero services accumulatorData =
        let digits = accumulatorData.digits
        let newDigits = services.accumulateZero digits
        let newAccumulatorData = {accumulatorData with digits=newDigits}
        newAccumulatorData // return

    let accumulateSeparator services accumulatorData =
        let digits = accumulatorData.digits
        let newDigits = services.accumulateSeparator digits
        let newAccumulatorData = {accumulatorData with digits=newDigits}
        newAccumulatorData // return

    let getComputationState services accumulatorStateData nextOp = 

        // helper to create a new ComputedState from a given displayNumber 
        // and the nextOp parameter
        let getNewState displayNumber =
            let newPendingOp = 
                nextOp |> Option.map (fun op -> op,displayNumber )
            {displayNumber=displayNumber; pendingOp = newPendingOp }
            |> ComputedState

        let currentNumber = 
            services.getNumberFromAccumulator accumulatorStateData 

        // If there is no pending op, create a new ComputedState using the currentNumber
        let computeStateWithNoPendingOp = 
            getNewState currentNumber 

        maybe {
            let! (op,previousNumber) = accumulatorStateData.pendingOp
            let result = services.doMathOperation(op,previousNumber,currentNumber)
            let newState =
                match result with
                | Success resultNumber ->
                    // If there was a pending op, create a new ComputedState using the result
                    getNewState resultNumber 
                | Failure error -> 
                    error |> ErrorState
            return newState
            } |> ifNone computeStateWithNoPendingOp 

    let replacePendingOp (computedStateData:ComputedStateData) nextOp = 
        let newPending = maybe {
            let! existing,displayNumber  = computedStateData.pendingOp
            let! next = nextOp
            return next,displayNumber  
            }
        {computedStateData with pendingOp=newPending}
        |> ComputedState

    let handleZeroState services pendingOp input = 
        // create a new accumulatorStateData object that is used when transitioning to other states
        let accumulatorStateData = {digits=""; pendingOp=pendingOp}
        match input with
        | Zero -> 
            ZeroState pendingOp // stay in ZeroState 
        | Digit digit -> 
            accumulatorStateData 
            |> accumulateNonZeroDigit services digit 
            |> AccumulatorState  // transition to AccumulatorState  
        | DecimalSeparator -> 
            accumulatorStateData 
            |> accumulateSeparator services 
            |> AccumulatorWithDecimalState  // transition to AccumulatorWithDecimalState  
        | MathOp op -> 
            let nextOp = Some op
            let newState = getComputationState services accumulatorStateData nextOp 
            newState  // transition to ComputedState or ErrorState
        | Equals -> 
            let nextOp = None
            let newState = getComputationState services accumulatorStateData nextOp 
            newState  // transition to ComputedState or ErrorState
        | Clear -> 
            ZeroState None // transition to ZeroState and throw away any pending ops

    let handleAccumulatorState services stateData input = 
        match input with
        | Zero -> 
            stateData 
            |> accumulateZero services 
            |> AccumulatorState  // stay in AccumulatorState  
        | Digit digit -> 
            stateData 
            |> accumulateNonZeroDigit services digit 
            |> AccumulatorState  // stay in AccumulatorState  
        | DecimalSeparator -> 
            stateData 
            |> accumulateSeparator services 
            |> AccumulatorWithDecimalState  // transition to AccumulatorWithDecimalState
        | MathOp op -> 
            let nextOp = Some op
            let newState = getComputationState services stateData nextOp 
            newState  // transition to ComputedState or ErrorState
        | Equals -> 
            let nextOp = None
            let newState = getComputationState services stateData nextOp 
            newState  // transition to ComputedState or ErrorState
        | Clear -> 
            ZeroState None // transition to ZeroState and throw away any pending ops

    let handleAccumulatorWithDecimalState services stateData input = 
        match input with
        | Zero -> 
            stateData
            |> accumulateZero services 
            |> AccumulatorWithDecimalState // stay in AccumulatorWithDecimalState 
        | Digit digit -> 
            stateData
            |> accumulateNonZeroDigit services digit 
            |> AccumulatorWithDecimalState  // stay in AccumulatorWithDecimalState 
        | DecimalSeparator -> 
            //ignore
            stateData 
            |> AccumulatorWithDecimalState  // stay in AccumulatorWithDecimalState 
        | MathOp op -> 
            let nextOp = Some op
            let newState = getComputationState services stateData nextOp 
            newState  // transition to ComputedState or ErrorState
        | Equals -> 
            let nextOp = None
            let newState = getComputationState services stateData nextOp 
            newState  // transition to ComputedState or ErrorState
        | Clear -> 
            ZeroState None // transition to ZeroState and throw away any pending ops

    let handleComputedState services stateData input = 
        let emptyAccumulatorStateData = {digits=""; pendingOp=stateData.pendingOp}
        match input with
        | Zero -> 
            ZeroState stateData.pendingOp  // transition to ZeroState with any pending ops
        | Digit digit -> 
            emptyAccumulatorStateData 
            |> accumulateNonZeroDigit services digit 
            |> AccumulatorState  // transition to AccumulatorState  
        | DecimalSeparator -> 
            emptyAccumulatorStateData 
            |> accumulateSeparator services 
            |> AccumulatorWithDecimalState  // transition to AccumulatorWithDecimalState  
        | MathOp op -> 
            // replace the pending op, if any
            let nextOp = Some op
            replacePendingOp stateData nextOp 
        | Equals -> 
            // replace the pending op, if any
            let nextOp = None
            replacePendingOp stateData nextOp 
        | Clear -> 
            ZeroState None // transition to ZeroState and throw away any pending ops

    let handleErrorState stateData input =
        match input with
        | Zero 
        | Digit _ 
        | DecimalSeparator 
        | MathOp _ 
        | Equals -> 
            // stay in error state             
            ErrorState stateData
        | Clear -> 
            ZeroState None // transition to ZeroState and throw away any pending ops
        
    let createCalculate (services:CalculatorServices) :Calculate = 
        // create some local functions with partially applied services
        let handleZeroState = handleZeroState services
        let handleAccumulator = handleAccumulatorState services
        let handleAccumulatorWithDecimal = handleAccumulatorWithDecimalState services
        let handleComputed = handleComputedState services
        let handleError = handleErrorState 

        fun (input,state) -> 
            match state with
            | ZeroState stateData-> 
                handleZeroState stateData input
            | AccumulatorState stateData -> 
                handleAccumulator stateData input
            | AccumulatorWithDecimalState stateData -> 
                handleAccumulatorWithDecimal stateData input
            | ComputedState stateData -> 
                handleComputed stateData input
            | ErrorState stateData -> 
                handleError stateData input

// ================================================
// Implementation of CalculatorConfiguration
// ================================================          
module CalculatorConfiguration =

    // A record to store configuration options
    // (e.g. loaded from a file or environment)
    type Configuration = {
        decimalSeparator : string
        divideByZeroMsg : string
        maxDisplayLength: int
        }

    let loadConfig() = {
        decimalSeparator = 
            System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator
        divideByZeroMsg = "ERR-DIV0" 
        maxDisplayLength = 10
        }
        
// ================================================
// Implementation of CalculatorServices 
// ================================================          
module CalculatorServices =
    open CalculatorDomain
    open CalculatorConfiguration

    let appendToAccumulator maxLen (accumulator:DigitAccumulator) appendCh = 
        // ignore new input if there are too many digits
        if (accumulator.Length > maxLen) then
            accumulator // ignore new input
        else
            // append the new char
            accumulator + appendCh

    let accumulateNonZeroDigit maxLen :AccumulateNonZeroDigit = 
        fun (digit, accumulator) ->

        // determine what character should be appended to the display
        let appendCh= 
            match digit with
            | One -> "1"
            | Two -> "2"
            | Three-> "3"
            | Four -> "4"
            | Five -> "5"
            | Six-> "6"
            | Seven-> "7"
            | Eight-> "8"
            | Nine-> "9"
        appendToAccumulator maxLen accumulator appendCh

    let accumulateZero maxLen :AccumulateZero = 
        fun accumulator ->
            let appendCh = "0"
            appendToAccumulator maxLen accumulator "0"

    let accumulateSeparator maxLen :AccumulateSeparator = 
        fun accumulator ->
            let appendCh = 
                if accumulator = "" then "0." else "."
            appendToAccumulator maxLen accumulator appendCh

    let getNumberFromAccumulator :GetNumberFromAccumulator =
        fun accumulatorStateData ->
            let digits = accumulatorStateData.digits
            match System.Double.TryParse digits with
            | true, d -> d
            | false, _ -> 0.0

    let doMathOperation  :DoMathOperation = fun (op,f1,f2) ->
        match op with
        | Add -> Success (f1 + f2)
        | Subtract -> Success (f1 - f2)
        | Multiply -> Success (f1 * f2)
        | Divide -> 
            if f2 = 0.0 then
                Failure DivideByZero 
            else
                Success (f1 / f2)

    let getDisplayFromState divideByZeroMsg :GetDisplayFromState =
        
        // helper
        let floatToString = sprintf "%g" 
        
        fun calculatorState ->
            match calculatorState with
            | ZeroState _ -> "0"
            | AccumulatorState stateData 
            | AccumulatorWithDecimalState stateData -> 
                stateData 
                |> getNumberFromAccumulator 
                |> floatToString 
            | ComputedState stateData -> 
                stateData.displayNumber
                 |> floatToString 
            | ErrorState stateData -> 
                match stateData with
                | DivideByZero -> divideByZeroMsg

    let getPendingOpFromState :GetPendingOpFromState=

        let opToString = function
            | Add -> "+" 
            | Subtract -> "-"
            | Multiply -> "*"  
            | Divide -> "/"

        let displayStringForPendingOp pendingOp =
            maybe {
                let! op, number = pendingOp 
                return sprintf "%g %s" number (opToString op)
                }
            |> defaultArg <| ""

        fun calculatorState ->
            match calculatorState with
            | ZeroState pendingOp -> 
                displayStringForPendingOp pendingOp 
            | AccumulatorState stateData 
            | AccumulatorWithDecimalState stateData -> 
                stateData.pendingOp 
                |> displayStringForPendingOp 
            | ComputedState stateData -> 
                stateData.pendingOp
                 |> displayStringForPendingOp 
            | ErrorState stateData -> 
                ""

    let createServices (config:Configuration) = {
        accumulateNonZeroDigit = accumulateNonZeroDigit (config.maxDisplayLength)
        accumulateZero = accumulateZero (config.maxDisplayLength)
        accumulateSeparator = accumulateSeparator (config.maxDisplayLength)
        doMathOperation = doMathOperation
        getNumberFromAccumulator = getNumberFromAccumulator 
        getDisplayFromState = getDisplayFromState (config.divideByZeroMsg)
        getPendingOpFromState = getPendingOpFromState
        }


// ================================================
// Implementation of Calculator UI 
// ================================================          
module CalculatorUI =

    open System
    open System.Drawing
    open System.Drawing.Drawing2D
    open System.Windows.Forms
    open CalculatorDomain

    type CalculatorForm(initialState:CalculatorState, calculate:Calculate, getDisplay:GetDisplayFromState, getPendingOp:GetPendingOpFromState) as this = 
        inherit Form()

        // constants
        let margin = 20
        let buttonDimension = 50
        let buttonPadding = 10
        let doubleDimension = buttonDimension + buttonPadding + buttonDimension
        let gridSize = buttonDimension + buttonPadding

        let buttonSize = Size(buttonDimension,buttonDimension)
        let doubleWidthSize = Size(doubleDimension,buttonDimension)
        let doubleHeightSize = Size(buttonDimension,doubleDimension)
        let decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator

        let DigitButtonColor = Color.White
        let OpButtonColor = Color.PeachPuff
        let DangerButtonColor = Color.Coral

        let largeFont = new Font(FontFamily.GenericSansSerif,14.f)
        let smallFont = new Font(FontFamily.GenericSansSerif,9.f)

        // initialization before constructor
        let mutable state = initialState

        // a function that sets the displayed text
        let mutable setDisplayedText = 
            fun text -> () // do nothing
        // traditional style -- have a label control as a field 
        // let mutable displayControl :Label = null

        // a function that sets the pending op text
        let mutable setPendingOpText = 
            fun text -> () // do nothing

        // ========================
        // private helper functions
        // ========================

        /// Get the physical location, given a row and column.
        /// Row/col are 0-based
        let getPos(row,col) = 
            let x = margin + (col*gridSize) 
            let y = margin + (row* gridSize) 
            Point(x,y)

        let handleInput input =
             let newState = calculate(input,state)
             state <- newState 
             setDisplayedText (getDisplay state)
             setPendingOpText (getPendingOp state)
            
        // ========================
        // initialization in constructor
        // ========================
        do
            this.SetupForm()


        // ========================
        // Public methods
        // ========================

        /// Create a member rather than let-bound so it can be called from constructor
        member this.SetupForm() = 
            this.Text <- "Calculator"
            this.Font <- largeFont 
            let clientSizeX = (2*margin) + (5*buttonDimension) + (4*buttonPadding)
            let clientSizeY = (2*margin) + (5*buttonDimension) + (4*buttonPadding)
            this.ClientSize <- Size(clientSizeX,clientSizeY)
            this.CenterToScreen()

            let keyPressHandler = new KeyPressEventHandler(fun obj e -> this.KeyPressHandler(e))
            this.KeyPress.AddHandler keyPressHandler 
            this.KeyPreview <- true  // let the form handle keypress events

            this.CreateButtons()
            this.CreateDisplayLabel()

        /// Use a member rather than a let-bound function so it can be called from the constructor
        member this.CreateDisplayLabel() = 
            let pendingOpHeight = largeFont.Height
            let displayWidth = 5*buttonDimension + 4*buttonPadding

            // add a label to display the pending op 
            let pendingOpSize = Size(displayWidth,pendingOpHeight)
            let pendingOpLocation = getPos(0,0) 
            let pendingOp = new Label(Text="",Size=pendingOpSize,Location=pendingOpLocation)
            pendingOp.TextAlign <- ContentAlignment.BottomRight
            pendingOp.BackColor <- Color.White
            pendingOp.Font <- smallFont
            this.Controls.Add(pendingOp)

            setPendingOpText <-
                (fun text -> pendingOp.Text <- text)

            // add a label to display the current result
            let displaySize = Size(displayWidth,buttonDimension - pendingOpHeight)
            let displayLocation = getPos(0,0) 
            displayLocation.Offset(0,pendingOpHeight) // shift down below pending op label
            let display = new Label(Text="",Size=displaySize,Location=displayLocation)
            display.TextAlign <- ContentAlignment.MiddleRight
            display.BackColor <- Color.White
            this.Controls.Add(display)

            // update the function that sets the text
            setDisplayedText <-
                (fun text -> display.Text <- text)
            // traditional style - set the field when the form has been initialized
            // displayControl <- display



        /// Use a member rather than a let-bound function so it can be called from the constructor
        member this.CreateButtons() = 
            let addButtonControl input (button:Button) =
                button.Click.AddHandler(EventHandler(fun _ _ -> handleInput input))
                this.Controls.Add(button)

            let sevenButton = new Button(Text="7",Size=buttonSize,Location=getPos(1,0),BackColor=DigitButtonColor)
            sevenButton |> addButtonControl (Digit Seven)

            let eightButton = new Button(Text="8",Size=buttonSize,Location=getPos(1,1),BackColor=DigitButtonColor)
            eightButton |> addButtonControl (Digit Eight)

            let nineButton = new Button(Text="9",Size=buttonSize,Location=getPos(1,2),BackColor=DigitButtonColor)
            nineButton |> addButtonControl (Digit Nine)

            let clearButton = new Button(Text="C",Size=buttonSize,Location=getPos(1,3),BackColor=DangerButtonColor)
            clearButton |> addButtonControl Clear

            let addButton = new Button(Text="+",Size=doubleHeightSize,Location=getPos(1,4),BackColor=OpButtonColor)
            addButton |> addButtonControl (MathOp Add)

            let fourButton = new Button(Text="4",Size=buttonSize,Location=getPos(2,0),BackColor=DigitButtonColor)
            fourButton |> addButtonControl (Digit Four)

            let fiveButton = new Button(Text="5",Size=buttonSize,Location=getPos(2,1),BackColor=DigitButtonColor)
            fiveButton |> addButtonControl (Digit Five)

            let sixButton = new Button(Text="6",Size=buttonSize,Location=getPos(2,2),BackColor=DigitButtonColor)
            sixButton |> addButtonControl (Digit Six)

            let divideButton = new Button(Text="/",Size=buttonSize,Location=getPos(2,3),BackColor=OpButtonColor)
            divideButton |> addButtonControl (MathOp Divide)

            let oneButton = new Button(Text="1",Size=buttonSize,Location=getPos(3,0),BackColor=DigitButtonColor)
            oneButton |> addButtonControl (Digit One)

            let twoButton = new Button(Text="2",Size=buttonSize,Location=getPos(3,1),BackColor=DigitButtonColor)
            twoButton |> addButtonControl (Digit Two)

            let threeButton = new Button(Text="3",Size=buttonSize,Location=getPos(3,2),BackColor=DigitButtonColor)
            threeButton |> addButtonControl (Digit Three)

            let multButton = new Button(Text="*",Size=buttonSize,Location=getPos(3,3),BackColor=OpButtonColor)
            multButton |> addButtonControl (MathOp Multiply)

            let equalButton = new Button(Text="=",Size=doubleHeightSize,Location=getPos(3,4),BackColor=OpButtonColor)
            equalButton |> addButtonControl Equals

            let zeroButton = new Button(Text="0",Size=doubleWidthSize,Location=getPos(4,0),BackColor=DigitButtonColor)
            zeroButton |> addButtonControl Zero

            let pointButton = new Button(Text=decimalSeparator,Size=buttonSize,Location=getPos(4,2),BackColor=DigitButtonColor)
            pointButton |> addButtonControl DecimalSeparator

            let minusButton = new Button(Text="-",Size=buttonSize,Location=getPos(4,3),BackColor=OpButtonColor)
            minusButton |> addButtonControl (MathOp Subtract)

        member this.KeyPressHandler(e:KeyPressEventArgs) =
            match e.KeyChar with
            | '0' -> handleInput Zero
            | '1' -> handleInput (Digit One)
            | '2' -> handleInput (Digit Two)
            | '3' -> handleInput (Digit Three)
            | '4' -> handleInput (Digit Four)
            | '5' -> handleInput (Digit Five)
            | '6' -> handleInput (Digit Six)
            | '7' -> handleInput (Digit Seven)
            | '8' -> handleInput (Digit Eight)
            | '9' -> handleInput (Digit Nine)
            | '.' | ',' -> handleInput DecimalSeparator
            | '+' -> handleInput (MathOp Add)
            | '-' -> handleInput (MathOp Subtract)
            | '/' -> handleInput (MathOp Divide)
            | '*' -> handleInput (MathOp Multiply)
            | '=' | '\n' | '\r' -> handleInput Equals
            | 'C' | 'c' -> handleInput Clear
            | _ -> ()

// ================================================
// Tests
// ================================================          

module CalculatorTests =
    open CalculatorDomain
    open System

    let config = CalculatorConfiguration.loadConfig()
    let services = CalculatorServices.createServices config 
    let calculate = CalculatorImplementation.createCalculate services

    let initialState = ZeroState None

    /// Given a sequence of inputs, start with the empty state
    /// and apply each input in turn. The final state is returned
    let processInputs inputs = 
        // helper for fold
        let folder state input = 
            calculate(input,state)

        inputs 
        |> List.fold folder initialState 

    /// Check that the state contains the expected display value
    let assertResult testLabel expected state =
        let actual = services.getDisplayFromState state
        if (expected <> actual) then
            printfn "Test %s failed: expected=%s actual=%s" testLabel expected actual 
        else
            printfn "Test %s passed" testLabel 

    let ``when I input 1, I expect 1``() = 
        [Digit One; ]
        |> processInputs 
        |> assertResult "1" "1"

    let ``when I input 1+, I expect 1``() = 
        [Digit One; MathOp Add]
        |> processInputs 
        |> assertResult "1+" "1"

    let ``when I input 1=, I expect 1``() = 
        [Digit One; Equals]
        |> processInputs 
        |> assertResult "1=" "1"

    let ``when I input 1+2, I expect 2``() = 
        [Digit One; MathOp Add; Digit Two]
        |> processInputs 
        |> assertResult "1+2" "2"

    let ``when I input 1+2=, I expect 3``() = 
        [Digit One; MathOp Add; Digit Two; Equals]
        |> processInputs 
        |> assertResult "1+2=" "3"

    let ``when I input 1+2+, I expect 3``() = 
        [Digit One; MathOp Add; Digit Two; MathOp Add; ]
        |> processInputs 
        |> assertResult "1+2+" "3"

    let ``when I input 1+2+4, I expect 4``() = 
        [Digit One; MathOp Add; Digit Two; MathOp Add; Digit Four]
        |> processInputs 
        |> assertResult "1+2+4" "4"

    let ``when I input 1+2+4=, I expect 7``() = 
        [Digit One; MathOp Add; Digit Two; MathOp Add; Digit Four; Equals]
        |> processInputs 
        |> assertResult "1+2+4=" "7"

    let ``when I input 4+-3=, I expect 1``() = 
        [Digit Four; MathOp Add; MathOp Subtract; Digit Three; Equals]
        |> processInputs 
        |> assertResult "4+-3=" "1"

    // run tests
    do 
        ``when I input 1, I expect 1``()  
        ``when I input 1+, I expect 1``() 
        ``when I input 1=, I expect 1``() 
        ``when I input 1+2, I expect 2``() 
        ``when I input 1+2=, I expect 3``() 
        ``when I input 1+2+, I expect 3``() 
        ``when I input 1+2+4, I expect 4``() 
        ``when I input 1+2+4=, I expect 7``() 
        ``when I input 4+-3=, I expect 1``() 

// ================================================
// Bootstrapper
// ================================================          

// assemble everything
open CalculatorDomain
open System

let config = CalculatorConfiguration.loadConfig()
let services = CalculatorServices.createServices config 
let initialState = ZeroState None
let calculate = CalculatorImplementation.createCalculate services

let form = new CalculatorUI.CalculatorForm(initialState,calculate,services.getDisplayFromState,services.getPendingOpFromState)
form.Show()



