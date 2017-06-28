/*
 * From http://christian-fei.com/tutorials/how-to-lazy-load-disqus-comments/
 */

var comments = document.getElementsByClassName('comments')[0],
    disqusLoaded = false;

function loadDisqus() {
  var disqus_shortname = 'your_disqus_shortname';
  var dsq = document.createElement('script');
  dsq.type = 'text/javascript';
  dsq.async = true;
  dsq.src = 'http://' + disqus_shortname + '.disqus.com/embed.js';
  (document.getElementsByTagName('head')[0] || document.getElementsByTagName('body')[0]).appendChild(dsq);
  disqusLoaded = true;
}

//Get the offset of an object
function findTop(obj) {
  var curtop = 0;
  if (obj.offsetParent) {
    do {
      curtop += obj.offsetTop;
    } while (obj = obj.offsetParent);
  return curtop;
  }
}

if(window.location.hash.indexOf('#comments') > 0)
  loadDisqus();

if(comments) {
  var commentsOffset = findTop(comments);

  window.onscroll = function() {
    if(!disqusLoaded && window.pageYOffset > commentsOffset - 1000)
      loadDisqus();
  }
}