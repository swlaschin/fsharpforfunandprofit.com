
// ================================================
// Part 1: The return of the EDFH
// ================================================

(*
From https://twitter.com/allenholub/status/1357115515672555520

Most candidates cannot solve this interview problem:

* Input: "aaaabbbcca"
* Output: [("a", 4), ("b", 3), ("c", 2), ("a", 1)]

Write a function that converts the input to the output.

I ask it in the screening interview and give it 25 minutes.

How would you solve it?

----

def func(x):
   return [("a", 4), ("b", 3), ("c", 2), ("a", 1)]

*)

/// EFDH implementation that meets the spec :)
module EdfhImplementation_v1 =
    //>efdh1
    let func inputStr =
        [('a',4); ('b',3); ('c',2); ('a',1)]
    //<

(*
// test
open EdfhImplementation_v1
func "aaaabbbcca"    //=> [('a',4); ('b',3); ('c',2); ('a',1)]
*)

module EdfhImplementation_v2 =
    //>efdh2
    let rle inputStr =
        match inputStr with
        | "" ->
            []
        | "a" ->
            [('a',1)]
        | "aab" ->
            [('a',2); ('b',1)]
        | "aaaabbbcca" ->
            [('a',4); ('b',3); ('c',2); ('a',1)]
        // everything else
        | _ -> []
    //<
(*
// test
open EdfhImplementation_v2
//>efdh2_test
rle "a"           //=> [('a',1);]
rle "aab"         //=> [('a',2); ('b',1)]
rle "aaaabbbcca"  //=> [('a',4); ('b',3); ('c',2); ('a',1)]
//<
*)

// -----------------------------------------------
// Using EDFH implementations to help think of properties
// -----------------------------------------------=


// EFDH implementation that returns empty list
//>rle_empty
let rle_empty (inputStr:string) : (char*int) list =
    []
//<

(*
// test
rle_empty "aaaabbbcca"     //=> []
*)

// EFDH implementation that returns all the characters
//>rle_allChars
let rle_allChars inputStr =
    inputStr
    |> Seq.toList
    |> List.map (fun ch -> (ch,1))
//<

(*
// test
//>rle_allChars_test
rle_allChars ""      //=> []
rle_allChars "a"     //=> [('a',1)]
rle_allChars "abc"   //=> [('a',1); ('b',1); ('c',1)]
rle_allChars "aab"   //=> [('a',1); ('a',1); ('b',1)]
//<
*)

// EFDH implementation that returns all the distinct characters
//>rle_distinct
let rle_distinct inputStr =
    inputStr
    |> Seq.distinct
    |> Seq.toList
    |> List.map (fun ch -> (ch,1))
//<

(*
// test
//>rle_distinct_test
rle_distinct "a"     //=> [('a',1)]
rle_distinct "aab"   //=> [('a',1); ('b',1))]
rle_distinct "aaabb" //=> [('a',1); ('b',1))]
//<
*)


// EFDH implementation that returns all the characters AND the counts add up
//>rle_groupedCount
let rle_groupedCount inputStr =
    inputStr
    |> Seq.countBy id
    |> Seq.toList
//<

(*
// test
//>rle_groupedCount_test
rle_groupedCount "aab"         //=> [('a',2); ('b',1))]
rle_groupedCount "aaabb"       //=> [('a',3); ('b',3))]
rle_groupedCount "aaaabbbcca"  //=> [('a',5); ('b',3); ('c',2))]
//<
*)

// What we wanted:
//>rle_groupedCount_1
[('a',4); ('b',3); ('c',2); ('a',1)]
//<

// What we got:
//>rle_groupedCount_2
[('a',5); ('b',3); ('c',2)]
//    ^ wrong              ^ another entry needed here
//<


// ================================================
// Part 2: Using FsCheck
// ================================================

// F# 5 will load a nuget package directly!
//>nugetFsCheck
#r "nuget:FsCheck"
//<

(*
// If not using F# 5, use nuget to download it using "nuget install FsCheck" or similar
// then add your nuget path here
#I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
#r "FsCheck.dll"
*)

//>propUsesAllCharacters
// An RLE implementation has this signature
type RleImpl = string -> (char*int) list

let propUsesAllCharacters (impl:RleImpl) inputStr =
    let output = impl inputStr
    let expected =
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

// -----------------------------------------------
// check the rle_empty implementation
// -----------------------------------------------

do
    //>rle_empty_proptest
    let impl = rle_empty
    let prop = propUsesAllCharacters impl
    FsCheck.Check.Quick prop
    //<

(*
//>rle_empty_proptest_result
Falsifiable, after 1 test (1 shrink) (StdGen (777291017, 296855223)):
Original:
"#"
Shrunk:
"a"
//<
*)

// -----------------------------------------------
// check the rle_allChars implementation
// -----------------------------------------------

do
    //>rle_allChars_proptest
    let impl = rle_allChars
    let prop = propUsesAllCharacters impl
    FsCheck.Check.Quick prop
    //<

(*
//>rle_allChars_proptest_result
Falsifiable, after 1 test (0 shrinks) (StdGen (153990125, 296855225)):
Original:
<null>
with exception:
System.ArgumentNullException: Value cannot be null.
//<
*)


// lets fix up those implementations
module EdfhImplementationsWithNullCheck =

    //>rle_allChars_fixed
    let rle_allChars inputStr =
        if System.String.IsNullOrEmpty inputStr then
            []
        else
            inputStr
            |> Seq.toList
            |> List.map (fun ch -> (ch,1))
    //<

    let rle_distinct inputStr =
        if System.String.IsNullOrEmpty inputStr then
            []
        else
            inputStr
            |> Seq.distinct
            |> Seq.toList
            |> List.map (fun ch -> (ch,1))


    let rle_countBy inputStr =
        if System.String.IsNullOrEmpty inputStr then
            []
        else
            inputStr
            |> Seq.countBy id
            |> Seq.toList

// corrected version of property that handles nulls
//>propUsesAllCharacters_fixed
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

// check the updated EdfhImplementation_allChars implementation
do
    let impl = EdfhImplementationsWithNullCheck.rle_allChars
    let prop = propUsesAllCharacters impl
    FsCheck.Check.Quick prop


// -----------------------------------------------
// Create the "Adjacent characters are not the same" property
// -----------------------------------------------

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

    // loop over the input, generating a new list (in reverse order)
    // then reverse the result
    charList |> List.fold folder [] |> List.rev
//<

(*
// test
removeDupAdjacentChars ['a';'a';'b';'b';'a']
// => ['a'; 'b'; 'a']
*)

//>propAdjacentCharactersAreNotSame
/// Property: "Adjacent characters in the output cannot be the same"
let propAdjacentCharactersAreNotSame (impl:RleImpl) inputStr =
    let output = impl inputStr
    let actual =
        output
        |> Seq.map fst
        |> Seq.distinct
        |> Seq.toList
    let expected =
        actual
        |> removeDupAdjacentChars // should have no effect
    expected = actual // should be the same
//<

open EdfhImplementationsWithNullCheck

// check the updated EdfhImplementation_allChars implementation
// This passes, but it shouldn't :(
do
    //>propAdjacentCharactersAreNotSame_rle_allChars
    let impl = rle_allChars
    let prop = propAdjacentCharactersAreNotSame impl
    FsCheck.Check.Quick prop
    //<

// try again with more runs
// This passes, but it shouldn't :(
do
    //>propAdjacentCharactersAreNotSame_rle_allChars_10000
    let impl = rle_allChars
    let prop = propAdjacentCharactersAreNotSame impl
    let config = {FsCheck.Config.Default with MaxTest = 10000}
    FsCheck.Check.One(config,prop)
    //<

//>propAdjacentCharactersAreNotSame_debug
let propAdjacentCharactersAreNotSame (impl:RleImpl) inputStr =
    let output = impl inputStr
    printfn "%s" inputStr
    // etc
//<


// ================================================
// Part 3: Generating and analyzing inputs
// ================================================






/// correct implementation
module GoodImplementation_v1 =

    let rle inputStr =
        let folder (currChar,currCount,prevCounts,firstTime) inputChar =
            if firstTime then
                // Start from scratch
                (inputChar,1,[],false)
            else if currChar <> inputChar then
                // Start new sequence:
                // prepend current count onto prev count
                let prevCounts' = (currChar,currCount) :: prevCounts
                (inputChar,1,prevCounts',firstTime)
            else
                // Same char, so just increment currCount
                (currChar,currCount+1,prevCounts,firstTime)

        let toFinalList (currChar,currCount,prevCounts,firstTime) =
            if firstTime then
                // input was empty
                []
            else
                // prepend the final count onto prev count
                (currChar,currCount) :: prevCounts
                |> List.rev

        // the initial char could be anything. I'm using NUL
        let initialState = ('\000',0,[],true)

        // process a string
        inputStr
        |> Seq.fold folder initialState
        |> toFinalList

(*
// test
GoodImplementation_v1.rle "aaaabbbcca"
// [('a', 4); ('b', 3); ('c', 2); ('a', 1)]
*)

/// another correct implementation with the special case
/// empty strings removed from "folder" function
module GoodImplementation_v2 =

    // correct implementation that meets the spec :)
    let rle inputStr =

        let folder (currChar,currCount,prevCounts) inputChar =
            if currChar <> inputChar then
                // start new sequence
                let prevCounts' = (currChar,currCount) :: prevCounts
                (inputChar,1,prevCounts')
            else
                // same char, so increment
                (currChar,currCount+1,prevCounts)

        let toFinalList (currChar,currCount,prevCounts) =
            (currChar,currCount) :: prevCounts
            |> List.rev

        // process a string
        if System.String.IsNullOrEmpty inputStr then
            []
        else
            let head = inputStr.[0]
            let tail = inputStr.[1..inputStr.Length-1]
            let initialState = (head,1,[])
            tail
            |> Seq.fold folder initialState
            |> toFinalList

(*
// test
GoodImplementation_v2.rle "aaaabbbcca"
// [('a', 4); ('b', 3); ('c', 2); ('a', 1)]

GoodImplementation_v2.rle ""
GoodImplementation_v2.rle "a"
GoodImplementation_v2.rle "aa"
GoodImplementation_v2.rle "ab"
GoodImplementation_v2.rle "aab"
GoodImplementation_v2.rle "abb"
*)

module GoodImplementation_v3 =

    let rle inputStr =
        let rec loop input =
            match input with
            | [] -> []
            | head::_ ->
                [
                let runLength = List.length (List.takeWhile ((=) head) input)
                yield head,runLength
                yield! loop (List.skip runLength input)
                ]
        inputStr |> Seq.toList |> loop

(*
// test
GoodImplementation_v3.rle "aaaabbbcca"
// [('a', 4); ('b', 3); ('c', 2); ('a', 1)]
*)


let removeAdjacent1 charList =
    let folder chars newChar =
        match chars with
        | [] -> [newChar]
        | head::tail ->
            if head <> newChar then
                newChar::chars
            else
                chars
    charList |> List.fold folder [] |> List.rev

let removeAdjacent pairList =
    let folder pairs newPair =
        match pairs with
        | [] -> [newPair]
        | head::tail ->
            if fst head <> fst newPair then
                newPair::pairs
            else
                pairs
    pairList |> List.fold folder [] |> List.rev

// test
removeAdjacent1 (Seq.toList "aabbcdeea")
removeAdjacent ['C',1; 'C',2]

// property based test that tries to reproduce the expected output
open FsCheck

let isInteresting (charAndLens:(char * PositiveInt) list) =
    let isLongList = charAndLens.Length > 3
    let hasLongRuns =
        (charAndLens
        |> List.filter (fun (_,PositiveInt run) -> run > 2)
        |> List.length)
         > 2
    isLongList && hasLongRuns

let prop1 (charAndLens:(char * PositiveInt) list) =
    let expected =
        charAndLens
        |> List.map (fun (ch,PositiveInt len) -> (ch,len))
        |> removeAdjacent
        |> List.filter (fun pair -> fst pair <> '\000')

    let inputStr =
        expected
        |> List.map (fun (ch,len) -> Array.replicate len ch |> System.String)
        |> String.concat ""
    let actual = Implementation_v2.rle inputStr

    (actual = expected)
    |> Prop.ofTestable
    |> Prop.classify (charAndLens.Length = 0) "0-length list"
    |> Prop.classify (charAndLens.Length = 1) "1-length list"
    |> Prop.classify (isInteresting charAndLens) "interesting list"

FsCheck.Check.Quick prop1
(*
Ok, passed 100 tests.
11% interesting.
8% 1.
7% 0.
*)

// FsCheck provides several ways to observe the distribution of test data.
// https://fscheck.github.io/FsCheck/Properties.html

// ==================================

type RleList = RleList of (char * PositiveInt) list

let rleListToString (RleList pairs) =
    let strFromChar (ch,PositiveInt len) =
        Array.replicate len ch |> System.String
    pairs |> List.map  strFromChar |> String.concat ""

let rleListStats (RleList pairs) =
    pairs
    |> List.countBy snd
    //|> List.map snd
    |> List.sort
    |> List.map (fun (pi,count) -> sprintf "%i runs %A long" count pi)


let genRleListSimple =
    Gen.zip Arb.generate<char> Arb.generate<PositiveInt>
    |> Gen.nonEmptyListOf
    |> Gen.map RleList

let genRleListComplex =
    let runSize = Gen.choose(2,5) |> Gen.map PositiveInt
    Gen.zip Arb.generate<char> runSize
    |> Gen.nonEmptyListOf
    |> Gen.map RleList


type MyGenerators1 =
  static member RleList() =
      {new Arbitrary<RleList>() with
          //override x.Generator = genRleListSimple
          override x.Generator = genRleListComplex
          override x.Shrinker t = Seq.empty }

Arb.register<MyGenerators1>()

Arb.generate<RleList>
|> Gen.sample 10 1

Arb.generate<RleList>
|> Gen.map rleListStats
|> Gen.sample 10 10

Arb.generate<RleList>
|> Gen.map (fun (RleList pairs) -> isInteresting pairs)
|> Gen.sample 50 100
|> List.countBy id

let prop1a (RleList charAndLens) =
    prop1 charAndLens

FsCheck.Check.Quick prop1a
(*
Ok, passed 100 tests.
40% interesting.
1% 1.
*)


// =====================================


type RleString = RleString of string

let flipRandomBits (str:string) = gen {
    let max = str.Length - 1
    let! indices = Gen.subListOf [0..max]
    let arr = str |> Seq.toArray
    for i in indices do
       arr.[i] <- '1'
    return (System.String arr)
    }


let genRleString =
    gen {
        let! max = Gen.choose(1,100)
        let str = String.replicate max "0"
        let! str' = flipRandomBits str
        return (RleString str')
    }


type MyGenerators2 =
  static member RleString() =
    let shrink (RleString str) = Arb.shrink str |> Seq.map RleString
    {new Arbitrary<RleString>() with
          override x.Generator = genRleString
          override x.Shrinker t = shrink t  }

Arb.register<MyGenerators2>()

Arb.generate<RleString>
|> Gen.sample 10 1
|> List.map (fun (RleString str) -> Implementation_v2.rle str)


let prop_all_chars impl (RleString inputStr) =
    let expected = inputStr |> Set.ofSeq
    let actual =
        inputStr
        |> impl
        |> List.map fst
        |> Set.ofSeq
    actual = expected

let prop_sameCount impl (RleString inputStr) =
    let expected = inputStr |> String.length
    let actual =
        inputStr
        |> impl
        |> List.sumBy snd
    actual = expected

let prop_reverse impl (RleString inputStr) =
    let revAfter = inputStr |> impl |> List.rev
    let revBefore = inputStr |> Seq.toArray |> Array.rev |> System.String |> impl
    revAfter = revBefore

FsCheck.Check.Quick (prop_all_chars Implementation_v1.rle)

FsCheck.Check.Quick (prop_all_chars Implementation_v1a.rle)
FsCheck.Check.Quick (prop_sameCount Implementation_v1a.rle)
FsCheck.Check.Quick (prop_reverse Implementation_v1a.rle)

FsCheck.Check.Quick (prop_all_chars Implementation_v2.rle)
FsCheck.Check.Quick (prop_sameCount Implementation_v2.rle)
FsCheck.Check.Quick (prop_reverse Implementation_v2.rle)
