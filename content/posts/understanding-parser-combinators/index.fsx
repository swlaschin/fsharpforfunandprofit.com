

open System

// ===============================================
// Implementation 1. Parsing a hard-coded character
// ===============================================

module ParseA =

    //>aparser
    let parseA str =
        if String.IsNullOrEmpty(str) then
            (false,"")
        else if str.[0] = 'A' then
            let remaining = str.[1..]
            (true,remaining)
        else
            (false,str)
    //<

    (*
    //>aparser_sig
    val parseA :
      string -> (bool * string)
    //<
    *)

open ParseA

/// Test the "parseA" function
module ParseA_Test =

    //>aparser_test
    let inputABC = "ABC"
    parseA inputABC
    //<
    |> printfn "%A"

    (*
    //>aparser_test_out
    (true, "BC")
    //<
    *)

    //>aparser_test_bad
    let inputZBC = "ZBC"
    parseA inputZBC
    //<
    |> printfn "%A"

    (*
    //>aparser_test_bad_out
    (false, "ZBC")
    //<
    *)

// ===============================================
// Parsing a specified character (pchar, version #1)
// ===============================================

module PChar_v1 =

    //>pchar_v1
    let pchar (charToMatch,str) =
        if String.IsNullOrEmpty(str) then
            let msg = "No more input"
            (msg,"")
        else
            let first = str.[0]
            if first = charToMatch then
                let remaining = str.[1..]
                let msg = sprintf "Found %c" charToMatch
                (msg,remaining)
            else
                let msg = sprintf "Expecting '%c'. Got '%c'" charToMatch first
                (msg,str)
    //<

    (*
    //>pchar_v1_sig
    val pchar :
      (char * string) -> (string * string)
    //<
    *)

open PChar_v1

/// Test the "pchar" function
module PChar_v1_Test =

    //>pchar_v1_test1
    let inputABC = "ABC"
    pchar('A',inputABC)
    //<
    |> printfn "%A"

    (*
    //>pchar_v1_test1_out
    ("Found A", "BC")
    //<
    *)

    //>pchar_v1_test2
    let inputZBC = "ZBC"
    pchar('A',inputZBC)
    //<
    |> printfn "%A"

    (*
    //>pchar_v1_test2_out
    ("Expecting 'A'. Got 'Z'", "ZBC")
    //<
    *)

    //>pchar_v1_test3
    pchar('Z',inputZBC)  // ("Found Z", "BC")
    //<
    |> printfn "%A"

// ===============================================
// Returning a Success/Failure (pchar, version #2)
// ===============================================

//>ParseResult
type ParseResult<'a> =
    | Success of 'a
    | Failure of string
//<

module PChar_v2 =

    //>pchar_v2
    let pchar (charToMatch,str) =
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
    //<

    (*
    //>pchar_v2_sig
    val pchar :
        (char * string) -> ParseResult<char * string>
    //<
    *)

open PChar_v2

/// Test the "pchar" function
module PChar_v2_Test =

    //>pchar_v2_test1
    let inputABC = "ABC"
    pchar('A',inputABC)
    //<
    |> printfn "%A"

    (*
    //>pchar_v2_test1_out
    Success ('A', "BC")
    //<
    *)

    //>pchar_v2_test2
    let inputZBC = "ZBC"
    pchar('A',inputZBC)
    //<
    |> printfn "%A"

    (*
    //>pchar_v2_test2_out
    Failure "Expecting 'A'. Got 'Z'"
    //<
    *)

// ===============================================
// Switching to a curried implementation (pchar, version #3)
// ===============================================


module PChar_v3 =

    //>pchar_v3
    let pchar charToMatch str =
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
    //<

// ===============================================
// What is currying?
// ===============================================

module CurryingExamples =

    /// 2-parameter function, automatically curried
    module Add_v1 =

        //>add_v1
        let add x y =
            x + y
        //<

        (*
        //>add_v1_sig
        val add : x:int -> y:int -> int
        //<
        *)

    /// 1-parameter function, that returns a 1-parameter lambda
    module Add_v2 =

        //>add_v2
        let add x =
            fun y -> x + y  // return a lambda
        //<


    /// 1-parameter function, that returns a 1-parameter inner function
    module Add_v3 =

        //>add_v3
        let add x =
            let innerFn y = x + y
            innerFn // return innerFn
        //<


        (*
        //>add_v3_sig
        val add : x:int -> (int -> int)
        //<
        *)

// ===============================================
// Rewriting with an inner function (pchar, version #4)
// ===============================================


module PChar_v4 =

    //>pchar_v4
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
        // return the inner function
        innerFn
    //<


    (*
    //>pchar_v4_sig
    val pchar :
        charToMatch:char -> (string -> ParseResult<char * string>)
    //<
    *)

open PChar_v4

//>parseA_curried
let parseA = pchar 'A'
//<

(*
//>parseA_curried_sig
val parseA : string -> ParseResult<char * string>
//<
*)

/// Test the "pchar" function
module PChar_v4_Test =

    //>parseA_curried2
    let inputABC = "ABC"
    parseA inputABC  //=> Success ('A', "BC")

    let inputZBC = "ZBC"
    parseA inputZBC  //=> Failure "Expecting 'A'. Got 'Z'"
    //<

//============================================
// Encapsulating the parsing function in a type (pchar, version #5)
//============================================

//>Parser
type Parser<'T> = Parser of (string -> ParseResult<'T * string>)
//<

module PChar_v5 =

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
        // return a "Parser"
        Parser innerFn


open PChar_v5

/// test the "pchar" function
module PChar_v5_Test =
    ()

    // uncomment to see compiler error
    (*
    //>parseA_error
    let parseA = pchar 'A'
    let inputABC = "ABC"
    parseA inputABC  // compiler error
    //<
    *)

    (*
    //>parseA_error_text
    error FS0003: This value is not a function and cannot be applied.
    //<
    *)


//>run
let run parser input =
    // unwrap parser to get inner function
    let (Parser innerFn) = parser
    // call inner function with input
    innerFn input
//<

/// test the "run" function
module Run_Test =

    let parseA = pchar 'A'

    //>parseA_success
    let inputABC = "ABC"
    run parseA inputABC  // Success ('A', "BC")

    let inputZBC = "ZBC"
    run parseA inputZBC  // Failure "Expecting 'A'. Got 'Z'"
    //<


//============================================
// AndThen combinator
//============================================

// uncomment for error
(*
do
    //>parseAThenB_incorrect
    let parseA = pchar 'A'
    let parseB = pchar 'B'

    let parseAThenB = parseA >> parseB
    //<
*)


//>andThen
let andThen parser1 parser2 =
    let innerFn input =
        // run parser1 with the input
        let result1 = run parser1 input

        // test the result for Failure/Success
        match result1 with
        | Failure err ->
            // return error from parser1
            Failure err

        | Success (value1,remaining1) ->
            // run parser2 with the remaining input
            let result2 =  run parser2 remaining1

            // test the result for Failure/Success
            match result2 with
            | Failure err ->
                // return error from parser2
                Failure err

            | Success (value2,remaining2) ->
                // combine both values as a pair
                let newValue = (value1,value2)
                // return remaining input after parser2
                Success (newValue,remaining2)

    // return the inner function
    Parser innerFn
//<

(*
//>andThen_sig
val andThen :
     parser1:Parser<'a> -> parser2:Parser<'b> -> Parser<'a * 'b>
//<
*)

// We'll also define an infix version of `andThen` so that we can use it like regular `>>` composition:

//>andThenOp
let ( .>>. ) = andThen
//<

/// test the "andThen" function
module AndThen_Test =

    //>andThen_test1
    let parseA = pchar 'A'
    let parseB = pchar 'B'
    let parseAThenB = parseA .>>. parseB
    //<

    (*
    //>andThen_test1_sig
    val parseA : Parser<char>
    val parseB : Parser<char>
    val parseAThenB : Parser<char * char>
    //<
    *)

    //>andThen_test1_run
    run parseAThenB "ABC"  // Success (('A', 'B'), "C")
    run parseAThenB "ZBC"  // Failure "Expecting 'A'. Got 'Z'"
    run parseAThenB "AZC"  // Failure "Expecting 'B'. Got 'Z'"
    //<

//===========================================
// Choosing between two parsers: the "or else" combinator
//===========================================

//>orElse
let orElse parser1 parser2 =
    let innerFn input =
        // run parser1 with the input
        let result1 = run parser1 input

        // test the result for Failure/Success
        match result1 with
        | Success result ->
            // if success, return the original result
            result1

        | Failure err ->
            // if failed, run parser2 with the input
            let result2 = run parser2 input

            // return parser2's result
            result2

    // return the inner function
    Parser innerFn
//<

(*
//>orElse_sig
val orElse :
    parser1:Parser<'a> -> parser2:Parser<'a> -> Parser<'a>
//<
*)

// We'll also define an infix version

//>orElseOp
let ( <|> ) = orElse
//<

/// test the "orElse" function
module OrElse_Test =

    //>orElse_test
    let parseA = pchar 'A'
    let parseB = pchar 'B'
    let parseAOrElseB = parseA <|> parseB
    //<

    (*
    //>orElse_test_sig
    val parseA : Parser<char>
    val parseB : Parser<char>
    val parseAOrElseB : Parser<char>
    //<
    *)

    //>orElse_test_run
    run parseAOrElseB "AZZ"  // Success ('A', "ZZ")
    run parseAOrElseB "BZZ"  // Success ('B', "ZZ")
    run parseAOrElseB "CZZ"  // Failure "Expecting 'B'. Got 'C'"
    //<


//===========================================
// Combining `andThen` and `orElse`
//===========================================

/// test the "andThen" and "orElse" functions
module AndThenOrElse_Test =

    //>andThenOrElse_test
    let parseA = pchar 'A'
    let parseB = pchar 'B'
    let parseC = pchar 'C'
    let bOrElseC = parseB <|> parseC
    let aAndThenBorC = parseA .>>. bOrElseC
    //<

    //>andThenOrElse_run
    run aAndThenBorC "ABZ"  // Success (('A', 'B'), "Z")
    run aAndThenBorC "ACZ"  // Success (('A', 'C'), "Z")
    run aAndThenBorC "QBZ"  // Failure "Expecting 'A'. Got 'Q'"
    run aAndThenBorC "AQZ"  // Failure "Expecting 'C'. Got 'Q'"
    //<

//===========================================
// Choosing from a list of parsers: "choice" and "anyOf"
//===========================================

//>choice
/// Choose any of a list of parsers
let choice listOfParsers =
    List.reduce ( <|> ) listOfParsers
//<

(*
//>choice_sig
val choice :
    Parser<'a> list -> Parser<'a>
//<
*)

/// test the "choice" function
module Choice_Test =
    let digitChars = ['0'..'9']

    // map each char to a Parser using pchar
    let digitParsers = List.map pchar digitChars
        //=> Parser<char> list

    // combine all parsers using choice
    let parseDigit = choice digitParsers
        //=> Parser<char>

    run parseDigit  "1ZZ"  // Success ('1', "ZZ")
    run parseDigit  "2ZZ"  // Success ('2', "ZZ")
    run parseDigit  "9ZZ"  // Success ('9', "ZZ")
    run parseDigit  "AZZ"  // Failure "Expecting '9'. Got 'A'"


//>anyOf
/// Choose any of a list of characters
let anyOf listOfChars =
    listOfChars
    |> List.map pchar // convert into parsers
    |> choice         // combine them
//<

/// test the "anyOf" function
module AnyOf_Test =

    //>parseLowercase
    let parseLowercase =
        anyOf ['a'..'z']

    let parseDigit =
        anyOf ['0'..'9']
    //<

    //>parseLowercase_run
    run parseLowercase "aBC"  // Success ('a', "BC")
    run parseLowercase "ABC"  // Failure "Expecting 'z'. Got 'A'"

    run parseDigit "1ABC"  // Success ("1", "ABC")
    run parseDigit "9ABC"  // Success ("9", "ABC")
    run parseDigit "|ABC"  // Failure "Expecting '9'. Got '|'"
    //<

//===========================================
// The complete parser library so far
//===========================================

module ParserLibrary_Complete =

    //>ParserLibrary
    open System

    /// Type that represents Success/Failure in parsing
    type ParseResult<'a> =
        | Success of 'a
        | Failure of string

    /// Type that wraps a parsing function
    type Parser<'T> = Parser of (string -> ParseResult<'T * string>)

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

    /// Combine two parsers as "A andThen B"
    let andThen parser1 parser2 =
        let innerFn input =
            // run parser1 with the input
            let result1 = run parser1 input

            // test the result for Failure/Success
            match result1 with
            | Failure err ->
                // return error from parser1
                Failure err

            | Success (value1,remaining1) ->
                // run parser2 with the remaining input
                let result2 =  run parser2 remaining1

                // test the result for Failure/Success
                match result2 with
                | Failure err ->
                    // return error from parser2
                    Failure err

                | Success (value2,remaining2) ->
                    // combine both values as a pair
                    let newValue = (value1,value2)
                    // return remaining input after parser2
                    Success (newValue,remaining2)

        // return the inner function
        Parser innerFn

    /// Infix version of andThen
    let ( .>>. ) = andThen

    /// Combine two parsers as "A orElse B"
    let orElse parser1 parser2 =
        let innerFn input =
            // run parser1 with the input
            let result1 = run parser1 input

            // test the result for Failure/Success
            match result1 with
            | Success result ->
                // if success, return the original result
                result1

            | Failure err ->
                // if failed, run parser2 with the input
                let result2 = run parser2 input

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
    //<
