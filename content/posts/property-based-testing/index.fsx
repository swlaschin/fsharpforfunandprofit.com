// ------------------
// Test 1
// ------------------

#r "nuget:NUnit"

open System
open NUnit.Framework

// placeholder
let add (x:int) (y:int) =
    x + y

// ================================
// Introducing the EDFH
// ================================

module Test1 =

    //>test1_edfh
    let add x y =
        if x=1 && y=2 then
            3
        else
            0
    //<

    //>test1
    [<Test>]
    let ``When I add 1 + 2, I expect 3``() =
        let result = add 1 2
        Assert.AreEqual(3,result)
    //<


// ------------------
// Test 2
// ------------------

module Test2 =

    //>test2_edfh
    let add x y =
        if x=1 && y=2 then
            3
        else if x=2 && y=2 then
            4
        else
            0 // all other cases
    //<

    //>test2
    [<Test>]
    let ``When I add 2 + 2, I expect 4``() =
        let result = add 2 2
        Assert.AreEqual(4,result)
    //<

// ------------------
// Test 3
// ------------------

module Test3 =

    //>test3_edfh
    let add x y =
        match x,y with
        | 1,2 -> 3
        | 2,2 -> 4
        | 3,5 -> 8
        | 27,15 -> 42
        | _ -> 0 // all other cases
    //<

    //>test3
    [<Test>]
    let ``Add two numbers, expect their sum``() =
        let testData = [ (1,2,3); (2,2,4); (3,5,8); (27,15,42) ]
        for (x,y,expected) in testData do
            let actual = add x y
            Assert.AreEqual(expected,actual)
    //<

module Test4 =

    //>test4
    let rand = System.Random()
    let randInt() = rand.Next()

    [<Test>]
    let ``Add two random numbers, expect their sum``() =
        let x = randInt()
        let y = randInt()
        let expected = x + y
        let actual = add x y
        Assert.AreEqual(expected,actual)
    //<

// reuse in rest of file
let randInt = Test4.randInt

module Test5 =

    //>test5
    [<Test>]
    let ``Add two random numbers 100 times, expect their sum``() =
        for _ in [1..100] do
            let x = randInt()
            let y = randInt()
            let expected = x + y
            let actual = add x y
            Assert.AreEqual(expected,actual)
    //<

// ========================================
// Property based testing
// ========================================

module RandomizedInputs =

    //>paramorder
    [<Test>]
    let addDoesNotDependOnParameterOrder() =
        for _ in [1..100] do
            let x = randInt()
            let y = randInt()
            let result1 = add x y
            let result2 = add y x // reversed params
            Assert.AreEqual(result1,result2)
    //<

    do
        let x = 1
        //>paramorder2
        let result1 = add x x
        let result2 = x * 2
        Assert.AreEqual(result1,result2)
        //<


    //>add1twice
    [<Test>]
    let addOneTwiceIsSameAsAddTwo() =
        for _ in [1..100] do
            let x = randInt()
            let y = randInt()
            let result1 = x |> add 1 |> add 1
            let result2 = x |> add 2
            Assert.AreEqual(result1,result2)
    //<

    // malicious implementation of add to replace in the above eamples
    module Add_Edfh =

        //>add1twice_edfh
        let add x y = 0  // malicious implementation
        //<


    //>addzero
    [<Test>]
    let addZeroIsSameAsDoingNothing() =
        for _ in [1..100] do
            let x = randInt()
            let result1 = x |> add 0
            let result2 = x
            Assert.AreEqual(result1,result2)
    //<

// ========================================
// Refactoring the common code
// ========================================

module HomeMadePropertyChecker =

    //>propertyCheck
    let propertyCheck property =
        // property has type: int -> int -> bool
        for _ in [1..100] do
            let x = randInt()
            let y = randInt()
            let result = property x y
            Assert.IsTrue(result)
    //<


    //>commutativeProperty
    let commutativeProperty x y =
        let result1 = add x y
        let result2 = add y x // reversed params
        result1 = result2

    [<Test>]
    let addDoesNotDependOnParameterOrder() =
        propertyCheck commutativeProperty
    //<

module HomeMadePropertyCheckerAll =

    //>propertyCheckAll
    let rand = System.Random()
    let randInt() = rand.Next()

    let add x y = x + y  // correct implementation

    let propertyCheck property =
        // property has type: int -> int -> bool
        for _ in [1..100] do
            let x = randInt()
            let y = randInt()
            let result = property x y
            Assert.IsTrue(result)

    let commutativeProperty x y =
        let result1 = add x y
        let result2 = add y x // reversed params
        result1 = result2

    [<Test>]
    let addDoesNotDependOnParameterOrder() =
        propertyCheck commutativeProperty

    let add1TwiceIsAdd2Property x _ =
        let result1 = x |> add 1 |> add 1
        let result2 = x |> add 2
        result1 = result2

    [<Test>]
    let addOneTwiceIsSameAsAddTwo() =
        propertyCheck add1TwiceIsAdd2Property

    let identityProperty x _ =
        let result1 = x |> add 0
        result1 = x

    [<Test>]
    let addZeroIsSameAsDoingNothing() =
        propertyCheck identityProperty
    //<

// ================================
// Introducing FsCheck
// ================================


//>nugetFsCheck
#r "nuget:NUnit"
open FsCheck
//<

// If not using F# 5, use nuget to download it using "nuget install FsCheck" or similar

(*
//>nugetFsCheckOld
// 1) use "nuget install FsCheck" or similar to download
// 2) include your nuget path here
#I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
// 3) reference the DLL
#r "FsCheck.dll"
open FsCheck
//<
*)


module FsCheckExample =


    //>quickCheckCommutativeProperty
    let add x y = x + y  // correct implementation

    let commutativeProperty (x,y) =
        let result1 = add x y
        let result2 = add y x // reversed params
        result1 = result2

    // check the property interactively
    Check.Quick commutativeProperty
    //<


    //>quickCheckAdditionProperty
    let add1TwiceIsAdd2Property x =
        let result1 = x |> add 1 |> add 1
        let result2 = x |> add 2
        result1 = result2

    // check the property interactively
    Check.Quick add1TwiceIsAdd2Property
    //<

    //>quickCheckIdentityProperty
    let identityProperty x =
        let result1 = x |> add 0
        result1 = x

    // check the property interactively
    Check.Quick identityProperty
    //<

    // If you check one of the properties interactively, you'll see the message:

    (*
    Ok, passed 100 tests.
    *)

// ===========================================
// Using FsCheck to find unsatified properties
// ===========================================

module FsCheckExample2 =

    //>quickCheckAdditionProperty_edfh
    let add x y =
        x * y // malicious implementation

    let add1TwiceIsAdd2Property x =
        let result1 = x |> add 1 |> add 1
        let result2 = x |> add 2
        result1 = result2

    // check the property interactively
    Check.Quick add1TwiceIsAdd2Property
    //<

    // The result from FsCheck is:

    (*
    //>quickCheckAdditionProperty_edfh_result
    Falsifiable, after 1 test (1 shrink) (StdGen (1657127138,295941511)):
    1
    //<
    *)

//===================================
// The return of the malicious EDFH
//===================================

module FsCheckExample3 =

    //>quickCheckAssoc_edfh
    let add x y =
        if (x < 10) || (y < 10) then
            x + y  // correct for low values
        else
            x * y  // incorrect for high values
    //<

    //>quickCheckAssoc
    let associativeProperty x y z =
        let result1 = add x (add y z)    // x + (y + z)
        let result2 = add (add x y) z    // (x + y) + z
        result1 = result2

    // check the property interactively
    Check.Quick associativeProperty
    //<


    (*
    //>quickCheckAssoc_edfh_result
    Falsifiable, after 38 tests (4 shrinks) (StdGen (127898154,295941554)):
    8
    2
    10
    //<
    *)


