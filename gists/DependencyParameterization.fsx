(* ===================================
Code from my post "Five approaches to dependency injection"
=================================== *)

open System

#load "Expecto.fsx"  // from https://gist.github.com/swlaschin/38e21ff8d64ebe4e93e42fd288b486d5


(* ======================================================================
3. Dependency Parameterization

Pass in dependencies BEFORE other parameters
====================================================================== *)


//========================================
// A pure implementation with a "strategy" dependency but no I/O dependencies
//========================================

module PureCore =

    type ComparisonResult =
        | Bigger
        | Smaller
        | Equal

    let compareTwoStrings (comparison:StringComparison) str1 str2 =
        // The StringComparison enum lets you pick culture and case-sensitivity options
        let result = String.Compare(str1,str2,comparison) 
        if result > 0 then
            Bigger
        else if result < 0 then
            Smaller
        else    
            Equal
            
    type CompareTwoStrings = string -> string -> ComparisonResult

    // these both have the same type as `CompareTwoStrings` 
    let compareCaseSensitive = compareTwoStrings StringComparison.CurrentCulture
    let compareCaseInsensitive= compareTwoStrings StringComparison.CurrentCultureIgnoreCase


//========================================
// An implementation with the "infrastructure services" passed in as separate parameters
//========================================

module Program_v1 =
    open PureCore

    // "infrastructure services" passed in as parameters
    let compareTwoStrings (readLn:unit->string) (writeLn:string->unit) =
        // ----------- impure section ----------- 
        writeLn "Enter the first value"
        let str1 = readLn()
        writeLn "Enter the second value"
        let str2 = readLn()

        // ----------- pure section ----------- 
        let result = PureCore.compareCaseSensitive str1 str2

        // ----------- impure section ----------- 
        match result with
        | Bigger ->
            writeLn "The first value is bigger"
        | Smaller ->
            writeLn "The first value is smaller"
        | Equal ->
            writeLn "The values are equal"

    // the final code with the "services" passed in
    let program() =
        let readLn() = Console.ReadLine()
        let writeLn str = printfn "%s" str
        // call the parameterized function
        compareTwoStrings readLn writeLn

// run the program
(*
Program_v1.program()
*)

module Program_v1_Test = 

    open Expecto

    // define a mock console for testing
    type MockConsole(inputs:string[]) =
        let mutable readIndex = -1
        let mutable writtenStr = ""

        member __.ReadLn() = 
            readIndex <- readIndex + 1
            inputs.[readIndex] 

        member __.WriteLn str = 
            writtenStr <- str

        member __.WrittenStr = writtenStr

    // setup tests
    let tests = testList "tests" [

        testCase "smaller" <| fun () ->
            let mockConsole = MockConsole [|"a"; "b"|] 
            let readLn,writeLn = mockConsole.ReadLn, mockConsole.WriteLn
            let expected = "The first value is smaller"
            Program_v1.compareTwoStrings readLn writeLn
            let actual = mockConsole.WrittenStr 
            Expect.equal expected actual "a < b"

        testCase "equal" <| fun () ->
            let mockConsole = MockConsole [|"a"; "a"|] 
            let readLn,writeLn = mockConsole.ReadLn, mockConsole.WriteLn
            let expected = "The values are equal"
            Program_v1.compareTwoStrings readLn writeLn
            let actual = mockConsole.WrittenStr 
            Expect.equal expected actual "a = a"

        testCase "bigger" <| fun () ->
            let mockConsole = MockConsole [|"b"; "a"|] 
            let readLn,writeLn = mockConsole.ReadLn, mockConsole.WriteLn
            let expected = "The first value is bigger"
            Program_v1.compareTwoStrings readLn writeLn
            let actual = mockConsole.WrittenStr 
            Expect.equal expected actual "b > a"
        ]

    let runTests() =
        runTest tests

// run the tests
(*
Program_v1_Test.runTests()
*)

    
//========================================
// An implementation with the "infrastructure services" passed in as a single parameter
//========================================

module Program_v2 =
    open PureCore

    /// All the actions combined in a single interface
    type IConsole = 
        abstract ReadLn : unit -> string
        abstract WriteLn : string -> unit 

    // All "infrastructure services" passed in as a single interface
    let compareTwoStrings (console:IConsole)  =
        // ----------- impure section ----------- 
        console.WriteLn "Enter the first value"
        let str1 = console.ReadLn()
        console.WriteLn "Enter the second value"
        let str2 = console.ReadLn()

        // ----------- pure section ----------- 
        let result = PureCore.compareCaseSensitive str1 str2

        // ----------- impure section ----------- 
        match result with
        | Bigger ->
            console.WriteLn "The first value is bigger"
        | Smaller ->
            console.WriteLn "The first value is smaller"
        | Equal ->
            console.WriteLn "The values are equal"

    // the final code with the "services" passed in
    let program() =
        let console = {
            new IConsole with
                member this.ReadLn() = Console.ReadLine()
                member this.WriteLn str = printfn "%s" str
            }
        // call the parameterized function
        compareTwoStrings console

// run the program
(*
Program_v2.program()
*)

//========================================
// How to manage logging? One option is use parameters
//========================================

type ILogger = 
    abstract Debug : string -> unit 
    abstract Info : string -> unit 
    abstract Error : string -> unit 

module LoggingExample =

    open PureCore

    let compareTwoStrings str1 str2 (logger:ILogger) =
        logger.Debug "compareTwoStrings: Starting"
    
        let result =
            if str1 > str2 then
                Bigger
            else if str1 < str2 then
                Smaller
            else
                Equal
        
        logger.Info (sprintf "compareTwoStrings: result=%A" result)

        logger.Debug "compareTwoStrings: Finished"
        result
