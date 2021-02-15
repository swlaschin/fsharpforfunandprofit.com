open System
open System.IO

let staticDir = Path.Combine(__SOURCE_DIRECTORY__,"../static") |> Path.GetFullPath
let contentDir = Path.Combine(__SOURCE_DIRECTORY__,"../content") |> Path.GetFullPath

/// Return the abs path of the local image.
/// If the path is relative, it is relative to the post
/// If the path is rooted, it is relative to the /static directory
let makeAbsPath (post:FileInfo) (imageSrc:string) =
    if Path.IsPathRooted imageSrc then
        let imageSrc = imageSrc.Substring(1,imageSrc.Length-1)
        Path.Combine(staticDir, imageSrc)
    else
        Path.Combine(post.DirectoryName, imageSrc)
    |> Path.GetFullPath

(*
// test
let post = Path.Combine(contentDir,"posts/tuples/index.md") |> FileInfo
makeAbsPath post "abc.jpg"
makeAbsPath post "./abc.jpg"
makeAbsPath post "/assets/abc.jpg"
*)


// Return Some if the imageSrc is not found in the context of the FileInfo
let tryMissingLink context (post:FileInfo) (imageSrc:string) =
    let imgPath = makeAbsPath post imageSrc
    if File.Exists imgPath then
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
*)

let checkMarkdownLinks (post:FileInfo) =
    let text = File.ReadAllText(post.FullName)
    Text.RegularExpressions.Regex.Matches(text,"\!\[.*?\]\((.*?)\)")
    |> Seq.choose (fun m ->
        let link = m.Groups.[1].Value
        tryMissingLink "markdown" post link
        )

(*
// test
let post = Path.Combine(contentDir,"posts/tuples/index.md") |> FileInfo
checkMarkdownLinks post
*)

let checkFrontMatter (post:FileInfo) =
    let text = File.ReadAllText(post.FullName)
    Text.RegularExpressions.Regex.Matches(text,"image:\s*\"(.*?)\"")
    |> Seq.choose (fun m ->
        let link = m.Groups.[1].Value
        tryMissingLink "frontmatter"  post link
        )

(*
// test
let post = Path.Combine(contentDir,"posts/tuples/index.md") |> FileInfo
checkFrontMatter post
*)

let rec checkDirectory (di:DirectoryInfo) =
    seq {
        for fi in di.EnumerateFiles() do
            yield! checkMarkdownLinks fi
        for di in di.EnumerateDirectories() do
            yield! checkDirectory di
    }

checkDirectory (DirectoryInfo contentDir)
