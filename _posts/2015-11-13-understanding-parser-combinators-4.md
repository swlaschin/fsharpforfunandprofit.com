---
layout: post
title: "Writing a JSON parser from scratch"
description: "In 250 lines of code"
categories: [Combinators,Patterns]
seriesId: "Understanding Parser Combinators"
seriesOrder: 4
---

*UPDATE: [Slides and video from my talk on this topic](/parser/)*

In this series, we are looking at how applicative parsers and parser combinators work.

* In the [first post](/posts/understanding-parser-combinators/), we created the foundations of a parsing library.
* In the [second post](/posts/understanding-parser-combinators-2/), we built out the library with many other useful combinators.
* In the [third post](/posts/understanding-parser-combinators-3/), we improved the error messages.
* In this last post, we'll use the library we've written to build a JSON parser.

<hr>

First, before we do anything else, we need to load the parser library script that we developed over the last few posts, and then open the `ParserLibrary` namespace:

```fsharp
#load "ParserLibrary.fsx"

open System
open ParserLibrary
```

You can download `ParserLibrary.fsx` [from here](https://gist.github.com/swlaschin/485f418fede6b6a36d89#file-parserlibrary-fsx).

## 1. Building a model to represent the JSON spec

The JSON spec is available at [json.org](http://www.json.org/). I'll paraphase it here:

* A `value` can be a `string` or a `number` or a `bool` or `null` or an `object` or an `array`. 
  * These structures can be nested.
* A `string` is a sequence of zero or more Unicode characters, wrapped in double quotes, using backslash escapes. 
* A `number` is very much like a C or Java number, except that the octal and hexadecimal formats are not used.
* A `boolean` is the literal `true` or `false`
* A `null` is the literal `null`
* An `object` is an unordered set of name/value pairs. 
  * An object begins with `{` (left brace) and ends with `}` (right brace). 
  * Each name is followed by `:` (colon) and the name/value pairs are separated by `,` (comma).
* An `array` is an ordered collection of values. 
  * An array begins with `[` (left bracket) and ends with `]` (right bracket). 
  * Values are separated by `,` (comma).
* Whitespace can be inserted between any pair of tokens. 
  
In F#, this definition can be modelled naturally as:

```fsharp
type JValue = 
    | JString of string
    | JNumber of float
    | JBool   of bool
    | JNull
    | JObject of Map<string, JValue>
    | JArray  of JValue list
```

So the goal of our JSON parser is:

* Given a string, we want to output a `JValue` value.

## 2. Getting started with `Null` and `Bool`

Let's start with the simplest tasks -- parsing the literal values for null and the booleans.

### Parsing Null

Parsing the `null` literal is trivial. The logic will be:

* Match the string "null".
* Map the result to the `JNull` case.

Here's the code:

```fsharp
let jNull = 
    pstring "null" 
    |>> (fun _ -> JNull)  // map to JNull
    <?> "null"            // give it a label
```

Note that we don't actually care about the value returned by the parser because we know in advance that it is going to be "null"!

This is a common situation, so let's write a little utility function, `>>%` to make this look nicer:

```fsharp
// applies the parser p, ignores the result, and returns x.
let (>>%) p x =
    p |>> (fun _ -> x)
```

Now we can rewrite `jNull` as follows:

```fsharp
let jNull = 
    pstring "null" 
    >>% JNull   // using new utility combinator
    <?> "null"  
```

Let's test:

```fsharp
run jNull "null"   
// Success: JNull

run jNull "nulp" |> printResult  
// Line:0 Col:3 Error parsing null
// nulp
//    ^Unexpected 'p'
```

That looks good. Let's try another one!

### Parsing Bool

The bool parser will be similar to null:

* Create a parser to match "true".
* Create a parser to match "false".
* And then choose between them using `<|>`.

Here's the code:

```fsharp
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
```

And here are some tests:

```fsharp
run jBool "true"   
// Success: JBool true

run jBool "false"
// Success: JBool false

run jBool "truX" |> printResult  
// Line:0 Col:0 Error parsing bool
// truX
// ^Unexpected 't'
```

Note that the error is misleading due to the backtracking issue discussed in the previous post. Since "true" failed,
it is trying to parse "false" now, and "t" is an unexpected character.

## 3. Parsing `String`

Now for something more complicated -- strings.

The spec for string parsing is available as a "railway diagram" like this:

![](/assets/img/json_string.gif)

*All diagrams sourced from [json.org](http://www.json.org).*

To build a parser from a diagram like this, we work from the bottom up, building small "primitive" parsers which we then combine into larger ones.

Let's start with "any unicode character other than quote and backslash". We have a simple condition to test, so we can just use the `satisfy` function:

```fsharp
let jUnescapedChar = 
    let label = "char"
    satisfy (fun ch -> ch <> '\\' && ch <> '\"') label 
```

We can test it immediately:

```fsharp
run jUnescapedChar "a"   // Success 'a'

run jUnescapedChar "\\" |> printResult
// Line:0 Col:0 Error parsing char
// \
// ^Unexpected '\'
```

Ok, good. 

### Escaped characters

Now what about the next case, the escaped characters?

In this case we have a list of strings to match (`"\""`, `"\n"`, etc) and for each of these, a character to use as the result.

The logic will be:

* First define a list of pairs in the form `(stringToMatch, resultChar)`.
* For each of these, build a parser using `pstring stringToMatch >>% resultChar)`.
* Finally, combine all these parsers together using the `choice` function.

Here's the code:

```fsharp
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
```

And again, let's test it immediately:

```fsharp
run jEscapedChar "\\\\" // Success '\'
run jEscapedChar "\\t"  // Success '\009'

run jEscapedChar "a" |> printResult
// Line:0 Col:0 Error parsing escaped char
// a
// ^Unexpected 'a'
```

It works nicely!

### Unicode characters

The final case is the parsing of unicode characters with hex digits.

The logic will be:

* First define the primitives for `backslash`, `u` and `hexdigit`.
* Combine them together, using four `hexdigit`s.
* The output of the parser will be a nested, ugly tuple, so we need a helper function to convert the
  digits to an int, and then a char.

Here's the code:
  
```fsharp
/// Parse a unicode char
let jUnicodeChar = 
    
    // set up the "primitive" parsers        
    let backslash = pchar '\\'
    let uChar = pchar 'u'
    let hexdigit = anyOf (['0'..'9'] @ ['A'..'F'] @ ['a'..'f'])

    // convert the parser output (nested tuples)
    // to a char
    let convertToChar (((h1,h2),h3),h4) = 
        let str = sprintf "%c%c%c%c" h1 h2 h3 h4
        Int32.Parse(str,Globalization.NumberStyles.HexNumber) |> char

    // set up the main parser
    backslash  >>. uChar >>. hexdigit .>>. hexdigit .>>. hexdigit .>>. hexdigit
    |>> convertToChar 
```

And let's test with a smiley face -- `\u263A`.

```fsharp
run jUnicodeChar "\\u263A"  
```

### The complete `String` parser

Putting it all together now:

* Define a primitive for `quote`
* Define a `jchar` as a choice between `jUnescapedChar`, `jEscapedChar`, and `jUnicodeChar`.
* The whole parser is then zero or many `jchar` between two quotes.

```fsharp
let quotedString = 
    let quote = pchar '\"' <?> "quote"
    let jchar = jUnescapedChar <|> jEscapedChar <|> jUnicodeChar 

    // set up the main parser
    quote >>. manyChars jchar .>> quote 
```

One more thing, which is to wrap the quoted string in a `JString` case and give it a label:

```fsharp
/// Parse a JString
let jString = 
    // wrap the string in a JString
    quotedString
    |>> JString           // convert to JString
    <?> "quoted string"   // add label
```

Let's test the complete `jString` function:

```fsharp
run jString "\"\""    // Success ""
run jString "\"a\""   // Success "a"
run jString "\"ab\""  // Success "ab"
run jString "\"ab\\tde\""      // Success "ab\tde"
run jString "\"ab\\u263Ade\""  // Success "ab?de"
```

## 4. Parsing `Number`

The "railway diagram" for Number parsing is:

![](/assets/img/json_number.gif)

Again, we'll work bottom up. Let's start with the most primitive components, the single chars and digits:


```fsharp
let optSign = opt (pchar '-')

let zero = pstring "0"

let digitOneNine = 
    satisfy (fun ch -> Char.IsDigit ch && ch <> '0') "1-9"

let digit = 
    satisfy (fun ch -> Char.IsDigit ch ) "digit"

let point = pchar '.'

let e = pchar 'e' <|> pchar 'E'

let optPlusMinus = opt (pchar '-' <|> pchar '+')
```

Now let's build the "integer" part of the number. This is either:

* The digit zero, or,
* A `nonZeroInt`, which is a `digitOneNine` followed by zero or more normal digits.

```fsharp
let nonZeroInt = 
    digitOneNine .>>. manyChars digit 
    |>> fun (first,rest) -> string first + rest

let intPart = zero <|> nonZeroInt
```

Note that, for the `nonZeroInt` parser, we have to combine the output of `digitOneNine` (a char) with `manyChars digit` (a string)
so a simple map function is needed.

The optional fractional part is a decimal point followed by one or more digits:

```fsharp
let fractionPart = point >>. manyChars1 digit
```

And the exponent part is an `e` followed by an optional sign, followed by one or more digits:

```fsharp
let exponentPart = e >>. optPlusMinus .>>. manyChars1 digit
```

With these components, we can assemble the whole number:

```fsharp
optSign .>>. intPart .>>. opt fractionPart .>>. opt exponentPart
|>> convertToJNumber
<?> "number"   // add label
```

We haven't defined `convertToJNumber` yet though. This function will take the four-tuple output by the parser and convert it
into a float.

Now rather than writing custom float logic, we're going to be lazy and let the .NET framework to the conversion for us!
That is, each of the components will be turned into a string, concatenated, and the whole string parsed into a float. 

The problem is that some of the components (like the sign and exponent) are optional. Let's write a helper that converts
an option to a string using a passed in function, but if the option is `None` return the empty string.

I'm going to call it `|>?` but it doesn't really matter because it is only used locally within the `jNumber` parser.

```fsharp
// utility function to convert an optional value to a string, or "" if missing
let ( |>? ) opt f = 
    match opt with
    | None -> ""
    | Some x -> f x
```

Now we can create `convertToJNumber`:

* The sign is converted to a string.
* The fractional part is converted to a string, prefixed with a decimal point.
* The exponent part is converted to a string, with the sign of the exponent also being converted to a string.

```fsharp
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
```

It's pretty crude, and converting things to strings can be slow, so feel free to write a better version.

With that, we have everything we need for the complete `jNumber` function:

```fsharp
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
```

It's a bit long-winded, but each component follows the spec, so I think it is still quite readable.

Let's start testing it:

```fsharp
run jNumber "123"     // JNumber 123.0
run jNumber "-123"    // JNumber -123.0
run jNumber "123.4"   // JNumber 123.4
```

And what about some failing cases?

```fsharp
run jNumber "-123."   // JNumber -123.0 -- should fail!
run jNumber "00.1"    // JNumber 0      -- should fail!
```

Hmm. Something went wrong! These cases should fail, surely?

Well, no. What's happening in the `-123.` case is that the parser is consuming everything up the to decimal point and then stopping,
leaving the decimal point to be matched by the next parser! So, not an error.

Similarly, in the `00.1` case, the parser is consuming only the first `0` then stopping,
leaving the rest of the input (`0.4`) to be matched by the next parser. Again, not an error.

To fix this properly is out of scope, so let's just add some whitespace to the parser to force it to terminate.

```fsharp
let jNumber_ = jNumber .>> spaces1
```

Now let's test again:

```fsharp
run jNumber_ "123"     // JNumber 123.0
run jNumber_ "-123"    // JNumber -123.0

run jNumber_ "-123." |> printResult
// Line:0 Col:4 Error parsing number andThen many1 whitespace
// -123.
//     ^Unexpected '.'
```

and we find the error is being detected properly now.

Let's test the fractional part:

```fsharp
run jNumber_ "123.4"   // JNumber 123.4

run jNumber_ "00.4" |> printResult
// Line:0 Col:1 Error parsing number andThen many1 whitespace
// 00.4
//  ^Unexpected '0'
```

and the exponent part now:

```fsharp
// exponent only
run jNumber_ "123e4"     // JNumber 1230000.0

// fraction and exponent 
run jNumber_ "123.4e5"   // JNumber 12340000.0
run jNumber_ "123.4e-5"  // JNumber 0.001234
```

It's all looking good so far. Onwards and upwards!

## 5. Parsing `Array`

Next up is the `Array` case.  Again, we can use the railway diagram to guide the implementation:

![](/assets/img/json_array.gif)

We will start with the primitives again. Note that we are adding optional whitespace after each token:

```fsharp
let jArray = 

    let left = pchar '[' .>> spaces
    let right = pchar ']' .>> spaces
    let comma = pchar ',' .>> spaces
    let value = jValue .>> spaces    
```

And then we create a list of values separated by a comma, with the whole list between the left and right brackets.

```fsharp
let jArray = 
    ...

    // set up the list parser
    let values = sepBy1 value comma

    // set up the main parser
    between left values right 
    |>> JArray
    <?> "array"
```

Hold on -- what is this `jValue`?  

```fsharp
let jArray = 
    ...
    let value = jValue .>> spaces    // <=== what is "jValue"?
    ...
```

Well, the spec says that an `Array` can contain a list of values, so we'll assume that we have a `jValue` parser that can parse them.

But to parse a `JValue`, we need to parse a `Array` first! 

We have hit a common problem in parsing -- mutually recursive definitions. We need a `JValue` parser to build an `Array`, but we need an `Array` parser to build a `JValue`.

How can we deal with this?

### Forward references

The trick is to create a forward reference, a dummy `JValue` parser that we can use right now to define the `Array` parser,
and then later on, we will fix up the forward reference with the "real" `JValue` parser.

This is one time where mutable references come in handy!

We will need a helper function to assist us with this, and the logic will be as follows:

* Define a dummy parser that will be replaced later.
* Define a real parser that forwards the input stream to the dummy parser.
* Return both the real parser and a reference to the dummy parser.

Now when the client fixes up the reference, the real parser will forward the input to the new parser that has replaced the dummy parser.

Here's the code:

```fsharp
let createParserForwardedToRef<'a>() =

    let dummyParser= 
        let innerFn input : Result<'a * Input> = failwith "unfixed forwarded parser"
        {parseFn=innerFn; label="unknown"}
    
    // ref to placeholder Parser
    let parserRef = ref dummyParser 

    // wrapper Parser
    let innerFn input = 
        // forward input to the placeholder
        runOnInput !parserRef input 
    let wrapperParser = {parseFn=innerFn; label="unknown"}

    wrapperParser, parserRef
```

With this in place, we can create a placeholder for a parser of type `JValue`:

```fsharp
let jValue,jValueRef = createParserForwardedToRef<JValue>()
```

### Finishing up the `Array` parser

Going back to the `Array` parser, we can now compile it successfully, using the `jValue` placeholder:

```fsharp
let jArray = 

    // set up the "primitive" parsers        
    let left = pchar '[' .>> spaces
    let right = pchar ']' .>> spaces
    let comma = pchar ',' .>> spaces
    let value = jValue .>> spaces   

    // set up the list parser
    let values = sepBy1 value comma

    // set up the main parser
    between left values right 
    |>> JArray
    <?> "array"
```

If we try to test it now, we get an exception because we haven't fixed up the reference:

```fsharp
run jArray "[ 1, 2 ]"

// System.Exception: unfixed forwarded parser
```

So for now, let's fix up the reference to use one of the parsers that we have already created, such as `jNumber`:

```fsharp
jValueRef := jNumber  
```

Now we *can* successfully test the `jArray` function, as long as we are careful to only use numbers in our array!

```fsharp
run jArray "[ 1, 2 ]"
// Success (JArray [JNumber 1.0; JNumber 2.0],

run jArray "[ 1, 2, ]" |> printResult
// Line:0 Col:6 Error parsing array
// [ 1, 2, ]
//       ^Unexpected ','
```

## 6. Parsing `Object`

The parser for `Object` is very similar to the one for `Array`. 

First, the railway diagram:

![](/assets/img/json_object.gif)

Using this, we can create the parser directly, so I'll present it here without comment:

```fsharp
let jObject = 

    // set up the "primitive" parsers        
    let left = pchar '{' .>> spaces
    let right = pchar '}' .>> spaces
    let colon = pchar ':' .>> spaces
    let comma = pchar ',' .>> spaces
    let key = quotedString .>> spaces 
    let value = jValue .>> spaces

    // set up the list parser
    let keyValue = (key .>> colon) .>>. value
    let keyValues = sepBy1 keyValue comma

    // set up the main parser
    between left keyValues right 
    |>> Map.ofList  // convert the list of keyValues into a Map
    |>> JObject     // wrap in JObject     
    <?> "object"    // add label
```

A bit of testing to make sure it works (but remember, only numbers are supported as values for now).

```fsharp
run jObject """{ "a":1, "b"  :  2 }"""
// JObject (map [("a", JNumber 1.0); ("b", JNumber 2.0)]),

run jObject """{ "a":1, "b"  :  2, }""" |> printResult
// Line:0 Col:18 Error parsing object
// { "a":1, "b"  :  2, }
//                   ^Unexpected ','
```

## 7. Putting it all together

Finally, we can combine all six of the parsers using the `choice` combinator, and we can assign this to the `JValue` parser reference that we created earlier:

```fsharp
jValueRef := choice 
    [
    jNull 
    jBool
    jNumber
    jString
    jArray
    jObject
    ]
```

And now we are ready to rock and roll!

### Testing the complete parser: example 1

Here's an example of a JSON string that we can attempt to parse:

```fsharp
let example1 = """{
    "name" : "Scott",
    "isMale" : true,
    "bday" : {"year":2001, "month":12, "day":25 },
    "favouriteColors" : ["blue", "green"]
}"""
run jValue example1
```

And here is the result:

```text
JObject
    (map
        [("bday", JObject(map
                [("day", JNumber 25.0); 
                ("month", JNumber 12.0);
                ("year", JNumber 2001.0)]));
        ("favouriteColors", JArray [JString "blue"; JString "green"]);
        ("isMale", JBool true); 
        ("name", JString "Scott")
        ])
```

### Testing the complete parser: example 2

Here's one from [the example page on json.org](http://json.org/example.html):

```fsharp
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
```

And here is the result:

```text
JObject(map
    [("widget",JObject(map
            [("debug", JString "on");
            ("image",JObject(map
                [("alignment", JString "center");
                    ("hOffset", JNumber 250.0); ("name", JString "sun1");
                    ("src", JString "Images/Sun.png");
                    ("vOffset", JNumber 250.0)]));
            ("text",JObject(map
                [("alignment", JString "center");
                    ("data", JString "Click Here");
                    ("hOffset", JNumber 250.0); 
                    ("name", JString "text1");
                    ("onMouseUp", JString "sun1.opacity = (sun1.opacity / 100) * 90;");
                    ("size", JNumber 36.0); 
                    ("style", JString "bold");
                    ("vOffset", JNumber 100.0)]));
            ("window",JObject(map
                [("height", JNumber 500.0);
                    ("name", JString "main_window");
                    ("title", JString "Sample Konfabulator Widget");
                    ("width", JNumber 500.0)]))]))]),
```

## Complete listing of the JSON parser 

Here's the complete listing for the JSON parser -- it's about 250 lines of useful code. 

*The source code displayed below is also available at [this gist](https://gist.github.com/swlaschin/149deab2d457d8c1be37#file-jsonparser-fsx).*

```fsharp
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

    let dummyParser= 
        let innerFn input : Result<'a * Input> = failwith "unfixed forwarded parser"
        {parseFn=innerFn; label="unknown"}
    
    // ref to placeholder Parser
    let parserRef = ref dummyParser 

    // wrapper Parser
    let innerFn input = 
        // forward input to the placeholder
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

/// Parse a unicode char
let jUnicodeChar = 
    
    // set up the "primitive" parsers        
    let backslash = pchar '\\'
    let uChar = pchar 'u'
    let hexdigit = anyOf (['0'..'9'] @ ['A'..'F'] @ ['a'..'f'])

    // convert the parser output (nested tuples)
    // to a char
    let convertToChar (((h1,h2),h3),h4) = 
        let str = sprintf "%c%c%c%c" h1 h2 h3 h4
        Int32.Parse(str,Globalization.NumberStyles.HexNumber) |> char

    // set up the main parser
    backslash  >>. uChar >>. hexdigit .>>. hexdigit .>>. hexdigit .>>. hexdigit
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
    let values = sepBy1 value comma

    // set up the main parser
    between left values right 
    |>> JArray
    <?> "array"

// ======================================
// Parsing a JObject
// ======================================


let jObject = 

    // set up the "primitive" parsers        
    let left = pchar '{' .>> spaces
    let right = pchar '}' .>> spaces
    let colon = pchar ':' .>> spaces
    let comma = pchar ',' .>> spaces
    let key = quotedString .>> spaces 
    let value = jValue .>> spaces

    // set up the list parser
    let keyValue = (key .>> colon) .>>. value
    let keyValues = sepBy1 keyValue comma

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
```

## Summary

In this post, we built a JSON parser using the parser library that we have developed over the previous posts.

I hope that, by building both the parser library and a real-world parser from scratch, you have gained a good appreciation for how parser combinators work,
and how useful they are.

I'll repeat what I said in the first post: if you are interesting in using this technique in production,
be sure to investigate the [FParsec library](http://www.quanttec.com/fparsec/) for F#, which is optimized for real-world usage.

And if you are using languages other than F#, there is almost certainly a parser combinator library available to use.

* For more information about parser combinators in general, search the internet for "Parsec", the Haskell library that influenced FParsec.
* For some more examples of using FParsec, try one of these posts:
  * [Implementing a phrase search query for FogCreek's Kiln](http://blog.fogcreek.com/fparsec/)
  * [A LOGO Parser](http://trelford.com/blog/post/FParsec.aspx)
  * [A Small Basic Parser](http://trelford.com/blog/post/parser.aspx)
  * [A C# Parser](http://trelford.com/blog/post/parsecsharp.aspx) and [building a C# compiler in F#](https://neildanson.wordpress.com/2014/02/11/building-a-c-compiler-in-f/)
  * [Write Yourself a Scheme in 48 Hours in F#](https://lucabolognese.wordpress.com/2011/08/05/write-yourself-a-scheme-in-48-hours-in-f-part-vi/)
  * [Parsing GLSL, the shading language of OpenGL](http://laurent.le-brun.eu/site/index.php/2010/06/07/54-fsharp-and-fparsec-a-glsl-parser-example)

Thanks!  
  
*The source code for this post is available at [this gist](https://gist.github.com/swlaschin/149deab2d457d8c1be37#file-understanding_parser_combinators-4-fsx).*

