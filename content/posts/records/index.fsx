// ======================================
// Companion file for index.md
//
// Use ../scripts/process_code_snippets.fsx to update the text
// ======================================


module Intro =
    // A record type is exactly that, a tuple where each element is labeled.
    //>intro1
    type ComplexNumber = { Real: float; Imaginary: float }
    type GeoCoord = { Lat: float; Long: float }
    //<

    // Let's compare the "type syntax" for a record type with a tuple type:
    //>intro2
    type ComplexNumberRecord = { Real: float; Imaginary: float }
    type ComplexNumberTuple = float * float
    //<

// -------------------------------
// Making and matching records
// -------------------------------

module Constructing =
    // To create a record value, use a similar format to the type definition, but using equals signs after the labels. This is called a "record expression."

    //>construct1
    type ComplexNumberRecord = { Real: float; Imaginary: float }
    let myComplexNumber = { Real = 1.1; Imaginary = 2.2 } // use equals!

    type GeoCoord = { Lat: float; Long: float } // use colon in type
    let myGeoCoord = { Lat = 1.1; Long = 2.2 }  // use equals in let
    //<

    // And to "deconstruct" a record, use the same syntax:

    //>deconstruct1
    let myGeoCoord = { Lat = 1.1; Long = 2.2 }   // "construct"
    let { Lat=myLat; Long=myLong } = myGeoCoord  // "deconstruct"
    //<

    // if you don't need some of the values, you can use the underscore as a placeholder; or more cleanly, just leave off the unwanted label altogether.

    //>deconstruct2
    let { Lat=_; Long=myLong2 } = myGeoCoord  // "deconstruct"
    let { Long=myLong3 } = myGeoCoord         // "deconstruct"
    //<

    // If you just need a single property, you can use dot notation rather than pattern matching.

    //>deconstruct3
    let x = myGeoCoord.Lat
    let y = myGeoCoord.Long
    //<

    // Note that you can leave a label off when deconstructing, but not when constructing:

    //>construct2
    let myGeoCoord = { Lat = 1.1; }  // error FS0764: No assignment
                                     // given for field 'Long'
    //<


// -------------------------------
// Label order
// -------------------------------

module LabelOrder =
    open Constructing

    // Unlike tuples, the order of the labels is not important. So the following two values are the same:

    //>labelOrder
    let myGeoCoordA = { Lat = 1.1; Long = 2.2 }
    let myGeoCoordB = { Long = 2.2; Lat = 1.1 }   // same as above
    //<

// -------------------------------
// Naming Conflicts
// -------------------------------

module NamingConflicts =


    //  what happens if there are two record types with the same labels? How can the compiler know which one you mean?  The answer is that it can't -- it will use the most recently defined type, and in some cases, issue a warning.  Try evaluating the following:

    //>namingConflicts1
    type Person1 = {First:string; Last:string}
    type Person2 = {First:string; Last:string}
    let p = {First="Alice"; Last="Jones"} //
    //<

    //>namingConflicts2
    let {First=f; Last=l} = p
    // warning FS0667: The labels of this record do not
    //   uniquely determine a corresponding record type
    //<


    //>namingConflicts3
    let p = {Person1.First="Alice"; Last="Jones"}
    //  ^Person1
    //<


    //>namingConflicts4
    module Module1 =
      type Person = {First:string; Last:string}

    module Module2 =
      type Person = {First:string; Last:string}

    let p =
      {Module1.Person.First="Alice"; Module1.Person.Last="Jones"}
    //<

    module Module4 =
      //>namingConflicts4b
      let p : Module1.Person =
        {First="Alice"; Last="Jones"}
      //<


    //>namingConflicts5
    module Module3 =
      open Module1  // bring only one definition into scope
      let p = {First="Alice"; Last="Jones"} // will be Module1.Person
    //<

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

