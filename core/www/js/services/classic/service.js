/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/
// javascript code for the generic part of the classic service GUI  widget on the service page


$(document).ready(function() {
		$('#' + $("#ifyServiceName").val() + '_status').html('');
		$('.query_message').html('');


		// Adds a validation span to the elements
		$("#" + $("#ifyServiceName").val()).find(":input[name][type!='hidden'][type!='button']:not(.dataset)").each(function (i) {
						$(this).before($("<span></span>")
							.attr({id:'_'+this.name+'_status'})
							.addClass('serviceFieldValidity')
						);
					});
		 $(":input[name!='']").change(function () {
			$(this).parent().find(".serviceFieldValidity").removeClass("serviceFieldInvalid");
		 })

		 
		// Link events to the parameters here
			$(".dataset").bind('OnError:Empty', function(event) {	
				var textTag = $(this).parent().find("label").text();
				var errMsg = "Please select a value for " + textTag;
				if (textTag.indexOf("Files") != -1) errMsg = "Please select the list of files to process (minimum 1)";
				displayMessage("error", errMsg);
				return false;
			})

		 
		 // Links the event to the service create buttons
			
			$(".ify_operation").bind('OnError:Post', function(event, HTTPRequest) {
				displayMessage("error", 'SYSERRP6: Unable to post values due to network or system error. Please try again later');
				// if(IfyIsUserAdmin) window.open().document.write
				// (XMLHttpRequest.responseText);
				return false;
			})
			
			
			$(".ify_operation").bind('OnError:Creation', function(event, msg) {
				var txt ="";
				$(msg).each(function(){
					txt += $(this).text() + '\n';		
				})					
				displayMessage("error", txt + ' Please review the parameters accordingly');
				return false;
			})


			$(".ify_operation").bind('OnError:Missing', function(event) {
				displayMessage("displayMessage", 'Please complete all the mandatory values for this task');
				return false;
			})
			
			
			$(".ify_operation_create").bind('OnSuccess', function(event, task, response) {
				displayMessage("info", 'Task '+ $(task).find('description:first').text() +' ('+$(task).attr('uid')+') was created '); 
				return true;
			})
			$(".ify_operation_submit").bind('OnSuccess', function(event, task, response) {
				window.location = '/tasks/?uid=' + $(task).attr('uid');
				return true;
			})
			
			
			
			$(".ify_operation").bind('submit', function(event, myUrl) {
				
				
				// there are two events before the actual submission
				// and one extra event on the case of invalid or empty
				// submission
				if ($(this).triggerHandler('OnBeforeValidate')==false) {return false};
				var allValuesIn=true;
				// first check if the dataset element has any selected file
				myDataset = $("#" + $("#ifyServiceName").val()).find(".dataset");
				// this checks if a dataset is present in this service and if
				// it's mandatory
				if( myDataset.length!=0){
					if(! myDataset.val()){
						if (myDataset.triggerHandler('OnError:Empty')==false) {return false};
					}
					
					// first we have to load the metadata values of the dataset
					// elements
					$("._dataset > .dataset_aux_info").each(function(){
						var elMetadata=$(this).data("metadata");
						var indexMetadata = -1;
						var tmp=Array();
						// check the index of the metadata
						$.each($(this).parent().find(".dataset").data("metadataDef"),function(ii){							
							if (this[0]==elMetadata){indexMetadata=ii}	
						});
						if(indexMetadata>-1){
							var mytempel=this;
							var IHaveAllMyValues=true;
							$(this).parent().find(".dataset").find("option:selected").each(function(){
								tmp[tmp.length]=$(this).data("metadata")[indexMetadata];
								if ( ( tmp[tmp.length-1] == '' ) && (!$(mytempel).hasClass("ifyOptional")) ){
									IHaveAllMyValues=false;
								}
							})
							if (!IHaveAllMyValues){
								allValuesIn=false;
								if ($(this).triggerHandler('OnError:Empty', mytempel)==false){return false };
							}
						}
						$(this).val(tmp.join(","));
					});
				}

				// check if the non-optional params have a value
				if (allValuesIn){
					$(":input[name!='']").each(function (){
						// alert(this.name + '=' + $(this).val() + '(' +
						// $(this).hasClass("ifyOptional") +')');
						// if ( ( $(this).val()=='' || $(this).val()=='?') &&
						// (!$(this).hasClass("ifyOptional")) ){
						if ( ( this.value == '' ) && (!$(this).hasClass("ifyOptional")) ){
							$(this).addClass("inputInvalidValue");
							$(this).parent().find(".serviceFieldValidity").addClass("serviceFieldInvalid");
							allValuesIn=false;
							if ($(this).triggerHandler('OnError:Empty', this)==false){return false };
						}
					})
				}
				if (!allValuesIn){
					return $(this).triggerHandler('OnError:Missing');
				}

				if ($(this).triggerHandler('OnBeforeSubmit')==false) return false;

				
				
				var values = $("#" + $("#ifyServiceName").val()).serializeArray();
				// alert($("#" + $("#ifyServiceName").val()).serialize());
				// values[values.length]= {name : '_request', value:'create'};
				values[values.length]= {name : '_format', value:'xml'};
				
				$('#' + $("#ifyServiceName").val() + '_status').addClass('processing');
				$(this).parent().parent().find('.ifyOperation').addClass('ifyInvisible');

				$.ajax({
					type: "POST",
					url: myUrl, // $("#ifyServiceUrl").val(), //
					dataType:"xml",
					data: values,
					async:true,
					myself:this,
					caller:$(this).val(),
					element:$("#ifyServiceName").val(),
					error : function (XMLHttpRequest, textStatus, errorThrown) {
						$('#'+this.element+'_status').removeClass('processing');
						$(this.myself).parent().parent().find('.ifyOperation').removeClass('ifyInvisible');
						$(this.myself).trigger('OnError:Post',XMLHttpRequest, textStatus, errorThrown);
					},
					/*
					 * complete: function (XMLHttpRequest, textStatus){
					 * window.open().document.write
					 * (XMLHttpRequest.responseText); },
					 */
					success: function (data, textStatus, XMLHttpRequest) {
						var error=false;
						$(this.myself).parent().parent().find('.ifyOperation').removeClass('ifyInvisible');

						var msg = $(data).find('message');
						if ( msg.attr('type')=='error'){
							$(data).find('item').contents().each(function (i) {						
								if ( $(this).attr('valid')=='false'){
// alert($(this).context.nodeName + $(this).attr('message'));
									$('#_'+$(this).context.nodeName+'_status').addClass('serviceFieldInvalid');
									$('#_'+$(this).context.nodeName+'_status').attr('title', 'This value is reported as invalid');
									$('#_'+$(this).context.nodeName+'_status').attr('title', $(this).attr('message'));
									error=true;
									$('#'+$(this).context.nodeName).triggerHandler('OnError:Invalid',this, data);
								}
								
							})
							$(this.myself).triggerHandler('OnError:Creation', msg, data);
							
						}
						// else{
						if (!error){
							if ($(data).find('singleItem').attr('entity')=='Scheduler'){
								// alert (XMLHttpRequest.responseText);
								// alert($(data).find('singleItem').attr('link'));
								window.location = $(data).find('singleItem').attr('link');
								return true;
							}
							
							var task = $(data).find('task');
							if (! task.attr('uid')){displayMessage("error",'SYSERR_7: No task ID');return false};
							$(this.myself).triggerHandler('OnSuccess',task, data);

						}
						$('#'+this.element+'_status').removeClass('processing');
				
					}
				})
			});

		
		$("._geo_box input").change(function (){
			$("._geo_box_configurable select").val($(this).val());
		})

		$("._ifyConfigurable select").click(function (){			
			if ($(this).find("option").length==1){
				loadConfigurableValues( $(this) );				
			}
		});
		
		
		
		$("._ifyConfigurable input").click(function (ev){
			ev.stopPropagation();
		});

		$("._ifyConfigurable .ifyIcon-add").click(function (ev){
			ifyConfigurableScope = $(this).parent();
			ifyConfigurableScope.find("select").addClass("ifyInvisible");
			ifyConfigurableScope.find("input").removeClass("ifyInvisible");
			ifyConfigurableScope.find(".ifyIcon-save").removeClass("ifyInvisible");
			ifyConfigurableScope.find("input").focus();
			ev.stopPropagation();						
			$(document).click(ifyClickOutsideHandler);	
			$(document).keyup(ifyKeyUpHandler);

		});
		$("._ifyConfigurable .ifyIcon-save").click(function (){
			if (ifyConfigurableScope.find("input").val()!='') addConfigurableValue( ifyConfigurableScope.find("select"), ifyConfigurableScope.find("input").val());
			ifyClickOutsideHandler();
			/*
			 * $(this).parent().find("select").removeClass("ifyInvisible");
			 * $(this).parent().find("input").addClass("ifyInvisible");
			 * $(this).parent().find(".ifyIcon-save").addClass("ifyInvisible");
			 * $(document).unbind('click',ifyClickOutsideHandler);
			 */
		})
		
		var ifyConfigurableScope;
		var ifyKeyUpHandler = function(event) {	
			switch(event.keyCode){
				case 13:
					event.preventDefault();
					ifyConfigurableScope.find(".ifyIcon-save").click();
					break;
				case 27:
					event.preventDefault();
					ifyClickOutsideHandler();
					break;
				default:
			   }
			}		
		var ifyClickOutsideHandler = function() {	
			ifyConfigurableScope.find("select").removeClass("ifyInvisible");
			ifyConfigurableScope.find("input").addClass("ifyInvisible");
			ifyConfigurableScope.find(".ifyIcon-save").addClass("ifyInvisible");
			$(document).unbind('keyup',ifyKeyUpHandler);
			$(document).unbind('click',ifyClickOutsideHandler);
			ifyConfigurableScope = null;
		};

		$("._ifyConfigurable .ifyIcon-del").click(function (){
			if ($(this).parent().find("select option:selected").val()!=''){
			
				if ($(this).parent().hasClass("_ifyConfigurableFromTextFile")){
					removeConfigurableValue( $(this).parent().find("select"), $(this).parent().find("select option:selected").val());
					}
				else{
					removeConfigurableValue( $(this).parent().find("select"), $(this).parent().find("select option:selected").text());
				}
			}else
			{
			displayMessage("warning", "Please select item to delete");
			}
		})

		$(".ifyServiceAoi").change(function (){	
			$("#"+$(this).data("owner")).val($(this).val());
			$("#"+$(this).data("owner")).change();
			OSMap.zoomToExtent(new OpenLayers.Bounds.fromString($(this).val()));
		})

		$(".ifyServiceAoi").click(function (){	
			if ($(this).find("option").length==1){
				myself=this;
				$.ajax({
					type: "GET", url: $(myself).data("AOI"),
					dataType:"xml", async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
					},
					success: function (data) {
						var output = [];
						$(data).find("item").each(function(){
							output.push('<option value="'+ $(this).attr('value') +'">'+ $(this).text() +'</option>');
						});
						$(myself).html(output.join(''));

					}
				})
			}
		});

		$(".ifyLookup input[type='button']").click(function (){
			var mySelect = $(this).parent().find("select");
			mySelect.trigger('beforequery');
			lookup_Query_Status =$('#' +mySelect.attr('id')+ '_query_status');
			lookup_Query_Status.removeClass("ifyMessage");
			lookup_Query_Status.addClass("waiting");
			lookup_Query_Status.html('Retrieving OpenSearch information from <br/>' + mySelect.data('engine') );
			var OpenSearchTemplateUrl = getOpenSearchTemplate ( 
				mySelect.data('engine'), 
					mySelect.data('template'),
						mySelect,
							function (url,selList){
								var myUrl=getOpensearchUrl(url, selList);
								if (myUrl){ExecuteUrlQuery(myUrl, selList);}
							}
				);			 
		});

		$(".ifyLookup select").change(function (){
			// $(this).trigger('newfeature',[$(this).children("option:selected")]);
			$("#" + $(this).data("owner")).val($(this).find("option:selected").data("spatial"));
			auxFeaturesLayer.destroyFeatures();
			$(this).data("OS_feature_id",null);
			if ($(this).find("option:selected").data("spatial")){
				var newgeom = new OpenLayers.Feature.Vector(
						(new OpenLayers.Geometry.fromWKT($(this).find("option:selected").data("spatial"))).transform(epsg4326, OSMap.getProjectionObject()),{								
							identifier : $(this).find("option:selected").data('identifier'),								
							metadata : $(this).find("option:selected").data('metadata')
						}
					);				
				auxFeaturesLayer.addFeatures(newgeom);
				$(this).data("OS_feature_id",newgeom.id);
			}
		})
		
		/*
		 * BY MANU 2010/09/07 to be released for multiple lookup selection //
		 * This function is triggered when a select input type selection is
		 * modified $(".ifyLookup select").change(function (){ // First, remove
		 * previous features from map features
		 * auxFeaturesLayer.destroyFeatures(); // Save select element in context
		 * var context = $(this); // Prepare a geometry collection for the owner
		 * of the select // next line TO BE UNCOMMENTED when Openlayers will
		 * have patched Geometry.Collection WKT bug //var geometryCollection =
		 * new OpenLayers.Geometry.Collection()); var geometryCollection = new
		 * Array(); // For each selected element
		 * $(this).find("option:selected").each(function() { // Add it as well
		 * in the map features var newgeom = new OpenLayers.Feature.Vector( (new
		 * OpenLayers.Geometry.fromWKT($(this).data("spatial"))).transform(epsg4326,
		 * OSMap.getProjectionObject()),{ identifier :
		 * $(this).data('identifier'), metadata : $(this).data('metadata') } );
		 * auxFeaturesLayer.addFeatures(newgeom); // add WKT to the geometry
		 * collection geometryCollection.push(newgeom.geometry.toString()); })
		 * $("#" +
		 * $(context).data("owner")).val('GEOMETRYCOLLECTION('+geometryCollection.join(",")+')');
		 * alert($("#" + $(context).data("owner")).val()); })
		 */
		
		
		

	$(".ifyRangeDiv input").change(function (){
		var el = $(this).parent().parent().find(".ifyRange");
	// alert($(el).val());
		$(el).val("[" + $(this).parent().find(".ifyRangeFrom").val() + ","  + $(this).parent().find(".ifyRangeTo").val() + "]"); 
	})
	
})

function loadConfigurableValues( myself, callback ){
				// myself=this;
				// window.open("/service.parameter.aspx?service="+
				// $(myself).data("serviceid") + "&name=" +
				// $(myself).data("configures") + "&_format=xml","new2");
				$.ajax({
					type: "GET", url: "/service.parameter.aspx?service="+$(myself).data("serviceid")+"&name="+$(myself).data("configures")+"&_format=xml",
					dataType:"xml", async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
						displayMessage("error", "Error! : " + $(XMLHttpRequest.responseXML).find("message").text());
					},
					success: function (data) {
						
						var output = [];
						if ($(data).find($(myself).data("configures") + " element").length ==0){
							output.push('<option value=""> no values available </option>');
						}
						else{
							output.push('<option value=""> -- </option>');
							$(data).find($(myself).data("configures") + " element").each(function(){
								output.push('<option value="'+ $(this).attr('value') +'" class="ify_' + $(this).attr('scope') + '_configurable">'+ $(this).text() +'</option>');
							});
						}
						$(myself).html(output.join(''));
						if (callback){callback(myself)};
					}
				})
}

function addConfigurableValue( el, caption ){
				// myself=this;
				var values = new Array();
				displayStatusWait(el,"submitting operation ...");
				var val = $("#" + $(el).data("configures")).val();
				values[values.length]= {name : 'value', value:val};
				$.ajax({
					type: "POST", url:  "/service.parameter.aspx?service="+$(el).data("serviceid")+"&caption="+encodeURIComponent(caption)+"&name="+$(el).data("configures")+"&_format=xml&_request=create",
					// dataType:"xml",
					data: values,
					async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
							displayMessage("error", $(XMLHttpRequest.responseXML).find("message").text());
							removeStatusWait(el);
					},
					success: function (data) {
						var msg = '';
						$(data).find("message").each(function(){
							displayMessage($(this).attr("type"),$(this).text());
							msg+=$(this).text();
						});
						removeStatusWait(el);
						displayMessage("info","File parameter saved");
						loadConfigurableValues(el,function (el){$(el).val(val)});
						/*
						 * var output = []; output.push('<option value=""> --
						 * </option>'); $(data).find("bbox
						 * element").each(function(){ output.push('<option
						 * value="'+ $(this).attr('value') +'">'+ $(this).text() +'</option>');
						 * }); $(myself).html(output.join(''));
						 */

					}
				})
}

function getConfigurableValue( el, caption ){
				// window.open("/service.parameter.aspx?service=" +
				// $(el).data("serviceid") + "&caption=" + caption + "&name=" +
				// $(el).data("configures"),"new3");
				$.ajax({
					type: "GET", 
					url: "/service.parameter.aspx?service=" + $(el).data("serviceid") + "&caption=" +  encodeURIComponent(caption) + "&name=" +
								$(el).data("configures"), // +"&_format=xml",
					async: true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
							displayMessage("error","Error! : " + $(XMLHttpRequest.responseXML).find("message").text());
								},
					success: function (data) {
						$("#" + $(el).data("configures")).val(data);
					}
				})
}


function removeConfigurableValue( el, caption ){
				// myself=this;
				var val = $("#" + $(el).data("configures")).val();
				$.ajax({
					type: "GET", url: "/service.parameter.aspx?service="+$(el).data("serviceid")+"&caption="+encodeURIComponent(caption)+"&name="+$(el).data("configures")+"&_format=xml&_request=delete",
					// dataType:"xml",
					async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
						displayMessage("error","Error! : " + $(XMLHttpRequest.responseXML).find("message").text());
					},
					success: function (data) {						
						loadConfigurableValues(el,function (el){$(el).val(val)});
					}
				})
}


function getMetadataIndex(el, name){
				var rr = null;
				$.each(el.data("metadataDef"), function (ii, vv){
					if (this[0]==name) { rr = ii; return false};		
				})
				return rr;
				
			}

function getMetadataIndexByName(el, name){
	var rr = null;
	$.each(el.data("metadataDef"), function (ii, vv){
		if (this[2]==name) { rr = ii; return false};		
	})
	return rr;
	
}
			
function isDateValid(date, showError)
{
	var validDateTimeFormat=/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$/ // Basic
																	// check for
																	// format
																	// validity
	var validDateFormat=/^\d{4}-\d{2}-\d{2}$/ // Basic check for format
												// validity
	
	var returnval=false
	if ((!validDateTimeFormat.test(date.replace(' ',''))) && (!validDateFormat.test(date.replace(' ','')))){
			if (showError) displayMessage("error","Invalid date format.")
	} else  { 
		var yearfield=date.split("-")[0];
		var monthfield=date.split("-")[1];
		var dayfield=date.split("-")[2].split("T")[0];
		var dayobj = new Date(yearfield, monthfield-1, dayfield);
		if ((dayobj.getMonth()+1!=monthfield)||(dayobj.getDate()!=dayfield)||(dayobj.getFullYear()!=yearfield)) { 				
			if (showError) displayMessage("error", "Invalid day, month, or year range.")
		} else {
			returnval=true;
		}
		if ( (returnval) && (date.indexOf('T')!=-1)){
			var time = date.split("-")[2].split("T")[1].split(':');
			if ((time[0]>23) || (time[0]<0)) {
				if (showError) displayMessage("error", "Invalid hour on date value.")
				return false;
			}
			if ((time[1]>59) || (time[1]<0)) {
				if (showError) displayMessage("error", "Invalid minute on date value.")
				return false;
			}
			if ((time[2]>59) || (time[2]<0)) {
				if (showError) displayMessage("error", "Invalid seconds on date value.")
				return false;
			}
		}
	}
	return returnval;
}

