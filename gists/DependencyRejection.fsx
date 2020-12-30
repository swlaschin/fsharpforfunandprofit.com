(* ===================================
Code from my series of posts "Six approaches to dependency injection"
=================================== *)

open System


(* ======================================================================
2. Dependency Rejection

A great term (coined by Mark Seemann), in which we avoid having *any* dependencies 
in our core business logic code. We do this by keeping all I/O and other impure code 
at the "edges" of our domain
====================================================================== *)

#load "Expecto.fsx"  // from https://gist.github.com/swlaschin/38e21ff8d64ebe4e93e42fd288b486d5

//========================================
// pure implementation with no I/O
//========================================
module PureCore =

    type ComparisonResult =
        | Bigger
        | Smaller
        | Equal

    let compareTwoStrings str1 str2 =
        if str1 > str2 then
            Bigger
        else if str1 < str2 then
            Smaller
        else
            Equal

(*
// It's very easy to unit test a pure function
PureCore.compareTwoStrings "a" "b"
PureCore.compareTwoStrings "a" "a"
PureCore.compareTwoStrings "b" "a"

*)

module PureCore_Test = 
    open Expecto

    let tests = testList "tests" [
        
        testCase "smaller" <| fun () ->
            let expected = PureCore.Smaller
            let actual = PureCore.compareTwoStrings "a" "b"
            Expect.equal actual expected "a < b"

        testCase "equal" <| fun () ->
            let expected = PureCore.Equal
            let actual = PureCore.compareTwoStrings "a" "a"
            Expect.equal actual expected "a = a"

        testCase "bigger" <| fun () ->
            let expected = PureCore.Bigger
            let actual = PureCore.compareTwoStrings "b" "a"
            Expect.equal actual expected "b > a"
        ]

    let runTests() =
        runTest tests

// run the tests
(*
PureCore_Test.runTests() 
*)

//========================================
// implementation of shell/api
//========================================

// The shell/api layer handles the I/O
// and then calls the pure code in PureCore
module Program =
    open PureCore

    let program() =
        // ----------- impure section ----------- 
        printfn "Enter the first value"
        let str1 = Console.ReadLine()
        printfn "Enter the second value"
        let str2 = Console.ReadLine()

        // ----------- pure section ----------- 
        let result = PureCore.compareTwoStrings str1 str2

        // ----------- impure section ----------- 
        match result with
        | Bigger ->
            printfn "The first value is bigger"
        | Smaller ->
            printfn "The first value is smaller"
        | Equal ->
            printfn "The values are equal"

// execute the program
(*
Program.program()
*)


