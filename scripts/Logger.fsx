open System

//=======================================
// Logging
//=======================================

let mutable debugOn = false

let logDebug (msg:string) =
    if debugOn then
        Console.ForegroundColor <- ConsoleColor.Gray
        Console.WriteLine("DEBUG: {0}",msg)
        Console.ResetColor()

let logInfo (msg:string) =
    Console.ForegroundColor <- ConsoleColor.Green
    Console.WriteLine("INFO: {0}",msg)
    Console.ResetColor()

let logWarn (msg:string) =
    Console.ForegroundColor <- ConsoleColor.Red
    Console.WriteLine("WARN: {0}",msg)
    Console.ResetColor()

