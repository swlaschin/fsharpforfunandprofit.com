{% if page.seriesId %}
	{% for otherpage in site.data.series[page.seriesId].posts %}
		{% if page.url == otherpage.url %}
	<ul class="pager">

		{% if otherpage.prevUrl %}
		<li class="previous">
		<a href="{{ otherpage.prevUrl }}" title="{{ otherpage.prevTitle | escape }}">&larr; {{ otherpage.prevOrder }}. {{ otherpage.prevTitle | escape }}</a>
		</li>
		{% endif %}

		{% if otherpage.nextUrl %}
		<li class="next">
		<a href="{{ otherpage.nextUrl }}" title="{{ otherpage.nextTitle | escape }}">{{ otherpage.nextOrder }}. {{ otherpage.nextTitle | escape }} &rarr; </a>
		</li>
		{% endif %}

	</ul>

		{% endif %}
	{% endfor %}

	
{% endif %}

