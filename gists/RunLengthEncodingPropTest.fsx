// If not using F# 5, add your nuget path here
// #I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
#I "/Users/swlas/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
#r "FsCheck.dll"

(*
Requirements!

Input: "aaaabbbcca"
Output: [("a", 4), ("b", 3), ("c", 2), ("a", 1)]

Write a function that converts the input to the output
*)

module Implementation_v1 =
    // Simple implementation that meets the spec :)
    let rle inputStr =
        [('a', 4); ('b', 3); ('c', 2); ('a', 1)]

module Implementation_v1a =
    // Simple implementation that meets the spec :)
    let rle (inputStr:string) =
        inputStr |> Seq.countBy id |> Seq.toList

module Implementation_v2 =

    // correct implementation that meets the spec :)
    let rle inputStr =
        let folder (currChar,currCount,prevCounts,firstTime) inputChar =
            if firstTime then
                (inputChar,1,[],false)
            else if currChar <> inputChar then
                // start new sequence
                let prevCounts' = (currChar,currCount) :: prevCounts
                (inputChar,1,prevCounts',firstTime)
            else
                // same char, so increment
                (currChar,currCount+1,prevCounts,firstTime)

        let toFinalList (currChar,currCount,prevCounts,firstTime) =
            if firstTime then
                // input was empty
                []
            else
                (currChar,currCount) :: prevCounts
                |> List.rev

        let initialState = ('\000',0,[],true)

        // process a string
        inputStr
        |> Seq.fold folder initialState
        |> toFinalList

module Implementation_v2a =

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
        inputStr
        |> Seq.toList
        |> function
            | [] -> []
            | head::tail ->
                let initialState = (head,1,[])
                tail
                |> Seq.fold folder initialState
                |> toFinalList



// test passes!
Implementation_v1.rle "aaaabbbcca"

Implementation_v1a.rle "aaaabbbcca"
Implementation_v1a.rle ""

Implementation_v2.rle "aaaabbbcca"
Implementation_v2.rle ""

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
