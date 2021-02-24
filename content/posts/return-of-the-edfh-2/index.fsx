#r "nuget:FsCheck"

(*
// If not using F# 5
// 1) use "nuget install FsCheck" or similar to download
// 2) include your nuget path here
#I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
// 3) reference the DLL
#r "FsCheck.dll"
*)

open System
open FsCheck


// ================================================
// classifying the inputs
// ================================================

//>isInterestingString
let isInterestingString inputStr =
    if System.String.IsNullOrEmpty inputStr then
        false
    else
        let distinctChars =
            inputStr
            |> Seq.countBy id
            |> Seq.length
        distinctChars <= (inputStr.Length / 2)
//<

//>isInterestingString_test
isInterestingString ""        //=> false
isInterestingString "aa"      //=> true
isInterestingString "abc"     //=> false
isInterestingString "aabbccc" //=> true
isInterestingString "aabaaac" //=> true
isInterestingString "abcabc"  //=> true (but no runs)
//<

//>propIsInterestingString
let propIsInterestingString input =
    let isInterestingInput = isInterestingString input

    true // we don't care about the actual test
    |> Prop.classify (not isInterestingInput) "not interesting"
    |> Prop.classify isInterestingInput "interesting"
//<

//>propIsInterestingString_check
FsCheck.Check.Quick propIsInterestingString
// Ok, passed 100 tests (100% not interesting).
//<

// ================================================
// generating interesting strings, version 1
// ================================================

//>arbTwoCharString
let arbTwoCharString =
    // helper function to create strings from a list of chars
    let listToString chars =
        chars |> List.toArray |> System.String

    // random lists of 'a's and 'b's
    let genListA = Gen.constant 'a' |> Gen.listOf
    let genListB  = Gen.constant 'b' |> Gen.listOf

    (genListA,genListB)
    ||> Gen.map2 (fun listA listB -> listA @ listB )
    |> Gen.map listToString
    |> Arb.fromGen
//<

//>arbTwoCharString_sample
arbTwoCharString.Generator |> Gen.sample 10 10
(*
[ "aaabbbbbbb"; "aaaaaaaaabb"; "b"; "abbbbbbbbbb";
  "aaabbbb"; "bbbbbb"; "aaaaaaaabbbbbbb";
  "a"; "aabbbb"; "aaaaabbbbbbbbb"]
*)
//<

do
    //>arbTwoCharString_propForAll_check
    // make a new property from the old one, with input from our generator
    let prop = Prop.forAll arbTwoCharString propIsInterestingString
    // check it
    Check.Quick prop

    (*
    Ok, passed 100 tests.
    97% interesting.
    3% not interesting.
    *)
    //<



// ================================================
// Generating interesting strings, part 2
// ================================================

//>flipRandomBits
let flipRandomBits (str:string) = gen {

    // convert input to a mutable array
    let arr = str |> Seq.toArray

    // get a random subset of pixels
    let max = str.Length - 1
    let! indices = Gen.subListOf [0..max]

    // flip them
    for i in indices do arr.[i] <- '1'

    // convert back to a string
    return (System.String arr)
    }
//<

//>arbPixels
let arbPixels =
    gen {
        // randomly choose a length up to 50,
        // and set all pixels to 0
        let! pixelCount = Gen.choose(1,50)
        let image1 = String.replicate pixelCount "0"

        // then flip some pixels
        let! image2 = flipRandomBits image1

        return image2
        }
    |> Arb.fromGen // create a new Arb from the generator
//<

//>arbPixels_test
arbPixels.Generator |> Gen.sample 10 10
(*
"0001001000000000010010010000000";
"00000000000000000000000000000000000000000000100";
"0001111011111011110000011111";
"0101101101111111011010";
"10000010001011000001000001000001101000100100100000";
"0000000000001000";
"00010100000101000001010000100100001010000010100";
"00000000000000000000000000000000000000000";
"0000110101001010010";
"11100000001100011000000000000000001"
*)
//<

do
    //>arbPixels_propForAll_check
    // make a new property from the old one, with input from our generator
    let prop = Prop.forAll arbPixels propIsInterestingString
    // check it
    Check.Quick prop

    (*
    Ok, passed 100 tests.
    94% interesting.
    6% not interesting.
    *)
    //<

// ================================================
// Properties to check: propUsesAllCharacters
// ================================================

//>RleImpl
// A RLE implementation has this signature
type RleImpl = string -> (char*int) list
//<

//>propUsesAllCharacters
let propUsesAllCharacters (impl:RleImpl) inputStr =
    let output = impl inputStr
    let expected =
        if System.String.IsNullOrEmpty inputStr then
            []
        else
            inputStr
            |> Seq.distinct
            |> Seq.toList
    let actual =
        output
        |> Seq.map fst
        |> Seq.distinct
        |> Seq.toList
    expected = actual
//<


// ================================================
// Properties to check: propAdjacentCharactersAreNotSame
// ================================================

//>removeDupAdjacentChars
/// Given a list of elements, remove elements that have the
/// same char as the preceding element.
/// Example:
///   removeDupAdjacentChars ['a';'a';'b';'b';'a'] => ['a'; 'b'; 'a']
let removeDupAdjacentChars charList =
    let folder stack element =
        match stack with
        | [] ->
            // First time? Create the stack
            [element]
        | top::_ ->
            // New element? add it to the stack
            if top <> element then
                element::stack
            // else leave stack alone
            else
                stack

    // Loop over the input, generating a list of non-dup items.
    // These are in reverse order. so reverse the result
    charList |> List.fold folder [] |> List.rev
//<

(*
removeDupAdjacentChars ['a';'a';'b';'b';'a'] // => ['a'; 'b'; 'a']
*)


//>propAdjacentCharactersAreNotSame
let propAdjacentCharactersAreNotSame (impl:RleImpl) inputStr =
    let output = impl inputStr
    let actual =
        output
        |> Seq.map fst
        |> Seq.toList
    let expected =
        actual
        |> removeDupAdjacentChars // should have no effect
    expected = actual // should be the same
//<

(*
propAdjacentCharactersAreNotSame
*)


// ================================================
// Properties to check:
//    The sum of the run lengths in the
//    output must equal the length of the input
// ================================================

//>propRunLengthSum_eq_inputLength
let propRunLengthSum_eq_inputLength (impl:RleImpl) inputStr =
    let output = impl inputStr
    let expected = inputStr.Length
    let actual = output |> List.sumBy snd
    expected = actual // should be the same
//<

// ================================================
// Properties to check:
//  If the input is reversed, the output must also be reversed
// ================================================

//>propInputReversed_implies_outputReversed
/// Helper to reverse strings
let strRev (str:string) =
    str
    |> Seq.rev
    |> Seq.toArray
    |> System.String

let propInputReversed_implies_outputReversed (impl:RleImpl) inputStr =
    // original
    let output1 =
        inputStr |> impl

    // reversed
    let output2 =
        inputStr |> strRev |> impl

    List.rev output1 = output2 // should be the same
//<

// ================================================
// Combining all properties
// ================================================

//>propRle
let propRle (impl:RleImpl) inputStr =
  let prop1 =
    propUsesAllCharacters impl inputStr
    |@ "propUsesAllCharacters"
  let prop2 =
    propAdjacentCharactersAreNotSame impl inputStr
    |@ "propAdjacentCharactersAreNotSame"
  let prop3 =
    propRunLengthSum_eq_inputLength impl inputStr
    |@ "propRunLengthSum_eq_inputLength"
  let prop4 =
    propInputReversed_implies_outputReversed impl inputStr
    |@ "propInputReversed_implies_outputReversed"

  // combine them
  prop1 .&. prop2 .&. prop3 .&. prop4
//<


// ================================================
// EDFH Implementations
// ================================================

//>rle_empty
/// Return an empty list
let rle_empty (inputStr:string) : (char*int) list =
    []
//<

do
    //>rle_empty_check
    let prop = Prop.forAll arbPixels (propRle rle_empty)
    // -- expect to fail on propUsesAllCharacters

    // check it
    Check.Quick prop
    (*
    Falsifiable, after 1 test (0 shrinks)
    Label of failing property: propUsesAllCharacters
    *)
    //<

// ----------------------------------------------

//>rle_allChars
/// Return each char with count 1
let rle_allChars inputStr =
    // add null check
    if System.String.IsNullOrEmpty inputStr then
        []
    else
        inputStr
        |> Seq.toList
        |> List.map (fun ch -> (ch,1))
//<

(*
arbPixels.Generator
|> Gen.sample 10 1
|> List.map rle_allChars
*)

do
    //>rle_allChars_check
    let prop = Prop.forAll arbPixels (propRle rle_allChars)
    // -- expect to fail on propAdjacentCharactersAreNotSame

    // check it
    Check.Quick prop
    (*
    Falsifiable, after 1 test (0 shrinks)
    Label of failing property: propAdjacentCharactersAreNotSame
    *)
    //<

// ----------------------------------------------


//>rle_distinct
let rle_distinct inputStr =
    // add null check
    if System.String.IsNullOrEmpty inputStr then
        []
    else
        inputStr
        |> Seq.distinct
        |> Seq.toList
        |> List.map (fun ch -> (ch,1))
//<

do
    //>rle_distinct_check
    let prop = Prop.forAll arbPixels (propRle rle_distinct)
    // -- expect to fail on propRunLengthSum_eq_inputLength

    // check it
    Check.Quick prop
    (*
    Falsifiable, after 1 test (0 shrinks)
    Label of failing property: propRunLengthSum_eq_inputLength
    *)
    //<

// ----------------------------------------------

//>rle_countBy
let rle_countBy inputStr =
    if System.String.IsNullOrEmpty inputStr then
        []
    else
        inputStr
        |> Seq.countBy id
        |> Seq.toList
//<

do
    //>rle_countBy_check
    let prop = Prop.forAll arbPixels (propRle rle_countBy)
    // -- expect to fail on propInputReversed_implies_outputReversed

    // check it
    Check.Quick prop
    (*
    Falsifiable, after 1 test (0 shrinks)
    Label of failing property: propInputReversed_implies_outputReversed
    *)
    //<


// ================================================
// Correct implementation - recursive
// ================================================

//>rle_recursive
let rle_recursive inputStr =

    // inner recursive function
    let rec loop input =
        match input with
        | [] -> []
        | head::_ ->
            [
            // get a run
            let runLength = List.length (List.takeWhile ((=) head) input)
            // return it
            yield head,runLength
            // skip the run and repeat
            yield! loop (List.skip runLength input)
            ]

    // main
    inputStr |> Seq.toList |> loop
//<

(*
//>rle_recursive_test
rle_recursive "aaaabbbcca"
// [('a', 4); ('b', 3); ('c', 2); ('a', 1)]
//<
*)


do
    //>rle_recursive_check
    let prop = Prop.forAll arbPixels (propRle rle_recursive)
    // -- expect it to not fail

    // check it
    Check.Quick prop
    (*
    Ok, passed 100 tests.
    *)
    //<


// ================================================
// Correct implementation: fold
// ================================================

//>rle_fold
let rle_fold inputStr =
    // This implementation iterates over the list
    // using the 'folder' function and accumulates
    // into 'acc'

    // helper
    let folder (currChar,currCount,acc) inputChar =
        if currChar <> inputChar then
            // push old run onto accumulator
            let acc' = (currChar,currCount) :: acc
            // start new run
            (inputChar,1,acc')
        else
            // same run, so increment count
            (currChar,currCount+1,acc)

    // helper
    let toFinalList (currChar,currCount,acc) =
        // push final run onto acc
        (currChar,currCount) :: acc
        |> List.rev

    // main
    if System.String.IsNullOrEmpty inputStr then
        []
    else
        let head = inputStr.[0]
        let tail = inputStr.[1..inputStr.Length-1]
        let initialState = (head,1,[])
        tail
        |> Seq.fold folder initialState
        |> toFinalList
//<

(*
//>rle_fold_test
rle_fold ""    //=> []
rle_fold "a"   //=> [('a',1)]
rle_fold "aa"  //=> [('a',2)]
rle_fold "ab"  //=> [('a',1); ('b',1)]
rle_fold "aab" //=> [('a',2); ('b',1)]
rle_fold "abb" //=> [('a',1); ('b',2)]
rle_fold "aaaabbbcca"
          //=> [('a',4); ('b',3); ('c',2); ('a',1)]
//<
*)

do
    //>rle_fold_check
    let prop = Prop.forAll arbPixels (propRle rle_fold)
    // -- expect it to not fail

    // check it
    Check.Quick prop
    (*
    Ok, passed 100 tests.
    *)
    //<

