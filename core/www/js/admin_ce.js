/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/
$(document).ready(function() {

	$(".ifyEntities").each(function(){
		$(this).data("delete", new Array);
		$(this).data("update", new Array);
		$(this).data("name", new Array);
		$(this).data("available", new Array);
		RefreshEntityValues($(this));
	});

	$(".ifyEntitiesEnableButton").click(function() {
		var parent= $(this).parent();
		$(this).parent().find(".ifyEntitiesSelect").children("option").each(function(ii){
			if ($(this).attr("selected")){
				$(this).removeClass("ifyEntitiesDisabled");
				SetEntityEnable(parent,1);
			}
		})
		RefreshEntityValues(parent);
	})

	$(".ifyEntitiesDisableButton").click(function() {
		var parent= $(this).parent();
		$(this).parent().find(".ifyEntitiesSelect").children("option").each(function(ii){
			if ($(this).attr("selected")){
				$(this).addClass("ifyEntitiesDisabled");
				//SetEntityEnable(parent,0,ii);
			}
		})
		RefreshEntityValues(parent);
	})

	$(".ifyEntitiesAddButton").click(function() {
		var option = new Option($(this).parent().find(".ifyEntitiesAddName").val(), "-1");	
		try {
			$(this).parent().find(".ifyEntitiesSelect")[0].add(option,null);// standards compliant; doesn't work in IE
		}
		catch(ex) {
			$(this).parent().find(".ifyEntitiesSelect")[0].add(option);// IE only
		}
		//$(this).parent().find(".ifyEntitiesSelect")[0].add(option,null);
		RefreshEntityValues($(this).parent());
	})

	$(".ifyEntitiesDeleteButton").click(function() {
		var parent= $(this).parent();
		$(this).parent().find('.ifyEntitiesSelect').children("option").each(function(ii){
			if ($(this).attr("selected")){
				parent.data("delete")[parent.data("delete").length]=$(this).val();
				$(this).remove();
			}			
		})
		parent.find('.ifyEntitiesDelete').val(parent.data("delete").join(','));
		RefreshEntityValues(parent);
	})
})


function RefreshEntityValues(parent){
		me=parent;
		me.data("update").clear();
		me.data("name").clear();
		me.data("available").clear();
		me.find(".ifyEntitiesSelect").children("option[value!=-1]").each(function(ii){
			me.data("update")[me.data("update").length]=$(this).val();
			me.data("name")[me.data("name").length]=$(this).text();
			if ($(this).hasClass("ifyEntitiesDisabled")){me.data("available")[me.data("available").length]="false";}
			else{me.data("available")[me.data("available").length]="true";}
		});
		me.find(".ifyEntitiesSelect").children("option[value=-1]").each(function(ii){
			me.data("name")[me.data("name").length]=$(this).text();
			if ($(this).hasClass("ifyEntitiesDisabled")){me.data("available")[me.data("available").length]="false";}
			else{me.data("available")[me.data("available").length]="true";}	
		});
		me.find(".ifyEntitiesUpdate").val(me.data("update").join(","));
		me.find(".entity_path").val(me.data("name").join(","));
		me.find(".entity_available").val(me.data("available").join(","));
		//alert(me.find(".entity_name").val());
}

function SetEntityEnable(parent, value, index){
		var aa = parent.find(".entity_available").val().split(",");
		aa[index]=value;			
		parent.find(".entity_available").val(aa.join(","));				
}
