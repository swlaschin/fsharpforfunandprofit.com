(*
JsonParser.fsx

A JSON parser built from scratch using a combinator library.

Related blog post: https://fsharpforfunandprofit.com/posts/understanding-parser-combinators-4/

*)

#load "ParserLibrary.fsx"

open System
open ParserLibrary


(*
// --------------------------------
JSON spec from http://www.json.org/
// --------------------------------

The JSON spec is available at [json.org](http://www.json.org/). I'll paraphase it here:

* A `value` can be a `string` or a `number` or a `bool` or `null` or an `object` or an `array`.
  * These structures can be nested.
* A `string` is a sequence of zero or more Unicode characters, wrapped in double quotes, using backslash escapes.
* A `number` is very much like a C or Java number, except that the octal and hexadecimal formats are not used.
* A `boolean` is the literal `true` or `false`
* A `null` is the literal `null`
* An `object` is an unordered set of name/value pairs.
  * An object begins with { (left brace) and ends with } (right brace).
  * Each name is followed by : (colon) and the name/value pairs are separated by , (comma).
* An `array` is an ordered collection of values.
  * An array begins with [ (left bracket) and ends with ] (right bracket).
  * Values are separated by , (comma).
* Whitespace can be inserted between any pair of tokens.

*)


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

    // utility function to convert an optional value to a string, or "" if missing
    let ( |>? ) opt f =
        match opt with
        | None -> ""
        | Some x -> f x

    let convertToJNumber (((optSign,intPart),fractionPart),expPart) =
        // convert to strings and let .NET parse them! - crude but ok for now.

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

        // add the parts together and convert to a float, then wrap in a JNumber
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
