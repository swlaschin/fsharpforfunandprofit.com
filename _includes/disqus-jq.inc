<h4>Comments</h4>

<!-- code for "load disqus on demand" from http://internet-inspired.com/wrote/load-disqus-on-demand/ -->

<!-- An element a visitor can click if they <3 comments! -->
<button class="show-comments">Load Disqus comments</button>
<noscript>Please enable JavaScript to view the <a href="http://disqus.com/?ref_noscript">comments powered by Disqus.</a></noscript>

<!-- The empty element required for Disqus to load comments into -->  
<div id="disqus_thread"></div>

<script type="text/javascript" defer>
    $j=jQuery.noConflict();
	// if (window.location.host == 'localhost:4000'){disqus_developer = 1}else{disqus_developer = 0} // developer mode is on

    $j(document).ready(function() {
        $j('.show-comments').on('click', function(){
              var disqus_shortname = 'fforfunandprofit'; 

              // ajax request to load the disqus javascript
              $j.ajax({
                      type: "GET",
                      url: "http://" + disqus_shortname + ".disqus.com/embed.js",
                      dataType: "script",
                      cache: true
              });
              // hide the button once comments load
              $j(this).fadeOut();
        });
    });
</script>
