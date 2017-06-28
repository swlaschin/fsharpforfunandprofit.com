function resizeIframe(obj) {
    obj.style.height = obj.contentWindow.document.body.scrollHeight + 'px';
}

$( document ).ready(function() {
  
 	// Add anchors to headings that have ids
    // https://github.com/circleci/circleci-docs/pull/97/files
 	$("h2, h3, h4, h5, h6").filter("[id]").each(function () {
 		$(this).append('<a href="#' + $(this).attr("id") + '"><i class="fa fa-link" style="display: none"></i></a>');
 	});
 	$("h2, h3, h4, h5, h6").filter("[id]").hover(function () {
 		$(this).find("i").toggle();
 	});
  });