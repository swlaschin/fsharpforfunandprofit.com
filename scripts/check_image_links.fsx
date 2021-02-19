open System
open System.IO

#load "Logger.fsx" 
open Logger

let staticDir = Path.Combine(__SOURCE_DIRECTORY__,"../static") |> Path.GetFullPath
let contentDir = Path.Combine(__SOURCE_DIRECTORY__,"../content") |> Path.GetFullPath

/// Return the abs path of the local image.
/// If the path is relative, it is relative to the post
/// If the path is rooted, it is relative to the /static directory
let makeSearchPaths (post:FileInfo) (imageSrc:string) =
    if Path.IsPathRooted imageSrc then
        let imageSrc = imageSrc.Substring(1,imageSrc.Length-1)
        [Path.Combine(staticDir, imageSrc); Path.Combine(contentDir, imageSrc)]
    else
        [Path.Combine(post.DirectoryName, imageSrc)]
    |> List.map Path.GetFullPath

(*
// test
let post = Path.Combine(contentDir,"posts/tuples/index.md") |> FileInfo
makeSearchPaths post "abc.jpg"
makeSearchPaths post "./abc.jpg"
makeSearchPaths post "/assets/abc.jpg"
*)


// Return Some if the imageSrc is not found in the context of the FileInfo
let tryMissingLink context (post:FileInfo) (imageSrc:string) =
    let imgPaths = makeSearchPaths post imageSrc
    if imgPaths |> List.exists File.Exists then
        None
    else
        let postName = Path.Combine(post.Directory.Name,post.Name)
        Some (sprintf "'%s': %s '%s'" postName context imageSrc)

(*
// test
let post = Path.Combine(contentDir,"posts/tuples/index.md") |> FileInfo
tryMissingLink "error" post "tuple_int_int.png"
tryMissingLink "error" post "./tuple_int_int.png"
tryMissingLink "error" post "/assets/tuple_int_int.png"
tryMissingLink "error" post "/posts/tuples/tuple_int_int.png"
*)

let checkMarkdownLinks (post:FileInfo) =
    let text = File.ReadAllText(post.FullName)
    Text.RegularExpressions.Regex.Matches(text,"\!\[.*?\]\((.*?)\)")
    |> Seq.cast<Text.RegularExpressions.Match>
    |> Seq.choose (fun m ->
        let link = m.Groups.[1].Value
        tryMissingLink "markdown" post link
        )
    |> Seq.iter (fun msg -> Logger.logWarn msg)

(*
// test
let post = Path.Combine(contentDir,"posts/tuples/index.md") |> FileInfo
checkMarkdownLinks post
*)

let checkFrontMatter (post:FileInfo) =
    let text = File.ReadAllText(post.FullName)
    Text.RegularExpressions.Regex.Matches(text,"image:\s*\"(.*?)\"")
    |> Seq.cast<Text.RegularExpressions.Match>
    |> Seq.choose (fun m ->
        let link = m.Groups.[1].Value
        tryMissingLink "frontmatter"  post link
        )
    |> Seq.iter (fun msg -> Logger.logWarn msg)

(*
// test
let post = Path.Combine(contentDir,"posts/tuples/index.md") |> FileInfo
checkFrontMatter post
*)

let rec checkDirectory (di:DirectoryInfo) =
    //logInfo (sprintf "Checking dir: %s" di.FullName)
    for fi in di.EnumerateFiles("*.md") do
        //logInfo (sprintf "Checking file: %s" fi.FullName)
        checkMarkdownLinks fi
        checkFrontMatter fi
    for di in di.EnumerateDirectories() do
        checkDirectory di


checkDirectory (DirectoryInfo contentDir)
