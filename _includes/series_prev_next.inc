{% if page.seriesOrder %}
	<ul class="pager">
		{% if page.previousInSeries %}
		<li class="previous">
		<a href="{{ page.previousInSeries.url }}/" title="{{ page.previousInSeries.title | escape }}">&larr; {{ page.previousInSeries.seriesOrder }}. {{ page.previousInSeries.title | escape }}</a>
		</li>
		{% endif %}

		{% if page.nextInSeries %}
		<li class="next">
		<a href="{{ page.nextInSeries.url }}/" title="{{ page.nextInSeries.title | escape }}">{{ page.nextInSeries.seriesOrder }}. {{ page.nextInSeries.title | escape }} &rarr; </a>
		</li>
		{% endif %}

	</ul>
	
{% endif %}

