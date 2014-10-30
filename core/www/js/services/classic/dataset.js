/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/

// This code deals with the list of products widget when we have SELECT element on the service page
// It assumes that there is some html element of the class .dataset to link the results


	$(document).ready(function() {
		

		$("input:button.dataset_button_delete").click(function(){
				$($(this).data('dataset')).trigger('deletefeature',[$($(this).data('dataset')).find("option:selected")]);			
			});

		$('.dataset').bind('deletefeature', function(event,els) {
			if (els){
				$(".ifyOSMap").trigger('deletefeatures',[els]);
				$(els).each(function (){$(this).remove()});
			}
			else{
				
				$(".ifyOSMap").trigger('deletefeatures',[$(this).find("option:selected")]);
				$(this).find("option:selected").remove();
			}
		})

		$("input:button.dataset_button_selectall").click(function(){
				if (this.value == "Select All"){
					$($(this).data('dataset')).trigger('selectall');
					//selectedFeaturesLayer.destroyFeatures();
					this.value="Unselect All";
				}else{
					$($(this).data('dataset')).trigger('unselectall');
					//selectedFeaturesLayer.destroyFeatures();
					this.value="Select All";
				}
			});
		
		$("input:button.dataset_button_deleteall").click(function(){
				$($(this).data('dataset')).trigger('deletefeature',[$($(this).data('dataset')).find("option")]);
			});
		
		
		
		$('.dataset').bind('selectall', function(event, params) {			
			$(this).find("option").each(function(){
					if($(this).attr("display") == "none") return
					$(this).attr('selected','selected');
					$(".ifyOSMap").trigger('selectfeatures',[$(this)]);
			})
		})
		
		$('.dataset').bind('unselectall', function(event, params) {	
			$(this).find("option").each(function(){
					if($(this).attr("display") == "none") return
					$(this).removeAttr('selected');
					$(".ifyOSMap").trigger('unselectfeatures',[$(this)]);
			})
		})

		$("input:button.dataset_button_query").click(function(){
				$(".time_start").change();
				var dataset = $($(this).data('dataset'));
				if (dataset.triggerHandler('beforequery')==false) {return false};
				if ($(this).triggerHandler('beforequery')==false) {return false};
				$(this).attr("disabled","disabled");
				var dataset_Query_Status =$('#' +  dataset.attr('id')+ '_query_status');
				dataset_Query_Status.removeClass("ifyMessage");
				dataset_Query_Status.addClass("waiting");
				var urltemplate = $(dataset.data('series')).find("option:selected").data('urltemplate');
				if (!urltemplate){
					var urldescription = $($($(this).data('dataset')).data('series')).find("option:selected").data('description');
					if (!urldescription){
						var urldescription = $($($(this).data('dataset')).data('series')).val();
						if (!urldescription){
						dataset_Query_Status.removeClass("waiting");
						dataset_Query_Status.addClass("ifyMessage");
						dataset_Query_Status.html('Unable to perform query because catalogue OpenSearch Description is missing');
						$(this).removeAttr("disabled");
						return false;
						}
					}
					dataset_Query_Status.html('Retrieving OpenSearch description from <br/>' + urldescription);
					var context = this;
					var urltemplate = getOpenSearchTemplate ( 
						urldescription, 
						$($(this).data('dataset')).data('template'),
							$($(this).data('dataset')), 
								function (value, list){
									if (value){								
										$(list.data('series')).find("option:selected").data('urltemplate', value);
										$(context).click();
									}
								}
							);
					return false
				}

				dataset_Query_Status.html('Retrieving OpenSearch information from <br/>' + urltemplate.substring(0,urltemplate.indexOf('?')));
				
				var myUrl = getOpensearchUrl(urltemplate, dataset);
				
				if (myUrl){
					ExecuteUrlQuery(myUrl, dataset);
				}
				else{
					dataset_Query_Status.removeClass("waiting");
					dataset_Query_Status.addClass("ifyMessage");
					dataset_Query_Status.html('Unable to perform query because some mandatory parameters are missing');
					$(this).removeAttr("disabled");
				}
								
			});

		$('.dataset').bind('afterquery', CataloguePagingSupport);


		$(".dataset").change(function (){
				$(".ifyOSMap").trigger('selectfeatures',[$(this).find("option:selected")]);
				$(".ifyOSMap").trigger('unselectfeatures',[$(this).find("option:not(:selected)")]);
			});

		$('.dataset').bind('selectfeature', function(event, identifier) {
			var selFeature = $(this).find('option[value=' + identifier + ']');
			if (selFeature.length==1) {
				if (! $(this).attr('multiple')!='') $(this).trigger("unselectall");
				selFeature.attr('selected','selected');
				$(".ifyOSMap").trigger('selectfeatures',[selFeature]);
			}
		})

		$('.dataset').bind('unselectfeature', function(event, identifier) {
			var selFeature = $(this).find('option[value=' + identifier + ']');
			if (selFeature.length==1) {
				selFeature.removeAttr('selected');
				$(".ifyOSMap").trigger('unselectfeatures',[selFeature]);
			}
		})


		$('.dataset').bind('newfeature', function(event, newOption) {
			if ($(".ifyOSMap").length){
				var geom = new OpenLayers.Geometry.fromWKT(newOption.data('spatial'));
				//if ( newOption.data('spatial') == "POLYGON((-180 -90,-180 90,180 90,180 -90,-180 -90))") return;
				if(geom.transform) {
					geom.transform(new OpenLayers.Projection("EPSG:4326"), OSMap.getProjectionObject());
	  				var newgeom = new OpenLayers.Feature.Vector(
								// to-do: this function is linking to the OSMap variable and it should map to the css class in the future
	  							geom,
								{								
									identifier : newOption.data('identifier'),								
									/* to-do: this might be removed and use the reference id
										to save browser memory (?) */
									metadata : newOption.data('metadata')
								}
							);
					newOption.data('OS_feature_id',newgeom.id);
					selectedFeaturesLayer.addFeatures(newgeom);
				} else {
					selectedFeaturesLayer.addFeatures(geom);
				}
			}
		});
		
		
		$('.series').change(function (){
/*
			var template = $(this).find('option[value="'+ this.value +'"]').data('urltemplate');
			//alert(template);
			var params = template.substr(template.indexOf('?')+1).split("&");
			var myform = this.form;
			$(myform).find(".catalogue_query_extension").addClass("ifyInvisible");
			for(var i=0; i<params.length; i++) {
				var param = params[i].replace('{','').replace('}','').split('=');
				if(param[1] == null){
					displayMessage("error","Serie "+$(this).find('option[value="'+ this.value +'"]').text()+" is not correctly initialized, query will return an error! Please check your catalogue.")
				}else{
					$(myform).find('._' + param[1].replace('?','').replace(':','_')).each(function(i){
						$(this).removeClass('ifyInvisible');
					})
				}
			}
*/
		})
		
		
		$(".geo_maxX, .geo_minX").change(function (e){
			if (e.isPropagationStopped()) return true;
			if ( $(this).val() > 180 ) ($(this).val("180"));
			if ( $(this).val() < -180 ) ($(this).val("-180"));
			var max = parseFloat($(this).parent().find(".geo_maxX").val());
			var min = parseFloat($(this).parent().find(".geo_minX").val());
			if (max<min) {
				if ( confirm("Warning: maximum Longitude is lower that minimum Longitude, Invert values ?")){
					$(this).parent().find(".geo_maxX").val(min);
					$(this).parent().find(".geo_minX").val(max);
				}
			}
		})

		$(".geo_minY, .geo_maxY").change(function (e){
			if (e.isPropagationStopped()) return true;
			if ( $(this).val() < -90 ) ($(this).val("-90"));
			if ( $(this).val() > 90 ) ($(this).val("90"));
			
			var max = parseFloat($(this).parent().find(".geo_maxY").val());
			var min = parseFloat($(this).parent().find(".geo_minY").val());
			if (max<min) {
				if ( confirm("Warning: maximum Latitude is lower that minimum Latitude. Invert values ?")){
					$(this).parent().find(".geo_maxY").val(min);
					$(this).parent().find(".geo_minY").val(max);
				}
			}
		})
		

		$(".geo_minX, .geo_minY, .geo_maxX, .geo_maxY").change(function (e){
			if (e.isPropagationStopped()) return true;
			$(document).find(".geo_box").val(
				$(this).parent().find(".geo_minX").val()+ ',' +
				$(this).parent().find(".geo_minY").val()+ ',' +
				$(this).parent().find(".geo_maxX").val()+ ',' +
				$(this).parent().find(".geo_maxY").val()
			);
			$(document).find(".geo_box").change();
		})
		
		
		$(".geo_box").change(function () {
			if ($(this).val()=='') var bbox=',,,'.split(",");
			else var bbox=$(this).val().split(",");
			
			var llpoint = new OpenLayers.Geometry.Point(bbox[0],bbox[1]);
			var urpoint = new OpenLayers.Geometry.Point(bbox[2],bbox[3]);
			
			llpoint.transform(new OpenLayers.Projection("EPSG:4326"), OSMap.getProjectionObject());
			urpoint.transform(new OpenLayers.Projection("EPSG:4326"), OSMap.getProjectionObject());
			
			$(document).find('.geo_minX').val(bbox[0]).data("value",bbox[0]);
			$(document).find('.geo_minY').val(bbox[1]).data("value",bbox[1]);
			$(document).find('.geo_maxX').val(bbox[2]).data("value",bbox[2]);
			$(document).find('.geo_maxY').val(bbox[3]).data("value",bbox[3]);
			
		})
				
		
		$(".time_start, .time_end").each(function (){$(this).data("value",this.value);})
		$(".time_start, .time_end").bind("change",TimeValidityFunction);
	
	})

var TimeValidityFunction = function (){
			if ( ($(this).val()=='') && ($(this).hasClass('ifyOptional')) ) return true;
			if (!isDateValid(this.value,true)) {this.value= $(this).data("value");return false};
			var validDateFormat=/^\d{4}-\d{2}-\d{2}$/ //Basic check for format validity
			if (validDateFormat.test(this.value.replace(' ',''))){ // if year only 
				if ($(this).hasClass("time_start")){$(this).val($(this).val() + "T00:00:00")}
				if ($(this).hasClass("time_end")){$(this).val($(this).val() + "T23:59:59")}
			}
			var max = ($(this).parent().parent().find(".time_end").val());
			var min = ($(this).parent().parent().find(".time_start").val());
			
			if (( max!='') && (min!='') && (max<min) ){
				if ( confirm("Warning: end date is before the start date. Invert values ?")){
					$(this).parent().parent().find(".time_end").val(min).change();
					$(this).parent().parent().find(".time_start").val(max).change();
				}
			}
			$(this).data("value",this.value);
}

	var CataloguePagingSupport = function (event,data) {	
			
					var pagingHtml = "";
					var itemsPerPage = getXPathValue(data,$(this).data("catalogue:itemsPerPage"));
					var totalResults = getXPathValue(data,$(this).data("catalogue:totalResults"));
					var startIndex = getXPathValue(data,$(this).data("catalogue:startIndex"));
					if (totalResults > 0){
						if (totalResults*1 < itemsPerPage*1){
							if (totalResults == 1) pagingHtml += "Found one result";	
							else pagingHtml += " Found " + totalResults + " results";
						}
						else{
							pagingHtml += 'Results from '+  (startIndex * 1 + 1) + " to ";
							if (totalResults < (startIndex * 1 + itemsPerPage*1)) pagingHtml += totalResults;
							else pagingHtml += (startIndex * 1 + itemsPerPage*1) + " out of " + totalResults;
							if ($(this).data("catalogue:duration")!="") pagingHtml += " (" + getXPathValue(data,$(this).data("catalogue:duration")) +")";
							pagingHtml += ' <br/>';
							var nextPage = getXPathValue(data, $(this).data("catalogue:nextPage"));
							var prevPage = getXPathValue(data, $(this).data("catalogue:prevPage"));
							if (prevPage !="") pagingHtml += "<a href='" + prevPage +"'> prev page </a> |"
							if (nextPage !="") pagingHtml += "| <a href='" + nextPage +"'> next page </a>"
						}
					}
					else{
						pagingHtml += " No results found ";
					}
					var myDataset = $(this);
               var dataset_Query_Status = $(this).parent().parent().children(".dataset_query_status").find("span");
               dataset_Query_Status.html(pagingHtml);

               dataset_Query_Status.find("a").click(function (e){
                  myDataset.trigger("deletefeature",
                     [myDataset.find("option:not(:selected)")]);

                  myDataset.trigger('beforequery');
                  dataset_Query_Status.addClass("waiting");
                  dataset_Query_Status.addClass("query_message");
                  dataset_Query_Status.removeClass("ifyMessage");

                  ExecuteUrlQuery($(this).attr("href"), myDataset);
                  e.preventDefault();
               });

		}
