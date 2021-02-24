
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

// place holders
let dotDotDot() = failwith "undefined"

(*
But here's a common problem. Everyone who sees a property-based testing tool
like FsCheck or QuickCheck thinks that it is amazing... but when it comes
time to start creating your own properties, the universal complaint is:
"what properties should I use? I can't think of any!"

## Categories for properties

In my experience, many properties can be discovered by using one of the seven approaches listed below.

* "Different paths, same destination"
* "There and back again"
* "Some things never change"
* "The more things change, the more they stay the same"
* "Solve a smaller problem first"
* "Hard to prove, easy to verify"
* "The test oracle"


*)

// =================================
// "Different paths, same destination"
// applied to a list sort
// =================================


//--------------------------
// addThenSort_isSameAs_sortThenAdd
//--------------------------

(*
Version 1

* Path 1: We add one to each element of the list, then sort.
* Path 2: We sort, then add one to each element of the list.
* Both lists should be equal.
*)

//>path_sort1
let addThenSort_eq_sortThenAdd sortFn aList =
    let add1 x = x + 1

    let result1 = aList |> sortFn |> List.map add1
    let result2 = aList |> List.map add1 |> sortFn
    result1 = result2
//<

//>path_sort1_check
let goodSort = List.sort
Check.Quick (addThenSort_eq_sortThenAdd goodSort)
// Ok, passed 100 tests.
//<


//>path_sort1_checkbad
let edfhSort1 aList = aList  // return the list unsorted!
Check.Quick (addThenSort_eq_sortThenAdd edfhSort1)
// Ok, passed 100 tests.
//<

//--------------------------
// Version 2: List sort with Int32.MinValue
//--------------------------

(*
* Path 1: We append `Int32.MinValue` to the *end* of the list, then sort.
* Path 2: We sort, then prepend `Int32.MinValue` to the *front* of the list.
* Both lists should be equal.
*)

//>path_sort2
let minValueThenSort_eq_sortThenMinValue sortFn aList =
    let minValue = Int32.MinValue

    let appendThenSort =
        (aList @ [minValue]) |> sortFn
    let sortThenPrepend =
        minValue :: (aList |> sortFn)
    appendThenSort = sortThenPrepend

// test
Check.Quick (minValueThenSort_eq_sortThenMinValue goodSort)
// Ok, passed 100 tests.
//<


//>path_sort2_check_edfhSort1
Check.Quick (minValueThenSort_eq_sortThenMinValue edfhSort1)
// Falsifiable, after 1 test (2 shrinks)
// [0]
//<

//>path_sort2_edfhSort2
// The Enterprise Developer From Hell strikes again
let edfhSort2 aList =
    match aList with
    | [] -> []
    | _ ->
        let head, tail = List.splitAt (aList.Length-1) aList
        let lastElem = tail.[0]
        // if the last element is Int32.MinValue,
        // then move it to front
        if (lastElem = Int32.MinValue) then
            lastElem :: head
        else
            // otherwise, do not sort the list!
            aList
//<


//>path_sort2_edfhSort2_check
// Oh dear, the bad implementation passes!
Check.Quick (minValueThenSort_eq_sortThenMinValue edfhSort2)
// Ok, passed 100 tests.
//<

//--------------------------
// Version 3 - List sort with negate
//--------------------------

//>path_sort3
let negateThenSort_eq_sortThenNegateThenReverse sortFn aList =
    let negate x = x * -1

    let negateThenSort =
        aList |> List.map negate |> sortFn
    let sortThenNegateAndReverse =
        aList |> sortFn |> List.map negate |> List.rev
    negateThenSort = sortThenNegateAndReverse
//<

//>path_sort3_check
// test the good implementation
Check.Quick (negateThenSort_eq_sortThenNegateThenReverse goodSort)
// Ok, passed 100 tests.

// test the first EDFH sort
Check.Quick (negateThenSort_eq_sortThenNegateThenReverse edfhSort1)
// Falsifiable, after 1 test (1 shrinks)
// [1; 0]

// test the second EDFH sort
Check.Quick (negateThenSort_eq_sortThenNegateThenReverse edfhSort2)
// Falsifiable, after 5 tests (3 shrinks)
// [1; 0]
//<

// =====================================
// "different paths, same destination" to a list reversal function
// =====================================

module Path1 =
    //>path_rev1
    let appendThenReverse_eq_reverseThenPrepend revFn anyValue aList =
        let appendThenReverse =
            (aList @ [anyValue]) |> revFn
        let reverseThenPrepend =
            anyValue :: (aList |> revFn)
        appendThenReverse = reverseThenPrepend
    //<

    //>path_rev1_check
    // Correct implementation suceeeds
    let goodReverse = List.rev
    Check.Quick (appendThenReverse_eq_reverseThenPrepend goodReverse)
    // Ok, passed 100 tests.

    // EDFH attempt #1 fails
    let edfhReverse1 aList = []    // incorrect implementation
    Check.Quick (appendThenReverse_eq_reverseThenPrepend edfhReverse1)
    // Falsifiable, after 1 test (2 shrinks)
    // true, []

    // EDFH attempt #2 fails
    let edfhReverse2 aList = aList  // incorrect implementation
    Check.Quick (appendThenReverse_eq_reverseThenPrepend edfhReverse2)
    // Falsifiable, after 1 test (1 shrinks)
    // true, [false]
    //<

// =====================================
// "There and back again"
// =====================================

module Path2 =
    //>inverseRev
    let reverseThenReverse_eq_original revFn aList =
        let reverseThenReverse = aList |> revFn |> revFn
        reverseThenReverse = aList
    //<


    //>inverseRev_check1
    let goodReverse = List.rev  // correct implementation
    Check.Quick (reverseThenReverse_eq_original goodReverse)
    // Ok, passed 100 tests.
    //<

    //>inverseRev_check2
    let edfhReverse aList = aList  // incorrect implementation
    Check.Quick (reverseThenReverse_eq_original edfhReverse)
    // Ok, passed 100 tests.
    //<

// =====================================
// "Hard to prove, easy to verify"
// =====================================

module StringSplit =
    //>hardConcat1
    let stringSplit (str:string) : string list = dotDotDot()
    //<

// actual implementation for tests below
let stringSplit (str:string) : string list =
    str.Split([|','|]) |> Array.toList

let concatProperty (inputString:string) =

    //>hardConcat2
    let tokens = stringSplit inputString

    // build a string from the tokens
    let recombinedString = tokens |> String.concat ","

    // compare with the original
    inputString = recombinedString
    //<


//>hardConcat3
let concatElementsOfSplitString_eq_originalString (strings:string list) =
    // make a string
    let inputString = strings |> String.concat ","

    // use the 'split' function on the inputString
    let tokens = stringSplit inputString

    // build a string from the tokens
    let recombinedString = tokens |> String.concat ","

    // compare the result with the original
    inputString = recombinedString
//<


//>hardConcat4
Check.Quick concatElementsOfSplitString_eq_originalString
// Ok, passed 100 tests.
//<

// ==================================
// "Hard to prove, easy to verify" for list sorting
// ==================================

// how can we apply this principle to a sorted list? What property is easy to verify?

module AdjacentPairs1 =

    //>hardList1
    let adjacentPairsAreOrdered sortFn aList =
        let pairs = aList |> sortFn |> Seq.pairwise
        pairs |> Seq.forall (fun (x,y) -> x <= y )
    //<

    (*
    //>hardList1_check
    let goodSort = List.sort
    Check.Quick (adjacentPairsAreOrdered goodSort)
    //<
    *)

    (*
    //>hardList1_out
    System.Exception: The type System.IComparable is not
                      handled automatically by FsCheck
    //<
    *)

module AdjacentPairs2 =

    //>hardList2
    let adjacentPairsAreOrdered sortFn (aList:int list) =
        let pairs = aList |> sortFn |> Seq.pairwise
        pairs |> Seq.forall (fun (x,y) -> x <= y )
    //<


    //>hardList2_check
    let goodSort = List.sort
    Check.Quick (adjacentPairsAreOrdered goodSort)
    // Ok, passed 100 tests.
    //<

module AdjacentPairs3  =

    //>hardList3
    let adjacentPairsAreOrdered sortFn (aList:string list) =
        let pairs = aList |> sortFn |> Seq.pairwise
        pairs |> Seq.forall (fun (x,y) -> x <= y )

    Check.Quick (adjacentPairsAreOrdered goodSort)
    // Ok, passed 100 tests.
    //<

    //>hardList4
    // EDFH implementation passes :(
    let edfhSort aList = []

    Check.Quick (adjacentPairsAreOrdered edfhSort )
    // Ok, passed 100 tests.
    //<

// ===============================
// "Some things never change"
// ===============================

module SortInvariant1 =

    //>invariant1
    let sortedHasSameLengthAsOriginal sortFn (aList:int list) =
        let sorted = aList |> sortFn
        List.length sorted = List.length aList
    //<


    //>invariant1_check1
    let goodSort = List.sort
    Check.Quick (sortedHasSameLengthAsOriginal goodSort )
    // Ok, passed 100 tests.
    //<

    //>invariant1_check2
    let edfhSort aList = []
    Check.Quick (sortedHasSameLengthAsOriginal edfhSort)
    // Falsifiable, after 1 test (1 shrink)
    // [0]
    //<

    //>invariant2
    // EDFH implementation has same length as original
    let edfhSort2 aList =
        match aList with
        | [] ->
            []
        | head::_ ->
            List.replicate (List.length aList) head

    // for example
    // edfhSort2 [1;2;3]  // => [1;1;1]
    //<


    //>invariant2_check1
    Check.Quick (sortedHasSameLengthAsOriginal edfhSort2 )
    // Ok, passed 100 tests.
    //<

    let adjacentPairsAreOrdered =
        AdjacentPairs2.adjacentPairsAreOrdered

    //>invariant2_check2
    Check.Quick (adjacentPairsAreOrdered edfhSort2)
    // Ok, passed 100 tests.
    //<

//==========================
// Sort invariant - 2nd attempt
//==========================


module SortInvariant2 =


    //>insertElement
    /// given aList and anElement to insert,
    /// generate all possible lists with anElement
    /// inserted into aList
    let rec distribute e list =
        match list with
        | [] -> [[e]]
        | x::xs' as xs -> (e::xs)::[for xs in distribute e xs' -> x::xs]

    /// Given a list, return all permutations of it
    let rec permute list =
        match list with
        | [] -> [[]]
        | e::xs -> List.collect (distribute e) (permute xs)
    //<

    //>permutations_out
    permute [1;2;3] |> Seq.toList
    //  [[1; 2; 3]; [2; 1; 3]; [2; 3; 1];
    //   [1; 3; 2]; [3; 1; 2]; [3; 2; 1]]

    permute [1;2;3;4] |> Seq.toList
    // [[1; 2; 3; 4]; [2; 1; 3; 4]; [2; 3; 1; 4]; [2; 3; 4; 1]; [1; 3; 2; 4];
    //  [3; 1; 2; 4]; [3; 2; 1; 4]; [3; 2; 4; 1]; [1; 3; 4; 2]; [3; 1; 4; 2];
    //  [3; 4; 1; 2]; [3; 4; 2; 1]; [1; 2; 4; 3]; [2; 1; 4; 3]; [2; 4; 1; 3];
    //  [2; 4; 3; 1]; [1; 4; 2; 3]; [4; 1; 2; 3]; [4; 2; 1; 3]; [4; 2; 3; 1];
    //  [1; 4; 3; 2]; [4; 1; 3; 2]; [4; 3; 1; 2]; [4; 3; 2; 1]]

    permute [3;3] |> Seq.toList
    //  [[3; 3]; [3; 3]]
    //<

    //>sortinvariant1
    let sortedListIsPermutationOfOriginal sortFn (aList:int list) =
        let sorted = aList |> sortFn
        let permutationsOfOriginalList = permute aList

        // the sorted list must be in the set of permutations
        permutationsOfOriginalList
        |> Seq.exists (fun permutation -> permutation = sorted)
    //<

    (*
    // DANGER! Be prepared to kill the FSI process.
    //>sortinvariant1_check
    Check.Quick (sortedListIsPermutationOfOriginal goodSort)
    //<
    *)

//==========================
// Sort invariant - 3rd attempt
//==========================

module SortInvariant3 =


    //>isPermutationOf
    /// Given an element and a list, and other elements previously skipped,
    /// return a new list without the specified element.
    /// If not found, return None

    /// Given an element and a list, return a new list
    /// without the first instance of the specified element.
    /// If element is not found, return None
    let withoutElement x aList =
        let folder (acc,found) elem =
            if elem = x && not found then
                acc, true  // start skipping
            else
                (elem::acc), found // accumulate

        let (filteredList,found) =
            aList |> List.fold folder ([],false)
        if found then
            filteredList |> List.rev |> Some
        else
            None


    /// Given two lists, return true if they have the same contents
    /// regardless of order
    let rec isPermutationOf list1 list2 =
        match list1 with
        | [] -> List.isEmpty list2 // if both empty, true
        | h1::t1 ->
            match withoutElement h1 list2 with
            | None -> false
            | Some t2 -> isPermutationOf t1 t2
    //<

    (*
    [] |> withoutElement 1
    [1] |> withoutElement 1
    [2] |> withoutElement 1
    [1;2] |> withoutElement 1
    [1;2;1] |> withoutElement 1


    [1;2;3] |> isPermutationOf [1;2]
    [1;2;3] |> isPermutationOf [1;2;3]
    [1;2;3] |> isPermutationOf [1;3;2]
    [1;2;3] |> isPermutationOf [3;2;1]
    [1;2;3] |> isPermutationOf [1;3;4]
    [1;2;3] |> isPermutationOf [1;2;3;4]
    *)

    //>sortinvariant2
    let sortedListIsPermutationOfOriginal sortFn (aList:int list) =
        let sorted = aList |> sortFn
        isPermutationOf aList sorted
    //<

    //>sortinvariant2_check
    Check.Quick (sortedListIsPermutationOfOriginal goodSort)
    // Ok, passed 100 tests.
    //<

    let edfhSort = SortInvariant1.edfhSort
    let edfhSort2 = SortInvariant1.edfhSort2

    //>sortinvariant2_check2
    Check.Quick (sortedListIsPermutationOfOriginal edfhSort)
    // Falsifiable, after 2 tests (4 shrinks)
    // [0]

    Check.Quick (sortedListIsPermutationOfOriginal edfhSort2)
    // Falsifiable, after 2 tests (5 shrinks)
    // [1; 0]
    //<

// ================================
// Sidebar: Combining properties
// ================================

module Combine =

    let adjacentPairsAreOrdered =
        AdjacentPairs2.adjacentPairsAreOrdered

    let sortedListIsPermutationOfOriginal =
        SortInvariant3.sortedListIsPermutationOfOriginal

    //>combine1
    let listIsSorted sortFn (aList:int list) =
        let prop1 = adjacentPairsAreOrdered sortFn aList
        let prop2 = sortedListIsPermutationOfOriginal sortFn aList
        prop1 .&. prop2
    //<


    //>combine2
    let goodSort = List.sort
    Check.Quick (listIsSorted goodSort )
    //<


    //>combine3
    let badSort aList = []
    Check.Quick (listIsSorted badSort )
    // Falsifiable, after 1 test (0 shrinks)
    // [0]
    //<

    //>combine4
    let listIsSorted_labelled sortFn (aList:int list) =
        let prop1 =
            adjacentPairsAreOrdered sortFn aList
            |@ "adjacent pairs from a list are ordered"
        let prop2 =
            sortedListIsPermutationOfOriginal sortFn aList
            |@ "sorted list is a permutation of original list"
        prop1 .&. prop2
    //<

    //>combine5
    Check.Quick (listIsSorted_labelled badSort )
    //  Falsifiable, after 1 test (2 shrinks)
    //  Label of failing property:
    //     sorted list is a permutation of original list
    //  [0]
    //<

//=========================
// "Solving a smaller problem"
//=========================

module Recurse =
    (*
    //>list_recurse1
    A list is sorted if:
    * The first element is smaller (or equal to) the second.
    * The rest of the elements after the first element are also sorted.
    //<
    *)

    //>list_recurse2
    let rec firstLessThanSecond_andTailIsSorted sortFn (aList:int list) =
        let sortedList = aList |> sortFn
        match sortedList with
        | [] -> true
        | [first] -> true
        | [first;second] -> first <= second
        | first::second::rest->
            first <= second &&
            let tail = second::rest
            // check that tail is sorted
            firstLessThanSecond_andTailIsSorted sortFn tail
    //<


    //>list_recurse3
    let goodSort = List.sort
    Check.Quick (firstLessThanSecond_andTailIsSorted goodSort )
    // Ok, passed 100 tests.
    //<

    //>list_recurse4
    let efdhSort aList = []
    Check.Quick (firstLessThanSecond_andTailIsSorted efdhSort)
    // Ok, passed 100 tests.

    let efdhSort2  aList =
        match aList with
        | [] -> []
        | head::_ -> List.replicate (List.length aList) head

    Check.Quick (firstLessThanSecond_andTailIsSorted efdhSort2)
    // Ok, passed 100 tests.
    //<

// =================================
// "The more things change, the more they stay the same"
// =================================

module Idem =
    //>idem_sort
    let sortTwice_eq_sortOnce sortFn (aList:int list) =
        let sortedOnce = aList |> sortFn
        let sortedTwice = aList |> sortFn |> sortFn
        sortedOnce = sortedTwice

    // test
    let goodSort = List.sort
    Check.Quick (sortTwice_eq_sortOnce goodSort )
    // Ok, passed 100 tests.
    //<


    //>idem_service1
    type IdempotentService() =
        let data = 0
        member this.Get() =
            data
    //<

    //>idem_service2
    type NonIdempotentService() =
        let mutable data = 0
        member this.Get() =
            data <- data + 1
            data
    //<

    //>idem_service_prop
    let idempotentServiceGivesSameResult get =
        // first GET
        let get1 = get()

        // second GET
        let get2 = get()
        get1 = get2
    //<

    do
        //>idem_service1_check
        let service = IdempotentService()
        let get() = service.Get()

        Check.Quick (idempotentServiceGivesSameResult get)
        // Ok, passed 100 tests.
        //<

    do
        //>idem_service2_check
        let service = NonIdempotentService()
        let get() = service.Get()

        Check.Quick (idempotentServiceGivesSameResult get)
        // Falsifiable, after 1 test
        //<


// ============================
// "Two heads are better than one"
// ============================

module Oracle =
    //>oracle1
    module InsertionSort =

        // Insert a new element into a list by looping over the list.
        // As soon as you find a larger element, insert in front of it
        let rec insert newElem list =
            match list with
            | head::tail when newElem > head ->
                head :: insert newElem tail
            | other -> // including empty list
                newElem :: other

        // Sorts a list by inserting the head into the rest of the list
        // after the rest have been sorted
        let rec sort list =
            match list with
            | []   -> []
            | head::tail ->
                insert head (sort tail)

        // test
        // sort  [5;3;2;1;1]
        //<

    //>oracle2
    let customSort_eq_insertionSort sortFn (aList:int list) =
        let sorted1 = aList |> sortFn
        let sorted2 = aList |> InsertionSort.sort
        sorted1 = sorted2
    //<


    //>oracle2_check1
    let goodSort = List.sort
    Check.Quick (customSort_eq_insertionSort goodSort)
    // Ok, passed 100 tests.
    //<


    //>oracle2_check2
    let edfhSort aList = aList
    Check.Quick (customSort_eq_insertionSort edfhSort)
    // Falsifiable, after 4 tests (6 shrinks)
    // [1; 0]
    //<

