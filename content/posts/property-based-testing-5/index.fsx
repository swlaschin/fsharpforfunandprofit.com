
// F# 5 will load a nuget package directly!
#r "nuget:FsCheck"

// If not using F# 5, use nuget to download it using "nuget install FsCheck" or similar
(*
// 1) use "nuget install FsCheck" or similar to download
// 2) include your nuget path here
#I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
// 3) reference the DLL
#r "FsCheck.dll"
*)

open System
open FsCheck


// =====================
// Version 1: Mutable Dollar
// =====================

module Dollar1 =

    //>dollar1a
    // OO style class with members
    type Dollar(amount:int) =
        /// factory method
        static member Create amount  =
            Dollar amount
        /// field
        member val Amount =
            amount with get, set
        /// Add to the amount
        member this.Add add =
            this.Amount <- this.Amount + add
        /// Multiply the amount
        member this.Times multiplier  =
            this.Amount <- this.Amount * multiplier
    //<

    //>dollar1b
    let d = Dollar.Create 2
    d.Amount  // 2
    d.Times 3
    d.Amount  // 6
    d.Add 1
    d.Amount  // 7
    //<

    //>dollar1c
    let setThenGetShouldGiveSameResult value =
        let obj = Dollar.Create 0
        obj.Amount <- value
        let newValue = obj.Amount
        value = newValue

    Check.Quick setThenGetShouldGiveSameResult
    // Ok, passed 100 tests.
    //<


    //>dollar1d
    let setIsIdempotent value =
        let obj = Dollar.Create 0
        obj.Amount <- value
        let afterFirstSet = obj.Amount
        obj.Amount <- value
        let afterSecondSet = obj.Amount
        afterFirstSet = afterSecondSet

    Check.Quick setIsIdempotent
    // Ok, passed 100 tests.
    //<

// =================================
// Version 2: Immutable dollar
// =================================

module Dollar2 =

    //>dollar2a
    type Dollar(amount:int) =
        static member Create amount  =
            Dollar amount
        member val Amount =
            amount
        member this.Add add =
            Dollar (amount + add)
        member this.Times multiplier  =
            Dollar (amount * multiplier)
    //<

    // interactive test
    //>dollar2b
    let d1 = Dollar.Create 2
    d1.Amount  // 2
    let d2 = d1.Times 3
    d2.Amount  // 6
    let d3 = d2.Add 1
    d3.Amount  // 7
    //<


    //>dollar2c
    let createThenTimes_eq_timesThenCreate start multiplier =
        let d1 = Dollar.Create(start).Times(multiplier)
        let d2 = Dollar.Create(start * multiplier)
        d1 = d2
    //<


    //>dollar2c_check
    Check.Quick createThenTimes_eq_timesThenCreate
    // Falsifiable, after 1 test
    //<

    //>dollar2d
    let dollarsWithSameAmountAreEqual amount =
        let d1 = Dollar.Create amount
        let d2 = Dollar.Create amount
        d1 = d2

    Check.Quick dollarsWithSameAmountAreEqual
    // Falsifiable, after 1 test
    //<

// =================================
// Version 3: Immutable dollar using F# record
// =================================

module rec Dollar3 =

    //>dollar3a
    type Dollar = {amount:int }
        with
        static member Create amount  =
            {amount = amount}
        member this.Add add =
            {amount = this.amount + add }
        member this.Times multiplier  =
            {amount = this.amount * multiplier }
    //<

    let dollarsWithSameAmountAreEqual amount =
        let d1 = Dollar.Create amount
        let d2 = Dollar.Create amount
        d1 = d2

    let createThenTimes_eq_timesThenCreate start multiplier =
        let d1 = Dollar.Create(start).Times(multiplier)
        let d2 = Dollar.Create(start * multiplier)
        d1 = d2

    //>dollar3b_check
    Check.Quick dollarsWithSameAmountAreEqual
    // Ok, passed 100 tests.

    Check.Quick createThenTimes_eq_timesThenCreate
    // Ok, passed 100 tests.
    //<


    //>dollar3c
    let createThenTimesThenGet_eq_times start multiplier =
        let d1 = Dollar.Create(start).Times(multiplier)
        let a1 = d1.amount
        let a2 = start * multiplier
        a1 = a2

    Check.Quick createThenTimesThenGet_eq_times
    // Ok, passed 100 tests.
    //<


    //>dollar3d
    let createThenTimesThenAdd_eq_timesThenAddThenCreate start multiplier adder =
        let d1 = Dollar.Create(start).Times(multiplier).Add(adder)
        let directAmount = (start * multiplier) + adder
        let d2 = Dollar.Create directAmount
        d1 = d2

    Check.Quick createThenTimesThenAdd_eq_timesThenAddThenCreate
    // Ok, passed 100 tests.
    //<


//  Dollar properties -- version 4

module Dollar4 =

    //>dollar4a
    type Dollar = {amount:int }
        with
        static member Create amount  =
            {amount = amount}
        member this.Map f =
            {amount = f this.amount}
        member this.Times multiplier =
            this.Map (fun a -> a * multiplier)
        member this.Add adder =
            this.Map (fun a -> a + adder)
    //<

    //>dollar4b
    let createThenMap_eq_mapThenCreate start f =
        let d1 = Dollar.Create(start).Map f
        let d2 = Dollar.Create(f start)
        d1 = d2
    //<


    //>dollar4b_check
    Check.Quick createThenMap_eq_mapThenCreate
    // Ok, passed 100 tests.
    //<

    // Logging the function parameter
    //>dollar4b_check2
    Check.Verbose createThenMap_eq_mapThenCreate
    //<

(*
//>dollar4b_check2_out
0:
18
<fun:Invoke@3000>
1:
7
<fun:Invoke@3000>
-- etc
98:
47
<fun:Invoke@3000>
99:
36
<fun:Invoke@3000>
Ok, passed 100 tests.
//<
*)


    //>dollar4c
    let createThenMap_eq_mapThenCreate_v2 start (F (_,f)) =
        let d1 = Dollar.Create(start).Map f
        let d2 = Dollar.Create(f start)
        d1 = d2
    //<

    //>dollar4c_check
    Check.Verbose createThenMap_eq_mapThenCreate_v2
    //<


(*
//>dollar4c_check_out
0:
0
{ 0->1 }
1:
0
{ 0->0 }
2:
2
{ 2->-2 }
-- etc
98:
-5
{ -5->-52 }
99:
10
{ 10->28 }
Ok, passed 100 tests.
//<
*)

