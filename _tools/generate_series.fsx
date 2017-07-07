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

module XX =             
	
	def nextInSeries
	  series = self.site.seriesPages[self.seriesId]
      pos = series.index(self)

      if pos && pos < series.length-1
        series[pos+1]
      else
        nil
      end
    end

    def previousInSeries
	  series = self.site.seriesPages[self.seriesId]
      pos = series.index(self)

      if pos && pos > 0
        series[pos-1]
      else
        nil
      end
    end

	alias orig_to_liquid to_liquid
	
	# Convert this Page's data to a Hash suitable for use by Liquid.
    def to_liquid
		self.data = self.data.deep_merge({
			"nextInSeries"      => self.nextInSeries,
			"previousInSeries"      => self.previousInSeries,
			})
		# self.data["nextInSeries"] = self.nextInSeries
		# self.data["previousInSeries"] = self.previousInSeries

		self.orig_to_liquid	

    end
	
  end

  
  # The SeriesIndex class creates a single series page for the specified series.
  class SeriesIndex < Page
    	
    # Initializes a new SeriesIndex.
    #
    #  +base+         is the String path to the <source>.
    #  +series_dir+ is the String path between <source> and the series folder.
    #  +series+     is the series currently being processed.
    def initialize(site, base, series_dir, series)
      @site = site
      @base = base
      @dir  = series_dir
      @name = 'index.html'
      self.process(@name)
      # Read the YAML data from the layout page.
      self.read_yaml(File.join(base, '_layouts'), 'series_index.html')
      
      # Set the title for this page.
      title_prefix             = 'The "'
	  title_suffix             = '" series'
      self.data['title']       = "#{title_prefix}#{series}#{title_suffix}"
	  self.data['seriesId']     = series
      # Set the meta-description for this page.
    end
    
  end
  
  # Extensions to the Jekyll Site class.
  class Site

	
	
	def seriesPages
      # Build a hash map, with each key being a the series id and each value being an array of pages in that series
      hash = Hash.new { |hsh, key| hsh[key] = Array.new }
      self.pages.each { |p| hash[p.seriesId] << p if not p.seriesId.nil? }
      self.posts.each { |p| hash[p.seriesId] << p if not p.seriesId.nil? }
      hash.values.map { |sortme| sortme.sort_by! { |p| p.seriesOrder } }
      hash
    end

	def seriesFirstPageArray
      # Build a array of the first page of all series found
      arr = Array.new 
      self.seriesPages.each_value {|value| arr << value.first }
	  arr.sort_by! { |p| p.seriesId } 
      arr
    end

	def seriesIds
      # Build a array of all seriesIds found
      self.seriesPages.keys
    end

    # Add some custom options to the site payload, accessible via the
    # "site" variable within templates.
    #
    # seriesPosts, in seriesOrder keyed by SeriesId
    alias orig_site_payload site_payload

    def site_payload
        h = orig_site_payload
        payload = h["site"]
        payload["seriesPages"] = seriesPages
		payload["seriesFirstPageArray"] = seriesFirstPageArray
        h["site"] = payload
        h
    end
	
	def series_filename(string)
		string.downcase.gsub(' ','-').gsub('f#','fsharp').gsub('c#','csharp').gsub(/([^a-zA-Z0-9_.-]+)/n,'')
	end

	# Creates an instance of SeriesIndex for each series page, renders it, and 
    # writes the output to a file.
    #
    #  +series_dir+ is the String path to the series folder.
    #  +seriesId+     is the series currently being processed.
    def write_series_index(series_dir, seriesId)
      index = SeriesIndex.new(self, self.source, series_dir, seriesId)
      index.render(self.layouts, site_payload)
      index.write(self.dest)
      # Record the fact that this page has been added, otherwise Site::cleanup will remove it.
      self.pages << index
    end
    
    # Loops through the list of series pages and processes each one.
    def write_series_indexes
      if self.layouts.key? 'series_index'
        dir = self.config['series_dir'] || 'series'
        self.seriesIds.each do |seriesId|
          self.write_series_index(File.join(dir, series_filename(seriesId)), seriesId)
        end
        
      # Throw an exception if the layout couldn't be found.
      else
        throw "No 'series_index' layout found."
      end
    end
	
  end

  
  # Jekyll hook - the generate method is called by jekyll, and generates all of the series pages.
  # class GenerateSeries < Generator
    # safe true
    # priority :low

    # def generate(site)
      # site.write_series_indexes
    # end

  # end  
  
  # Adds some extra filters used during the category creation process.
  module Filters
    
	def series_filename(string)
		string.downcase.gsub(' ','-').gsub('f#','fsharp').gsub('c#','csharp').gsub(/([^a-zA-Z0-9_.-]+)/n,'')
	end
	
  end
  
  
end



