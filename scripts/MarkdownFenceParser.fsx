// ==============================================
// Script file to insert source fragments into a blog post
// ==============================================

(*

Context:
* Each blog post has code blocks (e.g. fences). Each code block is given an id.

This library will:
* Parse the code blocks in the post and if the id matches, replace the code in that block
  using the lines in a code fragment with the same id

*)

// How it works
// * Look for blocks starting with ```fsharp src=#fragment-id"
// * Try to find a fragment with the same id in the fragment map
// * Copy the the contents of fragment in between the fences, replacing what was there before

// ==============================================


open System
open System.IO
open System.Text.RegularExpressions

#load "Logger.fsx" "FragmentParser.fsx"
open Logger

// import only the types we need
type FragmentId = FragmentParser.FragmentId
type Fragment = FragmentParser.Fragment

type FragmentMap = System.Collections.Generic.IDictionary<FragmentId,Fragment>

// extend with a new property of Dir/Name
type FileInfo
    with
    member this.Post =
        Path.Combine(this.Directory.Name,this.Name)

/// Classify a line in a blog post
let (|NamedFenceWithId|NamedFenceWithoutId|Fence|NormalLine|) line  =

    let namedFenceWithId = @"^```(\w+).*src=#(\w+)"
    let namedFenceWithIdGroup = 2  // the group number in the pattern above
    let namedFenceWithoutId = @"^```(\w+).*"
    let fence = @"^```\s*$"

    let namedFenceWithIdResult = Regex.Match(line,namedFenceWithId)
    let namedFenceWithoutIdResult = Regex.Match(line,namedFenceWithoutId)
    let fenceResult = Regex.Match(line,fence)

    if namedFenceWithIdResult.Success then
        let fragmentId = namedFenceWithIdResult.Groups.[namedFenceWithIdGroup].Value
        NamedFenceWithId fragmentId
    else if namedFenceWithoutIdResult.Success then
        NamedFenceWithoutId
    else if fenceResult.Success then
        Fence
    else
        NormalLine
(*
let testRegex = function
    | NamedFenceWithId id -> printfn "NamedFenceWithId %s" id
    | NamedFenceWithoutId -> printfn "NamedFenceWithoutId"
    | Fence -> printfn "Fence"
    | NormalLine -> printfn "NormalLine "
testRegex "```fsharp {src=#mySnip}"
testRegex "```fsharp abc=123 src=#mySnip"
testRegex "```text abc=123   "
testRegex "```    "
testRegex "```"
testRegex "abc"
*)

/// State of blog post parser
type PostParserState = {
    fi:FileInfo
    lineNo: int
    fragmentMap: FragmentMap
    currentFragmentOpt : Fragment option
    outputLines : ResizeArray<string>
    }

let processLine state line  =
    let fi = state.fi
    let fragmentMap = state.fragmentMap
    let outputLines = state.outputLines
    let lineNo = state.lineNo + 1

    match line, state.currentFragmentOpt with
    | NamedFenceWithId newFragmentId, currentFragmentOpt ->
        // check for unclosed fence
        currentFragmentOpt
        |> Option.iter (fun fragment -> logWarn (sprintf "%s, %i: WARNING: unclosed fence '%s'" fi.Post lineNo fragment.id))

        // add fence start
        outputLines.Add(line)

        if newFragmentId = "none" then
            // ignore without warning
            {state with lineNo=lineNo; currentFragmentOpt=None}
        else
            match fragmentMap.TryGetValue(newFragmentId) with
            | true, fragment ->
                // if the fragmentId is found in the map, then start a new fragment
                logDebug (sprintf "%s, %i: Found Fragment %s" fi.Post lineNo newFragmentId)
                {state with lineNo=lineNo; currentFragmentOpt=Some fragment }
            | false, _ ->
                // if the fragmentId is NOT found in the map, that's an warning
                logWarn (sprintf "%s, %i: WARNING: no fragment found with id '%s'" fi.Post lineNo newFragmentId)
                {state with lineNo=lineNo; currentFragmentOpt=None}

    | NamedFenceWithoutId, currentFragmentOpt ->
        // check for unclosed fence
        currentFragmentOpt
        |> Option.iter (fun fragment -> logWarn (sprintf "%s, %i: WARNING: unclosed fence '%s'" fi.Post lineNo fragment.id))

        // add fence start
        outputLines.Add(line)

        logWarn (sprintf "%s, %i: WARNING: named fence found without id" fi.Post lineNo)
        {state with lineNo=lineNo; currentFragmentOpt=None}

    | Fence, Some fragment ->
        // finished existing fragment
        logDebug (sprintf "%s, %i: ...Closed Fence for fragment %s " fi.Post lineNo fragment.id)

        // add fragment
        outputLines.AddRange(fragment.content)
        // add fence end
        outputLines.Add(line)

        // create a new state
        {state with lineNo=lineNo; currentFragmentOpt=None}

    | Fence, None ->

        // add fence end
        outputLines.Add(line)

        // state is unchanged
        {state with lineNo=lineNo}

    | NormalLine, Some _fragment ->
        // if there is a current fragment,
        // ignore the current line as it will be replaced
        {state with lineNo=lineNo}

    | NormalLine, None ->
        // if there is NOT current fragment just add the line
        outputLines.Add(line)
        {state with lineNo=lineNo}

/// Process an entire file
let processFile fragmentMap (fi:FileInfo) =
    logInfo (sprintf "%s: Processing Code Blocks" fi.Post)

    let initialState = {
        fi=fi
        outputLines=ResizeArray()
        currentFragmentOpt=None
        lineNo=0
        fragmentMap=fragmentMap
        }

    let finalState =
        File.ReadAllLines(fi.FullName)
        |> Array.fold processLine initialState

    // warn if there is a dangling fragment when the end of the file is reached
    finalState.currentFragmentOpt
    |> Option.iter (fun (fragment) -> logWarn (sprintf "%s, %i: WARNING: unclosed fence '%s'" fi.Post finalState.lineNo fragment.id))

    File.WriteAllLines(fi.FullName, finalState.outputLines)


(*
System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

let fi = FileInfo "test.txt"

let writeTestFile() =
    let fileContent = """
line1
line2
```fsharp src=#a
xxx
```
text
```fsharp src=#b
xxx
```
lineC1
```fsharp src=#c
xxx
```
text
```fsharp
xxx
```
text
    """

    File.WriteAllText(fi.FullName,fileContent)

let processTestFile() =
    debugOn <- true

    let fragments : Fragment list = [
        {source=fi; id="a"; content=["a1"; "a2"]}
        {source=fi; id="b"; content=["b1"; "b2"; "b3"]}
    ]
    let fragmentMap : FragmentMap =
        fragments
        |> List.map (fun f -> f.id, f)
        |> dict

    processFile fragmentMap fi

writeTestFile()
processTestFile()

*)


