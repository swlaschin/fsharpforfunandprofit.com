---
layout: post
title: "Trees in the real world"
description: "Examples using databases, JSON and error handling"
seriesId: "Recursive types and folds"
seriesOrder: 6
categories: [Folds, Patterns]
---

This post is the sixth in a series.

In the [previous post](/posts/recursive-types-and-folds-3/), we briefly looked at some generic types.

In this post, we'll do some deeper dives into some real-world examples of using trees and folds.

## Series contents

Here's the contents of this series:

* **Part 1: Introduction to recursive types and catamorphisms**
  * [A simple recursive type](/posts/recursive-types-and-folds/#basic-recursive-type)
  * [Parameterize all the things](/posts/recursive-types-and-folds/#parameterize)
  * [Introducing catamorphisms](/posts/recursive-types-and-folds/#catamorphisms)
  * [Benefits of catamorphisms](/posts/recursive-types-and-folds/#benefits)
  * [Rules for creating a catamorphism](/posts/recursive-types-and-folds/#rules)
* **Part 2: Catamorphism examples**  
  * [Catamorphism example: File system domain](/posts/recursive-types-and-folds-1b/#file-system)
  * [Catamorphism example: Product domain](/posts/recursive-types-and-folds-1b/#product)
* **Part 3: Introducing folds**    
  * [A flaw in our catamorphism implementation](/posts/recursive-types-and-folds-2/#flaw)
  * [Introducing `fold`](/posts/recursive-types-and-folds-2/#fold)
  * [Problems with fold](/posts/recursive-types-and-folds-2/#problems)
  * [Using functions as accumulators](/posts/recursive-types-and-folds-2/#functions)
  * [Introducing `foldback`](/posts/recursive-types-and-folds-2/#foldback)
  * [Rules for creating a fold](/posts/recursive-types-and-folds-2/#rules)
* **Part 4: Understanding folds**      
  * [Iteration vs. recursion](/posts/recursive-types-and-folds-2b/#iteration)
  * [Fold example: File system domain](/posts/recursive-types-and-folds-2b/#file-system)  
  * [Common questions about "fold"](/posts/recursive-types-and-folds-2b/#questions)
* **Part 5: Generic recursive types**  
  * [LinkedList: A generic recursive type](/posts/recursive-types-and-folds-3/#linkedlist)
  * [Making the Gift domain generic](/posts/recursive-types-and-folds-3/#revisiting-gift)
  * [Defining a generic Container type](/posts/recursive-types-and-folds-3/#container)
  * [A third way to implement the gift domain](/posts/recursive-types-and-folds-3/#another-gift)
  * [Abstract or concrete? Comparing the three designs](/posts/recursive-types-and-folds-3/#compare)
* **Part 6: Trees in the real world**  
  * [Defining a generic Tree type](/posts/recursive-types-and-folds-3b/#tree)
  * [The Tree type in the real world](/posts/recursive-types-and-folds-3b/#reuse)
  * [Mapping the Tree type](/posts/recursive-types-and-folds-3b/#map)
  * [Example: Creating a directory listing](/posts/recursive-types-and-folds-3b/#listing)
  * [Example: A parallel grep](/posts/recursive-types-and-folds-3b/#grep)
  * [Example: Storing the file system in a database](/posts/recursive-types-and-folds-3b/#database)
  * [Example: Serializing a Tree to JSON](/posts/recursive-types-and-folds-3b/#tojson)
  * [Example: Deserializing a Tree from JSON](/posts/recursive-types-and-folds-3b/#fromjson)
  * [Example: Deserializing a Tree from JSON - with error handling](/posts/recursive-types-and-folds-3b/#json-with-error-handling)


<a id="tree"></a>
<hr>

## Defining a generic Tree type

In this post, we'll be working with a generic `Tree` inspired by the `FileSystem` domain that we explored earlier.

Here was the original design:

```fsharp
type FileSystemItem =
    | File of FileInfo
    | Directory of DirectoryInfo
and FileInfo = {name:string; fileSize:int}
and DirectoryInfo = {name:string; dirSize:int; subitems:FileSystemItem list}
```

We can separate out the data from the recursion, and create a generic `Tree` type like this:

```fsharp
type Tree<'LeafData,'INodeData> =
    | LeafNode of 'LeafData
    | InternalNode of 'INodeData * Tree<'LeafData,'INodeData> seq
```

Notice that I have used `seq` to represent the subitems rather than `list`. The reason for this will become apparent shortly.

The file system domain can then be modelled using `Tree` by specifying `FileInfo` as data associated with a leaf node and `DirectoryInfo` as data associated with an internal node:

```fsharp
type FileInfo = {name:string; fileSize:int}
type DirectoryInfo = {name:string; dirSize:int}

type FileSystemItem = Tree<FileInfo,DirectoryInfo>
```

### `cata` and `fold` for Tree

We can define `cata` and `fold` in the usual way:

```fsharp
module Tree = 

    let rec cata fLeaf fNode (tree:Tree<'LeafData,'INodeData>) :'r = 
        let recurse = cata fLeaf fNode  
        match tree with
        | LeafNode leafInfo -> 
            fLeaf leafInfo 
        | InternalNode (nodeInfo,subtrees) -> 
            fNode nodeInfo (subtrees |> Seq.map recurse)

    let rec fold fLeaf fNode acc (tree:Tree<'LeafData,'INodeData>) :'r = 
        let recurse = fold fLeaf fNode  
        match tree with
        | LeafNode leafInfo -> 
            fLeaf acc leafInfo 
        | InternalNode (nodeInfo,subtrees) -> 
            // determine the local accumulator at this level
            let localAccum = fNode acc nodeInfo
            // thread the local accumulator through all the subitems using Seq.fold
            let finalAccum = subtrees |> Seq.fold recurse localAccum 
            // ... and return it
            finalAccum 
```

Note that I am *not* going to implement `foldBack` for the `Tree` type, because it's unlikely that the tree will get so deep as to cause a stack overflow.
Functions that need inner data can use `cata`.

### Modelling the File System domain with Tree

Let's test it with the same values that we used before:

```fsharp
let fromFile (fileInfo:FileInfo) = 
    LeafNode fileInfo 

let fromDir (dirInfo:DirectoryInfo) subitems = 
    InternalNode (dirInfo,subitems)

let readme = fromFile {name="readme.txt"; fileSize=1}
let config = fromFile {name="config.xml"; fileSize=2}
let build  = fromFile {name="build.bat"; fileSize=3}
let src = fromDir {name="src"; dirSize=10} [readme; config; build]
let bin = fromDir {name="bin"; dirSize=10} []
let root = fromDir {name="root"; dirSize=5} [src; bin]
```

The `totalSize` function is almost identical to the one in the previous post:

```fsharp
let totalSize fileSystemItem =
    let fFile acc (file:FileInfo) = 
        acc + file.fileSize
    let fDir acc (dir:DirectoryInfo)= 
        acc + dir.dirSize
    Tree.fold fFile fDir 0 fileSystemItem 

readme |> totalSize  // 1
src |> totalSize     // 16 = 10 + (1 + 2 + 3)
root |> totalSize    // 31 = 5 + 16 + 10
```

And so is the `largestFile` function:

```fsharp
let largestFile fileSystemItem =
    let fFile (largestSoFarOpt:FileInfo option) (file:FileInfo) = 
        match largestSoFarOpt with
        | None -> 
            Some file                
        | Some largestSoFar -> 
            if largestSoFar.fileSize > file.fileSize then
                Some largestSoFar
            else
                Some file

    let fDir largestSoFarOpt dirInfo = 
        largestSoFarOpt

    // call the fold
    Tree.fold fFile fDir None fileSystemItem

readme |> largestFile  
// Some {name = "readme.txt"; fileSize = 1}

src |> largestFile     
// Some {name = "build.bat"; fileSize = 3}

bin |> largestFile     
// None

root |> largestFile    
// Some {name = "build.bat"; fileSize = 3}
```

The source code for this section is available at [this gist](https://gist.github.com/swlaschin/1ef784481bae91b63a36).

<a id="reuse"></a>

## The Tree type in the real world

We can use the `Tree` to model the *real* file system too!  To do this,
just set the leaf node type to `System.IO.FileInfo` and the internal node type to `System.IO.DirectoryInfo`.

```fsharp
open System
open System.IO

type FileSystemTree = Tree<IO.FileInfo,IO.DirectoryInfo>
```

And let's create some helper methods to create the various nodes:

```fsharp
let fromFile (fileInfo:FileInfo) = 
    LeafNode fileInfo 

let rec fromDir (dirInfo:DirectoryInfo) = 
    let subItems = seq{
        yield! dirInfo.EnumerateFiles() |> Seq.map fromFile
        yield! dirInfo.EnumerateDirectories() |> Seq.map fromDir
        }
    InternalNode (dirInfo,subItems)
```

Now you can see why I used `seq` rather than `list` for the subitems. The `seq` is lazy, which means that we can create nodes 
without actually hitting the disk.

Here's the `totalSize` function again, this time using the real file information:

```fsharp
let totalSize fileSystemItem =
    let fFile acc (file:FileInfo) = 
        acc + file.Length
    let fDir acc (dir:DirectoryInfo)= 
        acc 
    Tree.fold fFile fDir 0L fileSystemItem 
```

Let's see what the size of the current directory is:

```fsharp
// set the current directory to the current source directory
Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

// get the current directory as a Tree
let currentDir = fromDir (DirectoryInfo("."))

// get the size of the current directory 
currentDir  |> totalSize  
```

Similarly, we can get the largest file:

```fsharp
let largestFile fileSystemItem =
    let fFile (largestSoFarOpt:FileInfo option) (file:FileInfo) = 
        match largestSoFarOpt with
        | None -> 
            Some file                
        | Some largestSoFar -> 
            if largestSoFar.Length > file.Length then
                Some largestSoFar
            else
                Some file

    let fDir largestSoFarOpt dirInfo = 
        largestSoFarOpt

    // call the fold
    Tree.fold fFile fDir None fileSystemItem

currentDir |> largestFile  
```

So that's one big benefit of using generic recursive types. If we can turn a real-world hierarchy into our tree structure, we can get all the benefits of fold "for free".

<a id="map"></a>

## Mapping with generic types

One other advantage of using generic types is that you can do things like `map` -- converting every element to a new type without changing the structure.

We can see this in action with the real world file system. But first we need to define `map` for the `Tree` type!

The implementation of `map` can also be done mechanically, using the following rules:

* Create a function parameter to handle each case in the structure.
* For non-recursive cases
  * First, use the function parameter to transform the non-recursive data associated with that case
  * Then wrap the result in the same case constructor
* For recursive cases, perform two steps:
  * First, use the function parameter to transform the non-recursive data associated with that case
  * Next, recursively `map` the nested values.
  * Finally, wrap the results in the same case constructor

Here's the implementation of `map` for `Tree`, created by following those rules:
  
```fsharp
module Tree = 

    let rec cata ...

    let rec fold ...

    let rec map fLeaf fNode (tree:Tree<'LeafData,'INodeData>) = 
        let recurse = map fLeaf fNode  
        match tree with
        | LeafNode leafInfo -> 
            let newLeafInfo = fLeaf leafInfo
            LeafNode newLeafInfo 
        | InternalNode (nodeInfo,subtrees) -> 
            let newNodeInfo = fNode nodeInfo
            let newSubtrees = subtrees |> Seq.map recurse 
            InternalNode (newNodeInfo, newSubtrees)
```

If we look at the signature of `Tree.map`, we can see that all the leaf data is transformed to type `'a`, all the node data is transformed to type `'b`,
and the final result is a `Tree<'a,'b>`.

```fsharp
val map :
  fLeaf:('LeafData -> 'a) ->
  fNode:('INodeData -> 'b) ->
  tree:Tree<'LeafData,'INodeData> -> 
  Tree<'a,'b>
```

We can define `Tree.iter` in a similar way:

```fsharp
module Tree = 

    let rec map ...

    let rec iter fLeaf fNode (tree:Tree<'LeafData,'INodeData>) = 
        let recurse = iter fLeaf fNode  
        match tree with
        | LeafNode leafInfo -> 
            fLeaf leafInfo
        | InternalNode (nodeInfo,subtrees) -> 
            subtrees |> Seq.iter recurse 
            fNode nodeInfo
```


<a id="listing"></a>
<hr>

## Example: Creating a directory listing

Let's say we want to use `map` to transform the file system into a directory listing - a tree of strings where each string has information
about the corresponding file or directory. Here's how we could do it:

```fsharp
let dirListing fileSystemItem =
    let printDate (d:DateTime) = d.ToString()
    let mapFile (fi:FileInfo) = 
        sprintf "%10i  %s  %-s"  fi.Length (printDate fi.LastWriteTime) fi.Name
    let mapDir (di:DirectoryInfo) = 
        di.FullName 
    Tree.map mapFile mapDir fileSystemItem
```

And then we can print the strings out like this:

```fsharp
currentDir 
|> dirListing 
|> Tree.iter (printfn "%s") (printfn "\n%s")
```

The results will look something like this:

```text
  8315  10/08/2015 23:37:41  Fold.fsx
  3680  11/08/2015 23:59:01  FoldAndRecursiveTypes.fsproj
  1010  11/08/2015 01:19:07  FoldAndRecursiveTypes.sln
  1107  11/08/2015 23:59:01  HtmlDom.fsx
    79  11/08/2015 01:21:54  LinkedList.fsx
```

*The source code for this example is available at [this gist](https://gist.github.com/swlaschin/77fadc19acb8cc850276).*

<a id="grep"></a>
<hr>

## Example: Creating a parallel grep

Let's look at a more complex example. I'll demonstrate how to create a parallel "grep" style search using `fold`.

The logic will be like this:

* Use `fold` to iterate through the files.
* For each file, if its name doesn't match the desired file pattern, return `None`.
* If the file is to be processed, then return an async that returns all the line matches in the file.
* Next, all these asyncs -- the output of the fold -- are aggregated into a sequence.
* The sequence of asyncs is transformed into a single one using `Async.Parallel` which returns a list of results.

Before we start writing the main code, we'll need some helper functions. 

First, a generic function that folds over the lines in a file asynchronously.
This will be the basis of the pattern matching.

```fsharp
/// Fold over the lines in a file asynchronously
/// passing in the current line and line number tothe folder function.
///
/// Signature:
///   folder:('a -> int -> string -> 'a) -> 
///   acc:'a -> 
///   fi:FileInfo -> 
///   Async<'a>
let foldLinesAsync folder acc (fi:FileInfo) = 
    async {
        let mutable acc = acc
        let mutable lineNo = 1
        use sr = new StreamReader(path=fi.FullName)
        while not sr.EndOfStream do
            let! lineText = sr.ReadLineAsync() |> Async.AwaitTask
            acc <- folder acc lineNo lineText 
            lineNo <- lineNo + 1
        return acc
    }
```

Next, a little helper that allows us to `map` over `Async` values:

```fsharp
let asyncMap f asyncX = async { 
    let! x = asyncX
    return (f x)  }
```

Now for the central logic. We will create a function that, given a `textPattern` and a `FileInfo`, will return a list of lines that match the textPattern, but asynchronously:

```fsharp
/// return the matching lines in a file, as an async<string list>
let matchPattern textPattern (fi:FileInfo) = 
    // set up the regex
    let regex = Text.RegularExpressions.Regex(pattern=textPattern)
    
    // set up the function to use with "fold"
    let folder results lineNo lineText =
        if regex.IsMatch lineText then
            let result = sprintf "%40s:%-5i   %s" fi.Name lineNo lineText
            result :: results
        else
            // pass through
            results
    
    // main flow
    fi
    |> foldLinesAsync folder []
    // the fold output is in reverse order, so reverse it
    |> asyncMap List.rev
```

And now for the `grep` function itself:

```fsharp
let grep filePattern textPattern fileSystemItem =
    let regex = Text.RegularExpressions.Regex(pattern=filePattern)

    /// if the file matches the pattern
    /// do the matching and return Some async, else None
    let matchFile (fi:FileInfo) =
        if regex.IsMatch fi.Name then
            Some (matchPattern textPattern fi)
        else
            None

    /// process a file by adding its async to the list
    let fFile asyncs (fi:FileInfo) = 
        // add to the list of asyncs
        (matchFile fi) :: asyncs 

    // for directories, just pass through the list of asyncs
    let fDir asyncs (di:DirectoryInfo)  = 
        asyncs 

    fileSystemItem
    |> Tree.fold fFile fDir []    // get the list of asyncs
    |> Seq.choose id              // choose the Somes (where a file was processed)
    |> Async.Parallel             // merge all asyncs into a single async
    |> asyncMap (Array.toList >> List.collect id)  // flatten array of lists into a single list
```

Let's test it!

```fsharp
currentDir 
|> grep "fsx" "LinkedList" 
|> Async.RunSynchronously
```

The result will look something like this:

```text
"                  SizeOfTypes.fsx:120     type LinkedList<'a> = ";
"                  SizeOfTypes.fsx:122         | Cell of head:'a * tail:LinkedList<'a>";
"                  SizeOfTypes.fsx:125     let S = size(LinkedList<'a>)";
"      RecursiveTypesAndFold-3.fsx:15      // LinkedList";
"      RecursiveTypesAndFold-3.fsx:18      type LinkedList<'a> = ";
"      RecursiveTypesAndFold-3.fsx:20          | Cons of head:'a * tail:LinkedList<'a>";
"      RecursiveTypesAndFold-3.fsx:26      module LinkedList = ";
"      RecursiveTypesAndFold-3.fsx:39              list:LinkedList<'a> ";
"      RecursiveTypesAndFold-3.fsx:64              list:LinkedList<'a> -> ";
```

That's not bad for about 40 lines of code. This conciseness is because we are using various kinds of `fold` and `map` which hide the recursion, allowing
us to focus on the pattern matching logic itself.

Of course, this is not at all efficient or optimized (an async for every line!), and so I wouldn't use it as a real implementation, but it does give you an idea of the power of fold.

*The source code for this example is available at [this gist](https://gist.github.com/swlaschin/137c322b5a46b97cc8be).*

<a id="database"></a>
<hr>

## Example: Storing the file system in a database

For the next example, let's look at how to store a file system tree in a database. I don't really know why you would want to do that, but
the principles would work equally well for storing any hierarchical structure, so I will demonstrate it anyway!

To model the file system hierarchy in the database, say that we have four tables:

* `DbDir` stores information about each directory.
* `DbFile` stores information about each file.
* `DbDir_File` stores the relationship between a directory and a file.
* `DbDir_Dir` stores the relationship between a parent directory and a child directory.

Here are the database table definitions:

```text
CREATE TABLE DbDir (
	DirId int IDENTITY NOT NULL,
	Name nvarchar(50) NOT NULL
)

CREATE TABLE DbFile (
	FileId int IDENTITY NOT NULL,
	Name nvarchar(50) NOT NULL,
	FileSize int NOT NULL
)

CREATE TABLE DbDir_File (
	DirId int NOT NULL,
	FileId int NOT NULL
)

CREATE TABLE DbDir_Dir (
	ParentDirId int NOT NULL,
	ChildDirId int NOT NULL
)
```

That's simple enough. But note that in order to save a directory completely along with its relationships to its child items, we first need the ids of all its children,
and each child directory needs the ids of its children, and so on.

This implies that we should use `cata` instead of `fold`, so that we have access to the data from the lower levels of the hierarchy.

### Implementing the database functions

We're not wise enough to be using the [SQL Provider](https://fsprojects.github.io/SQLProvider/) and so we have written our
own table insertion functions, like this dummy one:

```fsharp
/// Insert a DbFile record 
let insertDbFile name (fileSize:int64) =
    let id = nextIdentity()
    printfn "%10s: inserting id:%i name:%s size:%i" "DbFile" id name fileSize
```

In a real database, the identity column would be automatically generated for you, but for this example, I'll use a little helper function `nextIdentity`:

```fsharp
let nextIdentity =
    let id = ref 0
    fun () -> 
        id := !id + 1
        !id
        
// test
nextIdentity() // 1
nextIdentity() // 2
nextIdentity() // 3
```

Now in order to insert a directory, we need to first know all the ids of the files in the directory. This implies that the `insertDbFile` function should
return the id that was generated.

```fsharp
/// Insert a DbFile record and return the new file id
let insertDbFile name (fileSize:int64) =
    let id = nextIdentity()
    printfn "%10s: inserting id:%i name:%s size:%i" "DbFile" id name fileSize
    id
```

But that logic applies to the directories too:

```fsharp
/// Insert a DbDir record and return the new directory id
let insertDbDir name =
    let id = nextIdentity()
    printfn "%10s: inserting id:%i name:%s" "DbDir" id name
    id
```

But that's still not good enough. When the child ids are passed to the parent directory, it needs to distinguish between files and directories, because
the relations are stored in different tables.

No problem -- we'll just use a choice type to distinguish between them!

```fsharp
type PrimaryKey =
    | FileId of int
    | DirId of int
```

With this in place, we can complete the implementation of the database functions:

```fsharp
/// Insert a DbFile record and return the new PrimaryKey
let insertDbFile name (fileSize:int64) =
    let id = nextIdentity()
    printfn "%10s: inserting id:%i name:%s size:%i" "DbFile" id name fileSize
    FileId id

/// Insert a DbDir record and return the new PrimaryKey
let insertDbDir name =
    let id = nextIdentity()
    printfn "%10s: inserting id:%i name:%s" "DbDir" id name
    DirId id

/// Insert a DbDir_File record
let insertDbDir_File dirId fileId =
    printfn "%10s: inserting parentDir:%i childFile:%i" "DbDir_File" dirId fileId 

/// Insert a DbDir_Dir record
let insertDbDir_Dir parentDirId childDirId =
    printfn "%10s: inserting parentDir:%i childDir:%i" "DbDir_Dir" parentDirId childDirId
```

### Working with the catamorphism

As noted above, we need to use `cata` instead of `fold`, because we need the inner ids at each step.

The function to handle the `File` case is easy -- just insert it and return the `PrimaryKey`.

```fsharp
let fFile (fi:FileInfo) = 
    insertDbFile fi.Name fi.Length
```

The function to handle the `Directory` case will be passed the `DirectoryInfo` and a sequence of `PrimaryKey`s from the children that have already been inserted.

It should insert the main directory record, then insert the children, and then return the `PrimaryKey` for the next higher level:

```fsharp
let fDir (di:DirectoryInfo) childIds  = 
    let dirId = insertDbDir di.Name
    // insert the children
    // return the id to the parent
    dirId
```

After inserting the directory record and getting its id, for each child id, we insert either into the `DbDir_File` table or the `DbDir_Dir`,
depending on the type of the `childId`.

```fsharp
let fDir (di:DirectoryInfo) childIds  = 
    let dirId = insertDbDir di.Name
    let parentPK = pkToInt dirId 
    childIds |> Seq.iter (fun childId ->
        match childId with
        | FileId fileId -> insertDbDir_File parentPK fileId 
        | DirId childDirId -> insertDbDir_Dir parentPK childDirId 
    )
    // return the id to the parent
    dirId
```

Note that I've also created a little helper function `pkToInt` that extracts the integer id from the `PrimaryKey` type.

Here is all the code in one chunk:

```fsharp
open System
open System.IO

let nextIdentity =
    let id = ref 0
    fun () -> 
        id := !id + 1
        !id

type PrimaryKey =
    | FileId of int
    | DirId of int

/// Insert a DbFile record and return the new PrimaryKey
let insertDbFile name (fileSize:int64) =
    let id = nextIdentity()
    printfn "%10s: inserting id:%i name:%s size:%i" "DbFile" id name fileSize
    FileId id

/// Insert a DbDir record and return the new PrimaryKey
let insertDbDir name =
    let id = nextIdentity()
    printfn "%10s: inserting id:%i name:%s" "DbDir" id name
    DirId id

/// Insert a DbDir_File record
let insertDbDir_File dirId fileId =
    printfn "%10s: inserting parentDir:%i childFile:%i" "DbDir_File" dirId fileId 

/// Insert a DbDir_Dir record
let insertDbDir_Dir parentDirId childDirId =
    printfn "%10s: inserting parentDir:%i childDir:%i" "DbDir_Dir" parentDirId childDirId
    
let pkToInt primaryKey = 
    match primaryKey with
    | FileId fileId -> fileId 
    | DirId dirId -> dirId 

let insertFileSystemTree fileSystemItem =

    let fFile (fi:FileInfo) = 
        insertDbFile fi.Name fi.Length

    let fDir (di:DirectoryInfo) childIds  = 
        let dirId = insertDbDir di.Name
        let parentPK = pkToInt dirId 
        childIds |> Seq.iter (fun childId ->
            match childId with
            | FileId fileId -> insertDbDir_File parentPK fileId 
            | DirId childDirId -> insertDbDir_Dir parentPK childDirId 
        )
        // return the id to the parent
        dirId

    fileSystemItem
    |> Tree.cata fFile fDir 
```

Now let's test it:

```fsharp
// get the current directory as a Tree
let currentDir = fromDir (DirectoryInfo("."))

// insert into the database
currentDir 
|> insertFileSystemTree
```

The output should look something like this:

```text
     DbDir: inserting id:41 name:FoldAndRecursiveTypes
    DbFile: inserting id:42 name:Fold.fsx size:8315
DbDir_File: inserting parentDir:41 childFile:42
    DbFile: inserting id:43 name:FoldAndRecursiveTypes.fsproj size:3680
DbDir_File: inserting parentDir:41 childFile:43
    DbFile: inserting id:44 name:FoldAndRecursiveTypes.sln size:1010
DbDir_File: inserting parentDir:41 childFile:44
...
     DbDir: inserting id:57 name:bin
     DbDir: inserting id:58 name:Debug
 DbDir_Dir: inserting parentDir:57 childDir:58
 DbDir_Dir: inserting parentDir:41 childDir:57
```

You can see that the ids are being generated as the files are iterated over, and that each `DbFile` insert is followed by a `DbDir_File` insert.

*The source code for this example is available at [this gist](https://gist.github.com/swlaschin/3a416f26d873faa84cde).*


<a id="tojson"></a>
<hr>

## Example: Serializing a Tree to JSON

Let's look at another common challenge: serializing and deserializing a tree to JSON, XML, or some other format.

Let's use the Gift domain again, but this time, we'll model the `Gift` type as a tree.  That means we get to put more than one thing in a box!

### Modelling the Gift domain as a tree

Here are the main types again, but notice that the final `Gift` type is defined as a tree:

```fsharp
type Book = {title: string; price: decimal}
type ChocolateType = Dark | Milk | SeventyPercent
type Chocolate = {chocType: ChocolateType ; price: decimal}

type WrappingPaperStyle = 
    | HappyBirthday
    | HappyHolidays
    | SolidColor

// unified data for non-recursive cases
type GiftContents = 
    | Book of Book
    | Chocolate of Chocolate 

// unified data for recursive cases
type GiftDecoration = 
    | Wrapped of WrappingPaperStyle
    | Boxed 
    | WithACard of string

type Gift = Tree<GiftContents,GiftDecoration>
```

As usual, we can create some helper functions to assist with constructing a `Gift`:

```fsharp
let fromBook book = 
    LeafNode (Book book)

let fromChoc choc = 
    LeafNode (Chocolate choc)

let wrapInPaper paperStyle innerGift = 
    let container = Wrapped paperStyle 
    InternalNode (container, [innerGift])

let putInBox innerGift = 
    let container = Boxed
    InternalNode (container, [innerGift])

let withCard message innerGift = 
    let container = WithACard message
    InternalNode (container, [innerGift])

let putTwoThingsInBox innerGift innerGift2 = 
    let container = Boxed
    InternalNode (container, [innerGift;innerGift2])
```

And we can create some sample data:

```fsharp
let wolfHall = {title="Wolf Hall"; price=20m}
let yummyChoc = {chocType=SeventyPercent; price=5m}

let birthdayPresent = 
    wolfHall 
    |> fromBook
    |> wrapInPaper HappyBirthday
    |> withCard "Happy Birthday"
 
let christmasPresent = 
    yummyChoc
    |> fromChoc
    |> putInBox
    |> wrapInPaper HappyHolidays

let twoBirthdayPresents = 
    let thing1 = wolfHall |> fromBook 
    let thing2 = yummyChoc |> fromChoc
    putTwoThingsInBox thing1 thing2 
    |> wrapInPaper HappyBirthday

let twoWrappedPresentsInBox = 
    let thing1 = wolfHall |> fromBook |> wrapInPaper HappyHolidays
    let thing2 = yummyChoc |> fromChoc  |> wrapInPaper HappyBirthday
    putTwoThingsInBox thing1 thing2 
```

Functions like `description` now need to handle a *list* of inner texts, rather than one. We'll just concat the strings together with an `&` separator:

```fsharp
let description gift =

    let fLeaf leafData = 
        match leafData with
        | Book book ->
            sprintf "'%s'" book.title
        | Chocolate choc ->
            sprintf "%A chocolate" choc.chocType

    let fNode nodeData innerTexts = 
        let innerText = String.concat " & " innerTexts 
        match nodeData with
        | Wrapped style ->
            sprintf "%s wrapped in %A paper" innerText style
        | Boxed ->
            sprintf "%s in a box" innerText
        | WithACard message ->
            sprintf "%s with a card saying '%s'" innerText message 

    // main call
    Tree.cata fLeaf fNode gift  
```

Finally, we can check that the function still works as before, and that multiple items are handled correctly:

```fsharp
birthdayPresent |> description
// "'Wolf Hall' wrapped in HappyBirthday paper with a card saying 'Happy Birthday'"

christmasPresent |> description
// "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"

twoBirthdayPresents |> description
// "'Wolf Hall' & SeventyPercent chocolate in a box 
//   wrapped in HappyBirthday paper"

twoWrappedPresentsInBox |> description
// "'Wolf Hall' wrapped in HappyHolidays paper 
//   & SeventyPercent chocolate wrapped in HappyBirthday paper 
//   in a box"
```

### Step 1: Defining `GiftDto` 

Our `Gift` type consists of many discriminated unions. In my experience, these do not serialize well. In fact, most complex types do not serialize well!

So what I like to do is define [DTO](https://en.wikipedia.org/wiki/Data_transfer_object) types that are explicitly designed to be serialized well.
In practice this means that the DTO types are constrained as follows:

* Only record types should be used.
* The record fields should consist only primitive values such as `int`, `string` and `bool`.

By doing this, we also get some other advantages:

**We gain control of the serialization output.** These kinds of data types are handled the same by most serializers, while
"strange" things such as unions can be interpreted differently by different libraries.

**We have better control of error handling.** My number one rule when dealing with serialized data is "trust no one".
It's very common that the data is structured correctly but is invalid for the domain: supposedly non-null strings are null,
strings are too long, integers are outside the correct bounds, and so on.

By using DTOs, we can be sure that the deserialization step itself will work. Then, when we convert the DTO to a domain type, we can
do proper validation.

So, let's define some DTO types for out domain. Each DTO type will correspond to a domain type, so let's start with `GiftContents`.
We'll define a corresponding DTO type called `GiftContentsDto` as follows:

```fsharp
[<CLIMutableAttribute>]
type GiftContentsDto = {
    discriminator : string // "Book" or "Chocolate"
    // for "Book" case only
    bookTitle: string    
    // for "Chocolate" case only
    chocolateType : string // one of "Dark" "Milk" "SeventyPercent"
    // for all cases
    price: decimal
    }
```

Obviously, this quite different from the original `GiftContents`, so let's look at the differences:

* First, it has the `CLIMutableAttribute`, which allows deserializers to construct them using reflection.
* Second, it has a `discriminator` which indicates which case of the original union type is being used. Obviously, this string could be set to anything,
  so when converting from the DTO back to the domain type, we'll have to check that carefully!
* Next is a series of fields, one for every possible item of data that needs to be stored. For example, in the `Book` case, we need a `bookTitle`,
  while in the `Chocolate` case, we need the chocolate type. And finally the `price` field which is in both types.
  Note that the chocolate type is stored as a string as well, and so will also need special treatment when we convert from DTO to domain.

The `GiftDecorationDto` type is created in the same way, with a discriminator and strings rather than unions.

```fsharp
[<CLIMutableAttribute>]
type GiftDecorationDto = {
    discriminator: string // "Wrapped" or "Boxed" or "WithACard"
    // for "Wrapped" case only
    wrappingPaperStyle: string  // "HappyBirthday" or "HappyHolidays" or "SolidColor"   
    // for "WithACard" case only
    message: string  
    }
```

Finally, we can define a `GiftDto` type as being a tree that is composed of the two DTO types:

```fsharp
type GiftDto = Tree<GiftContentsDto,GiftDecorationDto>
```

### Step 2: Transforming a `Gift` to a `GiftDto` 

Now that we have this DTO type, all we need to do is use `Tree.map` to convert from a `Gift` to a `GiftDto`.
And in order to do that, we need to create two functions: one that converts from `GiftContents` to `GiftContentsDto` and one
that converts from `GiftDecoration` to `GiftDecorationDto`.

Here's the complete code for `giftToDto`, which should be self-explanatory:

```fsharp
let giftToDto (gift:Gift) :GiftDto =
    
    let fLeaf leafData :GiftContentsDto = 
        match leafData with
        | Book book ->
            {discriminator= "Book"; bookTitle=book.title; chocolateType=null; price=book.price}
        | Chocolate choc ->
            let chocolateType = sprintf "%A" choc.chocType
            {discriminator= "Chocolate"; bookTitle=null; chocolateType=chocolateType; price=choc.price}

    let fNode nodeData :GiftDecorationDto = 
        match nodeData with
        | Wrapped style ->
            let wrappingPaperStyle = sprintf "%A" style
            {discriminator= "Wrapped"; wrappingPaperStyle=wrappingPaperStyle; message=null}
        | Boxed ->
            {discriminator= "Boxed"; wrappingPaperStyle=null; message=null}
        | WithACard message ->
            {discriminator= "WithACard"; wrappingPaperStyle=null; message=message}

    // main call
    Tree.map fLeaf fNode gift  
```

You can see that the case (`Book`, `Chocolate`, etc.) is turned into a `discriminator` string and the `chocolateType` is also turned into a string, just
as explained above.

### Step 3: Defining a `TreeDto` 

I said above that a good DTO should be a record type. Well we have converted the nodes of the tree, but the tree *itself* is a union type!
We need to transform the `Tree` type as well, into say a `TreeDto` type.

How can we do this? Just as for the gift DTO types, we will create a record type which contains all the data for both cases. We could use a discriminator
field as we did before, but this time, since there are only two choices, leaf and internal node, I'll just check whether the values are null or not when deserializing.
If the leaf value is not null, then the record must represent the `LeafNode` case, otherwise the record must represent the `InternalNode` case.

Here's the definition of the data type:

```fsharp
/// A DTO that represents a Tree
/// The Leaf/Node choice is turned into a record
[<CLIMutableAttribute>]
type TreeDto<'LeafData,'NodeData> = {
    leafData : 'LeafData
    nodeData : 'NodeData
    subtrees : TreeDto<'LeafData,'NodeData>[] }
```

As before, the type has the `CLIMutableAttribute`. And as before, the type has fields to store the data from all possible choices.
The `subtrees` are stored as an array rather than a seq -- this makes the serializer happy! 

To create a `TreeDto`, we use our old friend `cata` to assemble the record from a regular `Tree`.

```fsharp
/// Transform a Tree into a TreeDto
let treeToDto tree : TreeDto<'LeafData,'NodeData> =
    
    let fLeaf leafData  = 
        let nodeData = Unchecked.defaultof<'NodeData>
        let subtrees = [||]
        {leafData=leafData; nodeData=nodeData; subtrees=subtrees}

    let fNode nodeData subtrees = 
        let leafData = Unchecked.defaultof<'NodeData>
        let subtrees = subtrees |> Seq.toArray 
        {leafData=leafData; nodeData=nodeData; subtrees=subtrees}

    // recurse to build up the TreeDto
    Tree.cata fLeaf fNode tree 
```

Note that in F#, records are not nullable, so I am using `Unchecked.defaultof<'NodeData>` rather than `null` to indicate missing data.

Note also that I am assuming that `LeafData` or `NodeData` are reference types.
If `LeafData` or `NodeData` are ever value types like `int` or `bool`, then this approach will break down, because you won't be able to
tell the difference between a default value and a missing value. In which case, I'd switch to a discriminator field as before.

Alternatively, I could have used an `IDictionary`. That would be less convenient to deserialize, but would avoid the need for null-checking.

### Step 4: Serializing a `TreeDto` 

Finally we can serialize the `TreeDto` using a JSON serializer. 

For this example, I am using the built-in `DataContractJsonSerializer` so that I don't need to take
a dependency on a NuGet package. There are other JSON serializers that might be better for a serious project.

```fsharp
#r "System.Runtime.Serialization.dll"

open System.Runtime.Serialization
open System.Runtime.Serialization.Json

let toJson (o:'a) = 
    let serializer = new DataContractJsonSerializer(typeof<'a>)
    let encoding = System.Text.UTF8Encoding()
    use stream = new System.IO.MemoryStream()
    serializer.WriteObject(stream,o) 
    stream.Close()
    encoding.GetString(stream.ToArray())
```

### Step 5: Assembling the pipeline

So, putting it all together, we have the following pipeline:

* Transform `Gift` to `GiftDto` using `giftToDto`,<br>
  that is, use `Tree.map` to go from `Tree<GiftContents,GiftDecoration>` to `Tree<GiftContentsDto,GiftDecorationDto>`
* Transform `Tree` to `TreeDto` using `treeToDto`,<br>
  that is, use `Tree.cata` to go from `Tree<GiftContentsDto,GiftDecorationDto>` to `TreeDto<GiftContentsDto,GiftDecorationDto>`
* Serialize `TreeDto` to a JSON string

Here's some example code:

```fsharp
let goodJson = christmasPresent |> giftToDto |> treeToDto |> toJson  
```

And here is what the JSON output looks like:

```text
{
  "leafData@": null,
  "nodeData@": {
    "discriminator@": "Wrapped",
    "message@": null,
    "wrappingPaperStyle@": "HappyHolidays"
  },
  "subtrees@": [
    {
      "leafData@": null,
      "nodeData@": {
        "discriminator@": "Boxed",
        "message@": null,
        "wrappingPaperStyle@": null
      },
      "subtrees@": [
        {
          "leafData@": {
            "bookTitle@": null,
            "chocolateType@": "SeventyPercent",
            "discriminator@": "Chocolate",
            "price@": 5
          },
          "nodeData@": null,
          "subtrees@": []
        }
      ]
    }
  ]
}
```

The ugly `@` signs on the field names are an artifact of serializing the F# record type.
This can be corrected with a bit of effort, but I'm not going to bother right now!

*The source code for this example is available at [this gist](https://gist.github.com/swlaschin/bbe70c768215b209c06c)*

<a id="fromjson"></a>
<hr>

## Example: Deserializing a Tree from JSON

Now that we have created the JSON, what about going the other way and loading it into a `Gift`?

Simple! We just need to reverse the pipeline:

* Deserialize a JSON string into a `TreeDto`.
* Transform a `TreeDto` into a `Tree` to  using `dtoToTree`,<br>
  that is, go from `TreeDto<GiftContentsDto,GiftDecorationDto>` to `Tree<GiftContentsDto,GiftDecorationDto>`.
    We can't use `cata` for this -- we'll have to create a little recursive loop.
* Transform `GiftDto` to `Gift` using `dtoToGift`,<br>
  that is, use `Tree.map` to go from `Tree<GiftContentsDto,GiftDecorationDto>` to `Tree<GiftContents,GiftDecoration>`.

### Step 1: Deserializing a `TreeDto` 

We can deserialize the `TreeDto` using a JSON serializer.

```fsharp
let fromJson<'a> str = 
    let serializer = new DataContractJsonSerializer(typeof<'a>)
    let encoding = System.Text.UTF8Encoding()
    use stream = new System.IO.MemoryStream(encoding.GetBytes(s=str))
    let obj = serializer.ReadObject(stream) 
    obj :?> 'a
```

What if the deserialization fails? For now, we will ignore any error handling and let the exception propagate.

### Step 2: Transforming a `TreeDto` into a `Tree`

To transform a `TreeDto` into a `Tree` we recursively loop through the record and its subtrees, turning each one into a `InternalNode`
or a `LeafNode`, based on whether the appropriate field is null or not.

```fsharp
let rec dtoToTree (treeDto:TreeDto<'Leaf,'Node>) :Tree<'Leaf,'Node> =
    let nullLeaf = Unchecked.defaultof<'Leaf>
    let nullNode = Unchecked.defaultof<'Node>
    
    // check if there is nodeData present
    if treeDto.nodeData <> nullNode then
        if treeDto.subtrees = null then
            failwith "subtrees must not be null if node data present"
        else
            let subtrees = treeDto.subtrees |> Array.map dtoToTree 
            InternalNode (treeDto.nodeData,subtrees)
    // check if there is leafData present
    elif treeDto.leafData <> nullLeaf then
        LeafNode (treeDto.leafData) 
    // if both missing then fail
    else
        failwith "expecting leaf or node data"
```

As you can see, a number of things could go wrong:

* What if the `leafData` and `nodeData` fields are both null?
* What if the `nodeData` field is not null but the `subtrees` field *is* null?

Again, we will ignore any error handling and just throw exceptions (for now).

*Question: Could we create a `cata` for `TreeDto` that would make this code simpler? Would it be worth it?*

### Step 3: Transforming a `GiftDto` into `Gift`

Now we have a proper tree, we can use `Tree.map` again to convert each leaf and internal node from a DTO to the proper domain type.

That means we need functions that map a `GiftContentsDto` into a `GiftContents` and a `GiftDecorationDto` into a `GiftDecoration`.

Here's the complete code -- it's a lot more complicated than going in the other direction!

The code can be grouped as follows:

* Helper methods (such as `strToChocolateType`) that convert a string into a proper domain type and throw an exception if the input is invalid.
* Case converter methods (such as `bookFromDto`) that convert an entire DTO into a case.
* And finally, the `dtoToGift` function itself.  It looks at the `discriminator` field to see which case converter to call,
  and throws an exception if the discriminator value is not recognized.

```fsharp
let strToBookTitle str =
    match str with
    | null -> failwith "BookTitle must not be null" 
    | _ -> str

let strToChocolateType str =
    match str with
    | "Dark" -> Dark
    | "Milk" -> Milk
    | "SeventyPercent" -> SeventyPercent
    | _ -> failwithf "ChocolateType %s not recognized" str

let strToWrappingPaperStyle str =
    match str with
    | "HappyBirthday" -> HappyBirthday
    | "HappyHolidays" -> HappyHolidays
    | "SolidColor" -> SolidColor
    | _ -> failwithf "WrappingPaperStyle %s not recognized" str

let strToCardMessage str =
    match str with
    | null -> failwith "CardMessage must not be null" 
    | _ -> str

let bookFromDto (dto:GiftContentsDto) =
    let bookTitle = strToBookTitle dto.bookTitle
    Book {title=bookTitle; price=dto.price}

let chocolateFromDto (dto:GiftContentsDto) =
    let chocType = strToChocolateType dto.chocolateType 
    Chocolate {chocType=chocType; price=dto.price}

let wrappedFromDto (dto:GiftDecorationDto) =
    let wrappingPaperStyle = strToWrappingPaperStyle dto.wrappingPaperStyle
    Wrapped wrappingPaperStyle 

let boxedFromDto (dto:GiftDecorationDto) =
    Boxed

let withACardFromDto (dto:GiftDecorationDto) =
    let message = strToCardMessage dto.message
    WithACard message 

/// Transform a GiftDto to a Gift
let dtoToGift (giftDto:GiftDto) :Gift=

    let fLeaf (leafDto:GiftContentsDto) = 
        match leafDto.discriminator with
        | "Book" -> bookFromDto leafDto
        | "Chocolate" -> chocolateFromDto leafDto
        | _ -> failwithf "Unknown leaf discriminator '%s'" leafDto.discriminator 

    let fNode (nodeDto:GiftDecorationDto)  = 
        match nodeDto.discriminator with
        | "Wrapped" -> wrappedFromDto nodeDto
        | "Boxed" -> boxedFromDto nodeDto
        | "WithACard" -> withACardFromDto nodeDto
        | _ -> failwithf "Unknown node discriminator '%s'" nodeDto.discriminator 

    // map the tree
    Tree.map fLeaf fNode giftDto  
```

### Step 4: Assembling the pipeline

We can now assemble the pipeline that takes a JSON string and creates a `Gift`.

```fsharp
let goodGift = goodJson |> fromJson |> dtoToTree |> dtoToGift

// check that the description is unchanged
goodGift |> description
// "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

This works fine, but the error handling is terrible!

Look what happens if we corrupt the JSON a little:

```fsharp
let badJson1 = goodJson.Replace("leafData","leafDataXX")

let badJson1_result = badJson1 |> fromJson |> dtoToTree |> dtoToGift
// Exception "The data contract type 'TreeDto' cannot be deserialized because the required data member 'leafData@' was not found."
```

We get an ugly exception.

Or what if a discriminator is wrong?

```fsharp
let badJson2 = goodJson.Replace("Wrapped","Wrapped2")

let badJson2_result = badJson2 |> fromJson |> dtoToTree |> dtoToGift
// Exception "Unknown node discriminator 'Wrapped2'"
```

or one of the values for the WrappingPaperStyle DU?

```fsharp
let badJson3 = goodJson.Replace("HappyHolidays","HappyHolidays2")
let badJson3_result = badJson3 |> fromJson |> dtoToTree |> dtoToGift
// Exception "WrappingPaperStyle HappyHolidays2 not recognized"
```

We get lots of exceptions, and as as functional programmers, we should try to remove them whenever we can.

How we can do that will be discussed in the next section.

*The source code for this example is available at [this gist](https://gist.github.com/swlaschin/bbe70c768215b209c06c).*

<a id="json-with-error-handling"></a>
<hr>

## Example: Deserializing a Tree from JSON - with error handling

To address the error handling issue, we're going use the `Result` type shown below:

```fsharp
type Result<'a> = 
    | Success of 'a
    | Failure of string list
```

I'm not going to explain how it works here.
If you are not familar with this approach, please [read my post](/posts/recipe-part2/) or [watch my talk](/rop/) on the topic of functional error handling.

Let's revisit all the steps from the previous section, and use `Result` rather than throwing exceptions.

### Step 1: Deserializing a `TreeDto` 

When we deserialize the `TreeDto` using a JSON serializer we will trap exceptions and turn them into a `Result`.

```fsharp
let fromJson<'a> str = 
    try
        let serializer = new DataContractJsonSerializer(typeof<'a>)
        let encoding = System.Text.UTF8Encoding()
        use stream = new System.IO.MemoryStream(encoding.GetBytes(s=str))
        let obj = serializer.ReadObject(stream) 
        obj :?> 'a 
        |> Result.retn
    with
    | ex -> 
        Result.failWithMsg ex.Message
```

The signature of `fromJson` is now `string -> Result<'a>`.

### Step 2: Transforming a `TreeDto` into a `Tree`

As before, we transform a `TreeDto` into a `Tree` by recursively looping through the record and its subtrees, turning each one into a `InternalNode`
or a `LeafNode`. This time, though, we use `Result` to handle any errors.

```fsharp
let rec dtoToTreeOfResults (treeDto:TreeDto<'Leaf,'Node>) :Tree<Result<'Leaf>,Result<'Node>> =
    let nullLeaf = Unchecked.defaultof<'Leaf>
    let nullNode = Unchecked.defaultof<'Node>
    
    // check if there is nodeData present
    if treeDto.nodeData <> nullNode then
        if treeDto.subtrees = null then
            LeafNode <| Result.failWithMsg "subtrees must not be null if node data present"
        else
            let subtrees = treeDto.subtrees |> Array.map dtoToTreeOfResults 
            InternalNode (Result.retn treeDto.nodeData,subtrees) 
    // check if there is leafData present
    elif treeDto.leafData <> nullLeaf then
        LeafNode <| Result.retn (treeDto.leafData) 
    // if both missing then fail
    else
        LeafNode <| Result.failWithMsg "expecting leaf or node data"
        
// val dtoToTreeOfResults : 
//   treeDto:TreeDto<'Leaf,'Node> -> Tree<Result<'Leaf>,Result<'Node>>
```

But uh-oh, we now have a `Tree` where every internal node and leaf is wrapped in a `Result`.  It's a tree of `Results`!
The actual ugly signature is this: `Tree<Result<'Leaf>,Result<'Node>>`.

But this type is useless as it stands -- what we *really* want is to merge all the errors together and return a `Result` containing a `Tree`. 

How can we transform a Tree of Results into a Result of Tree?

The answer is to use a `sequence` function which "swaps" the two types.
You can read much more about `sequence` in [my series on elevated worlds](/posts/elevated-world-4/#sequence).

*Note that we could also use the slightly more complicated `traverse` variant to combine the `map` and `sequence` into one step,
but for the purposes of this demonstration, it's easier to understand if the steps are kept separate.*

We need to create our own `sequence` function for the Tree/Result combination. Luckily the creation of a sequence function
is a mechanical process:

* For the lower type (`Result`) we need to define `apply` and `return` functions. See [here for more details](/posts/elevated-world/#apply) on what `apply` means.
* For the higher type (`Tree`) we need to have a `cata` function, which we do.
* In the catamorphism, each constructor of the higher type (`LeafNode` and `InternalNode` in this case) is replaced by an equivalent that is "lifted" to the `Result` type (e.g. `retn LeafNode <*> data`)

Here is the actual code -- don't worry if you can't understand it immediately. Luckily, we only need to write it once for each combination
of types, so for any kind of Tree/Result combination in the future, we're set!

```fsharp
/// Convert a tree of Results into a Result of tree
let sequenceTreeOfResult tree =
    // from the lower level
    let (<*>) = Result.apply 
    let retn = Result.retn

    // from the traversable level
    let fLeaf data = 
        retn LeafNode <*> data

    let fNode data subitems = 
        let makeNode data items = InternalNode(data,items)
        let subItems = Result.sequenceSeq subitems 
        retn makeNode <*> data <*> subItems

    // do the traverse
    Tree.cata fLeaf fNode tree
    
// val sequenceTreeOfResult :
//    tree:Tree<Result<'a>,Result<'b>> -> Result<Tree<'a,'b>>
```

Finally, the actual `dtoToTree` function is simple -- just send the `treeDto` through `dtoToTreeOfResults` and then use `sequenceTreeOfResult` to
convert the final result into a `Result<Tree<..>>`, which is just what we need.

```fsharp
let dtoToTree treeDto =
    treeDto |> dtoToTreeOfResults |> sequenceTreeOfResult 
    
// val dtoToTree : treeDto:TreeDto<'a,'b> -> Result<Tree<'a,'b>>    
```

### Step 3: Transforming a `GiftDto` into a `Gift`

Again we can use `Tree.map` to convert each leaf and internal node from a DTO to the proper domain type.

But our functions will handle errors, so they need to map a `GiftContentsDto` into a `Result<GiftContents>`
and a `GiftDecorationDto` into a `Result<GiftDecoration>`. This results in a Tree of Results again, and so we'll have to
use `sequenceTreeOfResult` again to get it back into the correct `Result<Tree<..>>` shape.

Let's start with the helper methods (such as `strToChocolateType`) that convert a string into a proper domain type.
This time, they return a `Result` rather than throwing an exception.

```fsharp
let strToBookTitle str =
    match str with
    | null -> Result.failWithMsg "BookTitle must not be null"
    | _ -> Result.retn str

let strToChocolateType str =
    match str with
    | "Dark" -> Result.retn Dark
    | "Milk" -> Result.retn Milk
    | "SeventyPercent" -> Result.retn SeventyPercent
    | _ -> Result.failWithMsg (sprintf "ChocolateType %s not recognized" str)

let strToWrappingPaperStyle str =
    match str with
    | "HappyBirthday" -> Result.retn HappyBirthday
    | "HappyHolidays" -> Result.retn HappyHolidays
    | "SolidColor" -> Result.retn SolidColor
    | _ -> Result.failWithMsg (sprintf "WrappingPaperStyle %s not recognized" str)

let strToCardMessage str =
    match str with
    | null -> Result.failWithMsg "CardMessage must not be null" 
    | _ -> Result.retn str
```

The case converter methods have to build a `Book` or `Chocolate` from parameters that are `Result`s rather than normal values. This is
where lifting functions like `Result.lift2` can help.
For details on how this works, see [this post on lifting](/posts/elevated-world/#lift) and [this one on validation with applicatives](/posts/elevated-world-3/#validation).
  
```fsharp
let bookFromDto (dto:GiftContentsDto) =
    let book bookTitle price = 
        Book {title=bookTitle; price=price}

    let bookTitle = strToBookTitle dto.bookTitle
    let price = Result.retn dto.price
    Result.lift2 book bookTitle price 

let chocolateFromDto (dto:GiftContentsDto) =
    let choc chocType price = 
        Chocolate {chocType=chocType; price=price}

    let chocType = strToChocolateType dto.chocolateType 
    let price = Result.retn dto.price
    Result.lift2 choc chocType price 

let wrappedFromDto (dto:GiftDecorationDto) =
    let wrappingPaperStyle = strToWrappingPaperStyle dto.wrappingPaperStyle
    Result.map Wrapped wrappingPaperStyle 

let boxedFromDto (dto:GiftDecorationDto) =
    Result.retn Boxed

let withACardFromDto (dto:GiftDecorationDto) =
    let message = strToCardMessage dto.message
    Result.map WithACard message 
```

And finally, the `dtoToGift` function itself is changed to return a `Result` if the `discriminator` is invalid.  

As before, this mapping creates a Tree of Results, so we pipe the output of the `Tree.map` through `sequenceTreeOfResult` ...

```fsharp
`Tree.map fLeaf fNode giftDto |> sequenceTreeOfResult`
```

... to return a Result of Tree.

Here's the complete code for `dtoToGift`:

```fsharp
open TreeDto_WithErrorHandling

/// Transform a GiftDto to a Result<Gift>
let dtoToGift (giftDto:GiftDto) :Result<Gift>=
    
    let fLeaf (leafDto:GiftContentsDto) = 
        match leafDto.discriminator with
        | "Book" -> bookFromDto leafDto
        | "Chocolate" -> chocolateFromDto leafDto
        | _ -> Result.failWithMsg (sprintf "Unknown leaf discriminator '%s'" leafDto.discriminator) 

    let fNode (nodeDto:GiftDecorationDto)  = 
        match nodeDto.discriminator with
        | "Wrapped" -> wrappedFromDto nodeDto
        | "Boxed" -> boxedFromDto nodeDto
        | "WithACard" -> withACardFromDto nodeDto
        | _ -> Result.failWithMsg (sprintf "Unknown node discriminator '%s'" nodeDto.discriminator)

    // map the tree
    Tree.map fLeaf fNode giftDto |> sequenceTreeOfResult   
```

The type signature of `dtoToGift` has changed -- it now returns a `Result<Gift>` rather than just a `Gift`.

```fsharp
// val dtoToGift : GiftDto -> Result<GiftUsingTree.Gift>
```


### Step 4: Assembling the pipeline

We can now reassemble the pipeline that takes a JSON string and creates a `Gift`. 

But changes are needed to work with the new error handling code:

* The `fromJson` function returns a `Result<TreeDto>` but the next function in the pipeline (`dtoToTree`) expects a regular `TreeDto` as input. 
* Similarly `dtoToTree` returns a `Result<Tree>` but the next function in the pipeline (`dtoToGift`) expects a regular `Tree` as input. 

In both case, `Result.bind` can be used to solve that problem of mis-matched output/input. See [here for a more detailed discussion of bind](/posts/elevated-world-2/#bind).

Ok, let's try deserializing the `goodJson` string we created earlier.

```fsharp
let goodGift = goodJson |> fromJson |> Result.bind dtoToTree |> Result.bind dtoToGift

// check that the description is unchanged
goodGift |> description
// Success "SeventyPercent chocolate in a box wrapped in HappyHolidays paper"
```

That's fine. 

Let's see if the error handling has improved now.
We'll corrupt the JSON again:

```fsharp
let badJson1 = goodJson.Replace("leafData","leafDataXX")

let badJson1_result = badJson1 |> fromJson |> Result.bind dtoToTree |> Result.bind dtoToGift
// Failure ["The data contract type 'TreeDto' cannot be deserialized because the required data member 'leafData@' was not found."]
```

Great! We get an nice `Failure` case.

Or what if a discriminator is wrong?

```fsharp
let badJson2 = goodJson.Replace("Wrapped","Wrapped2")
let badJson2_result = badJson2 |> fromJson |> Result.bind dtoToTree |> Result.bind dtoToGift
// Failure ["Unknown node discriminator 'Wrapped2'"]
```

or one of the values for the WrappingPaperStyle DU?

```fsharp
let badJson3 = goodJson.Replace("HappyHolidays","HappyHolidays2")
let badJson3_result = badJson3 |> fromJson |> Result.bind dtoToTree |> Result.bind dtoToGift
// Failure ["WrappingPaperStyle HappyHolidays2 not recognized"]
```

Again, nice `Failure` cases. 

What's very nice (and this is something that the exception handling approach can't offer) is that if there is
more than one error, the various errors can be aggregated so that we get a list of *all* the things that went wrong, rather than just one error at a time.

Let's see this in action by introducing two errors into the JSON string:

```fsharp
// create two errors
let badJson4 = goodJson.Replace("HappyHolidays","HappyHolidays2")
                       .Replace("SeventyPercent","SeventyPercent2")
let badJson4_result = badJson4 |> fromJson |> Result.bind dtoToTree |> Result.bind dtoToGift
// Failure ["WrappingPaperStyle HappyHolidays2 not recognized"; 
//          "ChocolateType SeventyPercent2 not recognized"]
```

So overall, I'd say that's a success!

*The source code for this example is available at [this gist](https://gist.github.com/swlaschin/2b06fe266e3299a656c1).*

<hr>
    
## Summary 

We've seen in this series how to define catamorphisms, folds, and in this post in particular, how to use them to solve real world problems.
I hope these posts have been useful, and have provided you with some tips and insights that you can apply to your own code.

This series turned out to be a lot longer that I intended, so thanks for making it to the end! Cheers!



