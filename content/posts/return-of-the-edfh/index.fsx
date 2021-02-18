
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
module ImplementationsWithNullCheck =

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

open ImplementationsWithNullCheck

// check the updated EdfhImplementation_allChars implementation
do
    let impl = rle_allChars
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

module Debug =
    //>propAdjacentCharactersAreNotSame_debug
    let propAdjacentCharactersAreNotSame (impl:RleImpl) inputStr =
        let output = impl inputStr
        printfn "%s" inputStr
        // etc
    //<
        let actual =
            output
            |> Seq.map fst
            |> Seq.distinct
            |> Seq.toList
        let expected =
            actual
            |> removeDupAdjacentChars // should have no effect
        expected = actual // should be the same
