<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Name:          opensearch.xsl
 Context:       template/xsl
 Version:       0.1 
 Description:   Generic template to process a opensearch description file 
		(this is also used by the series.xsl)

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->

<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:str="http://exslt.org/strings"
	 xmlns:msxsl="urn:schemas-microsoft-com:xslt"  
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">

<xsl:template match="field" mode="head"/>
<xsl:template match="field" mode="widget"/>


<!-- the trigger to all components is the count -->
<xsl:template match="field[@type='count']" mode="head">
		<script src="/js/table/jquery.dataTables.min.js">{}</script>
		<script src="/js/services/classic/dataset.js">{}</script>
		<script src="/js/services/table/dataset.js">{}</script>
		<script src="/js/services/classic/service.js">{}</script>
		<link rel="stylesheet" type="text/css" href="/template/css/services/classic.css" />
		<link rel="stylesheet" type="text/css" href="/template/css/services/maxfilelist.css" />
		<link rel="stylesheet" type="text/css" href="/js/table/css/classic.css" />
</xsl:template>


<xsl:template match="field[@type='time:start']" mode="head">
		<script src="/js/cal.js">{}</script>
		<link rel="stylesheet" type="text/css" href="/template/css/cal.css" />
</xsl:template>


<xsl:template match="/field[@type='time:start']">
	<xsl:apply-imports/>
</xsl:template>

<xsl:template match="/field[@type='time:start' or @type='time:end']" mode="javascript">
			$("#<xsl:value-of select="@name"/>").simpleDatepicker({startdate: "1999-01-01", enddate: "2011-12-31", x:-70 , y:0 });
</xsl:template>

			
			
<xsl:template match="field[@type='geo:box']" mode="head">
		<script src="/js/OpenLayers.js">{}</script>
		<script src="/js/services/classic/map.js">{}</script>
		<script src="/js/geoselect.js">{}</script>	
		
</xsl:template>
<xsl:template match="f_ield[@type='geo:box']" mode="javascript">
			$('#map').html('');
			$('#paneldiv').html('');
</xsl:template>


<xsl:template match="field[@type='geo:box']" mode="widget">
		<div id='mapPanel' >
			<div id='paneldiv' class='olControlPanel'>_</div>
			<div id='map' class='ifyOSMap' >map ... This page needs javascript</div>
		</div>
		<xsl:apply-imports/>
</xsl:template>

<xsl:template match="field[@type='geo:uid']">
	<xsl:apply-imports/>
</xsl:template>

<xsl:template match="os:Url" mode="head"/>
<xsl:template match="os:Url"/>


<xsl:template match="os:Url[@type='application/rdf+xml']" mode="head">
	<xsl:variable name="template">
		<xsl:call-template name="str:split">
			<xsl:with-param name="string" select="substring-after(@template,'?')" />
			<xsl:with-param name="pattern" select="'&amp;'" />
		</xsl:call-template>
	</xsl:variable> 
	<xsl:apply-templates select="msxsl:node-set($template)" mode="head"/>
</xsl:template>

<xsl:template match="os:Url" mode="geouid">
	<xsl:variable name="type"><xsl:value-of select="@type"/></xsl:variable>
	<xsl:if test="$opensearch/types/item[@value=$type]/@icon!=''">
	<span class="geouidIcon">
		<img>
			<xsl:attribute name="src"><xsl:value-of select="$opensearch/types/item[@value=$type]/@icon"/></xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="$opensearch/types/item[@value=$type]/@name"/></xsl:attribute>
		</img>
	</span>
	</xsl:if>
</xsl:template>

<xsl:template match="os:Url" mode="javascript">

	<xsl:variable name="type"><xsl:value-of select="@type"/></xsl:variable>
	<xsl:if test="$opensearch/types/item[@value=$type]/@icon!=''">
			$("#<xsl:value-of select="$opensearch/types/item[@value=$type]/@name"/>").data("template","<xsl:value-of select="@template" disable-output-escaping="yes"/>");
	</xsl:if>
</xsl:template>

<xsl:template match="os:Url[@type='application/rdf+xml']" mode="queryinput">
	<xsl:variable name="template">
		<xsl:call-template name="str:split">
			<xsl:with-param name="string" select="substring-after(@template,'?')" />
			<xsl:with-param name="pattern" select="'&amp;'" />
		</xsl:call-template>
	</xsl:variable> 
	<div id='query'>
		<xsl:apply-templates select="msxsl:node-set($template)/field[@group='main_queriables']"/>
	</div>
	<div id='aux_query'>
		<xsl:apply-templates select="msxsl:node-set($template)/field[@group='aux_queriables']"/>
	</div>
	<div id='unknown_queriables'>
		<xsl:apply-templates select="msxsl:node-set($template)/field[@group='unknown_queriables']"/>
	</div>
	
	<input type='button' value='Query' class="dataset_button_query" id="dataset_button_query"/>
	<div id='dataset_query_status' class='ifyMessage'>
		__	
	</div>
</xsl:template>


<xsl:template match="os:Url[@type='application/rdf+xml']">
	<xsl:variable name="template">
		<xsl:call-template name="str:split">
			<xsl:with-param name="string" select="substring-after(@template,'?')" />
			<xsl:with-param name="pattern" select="'&amp;'" />
		</xsl:call-template>
	</xsl:variable> 
<!--<xsl:copy-of select="$template"/>-->
	<div id="divSeriesQueryForm"><form method='get'>
		 <select id='series'><option><xsl:attribute name="value"><xsl:value-of select="item/name"/></xsl:attribute><xsl:value-of select="item/caption"/></option></select>
		<div id="divDatasetSelection">
			<xsl:apply-templates select="msxsl:node-set($template)/field" mode="widget"/>
		</div>
		
		<xsl:apply-templates select="." mode="queryinput"/>
		
		
	
		
		<xsl:if test="substring-before(@template,'{geo:uid')!=''">
		<div id="dataset_download" class="hidden">
			<xsl:apply-templates select="../os:Url[substring-before(@template,'{geo:uid')!='']" mode="geouid" />
		</div>
		</xsl:if>

		
	</form></div>
	<select multiple='multiple' id='dataset' class='dataset'><xsl:text> </xsl:text>
	</select>
		<div id='metadata'>
			<table id="table" class="display table_dataset ifyMoreInfo">
			<thead><tr><th class="no_sort">id</th><th class="no_sort">_</th><xsl:for-each select="$services/dataset/metadata/*[concat('',@hidden)!='true']"><th><xsl:value-of select="."/></th></xsl:for-each></tr></thead>
			<tbody>
			</tbody>
			</table>
		</div>
		<script type="text/javascript">			
		$(document).ready(function() {
			// link the html form elements to their opensearch extensions 
			<xsl:for-each select="msxsl:node-set($template)/*[concat('',@type)!='']">
			<!--$("#<xsl:value-of select='@name'/>").addClass("<xsl:value-of select='@type'/>".replace(':','_'));-->
			$("#<xsl:value-of select='@name'/>").data("ext", "<xsl:value-of select='@type'/>");</xsl:for-each>
			
			<xsl:apply-templates select="msxsl:node-set($template)/field" mode="javascript"/>
			
			$("#series option").data("urltemplate","<xsl:value-of select="@template" disable-output-escaping="yes"/>");
			<xsl:variable name="templateType" select="@type"/>
			$("#dataset")<xsl:for-each select="$services/dataset/catalogue[@type=$templateType]/*[@name!='identifier']"><xsl:variable name="typeName" select="@name"/>
				.data("catalogue:<xsl:value-of select='@name'/>","<xsl:value-of select="."/>")</xsl:for-each>
				.data("catalogue:identifier","dc:identifier")
				.data("metadataDef", new Array(<xsl:for-each select="$services/dataset/metadata/element">
					new Array("<xsl:value-of select="@value"/>","<xsl:value-of select="."/>","<xsl:value-of select="@name"/>","<xsl:value-of select="@hidden"/>","<xsl:value-of select="@inherited"/>","<xsl:value-of select="@required"/>")<xsl:if test="position() &lt; last()">,</xsl:if></xsl:for-each>))
				.data("series","#series")
				.data("template","<xsl:value-of select="$templateType"/>");
			
			<xsl:apply-templates select="../os:Url[substring-before(@template,'{geo:uid')!='']" mode="javascript" />
<!--			$(".catalogue_query_extension :input").each(function (i) {-->
<!--						$(this).before($("<span></span>")-->
<!--							.attr({id:'_'+ $(this).attr('name') + '_status'})-->
<!--							.addClass('serviceFieldValidity')-->
<!--						);-->
<!--					});-->
			<xsl:for-each select="msxsl:node-set($template)/*[@optional='true']">
			$("#<xsl:value-of select='@name'/>").addClass("ifyOptional");</xsl:for-each>

		})
	</script>
		<!--
		<form method='get'>
		<textarea id="selectedProducts">
			teste
		</textarea>
		</form>
		-->

<!--	<xsl:apply-templates/>-->

</xsl:template>



<xsl:template name="OpenSearchDescriptionHead">
		<script src="/js/series.js">{}</script>
		<link rel="stylesheet" type="text/css" href="/template/css/series.css" />
</xsl:template>


<xsl:template match="os:OpenSearchDescription">
<!-- add here all the html code  -->
<html>
<head>
	<meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
	<link rel="stylesheet" type="text/css" href="/template/css/gpod.css" />
	<xsl:apply-templates select="." mode="head"/>	
	<xsl:call-template name="OpenSearchDescriptionHead"/>
</head>

	<div id="LongName"><xsl:value-of select="os:LongName"/></div>
	<div id="Description"><xsl:value-of select="os:Description"/></div>
	<xsl:apply-templates select="os:Url"/>
</html>
</xsl:template>



</xsl:stylesheet>
