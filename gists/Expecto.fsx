// ======================================
// A simple test framework based on Expecto
// ======================================
open System
open System.Diagnostics

[<AutoOpen>]
module Domain =
    type ExpectoException(msg) = inherit Exception(msg)
    type AssertException(msg) = inherit ExpectoException(msg)
    type FailedException(msg) = inherit ExpectoException(msg)
    type IgnoreException(msg) = inherit ExpectoException(msg)

    /// Actual test function; either an async one, or a synchronous one.
    type TestCode =
      | Sync of stest: (unit -> unit)
      | Async of atest: Async<unit>

    /// Test tree – this is how you compose your tests as values. Since
    /// any of these can act as a test, you can pass any of these DU cases
    /// into a function that takes a Test.
    type Test =
      /// A test case is a function from unit to unit, that can be executed
      /// by Expecto to run the test code.
      | TestCase of code:TestCode
      /// A collection/list of tests.
      | TestList of tests:Test list
      /// A labelling of a Test (list or test code).
      | TestLabel of label:string * test:Test

    [<ReferenceEquality>]
    type FlatTest = {
        name      : string list
        test      : TestCode
        }
    with
        member x.shouldSkipEvaluation =
            false  // not supported here
        member x.fullName (joinWith: string) =
            String.concat joinWith x.name

    let rec private exnWithInnerMsg (ex: exn) msg =
      let currentMsg =
        msg + (sprintf "%s%s" Environment.NewLine (ex.ToString()))
      if isNull ex.InnerException then
        currentMsg
      else
        exnWithInnerMsg ex.InnerException currentMsg

    type TestResult =
      | Passed
      | Ignored of string
      | Failed of string
      | Error of exn
      override x.ToString() =
        match x with
        | Passed -> "Passed"
        | Ignored reason -> "Ignored: " + reason
        | Failed error -> "Failed: " + error
        | Error e -> "Exception: " + exnWithInnerMsg e ""
      member x.tag =
        match x with
        | Passed -> 0
        | Ignored _ -> 1
        | Failed _ -> 2
        | Error _ -> 3
      member x.order =
        match x with
        | Ignored _ -> 0
        | Passed -> 1
        | Failed _ -> 2
        | Error _ -> 3
      member x.isPassed =
        match x with
        | Passed -> true
        | _ -> false
      member x.isIgnored =
        match x with
        | Ignored _ -> true
        | _ -> false
      member x.isFailed =
        match x with
        | Failed _ -> true
        | _ -> false
      member x.isException =
        match x with
        | Error _ -> true
        | _ -> false
      static member max (a:TestResult) (b:TestResult) =
        if a.tag>=b.tag then a else b

    type TestSummary =
       { result        : TestResult
         count         : int
         meanDuration  : float
         maxDuration   : float }
        member x.duration = TimeSpan.FromMilliseconds x.meanDuration
        static member single result duration =
          { result        = result
            count         = 1
            meanDuration  = duration
            maxDuration   = duration }
        static member (+) (s:TestSummary, (r,x): TestResult*float) =
          { result        = TestResult.max s.result r
            count         = s.count + 1
            meanDuration  =
              s.meanDuration + (x-s.meanDuration)/float(s.count + 1)
            maxDuration   = max s.maxDuration x }

    type TestRunSummary =
      { results     : (FlatTest * TestSummary) list
        duration    : TimeSpan
        maxMemory   : int64
        memoryLimit : int64
        timedOut    : FlatTest list
      }
      static member fromResults results =
        { results  = results
          duration =
              results
              |> List.sumBy (fun (_,r:TestSummary) -> r.meanDuration)
              |> TimeSpan.FromMilliseconds
          maxMemory = 0L
          memoryLimit = 0L
          timedOut = [] }
      member x.passed = List.filter (fun (_,r) -> r.result.isPassed) x.results
      member x.ignored = List.filter (fun (_,r) -> r.result.isIgnored) x.results
      member x.failed = List.filter (fun (_,r) -> r.result.isFailed) x.results
      member x.errored = List.filter (fun (_,r) -> r.result.isException) x.results
      member x.errorCode =
        (if List.isEmpty x.failed then 0 else 1) |||
        (if List.isEmpty x.errored then 0 else 2) |||
        (if x.maxMemory <= x.memoryLimit then 0 else 4) |||
        (if List.isEmpty x.timedOut then 0 else 8)
      member x.successful = x.errorCode = 0

    let joinWithformat (parts: string list) =
        let by = "."
        String.concat by parts

    /// Hooks to print report through test run
    [<ReferenceEquality>]
    type TestPrinters =
      { /// Called before a test run (e.g. at the top of your main function)
        beforeRun: Test -> Async<unit>
        /// Called before atomic test (TestCode) is executed.
        beforeEach: string -> Async<unit>
        /// info
        info: string -> Async<unit>
        /// test name -> time taken -> unit
        passed: string -> TimeSpan -> Async<unit>
        /// test name -> ignore message -> unit
        ignored: string -> string -> Async<unit>
        /// test name -> other message -> time taken -> unit
        failed: string -> string -> TimeSpan -> Async<unit>
        /// test name -> exception -> time taken -> unit
        exn: string -> exn -> TimeSpan -> Async<unit>
        /// Prints a summary given the test result counts
        summary : TestRunSummary -> Async<unit> }

      static member printResult (printer:TestPrinters) (test:FlatTest) (result:TestSummary) =
        let name = joinWithformat test.name
        match result.result with
        | Passed -> printer.passed name result.duration
        | Failed message -> printer.failed name message result.duration
        | Ignored message -> printer.ignored name message
        | Error e -> printer.exn name e result.duration


// Extra functions for Async
module AsyncUtil =

    /// Lift a function to Async
    let map f xA =
        async {
        let! x = xA
        return f x
        }

    /// Apply an Async function to an Async value
    let apply fA xA =
        async {
         // start the two asyncs in parallel
        let! fChild = Async.StartChild fA  // run in parallel
        let! x = xA
        // wait for the result of the first one
        let! f = fChild
        return f x
        }

    /// Convert a list of Async into a Async<list> using monadic composition.
    /// All the errors are returned. The error type must be a list.
    let sequence (asyncList:Async<'a> list) :Async<'a list> =
        let (<*>) = apply
        let (<!>) = map
        let cons head tail = head::tail
        let consA headA tailA = cons <!> headA <*> tailA
        let initialValue = async.Return [] // empty list inside Async

        // loop through the list, prepending each element
        // to the initial value
        List.foldBack consA asyncList initialValue


module Impl =

    let joinWithformat (parts: string list) =
        let by = "."
        String.concat by parts

    /// Flattens a tree of tests
    let toTestCodeList (test:Test) :FlatTest list =
      let rec loop parentName testList =
        function
        | TestLabel (name, test) ->
          let fullName =
            if List.isEmpty parentName
              then [name]
              else parentName @ [name]
          loop fullName testList test
        | TestCase (test) ->
          { name=parentName
            test=test
            } :: testList
        | TestList (tests) ->
            List.collect (loop parentName testList) tests
      loop [] [] test

    // per-thread global state for current running test
    type private TestNameHolder() =
      [<ThreadStatic;DefaultValue>]
      static val mutable private name : string
      static member Name
          with get () = TestNameHolder.name
          and  set name = TestNameHolder.name <- name

    let printer : TestPrinters =
        let logger level msg = async {printfn "%s%s" level msg }
        {
            beforeRun = fun _tests -> logger "Info " "Running tests..."
            beforeEach = fun n -> logger "" (sprintf "'%s' starting..." n)
            info = fun s -> logger "Info " s
            passed = fun n d -> logger "" (sprintf "'%s' passed in %O." n d)
            ignored = fun n m -> logger "" (sprintf "'%s' was ignored. %s" n m)
            failed = fun n m d -> logger "Error " (sprintf "'%s' failed in %O. %s" n d m)
            exn = fun n e d -> logger "Error " (sprintf "'%s' errored in %O. %s" n d e.Message)
            summary = fun summary ->
                let splitSign = "."
                let spirit = if summary.successful then "Success!" else System.String.Empty
                let commonAncestor =
                  let rec loop (ancestor: string) (descendants : string list) =
                    match descendants with
                    | [] -> ancestor
                    | hd::tl when hd.StartsWith(ancestor)->
                      loop ancestor tl
                    | _ ->
                      if ancestor.Contains(splitSign) then
                        loop (ancestor.Substring(0, ancestor.LastIndexOf splitSign)) descendants
                      else
                        "miscellaneous"

                  let parentNames =
                    summary.results
                    |> List.map (fun (flatTest, _)  ->
                      if flatTest.name.Length > 1 then
                        let size = flatTest.name.Length - 1
                        joinWithformat flatTest.name.[0..size]
                      else
                        joinWithformat flatTest.name )

                  match parentNames with
                  | [x] -> x
                  | hd::tl ->
                    loop hd tl
                  | _ -> "miscellaneous" //we can't get here

                let inline commaString (i:int) = i.ToString("#,##0")
                let total = summary.results |> List.sumBy (fun (_,r) -> if r.result.isIgnored then 0 else r.count) |> commaString
                let passes = summary.passed |> List.sumBy (fun (_,r) -> r.count) |> commaString
                let ignores = summary.ignored |> List.sumBy (fun (_,r) -> r.count) |> commaString
                let failures = summary.failed |> List.sumBy (fun (_,r) -> r.count) |> commaString
                let errors = summary.errored |> List.sumBy (fun (_,r) -> r.count) |> commaString
                logger "" (sprintf "%s tests run in '%s' for %O – %s passed, %s ignored, %s failed, %s errored. %s" total commonAncestor summary.duration passes ignores failures errors spirit)
            }

    // run a single test
    let execTestAsync (test:FlatTest) : Async<TestSummary> =
      async {
        let w = Stopwatch.StartNew()
        try
            TestNameHolder.Name <- joinWithformat test.name
            match test.test with
            | Sync test ->
              test()
            | Async test ->
              do! test
            w.Stop()
            return TestSummary.single Passed (float w.ElapsedMilliseconds)
        with
          | :? AssertException as e ->
            w.Stop()
            let msg =
              "\n" + e.Message + "\n" +
              (e.StackTrace.Split('\n')
               |> Seq.skipWhile (fun l -> l.StartsWith("   at Expecto.Expect."))
               |> Seq.truncate 5
               |> String.concat "\n")
            return TestSummary.single (Failed msg) (float w.ElapsedMilliseconds)
          | :? FailedException as e ->
            w.Stop()
            return TestSummary.single (Failed ("\n"+e.Message)) (float w.ElapsedMilliseconds)
          | :? IgnoreException as e ->
            w.Stop()
            return TestSummary.single (Ignored e.Message) (float w.ElapsedMilliseconds)
          | :? AggregateException as e when e.InnerExceptions.Count = 1 ->
            w.Stop()
            if e.InnerException :? IgnoreException then
              return TestSummary.single (Ignored e.InnerException.Message) (float w.ElapsedMilliseconds)
            else
              return TestSummary.single (Error e.InnerException) (float w.ElapsedMilliseconds)
          | e ->
            w.Stop()
            return TestSummary.single (Error e) (float w.ElapsedMilliseconds)
      }

    let execTestsAsync (tests:FlatTest list) =

        let evalTestAsync (test:FlatTest) =

            let beforeEach (test:FlatTest) =
              let name = joinWithformat test.name
              //printer.beforeEach name
              async.Return ()

            async {
              let! beforeAsync = beforeEach test |> Async.StartChild
              let! result = execTestAsync test
              do! beforeAsync
              do! TestPrinters.printResult printer test result
              return test,result
            }

        async {
            let w = Stopwatch.StartNew()
            let! results =
                tests
                |> List.map evalTestAsync
                |> AsyncUtil.sequence
            w.Stop()

            let testRunSummary : TestRunSummary = {
                results = results
                duration = w.Elapsed
                maxMemory = 0L
                memoryLimit = 0L
                timedOut = []
            }
            do! printer.summary testRunSummary

            return testRunSummary.errorCode
            }

[<AutoOpen>]
module Api =
    /// Fail this test
    let inline failtest msg = raise <| AssertException msg
    /// Fail this test with a formatted message
    let inline failtestf fmt = Printf.ksprintf failtest fmt

    // create a single test
    let inline testCase name test = TestLabel(name, TestCase (Sync test))
    // create a single async test
    let inline testCaseAsync name test = TestLabel(name, TestCase (Async test))
    // create a list of tests
    let inline testList name tests = TestLabel(name, TestList (tests))

    let runTests (tests:Test list) =
        let flatTests = tests |> List.collect Impl.toTestCodeList
        Impl.execTestsAsync flatTests |> Async.RunSynchronously

    let runTest (test:Test) =
        let flatTests = test |> Impl.toTestCodeList
        Impl.execTestsAsync flatTests |> Async.RunSynchronously

// This module is your main entry-point when asserting.
/// All expect-functions have the signature
///      actual -> expected -> string -> unit
/// leaving out expected when obvious from the function.
module Expect =

    /// Expects the two values to equal each other.
    let equal (actual: 'a) (expected: 'a) message =
        if expected <> actual then
          failtestf "%s. Actual value was not equal to %A but had expected them to be equal." message actual

    /// Expects the two values not to equal each other.
    let notEqual (actual : 'a) (expected : 'a) message =
      if expected = actual then
        failtestf "%s. Actual value was equal to %A but had expected them to be non-equal." message actual

    /// Expects the value to be a Result.Ok value
    /// and returns it or fails the test.
    let wantOk x message =
        match x with
        | Ok x -> x
        | Result.Error x ->
        failtestf "%s. Expected Ok, was Error(%A)." message x

    /// Expects the value to be a Result.Ok value.
    let isOk x message = wantOk x message |> ignore

    /// Expects the value to be a Result.Error value
    /// and returns it or fails the test.
    let wantError x message =
        match x with
        | Ok x ->
        failtestf "%s. Expected Error, was Ok(%A)." message x
        | Result.Error x -> x

    /// Expects the value to be a Result.Error value.
    let isError x message = wantError x message |> ignore

