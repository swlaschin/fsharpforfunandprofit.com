<!--Sidebar content-->

<h6>Get the ebook</h6>
<div><a href="https://www.gitbook.com/book/swlaschin/fsharpforfunandprofit/">Reading offline? Download the ebook of this site</a></div>

<h6>Share the love</h6>
<div><a href="https://www.dropbox.com/sh/odx680qh5srur2n/AACc8tZ6-eDI09YK0-Y5F4hNa?dl=0"><img src="/assets/img/IHeartFsharp160.png"></></div>

<h6>Slides/Videos</h6>
<div><a href="/fppatterns/">Functional Design Patterns</a></div>
<div><a href="/ddd/">A functional approach to Domain Driven Design</a></div>
<div><a href="/rop/">A functional approach to error handling (Railway Oriented Programming)</a></div>
<div><a href="/pbt/">Property-based testing</a></div>
<div><a href="/ettt/">Enterprise Tic-Tac-Toe</a></div>
<div><a href="/monadster/">Dr Frankenfunctor and the Monadster</a></div>
<div><a href="/cap/">Designing with Capabilities</a></div>
<div><a href="/parser/">Understanding parser combinators</a></div>
<div><a href="/turtle/">Thirteen ways of looking at a turtle</a></div>
<div><a href="/csharp/">F# for C# Programmers</a></div>

<h6>Series</h6>
{% for seriesId in site.seriesPages | sort %}
<div><a href="/series/{{ seriesId[0] | series_filename }}.html">{{ seriesId[0] | escape}}</a></div>
{% endfor %}

<h6>Recent Posts</h6>
{% for post in site.posts limit:6 %}
<div><a href="{{ post.url }}/" title="{{ post.title | escape }}">{{ post.title | escape }}</a></div>
{% endfor %}
<div class="archive"><a href="/archives/">&raquo; Archives</a></div>

<h6>Categories</h6>
{% for cat in site.categories | sort %}
<span><a href="/category/{{ cat[0] | category_filename }}/">{{ cat[0] | escape}}</a> &bull; </span>
{% endfor %}

<h6>More</h6>
<div><a href="https://twitter.com/ScottWlaschin" class="twitter-follow-button" data-size="small" data-show-count="false" data-lang="en">Follow @ScottWlaschin</a></div>
<div><a href="http://feeds.feedburner.com/Fsharpforfunandprofit?format=xml"><img src="/assets/img/feed-icon-14x14.png"> RSS Posts</a></div>
<div><a href="/about/">About</a></div>
<div><a href="/installing-and-using/">Installing and using F#</a></div>
<div><a href="/learning-fsharp/">Learning F#</a></div>
<div><a href="/troubleshooting-fsharp/">Troubleshooting F#</a></div>
<div><a href="/subscribe.html">Newsletter</a></div>
