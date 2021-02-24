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
let propInputReversed_implies_outputReversed (impl:RleImpl) inputStr =
    // original
    let output1 = impl inputStr

    // reversed
    let reversedInput = inputStr |> Seq.rev |> Seq.toArray |> System.String
    let output2 = impl reversedInput

    List.rev output1 = output2 // should be the same
//<

// ================================================
// Combining all properties
// ================================================

module PropRle_v1 =
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
      prop1 .&. prop2 .&. prop3 .&. prop4
    //<

open PropRle_v1

// ================================================
// Generator for interesting strings
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


// ================================================
// Correct implementation
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

// ================================================
// EDFH Implementation
// ================================================


//>rle_corrupted
/// An incorrect implementation that satisfies all the properties
let rle_corrupted (inputStr:string) : (char*int) list =

    // helper
    let duplicateFirstTwoChars list =
        match list with
        | (ch1,n)::(ch2,m)::e3::e4::tail when n > 1 && m > 1 ->
            (ch1,1)::(ch2,1)::(ch1,n-1)::(ch2,m-1)::e3::e4::tail
        | _ ->
            list

    // start with correct output...
    let output = rle_recursive inputStr

    // ...and then corrupt it by
    // adding extra chars front and back
    output
    |> duplicateFirstTwoChars
    |> List.rev
    |> duplicateFirstTwoChars
    |> List.rev
//<

do
    //>rle_corrupted_check
    let prop = Prop.forAll arbPixels (propRle rle_corrupted)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 100 tests.
    //<

(*
rle_corrupted "11000"
rle_corrupted "00011" |> List.rev
*)

// ================================================
// Structure-preserving properties
// ================================================

(*
//>addingRles
// wrong
['a',1] + ['a',1]  //=> [('a',1); ('a',1)]
// correct
['a',1] + ['a',1]  //=> [('a',2)]
//<
*)

module RleAdd =
    //>rleAdd
    // A Rle is a list of chars and run-lengths
    type Rle = (char*int) list

    let rec rleConcat (rle1:Rle) (rle2:Rle) =
        match rle1 with
        // 0 elements, so return rle2
        | [] -> rle2

        // 1 element left, so compare with
        // first element of rle2 and merge if equal
        | [ (x,xCount) ] ->
            match rle2 with
            | [] ->
                rle1
            | (y,yCount)::tail ->
                if x = y then
                    // merge
                    (x,(xCount+yCount)) :: tail
                else
                    (x,xCount)::(y,yCount)::tail

        // longer than 1, so recurse
        | head::tail ->
            head :: (rleConcat tail rle2)
    //<

open RleAdd

//>rleAdd_test
rleConcat ['a',1] ['a',1]  //=> [('a',2)]
rleConcat ['a',1] ['b',1]  //=> [('a',1); ('b',1)]

let rle1 = rle_recursive "aaabb"
let rle2 = rle_recursive "bccc"
let rle3 = rle_recursive ("aaabb" + "bccc")
rle3 = rleConcat rle1 rle2   //=> true
//<

//>propStructurePreserving
let propConcat (impl:RleImpl) (str1,str2) =
    let ( <+> ) = rleConcat

    let rle1 = impl str1
    let rle2 = impl str2
    let actual = rle1 <+> rle2
    let expected = impl (str1 + str2)
    actual = expected
//<

//>arbPixelsPair
let arbPixelsPair =
    arbPixels.Generator
    |> Gen.two
    |> Arb.fromGen
//<

do
    //>propStructurePreserving_check_corrupted
    let prop = Prop.forAll arbPixelsPair (propConcat rle_corrupted)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Falsifiable, after 2 tests
    //<

do
    //>propStructurePreserving_check_recursive
    let prop = Prop.forAll arbPixelsPair (propConcat rle_recursive)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 10000 tests.
    //<


// ================================================
// Combining all properties
// ================================================

module PropRle_v2 =
    //>propRle_v2
    let propRle (impl:RleImpl) =
      let prop1 =
        Prop.forAll arbPixels (propUsesAllCharacters impl)
        |@ "propUsesAllCharacters"
      let prop2 =
        Prop.forAll arbPixels (propAdjacentCharactersAreNotSame impl)
        |@ "propAdjacentCharactersAreNotSame"
      let prop3 =
        Prop.forAll arbPixels (propRunLengthSum_eq_inputLength impl)
        |@ "propRunLengthSum_eq_inputLength"
      let prop4 =
        Prop.forAll arbPixelsPair (propConcat impl)
        |@ "propConcat"
      prop1 .&. prop2 .&. prop3 .&. prop4
    //<

open PropRle_v2

do
    //>propRle2_check_corrupted
    let prop = propRle rle_corrupted

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Falsifiable, after 2 tests
    // Label of failing property: propConcat
    //<

do
    //>propRle2_check_recursive
    let prop = propRle rle_recursive

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 10000 tests.
    //<

// ================================================
// Decoder
// ================================================

//>typeRle
type Rle = Rle of (char*int) list
//<

//>decode
let decode (Rle rle) : string =
    let sb = System.Text.StringBuilder()
    for (ch,count) in rle do
        for _ in [1..count] do
            sb.Append(ch) |> ignore
    sb.ToString()
//<

//>decode_test
rle_recursive "111000011"
|> Rle
|> decode  //=> "111000011"
//<

// ================================================
// propEncodeDecode
// ================================================

//>propEncodeDecode
let propEncodeDecode (encode:RleImpl) inputStr =
    let actual =
        inputStr
        |> encode
        |> Rle  // RleImpl doesn't return a Rle yet
        |> decode

    actual = inputStr
//<

do
    //>propEncodeDecode_check_corrupted
    let prop = Prop.forAll arbPixels (propEncodeDecode rle_corrupted)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Falsifiable, after 2 tests
    //<


do
    //>propEncodeDecode_check_recursive
    let prop = Prop.forAll arbPixels (propEncodeDecode rle_recursive)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 10000 tests.
    //<

// ================================================
// Defining the RLE specification
// ================================================

do
    //>rle_allChars_check
    /// a very simple RLE implementation
    let rle_allChars inputStr =
      inputStr
      |> Seq.toList
      |> List.map (fun ch -> (ch,1))

    // make a property 
    let prop = Prop.forAll arbPixels (propEncodeDecode rle_allChars)

    // and check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 10000 tests.
    //<

// ================================================
// Bonus: Going in the other direction
// ================================================

//>propDecodeEncode
let propDecodeEncode (encode:RleImpl) rle =
    let actual =
        rle
        |> decode
        |> encode
        |> Rle

    actual = rle
//<

do
    //>propDecodeEncode_check_corrupted
    let prop = propDecodeEncode rle_corrupted
    Check.Quick(prop)
    // Falsifiable, after 4 tests
    // Rle [('a', 0)]
    //<

    //>propDecodeEncode_check_recursive
    let prop = propDecodeEncode rle_recursive
    Check.Quick(prop)
    // Falsifiable, after 4 tests
    // Rle [('a', 0)]
    //<

// ================================================
// Observing interesting RLE
// ================================================

//>isInterestingRle
let isInterestingRle (Rle rle) =
    let isLongList = rle.Length > 2
    let noOflongRuns =
        rle
        |> List.filter (fun (_,run) -> run > 2)
        |> List.length
    isLongList && (noOflongRuns > 2)
//<

//>propIsInterestingRle
let propIsInterestingRle input =
    let isInterestingInput = isInterestingRle input

    true // we don't care about the actual test
    |> Prop.classify (not isInterestingInput) "not interesting"
    |> Prop.classify isInterestingInput "interesting"
//<

//>propIsInterestingRle_check
Check.Quick propIsInterestingRle
// Ok, passed 100 tests.
// 99% not interesting.
// 1% interesting.
//<

// ================================================
// Generating interesting RLE, attempt #1
// ================================================

// This is an incorrect implementation! Read on for why
module ArbRle_incorrect =
    //>arbRle_incorrect
    let arbRle =
        let genChar = Gen.elements ['a'..'z']
        let genRunLength = Gen.choose(1,10)
        Gen.zip genChar genRunLength
        |> Gen.listOf
        |> Gen.map Rle
        |> Arb.fromGen
    //<

open ArbRle_incorrect
do
    //>arbRle_incorrect_check
    let prop = Prop.forAll arbRle propIsInterestingRle
    Check.Quick prop
    // Ok, passed 100 tests.
    // 86% interesting.
    // 14% not interesting.
    //<



do
    //>propDecodeEncode2_check_recursive
    let prop = Prop.forAll arbRle (propDecodeEncode rle_recursive)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Falsifiable, after 82 tests
    // Rle [('e', 7); ('e', 6); ('z', 10)]
    //<

// ================================================
// Generating interesting RLE, attempt #2, with adjacent removed
// ================================================

//>removeAdjacentRuns
let removeAdjacentRuns pairList =
    let folder pairs newPair =
        match pairs with
        | [] -> [newPair]
        | head::tail ->
            if fst head <> fst newPair then
                newPair::pairs
            else
                pairs
    pairList
    |> List.fold folder []
    |> List.rev
//<

//>arbRle
let arbRle =
    let genRunLength = Gen.choose(1,10)
    let genChar = Gen.elements ['a'..'z']
    Gen.zip genChar genRunLength
    |> Gen.listOf
    |> Gen.map removeAdjacentRuns
    |> Gen.map Rle
    |> Arb.fromGen
//<


do
    //>propDecodeEncode3_check_recursive
    let prop = Prop.forAll arbRle (propDecodeEncode rle_recursive)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 10000 tests.
    //<


// ================================================
// Monoid properties
// ================================================

//>propAssociative
let propAssociative (impl:RleImpl) (str1,str2,str3) =
    let ( <+> ) = rleConcat

    let rle1 = impl str1
    let rle2 = impl str2
    let rle3 = impl str3
    let rleLeft = (rle1 <+> rle2) <+> rle3
    let rleRight = rle1 <+> (rle2 <+> rle3)
    rleLeft = rleRight
//<

//>arbPixelsTriple
let arbPixelsTriple =
    arbPixels.Generator
    |> Gen.three
    |> Arb.fromGen
//<

do
    //>propAssociative_check_corrupted
    let prop = Prop.forAll arbPixelsTriple (propAssociative rle_corrupted)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 100 tests.
    //<

do
    //>propAssociative_check_recursive
    let prop = Prop.forAll arbPixelsTriple (propAssociative rle_recursive)

    // check it
    Check.Quick prop
    // Ok, passed 100 tests.
    //<


//>propLeftZero
let propLeftZero (impl:RleImpl) str =
    let ( <+> ) = rleConcat
    let rleZero = []

    let rle = impl str
    let rleLeft = rleZero <+> rle
    rleLeft = rle
//<

do
    //>propLeftZero_check_corrupted
    let prop = Prop.forAll arbPixels (propLeftZero rle_corrupted)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 100 tests.
    //<

do
    //>propLeftZero_check_recursive
    let prop = Prop.forAll arbPixels (propLeftZero rle_recursive)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 100 tests.
    //<


//>propRightZero
let propRightZero (impl:RleImpl) str =
    let ( <+> ) = rleConcat
    let rleZero = []

    let rle = impl str
    let rleRight = rle <+> rleZero
    rleRight = rle
//<

do
    //>propLeftZero_check_corrupted
    let prop = Prop.forAll arbPixels (propRightZero rle_corrupted)

    // check it thoroughly
    let config = { Config.Default with MaxTest=10000}
    Check.One(config,prop)
    // Ok, passed 100 tests.
    //<

do
    //>propLeftZero_check_recursive
    let prop = Prop.forAll arbPixels (propRightZero rle_recursive)

    // check it
    Check.Quick prop
    // Ok, passed 100 tests.
    //<


// ================================================
// Registering an Arbitrary
// ================================================


//>MyGenerators
type MyGenerators =
    static member Rle() = arbRle

    // static member MyCustomType() = arbMyCustomType
//<


//>registerMyGenerators
Arb.register<MyGenerators>()
//<

//>sampleMyGenerators
Arb.generate<Rle> |> Gen.sample 5 4
// [Rle [('c', 2); ('m', 8)];
//  Rle [];
//  Rle [('e', 7); ('c', 2); ('s', 1); ('m', 8)];
//  Rle [('t', 3); ('e', 7); ('c', 2)]]
//<


//>propDecodeEncode_check_registered
let prop = propDecodeEncode rle_recursive

Check.Quick(prop)
// Ok, passed 100 tests.
//<