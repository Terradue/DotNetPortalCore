/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/

$(document).ready(function() {
//		$(".extendedSearch").toggle();
	// to-do clear the page control when loading the page 
		//ifySelectPagingControlUpdateSelected();		
/*
		$(".ifySelectPagingControlCheckbox").find("input:checked").each(function(){
			$(this).attr("checked","");
		});
		$("#ifySelectPagingControlIds").val('');
		$("#ifySelectPagingControlIds").data('selected',new Array);
*/
		$(".ifyDateRangeListFromTo").hide();
		$(".ifyDateRangeList").change(function() {
			if ($(this).val()==''){
				$(this).parent().find("input:first").val(''); 
				$(this).parent().find(".ifyDateRangeListFromTo").hide();
				return true;
			}

			if ($(this).val()<0){
				var d1 = new Date();
				var d2 = calculateDateRangeEnd(d1, $(this).val());
				$(this).parent().find(".ifyDateRangeListFromTo").hide();
				$(this).parent().find("input:first").val(getIsoDate(d2)+'/'+getIsoDate(d1));
				$(this).parent().find(".ifyDateRangeListFromTo").hide();
			}
			if ($(this).val()==0){
				$(this).parent().find(".ifyDateRangeListFromTo").show();
				$(this).parent().find(".ifyDateRangeListFrom").val($(this).parent().find("input:first").val().split('/')[0]);
				$(this).parent().find(".ifyDateRangeListTo").val($(this).parent().find("input:first").val().split('/')[1]);
				$(this).parent().find(".ifyDateRangeListFrom").simpleDatepicker({startdate: "1999-01-01", x:0 , y:20 });
				$(this).parent().find(".ifyDateRangeListTo").simpleDatepicker({startdate: "1999-01-01", x:0 , y:20 });
			}
			
		})
		$(".ifyDateRangeListFrom").change(function() {
			if (!isDate($(this).val())){ $(this).val('');$(this).get(0).focus(); return false}
			ifyDateRange = $(this).parent().parent().find("input:first");
			if (ifyDateRange.val()==''){ifyDateRange.val($(this).val())}
			else{
				var cDate = ifyDateRange.val().split('/');
				cDate[0]=$(this).val();
				ifyDateRange.val(cDate.join('/'));
			}
			ifyDateRange.change();
		})

		$(".ifyDateRangeListTo").change(function() {
			if (!isDate($(this).val())){ $(this).get(0).focus(); return false}
			ifyDateRange = $(this).parent().parent().find("input:first");
			if (ifyDateRange.val()==''){ifyDateRange.val( $(this).val() + '/' + $(this).val())}
			else{
				var cDate = ifyDateRange.val().split('/');
				cDate[1]=$(this).val();
				ifyDateRange.val(cDate.join('/'));
			}
			ifyDateRange.change();
		})

		
		$(".ifyDateRange").change(function(){
			if ($(this).val().split('/').length==2){
					
					var dRange = $(this).val().split('/');
					if ( !( isDate(dRange[0]) && isDate(dRange[1]))){alert(1);return false};
					if (dRange[1] != getIsoDate(new Date())){
						$(this).parent().find(".ifyDateRangeList").children("option[value='0']").attr('selected','selected');
						$(this).parent().find(".ifyDateRangeList").change()
						return false;
					};
					$(this).parent().find(".ifyDateRangeList").children("option[value='0']").attr('selected','selected');
					$(this).parent().find(".ifyDateRangeList").children("option").each(function() {
						if ($(this).val()<0){
							if ( getIsoDate(calculateDateRangeEnd(getDate(dRange[1]), $(this).val())) == dRange[0]){
								$(this).attr('selected','selected');						
							}
						}
					})									
					if ($(this).parent().find(".ifyDateRangeList").val()=='0'){ $(this).parent().find(".ifyDateRangeList").change()}

			}
			//todo check if date is correct
			//alert($(this).val());
			//if (!isDate($(this).val())){ alert('not date');$(this).get(0).focus() }
		})
		
		$(".ifyDateRange").change();
		//$(".ifyDateRangeList").children("option[value='0']").attr('selected','selected');
})

	
	function isDate (value){
		if (!value){ return false}
		if (value.split('-').length!=3){return false}
		return (!isNaN (new Date (value.split('-')[0], value.split('-')[1] - 1, value.split('-')[2]).getYear () ) ) ;
	}

	function calculateDateRangeEnd(d1, val){return new Date(Date.parse(d1)+ val *1000*60*60*24);}

	function getIsoDate(dateObj){
		var rr=dateObj.getFullYear() + "-";
		if (dateObj.getMonth()<9) {rr+="0"}
		rr+=(dateObj.getMonth()+1) + "-";
		if (dateObj.getDate()<10) {rr+="0"}
		rr+=dateObj.getDate();
		return  rr;
	}
	function getDate(isoDate){
		if (isDate(isoDate)){ return new Date (isoDate.split('-')[0], isoDate.split('-')[1] - 1, isoDate.split('-')[2])}
		else {return 0}
	}
