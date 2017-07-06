module Jekyll

  # Extensions to the Jekyll Page class.
  class Page

    def seriesId
        self.data['seriesId']
    end

	def seriesOrder
        self.data['seriesOrder'].to_i
    end
	
  end
  
  # Extensions to the Jekyll Post class.
  class Post

    def seriesId
        self.data['seriesId'] 
    end

	def seriesOrder
        self.data['seriesOrder'].to_i
    end
	
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



