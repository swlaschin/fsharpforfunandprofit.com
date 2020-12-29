(* ===================================
Code from my post "Five approaches to dependency injection"
=================================== *)

open System

(* ======================================================================
1. Dependency Retention

In which we don't worry about managing dependencies, 
and just inline and hard-code everything!
====================================================================== *)

let compareTwoStrings() =
    printfn "Enter the first value"
    let str1 = Console.ReadLine()
    printfn "Enter the second value"
    let str2 = Console.ReadLine()

    if str1 > str2 then
        printfn "The first value is bigger"
    else if str1 < str2 then
        printfn "The first value is smaller"
    else
        printfn "The values are equal"

// The only way to test this is manually
(*
compareTwoStrings()
*)

