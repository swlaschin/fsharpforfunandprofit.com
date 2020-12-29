(* ===================================
Code from my post "Five approaches to dependency injection"
=================================== *)

open System


(* ======================================================================
4. Dependency Injection

Pass in dependencies AFTER other parameters
====================================================================== *)

type ILogger = 
    abstract Debug : string -> unit 
    abstract Info : string -> unit 
    abstract Error : string -> unit 

let defaultLogger = {new ILogger with
    member __.Debug str = printfn "DEBUG %s" str
    member __.Info str = printfn "INFO %s" str
    member __.Error str = printfn "ERROR %s" str
    }

type IConsole = 
    abstract ReadLn : unit -> string
    abstract WriteLn : string -> unit 

let defaultConsole = {new IConsole with
    member __.ReadLn() = Console.ReadLine()
    member __.WriteLn str = printfn "%s" str
    }

type ComparisonResult =
    | Bigger
    | Smaller
    | Equal


//========================================
// Class with constructor injection
//========================================

module OODependencyInjection =
    
    // "infrastructure services" passed in via the constructor
    type StringComparisons(logger:ILogger) =

        member __.CompareTwoStrings str1 str2  =
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

    // create the logger
    let logger : ILogger = defaultLogger
    // construct the class
    let stringComparisons = StringComparisons logger 
    // call the method
    stringComparisons.CompareTwoStrings "a" "b"

//========================================
// logging using a dependency parameter in last place
//========================================

module FPInjection_DependencyInLastPlace =

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

//========================================
// returning a function 
//========================================

module FPInjection_InterpretedAsReturningAFunction =

    let compareTwoStrings str1 str2 =
        fun (logger:ILogger) ->
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

// ==================================================
// Reader monad
// ==================================================


type Reader<'env,'a> = Reader of action:('env -> 'a)

module Reader =
    /// Run a Reader with a given environment
    let run env (Reader action)  = 
        action env  // simply call the inner function

    /// Create a Reader which returns the environment itself
    let ask = Reader id 

    /// Map a function over a Reader 
    let map f reader = 
        Reader (fun env -> f (run env reader))

    /// flatMap a function over a Reader 
    let bind f reader =
        let newAction env =
            let x = run env reader 
            run env (f x)
        Reader newAction

    /// Transform a Reader's environment.
    /// Known as `withReader` in Haskell
    let withEnv (f:'env2->'env1) reader = 
        Reader (fun env' -> (run (f env') reader))


type ReaderBuilder() =
    member __.Return(x) = Reader (fun _ -> x)
    member __.Bind(x,f) = Reader.bind f x
    member __.Zero() = Reader (fun _ -> ())

// the builder instance
let reader = ReaderBuilder()

module FPInjection_ReaderMonad =

    let compareTwoStrings str1 str2 : Reader<ILogger,ComparisonResult> =
        fun (logger:ILogger) ->
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
        |> Reader // <------------------ NEW!!!


module FPInjection_ReaderComputationExpression =

    let compareTwoStrings str1 str2  =
        reader {
            let! (logger:ILogger) = Reader.ask
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
            return result 
            }

    let readFromConsole_bad() = 
        reader {
            let! (console:IConsole) = Reader.ask
            // let! (logger:ILogger) = Reader.ask // error

            console.WriteLn "Enter the first value"
            let str1 = console.ReadLn()
            console.WriteLn "Enter the second value"
            let str2 = console.ReadLn()

            return str1,str2
            }



(* ======================================================================
Like any monad, Readers can be chained, composed, etc

Approach 1 - using inheritance
====================================================================== *)

module ReaderComposition_v1 = 

    let readFromConsole() = 
        reader {
            let! (console:#IConsole) = Reader.ask
            let! (logger:#ILogger) = Reader.ask  // OK

            console.WriteLn "Enter the first value"
            let str1 = console.ReadLn()
            console.WriteLn "Enter the second value"
            let str2 = console.ReadLn()

            return str1,str2
            }

    let compareTwoStrings str1 str2  =
        reader {
            let! (logger:#ILogger) = Reader.ask
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
            return result 
            }

    let writeToConsole (result:ComparisonResult) = 
        reader {
            let! (console:#IConsole) = Reader.ask

            match result with
            | Bigger ->
                console.WriteLn "The first value is bigger"
            | Smaller ->
                console.WriteLn "The first value is smaller"
            | Equal ->
                console.WriteLn "The values are equal"

            }

    // compose them together in a program
    type IServices = 
        inherit ILogger
        inherit IConsole

    let program :Reader<IServices,_> = reader {
        let! str1,str2 = readFromConsole()  
        let! result = compareTwoStrings str1 str2
        do! writeToConsole result 
        }

    let services = 
        { new IServices 
          interface IConsole with 
            member __.ReadLn() = defaultConsole.ReadLn()
            member __.WriteLn str = defaultConsole.WriteLn str 
          interface ILogger with
            member __.Debug str = defaultLogger.Debug str
            member __.Info str = defaultLogger.Info str
            member __.Error str = defaultLogger.Error str
        }

// test
(*
open ReaderComposition_v1
Reader.run services program
*)
    
(* ======================================================================
Like any monad, Readers can be chained, composed, etc

Approach 2 - using withEnv
====================================================================== *)

module ReaderComposition_v2 = 

    let readFromConsole() = 
        reader {
            let! (console:IConsole),(logger:ILogger) = Reader.ask  // a tuple

            console.WriteLn "Enter the first value"
            let str1 = console.ReadLn()
            console.WriteLn "Enter the second value"
            let str2 = console.ReadLn()

            return str1,str2
            }

    let compareTwoStrings str1 str2  =
        reader {
            let! (logger:ILogger) = Reader.ask
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
            return result 
            }


    let writeToConsole (result:ComparisonResult) = 
        reader {
            let! (console:IConsole) = Reader.ask

            match result with
            | Bigger ->
                console.WriteLn "The first value is bigger"
            | Smaller ->
                console.WriteLn "The first value is smaller"
            | Equal ->
                console.WriteLn "The values are equal"

            }

    type Services = {
        Logger : ILogger
        Console : IConsole
        }
(*
    let program_bad = reader {
        let! str1, str2 = readFromConsole() 
        let! result = compareTwoStrings str1 str2 // error
        do! writeToConsole result // error 
        }
*)    

    let program = reader {
        // helper functions to transform the environment
        let getConsole services = services.Console 
        let getLogger services = services.Logger
        let getConsoleAndLogger services = services.Console,services.Logger  // a tuple

        let! str1, str2 = 
            readFromConsole() 
            |> Reader.withEnv getConsoleAndLogger 
        let! result = 
            compareTwoStrings str1 str2 
            |> Reader.withEnv getLogger 
        do! writeToConsole result 
            |> Reader.withEnv getConsole
        }

    let services = { 
        Console = defaultConsole
        Logger = defaultLogger
        }

// test
(*
open ReaderComposition_v2
Reader.run services program
*)
