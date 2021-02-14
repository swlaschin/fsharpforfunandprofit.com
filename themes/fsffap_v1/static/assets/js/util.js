function resizeIframe(obj) {
    obj.style.height = obj.contentWindow.document.body.scrollHeight + 'px';
}

$( document ).ready(function() {
  
 	// Add anchors to headings that have ids
    // https://github.com/circleci/circleci-docs/pull/97/files
 	$("h2, h3, h4, h5, h6").filter("[id]").each(function () {
		 $(this).append('<a href="#' + $(this).attr("id") + '"><i style="display: none"> <svg viewBox="0 0 24 24" width="18" height="18" class="Icon" role="presentation"><g fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round"><path d="M13.77,10.09l-0.71-.71a4,4,0,0,0-5.65,0L3.16,13.63a4,4,0,0,0,0,5.66l1.4,1.4a4,4,0,0,0,5.67,0l1.41-1.41"></path><path d="M10.23,13.62l0.71,0.71a4,4,0,0,0,5.65,0l4.25-4.25a4,4,0,0,0,0-5.66L19.43,3a4,4,0,0,0-5.67,0L12.35,4.43"></path></g></svg></i></a>');
 	});
 	$("h2, h3, h4, h5, h6").filter("[id]").hover(function () {
 		$(this).find("i").toggle();
 	});
  });