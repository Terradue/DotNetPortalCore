/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 	info@terradue.com
*/

// javascript code for the tasks page 


function getmyxml(){
	return '';//'<?xml version="1.0" encoding="utf-8"?><rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns:sru="http://a9.com/-/opensearch/extensions/sru/2.0/" xmlns:jers="http://www.eorc.jaxa.jp/JERS-1/en/" xmlns:os="http://a9.com/-/spec/opensearch/1.1/" xmlns:ws="http://dclite4g.xmlns.com/ws.rdf#" xmlns:fp="http://downlode.org/Code/RDF/file-properties/" xmlns:owl="http://www.w3.org/2002/07/owl#" xmlns:envisat="http://www.example.com/schemas/envisat.rdf#" xmlns:atom="http://www.w3.org/2005/Atom" xmlns:ical="http://www.w3.org/2002/12/cal/ical#" xmlns:dclite4g="http://xmlns.com/2008/dclite4g#" xmlns:dct="http://purl.org/dc/terms/" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:sar="http://earth.esa.int/sar" xmlns:geo="http://a9.com/-/opensearch/extensions/geo/1.0/" xmlns:time="http://a9.com/-/opensearch/extensions/time/1.0/" xmlns:eop="http://www.genesi-dr.eu/spec/opensearch/extensions/eop/1.0/"><dclite4g:Series rdf:about="unknownResource"><dc:description rdf:resource="unknownDescription" /></dclite4g:Series><dclite4g:DataSet rdf:about="ifynet.terradue.com/22371d4a-bca9-484d-93d6-4c0af912889b"><dc:identifier>mosaicCom</dc:identifier><dc:title>mosaicCom</dc:title><dc:subject>mosaicCom</dc:subject><dclite4g:onlineResource><ws:HTTP rdf:about="http://ifynet.terradue.com/tasks/metalink/?uid=22371d4a-bca9-484d-93d6-4c0af912889b" /></dclite4g:onlineResource><eop:processorVersion>mosaicCom/1.0</eop:processorVersion><eop:orbitNumber /><eop:acquisitionStation /><eop:processingCenter /><eop:processorVersion /><eop:processingDate /><ical:dtstart>2004-09-01T00:00:00</ical:dtstart><ical:dtend>2004-09-15T00:00:00</ical:dtend><dct:spatial>POLYGON((-25 30,45 30,45 70,-25 70,-25 30))</dct:spatial><dct:created>0001-01-01T00:00:00</dct:created><dct:modified>0001-01-01T00:00:00</dct:modified><dct:audience>Public</dct:audience><dc:source rdf:resource="http://ifynet.terradue.com/tasks/?uid=22371d4a-bca9-484d-93d6-4c0af912889b" /><dc:publisher rdf:resource="http://ifynet.terradue.com//services/mosaiccom/" /><dc:creator rdf:resource="http://ifynet.terradue.com/admin/user.asp?id=83" /><dc:title>New Task</dc:title><dc:abstract /></dclite4g:DataSet></rdf:RDF>';
}			
	
$(document).ready(function() {

	$("div.jobParameter textarea, div.jobParameter select").val('');



	$(".ifyShadetabs li").click(function(ev){
			var parent= $(this).parent().parent();
				parent.find(".jobDetailsContent").addClass("waiting");
				parent.find("li").removeClass("ifyShadeTabSelected");
				$(this).addClass("ifyShadeTabSelected");
				parent.find(".jobDetailsContent").html('');
	})

	$(".ifyShadetabs li.jobDetails").click(function(ev){
		var parent= $(this).parent().parent();
		var link = parent.parent().parent().find("input").val();
		GetContent(parent, link, "div.jobInfo div.jobDetailsContent", 
					function (html){
						parent.find(".jobDetailsContent").html(html);
					});
		
	})
	$(".ifyShadetabs li.jobParameters").click(function(ev){
		var parent= $(this).parent().parent();
		var link = $(this).find("input").val();
		GetContent( parent, link, 
				"div.jobInfo div.jobParameter", 
					function (html){
						parent.find(".jobDetailsContent").html(html);
						parent.find("select.jobParameter").change(function(){
							var newVal = $(this).parent().find("input#" + $(this).val()).val();
							$(this).parent().find("textarea").val(newVal);
						})
						parent.find("textarea#param_value").change(function(){
							var changed_value=$(this).val();
							var changed_id=$(this).parent().find("select.jobParameter").val();
							$(this).parent().find("input#" + changed_id).val(changed_value);
						})
					});
	})

	$(".ifyShadetabs li.jobInput").click(function(ev){
		var parent= $(this).parent().parent();
		var inProd = parent.find(".jobDetailsContent .inputProduct");
		if (inProd.length>0){
			parent.find(".jobDetailsContent").html(inProd.html());
		}
		else{
			var link = $(this).find("input").val();		
			GetContentVal(parent, link, ".jobDetailsContent input#inputProduct", function (html){
				var outHtml="";
				$(html.split("|")).each(function (i,val){
					var arr = val.split("/");
					outHtml+= "<li><a target='_new' href='/proxy4.aspx?url="+ val + "'>" +  arr[arr.length-2] +"</a></li>";
				});
				parent.find(".jobDetailsContent").html("<ul>"+outHtml+"</ul>");//html.replace(/\|/g,'<br/>'));
			});
		}		
	})
	
	$(".ifyShadetabs li.jobProcessingInfo").click(function(ev){
		var parent= $(this).parent().parent();
		var link = $(this).find("input").val();
		GetContent(parent, link, "div.jobInfo div.jobDetailsContent", 
					function (html){
						parent.find(".jobDetailsContent").html(html);
					});
		
	})
	
	updateTaskProgress();
	
	$("#displayJobs").click(function(){
		$(".jobsInfo").toggle();
		$(this).hide();
	});
	
	$(".dataset").bind('newfeature',function(event, option){
		var OnlineResourceIndex = getMetadataIndexByName($("#dataset"),"onlineresource");
		var outValue="";
		var meta = $(option).data("metadata");
		$(meta[OnlineResourceIndex].split(" ")).each(function (i,val){
			outValue += "<a target='_new' href='"+val+"'>"+val.replace("/tasks/download?url=","")+"</a><br/>"
                });
		meta[OnlineResourceIndex] = outValue;
		$(option).data("metadata", meta);
		
		var qlindex = getMetadataIndexByName($("#dataset"),"quicklook"); 
		var ql = meta[qlindex];
		if (ql != ""){
			outValue="";
			$(ql.split(" ")).each(function (i,val){
				outValue += "<a target='_new' href='"+val+"'><img src='"+val.replace("/tasks/download?url=","")+"' width='200' /></a><br/>"
				// protects this in the case we don't have a map 
				try{
					if ($(option).data("spatial") != "") OSMap.addLayer(new OpenLayers.Layer.Image(
						"quicklook",ql,
						 OpenLayers.Geometry.fromWKT($(option).data("spatial")).getBounds(),
						 new OpenLayers.Size(800, 400),
						{numZoomLevels: 18, 
						    resolutions:OSMap.layers[0].resolutions,
						    maxResolution:OSMap.layers[0].resolutions[0],
						    isBaseLayer: false}));
					OSMap.updateSize();
				}
				catch(err){  }
			});
			meta[qlindex] = outValue;
			$(option).data("metadata", meta);
		}
				
	})
	
	
})

	
function updateTaskProgress(){
	var pc_total=0;
	var nrjobs=0;
	$('.progressBarJob').each(function(){
		nrjobs++;
		if ($(this).data('pc') > 0) pc_total+=$(this).data('pc');
	});
	$("span.progressBarTask").progressBar((pc_total/nrjobs),{
		boxImage		: '/js/img/progressbar.gif',
		barImage		: {
			0:  '/js/img/progressbg_red.gif',
			30: '/js/img/progressbg_orange.gif',
			70: '/js/img/progressbg_green.gif'
			}
	});
}

function AssignJs(){

	$("select.jobParameter").change(function(){
		var newVal = $(this).parent().find("div." + $(this).val()).html();
		$(this).parent().find("textarea").val(newVal);
	})
}

function GetContent(parent, url, el, callback){
		$.ajax({
					type: "GET", url: url,
					dataType:"html", async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
						parent.find(".jobDetailsContent").removeClass("waiting");
					},
					success: function (data) {
						var html = $(data).find(el).html();
						callback(html);
						parent.find(".jobDetailsContent").removeClass("waiting");
					}
		})
}

function GetContentVal(parent, url, el, callback){
	$.ajax({
				type: "GET", url: url,
				dataType:"html", async:true,
				error : function (XMLHttpRequest, textStatus, errorThrown) {
					parent.find(".jobDetailsContent").removeClass("waiting");
				},
				success: function (data) {
					var val = $(data).find(el).val();
					callback(val);
					parent.find(".jobDetailsContent").removeClass("waiting");
				}
	})
}

function operationClick(inputButton, operationLink, operationMethod, elementType)
{


   if (operationMethod=='GET'){
      if (operationLink.indexOf("jobs/details/") >= 0){
         $.ajax({
            type: "GET",
            url: operationLink,
            context: inputButton,
            async:true,
            error : function (XMLHttpRequest, textStatus, errorThrown) {
               displayMessage("error","Error! : " + $(XMLHttpRequest.responseXML).find("message").text());
            },
            success: function (data) {
               displayMessage("info","Job information changed");
               window.location.reload();
            }
         })
      }else{
         window.location.href=operationLink;
      }
   }
   if (operationMethod=='POST'){
      if (operationLink.indexOf("/jobs/") >= 0 && operationLink.indexOf("/parameters") >= 0){

         $.ajax({
            type: "POST",
            url: operationLink,
            data: $(inputButton).parents('form:first').serialize(),
            async:true,
            error : function (XMLHttpRequest, textStatus, errorThrown) {
               displayMessage("error","Error! : " + $(XMLHttpRequest.responseXML).find("message").text());
            },
            success: function (data) {
               displayMessage("info","Job parameters changed");
            }
         })
      }else{
        SubmitOperation (inputButton, operationLink, operationMethod, elementType);
      }
   }
}


