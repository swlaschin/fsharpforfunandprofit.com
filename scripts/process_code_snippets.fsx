// ==============================================
// Script file to extract source fragments from a file and insert them into a blog post
// ==============================================

(*

Context:
* Each blog post has code blocks (e.g. fences). Each code block is given an id.
* Each blog post has a corresponding code file.
* In that file, code fragments to use are delimited with special comments and also given an id.

The tool will:
* 1. Extract fragments from the code file.
* 2. Parse the code blocks in the post and if the id matches, replace the code in that block

Also, it will copy the files to the fsharpforfunandprofit.com_code location
* The index.fsx file will have it's fragment delimiters removed and will be renamed
* All other .fsx files will be copied over unchanged

== Execution ==

The tool is run using `dotnet fsi process_code_snippets.fsx`
* If no file is specified the tool will process *all* files under the root directory.
  (The root directory is the `/content/` directory)
* If a post is specified the tool will process just that file

*)

// How it works
// 1) Extracting fragments from code
// * Look for lines containing "//>fragment-id" or "(*>fragment-id"
//      and continue to the first line containing "//<" or "<*)".
//      All text between these lines will be exported as a fragment
// * The fragment-id must have no spaces and no characters that cannot be used as a filename (e.g. !)
// * The tool will adjust tab indentation to 2 chars, suitable for publishing
//
// 2) Update the posts
// * Look for blocks starting with ```fsharp src=#fragment-id"
// * Copy the the contents of fragment in between the fences




// ==============================================

open System
open System.IO
open System.Text.RegularExpressions

#load "Logger.fsx" "FragmentParser.fsx" "MarkdownFenceParser.fsx"
open Logger


let postsDir = Path.Combine(__SOURCE_DIRECTORY__,@"../content/posts") |> DirectoryInfo

let processFile (postFile:FileInfo) =
    let tabStop = 2
    let codeFile = postFile.FullName.Replace(".md",".fsx") |> FileInfo
    let fragments = FragmentParser.processFile tabStop codeFile
    let fragmentMap =
        fragments
        |> List.map (fun f -> f.id,f)
        |> dict
    MarkdownFenceParser.processFile fragmentMap postFile

/// Copy index.fsx file to the fsharpforfunandprofit.com_code location, with fragment tags stripped
let copyIndexFileWithoutMarkup (postFile:FileInfo) =
    let codeFile = postFile.FullName.Replace(".md",".fsx") |> FileInfo
    let targetFile =
        codeFile.FullName
            .Replace(".com",".com_code")
            .Replace("content","")
            .Replace("index",codeFile.Directory.Name)
        |> FileInfo
    targetFile.Directory.Create()
    let context = sprintf "https://fsharpforfunandprofit.com/posts/%s" codeFile.Directory.Name
    FragmentParser.removeFragmentsFromFile context codeFile targetFile

/// Copy non-index code files (.fsx) to the fsharpforfunandprofit.com_code location
let copyNonIndexFile (sourceFile:FileInfo) =
    let targetFile =
        sourceFile.FullName
            .Replace(".com",".com_code")
            .Replace("content","")
        |> FileInfo
    targetFile.Directory.Create()
    File.Copy(sourceFile.FullName,targetFile.FullName,overwrite=true)


let rec processDirectory (d:DirectoryInfo) =

    d.EnumerateFiles("*.md")
    |> Seq.iter processFile

    // recurse down
    d.EnumerateDirectories()
    |> Seq.iter processDirectory


System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

(*
dotnet fsi .\process_code_snippets.fsx
or
dotnet fsi .\process_code_snippets.fsx abc
or to debug
dotnet fsi .\process_code_snippets.fsx -d abc
*)
let main() =
    let flagArgs = fsi.CommandLineArgs |> Array.filter (fun s -> s.StartsWith "-")
    if flagArgs |> Array.contains ("-d") then
        debugOn <- true

    let nonFlagArgs = fsi.CommandLineArgs |> Array.filter (fun s -> s.StartsWith "-" |> not)
    match nonFlagArgs with
    | [|_fsiPath|] ->
        logInfo (sprintf "processing all posts in '%s'" postsDir.FullName)
        processDirectory postsDir
    | [|_fsiPath; filename |] ->
        let dirPath = Path.Combine(postsDir.FullName,filename)
        let filePath =
            if Directory.Exists dirPath then
                Path.Combine(dirPath,"index.md")
            else
                dirPath
        let fileInfo = FileInfo filePath
        logInfo (sprintf "processing filename '%s'" fileInfo.FullName)
        processFile fileInfo

        // copy files to fsharpforfunandprofit.com_code location
        copyIndexFileWithoutMarkup fileInfo

        let di = DirectoryInfo dirPath
        di.EnumerateFiles("*.fsx")
        |> Seq.filter (fun fi -> fi.Name.Contains("index") |> not)
        |> Seq.iter copyNonIndexFile

        
    | _ ->
        logInfo "Pass 0 or 1 parameter. 0 for all posts; 1 for filename to process"

// CLI
main()