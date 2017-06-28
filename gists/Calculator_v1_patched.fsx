(*
Calculator_v1_patched.fsx

Related blog post: http://fsharpforfunandprofit.com/posts/calculator-complete-v1/
*)

// ================================================
// Patched implementation of Domain
// ================================================          
module CalculatorDomain =

    type Calculate = CalculatorInput * CalculatorState -> CalculatorState 
    and CalculatorState = {
        display: CalculatorDisplay
        pendingOp: (CalculatorMathOp * Number) option
        allowAppend: bool
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

    let updateDisplayFromDigit services digit state =
        let buffer = 
            if state.allowAppend then
                state.display
            else
                ""
        let newDisplay = services.updateDisplayFromDigit (digit,buffer)
        let newState = {state with display=newDisplay; allowAppend=true}
        newState //return

    let updateDisplayFromPendingOp services state =

        // helper to do the math op
        let doMathOp (op,pendingNumber,currentNumber) = 
            let result = services.doMathOperation (op,pendingNumber,currentNumber)
            let newDisplay = 
                match result with
                | Success resultNumber ->
                    services.setDisplayNumber resultNumber 
                | Failure error -> 
                    services.setDisplayError error
            {display=newDisplay; pendingOp=None; allowAppend=false}
            
        // fetch the two options and combine them
        let newState = maybe {
            let! (op,pendingNumber) = state.pendingOp
            let! currentNumber = services.getDisplayNumber state.display
            return doMathOp (op,pendingNumber,currentNumber)
            }
        newState |> ifNone state

    let addPendingMathOp services op state = 
        maybe {            
            let! currentNumber = 
                state.display |> services.getDisplayNumber 
            let pendingOp = Some (op,currentNumber)
            return {state with pendingOp=pendingOp; allowAppend=false}
            }
        |> ifNone state // return original state if anything fails

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

    let updateDisplayFromDigit (config:Configuration) :UpdateDisplayFromDigit = 
        fun (digit, display) ->

        // determine what character should be appended to the display
        let appendCh= 
            match digit with
            | Zero -> 
                // only allow one 0 at start of display
                if display="0" then "" else "0"
            | One -> "1"
            | Two -> "2"
            | Three-> "3"
            | Four -> "4"
            | Five -> "5"
            | Six-> "6"
            | Seven-> "7"
            | Eight-> "8"
            | Nine-> "9"
            | DecimalSeparator -> 
                if display="" then 
                    // handle empty display with special case
                    "0" + config.decimalSeparator  
                else if display.Contains(config.decimalSeparator) then 
                    // don't allow two decimal separators
                    "" 
                else 
                    config.decimalSeparator
        
        // ignore new input if there are too many digits
        if (display.Length > config.maxDisplayLength) then
            display // ignore new input
        else
            // append the new char
            display + appendCh

    let getDisplayNumber :GetDisplayNumber = fun display ->
        match System.Double.TryParse display with
        | true, d -> Some d
        | false, _ -> None

    let setDisplayNumber :SetDisplayNumber = fun f ->
        sprintf "%g" f

    let setDisplayError divideByZeroMsg :SetDisplayError = fun f ->
        match f with
        | DivideByZero -> divideByZeroMsg

    let doMathOperation  :DoMathOperation = fun (op,f1,f2) ->
        match op with
        | Add -> Success (f1 + f2)
        | Subtract -> Success (f1 - f2)
        | Multiply -> Success (f1 * f2)
        | Divide -> 
            try
                Success (f1 / f2)
            with
            | :? System.DivideByZeroException -> 
                Failure DivideByZero 

    let initState :InitState = fun () -> 
        {
        display=""
        pendingOp = None
        allowAppend = true
        }

    let createServices (config:Configuration) = {
        updateDisplayFromDigit = updateDisplayFromDigit config
        doMathOperation = doMathOperation
        getDisplayNumber = getDisplayNumber
        setDisplayNumber = setDisplayNumber
        setDisplayError = setDisplayError (config.divideByZeroMsg)
        initState = initState
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

    type CalculatorForm(initState:InitState, calculate:Calculate) as this = 
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

        // initialization before constructor
        let mutable state = initState()

        // a function that sets the displayed text
        let mutable setDisplayedText = 
            fun text -> () // do nothing

        // traditional style -- have a label control as a field 
        // let mutable displayControl :Label = null

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
             setDisplayedText state.display 
            
        let handleDigit digit =
             Digit digit |> handleInput 
        
        let handleOp op =
             Op op |> handleInput 

        let handleAction action =
             Action action |> handleInput 

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
            this.Font <- new Font(FontFamily.GenericSansSerif,14.f)
            let clientSizeX = (2*margin) + (5*buttonDimension) + (4*buttonPadding)
            let clientSizeY = (2*margin) + (5*buttonDimension) + (4*buttonPadding)
            this.ClientSize <- Size(clientSizeX,clientSizeY)
            this.CenterToScreen()

            let keyPressHandler = new KeyPressEventHandler(fun obj e -> this.KeyPressHandler(e))
            this.KeyPress.AddHandler keyPressHandler 
            this.KeyPreview <- true  // let the form handle key events

            this.CreateButtons()
            this.CreateDisplayLabel()

        /// Use a member rather than a let-bound function so it can be called from the constructor
        member this.CreateDisplayLabel() = 
            let displayWidth = 5*buttonDimension + 4*buttonPadding
            let displaySize = Size(displayWidth,buttonDimension)
            let display = new Label(Text="",Size=displaySize,Location=getPos(0,0))
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
            let addDigitButton digit (button:Button) =
                button.Click.AddHandler(EventHandler(fun _ _ -> handleDigit digit))
                this.Controls.Add(button)

            let addOpButton op (button:Button) =
                button.Click.AddHandler(EventHandler(fun _ _ -> handleOp op))
                this.Controls.Add(button)

            let addActionButton misc (button:Button) =
                button.Click.AddHandler(EventHandler(fun _ _ -> handleAction misc))
                this.Controls.Add(button)


            let sevenButton = new Button(Text="7",Size=buttonSize,Location=getPos(1,0),BackColor=DigitButtonColor)
            sevenButton |> addDigitButton Seven

            let eightButton = new Button(Text="8",Size=buttonSize,Location=getPos(1,1),BackColor=DigitButtonColor)
            eightButton |> addDigitButton Eight

            let nineButton = new Button(Text="9",Size=buttonSize,Location=getPos(1,2),BackColor=DigitButtonColor)
            nineButton |> addDigitButton Nine

            let clearButton = new Button(Text="C",Size=buttonSize,Location=getPos(1,3),BackColor=DangerButtonColor)
            clearButton |> addActionButton Clear

            let addButton = new Button(Text="+",Size=doubleHeightSize,Location=getPos(1,4),BackColor=OpButtonColor)
            addButton |> addOpButton Add

            let fourButton = new Button(Text="4",Size=buttonSize,Location=getPos(2,0),BackColor=DigitButtonColor)
            fourButton |> addDigitButton Four

            let fiveButton = new Button(Text="5",Size=buttonSize,Location=getPos(2,1),BackColor=DigitButtonColor)
            fiveButton |> addDigitButton Five

            let sixButton = new Button(Text="6",Size=buttonSize,Location=getPos(2,2),BackColor=DigitButtonColor)
            sixButton |> addDigitButton Six

            let divideButton = new Button(Text="/",Size=buttonSize,Location=getPos(2,3),BackColor=OpButtonColor)
            divideButton |> addOpButton Divide

            let oneButton = new Button(Text="1",Size=buttonSize,Location=getPos(3,0),BackColor=DigitButtonColor)
            oneButton |> addDigitButton One

            let twoButton = new Button(Text="2",Size=buttonSize,Location=getPos(3,1),BackColor=DigitButtonColor)
            twoButton |> addDigitButton Two

            let threeButton = new Button(Text="3",Size=buttonSize,Location=getPos(3,2),BackColor=DigitButtonColor)
            threeButton |> addDigitButton Three

            let multButton = new Button(Text="*",Size=buttonSize,Location=getPos(3,3),BackColor=OpButtonColor)
            multButton |> addOpButton Multiply

            let equalButton = new Button(Text="=",Size=doubleHeightSize,Location=getPos(3,4),BackColor=OpButtonColor)
            equalButton |> addActionButton Equals

            let zeroButton = new Button(Text="0",Size=doubleWidthSize,Location=getPos(4,0),BackColor=DigitButtonColor)
            zeroButton |> addDigitButton Zero

            let pointButton = new Button(Text=decimalSeparator,Size=buttonSize,Location=getPos(4,2),BackColor=DigitButtonColor)
            pointButton |> addDigitButton DecimalSeparator

            let minusButton = new Button(Text="-",Size=buttonSize,Location=getPos(4,3),BackColor=OpButtonColor)
            minusButton |> addOpButton Subtract

        member this.KeyPressHandler(e:KeyPressEventArgs) =
            match e.KeyChar with
            | '0' -> handleDigit Zero
            | '1' -> handleDigit One
            | '2' -> handleDigit Two
            | '3' -> handleDigit Three
            | '4' -> handleDigit Four
            | '5' -> handleDigit Five
            | '6' -> handleDigit Six
            | '7' -> handleDigit Seven
            | '8' -> handleDigit Eight
            | '9' -> handleDigit Nine
            | '.' | ',' -> handleDigit DecimalSeparator
            | '+' -> handleOp Add
            | '-' -> handleOp Subtract
            | '/' -> handleOp Divide
            | '*' -> handleOp Multiply
            | '=' | '\n' | '\r' -> handleAction Equals
            | 'C' | 'c' -> handleAction Clear
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

    let emptyState = services.initState()

    /// Given a sequence of inputs, start with the empty state
    /// and apply each input in turn. The final state is returned
    let processInputs inputs = 
        // helper for fold
        let folder state input = 
            calculate(input,state)

        inputs 
        |> List.fold folder emptyState 

    /// Check that the state contains the expected display value
    let assertResult testLabel expected state =
        let actual = state.display
        if (expected <> actual) then
            printfn "Test %s failed: expected=%s actual=%s" testLabel expected actual 
        else
            printfn "Test %s passed" testLabel 

    let ``when I input 1 + 2, I expect 3``() = 
        [Digit One; Op Add; Digit Two; Action Equals]
        |> processInputs 
        |> assertResult "1+2=3" "3"

    let ``when I input 1 + 2 + 3, I expect 6``() = 
        [Digit One; Op Add; Digit Two; Op Add; Digit Three; Action Equals]
        |> processInputs 
        |> assertResult "1+2+3=6" "6"

    // run tests
    do 
        ``when I input 1 + 2, I expect 3``()
        ``when I input 1 + 2 + 3, I expect 6``() 


// ================================================
// Bootstrapper
// ================================================          

// assemble everything
open CalculatorDomain
open System

let config = CalculatorConfiguration.loadConfig()
let services = CalculatorServices.createServices config 
let initState = services.initState
let calculate = CalculatorImplementation.createCalculate services

let form = new CalculatorUI.CalculatorForm(initState,calculate)
form.Show()



