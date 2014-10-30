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

		$(".ifySelectPagingControlCheckbox").find("input:checked").each(function(){
			$(this).attr("checked","");
		});
		$("#ifySelectPagingControlIds").val('');
		$("#ifySelectPagingControlIds").data('selected',new Array);
})



	function ifySelectPagingControlExecute(page, sort, caption){
				$("#ifySelectPagingControlLabel").addClass("waiting");
				var origQuery=$("#ifySearchBox #ifySearchOriginalRequest").val();
				var hasSort = origQuery.indexOf("##SORT##")>-1;
				//alert(origQuery);
				//return true;
				if (page)	{
					$("#ifySelectPagingControlLabel").html("Retrieving page " + page);
					origQuery= origQuery.replace("##PAGE##",page);
				}
				if (caption){
					if ($("#ifySelectPagingControlSortField").val()==sort){$("#ifySelectPagingControlSortField").val("-" + sort);caption += " descending";}
					else {$("#ifySelectPagingControlSortField").val(sort)};
					$("#ifySelectPagingControlLabel").html("Sorting by " + caption);
					origQuery= origQuery.replace("##SORT##",$("#ifySelectPagingControlSortField").val());
				}
				else{
					origQuery= origQuery.replace("##SORT##",$("#ifySelectPagingControlSortField").val());
				}
				origQuery= origQuery.replace("##SORT##",'');
				origQuery= origQuery.replace("##PAGE##",'');
				//alert(origQuery);
				/*
				values[values.length]= {name : 'page', value:$(el).html()};
				return true;
				*/
				$.ajax({
					type: "GET",
					url: "?" + origQuery,
					dataType:"html",
					//data: values,
					async:true,
					error : function (XMLHttpRequest, textStatus, errorThrown) {
						$("#ifySelectPagingControlLabel").removeClass("waiting");
						$('#'+this.element+'_status').removeClass('waiting');
						alert('Unable to post values due to network or system error ('+errorThrown+'). Please try again later');
					},
					success: function (data, textStatus) {
						$("#ifySelectPagingControlLabel").removeClass("waiting");
						if (!$(data).find("#elements").html()){
								$("#ifySearchPagingBox").html($(data).find("div.ifyMessage").html());
								$("#elements").html('');
								$("#login_form").submit(function(){
										return login_form_bind (
												function(){ifySelectPagingControlExecute(page, sort, caption)}
										)
									}
								);
						}
						else{
							$("#divListTabPagingControlLinks").html($(data).find("#divListTabPagingControlLinks").html());
							$("#ifySearchPagingBox").html($(data).find("#ifySearchPagingBox").html());
							$("#elements").html($(data).find("#elements").html());
							if (hasSort){ifySelectPagingControlBindSort()};
							ifySelectPagingControlUpdateSelected();
						}
					}
				})
	}

	function ifySelectPagingControlShowSelected(el){
		var hh = $("#elements tr:first-child");
		$("#elements").html('');
		$("#elements").append(hh);	
		$.each($("#ifySelectPagingControlIds").val().split(","), function() {
				$("#elements").append($("#ifySelectPagingControlIds").data(this + ""));
			});
		//$("#ifySelectPagingControlSelectLabel").html('(<span style="cursor: pointer;" onclick="ifySelectPagingControlExecute(1)" title="Show All">Show All</span>)');
		$("#ifySelectPagingControlNav").html(' <span class="ifyTextButton" onclick="ifySelectPagingControlExecute(1)" title="Show All">Show all the records found</span> ');
		makeTableSortable($("#elements"));
	}

	function ifySelectPagingControlUpdateSelected(){
		
		if ($("#ifySelectPagingControlIds").length!=0){
			if ($('#ifySelectPagingControl #ifySelectPagingControlIds').val()==""){
				$("#ifySelectPagingControlSelectLabel").html('');
			}
			else{
				$("#ifySelectPagingControlSelectLabel").html('(<span class="ifyTextButton" onclick="ifySelectPagingControlShowSelected(this)" title="Show Selected">' + $('#ifySelectPagingControl #ifySelectPagingControlIds').val().split(',').length + ' selected</span>)');
				$("#ifySelectPagingControlSelectLabel").show();
			}
			$.each($("#ifySelectPagingControlIds").val().split(","), function() {
					$("#c" +this).attr("checked","checked");
			});
		}
	}


	function ifySelectPagingControlBindSort(){

		$("#elements th").click(function(){ifySelectPagingControlExecute(null,this.id.replace('ifyHeader_',''),$(this).html());})
		var sortValue = $("#ifySelectPagingControlSortField").val();
		if (sortValue!=''){ 					
				if (sortValue.indexOf("-")==0){{$("#ifyHeader_" + sortValue.substr(1)).addClass("sortAsc")}}
				else{$("#ifyHeader_" + sortValue).addClass("sortDesc")}
		}	
	}

	function ifySelectPagingControlSelectElement(value){
			if ($('#ifySelectPagingControl #ifySelectPagingControlIds').val()==''){
				$("#ifySelectPagingControlIds").data(value,$("#ifyRow_"+value));
				$('#ifySelectPagingControl #ifySelectPagingControlIds').val(value);
			}
			else{
				var arr=$('#ifySelectPagingControl #ifySelectPagingControlIds').val().split(",");
				var removeItem=value;
				if (jQuery.inArray(value,arr)!=-1){
					arr = jQuery.grep(arr, function(value){return value != removeItem;});
					$("#ifySelectPagingControlIds").removeData(value);
				}
				else{
					arr[arr.length]=value;
					$("#ifySelectPagingControlIds").data(value,$("#ifyRow_"+value));	
				}
				$('#ifySelectPagingControl #ifySelectPagingControlIds').val(arr.join(','));
			}
			//var page=$(".ifySelectPagingControlItemSelected").html();
			ifySelectPagingControlUpdateSelected();
		}



function makeTableSortable(el){
				var $table = el;
				$('th', $table).each(function(column) {
					$(this).removeClass("sortAsc");
					$(this).removeClass("sortDesc");
					if (true){
					//if ($(this).is('.sort-alpha')) {
						$(this).click(function (){
							//$(this).removeClass("sortDesc");
							//$(this).addClass("sortAsc");
							var rows = $table.find('tbody > tr:gt(0)').get();
							if ($(this).hasClass("sortDesc")){
								$table.find('th').each(function(){
									$(this).removeClass("sortAsc");
									$(this).removeClass("sortDesc");
								})
								
								$(this).addClass("sortAsc");								
								rows.sort(function(a, b) {
										var keyA = $(a).children('td').eq(column).text().toUpperCase();
										var keyB = $(b).children('td').eq(column).text().toUpperCase();
										if (keyA < keyB) return 1;
										if (keyA > keyB) return -1;
										return 0;
								})
							}
							else{
								$table.find('th').each(function(){
									$(this).removeClass("sortAsc");
									$(this).removeClass("sortDesc");
								})
								$(this).addClass("sortDesc");
								rows.sort(function(a, b) {
										var keyA = $(a).children('td').eq(column).text().toUpperCase();
										var keyB = $(b).children('td').eq(column).text().toUpperCase();
										if (keyA < keyB) return -1;
										if (keyA > keyB) return 1;
										return 0;
								})
							}
							$.each(rows, function(index, row) {								
								$table.children('tbody').append(row)
							})

						});
						/*
						$(this).addClass('clickable').hover(function() {
							$(this).addClass('hover')
							}, function() {
							  $(this).removeClass('hover')
							}).click(function() {
									var rows = $table.find('tbody > tr').get();
									rows.sort(function(a, b) {
										var keyA = $(a).children('td').eq(column).text().toUpperCase();
										var keyB = $(b).children('td').eq(column).text().toUpperCase();
										if (keyA < keyB) return -1;
										if (keyA > keyB) return 1;
										return 0;
									})
									$.each(rows, function(index, row) {
										$table.children('tbody').append(row)
									})
						})
						*/
					}
				})
}


/*


		$(document).ready(function() {
			$('table.sortable').each(function() {
				var $table = $(this)
				$('th', $table).each(function(column) {
					if ($(this).is('.sort-alpha')) {
						$(this).addClass('clickable').hover(function() {
							$(this).addClass('hover')
							}, function() {
							  $(this).removeClass('hover')
							}).click(function() {
									var rows = $table.find('tbody > tr').get()
									rows.sort(function(a, b) {
										var keyA = $(a).children('td').eq(column).text().toUpperCase();
										var keyB = $(b).children('td').eq(column).text().toUpperCase();
										if (keyA < keyB) return -1;
										if (keyA > keyB) return 1;
										return 0;
									})
								$.each(rows, function(index, row) {
									$table.children('tbody').append(row)
								})
						})
					}
				})
			})
		 })


*/