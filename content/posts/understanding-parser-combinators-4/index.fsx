

//>Load
#load "ParserLibrary.fsx"

open System
open ParserLibrary
//<


//==============================================
// 1. Building a model to represent the JSON spec
//==============================================

//>JValue
type JValue =
    | JString of string
    | JNumber of float
    | JBool   of bool
    | JNull
    | JObject of Map<string, JValue>
    | JArray  of JValue list
//<

//==============================================
// 2. Getting started with `Null` and `Bool`
//==============================================

module JNull_v1 =

    //>jNull
    let jNull =
        pstring "null"
        |>> (fun _ -> JNull)  // map to JNull
        <?> "null"            // give it a label
    //<


//>ignore
// applies the parser p, ignores the result,
// and returns x.
let (>>%) p x =
    p |>> (fun _ -> x)
//<

// Now we can rewrite `jNull` as follows:

//>jNull_v2
let jNull =
    pstring "null"
    >>% JNull   // using new utility combinator
    <?> "null"
//<

module JNull_Test =

    //>jNull_test
    run jNull "null"
    // Success: JNull

    run jNull "nulp" |> printResult
    // Line:0 Col:3 Error parsing null
    // nulp
    //    ^Unexpected 'p'
    //<


//>jBool
let jBool =
    let jtrue =
        pstring "true"
        >>% JBool true   // map to JBool
    let jfalse =
        pstring "false"
        >>% JBool false  // map to JBool

    // choose between true and false
    jtrue <|> jfalse
    <?> "bool"           // give it a label
//<

module JBool_Test =

    //>jBool_test
    run jBool "true"
    // Success: JBool true

    run jBool "false"
    // Success: JBool false

    run jBool "truX" |> printResult
    // Line:0 Col:0 Error parsing bool
    // truX
    // ^Unexpected 't'
    //<


//==============================================
// 3. Parsing "String"
//==============================================


//>jUnescapedChar
let jUnescapedChar =
    let label = "char"
    satisfy (fun ch -> ch <> '\\' && ch <> '\"') label
//<

module UnescapedChar_Test =

    //>jUnescapedChar_test
    run jUnescapedChar "a"
    // Success 'a'

    run jUnescapedChar "\\" |> printResult
    // Line:0 Col:0 Error parsing char
    // \
    // ^Unexpected '\'
    //<


//>jEscapedChar
/// Parse an escaped char
let jEscapedChar =
    [
    // (stringToMatch, resultChar)
    ("\\\"",'\"')      // quote
    ("\\\\",'\\')      // reverse solidus
    ("\\/",'/')        // solidus
    ("\\b",'\b')       // backspace
    ("\\f",'\f')       // formfeed
    ("\\n",'\n')       // newline
    ("\\r",'\r')       // cr
    ("\\t",'\t')       // tab
    ]
    // convert each pair into a parser
    |> List.map (fun (toMatch,result) ->
        pstring toMatch >>% result)
    // and combine them into one
    |> choice
    <?> "escaped char" // set label
//<

module EscapedChar_Test =

    //>jEscapedChar_test
    run jEscapedChar "\\\\" // Success '\\'
    run jEscapedChar "\\t"  // Success '\009'

    // or using @-strings to escape the input
    run jEscapedChar @"\\"  // Success '\\'
    run jEscapedChar @"\n"  // Success '\010'

    run jEscapedChar "a" |> printResult
    // Line:0 Col:0 Error parsing escaped char
    // a
    // ^Unexpected 'a'
    //<



//>jUnicodeChar
/// Parse a unicode char
let jUnicodeChar =

    // set up the "primitive" parsers
    let backslash = pchar '\\'
    let uChar = pchar 'u'
    let hexdigit =
        anyOf (['0'..'9'] @ ['A'..'F'] @ ['a'..'f'])
    let fourHexDigits =
        hexdigit .>>. hexdigit .>>. hexdigit .>>. hexdigit

    // convert the parser output (nested tuples)
    // to a char
    let convertToChar (((h1,h2),h3),h4) =
        let str = sprintf "%c%c%c%c" h1 h2 h3 h4
        Int32.Parse(str,Globalization.NumberStyles.HexNumber) |> char

    // set up the main parser
    backslash >>. uChar >>. fourHexDigits
    |>> convertToChar
//<

module JUnicodeChar_Test =

    // let's test with a smiley face -- `\u263A`.

    //>jUnicodeChar_test
    run jUnicodeChar "\\u263A"  //  Success ('☺')
    //<


//----------------------------------
// The complete `String` parser
// Putting it all together now


//>quotedString
let quotedString =
    let quote = pchar '\"' <?> "quote"
    let jchar = jUnescapedChar <|> jEscapedChar <|> jUnicodeChar

    // set up the main parser
    quote >>. manyChars jchar .>> quote
//<

//>jString
/// Parse a JString
let jString =
    // wrap the string in a JString
    quotedString
    |>> JString           // convert to JString
    <?> "quoted string"   // add label
//<

module JString_Test =

    //>jString_test
    run jString "\"\""
        // Success (JString "")
    run jString "\"a\""
        // Success (JString "a")
    run jString "\"ab\""
        // Success (JString "ab")
    run jString "\"ab\\tde\""
        // Success (JString "ab\tde")
    run jString "\"ab\\u263Ade\""
        // Success (JString "ab☺de")
    //<


//==============================================
// 4. Parsing `Number`
//==============================================

module NumberComponents =


    //>digit
    let optSign = opt (pchar '-')

    let zero = pstring "0"

    let digitOneNine =
        satisfy (fun ch -> Char.IsDigit ch && ch <> '0') "1-9"

    let digit =
        satisfy (fun ch -> Char.IsDigit ch ) "digit"

    let point = pchar '.'

    let e = pchar 'e' <|> pchar 'E'

    let optPlusMinus = opt (pchar '-' <|> pchar '+')
    //<

    //>nonZeroInt
    let nonZeroInt =
        digitOneNine .>>. manyChars digit
        |>> fun (first,rest) -> string first + rest

    let intPart = zero <|> nonZeroInt
    //<

    //>fractionPart
    let fractionPart = point >>. manyChars1 digit
    //<

    //>exponentPart
    let exponentPart = e >>. optPlusMinus .>>. manyChars1 digit
    //<

    //>optToString
    // utility function to convert an optional value 
    // to a string, or "" if missing
    let ( |>? ) opt f =
        match opt with
        | None -> ""
        | Some x -> f x
    //<

    //>convertToJNumber
    let convertToJNumber (((optSign,intPart),fractionPart),expPart) =
        // convert to strings and let .NET parse them!
        // -- crude but ok for now.

        let signStr =
            optSign
            |>? string   // e.g. "-"

        let fractionPartStr =
            fractionPart
            |>? (fun digits -> "." + digits )  // e.g. ".456"

        let expPartStr =
            expPart
            |>? fun (optSign, digits) ->
                let sign = optSign |>? string
                "e" + sign + digits          // e.g. "e-12"

        // add the parts together and convert to a float,
        // then wrap in a JNumber
        (signStr + intPart + fractionPartStr + expPartStr)
        |> float
        |> JNumber
    //<

    // the number parsing pipeline before encapsulation
    //>number_pipeline
    optSign .>>. intPart .>>. opt fractionPart .>>. opt exponentPart
    |>> convertToJNumber
    <?> "number"   // add label
    //<


//>jNumber
/// Parse a JNumber
let jNumber =

    // set up the "primitive" parsers
    let optSign = opt (pchar '-')

    let zero = pstring "0"

    let digitOneNine =
        satisfy (fun ch -> Char.IsDigit ch && ch <> '0') "1-9"

    let digit =
        satisfy (fun ch -> Char.IsDigit ch ) "digit"

    let point = pchar '.'

    let e = pchar 'e' <|> pchar 'E'

    let optPlusMinus = opt (pchar '-' <|> pchar '+')

    let nonZeroInt =
        digitOneNine .>>. manyChars digit
        |>> fun (first,rest) -> string first + rest

    let intPart = zero <|> nonZeroInt

    let fractionPart = point >>. manyChars1 digit

    let exponentPart = e >>. optPlusMinus .>>. manyChars1 digit

    // utility function to convert an optional value 
    // to a string, or "" if missing
    let ( |>? ) opt f =
        match opt with
        | None -> ""
        | Some x -> f x

    let convertToJNumber (((optSign,intPart),fractionPart),expPart) =
        // convert to strings and let .NET parse them! 
        // -- crude but ok for now.

        let signStr =
            optSign
            |>? string   // e.g. "-"

        let fractionPartStr =
            fractionPart
            |>? (fun digits -> "." + digits )  // e.g. ".456"

        let expPartStr =
            expPart
            |>? fun (optSign, digits) ->
                let sign = optSign |>? string
                "e" + sign + digits          // e.g. "e-12"

        // add the parts together and convert to a float, 
        // then wrap in a JNumber
        (signStr + intPart + fractionPartStr + expPartStr)
        |> float
        |> JNumber

    // set up the main parser
    optSign .>>. intPart .>>. opt fractionPart .>>. opt exponentPart
    |>> convertToJNumber
    <?> "number"   // add label
//<


module JNumber_Test =

    //>jNumber_test1
    run jNumber "123"     // JNumber 123.0
    run jNumber "-123"    // JNumber -123.0
    run jNumber "123.4"   // JNumber 123.4
    //<

    // And what about some failing cases?

    //>jNumber_test2
    run jNumber "-123."   // JNumber -123.0 -- should fail!
    run jNumber "00.1"    // JNumber 0      -- should fail!
    //<

    // add some whitespace to the parser to force it to terminate.

    //>jNumber2
    let jNumber_ = jNumber .>> spaces1
    //<

    //>jNumber2_test1
    run jNumber_ "123"     // JNumber 123.0
    run jNumber_ "-123"    // JNumber -123.0

    run jNumber_ "-123." |> printResult
    // Line:0 Col:4 Error parsing number andThen many1 whitespace
    // -123.
    //     ^Unexpected '.'
    //<


    // Let's test the fractional part:
    //>jNumber2_test2
    run jNumber_ "123.4"   // JNumber 123.4

    run jNumber_ "00.4" |> printResult
    // Line:0 Col:1 Error parsing number andThen many1 whitespace
    // 00.4
    //  ^Unexpected '0'
    //<

    // and the exponent part now:

    //>jNumber2_test3
    // exponent only
    run jNumber_ "123e4"     // JNumber 1230000.0

    // fraction and exponent
    run jNumber_ "123.4e5"   // JNumber 12340000.0
    run jNumber_ "123.4e-5"  // JNumber 0.001234
    //<


//==============================================
// 5. Parsing "Array"
//==============================================

module JArray_Fragment1 =

    let jValue : Parser<char> = pchar ' '

    //>jArray1
    let jArray =

        let left = pchar '[' .>> spaces
        let right = pchar ']' .>> spaces
        let comma = pchar ',' .>> spaces
        let value = jValue .>> spaces
        //          ^ what is "jValue"?
    //<
        ()

module JArray_Fragment2 =

    // placeholder
    let jValue : Parser<JValue> = jNull

    let left = pchar '[' .>> spaces
    let right = pchar ']' .>> spaces
    let comma = pchar ',' .>> spaces
    let value = jValue .>> spaces

    // Will be replaced with ellipses in the post.
    // Used in the example code to let the code type check without having to provide full details.
    let dotDotDot() = ()

    //>jArray2
    let jArray =
        dotDotDot()

        // set up the list parser
        let values = sepBy value comma

        // set up the main parser
        between left values right
        |>> JArray
        <?> "array"
    //<

module JArray_Fragment3 =

    // placeholder
    let jValue : Parser<JValue> = jNull

    // Will be replaced with ellipses in the post.
    // Used in the example code to let the code type check without having to provide full details.
    let dotDotDot() = ()

    //>jArray3
    let jArray =
        dotDotDot()
        let value = jValue .>> spaces
        //          ^ what is "jValue"?
        dotDotDot()
    //<
        ()

// adding a forward ref
//>createParserForwardedToRef
let createParserForwardedToRef<'a>() =

    let dummyParser : Parser<'a>=
        let innerFn _ =
            failwith "unfixed forwarded parser"
        {parseFn=innerFn; label="unknown"}

    // mutable pointer to placeholder Parser
    let parserRef = ref dummyParser

    // wrapper Parser
    let innerFn input =
        // forward input to the placeholder
        // (Note: "!" is the deferencing operator)
        runOnInput !parserRef input
    let wrapperParser = {parseFn=innerFn; label="unknown"}

    wrapperParser, parserRef
//<


//>jValue_jValueRef
let jValue,jValueRef = createParserForwardedToRef<JValue>()
//<

// Finishing up the `Array` parser

//>jArray
let jArray =

    // set up the "primitive" parsers
    let left = pchar '[' .>> spaces
    let right = pchar ']' .>> spaces
    let comma = pchar ',' .>> spaces
    let value = jValue .>> spaces

    // set up the list parser
    let values = sepBy value comma

    // set up the main parser
    between left values right
    |>> JArray
    <?> "array"
//<


module JArray_Test =
    // If we try to test it now, we get an exception because we haven't fixed up the reference:

    //>jArray_test_error
    run jArray "[ 1, 2 ]"

    // System.Exception: unfixed forwarded parser
    //<

    // for now, let's fix up the reference

    //>jArray_test_fix
    jValueRef := jNumber
    //<

    // Now we *can* successfully test the `jArray` function
    // as long as we are careful to only use numbers in our array!

    //>jArray_test_ok
    run jArray "[ 1, 2 ]"
    // Success (JArray [JNumber 1.0; JNumber 2.0],

    run jArray "[ 1, 2, ]" |> printResult
    // Line:0 Col:6 Error parsing array
    // [ 1, 2, ]
    //       ^Unexpected ','
    //<

//==============================================
// 6. Parsing `Object`
//==============================================

//>jObject
let jObject =

    // set up the "primitive" parsers
    let left = spaces >>. pchar '{' .>> spaces
    let right = pchar '}' .>> spaces
    let colon = pchar ':' .>> spaces
    let comma = pchar ',' .>> spaces
    let key = quotedString .>> spaces
    let value = jValue .>> spaces

    // set up the list parser
    let keyValue = (key .>> colon) .>>. value
    let keyValues = sepBy keyValue comma

    // set up the main parser
    between left keyValues right
    |>> Map.ofList  // convert the list of keyValues into a Map
    |>> JObject     // wrap in JObject
    <?> "object"    // add label
//<

module JObject_Test =

    //>jObject_test
    run jObject """{ "a":1, "b"  :  2 }"""
    // JObject (map [("a", JNumber 1.0); ("b", JNumber 2.0)]),

    run jObject """{ "a":1, "b"  :  2, }""" |> printResult
    // Line:0 Col:18 Error parsing object
    // { "a":1, "b"  :  2, }
    //                   ^Unexpected ','
    //<


//==============================================
// 7. Putting it all together
//==============================================


//>jValueRef
jValueRef := choice
    [
    jNull
    jBool
    jNumber
    jString
    jArray
    jObject
    ]
//<

//==============================================
// Testing the complete parser: example 1
//==============================================

module Example1 =

    //>example1
    let example1 = """{
        "name" : "Scott",
        "isMale" : true,
        "bday" : {"year":2001, "month":12, "day":25 },
        "favouriteColors" : ["blue", "green"],
        "emptyArray" : [],
        "emptyObject" : {}
    }"""
    run jValue example1
    //<

    // And here is the result:

    (*
    //>example1_out
    JObject
        (map
            [("bday", JObject(map
                    [("day", JNumber 25.0);
                    ("month", JNumber 12.0);
                    ("year", JNumber 2001.0)]));
            ("emptyArray", JArray []);
            ("emptyObject", JObject (map []));
            ("favouriteColors", JArray [JString "blue"; JString "green"]);
            ("isMale", JBool true);
            ("name", JString "Scott");
            ])
    //<
    *)

//==============================================
// Testing the complete parser: example 2
//==============================================

module Example2 =


    //>example2
    let example2= """{"widget": {
        "debug": "on",
        "window": {
            "title": "Sample Konfabulator Widget",
            "name": "main_window",
            "width": 500,
            "height": 500
        },
        "image": {
            "src": "Images/Sun.png",
            "name": "sun1",
            "hOffset": 250,
            "vOffset": 250,
            "alignment": "center"
        },
        "text": {
            "data": "Click Here",
            "size": 36,
            "style": "bold",
            "name": "text1",
            "hOffset": 250,
            "vOffset": 100,
            "alignment": "center",
            "onMouseUp": "sun1.opacity = (sun1.opacity / 100) * 90;"
        }
    }}  """

    run jValue example2
    //<

    // And here is the result:
    (*
    //>example2_out
    JObject(map
        [("widget",JObject(map
            [("debug", JString "on");
            ("image",JObject(map
                [("alignment", JString "center");
                    ("hOffset", JNumber 250.0);
                    ("name", JString "sun1");
                    ("src", JString "Images/Sun.png");
                    ("vOffset", JNumber 250.0)]));
            ("text",JObject(map
                [("alignment", JString "center");
                    ("data", JString "Click Here");
                    ("hOffset", JNumber 250.0);
                    ("name", JString "text1");
                    ("onMouseUp", 
                      JString "sun1.opacity = (sun1.opacity/100) * 90;");
                    ("size", JNumber 36.0);
                    ("style", JString "bold");
                    ("vOffset", JNumber 100.0)]));
            ("window",JObject(map
                [("height", JNumber 500.0);
                    ("name", JString "main_window");
                    ("title", JString "Sample Konfabulator Widget");
                    ("width", JNumber 500.0)]))]))]),
    //<
    *)


//===========================================
// The complete JSON parser library
//===========================================

(*
Code is available at JsonParser.fsx
*)

module JsonParser_Complete =
    //>JsonParser_Complete
    open System
    open ParserLibrary

    type JValue =
        | JString of string
        | JNumber of float
        | JBool   of bool
        | JNull
        | JObject of Map<string, JValue>
        | JArray  of JValue list


    // ======================================
    // Forward reference
    // ======================================

    /// Create a forward reference
    let createParserForwardedToRef<'a>() =

        let dummyParser : Parser<'a> =
            let innerFn _ = failwith "unfixed forwarded parser"
            {parseFn=innerFn; label="unknown"}

        // ref to placeholder Parser
        let parserRef = ref dummyParser

        // wrapper Parser
        let innerFn input =
            // forward input to the placeholder
            // (Note: "!" is the deferencing operator)
            runOnInput !parserRef input
        let wrapperParser = {parseFn=innerFn; label="unknown"}

        wrapperParser, parserRef

    let jValue,jValueRef = createParserForwardedToRef<JValue>()

    // ======================================
    // Utility function
    // ======================================

    // applies the parser p, ignores the result, and returns x.
    let (>>%) p x =
        p |>> (fun _ -> x)

    // ======================================
    // Parsing a JNull
    // ======================================

    let jNull =
        pstring "null"
        >>% JNull   // map to JNull
        <?> "null"  // give it a label

    // ======================================
    // Parsing a JBool
    // ======================================

    let jBool =
        let jtrue =
            pstring "true"
            >>% JBool true   // map to JBool
        let jfalse =
            pstring "false"
            >>% JBool false  // map to JBool

        // choose between true and false
        jtrue <|> jfalse
        <?> "bool"           // give it a label


    // ======================================
    // Parsing a JString
    // ======================================

    /// Parse an unescaped char
    let jUnescapedChar =
        satisfy (fun ch -> ch <> '\\' && ch <> '\"') "char"

    /// Parse an escaped char
    let jEscapedChar =
        [
        // (stringToMatch, resultChar)
        ("\\\"",'\"')      // quote
        ("\\\\",'\\')      // reverse solidus
        ("\\/",'/')        // solidus
        ("\\b",'\b')       // backspace
        ("\\f",'\f')       // formfeed
        ("\\n",'\n')       // newline
        ("\\r",'\r')       // cr
        ("\\t",'\t')       // tab
        ]
        // convert each pair into a parser
        |> List.map (fun (toMatch,result) ->
            pstring toMatch >>% result)
        // and combine them into one
        |> choice
        <?> "escaped char" // set label

    /// Parse a unicode char
    let jUnicodeChar =

        // set up the "primitive" parsers
        let backslash = pchar '\\'
        let uChar = pchar 'u'
        let hexdigit = 
            anyOf (['0'..'9'] @ ['A'..'F'] @ ['a'..'f'])
        let fourHexDigits =
            hexdigit .>>. hexdigit .>>. hexdigit .>>. hexdigit

        // convert the parser output (nested tuples)
        // to a char
        let convertToChar (((h1,h2),h3),h4) =
            let str = sprintf "%c%c%c%c" h1 h2 h3 h4
            Int32.Parse(str,Globalization.NumberStyles.HexNumber) |> char

        // set up the main parser
        backslash  >>. uChar >>. fourHexDigits 
        |>> convertToChar


    /// Parse a quoted string
    let quotedString =
        let quote = pchar '\"' <?> "quote"
        let jchar = jUnescapedChar <|> jEscapedChar <|> jUnicodeChar

        // set up the main parser
        quote >>. manyChars jchar .>> quote

    /// Parse a JString
    let jString =
        // wrap the string in a JString
        quotedString
        |>> JString           // convert to JString
        <?> "quoted string"   // add label

    // ======================================
    // Parsing a JNumber
    // ======================================

    /// Parse a JNumber
    let jNumber =

        // set up the "primitive" parsers
        let optSign = opt (pchar '-')

        let zero = pstring "0"

        let digitOneNine =
            satisfy (fun ch -> Char.IsDigit ch && ch <> '0') "1-9"

        let digit =
            satisfy (fun ch -> Char.IsDigit ch ) "digit"

        let point = pchar '.'

        let e = pchar 'e' <|> pchar 'E'

        let optPlusMinus = opt (pchar '-' <|> pchar '+')

        let nonZeroInt =
            digitOneNine .>>. manyChars digit
            |>> fun (first,rest) -> string first + rest

        let intPart = zero <|> nonZeroInt

        let fractionPart = point >>. manyChars1 digit

        let exponentPart = e >>. optPlusMinus .>>. manyChars1 digit

        // utility function to convert an optional value 
        // to a string, or "" if missing
        let ( |>? ) opt f =
            match opt with
            | None -> ""
            | Some x -> f x

        let convertToJNumber (((optSign,intPart),fractionPart),expPart) =
            // convert to strings and let .NET parse them! 
            // -- crude but ok for now.

            let signStr =
                optSign
                |>? string   // e.g. "-"

            let fractionPartStr =
                fractionPart
                |>? (fun digits -> "." + digits )  // e.g. ".456"

            let expPartStr =
                expPart
                |>? fun (optSign, digits) ->
                    let sign = optSign |>? string
                    "e" + sign + digits          // e.g. "e-12"

            // add the parts together and convert to a float, 
            // then wrap in a JNumber
            (signStr + intPart + fractionPartStr + expPartStr)
            |> float
            |> JNumber

        // set up the main parser
        optSign .>>. intPart .>>. opt fractionPart .>>. opt exponentPart
        |>> convertToJNumber
        <?> "number"   // add label

    // ======================================
    // Parsing a JArray
    // ======================================

    let jArray =

        // set up the "primitive" parsers
        let left = pchar '[' .>> spaces
        let right = pchar ']' .>> spaces
        let comma = pchar ',' .>> spaces
        let value = jValue .>> spaces

        // set up the list parser
        let values = sepBy value comma

        // set up the main parser
        between left values right
        |>> JArray
        <?> "array"

    // ======================================
    // Parsing a JObject
    // ======================================


    let jObject =

        // set up the "primitive" parsers
        let left = spaces >>. pchar '{' .>> spaces
        let right = pchar '}' .>> spaces
        let colon = pchar ':' .>> spaces
        let comma = pchar ',' .>> spaces
        let key = quotedString .>> spaces
        let value = jValue .>> spaces

        // set up the list parser
        let keyValue = (key .>> colon) .>>. value
        let keyValues = sepBy keyValue comma

        // set up the main parser
        between left keyValues right
        |>> Map.ofList  // convert the list of keyValues into a Map
        |>> JObject     // wrap in JObject
        <?> "object"    // add label

    // ======================================
    // Fixing up the jValue ref
    // ======================================

    // fixup the forward ref
    jValueRef := choice
        [
        jNull
        jBool
        jNumber
        jString
        jArray
        jObject
        ]
    //<