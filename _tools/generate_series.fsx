// Generate a file for each series


open System
open System.Collections.Generic

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

// ========================================
// String Utilities
// ========================================
module StringUtilities = 

    let trim (str:string) =
        str.Trim()

    let removeDelimiters leftDelimiter rightDelimiter (str:string) =
        let mutable str = str
        if str.StartsWith(leftDelimiter) then str <- str.Remove(0,1)
        if str.EndsWith(rightDelimiter) then str <- str.Remove(str.Length-1,1)
        str

    let removeQuotes = trim >> removeDelimiters "\"" "\"" >> trim 
    let removeSquareBrackets = trim >> removeDelimiters "[" "]" >> trim 

    // "abc" |> removeQuotes 
    // "\"abc" |> removeQuotes 
    // "\"abc\"" |> removeQuotes 

    let toLower (s:string) = s.ToLowerInvariant()

    let splitYamlLine (line:string) = 
        match line.Split([|':'|],2) with
        | [|key;value|] ->
            let key = key |> trim 
            let value = value |> removeQuotes 
            Some (key,value)
        | _ -> 
            None
        // """description: "An overview of the benefits of F#" """ |> splitYamlLine 

    let strToInt defaultVal str =
        match Int32.TryParse(str) with
        | true,value-> value
        | _ -> defaultVal 


module Logging =
    let log level msg = printfn "%s %s" level msg
    let logWarn = log "WARN "
    let logError = log "ERROR"

// ========================================
// Types and code used to parse Post headers
// ========================================
module PostMetadata =
    open StringUtilities
    open Logging

    type PostMetadata = {
        Slug : string
        Date : DateTime
        Layout: string
        Title: string
        Description: string
        SeriesId : string      // series posts only
        SeriesOrder: int 
        SeriesIndexId : string // series index page only
        Permalink : string
        Categories: string list
        }

    let extractYamlHeaderDictionary path =
        IO.File.ReadLines(path) 
        |> Seq.skipWhile (fun s -> s.StartsWith "---")
        |> Seq.takeWhile (fun s -> s.StartsWith "---" |> not)
        |> Seq.choose splitYamlLine 
        |> Seq.map (fun (k,v) -> toLower k,v)
        |> dict  

    let lookupDictValue (dict:IDictionary<string,string>) (key:string) = 
        match dict.TryGetValue(toLower key) with
        | (true,value) -> value
        | (false,_) -> ""

    let yamlToList str =
        str
        |> removeSquareBrackets
        |> fun s -> s.Split([| ',' |]) 
        |> Array.toList
        |> List.map removeQuotes 
        // yamlToList ""
        // yamlToList "[]"
        // yamlToList "[a,  b]"
        // yamlToList """[ "a",  "b"]"""

    let createPostMetadata slug date path =
        let dict = extractYamlHeaderDictionary path
        let lookup = lookupDictValue dict
        {
            Slug = slug
            Date = date
            Layout = lookup "Layout" 
            Title = lookup "Title" 
            Description = lookup "Description" 
            SeriesId = lookup "SeriesId" 
            SeriesOrder = lookup "SeriesOrder" |> strToInt 0
            SeriesIndexId = lookup "SeriesIndexId" 
            Permalink = lookup "Permalink" 
            Categories =  lookup "Categories" |> yamlToList 
        }

    // dated Post
    let extractPostMetadata path =
        let local = IO.Path.GetFileNameWithoutExtension path 
        match local.Split([|'-'|],4) with
            | [|year;month;day;slug|] -> 
                let dt = DateTime(int year,int month,int day)
                createPostMetadata slug dt path |> Some
            | _ -> 
                logWarn (sprintf "Invalid path %s" path)
                None

    // non-dated Page
    let extractPageMetadata path =
        let slug = IO.Path.GetFileNameWithoutExtension path 
        let dt = DateTime(1980,1,1)
        createPostMetadata slug dt path |> Some

    (*
    // test
    open PostMetadata

    let path = @"../_posts\2012-04-01-why-use-fsharp-intro.md"
    extractPostMetadata path 

    let path = @"../_posts\2013-05-14-why-i-wont-be-writing-a-monad-tutorial.md"
    extractPostMetadata path 

    let path = @"../_posts\2013-10-23-monoids-without-tears.md"
    extractPostMetadata path 

    let path = @"../series\handling-state.md"
    extractPageMetadata path 

    *)


    let extractAllPostMetadata root =
        let postsDir = IO.Path.Combine(root,"_posts")
        IO.Directory.EnumerateFiles(postsDir) 
        |> Seq.choose extractPostMetadata 

    let extractAllPageMetadata root =
        let seriesDir = IO.Path.Combine(root,"series")
        IO.Directory.EnumerateFiles(seriesDir) 
        |> Seq.choose extractPageMetadata 

    (*
    // test
    open PostMetadata
    let root = @".."
    extractAllPostMetadata root |> Seq.take 5 |> Seq.toList
    *)


// ========================================
// Generate Series
// ========================================
module Series =
    open PostMetadata

    type Series = {
        SeriesTitle : string
        Permalink : string
        Posts : PostMetadata list
        }

    let collectSeries (posts:PostMetadata seq) (pages:PostMetadata seq) =
        let findPermalink seriesId = 
            pages
            |> Seq.tryFind (fun page -> page.SeriesIndexId = seriesId)
            |> Option.map (fun p -> p.Permalink)
            |> defaultArg <| ""

        posts
        |> Seq.groupBy (fun meta -> meta.SeriesId)
        |> Seq.filter (fun (seriesId,posts) -> seriesId <> "" )
        |> Seq.map (fun (seriesId,posts) -> 
            {
            SeriesTitle = seriesId
            Permalink = findPermalink seriesId
            Posts = posts |> Seq.toList |> List.sortBy (fun p -> p.SeriesOrder)
            })

    let getPrevPost post posts =
        posts |> List.tryFind (fun p -> p.SeriesOrder = (post.SeriesOrder-1))

    let getNextPost post posts =
        posts |> List.tryFind (fun p -> p.SeriesOrder = (post.SeriesOrder+1))

    let emitSeriesFile path seriesList =
        let fileName = IO.Path.Combine(path,"series.yaml")
        use writer = new IO.StreamWriter(path=fileName)
        for series in seriesList do
            writer.WriteLine("\"{0}\":",series.SeriesTitle)
            writer.WriteLine("  title: {0}",series.SeriesTitle)
            writer.WriteLine("  permalink: {0}",series.Permalink)
            writer.WriteLine("  posts:")
            for post in series.Posts do
                writer.WriteLine("    - slug: {0}",post.Slug)
                writer.WriteLine("      seriesOrder: {0}",post.SeriesOrder)
                writer.WriteLine("      url: /posts/{0}/",post.Slug)
                writer.WriteLine("      title: \"{0}\"",post.Title)
                writer.WriteLine("      description: \"{0}\"",post.Description)
                match getPrevPost post series.Posts with
                | None -> ()
                | Some prevPost ->
                    writer.WriteLine("      prevUrl: /posts/{0}/",prevPost.Slug)
                    writer.WriteLine("      prevTitle: \"{0}\"",prevPost.Title)
                    writer.WriteLine("      prevOrder: \"{0}\"",prevPost.SeriesOrder)
                match getNextPost post series.Posts with
                | None -> ()
                | Some nextPost ->
                    writer.WriteLine("      nextUrl: /posts/{0}/",nextPost.Slug)
                    writer.WriteLine("      nextTitle: \"{0}\"",nextPost.Title)
                    writer.WriteLine("      nextOrder: \"{0}\"",nextPost.SeriesOrder)
                writer.WriteLine() // end post
            writer.WriteLine() // end series

    (*
    // test
    open PostMetadata
    open Series 
    let root = @".."
    let posts = extractAllPostMetadata root 
    let seriesPages = extractAllPageMetadata root 
    let series = collectSeries posts seriesPages 
    series |> emitSeriesFile @"../_data"
    *)


// ========================================
// Generate Archives
// ========================================
module Archives =
    open PostMetadata

    let emitArchivesFile path posts =
        let fileName = IO.Path.Combine(path,"archives.yaml")
        use writer = new IO.StreamWriter(path=fileName)
        
        let sortByDescendingYearMonth = Seq.sortByDescending (fun ((year,month),_) -> (year,month) )
        let sortByDescendingPostDate = Seq.sortByDescending (fun p -> p.Date )

        let postsByYearMonth = 
            posts 
            |> Seq.groupBy (fun p -> p.Date.Year, p.Date.Month)
            |> sortByDescendingYearMonth 
            |> Seq.map (fun ((year,month),posts) -> (year,month),posts |> sortByDescendingPostDate  )


        for (year,month),posts in postsByYearMonth do
            let monthName = Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
            writer.WriteLine("- year: {0}",year)
            writer.WriteLine("  month: {0}",month)
            writer.WriteLine("  monthName: {0}",monthName)
            writer.WriteLine("  posts:")
            for post in posts do
                writer.WriteLine("    - slug: {0}",post.Slug)
                writer.WriteLine("      url: /posts/{0}/",post.Slug)
                writer.WriteLine("      title: \"{0}\"",post.Title)
                writer.WriteLine("      date: \"{0}\"",post.Date.ToString("dd MMM yyyy"))
                writer.WriteLine("      description: \"{0}\"",post.Description)
                writer.WriteLine() // end post
            writer.WriteLine() // end year/month
            
    (*
    // test
    open PostMetadata
    open Archives
    let root = @".."
    let posts = extractAllPostMetadata root 
    posts |> emitArchivesFile @"../_data"
    *)
