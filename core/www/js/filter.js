
// task filter js code
// admin/task.aspx 


$(document).ready(function(){

		if (!$("#ifyExtendedSearch").is(':visible'))$("#_task_filter_configurable").hide();
			
		$("#ifyExtendedSearchButton").click(function (){
			if($('#ifyExtendedSearch').is(':visible') ) {
				$("#_task_filter_configurable").show();		
			}
			else{
				$("#_task_filter_configurable").hide();			
			}
				;
		})
		/*
		$(".ifyFilter").click(function (){			
			if ($(this).find("option").length==1){
				loadConfigurableValues( $(this) );				
			}
		});
		*/
		$(".ifyFilter").change(function(){
			if ($(this).val()!='') window.location.href = $(this).val();	
		});
		
		
		$("._ifyFilter .ifyIcon-add").click(function (ev){
			ifyConfigurableScope = $(this).parent();
			ifyConfigurableScope.find("select").addClass("ifyInvisible");
			ifyConfigurableScope.find("input").removeClass("ifyInvisible");
			ifyConfigurableScope.find(".ifyIcon-save").removeClass("ifyInvisible");
			ifyConfigurableScope.find("input").focus();
			ev.stopPropagation();						
			//$(document).click(ifyClickOutsideHandler);	
			//$(document).keyup(ifyKeyUpHandler);

		});
		$("._ifyFilter .ifyIcon-save").click(function (){
			if (ifyConfigurableScope.find("input").val()!='') {
				addConfigurableValue( ifyConfigurableScope.find("select"), ifyConfigurableScope.find("input").val());
			
				
			}
			ifyClickOutsideHandler();
			
			
			/*
			$(this).parent().find("select").removeClass("ifyInvisible");
			$(this).parent().find("input").addClass("ifyInvisible");
			$(this).parent().find(".ifyIcon-save").addClass("ifyInvisible");
			$(document).unbind('click',ifyClickOutsideHandler);
			*/
		})

		$("._ifyFilter .ifyIcon-del").click(function (){
			if ($(this).parent().find("select option:selected").val()!=''){			
				removeConfigurableValue( $(this).parent().find("select"), $(this).parent().find("select option:selected").attr("id"));
			}else{
				displayMessage("warning", "Please select item to delete");
			}
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

		
});


function loadConfigurableValues( myself, callback ){
	//myself=this;
	//window.open("/service.parameter.aspx?service="+ $(myself).data("serviceid") + "&name=" + $(myself).data("configures") + "&_format=xml","new2");
	$.ajax({
		type: "GET", url: $(myself).data("url"),
		dataType:"xml", async:true,
		error : function (XMLHttpRequest, textStatus, errorThrown) {
			displayMessage("error", "Error! : " + $(XMLHttpRequest.responseXML).find("message").text());
		},
		success: function (data) {
			
			var output = [];
			if ($(data).find("element").length ==0){
				output.push('<option value=""> no values available </option>');
			}
			else{
				output.push('<option value=""> -- </option>');
				$(data).find("element").each(function(){
									// this will change for the link
					output.push('<option value="'+ $(this).attr('link') +'" id="' + $(this).attr('token') + '">'+ $(this).text() +'</option>');
				});
			}
			$(myself).html(output.join(''));
			
			if (callback){callback(myself)};
		}
	})
}

function checkConfigurationValues( el ){
	el.val('/admin/tasks?' + unescape(el.data("form").serialize().replace(/\+/g," ")));
}
	
function addConfigurableValue( el, caption ){
	//myself=this;
	//displayStatusWait(el,"submitting operation ...");
	var values = new Array();
	values[0] = {name : '_request', value : 'create'};
	values[1] = {name : 'e', value : '1'};
	values[2] = {name : 'link', value : '/admin/tasks?'+ $("#ifySearchForm").serialize()};
	values[3] = {name : 'caption' , value : caption};
	
	$.ajax({
		type: "GET", 
		url:  "/account/filters",
		data: values,
		async:true,
		error : function (XMLHttpRequest, textStatus, errorThrown) {
			displayMessage("error", $(XMLHttpRequest.responseXML).find("message").text());
			removeStatusWait(el);
		},
		success: function (data) {
			var msg = '';
			if ($(data).find("message").length>0){
				$(data).find("message").each(function(){
					displayMessage($(this).attr("type"),$(this).text());
					
				});
				return false;
			}
			displayMessage("info","File parameter saved");
			val = $(data).find("filter").attr('link') ;
			loadConfigurableValues(el,function (el){$(el).val(val)});
		}
	})
}


function removeConfigurableValue( el, token ){
	$.ajax({
		type: "GET", url: "/account/filters?_request=delete&token="+ token,
		//dataType:"xml",
		async:true,
		error : function (XMLHttpRequest, textStatus, errorThrown) {
			displayMessage("error","Error! : " + $(XMLHttpRequest.responseXML).find("message").text());
		},
		success: function (data) {						
			loadConfigurableValues(el,function (el){""});
		}
	})
}


