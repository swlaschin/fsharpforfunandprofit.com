(* ===================================
Code from my post "Five approaches to dependency injection"
=================================== *)

open System


(* ======================================================================
5. Dependency Interpretation

In which we create a data structure and then interpret it later
====================================================================== *)


//----------------------------------------
// Version 1 - a bad attempt to return commands
//----------------------------------------

module ReadFromConsole_v1 =

    type Instruction =
        | ReadLn
        | WriteLn of string

    let readFromConsole() = 
        let cmd1 = WriteLn "Enter the first value"
        let cmd2 = ReadLn
        let cmd3 = WriteLn "Enter the second value"
        let cmd4 = ReadLn

        // return all the instructions I want the I/O part to do
        [cmd1; cmd2; cmd3; cmd4]

// doesn't compile!
(*
    let interpretInstruction instruction =
        match instruction with
        | ReadLn -> Console.ReadLine()
        | WriteLn str -> printfn "%s" str  
  
*)


//----------------------------------------
// Version 2 - a correct attempt to using a Program structure
//----------------------------------------

module ReadFromConsole_v2 =

    
    // normal ReadLn is  unit -> string
    // it becomes        unit * (string -> Program)
    //
    // normal WriteLn is string -> unit
    // it becomes        string * (unit -> Program)

    type Program<'a> =
        //           input to        output of 
        //           interpreter     interpreter
        | ReadLn  of unit    * next:(string    -> Program<'a>)
        | WriteLn of string  * next:(unit      -> Program<'a>)
        | Stop    of 'a

    let readFromConsole = 
        WriteLn ("Enter the first value" , fun () ->
        ReadLn  ( ()                     , fun str1 ->
        WriteLn ("Enter the second value", fun () ->
        ReadLn  ( ()                     , fun str2 ->
        Stop  (str1,str2)
        ))))
        
    let rec interpret program =
        match program with
        | ReadLn ((), next) -> 
            // do the actual I/O
            let str = Console.ReadLine()
            // call "next" with the output of the interpreter
            // to get another program
            let nextProgram = next str 
            // interpret the new program
            interpret nextProgram   
        | WriteLn (str,next) -> 
            printfn "%s" str
            let nextProgram = next()
            interpret nextProgram   
        | Stop value -> 
            value // return as overall result
    
// test
(*
open ReadFromConsole_v2 
interpret readFromConsole
*)

//----------------------------------------
// Version 3 - Same as above, but using computation expressions
//----------------------------------------

module ReadFromConsole_v3 =
    open ReadFromConsole_v2 

    // same as before
    type Program<'a> = ReadFromConsole_v2.Program<'a>

    module Program =
        let rec bind f program = 
            match program with
            | ReadLn ((),next) -> ReadLn ((),next >> bind f)
            | WriteLn (str,next) -> WriteLn (str, next >> bind f)
            | Stop x -> f x

    type ProgramBuilder() =
        member __.Return(x) = Stop x 
        member __.Bind(x,f) = Program.bind f x
        member __.Zero() = Stop ()

    // the builder instance
    let program = ProgramBuilder()

    // helpers to use within the computation expression
    let writeLn str = WriteLn (str,Stop)
    let readLn() = ReadLn ((),Stop)

    let readFromConsole = program {
        do! writeLn "Enter the first value"
        let! str1 = readLn()  
        do! writeLn "Enter the second value"
        let! str2 = readLn()  
        return  (str1,str2)
        }

    // same as before
    let interpret = ReadFromConsole_v2.interpret
    
// test
(*
open ReadFromConsole_v3 
interpret readFromConsole
*)


//----------------------------------------
// The complete mini-application
//----------------------------------------

module MiniApplication_v1 =


    // 1. Define the set of instructions we want to support
    type ConsoleInstruction<'a> =
        | ReadLn  of unit    * next:(string -> 'a)
        | WriteLn of string  * next:(unit   -> 'a)

    type LoggerInstruction<'a> =
        | LogDebug of string * next:(unit -> 'a)
        | LogInfo of string  * next:(unit -> 'a)

    type Program<'a> =
        | ConsoleInstruction of ConsoleInstruction<Program<'a>>
        | LoggerInstruction of LoggerInstruction<Program<'a>>
        | Stop  of 'a

    // 2. Define a "map" for each group of instructions
    module ConsoleInstruction =
        let rec map f program = 
            match program with
            | ReadLn ((),next) -> ReadLn ((),next >> f)
            | WriteLn (str,next) -> WriteLn (str, next >> f)

    module LoggerInstruction =
        let rec map f program = 
            match program with
            | LogDebug (str,next) ->  LogDebug (str,next >> f)
            | LogInfo (str,next) ->  LogInfo (str,next >> f)

    // 3. Define the corresponding "bind" 
    module Program =
        let rec bind f program = 
            match program with
            | ConsoleInstruction inst -> 
                inst |> ConsoleInstruction.map (bind f) |> ConsoleInstruction 
            | LoggerInstruction inst -> 
                inst |> LoggerInstruction.map (bind f) |> LoggerInstruction 
            | Stop x -> f x

    // 4. Define the computation expression
    type ProgramBuilder() =
        member __.Return(x) = Stop x 
        member __.Bind(x,f) = Program.bind f x
        member __.Zero() = Stop ()

    // the builder instance
    let program = ProgramBuilder()

    
    // 5. Define the interpreter
    let rec interpret program =

        let interpretConsole inst =
            match inst with
            | ReadLn ((), next) -> 
                let str = Console.ReadLine()
                interpret (next str)
            | WriteLn (str,next) -> 
                printfn "%s" str
                interpret (next())

        let interpretLogger inst =
            match inst with
            | LogDebug (str, next) -> 
                printfn "DEBUG %s" str
                interpret (next())
            | LogInfo (str, next) -> 
                printfn "INFO %s" str
                interpret (next())

        match program with
        | ConsoleInstruction inst -> interpretConsole inst
        | LoggerInstruction inst -> interpretLogger inst
        | Stop value -> value 

    // helpers to use within the computation expression
    let writeLn str = ConsoleInstruction (WriteLn (str,Stop))
    let readLn() = ConsoleInstruction (ReadLn ((),Stop))
    let logDebug str = LoggerInstruction (LogDebug (str,Stop))
    let logInfo str = LoggerInstruction (LogInfo (str,Stop))


    type ComparisonResult =
        | Bigger
        | Smaller
        | Equal

    let readFromConsole = program {
        do! writeLn "Enter the first value"
        let! str1 = readLn()  
        do! writeLn "Enter the second value"
        let! str2 = readLn()  
        return  (str1,str2)
        }

    let compareTwoStrings str1 str2 = program {
        do! logDebug "compareTwoStrings: Starting"

        let result =
            if str1 > str2 then
                Bigger
            else if str1 < str2 then
                Smaller
            else
                Equal

        do! logInfo (sprintf "compareTwoStrings: result=%A" result)
        do! logDebug "compareTwoStrings: Finished"
        return result 
        }


    let writeToConsole (result:ComparisonResult) = program {
        match result with
        | Bigger ->
            do! writeLn "The first value is bigger"
        | Smaller ->
            do! writeLn "The first value is smaller"
        | Equal ->
            do! writeLn "The values are equal"
        }


    let myProgram = program {
        let! str1, str2 = readFromConsole 
        let! result = compareTwoStrings str1 str2 
        do! writeToConsole result 
        }

(*
open MiniApplication_v1
interpret myProgram
*)


//----------------------------------------
// A generic program that does not know about specific instructions
//----------------------------------------

module GenericProgram =

    // 1. Define a instruction interface that contains a "map" 
    type IInstruction<'a> =
        abstract member Map : ('a->'b) -> IInstruction<'b> 

    // 2, Use the interface in the Program type
    type Program<'a> =
        | Instruction of IInstruction<Program<'a>>
        | Stop of 'a

    // 3. Define the corresponding "bind" 
    module Program =
        let rec bind f program = 
            match program with
            | Instruction inst -> 
                inst.Map (bind f) |> Instruction 
            | Stop x -> f x

    // 4. Define the computation expression
    type ProgramBuilder() =
        member __.Return(x) = Stop x 
        member __.Bind(x,f) = Program.bind f x
        member __.Zero() = Stop ()

    // and the builder instance
    let program = ProgramBuilder()

//----------------------------------------
// The complete mini-application, using the generic program approach
//----------------------------------------

module MiniApplication_v2 =

    open GenericProgram
    
    // to support a specific application:

    // 1. Define the set of instructions we want to support, and their map
    type ConsoleInstruction<'a> =
        | ReadLn  of unit    * next:(string -> 'a)
        | WriteLn of string  * next:(unit   -> 'a)
        interface IInstruction<'a> with
            member this.Map f  = 
                match this with
                | ReadLn ((),next) -> ReadLn ((),next >> f)
                | WriteLn (str,next) -> WriteLn (str, next >> f)
                :> IInstruction<_> 

    type LoggerInstruction<'a> =
        | LogDebug of string * next:(unit -> 'a)
        | LogInfo of string  * next:(unit -> 'a)
        interface IInstruction<'a> with
            member this.Map f  = 
                match this with
                | LogDebug (str,next) ->  LogDebug (str,next >> f)
                | LogInfo (str,next) ->  LogInfo (str,next >> f)
                :> IInstruction<_> 

    // 2. Define the interpreter

    // modular interpreter for ConsoleInstruction
    let interpretConsole interpret inst =
        match inst with
        | ReadLn ((), next) -> 
            let str = Console.ReadLine()
            interpret (next str)
        | WriteLn (str,next) -> 
            printfn "%s" str
            interpret (next())

    // modular interpreter for LoggerInstruction
    let interpretLogger interpret inst =
        match inst with
        | LogDebug (str, next) -> 
            printfn "DEBUG %s" str
            interpret (next())
        | LogInfo (str, next) -> 
            printfn "INFO %s" str
            interpret (next())

    // interpreter for this particular workflow
    let rec interpret program =
        match program with
        | Instruction inst ->
            match inst with
            | :? ConsoleInstruction<Program<_>> as i -> interpretConsole interpret i
            | :? LoggerInstruction<Program<_>> as i -> interpretLogger interpret i
            | _ -> failwithf "unknown instruction type %O" (inst.GetType())
        | Stop value -> value 

    // helpers to use within the computation expression
    let writeLn str = Instruction (WriteLn (str,Stop))
    let readLn() = Instruction (ReadLn ((),Stop))
    let logDebug str = Instruction (LogDebug (str,Stop))
    let logInfo str = Instruction (LogInfo (str,Stop))

    type ComparisonResult =
        | Bigger
        | Smaller
        | Equal

    let readFromConsole = program {
        do! writeLn "Enter the first value"
        let! str1 = readLn()  
        do! writeLn "Enter the second value"
        let! str2 = readLn()  
        return  (str1,str2)
        }

    let compareTwoStrings str1 str2 = program {
        do! logDebug "compareTwoStrings: Starting"

        let result =
            if str1 > str2 then
                Bigger
            else if str1 < str2 then
                Smaller
            else
                Equal

        do! logInfo (sprintf "compareTwoStrings: result=%A" result)
        do! logDebug "compareTwoStrings: Finished"
        return result 
        }

    let writeToConsole (result:ComparisonResult) = program {
        match result with
        | Bigger ->
            do! writeLn "The first value is bigger"
        | Smaller ->
            do! writeLn "The first value is smaller"
        | Equal ->
            do! writeLn "The values are equal"
        }


    let myProgram = program {
        let! str1, str2 = readFromConsole 
        let! result = compareTwoStrings str1 str2 
        do! writeToConsole result 
        }

(*
open MiniApplication_v2
interpret myProgram
*)

//----------------------------------------
// Demonstrates that this approach is modular
//----------------------------------------

module ModularityDemo =

    open GenericProgram
    open MiniApplication_v2

    type DbInstruction<'a> =
        | DbInsert of string  * next:(unit   -> 'a)
        interface IInstruction<'a> with
            member this.Map f  = 
                match this with
                | DbInsert (str,next) -> DbInsert (str, next >> f)
                :> IInstruction<_> 

    // helpers to use within the computation expression
    let dbInsert str = Instruction (DbInsert(str,Stop))
    
    let logStringsToDb str1 str2 = program {
        do! dbInsert str1
        do! dbInsert str2
        }

    let myProgram = program {
        let! str1, str2 = readFromConsole 
        do! logStringsToDb str1 str2 
        let! result = compareTwoStrings str1 str2 
        do! writeToConsole result 
        }
        
    // modular interpreter for LoggerInstruction
    let interpretDbInstruction interpret inst =
        match inst with
        | DbInsert (str, next) -> 
            printfn "DbInsert %s" str
            interpret (next())

    // interpreter for this particular workflow
    let rec interpret program =
        match program with
        | Instruction inst ->
            match inst with
            | :? ConsoleInstruction<Program<_>> as i -> interpretConsole interpret i
            | :? LoggerInstruction<Program<_>> as i -> interpretLogger interpret i
            | :? DbInstruction<Program<_>> as i -> interpretDbInstruction interpret i
            | _ -> failwithf "unknown instruction type %O" (inst.GetType())
        | Stop value -> value 

(*
open ModularityDemo
interpret myProgram
*)
