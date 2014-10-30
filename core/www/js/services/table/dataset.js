/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/

// This code deals with the list of products widget when we have table element on the service page
// It assumes that there is some html element of the class .dataset to link the results
// It the select element and includes a table on it's place 


	var gaiSelected =  [];

	$(document).ready(function() {
			$('.table_dataset').each(function(){
			var aoColumns = [];
				$(this).find('th').each( function () {
					if ($(this).hasClass( 'no_sort' )) {
						aoColumns.push( { "bSortable": false } );
					} else {
						aoColumns.push( null );
					}
				} );
				var myTable = new $(this).dataTable( {
					"aoColumns": aoColumns,
					"sDom": '<"top"><"wrap"t><"bottom"irflp<"clear">',
					"iDisplayLength": 10,
					"bPaginate": false,
					"bAutoWidth": true
				});
				//$(this).parent().parent().parent().find(".dataset").data("filesTable",myTable);
				if ( $(this).parent().parent().parent().find(".dataset").length ) {
					$(this).parent().parent().parent().find(".dataset").data("filesTable",myTable);
				}else{
					$(this).parent().parent().parent().parent().find(".dataset").data("filesTable",myTable) ;
				} ;
				myTable.fnSetColumnVis( 0, false );
				var oSettings = myTable.fnSettings();
				$(oSettings.aoColumns[1].nTh).html('');
				$(oSettings.aoColumns[1].nTh).width('10px');
			})
			
			
			$('._dataset_table .dataset').bind('deletefeature', function(event,els) {
				myTable = $(this).data("filesTable");
				var anodes = myTable.fnGetNodes();
					$(anodes).each(function (){
						node=this;
						$(els).each (function(){
							if (myTable.fnGetData(node)[0]==$(this).val()){
								myTable.fnDeleteRow( node );
							}
						})
					});
				// todo : check if the deleted feature is not in some service parameter (e.g. master)
			})


			$('._dataset_table .dataset').bind('newfeature', function(event, newOption) {
				// todo check this !!1
				if (! $(this).find('.dataTables_length').hasClass('dataTables_show_elements')){$(this).find('.dataTables_length').addClass('dataTables_show_elements')};
				if (! $(this).find('.dataTables_filter').hasClass('dataTables_show_elements')){$(this).find('.dataTables_filter').addClass('dataTables_show_elements')};

				var myTable = $(this).data("filesTable");
				
				var arr = new Array;
				$($(this).data("metadataDef")).each(function (i){
					if (this[3]!='true'){
						arr[arr.length]=newOption.data('metadata')[i];
					}
				});				
				
				//for (i=0; i<arr.length;i++ ){
					//arr[i]=arr[i].replace('.000000Z','Z');
					//arr[i]=arr[i].replace('.000Z','Z');
				//}
				arr.splice(0,0,"<div class='ifyIconItemInfo'/>");
				arr.splice(0,0,newOption.data('identifier'));				
				rr = myTable.fnAddData(arr);
				var node = myTable.fnGetNodes(rr);
				$(node).disableTextSelect();
				// this defines the default datset details in the moreinfo element is true in the service's xml
				if ($('.table_dataset').hasClass('ifyMoreInfo')){
					node.Table = myTable;
					node.OnOpen = function (oTable, nTr){}
					node.OnClose = function (oTable, nTr){}
					node.DataSetDetails = function (oTable, nTr, option){
						return _fnDataSetDetails ( oTable, nTr, option )
					}
				}				
				$(this).parent().find('.table_dataset').trigger('newfeature', [$(this), myTable, node, newOption]);
			})
			
			//$('.dataset').unbind('selectfeature');
			$('._dataset_table .dataset').bind('selectfeature', function(event, identifier) {
					myTable = $(this).data("filesTable");
					var aTrs = fnGetUnSelected(myTable);	
					$(aTrs).each(function (){
						 if (myTable.fnGetData(this)[0]==identifier){
							$(this).addClass('row_selected');	 
							return false;
						 }
					});
			})
			$('._dataset_table .dataset').bind('selectall', function(event, params) {	
					//var aTrs = fnGetDisplayNodes(filesTable);
					myTable = $(this).data("filesTable");
					var aTrs = myTable.fnGetDisplayNodes();
					$(aTrs).each(function (){						 
						$(this).addClass('row_selected');	
					});
			})
			
			$('._dataset_table .dataset').bind('unselectall', function(event, params) {	
					//var aTrs = fnGetDisplayNodes(filesTable);
					myTable = $(this).data("filesTable");
					var aTrs = myTable.fnGetDisplayNodes();
					$(aTrs).each(function (){						 
						$(this).removeClass('row_selected');	
					});
			})
			
			//$('.dataset').unbind('unselectfeature');
			$('._dataset_table .dataset').bind('unselectfeature', function(event, identifier) {
					myTable = $(this).data("filesTable");
					var aTrs = fnGetSelected(myTable);	
					$(aTrs).each(function (){
						 if (myTable.fnGetData(this)[0]==identifier){
							$(this).removeClass('row_selected');	 
							return false;
						 }
					});
			})
			
			$('.table_dataset').bind('newfeature', function (event, dataset, table, row, newoption){
				//myTable = $(this).data("filesTable");
				$(row).click(function() {
						// todo check this 		
						if ( $(this).hasClass('row_selected') ){dataset.trigger('unselectfeature',table.fnGetData(this)[0] );}
						else{dataset.trigger('selectfeature',table.fnGetData(this)[0] );}
						event.stopPropagation();
						return false;
				} );	
				
				if ($(this).hasClass("ifyMoreInfo")){
					fnOpenClose(table, row, newoption);
				}
			});


	})



function fnGetSelected( oTableLocal ){
	var aReturn = new Array();
	var aTrs = oTableLocal.fnGetNodes();			
	for ( var i=0 ; i<aTrs.length ; i++ ){
		if ( $(aTrs[i]).hasClass('row_selected') ){
			aReturn.push( aTrs[i] );
		}
	}
	return aReturn;
}

function fnGetUnSelected( oTableLocal ){
	var aReturn = new Array();
	var aTrs = oTableLocal.fnGetNodes();			
	for ( var i=0 ; i<aTrs.length ; i++ ){
		if ( ! $(aTrs[i]).hasClass('row_selected') ){
			aReturn.push( aTrs[i] );
		}
	}
	return aReturn;
}

function fnOpenClose ( table, row, option ){

	$(row).find('td div.ifyIconItemInfo').addClass('ifyIcon-plus');
	$(row).find('td div.ifyIconItemInfo').click( function (event) {
			event.stopPropagation();
			var nTr = this.parentNode.parentNode;
			if ( $(this).hasClass('ifyIcon-minus') ){
				$(this).removeClass('ifyIcon-minus');
				row.OnClose(table, nTr);
				table.fnClose( nTr );
				
			}
			else{
				$(this).addClass('ifyIcon-minus');
				row.OnOpen(table, table.fnOpen( nTr, 
					row.DataSetDetails(table, nTr, option), 
					'details' ));
			}
	} );
}

function fnDataSetDetails ( oTable, nTr, option ){
	return _fnDataSetDetails ( oTable, nTr, option )
}

function _fnDataSetDetails ( oTable, nTr, option ){
		var aData = oTable.fnGetData( nTr );
		op = option; //$('.dataset').find('option[value=' + aData[0] + ']');
		var sOut = '<table class="DatasetDetails">';
		$(option.parent().data("metadataDef")).each(function(i){
		// hidden metadata element are shown here 
			if ( (this[3]=='true') && (this[1] !='') ){
				sOut += '<tr><td><label>' + this[1] + ':</label></td> <td>' + op.data("metadata")[i] + '</td></tr>';
			}
		});
		sOut += '</table>';
		return sOut;
}

/*
 * Function: fnGetDisplayNodes
 * Purpose:  Return an array with the TR nodes used for displaying the table
 * Returns:  array node: TR elements
 *           or
 *           node (if iRow specified)
 * Inputs:   object:oSettings - automatically added by DataTables
 *           int:iRow - optional - if present then the array returned will be the node for
 *             the row with the index 'iRow'
 */
$.fn.dataTableExt.oApi.fnGetDisplayNodes = function ( oSettings, iRow )
{
	var anRows = [];
	if ( oSettings.aiDisplay.length !== 0 )
	{
		if ( typeof iRow != 'undefined' )
		{
			return oSettings.aoData[ oSettings.aiDisplay[iRow] ].nTr;
		}
		else
		{
			for ( var j=oSettings._iDisplayStart ; j<oSettings._iDisplayEnd ; j++ )
			{
				var nRow = oSettings.aoData[ oSettings.aiDisplay[j] ].nTr;
				anRows.push( nRow );
			}
		}
	}
	return anRows;
};
