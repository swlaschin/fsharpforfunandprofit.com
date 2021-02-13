<h4>Comments</h4>

<!-- code for "load disqus on demand" from http://internet-inspired.com/wrote/load-disqus-on-demand/ -->

<!-- An element a visitor can click if they <3 comments! -->
<button class="show-comments">Load Disqus comments</button>
<noscript>Please enable JavaScript to view the <a href="http://disqus.com/?ref_noscript">comments powered by Disqus.</a></noscript>

<!-- The empty element required for Disqus to load comments into -->  
<div id="disqus_thread"></div>

<script type="text/javascript" defer>

	// if (window.location.host == 'localhost:4000'){disqus_developer = 1}else{disqus_developer = 0} // developer mode is on

    
    function fadeOut( elem, ms )
    {
      if( ! elem )
        return;

      if( ms )
      {
        var opacity = 1;
        var timer = setInterval( function() {
          opacity -= 50 / ms;
          if( opacity <= 0 )
          {
            clearInterval(timer);
            opacity = 0;
            elem.style.display = "none";
            elem.style.visibility = "hidden";
          }
          elem.style.opacity = opacity;
          elem.style.filter = "alpha(opacity=" + opacity * 100 + ")";
        }, 50 );
      }
      else
      {
        elem.style.opacity = 0;
        elem.style.filter = "alpha(opacity=0)";
        elem.style.display = "none";
        elem.style.visibility = "hidden";
      }
    }

    function ready(fn) {
      if (document.readyState != 'loading'){
        fn();
      } else {
        document.addEventListener('DOMContentLoaded', fn);
      }
    }    
    
    ready(function() {
        var b = document.querySelectorAll('.show-comments')[0];
        b.addEventListener('click', function(){
            var disqus_shortname = 'fforfunandprofit'; 

            // ajax request to load the disqus javascript
            var request = new XMLHttpRequest();
            request.open('GET', "http://" + disqus_shortname + ".disqus.com/embed.js", true);
            request.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
            request.send();
            
            // hide the button once comments load
            fadeOut(b,1000);
        });
    });
</script>
