---
layout: post
title: "Worked example: Roman numerals"
description: "More pattern matching in practice"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 12
categories: [Patterns, Worked Examples]
---

[Last time](/posts/pattern-matching-command-line) we looked at parsing a command line. This time we'll we'll look at another pattern matching example, this time using Roman numerals.

As before, we will try to have a "pure" internal model with separate stages to convert the input to the internal model, and then another separate stage to convert from the internal model to the output.

![](/assets/img/function_transform2.png)

## Requirements

Let's start with the requirements:

	1) Accept a string of letters like "MMMXCLXXIV" as a string and convert it to an integer.
	The conversions are: I=1; V=5; X=10; L=50; C=100; D=500; and M=1000;
	
	If a lower letter comes before a higher one, the value of the higher is reduced accordingly, so
	IV=4; IX=9; XC=90; and so on.

	2) As an additional step, validate the string of letters to see if it is a valid number. For example: "IIVVMM" is a not a valid roman numeral.


## First version

As before we'll start by first creating the internal model, and then look at how we can parse the input into the internal model. 

Here's a first stab at the model. We'll treat a `RomanNumeral` as a list of `RomanDigits`.

```fsharp
type RomanDigit = int
type RomanNumeral = RomanDigit list 
```

No, stop right there! A `RomanDigit` is not just *any* digit, it has to be taken from a limited set.

Also `RomanNumeral` should not just be a [type alias](/posts/type-abbreviations) for a list of digits. It would be better if it was its own special type as well.
We can do this by creating a [single case union type](/posts/discriminated-unions).

Here's a much better version:

```fsharp
type RomanDigit = I | V | X | L | C | D | M
type RomanNumeral = RomanNumeral of RomanDigit list 
```

### Output: Converting a numeral to an int

Now let's do the output logic, converting a Roman numeral to an int.

The digit conversion is easy:

```fsharp
/// Converts a single RomanDigit to an integer
let digitToInt =
    function
    | I -> 1
    | V -> 5
    | X -> 10
    | L -> 50
    | C -> 100
    | D -> 500
    | M -> 1000

// tests
I  |> digitToInt
V  |> digitToInt
M  |> digitToInt
```

Note that we're using the `function` keyword instead of the `match..with` expression.

To convert a list of digits, we'll use a recursive loop again.
There is a special case when we need to look ahead to the next digit, and if it is larger than the current one, then use their difference.

```fsharp
let rec digitsToInt =
    function
        
    // empty is 0
    | [] -> 0

    // special case when a smaller comes before larger
    // convert both digits and add the difference to the sum 
	// Example: "IV" and "CM"
    | smaller::larger::ns when smaller < larger -> 
        (digitToInt larger - digitToInt smaller)  + digitsToInt ns

    // otherwise convert the digit and add to the sum 
    | digit::ns -> 
        digitToInt digit + digitsToInt ns

// tests
[I;I;I;I]  |> digitsToInt
[I;V]  |> digitsToInt
[V;I]  |> digitsToInt
[I;X]  |> digitsToInt
[M;C;M;L;X;X;I;X]  |> digitsToInt // 1979
[M;C;M;X;L;I;V] |> digitsToInt // 1944
```

*Note that the "less than" operation did not have to be defined. The types are automatically sorted by their order of declaration.*

Finally, we can convert the `RomanNumeral` type itself by unpacking the contents into a list and calling `digitsToInt`.

```fsharp
/// converts a RomanNumeral to an integer
let toInt (RomanNumeral digits) = digitsToInt digits

// test
let x = RomanNumeral [I;I;I;I]
x |> toInt

let x = RomanNumeral [M;C;M;L;X;X;I;X]
x |> toInt
```

That takes care of the output.

### Input: Converting a string to an Roman Numeral

Now let's do the input logic, converting a string to our internal model.

First, let's handle a single character conversion. It seems straightforward.

```fsharp
let charToRomanDigit =
    function
    | 'I' -> I
    | 'V' -> V
    | 'X' -> X
    | 'L' -> L
    | 'C' -> C
    | 'D' -> D
    | 'M' -> M
```

The compiler doesn't like that! What happens if we get some other character? 

This is a great example of how [exhaustive pattern matching](/posts/correctness-exhaustive-pattern-matching) can force you to think about missing requirements. 

So, what should be done for bad input. How about printing an error message? 

Let's try again and add a case to handle all other characters:

```fsharp
let charToRomanDigit =
    function
    | 'I' -> I
    | 'V' -> V
    | 'X' -> X
    | 'L' -> L
    | 'C' -> C
    | 'D' -> D
    | 'M' -> M
	| ch -> eprintf "%c is not a valid character" ch
```

The compiler doesn't like that either! The normal cases return a valid `RomanDigit` but the error case returns `unit`. As we saw in the [earlier post](/posts/pattern-matching), every branch *must* return the same type.

How can we fix this? We could throw an exception, but that seems a bit excessive. If we think about it some more, there's no way that `charToRomanDigit` can *always* return a valid `RomanDigit`.
Sometimes it can, and sometimes it can't. In other words, we need to use something like an option type here. 

But on further consideration, we might also want the caller to know what the bad char was. So we need to create our own little variant on the option type to hold both cases.

Here's the fixed up version:

```fsharp
type ParsedChar = 
    | Digit of RomanDigit 
    | BadChar of char

let charToRomanDigit =
    function
    | 'I' -> Digit I
    | 'V' -> Digit V
    | 'X' -> Digit X
    | 'L' -> Digit L
    | 'C' -> Digit C
    | 'D' -> Digit D
    | 'M' -> Digit M
    | ch -> BadChar ch
```

Note that I have removed the error message. Since the bad char is being returned, the caller can print its own message for the `BadChar` case.

Next, we should check the function signature to make sure it is what we expect:

```fsharp
charToRomanDigit : char -> ParsedChar
```

That looks good.  

Now, how can we convert a string into these digits? We convert the string to a char array, convert that into a list, and then do a final conversion using `charToRomanDigit`.

```fsharp
let toRomanDigitList s = 
    s.ToCharArray() // error FS0072
    |> List.ofArray 
    |> List.map charToRomanDigit
```

But the compiler complains again with "FS0072: Lookup on object of indeterminate type",

That typically happens when you use a method rather than a function.  Any object could implement `.ToCharArray()` so the type inference cannot tell what type is meant.

In this case, the solution is just to use an explicit type annotation on the parameter -- our first so far!

```fsharp
let toRomanDigitList (s:string) = 
    s.ToCharArray() 
    |> List.ofArray 
    |> List.map charToRomanDigit
```

But look at the signature:

```fsharp
toRomanDigitList : string -> ParsedChar list
```

It still has the pesky `ParsedChar` in it rather than `RomanDigits`. How do we want to proceed? Answer, let's pass the buck again and let someone else deal with it!

"Passing the buck" in this case is actually a good design principle. This function doesn't know what its clients might want to do -- some might want to ignore errors, while others might want to fail fast. So just pass back the information and let them decide.

In this case, the client is the top level function that creates a `RomanNumeral` type. Here's our first attempt:

```fsharp
// convert a string to a RomanNumeral
let toRomanNumeral s = 
    toRomanDigitList s
    |> RomanNumeral
```

The compiler is not happy -- the `RomanNumeral` constructor requires a list of `RomanDigits`, but the `toRomanDigitList` is giving us a list of `ParsedChars` instead.

Now we finally *do* have to commit to an error handling policy. Let's choose to ignore bad chars, but print out errors when they occur. We'll use the `List.choose` function for this.
It's similar to `List.map`, but in addition has a filter built into it. Elements that are valid (`Some something`) are returned, but elements that are `None` are filtered out.

Our choose function should thus do the following:

* For valid digits return `Some digit` 
* For the invalid `BadChars`, print the error message and return `None`.

If we do this, the output of `List.choose` will be a list of `RomanDigits`, exactly as needed as the input to the `RomanNumeral` constructor.

Here is everything put together:

```fsharp
/// Convert a string to a RomanNumeral
/// Does not validate the input.E.g. "IVIV" would be valid
let toRomanNumeral s = 
    toRomanDigitList s
    |> List.choose (
        function 
        | Digit digit -> 
            Some digit 
        | BadChar ch -> 
            eprintfn "%c is not a valid character" ch
            None
        )
    |> RomanNumeral
```

Let's test!

```fsharp
// test good cases

"IIII"  |> toRomanNumeral
"IV"  |> toRomanNumeral
"VI"  |> toRomanNumeral
"IX"  |> toRomanNumeral
"MCMLXXIX"  |> toRomanNumeral
"MCMXLIV" |> toRomanNumeral
"" |> toRomanNumeral

// error cases
"MC?I" |> toRomanNumeral
"abc" |> toRomanNumeral
```

Ok, everything is good so far. Let's move on to validation.

### Validation rules

The validation rules were not listed in the requirements, so let's put down our best guess based on what we know about Roman numerals:

* Five in a row of any digit is not allowed
* Some digits are allowed in runs of up to 4. They are I,X,C, and M. The others (V,L,D) can only appear singly.
* Some lower digits can come before a higher digit, but only if they appear singly. E.g. "IX" is ok but "IIIX" is not.
* But this is only for pairs of digits. Three ascending numbers in a row is invalid. E.g. "IX" is ok but "IXC" is not.
* A single digit with no runs is always allowed

We can convert these requirements into a pattern matching function as follows:

```fsharp
let runsAllowed = 
    function 
    | I | X | C | M -> true
    | V | L | D -> false

let noRunsAllowed  = runsAllowed >> not 

// check for validity
let rec isValidDigitList digitList =
    match digitList with

    // empty list is valid
    | [] -> true

    // A run of 5 or more anything is invalid
    // Example:  XXXXX
    | d1::d2::d3::d4::d5::_ 
        when d1=d2 && d1=d3 && d1=d4 && d1=d5 -> 
            false

    // 2 or more non-runnable digits is invalid
    // Example:  VV
    | d1::d2::_ 
        when d1=d2 && noRunsAllowed d1 -> 
            false

    // runs of 2,3,4 in the middle are invalid if next digit is higher
    // Example:  IIIX
    | d1::d2::d3::d4::higher::ds 
        when d1=d2 && d1=d3 && d1=d4 
        && runsAllowed d1 // not really needed because of the order of matching
        && higher > d1 -> 
            false

    | d1::d2::d3::higher::ds 
        when d1=d2 && d1=d3 
        && runsAllowed d1 
        && higher > d1 -> 
            false

    | d1::d2::higher::ds 
        when d1=d2 
        && runsAllowed d1 
        && higher > d1 -> 
            false

    // three ascending numbers in a row is invalid
    // Example:  IVX
    | d1::d2::d3::_  when d1<d2 && d2<= d3 -> 
        false

    // A single digit with no runs is always allowed
    | _::ds -> 
        // check the remainder of the list
        isValidDigitList ds 

```

*Again, note that "equality" and "less than" did not need to be defined.*

And let's test the validation:

```fsharp
// test valid 
let validList = [
    [I;I;I;I]
    [I;V]
    [I;X]
    [I;X;V]
    [V;X]
    [X;I;V]
    [X;I;X]
    [X;X;I;I]
    ]

let testValid = validList |> List.map isValidDigitList

let invalidList = [
    // Five in a row of any digit is not allowed
    [I;I;I;I;I]
    // Two in a row for V,L, D is not allowed
    [V;V] 
    [L;L] 
    [D;D]
    // runs of 2,3,4 in the middle are invalid if next digit is higher
    [I;I;V]
    [X;X;X;M]
    [C;C;C;C;D]
    // three ascending numbers in a row is invalid
    [I;V;X]
    [X;L;D]
    ]
let testInvalid = invalidList |> List.map isValidDigitList
```

Finally, we add a top level function to test validity of the `RomanNumeral` type itself.
```fsharp
// top level check for validity
let isValid (RomanNumeral digitList) =
    isValidDigitList digitList


// test good cases
"IIII"  |> toRomanNumeral |> isValid
"IV"  |> toRomanNumeral |> isValid
"" |> toRomanNumeral |> isValid

// error cases
"IIXX" |> toRomanNumeral |> isValid
"VV" |> toRomanNumeral |> isValid

// grand finale
[ "IIII"; "XIV"; "MMDXC"; 
"IIXX"; "VV"; ]
|> List.map toRomanNumeral 
|> List.iter (function
    | n when isValid n ->
        printfn "%A is valid and its integer value is %i" n (toInt n) 
    | n ->
        printfn "%A is not valid" n
    )
```

## The entire code for the first version

Here's all the code in one module:

```fsharp
module RomanNumeralsV1 =

    // ==========================================
    // Types
    // ==========================================

    type RomanDigit = I | V | X | L | C | D | M
    type RomanNumeral = RomanNumeral of RomanDigit list 

    // ==========================================
    // Output logic
    // ==========================================

    /// Converts a single RomanDigit to an integer
    let digitToInt =
        function
        | I -> 1
        | V -> 5
        | X -> 10
        | L -> 50
        | C -> 100
        | D -> 500
        | M -> 1000

    /// converts a list of digits to an integer
    let rec digitsToInt =
        function
        
        // empty is 0
        | [] -> 0

        // special case when a smaller comes before larger
        // convert both digits and add the difference to the sum 
        // Example: "IV" and "CM"
        | smaller::larger::ns when smaller < larger -> 
            (digitToInt larger - digitToInt smaller)  + digitsToInt ns

        // otherwise convert the digit and add to the sum 
        | digit::ns -> 
            digitToInt digit + digitsToInt ns

    /// converts a RomanNumeral to an integer
    let toInt (RomanNumeral digits) = digitsToInt digits

    // ==========================================
    // Input logic
    // ==========================================

    type ParsedChar = 
        | Digit of RomanDigit 
        | BadChar of char

    let charToRomanDigit =
        function
        | 'I' -> Digit I
        | 'V' -> Digit V
        | 'X' -> Digit X
        | 'L' -> Digit L
        | 'C' -> Digit C
        | 'D' -> Digit D
        | 'M' -> Digit M
        | ch -> BadChar ch

    let toRomanDigitList (s:string) = 
        s.ToCharArray() 
        |> List.ofArray 
        |> List.map charToRomanDigit

    /// Convert a string to a RomanNumeral
    /// Does not validate the input.E.g. "IVIV" would be valid
    let toRomanNumeral s = 
        toRomanDigitList s
        |> List.choose (
            function 
            | Digit digit -> 
                Some digit 
            | BadChar ch -> 
                eprintfn "%c is not a valid character" ch
                None
            )
        |> RomanNumeral

    // ==========================================
    // Validation logic
    // ==========================================

    let runsAllowed = 
        function 
        | I | X | C | M -> true
        | V | L | D -> false

    let noRunsAllowed  = runsAllowed >> not 

    // check for validity
    let rec isValidDigitList digitList =
        match digitList with

        // empty list is valid
        | [] -> true

        // A run of 5 or more anything is invalid
        // Example:  XXXXX
        | d1::d2::d3::d4::d5::_ 
            when d1=d2 && d1=d3 && d1=d4 && d1=d5 -> 
                false

        // 2 or more non-runnable digits is invalid
        // Example:  VV
        | d1::d2::_ 
            when d1=d2 && noRunsAllowed d1 -> 
                false

        // runs of 2,3,4 in the middle are invalid if next digit is higher
        // Example:  IIIX
        | d1::d2::d3::d4::higher::ds 
            when d1=d2 && d1=d3 && d1=d4 
            && runsAllowed d1 // not really needed because of the order of matching
            && higher > d1 -> 
                false

        | d1::d2::d3::higher::ds 
            when d1=d2 && d1=d3 
            && runsAllowed d1 
            && higher > d1 -> 
                false

        | d1::d2::higher::ds 
            when d1=d2 
            && runsAllowed d1 
            && higher > d1 -> 
                false

        // three ascending numbers in a row is invalid
        // Example:  IVX
        | d1::d2::d3::_  when d1<d2 && d2<= d3 -> 
            false

        // A single digit with no runs is always allowed
        | _::ds -> 
            // check the remainder of the list
            isValidDigitList ds 

    // top level check for validity
    let isValid (RomanNumeral digitList) =
        isValidDigitList digitList

```

## Second version

The code works, but there is something that's bugging me about it. The validation logic seems very complicated. Surely the Romans didn't have to think about all of this?

And also, I can think of examples that should fail validation, but pass, such as "VIV":

```fsharp
"VIV" |> toRomanNumeral |> isValid
```

We could try to tighten up our validation rules, but let's try another tack. Complicated logic is often a sign that you don't quite understand the domain properly.

In other words -- could we change the internal model to make everything simpler?

What about if we stopped trying to map letters to digits, and created a domain that mapped how the Romans thought it.  In this model "I", "II", "III", "IV" and so on would each be a separate digit.

Let's run with it and see what happens.

Here's the new types for the domain. I now have a digit type for every possible digit. The `RomanNumeral` type stays the same.

```fsharp
type RomanDigit = 
    | I | II | III | IIII 
    | IV | V 
    | IX | X | XX | XXX | XXXX  
    | XL | L 
    | XC | C | CC | CCC | CCCC 
    | CD | D 
    | CM | M | MM | MMM | MMMM
type RomanNumeral = RomanNumeral of RomanDigit list 
```

### Output: second version

Next, converting a single `RomanDigit` to an integer is the same as before, but with more cases:

```fsharp
/// Converts a single RomanDigit to an integer
let digitToInt =
    function
    | I -> 1 | II -> 2 | III -> 3 | IIII -> 4 
    | IV -> 4 | V -> 5
    | IX -> 9 | X -> 10 | XX -> 20 | XXX -> 30 | XXXX -> 40 
    | XL -> 40 | L -> 50 
    | XC -> 90 | C -> 100 | CC -> 200 | CCC -> 300 | CCCC -> 400 
    | CD -> 400 | D -> 500 
    | CM -> 900 | M -> 1000 | MM -> 2000 | MMM -> 3000 | MMMM -> 4000

// tests
I  |> digitToInt
III  |> digitToInt
V  |> digitToInt
CM  |> digitToInt
```

Calculating the sum of the digits is now trivial. No special cases needed:

```fsharp
/// converts a list of digits to an integer
let digitsToInt list = 
    list |> List.sumBy digitToInt 

// tests
[IIII]  |> digitsToInt
[IV]  |> digitsToInt
[V;I]  |> digitsToInt
[IX]  |> digitsToInt
[M;CM;L;X;X;IX]  |> digitsToInt // 1979
[M;CM;XL;IV] |> digitsToInt // 1944
```

Finally, the top level function is identical:

```fsharp
/// converts a RomanNumeral to an integer
let toInt (RomanNumeral digits) = digitsToInt digits

// test
let x = RomanNumeral [M;CM;LX;X;IX]
x |> toInt
```

### Input: second version

For the input parsing, we'll keep the `ParsedChar` type. But this time, we have to match 1,2,3, or 4 chars at a time.
That means we can't just pull off one character like we did in the first version -- we have to match in the main loop. This means the loop now has to be recursive.

Also, we want to convert IIII into a single `IIII` digit rather than 4 separate `I` digits, so we put the longest matches at the front.

```fsharp
type ParsedChar = 
    | Digit of RomanDigit 
    | BadChar of char

let rec toRomanDigitListRec charList = 
    match charList with
    // match the longest patterns first

    // 4 letter matches
    | 'I'::'I'::'I'::'I'::ns -> 
        Digit IIII :: (toRomanDigitListRec ns)
    | 'X'::'X'::'X'::'X'::ns -> 
        Digit XXXX :: (toRomanDigitListRec ns)
    | 'C'::'C'::'C'::'C'::ns -> 
        Digit CCCC :: (toRomanDigitListRec ns)
    | 'M'::'M'::'M'::'M'::ns -> 
        Digit MMMM :: (toRomanDigitListRec ns)

    // 3 letter matches
    | 'I'::'I'::'I'::ns -> 
        Digit III :: (toRomanDigitListRec ns)
    | 'X'::'X'::'X'::ns -> 
        Digit XXX :: (toRomanDigitListRec ns)
    | 'C'::'C'::'C'::ns -> 
        Digit CCC :: (toRomanDigitListRec ns)
    | 'M'::'M'::'M'::ns -> 
        Digit MMM :: (toRomanDigitListRec ns)

    // 2 letter matches
    | 'I'::'I'::ns -> 
        Digit II :: (toRomanDigitListRec ns)
    | 'X'::'X'::ns -> 
        Digit XX :: (toRomanDigitListRec ns)
    | 'C'::'C'::ns -> 
        Digit CC :: (toRomanDigitListRec ns)
    | 'M'::'M'::ns -> 
        Digit MM :: (toRomanDigitListRec ns)

    | 'I'::'V'::ns -> 
        Digit IV :: (toRomanDigitListRec ns)
    | 'I'::'X'::ns -> 
        Digit IX :: (toRomanDigitListRec ns)
    | 'X'::'L'::ns -> 
        Digit XL :: (toRomanDigitListRec ns)
    | 'X'::'C'::ns -> 
        Digit XC :: (toRomanDigitListRec ns)
    | 'C'::'D'::ns -> 
        Digit CD :: (toRomanDigitListRec ns)
    | 'C'::'M'::ns -> 
        Digit CM :: (toRomanDigitListRec ns)

    // 1 letter matches
    | 'I'::ns -> 
        Digit I :: (toRomanDigitListRec ns)
    | 'V'::ns -> 
        Digit V :: (toRomanDigitListRec ns)
    | 'X'::ns -> 
        Digit X :: (toRomanDigitListRec ns)
    | 'L'::ns -> 
        Digit L :: (toRomanDigitListRec ns)
    | 'C'::ns -> 
        Digit C :: (toRomanDigitListRec ns)
    | 'D'::ns -> 
        Digit D :: (toRomanDigitListRec ns)
    | 'M'::ns -> 
        Digit M :: (toRomanDigitListRec ns)

    // bad letter matches
    | badChar::ns -> 
        BadChar badChar :: (toRomanDigitListRec ns)

    // 0 letter matches
    | [] -> 
        []

```

Well, this is much longer than the first version, but otherwise basically the same.

The top level functions are unchanged.

```fsharp
let toRomanDigitList (s:string) = 
    s.ToCharArray() 
    |> List.ofArray 
    |> toRomanDigitListRec

/// Convert a string to a RomanNumeral
let toRomanNumeral s = 
    toRomanDigitList s
    |> List.choose (
        function 
        | Digit digit -> 
            Some digit 
        | BadChar ch -> 
            eprintfn "%c is not a valid character" ch
            None
        )
    |> RomanNumeral

// test good cases
"IIII"  |> toRomanNumeral
"IV"  |> toRomanNumeral
"VI"  |> toRomanNumeral
"IX"  |> toRomanNumeral
"MCMLXXIX"  |> toRomanNumeral
"MCMXLIV" |> toRomanNumeral
"" |> toRomanNumeral

// error cases
"MC?I" |> toRomanNumeral
"abc" |> toRomanNumeral
```

### Validation: second version

Finally, let's see how the new domain model affects the validation rules.  Now, the rules are *much* simpler. In fact, there is only one.

* Each digit must be smaller than the preceding digit

```fsharp
// check for validity
let rec isValidDigitList digitList =
    match digitList with

    // empty list is valid
    | [] -> true

    // a following digit that is equal or larger is an error
    | d1::d2::_ 
        when d1 <= d2  -> 
            false

    // A single digit is always allowed
    | _::ds -> 
        // check the remainder of the list
        isValidDigitList ds 

// top level check for validity
let isValid (RomanNumeral digitList) =
    isValidDigitList digitList

// test good cases
"IIII"  |> toRomanNumeral |> isValid
"IV"  |> toRomanNumeral |> isValid
"" |> toRomanNumeral |> isValid

// error cases
"IIXX" |> toRomanNumeral |> isValid
"VV" |> toRomanNumeral |> isValid

```

Alas, after all that, we still didn't fix the bad case that triggered the rewrite!

```fsharp
"VIV" |> toRomanNumeral |> isValid
```

There is a not-too-complicated fix for this, but I think it's time to leave it alone now!

## The entire code for the second version

Here's all the code in one module for the second version:

```fsharp
module RomanNumeralsV2 =

    // ==========================================
    // Types
    // ==========================================

    type RomanDigit = 
        | I | II | III | IIII 
        | IV | V 
        | IX | X | XX | XXX | XXXX  
        | XL | L 
        | XC | C | CC | CCC | CCCC 
        | CD | D 
        | CM | M | MM | MMM | MMMM
    type RomanNumeral = RomanNumeral of RomanDigit list 

    // ==========================================
    // Output logic
    // ==========================================

    /// Converts a single RomanDigit to an integer
    let digitToInt =
        function
        | I -> 1 | II -> 2 | III -> 3 | IIII -> 4 
        | IV -> 4 | V -> 5
        | IX -> 9 | X -> 10 | XX -> 20 | XXX -> 30 | XXXX -> 40 
        | XL -> 40 | L -> 50 
        | XC -> 90 | C -> 100 | CC -> 200 | CCC -> 300 | CCCC -> 400 
        | CD -> 400 | D -> 500 
        | CM -> 900 | M -> 1000 | MM -> 2000 | MMM -> 3000 | MMMM -> 4000

    /// converts a RomanNumeral to an integer
    let toInt (RomanNumeral digits) = digitsToInt digits

    // ==========================================
    // Input logic
    // ==========================================

    type ParsedChar = 
        | Digit of RomanDigit 
        | BadChar of char

    let rec toRomanDigitListRec charList = 
        match charList with
        // match the longest patterns first

        // 4 letter matches
        | 'I'::'I'::'I'::'I'::ns -> 
            Digit IIII :: (toRomanDigitListRec ns)
        | 'X'::'X'::'X'::'X'::ns -> 
            Digit XXXX :: (toRomanDigitListRec ns)
        | 'C'::'C'::'C'::'C'::ns -> 
            Digit CCCC :: (toRomanDigitListRec ns)
        | 'M'::'M'::'M'::'M'::ns -> 
            Digit MMMM :: (toRomanDigitListRec ns)

        // 3 letter matches
        | 'I'::'I'::'I'::ns -> 
            Digit III :: (toRomanDigitListRec ns)
        | 'X'::'X'::'X'::ns -> 
            Digit XXX :: (toRomanDigitListRec ns)
        | 'C'::'C'::'C'::ns -> 
            Digit CCC :: (toRomanDigitListRec ns)
        | 'M'::'M'::'M'::ns -> 
            Digit MMM :: (toRomanDigitListRec ns)

        // 2 letter matches
        | 'I'::'I'::ns -> 
            Digit II :: (toRomanDigitListRec ns)
        | 'X'::'X'::ns -> 
            Digit XX :: (toRomanDigitListRec ns)
        | 'C'::'C'::ns -> 
            Digit CC :: (toRomanDigitListRec ns)
        | 'M'::'M'::ns -> 
            Digit MM :: (toRomanDigitListRec ns)

        | 'I'::'V'::ns -> 
            Digit IV :: (toRomanDigitListRec ns)
        | 'I'::'X'::ns -> 
            Digit IX :: (toRomanDigitListRec ns)
        | 'X'::'L'::ns -> 
            Digit XL :: (toRomanDigitListRec ns)
        | 'X'::'C'::ns -> 
            Digit XC :: (toRomanDigitListRec ns)
        | 'C'::'D'::ns -> 
            Digit CD :: (toRomanDigitListRec ns)
        | 'C'::'M'::ns -> 
            Digit CM :: (toRomanDigitListRec ns)

        // 1 letter matches
        | 'I'::ns -> 
            Digit I :: (toRomanDigitListRec ns)
        | 'V'::ns -> 
            Digit V :: (toRomanDigitListRec ns)
        | 'X'::ns -> 
            Digit X :: (toRomanDigitListRec ns)
        | 'L'::ns -> 
            Digit L :: (toRomanDigitListRec ns)
        | 'C'::ns -> 
            Digit C :: (toRomanDigitListRec ns)
        | 'D'::ns -> 
            Digit D :: (toRomanDigitListRec ns)
        | 'M'::ns -> 
            Digit M :: (toRomanDigitListRec ns)

        // bad letter matches
        | badChar::ns -> 
            BadChar badChar :: (toRomanDigitListRec ns)

        // 0 letter matches
        | [] -> 
            []

    let toRomanDigitList (s:string) = 
        s.ToCharArray() 
        |> List.ofArray 
        |> toRomanDigitListRec

    /// Convert a string to a RomanNumeral
    /// Does not validate the input.E.g. "IVIV" would be valid
    let toRomanNumeral s = 
        toRomanDigitList s
        |> List.choose (
            function 
            | Digit digit -> 
                Some digit 
            | BadChar ch -> 
                eprintfn "%c is not a valid character" ch
                None
            )
        |> RomanNumeral

    // ==========================================
    // Validation logic
    // ==========================================

    // check for validity
    let rec isValidDigitList digitList =
        match digitList with

        // empty list is valid
        | [] -> true

        // a following digit that is equal or larger is an error
        | d1::d2::_ 
            when d1 <= d2  -> 
                false

        // A single digit is always allowed
        | _::ds -> 
            // check the remainder of the list
            isValidDigitList ds 

    // top level check for validity
    let isValid (RomanNumeral digitList) =
        isValidDigitList digitList


```

## Comparing the two versions

Which version did you like better?  The second one is more longwinded because it has many more cases, but on the other hand, the actual logic is the same or simpler in all areas, with no special cases.
And as a result, the total number of lines of code is about the same for both versions.

Overall, I prefer the second implementation because of the lack of special cases.

As a fun experiment, try writing the same code in C# or your favorite imperative language!

## Making it object-oriented

Finally, let's see how we might make this object oriented. We don't care about the helper functions, so we probably just need three methods:

* A static constructor
* A method to convert to a int
* A method to convert to a string

And here they are:

```fsharp
type RomanNumeral with

    static member FromString s = 
        toRomanNumeral s

    member this.ToInt() = 
        toInt this

    override this.ToString() = 
        sprintf "%A" this
```

*Note: you can ignore the compiler warning about deprecated overrides.*

Let's use this in an object oriented way now:

```fsharp
let r = RomanNumeral.FromString "XXIV"
let s = r.ToString()
let i = r.ToInt()
```


## Summary

In this post we've seen lots and lots of pattern matching!

But again, as with the last post, what's equally important is that we've seen how easy it is to create a properly designed internal model for very trivial domains.
And again, our internal model used no primitive types -- there is no excuse not to create lots of little types in order to represent the domain better. For example, the `ParsedChar` type -- would you have bothered to create that in C#?

And as should be clear, the choice of an internal model can make a lot of difference to the complexity of the design. But if and when we do refactor, the compiler will almost always warn us if we have forgotten something.

