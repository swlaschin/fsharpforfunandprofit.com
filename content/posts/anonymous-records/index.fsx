// ======================================
// Companion file for index.md
//
// Use ../scripts/process_code_snippets.fsx to update the text
// ======================================


module Intro =

    // A record type is exactly that, a tuple where each element is labeled.
    //>construct1
    // a named record with an explicit type definition
    type Person = {First:string; Last:string}
    let person = {First="Alice"; Last="Jones"}
    // output:
    //   val person : Person = ...

    // an anonymous record without a type definition
    let contact = {|Name="Alice"; Email="a@example.com"|}
    // output:
    //   val contact : {| Email: string; Name: string |} = ...
    //<

module Working =

    //>deconstruct1
    let myGeoCoord = {| Lat = 1.1; Long = 2.2 |}
    // dotting works
    let lat = myGeoCoord.Lat
    let long = myGeoCoord.Long

    // pattern matching does NOT work
    let {| Lat=myLat; Long=myLong |} = myGeoCoord
    //   ^--- ERROR Unexpected symbol '{|' in binding
    //<

    //>copywith1
    let c1 = {| Name="Alice"; Email="a@example.com" |}
    let c2 = {| c1 with Name="Bob" |}
    //<

    //>copywith2
    type Person = {First:string; Last:string}
    let person = {First="Alice"; Last="Jones"}

    let p2 = {| person with Email="a@example.com" |}
    //<

    //>type1
    // Define an anonymous record
    let a = {| Id="A"; Email="a@example.com" |}

    // Define another anonymous record
    // It is the same type as `a`
    let b = {| Id="B"; Email="b@example.com" |}
    printfn "a=b is %b" (a=b)  // a=b is true
    //<

    //>type2
    let a1 = {| Id="A"; Email="a@example.com" |}
    // use the structure definition as the "name" of the type

    // here's the type "name" used to annotate a value
    let a2 : {| Id:string; Email:string |} = a1 // a2 is same type as a1

    // here's the type "name" used to annotate a function parameter
    let myFunc (x:{| Id:string; Email:string |}) =
        printfn "x is %A" x

    // this function can be called with any value of the same type
    myFunc a2
    //<

  module UnrelatedType =
    //>type3
    // Define an anonymous record
    let a = {| Id="A" |}

    // Define another anonymous record
    // It is NOT the same type as `a`
    let b : {| Id:string; Email:string |} = a
    //                                      ^--ERROR
    // ERROR: This anonymous record does not have enough fields.
    // Add the missing fields [Email].

    // Define another anonymous record based on `a`
    // It is NOT the same type as `a`
    let c = {| a with Email="a@example.com"|}
    printfn "a=c is %b" (a=c)  // error
    //                     ^--ERROR
    // ERROR: This anonymous record has too many fields.
    // Remove the extra fields [Email].
    //<


type Union =
  | A of {| Id: int; Name:string |}

type ComplexTuple = bool * {| Id: int; Name:string |}

type AnonymousRecord = {| Name : string; Age : int |}

// -------------------------------
// Using records in practice
// -------------------------------
module RecordsInPractice =

    //>practice1
    // the tuple version of TryParse
    let tryParseTuple intStr =
        try
            let i = System.Int32.Parse intStr
            (true,i)
        with _ ->
            (false,0)  // any exception

    // for the record version, create a type to hold the return result
    type TryParseResult = {Success:bool; Value:int}

    // the record version of TryParse
    let tryParseRecord intStr =
        try
            let i = System.Int32.Parse intStr
            {Success=true;Value=i}
        with _ ->
            {Success=false;Value=0}

    //test it
    tryParseTuple "99"   // (true, 99)
    tryParseRecord "99"  // { Success = true; Value = 99 }
    tryParseTuple "abc"  // (false, 0)
    tryParseRecord "abc" // { Success = false; Value = 0 }
    //<

    //>practice2
    //define return type
    type WordAndLetterCountResult = {WordCount:int; LetterCount:int}

    let wordAndLetterCount (s:string) =
       let words = s.Split [|' '|]
       let letterCount = words |> Array.sumBy (fun word -> word.Length )
       {WordCount=words.Length; LetterCount=letterCount}

    //test
    wordAndLetterCount "to be or not to be"
       // { WordCount = 6; LetterCount = 13 }
    //<

    // Creating records from other records

    type GeoCoord = { Lat: float; Long: float } // use colon in type

    //>practice3
    let addOneToGeoCoord aGeoCoord =
       let {Lat=x; Long=y} = aGeoCoord
       {Lat = x + 1.0; Long = y + 1.0}   // create a new one

    // try it
    addOneToGeoCoord {Lat=1.1; Long=2.2}
    //<


    //>practice4
    let addOneToGeoCoord {Lat=x; Long=y} = {Lat=x+1.0; Long=y+1.0}

    // try it
    addOneToGeoCoord {Lat=1.0; Long=2.0}
    //<

    //>practice5
    let addOneToGeoCoord aGeoCoord =
       {Lat=aGeoCoord.Lat + 1.0; Long= aGeoCoord.Long + 1.0}
    //<

    type Person = {First:string; Last:string}

    //>practice6
    let g1 = {Lat=1.1; Long=2.2}
    // create a new record based on g1
    let g2 = {g1 with Lat=99.9}

    let p1 = {First="Alice"; Last="Jones"}
    // create a new record based on p1
    let p2 = {p1 with Last="Smith"}
    //<

// -------------------------------
//  Record equality
// -------------------------------
module RecordEquality =

    type Person = {First:string; Last:string}

    //>equality1
    let p1 = {First="Alice"; Last="Jones"}
    let p2 = {First="Alice"; Last="Jones"}
    printfn "p1=p2 is %b" (p1=p2)  // p1=p2 is true
    //<

    //>equality2
    let h1 = {First="Alice"; Last="Jones"}.GetHashCode()
    let h2 = {First="Alice"; Last="Jones"}.GetHashCode()
    printfn "h1=h2 is %b" (h1=h2)  // h1=h2 is true
    //<

// -------------------------------
//  Record representation
// -------------------------------

    //>print1
    let p = {First="Alice"; Last="Jones"}
    printfn "%A" p
    // output:
    //   { First = "Alice"
    //     Last = "Jones" }
    printfn "%O" p   // same as above
    //<

    // Sidebar: %A vs. %O in print format strings
    module OverrideToString =

        //>print2
        type Person = {First:string; Last:string}
            with
            override this.ToString() = sprintf "%s %s" this.First this.Last

        printfn "%A" {First="Alice"; Last="Jones"}
        // output:
        //   { First = "Alice"
        //     Last = "Jones" }
        printfn "%O" {First="Alice"; Last="Jones"}
        // output:
        //   "Alice Jones"
        //<

