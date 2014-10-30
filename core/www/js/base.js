/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 	info@terradue.com
*/
var ProxyHost = "/proxy4.aspx";
var IfyIsUserAdmin= true;
 function leftTrim(sString){
	while (sString.substring(0,1) == ' '){String = sString.substring(1, sString.length);}
	return sString;
 }
 function rightTrim(sString){
	while (sString.substring(sString.length-1, sString.length) == ' '){sString = sString.substring(0,sString.length-1);}
	return sString;
 }


function trim(sString){
	return rightTrim(leftTrim(sString));
}

Array.prototype.remove = function(from, to) {
  var rest = this.slice((to || from) + 1 || this.length);
  this.length = from < 0 ? this.length + from : from;
  return this.push.apply(this, rest);
};
Array.prototype.last = function() {return this[this.length-1];}

Array.prototype.clear=function(){
      this.length = 0;
};

// from the site.xml definition 
// 
var TRUE = 'true';
var FALSE = 'false';

// debug function
	function alertObj(pResult){	
		var pstr="";
		for (i in pResult){if (i!='channel'){pstr+=i+"="+pResult[i]+"\n"}}
		//document.write(pstr);
		alert("alertObj: " + pstr);
	}


$(document).ready(function(){
	$(".ifyHelpButton").html('');
	$(".ifyIcon").html('');
	$(".ifyStatus").html('');
	//$('.itemContent').disableTextSelect();//No text selection on elements with a class of 'noSelect'
	
	$("#ifyExtendedSearchButton").click(function (){
		$('#ifyExtendedSearch').toggle();
		if($(this).html()=='more...') 
			{$(this).html('less...') }
		else {$(this).html('more...');}
	})

	$('.ifyBoolCheckBox').click(function (){
		var el = $(this).parent().find('.ifyBool');
			if ($(this).attr("checked")==true || $(this).attr("checked")=="checked") {$(el).val(TRUE);}
			else {$(el).val(FALSE)};
	})

	
	$(".ifyHelpButton").click(function (){
		if ($(this).hasClass("ifyHelpButtonOn")){
			$(this).removeClass("ifyHelpButtonOn");
		}
		else{
			if ($(this).hasClass("ifyHelpButtonLoaded")){
				$(myself).addClass("ifyHelpButtonOn");
			}
			else{
				$(this).addClass("ifyHelpButtonWaiting");
				myself=this;
				$.ajax({
					type: "GET", url: "/site.xml",
					dataType:"xml", async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
					//		$(myself).removeClass("ifyHelpButtonWaiting");
					},
					success: function (data) {
						$(data).find("item").each(function(){					
							$($(this).attr("type")).wTooltip({
								content: $(this).text(), style: false,offsetX: 20,className:"ifyHelp",
								callBefore: function(tooltip, node, settings){ if ($(myself).hasClass("ifyHelpButtonOn")){settings.auto=true}else{settings.auto=false;}}
							});
						});
						//$(myself).removeClass("ifyHelpButtonWaiting");
						$(myself).addClass("ifyHelpButtonOn");
						$(myself).addClass("ifyHelpButtonLoaded");
					}
				})
				//alert(document.location.pathname.replace('.aspx',''));
				$.ajax({
					type: "GET", url: document.location.pathname.replace('.aspx','')+'help.xml',
					dataType:"xml", async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
							$(myself).removeClass("ifyHelpButtonWaiting");
					},
					success: function (data) {
						$(data).find("item").each(function(){					
							$($(this).attr("type")).wTooltip({
								content: $(this).text(), style: false, offsetX: 20, className:"ifyHelp",
								callBefore: function(tooltip, node, settings){ 
									if ($(myself).hasClass("ifyHelpButtonOn")){settings.auto=true}else{settings.auto=false;}}
							});
						});
						$(myself).removeClass("ifyHelpButtonWaiting");
					}
				})

			}
		}

	});

	$("#login_form").submit(function(){
		return login_form_bind ( 
			function (){window.location.reload()},
			null);
 	});
	//now call the ajax also focus move from 
//	$("#login_password").blur(function(){
//		$("#login_form").trigger('submit');
//	});

	$(".int, .float").each(function(){
		if ( !($(this).hasClass("ifyOptional")) ){$(this).data("value",this.value);}
	})
	
	$(".float").change(function(event){
		if ( (this.value=="") && ($(this).hasClass("ifyOptional")) ){return true;}
		if (!(/^(\+|-)?[0-9]\d*(\.\d*)?$/.test(this.value))){
			this.value="";
			if ($(this).data("value")){this.value=$(this).data("value")};
			event.stopPropagation();
			return false;
		}
		$(this).data("value",this.value);
		return true;
	});


	$(".int").change(function(){
		if ( (this.value=="") && ($(this).hasClass("ifyOptional")) ){return true;}
		if (!(/^\d+$/.test(this.value))){
			this.value="";
			if ($(this).data("value")){this.value=$(this).data("value")};
			return false;
		}
		$(this).data("value",this.value);
		return true;
	});

	$(".ifyPassword").each ( function (){
		$(this).data("name",$(this).attr("name"));
	});
	$(".ifyPassword").change(function (){
		//$(this).data("set", true);
		if ($(this).val() != $(this).parent().find(".ifyPasswordCheck").val()){
			$(this).parent().find(".ifyPasswordCheck").addClass("ifyWrongPassword");
			$(this).attr("name","");			
		}else{
			$(this).parent().find(".ifyPasswordCheck").removeClass("ifyWrongPassword");
			$(this).attr("name",$(this).data("name"));
		}
	})
	$(".ifyPasswordCheck").change(function (){
		if ($(this).val() != $(this).parent().find(".ifyPassword").val()) {
			$(this).addClass("ifyWrongPassword");
			$(this).parent().find(".ifyPassword").attr("name","");
		}else{
			$(this).removeClass("ifyWrongPassword");
			$(this).parent().find(".ifyPassword").attr("name",$(this).parent().find(".ifyPassword").data("name"));
		}
	})
		
	$(".ifyConfigurable").change ( function (){
		$("#" + $(this).data("configures")).val($(this).val());
		$("#" + $(this).data("configures")).change();
	})
	
});

function login_form_bind ( login_callback, error_callback ){
		$("#login_msgbox").html('Validating ...');
		$.ajax({
			type: "POST",
			url: "/account/signin?_format=xml",
			dataType:"xml",
			data: ({username : $('#login_username').val(), password:$('#login_password').val()}),
			error : function (XMLHttpRequest, textStatus, errorThrown) {
				//alertObj(XMLHttpRequest);
				var msg = $(XMLHttpRequest.responseXML).find("message").text();
				if (msg) $("#login_msgbox").html(msg)
				else alert('IDCD Error 4: Unable to login. Please contact your administrator if it persists.');	
			},

			success: function (data, textStatus) {
				// data could be xmlDoc, jsonObj, html, text, etc...
				var msg = $(data).find('message[type="warning"]');
				if (msg.text()) alert(msg.text());
				var msg = $(data).find('message[type="info"]');
				if (msg.text()) {
					$("#login_msgbox").html(msg.text() + ' - please wait for page redirect');
					login_callback();
				}
				var msg = $(data).find('message[type="error"]');
				if (msg.text()){
					$("#login_msgbox").html(msg.text());
					if(error_callback) error_callback();
				}
			}
		})
 		return false; //not to post the form physically
}

function getOpenSearchTemplate ( urlTemplate, templateType, selectList, callback){
		$.ajax({
				type: "GET",
				url: ProxyHost,
				dataType:"xml",
				data : [{name:'url',value:urlTemplate}],
				async : false,
				error : function (XMLHttpRequest, textStatus, errorThrown) {
					displayMessage('error','Unable to retrieve OpenSearch Template from Catalogue due to network or system error ('+textStatus+'). Please try again later');
					$('#' + selectList.attr('id')+ '_query_status').removeClass("waiting");
					$('#' + selectList.attr('id')+ '_query_status').addClass("ifyMessage");
					$('#' + selectList.attr('id')+ '_query_status').html('Unable to post values due to network or system error (' + textStatus + ').<br/> Please try again later');
					$("#" + selectList.attr('id')+'_button_query').removeAttr("disabled");
				},
				success: function (data, textStatus) {
					var temp =  $(data).find("Url[type='" + templateType + "']").attr('template');
					if (temp ){						
						if (callback){ 
							callback (  temp  , selectList )
						}
						else {return temp;
						}
					}
					else{
					displayMessage('error','Could not find OpenSearch Template from Catalogue of the type requested ('+templateType+') on '+ urlTemplate +'.');
					$('#' + selectList.attr('id')+ '_query_status').removeClass("waiting");
					$('#' + selectList.attr('id')+ '_query_status').addClass("ifyMessage");
					$('#' + selectList.attr('id')+ '_query_status').html('Could not find OpenSearch Template of the type requested ('+templateType+') on <a href="'+ urlTemplate +'">'+ urlTemplate +'</a>.'
						//+ '<a <a style="cursor: pointer;" onclick="$(/'#/' + selectList.attr(/'id/').data(">try with application/xhtml+xml</a>'
						);
					$("#" + selectList.attr('id')+'_button_query').removeAttr("disabled");	
					}
				}
			})
}


//function ExecuteQuery ( urlTemplate, selectList, callback, scope ){
function getOpensearchUrl ( urlTemplate, selectList, scope){ 
	var url = urlTemplate;
	var validUrl = true;
		var query = url.substr(url.indexOf('?')+1).split("&");
		url = url.substring(0,url.indexOf('?')+1);
		var el;
		if (scope) el = scope 
		else{
			el = ".catalogue_query_extension :input";
			if (selectList.data("scope")) el = selectList.data("scope");// +  " :input";		
		}
		for(var i=0; i<query.length; i++) {
			var param = query[i].replace('{','').replace('}','').split('=');
			param[0] = "" + param[0] + "";
			if ((param[0]+"="+param[1])==query[i]){
			
			}
			else{
			switch(param[1].replace('?','')){
				case "___count": 
					param[1]=selectList.data("count");
					break;
				case "startPage":
					param[1]=selectList.data("startPage");
					break;
				case "startIndex":
					param[1]=selectList.data("startIndex");
					break;		
				default:
					var search=param[1];
					if ((param[1].replace('?','')=="count")&&(selectList.data("count"))){
						param[1]=selectList.data("count");
					}else{
						param[1]="";
						$(el).each(function (i) {
							if ($(this).data('ext')==search.replace('?','')){
								if ( ($(this).val()=="") && ( (! $(this).hasClass("ifyOptional")) || (search.indexOf('?')==-1) ) ){
									$(this).parent().find(".serviceFieldValidity").addClass("serviceFieldInvalid");
									validUrl=false;
								}
								param[1]="" + $(this).val() + "";
								return true;
							}
						})
					}
			}
			}
			query[i]=param.join('=');
		}

		if(validUrl){ return url+query.join('&')}
		else {return false}
		//if (callback) callback(url+query.join('&'), selectList)
		//else ExecuteUrlQuery(url+query.join('&'), selectList);
		//}
}

function ExecuteUrlQuery ( url, selectList, callbackfunction, dontuseproxy){
		var dataset_Query_Status =$('#' +  selectList.attr('id')+ '_query_status');
		dataset_Query_Status.html('Executing query on <br/>' + url.split('?')[0]);
		var request;
		var myurl;
		if (dontuseproxy){
			request=null;
			myurl = url;
		}
		else{
			request = eval('({url:"'+ encodeURI(url) +'"})');
			myurl = ProxyHost + '?ct=text/xml';
		}
		$.ajax({
			type: "GET",
			url: myurl,
			dataType:"xml",
			data: request,
			async:true,
			cache: true,
			error : function (XMLHttpRequest, textStatus, errorThrown) {
				dataset_Query_Status.removeClass("waiting");
				dataset_Query_Status.addClass("ifyMessage");
				dataset_Query_Status.html('SYSERRP2: Unable to post values due to network or system error (' + textStatus + ').<br/> Please try again later');
				$("#" + selectList.attr('id')+'_button_query').removeAttr("disabled");
			},
			success: function (data, textStatus) {
				// check if there is a relation -> not needed 
				/*
				var masterRelation="" 
				$(data).find(selectList.data("catalogue:series").replace(":","\\:") + ":has(dc\\:relation)").each(function() {
					masterRelation=$(this).attr('rdf:about');
				});
				*/
				var t1 = new Date();
				selectList.trigger('receivedresults',data);
				var cc = LoadQueryResult ( data, selectList );
				var t2 = new Date();
				//alert( t2 - t1);
				dataset_Query_Status.removeClass("waiting");
				dataset_Query_Status.addClass("ifyMessage");				
				//dataset_Query_Status.html('Received new ' + cc +  ' entries('+$(data).find("dc\\:SizeOrDuration").text()+')');
				var msgTxt = 'Received new ' + cc +  ' entries';
				if (selectList.data("catalogue:duration")!="") msgTxt += " (" + getXPathValue(data,selectList.data("catalogue:duration")) +")";
				dataset_Query_Status.html(msgTxt);
				$("#" + selectList.attr('id')+'_button_query').removeAttr("disabled");
				selectList.trigger('afterquery',data);
				if (callbackfunction) callbackfunction(selectList);
			}
		})
}


function LoadQueryResult ( xml, selectList ){
		var cc=0;
		//try{xml.documentElement.ownerDocument.setProperty("SelectionLanguage","XPath");}catch(err) {};
		$(xml.selectNodes(selectList.data("catalogue:dataset"))).each(function (){
			try{
				// IE code for XPATH
				this.ownerDocument.setProperty("SelectionLanguage","XPath");
				this.ownerDocument.setProperty("SelectionNamespaces",selectList.data("catalogue:namespaces"));
			}catch(err) {};
			var seriesResource = getXPathValue(this, selectList.data("catalogue:datasetseries"));
			series = xml.selectSingleNode(selectList.data("catalogue:series").replace('$datasetseries',"'"+seriesResource+"'"));
			var value = getXPathValue(this, selectList.data("catalogue:value"))
			value = value.replace(/\n/g,"");
			if (value && selectList.find('option[value=' + value + ']').length==0){
				var option = new Option(getXPathValue(this, selectList.data("catalogue:caption")),value);
				var hasAllRequired= true;
				cc+=1;
				var last = $(option);//selectList.children("option:last"); 
				last.data('identifier',value);		
				last.data('spatial',getXPathValue(this, selectList.data("catalogue:spatial"),series));
				myDataset=this;
				var spatialT = selectList.triggerHandler('OnNewGeometry',[last.data('spatial'), myDataset, xml]);
				if (spatialT){last.data('spatial',spatialT);}
				last.data("metadata", new Array());
				$.each(selectList.data("metadataDef"), function(ii,vv) {
					// check if the metadata field is required 
					// and if it should inherit the value of the series 	
					var xmlPath = vv[0];
					//removed the required attribute var isRequired = vv[5];
					var getItFromSeries = vv[4];
					if (getItFromSeries=="false"){
						last.data("metadata")[ last.data("metadata").length ] = getXPathValue(myDataset, xmlPath, null)
					}
					else{
						last.data("metadata")[ last.data("metadata").length ] = getXPathValue(myDataset, xmlPath, series)
					};
					/* removed the required attribute 
					if (isRequired){
						// to-do check if parent is defined for the case of cross-relations
						if (last.data("metadata")[ last.data("metadata").length -1] == "") hasAllRequired=false;
					}
					*/
				})
				if (hasAllRequired){
					try {
						selectList[0].add(option,null); // standards compliant; doesn't work in IE
					}
					catch(ex) {
						selectList[0].add(option); // IE only
					}
					selectList.trigger('newfeature',[last]);
				}
			}
		})
		if (cc>0) selectList.trigger('change');
	return cc;	
}

function SubmitOperation(el, link, method, context){

		if ($(el).triggerHandler('OnBeforeSubmit')==false) return false;

		if ($(el.form).find(".ifyWrongPassword").length!=0) {
			alert("Please correct password fields or leave them empty");
			return false;
		}
		var values = new Array();
		if (method=='POST') values = $(el.form).serializeArray();
		
		/*$(el.form).find(".ifyPassword").each(function() {
			if ($(this).data("set")) values[values.length] = { name : this.name + ':set', value : 'true' };
		});*/
		
		values[values.length]= {name : '_format', value : 'xml'};

		//alertObj(myStatus);
		displayStatusWait(el,"submitting operation ...");
		// Not needed anymore since method is provided by the Ify.dll
		//var myUrl = link + unescape("%26") + $(el.form).find("input.ifyHiddenIdElement").attr("name") + "=" + $(el.form).find("input.ifyHiddenIdElement").val();
		//alert("debug "  + myUrl);
		$.ajax({
			type: "POST",
			url: link, 
			dataType:"xml",
			data: values,
			async:true,
			error: function (XMLHttpRequest, textStatus, errorThrown) {
				removeStatusWait(el);
				$('#'+this.element+'_status').removeClass('processing');
				if ($(el).triggerHandler('OnError:Post',XMLHttpRequest, textStatus, errorThrown)==false) return false;
				displayMessage("error",'SYSERRP3: Unable to post values due to network or system error ('+textStatus+'). Please try again later');
//				if(IfyIsUserAdmin)  window.open().document.write (XMLHttpRequest.responseText);
			},
			success: function (data, textStatus) {
				removeStatusWait(el);
				var msg = '';
				$(data).find("message").each(function(){
					displayMessage($(this).attr("type"),$(this).text());
					msg+=$(this).text();
				});
				//if ($(el).triggerHandler('OnSuccess',data)==false) return false;
				$(el.form).find("div").removeClass('input-invalid');
				if ($(data).find("message").attr("type") == "error") { 
					$(data).find("item *").each(function (){
						if($(this).attr("valid")=="false"){
							var tmp = $(el.form).find("#" + this.localName);
							if (tmp.length){
								tmp.parent().addClass('input-invalid');
								tmp.bind("change", function(){
									$(this).parent().removeClass('input-invalid');	
								})			
							}
						}
					})
					//isFormValid($(el.form));
					return; 
				}
				$(el.form).find(".ifyPassword").each(function() { $(this).data("set", null); });
				
				//if (el.value == "Modify") return;
				if (context=='singleItem' && $(data).find('singleItem').attr('link')) {
					window.location.href = $(data).find('singleItem').attr('link');
				}
				else{
					if ($("#ifySelectPagingControlNav").length) {
						$(el.form).find("input.ifyHiddenIdElement").val('');	
						ifySelectPagingControlExecute($("#ifySelectPagingControlNav	> span.ifySelectPagingControlItemSelected").html());
					}
					else window.location.reload();
				}
			}
		});





}

function isFormValid(form){
	var rc=true;
	$(form).find('input').each(function(){
		if(!$(this).hasClass('ifyOptional')){
			if($(this).val()==""){
				$(this).addClass('inputInvalid');
				rc=false;
			}
			else{
				$(this).removeClass('inputInvalid');
			}
		}
	})
	return rc;
}

function displayMessage(type, message){
	alert(type+":"+message);
}


function getXPathValue(el, path, elPar){
	var vv = new Array;	
	try{
	var xItems = el.selectNodes( path); 
	for( var i = 0; i < xItems.length; i++ ){ 
		
		if (xItems[i].nodeValue){
			vv[i]= xItems[i].nodeValue;
		}else{
		// if (xItems[i].childNodes.length>1){
		//		alert("XPATH_NODE_1: " + xItems[i].childNodes.length);
		//		alert("XPATH_CONTENT_1: " + xItems[i].textContent);
		//	}
			if (xItems[i].childNodes.length) vv[i]= xItems[i].firstChild.nodeValue;
		}

	} 	
	if (vv.length==0 && elPar){		
		return getXPathValue(elPar,path);
	}
	return vv.join(" ");
	}
	catch(err){
		return ""
	}
}

function createXMLFromString(xmlstr){
	if (window.DOMParser){
		parser=new DOMParser();
		xmlDoc=parser.parseFromString(xmlstr,"text/xml");
	}
	else // Internet Explorer
	{
		xmlDoc=new ActiveXObject("Microsoft.XMLDOM");
		xmlDoc.async="false";
		xmlDoc.loadXML(xmlstr);
	} 
	return xmlDoc;
}

function operationClick(inputButton, operationLink, operationMethod, elementType)
{
	if (operationMethod=='GET'){
		window.location.href=operationLink;
	}
	if (operationMethod=='POST'){
		// TODO to be changed to be in site specifics
		if (operationLink.indexOf("/jobs/") >= 0 && operationLink.indexOf("/parameters") >= 0){
			SubmitOperation (inputButton, operationLink, operationMethod, "parameters");
		}else{
			SubmitOperation (inputButton, operationLink, operationMethod, elementType);
		}
	}
	
	
}





// mozXPath [http://km0ti0n.blunted.co.uk/mozxpath/] km0ti0n@gmail.com
// Code licensed under Creative Commons Attribution-ShareAlike License 
// http://creativecommons.org/licenses/by-sa/2.5/
if( document.implementation.hasFeature("XPath", "3.0") ){

	if( typeof XMLDocument == "undefined" ){ XMLDocument = Document; }
	XMLDocument.prototype.selectNodes = function(cXPathString, xNode){
	if( !xNode ) { xNode = this; } 
		var oNSResolver = this.createNSResolver(this.documentElement)
		var aItems = this.evaluate(cXPathString, xNode, oNSResolver, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null)
		var aResult = [];
		for( var i = 0; i < aItems.snapshotLength; i++){
			aResult[i] =  aItems.snapshotItem(i);	
		}		
		return aResult;

	}

	XMLDocument.prototype.selectSingleNode = function(cXPathString, xNode){
		if( !xNode ) { xNode = this; } 
		var xItems = this.selectNodes(cXPathString, xNode);
		if( xItems.length > 0 ){return xItems[0];	}
		else{return null;	}
	}

	Element.prototype.selectNodes = function(cXPathString){
		if(this.ownerDocument.selectNodes){	return this.ownerDocument.selectNodes(cXPathString, this);}
		else{throw "For XML Elements Only";}
	}
	Element.prototype.selectSingleNode = function(cXPathString){	
		if(this.ownerDocument.selectSingleNode){return this.ownerDocument.selectSingleNode(cXPathString, this);	}
		else{throw "For XML Elements Only";}
	}
}

	$.extend($.fn.disableTextSelect = function() {
		return this.each(function(){
			if($.browser.mozilla){//Firefox
				$(this).css('MozUserSelect','none');
			}else if($.browser.msie){//IE
				$(this).bind('selectstart',function(){return false;});
			}else{//Opera, etc.
				$(this).mousedown(function(){return false;});
			}
		});
	});


