<!--Sidebar content-->

{% unless page.isBookPage %}
<h6>Get the books</h6>
<div>
<a href="/books/">
Reading offline? Download the ebook of this site.
<!--
<br>
Or buy my proper book: <br>
<img src="/books/domain-modeling-made-functional-200.jpg">
-->
</a>
</div>
{% endunless %}

<h6>Greatest Hits</h6>

<div>New here? Try one of these:<div>
<div><a href="/site-contents/">Explore this site</a></div> 
<div><a href="/posts/why-use-fsharp-intro/">Why use F#?</a></div> 
<div><a href="/learning-fsharp/">Tips for learning F#</a></div> 
<div><a href="/troubleshooting-fsharp/">Troubleshooting F#</a></div>
<div><a href="/fppatterns/">Functional design patterns (talk)</a></div>
<div><a href="/series/thinking-functionally.html">Thinking Functionally</a></div> 
<div><a href="/ddd/">Domain modeling with F# (talk)</a></div>
<div><a href="/series/designing-with-types.html">Designing with Types</a></div>
<div><a href="/rop/">Railway Oriented Programming (talk)</a> and <a href="/posts/recipe-part2/">the original post</a></div>
<div><a href="/posts/13-ways-of-looking-at-a-turtle/">Thirteen ways of looking at a turtle</a></div>
<div><a href="/parser/">Understanding Parser Combinators</a></div>
<div><a href="/posts/roman-numeral-kata/">Roman Numerals Kata</a></div> 
<div><a href="/posts/property-based-testing-2/">Choosing properties for property-based testing</a></div>

<h6>Recent Posts</h6>
{% for post in site.posts limit:3 %}
<div><a href="{{ post.url }}" title="{{ post.title | escape }}">{{ post.title | escape }}</a></div>
{% endfor %}
<div class="archive"><a href="/archives/">&raquo; Archives</a></div>

<h6>More</h6>
<div><a href="https://goo.gl/a1j5CS" class="twitter-follow-button" data-size="small" data-show-count="false" data-lang="en">Follow @ScottWlaschin</a></div>
<div><a href="/about/">About</a></div>
<div><a href="/subscribe.html">Newsletter</a></div>
<div><a href="https://github.com/swlaschin/fsharpforfunandprofit.com">Typos?</a></div>

<h6>Share the love</h6>
<div><a href="https://www.dropbox.com/sh/odx680qh5srur2n/AACc8tZ6-eDI09YK0-Y5F4hNa?dl=0"><img src="/assets/img/IHeartFsharp160.png"></a></div>
