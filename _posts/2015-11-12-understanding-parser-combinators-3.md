---
layout: post
title: "Improving the parser library"
description: "Adding more informative errors"
categories: [Patterns]
seriesId: "Understanding Parser Combinators"
seriesOrder: 3
---

*UPDATE: [Slides and video from my talk on this topic](/parser/)*

In this series, we are looking at how applicative parsers and parser combinators work.

* In the [first post](/posts/understanding-parser-combinators/), we created the foundations of a parsing library.
* In the [second post](/posts/understanding-parser-combinators-2/), we built out the library with many other useful combinators.
* In this post, we'll rework the library to provide more helpful error messages.

<hr>

## 1. Labelling a Parser

In some of the failing code examples from earlier posts, we got confusing errors:

{% highlight fsharp %}
let parseDigit = anyOf ['0'..'9']
run parseDigit "|ABC"  // Failure "Expecting '9'. Got '|'"
{% endhighlight fsharp %}

`parseDigit` is defined as a choice of digit characters, so when the last choice (`'9'`) fails, that is the error message we receive.

But that message is quite confusing. What we *really* want is to receive is an error that mentions "digit", something like: `Failure "Expecting digit. Got '|'"`.

That is, what we need is a way of labeling parsers with a word like "digit" and then showing that label when a failure occurs.

As a reminder, this is how the `Parser` type was defined in earlier posts:

{% highlight fsharp %}
type Parser<'a> = Parser of (string -> Result<'a * string>)
{% endhighlight fsharp %}

In order to add a label, we need to change it into a record structure:

{% highlight fsharp %}
type ParserLabel = string

/// A Parser structure has a parsing function & label
type Parser<'a> = {
    parseFn : (string -> Result<'a * string>)
    label:  ParserLabel 
    }
{% endhighlight fsharp %}

The record contains two fields: the parsing function (`parseFn`) and the `label`.

One problem is that the label is in the parser itself, but not in the `Result`, which means that clients will not know how to display the label along with the error.

So let's add it to the `Failure` case of `Result` as well, in addition to the error message:

{% highlight fsharp %}
// Aliases 
type ParserLabel = string
type ParserError = string

type Result<'a> =
    | Success of 'a
    | Failure of ParserLabel * ParserError 
{% endhighlight fsharp %}

And while we are at it, let's define a helper function to display the result of a parse:

{% highlight fsharp %}
let printResult result =
    match result with
    | Success (value,input) -> 
        printfn "%A" value
    | Failure (label,error) -> 
        printfn "Error parsing %s\n%s" label error
{% endhighlight fsharp %}

### Updating the code

With this change to the definition of `Parser` and `Result`, we have to change some of the basic functions, such as `bindP`:

{% highlight fsharp %}
/// "bindP" takes a parser-producing function f, and a parser p
/// and passes the output of p into f, to create a new parser
let bindP f p =
    let label = "unknown"           // <====== "label" is new!     
    let innerFn input =
        ...
        match result1 with
        | Failure (label,err) ->    // <====== "label" is new!
            ...
        | Success (value1,remainingInput) ->
            ...
    {parseFn=innerFn; label=label}  // <====== "parseFn" and "label" are new!
{% endhighlight fsharp %}

We have to make similar changes to `returnP`, `orElse`, and `many`.  For the complete code, see the gist linked to below.

### Updating the label

When we use a combinator to build a new compound parser, we will often want to assign a new label to it.
In order to do this, we replace the original `parseFn` with another one that returns the new label.

Here's the code:

{% highlight fsharp %}
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
{% endhighlight fsharp %}

And let's create an infix version of this called `<?>`:

{% highlight fsharp %}
/// infix version of setLabel
let ( <?> ) = setLabel
{% endhighlight fsharp %}

Let's test our new toy!

{% highlight fsharp %}
let parseDigit_WithLabel = 
    anyOf ['0'..'9'] 
    <?> "digit"

run parseDigit_WithLabel "|ABC"  
|> printResult
{% endhighlight fsharp %}

And the output is:

{% highlight text %}
Error parsing digit
Unexpected '|'
{% endhighlight text %}

The error message is now `Error parsing digit` rather than `Expecting '9'`. Much better!

### Setting default labels:

We can also set the default labels for certain combinators such as `andThen` and `orElse` based on the inputs:

{% highlight fsharp %}
/// Combine two parsers as "A andThen B"
let andThen p1 p2 =         
    let label = sprintf "%s andThen %s" (getLabel p1) (getLabel p2)
    p1 >>= (fun p1Result -> 
    p2 >>= (fun p2Result -> 
        returnP (p1Result,p2Result) ))
    <?> label         // <====== provide a custom label

// combine two parsers as "A orElse B"
let orElse parser1 parser2 =
    // construct a new label
    let label =       // <====== provide a custom label
        sprintf "%s orElse %s" (getLabel parser1) (getLabel parser2)
            
            
    let innerFn input =
       ... etc ...

/// choose any of a list of characters
let anyOf listOfChars = 
    let label = sprintf "any of %A" listOfChars 
    listOfChars
    |> List.map pchar 
    |> choice
    <?> label         // <====== provide a custom label     
{% endhighlight fsharp %}

<hr>

## 2. Replacing "pchar" with "satisfy"

One thing that has bothered me about all the implementations so far is `pchar`, the basic primitive that all the other functions have built on.

I don't like that it is so tightly coupled to the input model. What happens if we want to parse bytes from a binary format, or other kinds of input.
All the combinators other than `pchar` are loosely coupled. If we could decouple `pchar` as well,
we would be set up for parsing *any* stream of tokens, and that would make me happy!

At this point, I'll repeat one of my favorite FP slogans: "parameterize all the things!" In the case of `pchar`, we'll remove the `charToMatch` parameter and
replace it with a function -- a predicate. We'll call the new function `satisfy`:

{% highlight fsharp %}
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
{% endhighlight fsharp %}

Other than the parameters, the only thing that has changed from the `pchar` implementation is this one line: 

{% highlight fsharp %}
let satisfy predicate label =
    ...
    if predicate first then
    ...
{% endhighlight fsharp %}

With `satisfy` available, we can rewrite `pchar`:

{% highlight fsharp %}
/// parse a char 
let pchar charToMatch = 
    let predicate ch = (ch = charToMatch) 
    let label = sprintf "%c" charToMatch 
    satisfy predicate label 
{% endhighlight fsharp %}

Note that we are setting the label to be the `charToMatch`. This refactoring would not have been as convenient before, because we didn't have the concept of "labels" yet,
and so `pchar` would not have been able to return a useful error message.

The `satisfy` function also lets us write more efficient versions of other parsers. For example, parsing a digit looked like this originally:

{% highlight fsharp %}
/// parse a digit
let digitChar = 
    anyOf ['0'..'9']
{% endhighlight fsharp %}

But now we can rewrite it using a predicate directly, making it a lot more efficient:

{% highlight fsharp %}
/// parse a digit
let digitChar = 
    let predicate = Char.IsDigit 
    let label = "digit"
    satisfy predicate label 
{% endhighlight fsharp %}

Similarly, we can create a more efficient whitespace parser too:

{% highlight fsharp %}
/// parse a whitespace char
let whitespaceChar = 
    let predicate = Char.IsWhiteSpace 
    let label = "whitespace"
    satisfy predicate label 
{% endhighlight fsharp %}

## 3. Adding position and context to error messages

Another way to improve the error messages is to show the line and column that the error occurred on.

Obviously, for simple one-liners, keeping track of the error location is not a problem, but when you are parsing a 100 line JSON file, it will be very helpful.

In order to track the line and column we are going to have to abandon the simple `string` input and replace it with something more complex,
so let's start with that.

### Defining a input that tracks position

First, we will need a `Position` type to store the line and column, with helper functions to increment one column and one line:

{% highlight fsharp %}
type Position = {
    line : int
    column : int
}

/// define an initial position
let initialPos = {line=0; column=0}

/// increment the column number
let incrCol pos = 
    {pos with column=pos.column + 1}

/// increment the line number and set the column to 0
let incrLine pos = 
    {line=pos.line + 1; column=0}
{% endhighlight fsharp %}

Next, we'll need to combine the input string with a position into a single "input state" type.  Since we are line oriented, we can make our
lives easier and store the input string as a array of lines rather than as one giant string:

{% highlight fsharp %}
/// Define the current input state
type InputState = {
    lines : string[]
    position : Position 
}
{% endhighlight fsharp %}

We will also need a way to convert a string into a initial `InputState`:

{% highlight fsharp %}
/// Create a new InputState from a string
let fromStr str = 
    if String.IsNullOrEmpty(str) then
        {lines=[||]; position=initialPos}
    else
        let separators = [| "\r\n"; "\n" |]
        let lines = str.Split(separators, StringSplitOptions.None)
        {lines=lines; position=initialPos}
{% endhighlight fsharp %}

Finally, and most importantly, we need a way to read the next character from the input -- let's call it `nextChar`.

We know what the input for `nextChar` will be (an `InputState`) but what should the output look like?

* If the input is at the end, we need a way to indicate that there is no next character, so in that case return `None`.
* Therefore in the case when a character is available, we will return `Some`. 
* In addition, the input state will have changed because the column (or line) will have been incremented as well.

So, putting this together, the input for `nextChar` is an `InputState` and the output is a pair `char option * InputState`.

The logic for returning the next char will be as follows then:

* If we are at the last character of the input, return EOF (`None`) and don't change the state.
* If the current column is *not* at the end of a line, return the character at that position and change the state by incrementing the column position.
* If the current column *is* at the end of a line, return a newline character and change the state by incrementing the line position.

Here's the code:

{% highlight fsharp %}
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
{% endhighlight fsharp %}

Unlike the earlier `string` implementation, the underlying array of lines is never altered or copied -- only the position is changed. This means that
making a new state each time the position changes should be reasonably efficient, because the text is shared everywhere.

Let's quickly test that the implementation works. We'll create a helper function `readAllChars` and then see what it returns
for different inputs:

{% highlight fsharp %}
let rec readAllChars input =
    [
        let remainingInput,charOpt = nextChar input 
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
{% endhighlight fsharp %}

Here it is with some example inputs:

{% highlight fsharp %}
fromStr "" |> readAllChars       // []
fromStr "a" |> readAllChars      // ['a'; '\n']
fromStr "ab" |> readAllChars     // ['a'; 'b'; '\n']
fromStr "a\nb" |> readAllChars   // ['a'; '\n'; 'b'; '\n']
{% endhighlight fsharp %}

Note that the implementation returns a newline at the end of the input, even if the input doesn't have one. I think that this is a feature, not a bug!

### Changing the parser to use the input

We now need to change the `Parser` type again. 

To start with, the `Failure` case needs to return some kind of data that indicates the position, so we can show it in an error message.

We could just use the `InputState` as is, but let's be good and define a new type specially for this use, called `ParserPosition`:

{% highlight fsharp %}
/// Stores information about the parser position for error messages
type ParserPosition = {
    currentLine : string
    line : int
    column : int
    }
{% endhighlight fsharp %}

We'll need some way to convert a `InputState` into a `ParserPosition`:

{% highlight fsharp %}
let parserPositionFromInputState (inputState:Input) = {
    currentLine = TextInput.currentLine inputState
    line = inputState.position.line
    column = inputState.position.column
    }
{% endhighlight fsharp %}

And finally, we can update the `Result` type to include `ParserPosition`:

{% highlight fsharp %}
// Result type
type Result<'a> =
    | Success of 'a
    | Failure of ParserLabel * ParserError * ParserPosition 
{% endhighlight fsharp %}

In addition, the `Parser` type needs to change from `string` to `InputState`:

{% highlight fsharp %}
type Input = TextInput.InputState  // type alias

/// A Parser structure has a parsing function & label
type Parser<'a> = {
    parseFn : (Input -> Result<'a * Input>)
    label:  ParserLabel 
    }
{% endhighlight fsharp %}

With all this extra information available, the `printResult` function can be enhanced to print the text of the current line, along with a caret where the error is:

{% highlight fsharp %}
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
{% endhighlight fsharp %}

Let's test `printResult` with a dummy error value:

{% highlight fsharp %}
let exampleError = 
    Failure ("identifier", "unexpected |",
             {currentLine = "123 ab|cd"; line=1; column=6})

printResult exampleError 
{% endhighlight fsharp %}

The output is shown below:

{% highlight text %}
Line:1 Col:6 Error parsing identifier
123 ab|cd
      ^unexpected |
{% endhighlight text %}

Much nicer than before!

### Fixing up the `run` function

The `run` function now needs to take an `InputState` not a string.  But we also want the convenience of running against string input,
so let's create two `run` functions, one that takes an `InputState` and one that takes a `string`:

{% highlight fsharp %}
/// Run the parser on a InputState
let runOnInput parser input = 
    // call inner function with input
    parser.parseFn input

/// Run the parser on a string
let run parser inputStr = 
    // call inner function with input
    runOnInput parser (TextInput.fromStr inputStr)
{% endhighlight fsharp %}

### Fixing up the combinators

We now have three items in the `Failure` case rather than two. This breaks some code but is easy to fix. I'm tempted to create a special `ParserError` type
so that it never happens again, but for now, I'll just fix up the errors.

Here's a new version of `satisfy`:

{% highlight fsharp %}
/// Match an input token if the predicate is satisfied
let satisfy predicate label =
    let innerFn input =
        let remainingInput,charOpt = TextInput.nextChar input 
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
{% endhighlight fsharp %}

Note that the failure case code is now `Failure (label,err,pos)` where the parser position is built from the input state.

And here is `bindP`:

{% highlight fsharp %}
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
{% endhighlight fsharp %}

We can fix up the other functions in the same way.

### Testing the positional errors

Let's test with a real parser now:

{% highlight fsharp %}
let parseAB = 
    pchar 'A' .>>. pchar 'B' 
    <?> "AB"

run parseAB "A|C"  
|> printResult
{% endhighlight fsharp %}

And the output is:

{% highlight text %}
// Line:0 Col:1 Error parsing AB
// A|C
//  ^Unexpected '|'
{% endhighlight text %}

Excellent!  I think we can stop now.

## 4. Adding some standard parsers to the library

In the previous posts, we've built parsers for strings and ints in passing, but now let's add them to the core library, so that clients don't have to reinvent the wheel.

These parsers are based on those in the [the FParsec library](http://www.quanttec.com/fparsec/reference/charparsers.html#).

Let's start with some string-related parsers. I will present them without comment -- I hope that the code is self-explanatory by now.

{% highlight fsharp %}
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
{% endhighlight fsharp %}

Let's test `pstring`, for example:

{% highlight fsharp %}
run (pstring "AB") "ABC"  
|> printResult   
// Success
// "AB"

run (pstring "AB") "A|C"  
|> printResult
// Line:0 Col:1 Error parsing AB
// A|C
//  ^Unexpected '|'
{% endhighlight fsharp %}

### Whitespace parsers

Whitespace is important in parsing, even if we do end up mostly throwing it away!

{% highlight fsharp %}
/// parse a whitespace char
let whitespaceChar = 
    let predicate = Char.IsWhiteSpace 
    let label = "whitespace"
    satisfy predicate label 

/// parse zero or more whitespace char
let spaces = many whitespaceChar

/// parse one or more whitespace char
let spaces1 = many1 whitespaceChar
{% endhighlight fsharp %}

And here's some whitespace tests:

{% highlight fsharp %}
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
{% endhighlight fsharp %}

### Numeric parsers

Finally, we need a parser for ints and floats.

{% highlight fsharp %}
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
{% endhighlight fsharp %}

And some tests:

{% highlight fsharp %}
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
{% endhighlight fsharp %}

## 5. Backtracking

One more topic that we should discuss is "backtracking".

Let's say that you have two parsers: one to match the string `A-1` and and another to match the string `A-2`. If the input is
`A-2` then the first parser will fail at the third character and the second parser will be attempted. 

Now the second parser must start at the *beginning* of the original sequence of characters, not at the third character. That is, we
need to undo the current position in the input stream and go back to the first position.

If we were using a mutable input stream then this might be a tricky problem, but thankfully we are using immutable data, and so
"undoing" the position just means using the original input value. And of course, this is exactly what combinators such as `orElse` (`<|>`) do.

In other words, we get backtracking "for free" when we use immutable input state. Yay!

Sometimes however, we *don't* want to backtrack. For example, let's say we have these parsers:

* let `forExpression` = the "for" keyword, then an identifier, then the "in" keyword, etc. 
* let `ifExpression` = the "if" keyword, then an identifier, then the "then" keyword, etc.

and we then create a combined expression parser that chooses between them:

* let `expression` = `forExpression <|> ifExpression` 

Now, if the input stream is `for &&& in something` then the `forExpression` parser will error when it hits the sequence `&&&`, because it is expecting
a valid identifier. At this point we *don't* want to backtrack and try the `ifExpression` -- we want to show an error such as "identifier expected after 'for'".

The rule then is that: *if* input has been consumed successfully (in this case, the `for` keyword was matched successfully) then do *not* backtrack.

We're not going to implement this rule in our simple library, but a proper library like FParsec does implement this and also has support
for [bypassing it when needed](http://www.quanttec.com/fparsec/reference/primitives.html#members.attempt).

## Listing of the final parser library 

The parsing library is up to 500 lines of code now, so I won't show it here. You can see it at [this gist](https://gist.github.com/swlaschin/485f418fede6b6a36d89#file-parserlibrary-fsx).

## Summary

In this post, we added better error handling and some more parsers.

Now we have everything we need to build a JSON parser!
That will be the topic of the [next post](/posts/understanding-parser-combinators-4/).  

*The source code for this post is available at [this gist](https://gist.github.com/swlaschin/485f418fede6b6a36d89#file-understanding_parser_combinators-3-fsx).*

