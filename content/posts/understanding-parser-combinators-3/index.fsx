

//===========================================
// The parser library from part 2
//===========================================

open System

module ParserLibraryFromPart2 =

    /// Type that represents Success/Failure in parsing
    type ParseResult<'a> =
        | Success of 'a
        | Failure of string

    //>Parser_v1
    /// Type that wraps a parsing function
    type Parser<'T> = Parser of (string -> ParseResult<'T * string>)
    //<

    /// Parse a single character
    let pchar charToMatch =
        // define a nested inner function
        let innerFn str =
            if String.IsNullOrEmpty(str) then
                Failure "No more input"
            else
                let first = str.[0]
                if first = charToMatch then
                    let remaining = str.[1..]
                    Success (charToMatch,remaining)
                else
                    let msg = sprintf "Expecting '%c'. Got '%c'" charToMatch first
                    Failure msg
        // return the "wrapped" inner function
        Parser innerFn

    /// Run a parser with some input
    let run parser input =
        // unwrap parser to get inner function
        let (Parser innerFn) = parser
        // call inner function with input
        innerFn input

    /// "bindP" takes a parser-producing function f, and a parser p
    /// and passes the output of p into f, to create a new parser
    let bindP f p =
        let innerFn input =
            let result1 = run p input
            match result1 with
            | Failure err ->
                // return error from parser1
                Failure err
            | Success (value1,remainingInput) ->
                // apply f to get a new parser
                let p2 = f value1
                // run parser with remaining input
                run p2 remainingInput
        Parser innerFn

    /// Infix version of bindP
    let ( >>= ) p f = bindP f p

    /// Lift a value to a Parser
    let returnP x =
        let innerFn input =
            // ignore the input and return x
            Success (x,input)
        // return the inner function
        Parser innerFn

    /// apply a function to the value inside a parser
    let mapP f =
        bindP (f >> returnP)

    /// infix version of mapP
    let ( <!> ) = mapP

    /// "piping" version of mapP
    let ( |>> ) x f = mapP f x

    /// apply a wrapped function to a wrapped value
    let applyP fP xP =
        fP >>= (fun f ->
        xP >>= (fun x ->
            returnP (f x) ))

    /// infix version of apply
    let ( <*> ) = applyP

    /// lift a two parameter function to Parser World
    let lift2 f xP yP =
        returnP f <*> xP <*> yP

    /// Combine two parsers as "A andThen B"
    let andThen p1 p2 =
        p1 >>= (fun p1Result ->
        p2 >>= (fun p2Result ->
            returnP (p1Result,p2Result) ))

    /// Infix version of andThen
    let ( .>>. ) = andThen

    /// Combine two parsers as "A orElse B"
    let orElse p1 p2 =
        let innerFn input =
            // run parser1 with the input
            let result1 = run p1 input

            // test the result for Failure/Success
            match result1 with
            | Success result ->
                // if success, return the original result
                result1

            | Failure err ->
                // if failed, run parser2 with the input
                let result2 = run p2 input

                // return parser2's result
                result2

        // return the inner function
        Parser innerFn

    /// Infix version of orElse
    let ( <|> ) = orElse

    /// Choose any of a list of parsers
    let choice listOfParsers =
        List.reduce ( <|> ) listOfParsers

    /// Choose any of a list of characters
    let anyOf listOfChars =
        listOfChars
        |> List.map pchar // convert into parsers
        |> choice

    /// Convert a list of Parsers into a Parser of a list
    let rec sequence parserList =
        // define the "cons" function, which is a two parameter function
        let cons head tail = head::tail

        // lift it to Parser World
        let consP = lift2 cons

        // process the list of parsers recursively
        match parserList with
        | [] ->
            returnP []
        | head::tail ->
            consP head (sequence tail)

    /// (helper) match zero or more occurrences of the specified parser
    let rec parseZeroOrMore parser input =
        // run parser with the input
        let firstResult = run parser input
        // test the result for Failure/Success
        match firstResult with
        | Failure err ->
            // if parse fails, return empty list
            ([],input)
        | Success (firstValue,inputAfterFirstParse) ->
            // if parse succeeds, call recursively
            // to get the subsequent values
            let (subsequentValues,remainingInput) =
                parseZeroOrMore parser inputAfterFirstParse
            let values = firstValue::subsequentValues
            (values,remainingInput)

    /// matches zero or more occurrences of the specified parser
    let many parser =
        let innerFn input =
            // parse the input -- wrap in Success as it always succeeds
            Success (parseZeroOrMore parser input)

        Parser innerFn

    /// matches one or more occurrences of the specified parser
    let many1 p =
        p      >>= (fun head ->
        many p >>= (fun tail ->
            returnP (head::tail) ))

    /// Parses an optional occurrence of p and returns an option value.
    let opt p =
        let some = p |>> Some
        let none = returnP None
        some <|> none

    /// Keep only the result of the left side parser
    let (.>>) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the first value
        |> mapP (fun (a,b) -> a)

    /// Keep only the result of the right side parser
    let (>>.) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the second value
        |> mapP (fun (a,b) -> b)

    /// Keep only the result of the middle parser
    let between p1 p2 p3 =
        p1 >>. p2 .>> p3

    /// Parses one or more occurrences of p separated by sep
    let sepBy1 p sep =
        let sepThenP = sep >>. p
        p .>>. many sepThenP
        |>> fun (p,pList) -> p::pList

    /// Parses zero or more occurrences of p separated by sep
    let sepBy p sep =
        sepBy1 p sep <|> returnP []

open ParserLibraryFromPart2

//===========================================
// 1. Labelling a Parser
//===========================================

module ConfusingErrors =

    //>ConfusingErrors
    let parseDigit = anyOf ['0'..'9']
    run parseDigit "|ABC"  // Failure "Expecting '9'. Got '|'"
    //<

// fix - add a label to the Parser
module LabelledParser =

    //>Parser_labelled
    type ParserLabel = string

    /// A Parser structure has a parsing function & label
    type Parser<'a> = {
        parseFn : (string -> ParseResult<'a * string>)
        label:  ParserLabel
        }
    //<

// ==========================================================

/// Update the library with labelled Failure case and labelled Parser
module ParserLibrary_Labelled =

    open System

    //>Failure_labelled
    // Aliases
    type ParserLabel = string
    type ParserError = string

    type ParseResult<'a> =
        | Success of 'a
        | Failure of ParserLabel * ParserError
    //<

    /// A Parser structure has a parsing function & label
    type Parser<'a> = {
        parseFn : (string -> ParseResult<'a * string>)
        label:  ParserLabel
        }

    //>printResult
    let printResult result =
        match result with
        | Success (value,_input) ->
            printfn "%A" value
        | Failure (label,error) ->
            printfn "Error parsing %s\n%s" label error
    //<

    //--------------------------------------
    // new label functions
    //--------------------------------------

    //>getLabel
    /// get the label from a parser
    let getLabel parser =
        // get label
        parser.label
    //<

    //>setLabel
    /// Update the label in the parser
    let setLabel parser newLabel =
        // change the inner function to use the new label
        let newInnerFn input =
            let result = parser.parseFn input
            match result with
            | Success s ->
                // if Success, do nothing
                Success s
            | Failure (oldLabel,err) ->
                // if Failure, return new label
                Failure (newLabel,err)        // <====== use newLabel here
        // return the Parser
        {parseFn=newInnerFn; label=newLabel}  // <====== use newLabel here
    //<

    //>setLabel_op
    /// infix version of setLabel
    let ( <?> ) = setLabel
    //<

    //--------------------------------------
    // combinators updated to handle labels
    //--------------------------------------

    //>run_labelled
    /// Run a parser with some input
    let run (parser:Parser<_>) input =
        // call inner function with input
        parser.parseFn input
    //<

    //>bindP_labelled
    /// "bindP" takes a parser-producing function f, and a parser p
    /// and passes the output of p into f, to create a new parser
    let bindP f p =
        let label = "unknown"           // <== changed
        let innerFn input =
            let result1 = run p input
            match result1 with
            | Failure (label,err) ->    // <== changed
                // return failure from parser1
                Failure (label,err)
            | Success (value1,remainingInput) ->
                // apply f to get a new parser
                let p2 = f value1
                // run parser with remaining input
                run p2 remainingInput
        {parseFn=innerFn; label=label}  // <== changed
    //<

    /// Infix version of bindP
    let ( >>= ) p f = bindP f p

    /// Lift a value to a Parser
    let returnP x =
        let label = sprintf "%A" x
        let innerFn input =
            // ignore the input and return x
            Success (x,input)
        // return the inner function
        {parseFn=innerFn; label=label}  // <== changed

    /// apply a function to the value inside a parser
    let mapP f =
        bindP (f >> returnP)

    /// infix version of mapP
    let ( <!> ) = mapP

    /// "piping" version of mapP
    let ( |>> ) x f = mapP f x

    /// apply a wrapped function to a wrapped value
    let applyP fP xP =
        fP >>= (fun f ->
        xP >>= (fun x ->
            returnP (f x) ))

    /// infix version of apply
    let ( <*> ) = applyP

    /// lift a two parameter function to Parser World
    let lift2 f xP yP =
        returnP f <*> xP <*> yP

    //>andThen_labelled
    /// Combine two parsers as "A andThen B"
    let andThen p1 p2 =
        let label = sprintf "%s andThen %s" (getLabel p1) (getLabel p2)
        p1 >>= (fun p1Result ->
        p2 >>= (fun p2Result ->
            returnP (p1Result,p2Result) ))
        <?> label     // <====== provide a default label
    //<

    /// Infix version of andThen
    let ( .>>. ) = andThen

    //>orElse_labelled
    /// Combine two parsers as "A orElse B"
    let orElse parser1 parser2 =
        // construct a new label
        let label =  // <====== provide a default label
            sprintf "%s orElse %s" (getLabel parser1) (getLabel parser2)

        let innerFn input =
            //etc
            //<

            // run parser1 with the input
            let result1 = run parser1 input

            // test the result for Failure/Success
            match result1 with
            | Success result ->
                // if success, return the result
                result1
            | Failure (_,err) ->
                // if failed, run parser2 with the input
                let result2 = run parser2 input

                // return parser2's result
                match result2 with
                | Success _ ->
                    // if success, return the result
                    result2
                | Failure (_,err) ->
                    // if failed, return the error with overall label
                    Failure (label,err)

        // return the Parser
        {parseFn=innerFn; label=label}


    /// Infix version of orElse
    let ( <|> ) = orElse

    /// Choose any of a list of parsers
    let choice listOfParsers =
        List.reduce ( <|> ) listOfParsers

    /// parse a char
    let pchar charToMatch =
        let label = sprintf "%c" charToMatch
        let innerFn input =
            if String.IsNullOrEmpty(input) then
                Failure (label,"No more input")
            else
                let first = input.[0]
                if first = charToMatch then
                    let remainingInput = input.[1..]
                    Success (charToMatch,remainingInput)
                else
                    let err = sprintf "Unexpected '%c'" first
                    Failure (label,err)
        // return the parser
        {parseFn=innerFn;label=label}

    //>anyOf_labelled
    /// Choose any of a list of characters
    let anyOf listOfChars =
        let label = sprintf "any of %A" listOfChars
        listOfChars
        |> List.map pchar // convert into parsers
        |> choice
        <?> label         // <====== provide a default label
    //<

open ParserLibrary_Labelled

// ----------------------------------------------------
// Test the labelled functions


module SetLabel_Test =

    //>setLabel_test
    let parseDigit_WithLabel =
        anyOf ['0'..'9']
        <?> "digit"

    run parseDigit_WithLabel "|ABC"
    |> printResult
    //<

    (*
    //>setLabel_test_out
    Error parsing digit
    Unexpected '|'
    //<
    *)

//===========================================
// 2. Replacing "pchar" with "satisfy"
//===========================================

//>satisfy
/// Match an input token if the predicate is satisfied
let satisfy predicate label =
    let innerFn input =
        if String.IsNullOrEmpty(input) then
            Failure (label,"No more input")
        else
            let first = input.[0]
            if predicate first then      // <====== use predicate here
                let remainingInput = input.[1..]
                Success (first,remainingInput)
            else
                let err = sprintf "Unexpected '%c'" first
                Failure (label,err)
    // return the parser
    {parseFn=innerFn;label=label}
//<

//>pchar_satisfy
/// parse a char
let pchar charToMatch =
    let predicate ch = (ch = charToMatch)
    let label = sprintf "%c" charToMatch
    satisfy predicate label
//<


module WithoutSatisfy =

    //>digitChar_anyof
    /// parse a digit
    let digitChar =
        anyOf ['0'..'9']
    //<

// But now we can rewrite it using a predicate directly, making it a lot more efficient:

module WithSatisfy =

    //>digitChar_satisfy
    /// parse a digit
    let digitChar =
        let predicate = Char.IsDigit
        let label = "digit"
        satisfy predicate label
    //<

    // Similarly, we can create a more efficient whitespace parser too:

    //>whitespaceChar_satisfy
    /// parse a whitespace char
    let whitespaceChar =
        let predicate = Char.IsWhiteSpace
        let label = "whitespace"
        satisfy predicate label
    //<


//===========================================
// Adding position and context to error messages
//===========================================

//>Position
type Position = {
    line : int
    column : int
}

/// define an initial position
let initialPos = {line=0; column=0}

/// increment the column number
let incrCol (pos:Position) =
    {pos with column=pos.column + 1}

/// increment the line number and set the column to 0
let incrLine pos =
    {line=pos.line + 1; column=0}
//<

//>InputState
/// Define the current input state
type InputState = {
    lines : string[]
    position : Position
}
//<


// We will also need a way to convert a string into a initial `InputState`:

//>fromStr
module InputState =
    /// Create a new InputState from a string
    let fromStr str =
        if String.IsNullOrEmpty(str) then
            {lines=[||]; position=initialPos}
        else
            let separators = [| "\r\n"; "\n" |]
            let lines = str.Split(separators, StringSplitOptions.None)
            {lines=lines; position=initialPos}
//<


    //>nextChar
    // return the current line
    let currentLine inputState =
        let linePos = inputState.position.line
        if linePos < inputState.lines.Length then
            inputState.lines.[linePos]
        else
            "end of file"

    /// Get the next character from the input, if any
    /// else return None. Also return the updated InputState
    /// Signature: InputState -> InputState * char option
    let nextChar input =
        let linePos = input.position.line
        let colPos = input.position.column
        // three cases
        // 1) if line >= maxLine ->
        //       return EOF
        // 2) if col less than line length ->
        //       return char at colPos, increment colPos
        // 3) if col at line length ->
        //       return NewLine, increment linePos

        if linePos >= input.lines.Length then
            input, None
        else
            let currentLine = currentLine input
            if colPos < currentLine.Length then
                let char = currentLine.[colPos]
                let newPos = incrCol input.position
                let newState = {input with position=newPos}
                newState, Some char
            else
                // end of line, so return LF and move to next line
                let char = '\n'
                let newPos = incrLine input.position
                let newState = {input with position=newPos}
                newState, Some char
    //<

module InputState_Test =
    // Let's quickly test that the implementation works. We'll create a helper function `readAllChars` and then see what it returns for different inputs:

    //>readAllChars
    let rec readAllChars input =
        [
            let remainingInput,charOpt = InputState.nextChar input
            match charOpt with
            | None ->
                // end of input
                ()
            | Some ch ->
                // return first character
                yield ch
                // return the remaining characters
                yield! readAllChars remainingInput
        ]
    //<

    // Here it is with some example inputs:

    //>readAllChars_test
    InputState.fromStr "" |> readAllChars
        //=> []
    InputState.fromStr "a" |> readAllChars
        //=> ['a'; '\010']
    InputState.fromStr "ab" |> readAllChars
        //=> ['a'; 'b'; '\010']
    InputState.fromStr "a\nb" |> readAllChars
        //=> ['a'; '\010'; 'b'; '\010']
    //<

//------------------------------------------------
// Changing the parser to use the new input type

//>ParserPosition
/// Stores information about the parser position for error messages
type ParserPosition = {
    currentLine : string
    line : int
    column : int
    }
//<

// We'll need some way to convert a `InputState` into a `ParserPosition`:

//>parserPositionFromInputState
let parserPositionFromInputState (inputState:InputState) = {
    currentLine = InputState.currentLine inputState
    line = inputState.position.line
    column = inputState.position.column
    }
//<

// update the `ParseResult` type to include `ParserPosition`:

//>ParseResult_withPosition
type ParseResult<'a> =
    | Success of 'a
    | Failure of ParserLabel * ParserError * ParserPosition
//<

// the `Parser` type needs to change from `string` to `InputState`:

//>Parser_withPosition
/// A Parser structure has a parsing function & label
type Parser<'a> = {
    parseFn : (InputState -> ParseResult<'a * InputState>)
    label:  ParserLabel
    }
//<

// the `printResult` function can be enhanced to print the text of the current line

//>printResult_withPosition
let printResult result =
    match result with
    | Success (value,input) ->
        printfn "%A" value
    | Failure (label,error,parserPos) ->
        let errorLine = parserPos.currentLine
        let colPos = parserPos.column
        let linePos = parserPos.line
        let failureCaret = sprintf "%*s^%s" colPos "" error
        printfn "Line:%i Col:%i Error parsing %s\n%s\n%s" linePos colPos label errorLine failureCaret
//<


module PrintResultWithPosition_Test =
    // Let's test `printResult` with a dummy error value:

    //>printResult_withPosition_test
    let exampleError =
        Failure ("identifier", "unexpected |",
                 {currentLine = "123 ab|cd"; line=1; column=6})

    printResult exampleError
    //<


    (*
    //>printResult_withPosition_out
    Line:1 Col:6 Error parsing identifier
    123 ab|cd
          ^unexpected |
    //<
    *)


//===========================================
// Fixing up the `run` function
//===========================================

//>runOnInput
/// Run the parser on a InputState
let runOnInput parser input =
    // call inner function with input
    parser.parseFn input

/// Run the parser on a string
let run parser inputStr =
    // call inner function with input
    runOnInput parser (InputState.fromStr inputStr)
//<


//===========================================
// Fixing up the combinators
//===========================================

module CombinatorsWithPosition =

    /// get the label from a parser
    let getLabel parser =
        // get label
        parser.label

    /// update the label in the parser
    let setLabel parser newLabel =
        // change the inner function to use the new label
        let newInnerFn input =
            let result = parser.parseFn input
            match result with
            | Success s ->
                // if Success, do nothing
                Success s
            | Failure (oldLabel,err,pos) ->
                // if Failure, return new label
                Failure (newLabel,err,pos)
        // return the Parser
        {parseFn=newInnerFn; label=newLabel}

    /// infix version of setLabel
    let ( <?> ) = setLabel

    //>satisfy_withPosition
    /// Match an input token if the predicate is satisfied
    let satisfy predicate label =
        let innerFn input =
            let remainingInput,charOpt = InputState.nextChar input
            match charOpt with
            | None ->
                let err = "No more input"
                let pos = parserPositionFromInputState input
                //Failure (label,err)     // <====== old version
                Failure (label,err,pos)   // <====== new version
            | Some first ->
                if predicate first then
                    Success (first,remainingInput)
                else
                    let err = sprintf "Unexpected '%c'" first
                    let pos = parserPositionFromInputState input
                    //Failure (label,err)     // <====== old version
                    Failure (label,err,pos)   // <====== new version
        // return the parser
        {parseFn=innerFn;label=label}
    //<


    //>bindP_withPosition
    /// "bindP" takes a parser-producing function f, and a parser p
    /// and passes the output of p into f, to create a new parser
    let bindP f p =
        let label = "unknown"
        let innerFn input =
            let result1 = runOnInput p input
            match result1 with
            | Failure (label,err,pos) ->     // <====== new with pos
                // return error from parser1
                Failure (label,err,pos)
            | Success (value1,remainingInput) ->
                // apply f to get a new parser
                let p2 = f value1
                // run parser with remaining input
                runOnInput p2 remainingInput
        {parseFn=innerFn; label=label}
    //<


    /// Infix version of bindP
    let ( >>= ) p f = bindP f p

    /// Lift a value to a Parser
    let returnP x =
        let label = sprintf "%A" x
        let innerFn input =
            // ignore the input and return x
            Success (x,input)
        // return the inner function
        {parseFn=innerFn; label=label}

    /// apply a function to the value inside a parser
    let mapP f =
        bindP (f >> returnP)

    /// infix version of mapP
    let ( <!> ) = mapP

    /// "piping" version of mapP
    let ( |>> ) x f = mapP f x

    /// apply a wrapped function to a wrapped value
    let applyP fP xP =
        fP >>= (fun f ->
        xP >>= (fun x ->
            returnP (f x) ))

    /// infix version of apply
    let ( <*> ) = applyP

    /// lift a two parameter function to Parser World
    let lift2 f xP yP =
        returnP f <*> xP <*> yP

    /// Combine two parsers as "A andThen B"
    let andThen p1 p2 =
        let label = sprintf "%s andThen %s" (getLabel p1) (getLabel p2)
        p1 >>= (fun p1Result ->
        p2 >>= (fun p2Result ->
            returnP (p1Result,p2Result) ))
        <?> label

    /// Infix version of andThen
    let ( .>>. ) = andThen

    /// Combine two parsers as "A orElse B"
    let orElse p1 p2 =
        let label = sprintf "%s orElse %s" (getLabel p1) (getLabel p2)
        let innerFn input =
            // run parser1 with the input
            let result1 = runOnInput p1 input

            // test the result for Failure/Success
            match result1 with
            | Success result ->
                // if success, return the original result
                result1

            | Failure _ ->
                // if failed, run parser2 with the input
                let result2 = runOnInput p2 input

                // return parser2's result
                result2

        // return the inner function
        {parseFn=innerFn; label=label}

    /// Infix version of orElse
    let ( <|> ) = orElse

    /// Choose any of a list of parsers
    let choice listOfParsers =
        List.reduce ( <|> ) listOfParsers

    let rec sequence parserList =
        // define the "cons" function, which is a two parameter function
        let cons head tail = head::tail

        // lift it to Parser World
        let consP = lift2 cons

        // process the list of parsers recursively
        match parserList with
        | [] ->
            returnP []
        | head::tail ->
            consP head (sequence tail)

    /// (helper) match zero or more occurences of the specified parser
    let rec parseZeroOrMore parser input =
        // run parser with the input
        let firstResult = runOnInput parser input
        // test the result for Failure/Success
        match firstResult with
        | Failure (_,_,_) ->
            // if parse fails, return empty list
            ([],input)
        | Success (firstValue,inputAfterFirstParse) ->
            // if parse succeeds, call recursively
            // to get the subsequent values
            let (subsequentValues,remainingInput) =
                parseZeroOrMore parser inputAfterFirstParse
            let values = firstValue::subsequentValues
            (values,remainingInput)

    /// matches zero or more occurences of the specified parser
    let many parser =
        let label = sprintf "many %s" (getLabel parser)
        let rec innerFn input =
            // parse the input -- wrap in Success as it always succeeds
            Success (parseZeroOrMore parser input)
        {parseFn=innerFn; label=label}

    /// matches one or more occurences of the specified parser
    let many1 p =
        let label = sprintf "many1 %s" (getLabel p)

        p      >>= (fun head ->
        many p >>= (fun tail ->
            returnP (head::tail) ))
        <?> label

    /// Parses an optional occurrence of p and returns an option value.
    let opt p =
        let label = sprintf "opt %s" (getLabel p)
        let some = p |>> Some
        let none = returnP None
        (some <|> none) <?> label

    /// Keep only the result of the left side parser
    let (.>>) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the first value
        |> mapP (fun (a,b) -> a)

    /// Keep only the result of the right side parser
    let (>>.) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the second value
        |> mapP (fun (a,b) -> b)

    /// Keep only the result of the middle parser
    let between p1 p2 p3 =
        p1 >>. p2 .>> p3

    /// Parses one or more occurrences of p separated by sep
    let sepBy1 p sep =
        let sepThenP = sep >>. p
        p .>>. many sepThenP
        |>> fun (p,pList) -> p::pList

    /// Parses zero or more occurrences of p separated by sep
    let sepBy p sep =
        sepBy1 p sep <|> returnP []


open CombinatorsWithPosition

//===========================================
// 4. Adding some standard parsers to the library
//===========================================

module StandardParsers =

    //>standardParsers
    /// parse a char
    let pchar charToMatch =
        // label is just the character
        let label = sprintf "%c" charToMatch

        let predicate ch = (ch = charToMatch)
        satisfy predicate label

    /// Choose any of a list of characters
    let anyOf listOfChars =
        let label = sprintf "anyOf %A" listOfChars
        listOfChars
        |> List.map pchar // convert into parsers
        |> choice
        <?> label

    /// Convert a list of chars to a string
    let charListToStr charList =
        String(List.toArray charList)

    /// Parses a sequence of zero or more chars with the char parser cp.
    /// It returns the parsed chars as a string.
    let manyChars cp =
        many cp
        |>> charListToStr

    /// Parses a sequence of one or more chars with the char parser cp.
    /// It returns the parsed chars as a string.
    let manyChars1 cp =
        many1 cp
        |>> charListToStr

    /// parse a specific string
    let pstring str =
        // label is just the string
        let label = str

        str
        // convert to list of char
        |> List.ofSeq
        // map each char to a pchar
        |> List.map pchar
        // convert to Parser<char list>
        |> sequence
        // convert Parser<char list> to Parser<string>
        |> mapP charListToStr
        <?> label
    //<

open StandardParsers

// Let's test `pstring`, for example:
module PString_Test =

    //>pstring_test
    run (pstring "AB") "ABC"
    |> printResult
    // "AB"

    run (pstring "AB") "A|C"
    |> printResult
    // Line:0 Col:1 Error parsing AB
    // A|C
    //  ^Unexpected '|'
    //<


//===========================================
// Whitespace parsers
//===========================================

module WhitespaceParsers =

    //>WhitespaceParsers
    /// parse a whitespace char
    let whitespaceChar =
        let predicate = Char.IsWhiteSpace
        let label = "whitespace"
        satisfy predicate label

    /// parse zero or more whitespace char
    let spaces = many whitespaceChar

    /// parse one or more whitespace char
    let spaces1 = many1 whitespaceChar
    //<

open WhitespaceParsers

module WhitespaceParsers_Test =

    //>WhitespaceParsers_test
    run spaces " ABC"
    |> printResult
    // [' ']

    run spaces "A"
    |> printResult
    // []

    run spaces1 " ABC"
    |> printResult
    // [' ']

    run spaces1 "A"
    |> printResult
    // Line:0 Col:0 Error parsing many1 whitespace
    // A
    // ^Unexpected 'A'
    //<


//===========================================
// Numeric parsers
//===========================================

module NumericParsers =

    //>NumericParsers
    /// parse a digit
    let digitChar =
        let predicate = Char.IsDigit
        let label = "digit"
        satisfy predicate label

    // parse an integer
    let pint =
        let label = "integer"

        // helper
        let resultToInt (sign,digits) =
            let i = digits |> int  // ignore int overflow for now
            match sign with
            | Some ch -> -i  // negate the int
            | None -> i

        // define parser for one or more digits
        let digits = manyChars1 digitChar

        // an "int" is optional sign + one or more digits
        opt (pchar '-') .>>. digits
        |> mapP resultToInt
        <?> label

    // parse a float
    let pfloat =
        let label = "float"

        // helper
        let resultToFloat (((sign,digits1),point),digits2) =
            let fl = sprintf "%s.%s" digits1 digits2 |> float
            match sign with
            | Some ch -> -fl  // negate the float
            | None -> fl

        // define parser for one or more digits
        let digits = manyChars1 digitChar

        // a float is sign, digits, point, digits (ignore exponents for now)
        opt (pchar '-') .>>. digits .>>. pchar '.' .>>. digits
        |> mapP resultToFloat
        <?> label
    //<

open NumericParsers

module NumericParsers_Test =

    //>NumericParsers_test
    run pint "-123Z"
    |> printResult
    // -123

    run pint "-Z123"
    |> printResult
    // Line:0 Col:1 Error parsing integer
    // -Z123
    //  ^Unexpected 'Z'

    run pfloat "-123.45Z"
    |> printResult
    // -123.45

    run pfloat "-123Z45"
    |> printResult
    // Line:0 Col:4 Error parsing float
    // -123Z45
    //     ^Unexpected 'Z'
    //<



//===========================================
// The complete parser library
//===========================================

(*
Code is available at ParserLibrary.fsx
*)

