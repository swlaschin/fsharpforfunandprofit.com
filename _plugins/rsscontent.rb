# RSS content
#
# Included filters :
# - rss_absolute:      Transforms the content to absolute form by prefixing with http://fsharpforfunandprofit.com

module Jekyll
  
  
  # Converts all relative links to absolute links
  module Filters
    
	def rss_absolute(string)
        string.gsub("href=\"/","href=\"http://fsharpforfunandprofit.com/")
	end
    
  end
  
end