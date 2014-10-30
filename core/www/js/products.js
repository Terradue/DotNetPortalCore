/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/
var identifierArr = new Array();

	$(document).ready(function() {
		
		
		
		
		$("#dataset_query_status").html("");

		$('.table_dataset').bind('newfeature', function (event, dataset, table, nRow, newoption){ 
				nRow.DataSetDetails = function (oTable, nTr, option){
					// this is 5 because is the fifth element ... got to find out the original name in the movie  
					/// see config/services.xml //services/dataset/metadata/
					// to-do remove the 5 and add a by name logic
					var arr = option.data("metadata")[5].split(" ");
					var result = '', resultImg = '';
					$.each(arr, function (index, val) {
						if (val.indexOf('_browse') != -1) resultImg += '<img src="' + val + '" /><br/>';
						else result += '<a href="' + val + '">' + val + '</a><br/>'
					});
					return result + resultImg;
					
				}
			});

		$('.dataset').bind('selectfeature', function(event, identifier) {
			identifierArr[ identifierArr.length ] = identifier;
			if (identifierArr.length >0) $("#dataset_download").removeClass("hidden")
			// to-do check this code 
		})

		$('.dataset').bind('unselectfeature', function(event, identifier) {
			$.each(identifierArr, function(index,value) {
				if (identifier==value) {identifierArr.remove(index); return false}
			})
			if (identifierArr.length == 0) $("#dataset_download").addClass("hidden")
			
		})
		
		$('.dataset').bind('beforequery', function(event){
			$(this).trigger('deletefeature',[$(this).find("option")]);
		});

		$('.geouidIcon img').click(function(){
			$(".geo_uid").val(identifierArr.join(","));
			var myUrl= getOpensearchUrl ( $(this).data("template"), $("#dataset"), ".geo_uid");
			if (myUrl){
				window.open(myUrl,"_new");
				$(".geo_uid").val('');
			};
		})
				

		$('.dataset').bind('receivedresults', function(event, data) {	
			//$('.dataset').trigger('selectall');
			$(this).trigger('deletefeature',$(this).find("option"));
			$(this).data("filesTable").fnSettings().iDisplayLength=20;
		})
		//$("#dataset").data("count", "20")
		$("#dataset").data("scope","#query :input, #aux_query :input");

		$('.dataset').bind('afterquery', function(event,data) {	
					
			// to-do 
			/*
			from the list of selected id we now have to call the 
			$('.dataset').trigger('selectfeature', 
			*/
					$.each(identifierArr, function(index,value) {
						$('#dataset').trigger('selectfeature', value);
					})
					
					var pagingHtml = "";
					var itemsPerPage = $(data).find("os\\:itemsPerPage").text() ;
					var totalResults = $(data).find("os\\:totalResults").text() ;
					var startIndex = $(data).find("os\\:startIndex").text() ;
					if (totalResults > 0){
						if (totalResults*1 < itemsPerPage*1){
							if (totalResults == 1) pagingHtml += "Only one result found";
							else pagingHtml += " Found " + totalResults + " results";
						}
						else{

							pagingHtml += 'Showing results from '+  (startIndex * 1 + 1) + " to ";
							if (totalResults < (startIndex * 1 + itemsPerPage*1)) pagingHtml += totalResults;
							else pagingHtml += (startIndex * 1 + itemsPerPage*1) + " out of " + totalResults;
							pagingHtml += ' <br/>';
							var nextPage = getXPathValue(data, "//atom:link[@atom:rel='next']/@atom:href");
							var prevPage = getXPathValue(data, "//atom:link[@atom:rel='previous']/@atom:href");
							//pagingHtml += " " + nextPage + " ";
							//alert(nextPage);
							//pagingHtml += "<br/>" + getXPathValue(data, "//rdf:Description/@rdf:about") +"<br/><br/>";
							if (prevPage) pagingHtml += "<a href='" + prevPage +"'> prev page </a> - "
							if (nextPage) pagingHtml += "<a href='" + nextPage +"'> next page </a>"
						}
					}
					else{
						pagingHtml += " No results found ";
					}
					$("#dataset_query_status").html(pagingHtml);

					$("#dataset_query_status a").click(function (e){
						$(this).parent().addClass("waiting");
						$('.dataset').trigger('beforequery');
						ExecuteUrlQuery($(this).attr("href"), $("#dataset"));
						e.preventDefault();
					});

		})


			
		$('#dataset_button_query').data("dataset","#dataset");


		$('#map').html('');
		$('#paneldiv').html('');
		
	})
function onSeriesPageInit(){
 		OSMap = new OpenLayers.Map('map', 
                                {numZoomLevels: 6, maxResolution: 'auto', units: "dd", controls: [] }
                        );
                
 			OSMap.addLayers([ new OpenLayers.Layer.WMS( "Base Map","http://gpod-map.eo.esa.int/cgi-bin/mapserv?map=base.map&amp;",{layers: "NE2_HR_LC_SR_W_DR,Borders", "STYLES" : "", format: 'image/png', 'buffer':4}) ]);
                OSMap.zoomToMaxExtent();
                SetMapControls(OSMap.layers.length, OSMap);
		OSMap.layers[1].styleMap.styles.default.defaultStyle.fillOpacity=0.01;             
        	var Sel = startSelectGeoBox(OSMap,".geo_box")
        
                OSIconPanel.addControls([Sel]);
                selectionBox.activate();
        
        	OSMap.updateSize();
        	OSMap.zoomToMaxExtent();

}
