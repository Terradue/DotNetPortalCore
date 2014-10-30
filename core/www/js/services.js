/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/

// javascript code for the services page (highligths search string)

// This is commented to be used directly in the sites
// It is not a mandatory function but must be used when needed
/*$(document).ready(function() {
	highlightOnLoad();
})*/

function highlightOnLoad(containerName) {
  // Get search string
  //alert(test(window.location.search));
  if (true){//(/s\=/.test(window.location.search)) {
  var searchString = getSearchString();
    // Starting node, parent to all nodes you want to search
    var textContainerNode = document.getElementById(containerName);

    // Informational message for search
    var searchInfo = 'Search Results for: ';

    // Split search terms on '|' and iterate over resulting array
    var searchTerms = searchString.split('|');
    for (var i=0; i<searchTerms.length;++i ){
	//for (var i in searchTerms) 	{
      // The regex is the secret, it prevents text within tag declarations to be affected
      var regex = new RegExp(">([^<]*)?("+searchTerms[i]+")([^>]*)?<","ig");
      highlightTextNodes(textContainerNode, regex, i);
      // Add to info-string
      searchInfo += ' <span class="highlighted term'+i+'">'+searchTerms[i]+'</span> ';
    }

    // Create div describing the search
    /*
	var searchTermDiv = document.createElement("H2");
    searchTermDiv.className = 'searchterms';
    searchTermDiv.innerHTML = searchInfo;
	*/
    // Insert as very first child in searched node
    //textContainerNode.insertBefore(searchTermDiv, textContainerNode.childNodes[0]);
  }
}

// Pull the search string out of the URL
function getSearchString() {
  // Return sanitized search string if it exists
//  alert(window.location.search);
  var rawSearchString = window.location.search.replace(/[a-zA-Z0-9\?\&\=\%\#]+q\=(\w*)(\&.*)?/,"$1");
//  alert(rawSearchString);
  // Replace '+' with '|' for regex
  // Also replace '%20' if your cms/blog uses this instead (credit to erlando for adding this)
  return rawSearchString.replace(/\%20|\+/g,"\|");
}

function highlightTextNodes(element, regex, termid) {
  var tempinnerHTML = element.innerHTML;
  // Do regex replace
  // Inject span with class of 'highlighted termX' for google style highlighting
  element.innerHTML = tempinnerHTML.replace(regex,'>$1<span class="highlighted term'+termid+'">$2</span>$3<');
}

