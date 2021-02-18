// ==============================================
// Script file to extract source fragments from a file
// ==============================================

(*

Context:
* In a source file, code fragments to use are delimited with special comments and also given an id.

This library will:
* 1. Extract fragments from the code file.

*)

// How it works
// 1) Extracting fragments from code
// * Look for lines containing "//>fragment-id" or "(*>fragment-id"
//      and continue to the first line containing "//<" or "<*)".
//      All text between these lines will be exported as a fragment
// * The fragment-id must have no spaces and no characters that cannot be used as a filename (e.g. !)
// * The tool will adjust tab indentation to 2 chars, suitable for publishing


// ==============================================


open System
open System.IO
open System.Text.RegularExpressions

#load "Logger.fsx"
open Logger

//=======================================
// Fragment parsing
//=======================================

type FragmentId = string
type FragmentLines = string list
type Fragment = {
    source: FileInfo
    id: FragmentId
    content: FragmentLines
    }

// extend with a new property of Dir/Name
type FileInfo
    with
    member this.Post =
        Path.Combine(this.Directory.Name,this.Name)

/// Reformat a fragment by:
/// * changing the tabstops
/// * replacing certain expressions with non-compilable text (e.g. "...")
module ReformatFragment =

    type Indent = int
    type FragmentLine =
        | Empty
        | NonEmpty of string * Indent   // text and indent

    /// given a line, determine where the first non-whitespace is
    let determineIndent line =
        let indentPattern = @"^(\s*).*"
        let m = Regex.Match(line,indentPattern)
        m.Groups.[1].Value.Length
    (*
    determineIndent ""
    determineIndent "abc"
    determineIndent " abc"
    determineIndent "  abc"
    *)

    let toFragmentLine line =
        if String.IsNullOrWhiteSpace(line) then
            Empty
        else
            NonEmpty (line, determineIndent line)
    (*
    toFragmentLine ""
    toFragmentLine "  abc"
    *)

    type IndentMap = System.Collections.Generic.IDictionary<Indent, string>

    /// Find all the old indents for this code block
    /// and determine what the corresponding new one is.
    /// Eg [4;8;9] becomes [0;2;4]
    /// return old indent as key, and new indent as value
    let makeIndentMap tabStop fragmentLines :IndentMap =
        fragmentLines
        // ignore blank lines in calculation
        |> List.choose (function Empty -> None | NonEmpty (_,indent) -> Some indent)
        |> List.distinct
        |> List.sort
        |> List.mapi (fun i oldIndent ->
            let newIndent = i * tabStop
            oldIndent, String.replicate newIndent " ")
        |> dict
    (*
    [""; "    4";"     5";"    4";"      6";]
    |> List.map toFragmentLine
    |> makeIndentMap 2
    *)

    let changeIndent (indentMap:IndentMap) fragmentLine =
        let indentPattern = @"^\s*"
        match fragmentLine with
        | Empty -> ""
        | NonEmpty (line,oldIndent) ->
            let newIndentStr = indentMap.[oldIndent]
            Regex.Replace(line,indentPattern,replacement=newIndentStr)
    (*
    let map =
        [""; "    4";"     5";"    4";"      6";]
        |> List.map toFragmentLine
        |> makeIndentMap 2

    "      6" |> toFragmentLine |> changeIndent map
    *)

    let replaceSpecial (s:string) =
        s
         .Replace("//...","...")
         .Replace("PrivateDotDotDot","private ...")
         .Replace("DotDotDot","...")
         .Replace("dotDotDot()","...")
         .Replace("dotDotDot","...")
         .Replace("question()","???")


    // reformat the fragment lines with a smaller tabstop
    let reformatFragmentContent tabStop contentLines =
        let fragmentLines =
            contentLines
            |> List.map toFragmentLine

        let indentMap =
            fragmentLines
            |> makeIndentMap tabStop

        fragmentLines
        |> List.map (changeIndent indentMap)
        |> List.map replaceSpecial

    (*
    let tabStop = 2
    let actual = reformatFragmentContent tabStop []
    let expected = []
    if actual <> expected then failwithf "Error with %A" expected

    let actual = reformatFragmentContent tabStop ["depth0"; "depth0"]
    let expected = ["depth0"; "depth0"]
    if actual <> expected then failwithf "Error with %A" expected

    let actual = reformatFragmentContent tabStop ["depth0"; "    depth4"; "     depth5"; "    depth4"; "     depth5"; "     depth5"; "depth0"]
    let expected = ["depth0"; "  depth4"; "    depth5"; "  depth4"; "    depth5"; "    depth5";   "depth0"]
    if actual <> expected then failwithf "Error with %A" expected

    let actual = reformatFragmentContent tabStop ["    depth4"; "     depth5"; "    depth4"; "     depth5"; "     depth5"]
    let expected = ["depth4"; "  depth5"; "depth4"; "  depth5"; "  depth5"]
    if actual <> expected then failwithf "Error with %A" expected

    *)

let reformatFragment tabStop fragment =
    {fragment with
        content =
            fragment.content
            |> ReformatFragment.reformatFragmentContent tabStop
    }

/// Classify a line in a F# file
let (|StartFragment|EndFragment|NormalLine|) line  =

    // regular expressions
    let startOneLineComment = Regex.Escape "//>"
    let endOneLineComment = Regex.Escape "//<"
    let startBlockComment = Regex.Escape "(*>"
    let endBlockComment = Regex.Escape "<*)"

    // Fragment start is: start line, spaces, then either of the start patterns, then a sequence of non blank chars
    let startFragmentPattern = sprintf @"^\s*(%s|%s)(\S+)" startOneLineComment startBlockComment
    let fragmentNameGroup = 2  // the group number in the pattern above

    // Fragment end is: start line, spaces, then either of the end patterns
    let endFragmentPattern = sprintf @"^\s*(%s|%s)" endOneLineComment endBlockComment

    let startFragmentResult = Regex.Match(line,startFragmentPattern)
    if startFragmentResult.Success then
        let fragmentId = startFragmentResult.Groups.[fragmentNameGroup].Value
        StartFragment fragmentId
    else
        let endFragmentResult = Regex.Match(line,endFragmentPattern)
        if endFragmentResult .Success then
            EndFragment
        else
            NormalLine
(*
let testRegex = function
    | StartFragment name -> printfn "StartFragment %s" name
    | EndFragment -> printfn "EndFragment"
    | NormalLine -> printfn "NormalLine "
let left = "(" + "*"
let right = "*" + ")"  // to avoid parsing errors in Ionide :(
testRegex "   //>mySnip 2  "
testRegex "   //<233   "
testRegex ("   " + left + ">mySnip 2  ")
testRegex ("   <" + right + "   ")
// comments without special tag should be ignored
testRegex "   //   "
testRegex left
testRegex right
*)

/// State of fragment parser
type FragmentParserState = {
    fi:FileInfo
    fragmentsSoFar : Fragment list
    partialFragment : (FragmentId * FragmentLines) option
    lineNo: int
    }

let processLine tabStop state line  =
    let fi = state.fi
    let lineNo = state.lineNo + 1

    match line, state.partialFragment with
    | StartFragment newFragmentId, partialFragmentOpt ->
        // check for unclosed
        partialFragmentOpt
        |> Option.iter (fun (fragmentId,_) -> logWarn (sprintf "%s, %i: WARNING: unclosed fragment '%s'" fi.Post lineNo fragmentId))

        // start a new fragment
        logDebug (sprintf "%s, %i: Starting Fragment %s" fi.Post lineNo newFragmentId)
        let partialFragment' = newFragmentId,[]
        {state with lineNo=lineNo; partialFragment=Some partialFragment'}

    | EndFragment, Some (id,lines)->
        // finished existing fragment
        logDebug (sprintf "%s, %i: ...Finished Fragment %s " fi.Post lineNo id)
        let fragment' =
            {source=fi; id=id; content=lines |> List.rev} // reverse the list of strings
            |> reformatFragment tabStop  // and reformat

        // create a new state with fragment added to fragmentsSoFar
        let fragmentsSoFar' = fragment'::state.fragmentsSoFar
        {state with lineNo=lineNo; fragmentsSoFar=fragmentsSoFar'; partialFragment=None}

    | EndFragment, None ->
        // unopened
        logWarn (sprintf "%s, %i: WARNING: closing unopened fragment" fi.Post lineNo)
        // state is unchanged
        {state with lineNo=lineNo}

    | NormalLine, Some (id,lines) ->
        // if there is a current fragment, add the current line to it
        let partialFragment' = id, (line::lines)
        {state with lineNo=lineNo; partialFragment=Some partialFragment'}

    | NormalLine, None ->
        {state with lineNo=lineNo; partialFragment=None}

/// Process an entire file
let processFile tabStop (fi:FileInfo) =
    logInfo (sprintf "%s: Processing Fragments" fi.Post)

    let initialState = {fi=fi; fragmentsSoFar=[]; partialFragment=None; lineNo=0}
    let finalState =
        File.ReadAllLines(fi.FullName)
        |> Array.fold (processLine tabStop) initialState

    // warn if there is a dangling fragment when the end of the file is reached
    finalState.partialFragment
    |> Option.iter (fun (fragmentId,_) -> logWarn (sprintf "%s, %i: WARNING: unclosed fragment '%s'" fi.Post finalState.lineNo fragmentId))

    finalState.fragmentsSoFar

(*
System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

let fi = FileInfo "test.txt"

let writeTestFile() =
    let fileContent = """
line1
line2
  //>snipA
  lineA1
  //<
line5
  //>snipB
  //>snipC
lineC1
  lineC2
lineC3
  //<
  //>snipD
    """

    File.WriteAllText(fi.FullName,fileContent)

let processTestFile() =
    debugOn <- true
    let tabStop = 2
    processFile tabStop fi

writeTestFile()
processTestFile()

*)


let copyNonFragment (textWriter:TextWriter) line  =
    match line with
    | StartFragment newFragmentId ->
        () // do nothing
    | EndFragment ->
        () // do nothing
    | NormalLine ->
        textWriter.WriteLine(line) // copy the line

/// Process an entire file
let removeFragmentsFromFile (context:string) (source:FileInfo) (target:FileInfo) =
    logInfo (sprintf "%s: Removing Fragments and writing to '%s' " source.Post target.FullName)

    let header = """
//=====================================================================
// Source code related to post: {0}
//
// THIS IS A GENERATED FILE. DO NOT EDIT.
// To suggest changes to this file, see instructions at
// https://github.com/swlaschin/fsharpforfunandprofit.com
//=====================================================================
    """
    let header = String.Format(header.Trim(),context)

    use textWriter = new StreamWriter(path=target.FullName)

    // write header
    textWriter.WriteLine(header)

    // write body
    File.ReadAllLines(source.FullName)
    |> Array.iter (copyNonFragment textWriter)

(*
System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

let source = FileInfo "test.txt"
let target = FileInfo "test_out.txt"

let writeTestFile() =
    let fileContent = """
line1
line2
  //>snipA
  lineA1
  //<
line5
  //>snipB
  //>snipC
lineC1
  lineC2
lineC3
  //<
  //>snipD
    """

    File.WriteAllText(source.FullName,fileContent)


writeTestFile()
let context = "my context"
removeFragmentsFromFile context source target

*)
