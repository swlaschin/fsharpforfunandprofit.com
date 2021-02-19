
#r "nuget:NUnit"
#r "nuget:FsCheck"

(*
// If not using F# 5, use nuget to download it using "nuget install FsCheck" or similar
// then add your nuget path here
#I "/Users/%USER%/.nuget/packages/fscheck/2.14.4/lib/netstandard2.0"
#r "FsCheck.dll"
*)

open System
open NUnit.Framework
open FsCheck

// =======================
// Understanding FsCheck: Generators
// =======================


//>intGenerator
// get the generator for ints
let intGenerator = Arb.generate<int>
//<

//>intGenerator1
// generate three ints with a maximum size of 1
Gen.sample 1 3 intGenerator    // e.g. [0; 0; -1]

// generate three ints with a maximum size of 10
Gen.sample 10 3 intGenerator   // e.g. [-4; 8; 5]

// generate three ints with a maximum size of 100
Gen.sample 100 3 intGenerator  // e.g. [-37; 24; -62]
//<

//>intGenerator2
// see how the values are clustered around the center point
intGenerator
|> Gen.sample 10 1000
|> Seq.groupBy id  // use the generated number as key
|> Seq.map (fun (k,v) -> (k,Seq.length v)) // count the occurences
|> Seq.sortBy fst  // sort by key
|> Seq.toList
//<

//>intGenerator3
// the (key, count) pairs
// see how the values are clustered around the center point of 0
[(-10, 3); (-9, 14); (-8, 18); (-7, 10); (-6, 27);
 (-5, 42); (-4, 49); (-3, 56); (-2, 76); (-1, 119);
 (0, 181); (1, 104); (2, 77); (3, 62); (4, 47); (5, 44);
 (6, 26); (7, 16); (8, 14); (9, 12); (10, 3)]
//<


//>intGenerator4
intGenerator
|> Gen.sample 30 10000
|> Seq.groupBy id
|> Seq.map (fun (k,v) -> (k,Seq.length v))
|> Seq.sortBy (fun (k,v) -> k)
|> Seq.toList
//<


//>tupleGenerator
let tupleGenerator = Arb.generate<int*int*int>

// generate 3 tuples with a maximum size of 1
Gen.sample 1 3 tupleGenerator
// result: [(0, 0, 0); (0, 0, 0); (0, 1, -1)]

// generate 3 tuples with a maximum size of 10
Gen.sample 10 3 tupleGenerator
// result: [(-6, -4, 1); (2, -2, 8); (1, -4, 5)]

// generate 3 tuples with a maximum size of 100
Gen.sample 100 3 tupleGenerator
// result: [(-2, -36, -51); (-5, 33, 29); (13, 22, -16)]
//<


//>intOptionGenerator
let intOptionGenerator = Arb.generate<int option>
// generate 10 int options with a maximum size of 5
Gen.sample 5 10 intOptionGenerator
// result:  [Some 0; Some -1; Some 2; Some 0; Some 0;
//           Some -4; null; Some 2; Some -2; Some 0]
//<



//>intListGenerator
let intListGenerator = Arb.generate<int list>
// generate 10 int lists with a maximum size of 5
Gen.sample 5 10 intListGenerator
// result:  [ []; []; [-4]; [0; 3; -1; 2]; [1];
//            [1]; []; [0; 1; -2]; []; [-1; -2]]
//<


//>stringGenerator
let stringGenerator = Arb.generate<string>

// generate 3 strings with a maximum size of 1
Gen.sample 1 3 stringGenerator
// result: [""; "!"; "I"]

// generate 3 strings with a maximum size of 10
Gen.sample 10 3 stringGenerator
// result: [""; "eiX$a^"; "U%0Ika&r"]
//<



//>udtGenerator
type Color = Red | Green of int | Blue of bool

let colorGenerator = Arb.generate<Color>

// generate 10 colors with a maximum size of 50
Gen.sample 50 10 colorGenerator

// result:  [Green -47; Red; Red; Red; Blue true;
//           Green 2; Blue false; Red; Blue true; Green -12]
//<


//>udtGenerator2
type Point = {x:int; y:int; color: Color}

let pointGenerator = Arb.generate<Point>

// generate 10 points with a maximum size of 50
Gen.sample 50 10 pointGenerator

(* result
[{x = -8; y = 12; color = Green -4;};
 {x = 28; y = -31; color = Green -6;};
 {x = 11; y = 27; color = Red;};
 {x = -2; y = -13; color = Red;};
 {x = 6; y = 12; color = Red;};
 // etc
*)
//<


// =================================
// Understanding FsCheck: Shrinking
// =================================

module Shrink =
    //>shrink1
    let isSmallerThan80 x = x < 80
    //<

    //>shrink2
    isSmallerThan80 100 // false, so start shrinking

    Arb.shrink 100 |> Seq.toList
    //  [0; 50; 75; 88; 94; 97; 99]
    //<

    //>shrink3
    isSmallerThan80 0 // true
    isSmallerThan80 50 // true
    isSmallerThan80 75 // true
    isSmallerThan80 88 // false, so shrink again
    //<

    //>shrink4
    Arb.shrink 88 |> Seq.toList
    //  [0; 44; 66; 77; 83; 86; 87]
    isSmallerThan80 0 // true
    isSmallerThan80 44 // true
    isSmallerThan80 66 // true
    isSmallerThan80 77 // true
    isSmallerThan80 83 // false, so shrink again
    //<

    //>shrink5
    Arb.shrink 83 |> Seq.toList
    //  [0; 42; 63; 73; 78; 81; 82]
    // smallest failure is 81, so shrink again
    //<

    //>shrink6
    Arb.shrink 81 |> Seq.toList
    //  [0; 41; 61; 71; 76; 79; 80]
    // smallest failure is 80
    //<


    //>shrinkTuple
    Arb.shrink (1,2,3) |> Seq.toList
    //  [(0, 2, 3); (1, 0, 3); (1, 1, 3);
    //   (1, 2, 0); (1, 2, 2)]

    Arb.shrink "abcd" |> Seq.toList
    //  ["bcd"; "acd"; "abd"; "abc"; "abca";
    //   "abcb"; "abcc"; "abad"; "abbd"; "aacd"]

    Arb.shrink [1;2;3] |> Seq.toList
    //  [[2; 3]; [1; 3]; [1; 2]; [1; 2; 0]; [1; 2; 2];
    //  [1; 0; 3]; [1; 1; 3]; [0; 2; 3]]
    //<

// =========================================
// Configuring FsCheck: Changing the number of tests
// =========================================

//>testCount1
// silly property to test
let isSmallerThan80 x = x < 80

Check.Quick isSmallerThan80
// result: Ok, passed 100 tests.
//<

module ConfigMaxTest1000 =
    //>testCount2
    let config = {
        Config.Quick with
            MaxTest = 1000
        }
    Check.One(config,isSmallerThan80 )
    // result: Ok, passed 1000 tests.
    //<

module ConfigMaxTest10000 =

    //>testCount3
    let config = {
        Config.Quick with
            MaxTest = 10000
        }
    Check.One(config,isSmallerThan80 )
    // result: Falsifiable, after 8660 tests (1 shrink):
    //         80
    //<

module ConfigEndSize =
    //>testCount4
    let config = {
        Config.Quick with
            EndSize = 1000
        }
    Check.One(config,isSmallerThan80 )
    // result: Falsifiable, after 21 tests (4 shrinks):
    //         80
    //<

//============================================
// Configuring FsCheck: Verbose mode and logging
//============================================


//>logging1
let add x y =
    if (x < 25) || (y < 25) then
        x + y  // correct for low values
    else
        x * y  // incorrect for high values

let associativeProperty x y z =
    let result1 = add x (add y z)    // x + (y + z)
    let result2 = add (add x y) z    // (x + y) + z
    result1 = result2

// check the property interactively
Check.Quick associativeProperty
//<

(*
//>logging2
Falsifiable, after 66 tests (12 shrinks):
1
24
25
//<
*)


//>logging3
// check the property interactively
Check.Quick associativeProperty

// with tracing/logging
Check.Verbose associativeProperty
//<

(*
//>logging4
0:    // test #0
-1    // generated parameter #1 ("x")
-1    // generated parameter #2 ("y")
0     // generated parameter #3 ("z")
//       associativeProperty(-1,-1,0) => true, keep going
1:    // test #1
0
0
0     // associativeProperty 0 0 0  => true, keep going
2:    // test #2
-2
0
-3    // associativeProperty -2 0 -3  => true, keep going
3:    // test #3
1
2
0     // associativeProperty 1 2 0  => true, keep going
// etc
49:   // test #49
46
-4
50    // associativeProperty 46 -4 50  => false, start shrinking
// etc
shrink:
35
-4
50    // associativeProperty 35 -4 50  => false, keep shrinking
shrink:
27
-4
50    // associativeProperty 27 -4 50  => false, keep shrinking
// etc
shrink:
25
1
29    // associativeProperty 25 1 29  => false, keep shrinking
shrink:
25
1
26    // associativeProperty 25 1 26  => false, keep shrinking
// next shrink fails
Falsifiable, after 50 tests (10 shrinks) (StdGen (995282583,295941602)):
25
1
26
//<
*)


//>logging5
// create a function for displaying a test
let printTest testNum [x;y;z] =
    sprintf "#%-3i %3O %3O %3O\n" testNum x y z

// create a function for displaying a shrink
let printShrink [x;y;z] =
    sprintf "shrink %3O %3O %3O\n" x y z
//<


//>logging5b
// create a new FsCheck configuration
let config = {
    Config.Quick with
        Replay = Random.StdGen (995282583,295941602) |> Some
        Every = printTest
        EveryShrink = printShrink
    }

// check the given property with the new configuration
Check.One(config,associativeProperty)
//<

(*
//>logging6
#0    -1  -1   0
#1     0   0   0
#2    -2   0  -3
#3     1   2   0
#4    -4   2  -3
#5     3   0  -3
#6    -1  -1  -1
// etc
#46  -21 -25  29
#47  -10  -7 -13
#48   -4 -19  23
#49   46  -4  50
// start shrinking first parameter
shrink  35  -4  50
shrink  27  -4  50
shrink  26  -4  50
shrink  25  -4  50
// start shrinking second parameter
shrink  25   4  50
shrink  25   2  50
shrink  25   1  50
// start shrinking third parameter
shrink  25   1  38
shrink  25   1  29
shrink  25   1  26
Falsifiable, after 50 tests (10 shrinks) (StdGen (995282583,295941602)):
25
1
26
//<
*)

// ==================================
// A real world example of shrinking
// ==================================

//>rwshrink1
// The last set of inputs (46,-4,50) was false, so shrinking started
associativeProperty 46 -4 50  // false, so shrink

// list of possible shrinks starting at 46
Arb.shrink 46 |> Seq.toList
// result [0; 23; 35; 41; 44; 45]
//<

module RwShrink2 =
    //>rwshrink2
    // find the next test that fails when shrinking the x parameter
    let x,y,z = (46,-4,50)
    Arb.shrink x
    |> Seq.tryPick (fun x ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (35, -4, 50)
    //<

module RwShrink3 =
    //>rwshrink3
    // find the next test that fails when shrinking the x parameter
    let x,y,z = (35,-4,50)
    Arb.shrink x
    |> Seq.tryPick (fun x ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (27, -4, 50)
    //<

module RwShrink4 =
    //>rwshrink4
    // find the next test that fails when shrinking the x parameter
    let x,y,z = (27,-4,50)
    Arb.shrink x
    |> Seq.tryPick (fun x ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (26, -4, 50)

    // find the next test that fails when shrinking the x parameter
    let x,y,z = (26,-4,50)
    Arb.shrink x
    |> Seq.tryPick (fun x ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (25, -4, 50)

    // find the next test that fails when shrinking the x parameter
    let x,y,z = (25,-4,50)
    Arb.shrink x
    |> Seq.tryPick (fun x ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer None
    //<

module RwShrink5 =
    //>rwshrink5
    // find the next test that fails when shrinking the y parameter
    let x,y,z = (25,-4,50)
    Arb.shrink y
    |> Seq.tryPick (fun y ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (25, 4, 50)

    // find the next test that fails when shrinking the y parameter
    let x,y,z = (25,4,50)
    Arb.shrink y
    |> Seq.tryPick (fun y ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (25, 2, 50)

    // find the next test that fails when shrinking the y parameter
    let x,y,z = (25,2,50)
    Arb.shrink y
    |> Seq.tryPick (fun y ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (25, 1, 50)

    // find the next test that fails when shrinking the y parameter
    let x,y,z = (25,1,50)
    Arb.shrink y
    |> Seq.tryPick (fun y ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer None
    //<

module RwShrink6 =
    //>rwshrink6
    // find the next test that fails when shrinking the z parameter
    let x,y,z = (25,1,50)
    Arb.shrink z
    |> Seq.tryPick (fun z ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (25, 1, 38)

    // find the next test that fails when shrinking the z parameter
    let x,y,z = (25,1,38)
    Arb.shrink z
    |> Seq.tryPick (fun z ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (25, 1, 29)

    // find the next test that fails when shrinking the z parameter
    let x,y,z = (25,1,29)
    Arb.shrink z
    |> Seq.tryPick (fun z ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer (25, 1, 26)

    // find the next test that fails when shrinking the z parameter
    let x,y,z = (25,1,26)
    Arb.shrink z
    |> Seq.tryPick (fun z ->
        if associativeProperty x y z then None else Some (x,y,z) )
    // answer None
    //<


//============================
// Adding pre-conditions
//============================

module PreCondition1 =
    //>precond1
    let additionIsNotMultiplication x y =
        x + y <> x * y
    //<


    //>precond1check
    Check.Quick additionIsNotMultiplication
    // Falsifiable, after 3 tests (0 shrinks):
    // 0
    // 0
    //<

module PreCondition2 =
    //>precond2
    let additionIsNotMultiplication x y =
        x + y <> x * y

    let preCondition x y =
        (x,y) <> (0,0)

    let additionIsNotMultiplication_withPreCondition x y =
        preCondition x y ==> additionIsNotMultiplication x y
    //<

    //>precond2check
    Check.Quick additionIsNotMultiplication_withPreCondition
    // Falsifiable, after 38 tests (0 shrinks):
    // 2
    // 2
    //<

module PreCondition3 =
    let additionIsNotMultiplication =
        PreCondition2.additionIsNotMultiplication

    //>precond3
    let preCondition x y =
        (x,y) <> (0,0)
        && (x,y) <> (2,2)

    let additionIsNotMultiplication_withPreCondition x y =
        preCondition x y ==> additionIsNotMultiplication x y
    //<


    //>precond3check
    Check.Quick additionIsNotMultiplication_withPreCondition
    // Ok, passed 100 tests.
    //<

// ======================
// Naming convention for properties
// ======================

module NamingConvention =
    //>combine1
    let add x y = x + y // good implementation

    let commutativeProperty x y =
        add x y = add y x

    let associativeProperty x y z =
        add x (add y z) = add (add x y) z

    let leftIdentityProperty x =
        add x 0 = x

    let rightIdentityProperty x =
        add 0 x = x
    //<

open NamingConvention

//>combine2
type AdditionSpecification =
    static member ``Commutative`` x y =
        commutativeProperty x y
    static member ``Associative`` x y z =
        associativeProperty x y z
    static member ``Left Identity`` x =
        leftIdentityProperty x
    static member ``Right Identity`` x =
        rightIdentityProperty x

Check.QuickAll<AdditionSpecification>()
//<

(*
//>combine2_check
--- Checking AdditionSpecification ---
AdditionSpecification.Commutative-Ok, passed 100 tests.
AdditionSpecification.Associative-Ok, passed 100 tests.
AdditionSpecification.Left Identity-Ok, passed 100 tests.
AdditionSpecification.Right Identity-Ok, passed 100 tests.
//<
*)

module MixPbtAndExamples =
    //>combine3
    type AdditionSpecification =

        // some properties
        static member ``Commutative`` x y =
            commutativeProperty x y
        static member ``Associative`` x y z =
            associativeProperty x y z
        static member ``Left Identity`` x =
            leftIdentityProperty x
        static member ``Right Identity`` x =
            rightIdentityProperty x

        // some example-based tests as well
        static member ``1 + 2 = 3``() =
            add 1 2 = 3

        static member ``1 + 2 = 2 + 1``() =
            add 1 2 = add 2 1

        static member ``42 + 0 = 0 + 42``() =
            add 42 0 = add 0 42
    //<

// ===============================
// Using FsCheck from NUnit
// ===============================

//>nugetFsCheckNUnit
#r "nuget:FsCheck.NUnit"
open FsCheck.NUnit
//<

//>fscheck_nunit
open NUnit.Framework
open FsCheck
open FsCheck.NUnit

[<Property(QuietOnSuccess = true)>]
let ``Commutative`` x y =
    commutativeProperty x y

[<Property(Verbose= true)>]
let ``Associative`` x y z =
    associativeProperty x y z

[<Property(EndSize=300)>]
let ``Left Identity`` x =
    leftIdentityProperty x
//<



