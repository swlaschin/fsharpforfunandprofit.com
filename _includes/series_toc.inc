{% if page.seriesId %}
	<div class="well">
	    <a name="series-toc"></a>
		<h4>The "{{page.seriesId | escape }}" series</h4>
		{% for otherpage in site.data.series[page.seriesId].posts %}
		
			{% if page.url == otherpage.url %}
			<div>{{ otherpage.seriesOrder }}. {{ otherpage.title | escape }}</div>
			{% else %}
			<div><a href="{{ otherpage.url }}" title="{{ otherpage.title | escape }}">{{ otherpage.seriesOrder }}. {{ otherpage.title | escape }}</a></div>
			{% endif %}
			
		{% endfor %}
	</div>
{% endif %}

