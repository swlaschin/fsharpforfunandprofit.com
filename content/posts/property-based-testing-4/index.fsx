

// F# 5 will load a nuget package directly!
#r "nuget:FsCheck"

// If not using F# 5, use nuget to download it using "nuget install FsCheck" or similar
(*
// 1) use "nuget install FsCheck" or similar to download
// 2) include your nuget path here
#I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
// 3) reference the DLL
#r "FsCheck.dll"
*)

open System
open FsCheck


//==============================
// Generating Roman numerals in two different ways
//==============================


//>tally_impl
module TallyImpl =
    let arabicToRoman arabic =
        (String.replicate arabic "I")
            .Replace("IIIII","V")
            .Replace("VV","X")
            .Replace("XXXXX","L")
            .Replace("LL","C")
            .Replace("CCCCC","D")
            .Replace("DD","M")
            // optional substitutions
            .Replace("IIII","IV")
            .Replace("VIV","IX")
            .Replace("XXXX","XL")
            .Replace("LXL","XC")
            .Replace("CCCC","CD")
            .Replace("DCD","CM")
//<

(*
//>tally_test
TallyImpl.arabicToRoman 1    //=> "I"
TallyImpl.arabicToRoman 9    //=> "IX"
TallyImpl.arabicToRoman 24   //=> "XXIV"
TallyImpl.arabicToRoman 999  //=> "CMXCIX"
TallyImpl.arabicToRoman 1493 //=> "MCDXCIII"
//<
*)

//>biqunary_impl
module BiQuinaryImpl =
    let biQuinaryDigits place (unit,five,ten) arabic =
      let digit =  arabic % (10*place) / place
      match digit with
      | 0 -> ""
      | 1 -> unit
      | 2 -> unit + unit
      | 3 -> unit + unit + unit
      | 4 -> unit + five // changed to be one less than five
      | 5 -> five
      | 6 -> five + unit
      | 7 -> five + unit + unit
      | 8 -> five + unit + unit + unit
      | 9 -> unit + ten  // changed to be one less than ten
      | _ -> failwith "Expected 0-9 only"

    let arabicToRoman arabic =
      let units = biQuinaryDigits 1 ("I","V","X") arabic
      let tens = biQuinaryDigits 10 ("X","L","C") arabic
      let hundreds = biQuinaryDigits 100 ("C","D","M") arabic
      let thousands = biQuinaryDigits 1000 ("M","?","?") arabic
      thousands + hundreds + tens + units
    //<

(*
//>biqunary_impl_test
BiQuinaryImpl.arabicToRoman 1    //=> "I"
BiQuinaryImpl.arabicToRoman 9    //=> "IX"
BiQuinaryImpl.arabicToRoman 24   //=> "XXIV"
BiQuinaryImpl.arabicToRoman 999  //=> "CMXCIX"
BiQuinaryImpl.arabicToRoman 1493 //=> "MCDXCIII"
//<
*)

// ==========================================
// Oracle
// ==========================================

//>oracle_prop1
let biquinary_eq_tally number =
    let tallyResult = TallyImpl.arabicToRoman number
    let biquinaryResult = BiQuinaryImpl.arabicToRoman number
    tallyResult = biquinaryResult
//<


//>oracle_prop1_check
Check.Quick biquinary_eq_tally
// ArgumentException: The input must be non-negative.
//<


//>oracle_input
let arabicNumber =
    Arb.Default.Int32()
    |> Arb.filter (fun i -> i > 0 && i <= 4000)
//<

//>oracle_prop2
let biquinary_eq_tally_withinRange =
    Prop.forAll arabicNumber biquinary_eq_tally
//<

//>oracle_prop2_check
Check.Quick biquinary_eq_tally_withinRange
// Ok, passed 100 tests.
//<

// =====================
// Checking the entire domain
// =====================


//>oracle_4000_check
[1..4000] |> List.choose (fun i ->
    if biquinary_eq_tally i then None else Some i
    )
// output => [4000]
//<

//>oracle_4000_error
TallyImpl.arabicToRoman 4000     //=> "MMMM"
BiQuinaryImpl.arabicToRoman 4000 //=> "M?"
//<


module TallyImpl2 =
    //>tally_impl2
    let arabicToRoman arabic =
        if (arabic <= 0 || arabic >= 4000) then
            None
        else
            (String.replicate arabic "I")
                .Replace("IIIII","V")
                .Replace("VV","X")
                // etc
            |> Some
    //<

// =====================
// Checking at boundaries is tricky with PBT
// =====================

// doesn't work as you would expect
do
    //>oracle_prop3_check
    let config = {Config.Quick with EndSize = 4000}
    Check.One(config,biquinary_eq_tally_withinRange )
    // Ok, passed 100 tests.
    //<


// doesn't work as you would expect
do
    //>oracle_prop4_check
    let config = {
        Config.Quick with
            StartSize = 3900
            EndSize = 4000
            MaxTest = 1000
            }
    Check.One(config,biquinary_eq_tally_withinRange)
    // Ok, passed 100 tests.
    //<

// best to check explicitly
do
    //>oracle_boundary_check
    for i in [3999..4001] do
        if not (biquinary_eq_tally i) then
            failwithf "test failed for %i" i
    //<



//==============================
// Decoding and testing with an inverse
//==============================

module TallyDecode =

    //>TallyDecode_impl
    let romanToArabic (str:string) =
        str
            .Replace("CM","DCD")
            .Replace("CD","CCCC")
            .Replace("XC","LXL")
            .Replace("XL","XXXX")
            .Replace("IX","VIV")
            .Replace("IV","IIII")
            .Replace("M","DD")
            .Replace("D","CCCCC")
            .Replace("C","LL")
            .Replace("L","XXXXX")
            .Replace("X","VV")
            .Replace("V","IIIII")
            .Length
    //<

(*
//>TallyDecode_test
TallyDecode.romanToArabic "I"       //=> 1
TallyDecode.romanToArabic "IX"      //=> 9
TallyDecode.romanToArabic "XXIV"    //=> 24
TallyDecode.romanToArabic "CMXCIX"  //=> 999
TallyDecode.romanToArabic "MCDXCIII"//=> 1493
//<
*)


//>inverse_prop
/// encoding then decoding should return
/// the original number
let encodeThenDecode_eq_original =

    // define an inner property
    let innerProp arabic1 =
        let arabic2 =
            arabic1
            |> TallyImpl.arabicToRoman // encode
            |> TallyDecode.romanToArabic // decode
        // should be same
        arabic1 = arabic2

    Prop.forAll arabicNumber innerProp
//<

//>inverse_prop_check
Check.Quick encodeThenDecode_eq_original
// Ok, passed 100 tests.
//<

//>inverse_prop2
/// encoding then decoding should return
/// the original number
let encodeThenDecode_eq_original2 =

    // define an inner property
    let innerProp arabic1 =
        let arabic2 =
            arabic1
            |> BiQuinaryImpl.arabicToRoman // encode
            |> TallyDecode.romanToArabic // decode
        // should be same
        arabic1 = arabic2

    Prop.forAll arabicNumber innerProp
//<

//>inverse_prop2_check
Check.Quick encodeThenDecode_eq_original2
// Ok, passed 100 tests.
//<

//==============================
// Recursively testing
//==============================


/// if we break the number into 1000s, 100s, 10s,
/// and units, and encode them separately, the concat
/// of the components should be the same as encoded directly.
//>recursive_prop
let recursive_prop =

    // define an inner property
    let innerProp arabic =
        let thousands =
            (arabic / 1000 % 10) * 1000
            |> BiQuinaryImpl.arabicToRoman
        let hundreds =
            (arabic / 100 % 10) * 100
            |> BiQuinaryImpl.arabicToRoman
        let tens =
            (arabic / 10 % 10) * 10
            |> BiQuinaryImpl.arabicToRoman
        let units =
            arabic % 10
            |> BiQuinaryImpl.arabicToRoman

        let direct =
            arabic
            |> BiQuinaryImpl.arabicToRoman

        // should be same
        direct = thousands+hundreds+tens+units

    Prop.forAll arabicNumber innerProp
//<

//>recursive_prop_check
Check.Quick recursive_prop
// Ok, passed 100 tests.
//<

//==============================
// Invariants
//==============================

//>matches
let matchesFor pattern input =
    System.Text.RegularExpressions.Regex.Matches(input,pattern).Count

(*
"MMMCXCVIII" |> matchesFor "I"   //=> 3
"MMMCXCVIII" |> matchesFor "XC"  //=> 1
"MMMCXCVIII" |> matchesFor "C"   //=> 2
"MMMCXCVIII" |> matchesFor "M"   //=> 3
*)
//<

//>invariant_prop
let invariant_prop =

    let maxMatchesFor pattern n input =
        (matchesFor pattern input) <= n

    // define an inner property
    let innerProp arabic =
        let roman = arabic |> TallyImpl.arabicToRoman
        (roman |> maxMatchesFor "I" 3)
        && (roman |> maxMatchesFor "V" 1)
        && (roman |> maxMatchesFor "X" 4)
        && (roman |> maxMatchesFor "L" 1)
        && (roman |> maxMatchesFor "C" 4)
        // etc

    Prop.forAll arabicNumber innerProp
//<

//>invariant_prop_check
Check.Quick invariant_prop
// Ok, passed 100 tests.
//<

//==============================
// Commutative
//==============================


//>commutative_prop1
/// Encoding a number less than 400 and then replacing
/// all the characters with the corresponding 10x higher one
/// should be the same as encoding the 10x number directly.
let commutative_prop1 =

    // define an inner property
    let innerProp arabic =
        // take the part < 1000
        let arabic = arabic % 1000
        // encode it
        let result1 =
            (TallyImpl.arabicToRoman arabic)
              .Replace("C","M")
              .Replace("L","D")
              .Replace("X","C")
              .Replace("V","L")
              .Replace("I","X")
        // encode the 10x number
        let result2 =
            TallyImpl.arabicToRoman (arabic * 10)

        // should be same
        result1 = result2

    Prop.forAll arabicNumber innerProp
//<

//>commutative_prop1_check
Check.Quick commutative_prop1
// Ok, passed 100 tests.
//<

//>commutative_prop2
/// Encoding a number and then replacing all the characters with
/// the corresponding 10x lower one should be the same as
/// encoding the 10x lower number directly.
let commutative_prop2 =

    // define an inner property
    let innerProp arabic =
        // encode it
        let result1 =
            (TallyImpl.arabicToRoman arabic)
                .Replace("I","")
                .Replace("V","")
                .Replace("X","I")
                .Replace("L","V")
                .Replace("C","X")
                .Replace("D","L")
                .Replace("M","C")
        // encode the 10x lower number
        let result2 =
            TallyImpl.arabicToRoman (arabic / 10)

        // should be same
        result1 = result2

    Prop.forAll arabicNumber innerProp
//<

//>commutative_prop2_check
Check.Quick commutative_prop2
// Falsifiable, after 9 tests
// 9
//<


