<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<!-- Generic scheduler control panel display  -->

<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">

<xsl:import href='../elements.xsl'/>

<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<xsl:choose>
	<xsl:when test="count(//content/singleItem)=0">
	<xsl:copy-of select="//content/itemList/link"/>
	<script src="/js/cal.js">{}</script>
	<script src="/js/dates.js">{}</script>
	<script src="/js/paging.js">{}</script>
	<xsl:if test="//content/itemList/@entity='Task'">
	<script>
     Timeline_ajax_url="/js/timeline_2.3.0/timeline_ajax/simile-ajax-api.js";
     Timeline_urlPrefix='/js/timeline_2.3.0/timeline_js/';       
     Timeline_parameters='bundle=true';
    </script>
		<script src="/js/timeline_2.3.0/timeline_js/timeline-api.js" type="text/javascript">{}</script>
	</xsl:if>
	</xsl:when>
	<xsl:otherwise>
	</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template match="scheduler">
	<div class="siteNavigation"><a href="?">Schedulers</a> / <xsl:value-of select="caption"/></div>
</xsl:template>


<xsl:template match="item/definition|field[@name='definition']" mode="table"/>
<xsl:template match="item/name|field[@name='name']" mode="table"/>


<xsl:template match="field[@name='enabled']" mode="table">
<th></th>
</xsl:template>

<xsl:template match="item/enabled" mode="table">
	<td>
	<img>
		<xsl:choose>
			<xsl:when test=".='false'">
				<xsl:attribute name="src" ><xsl:value-of select="'/template/images/scheduler-stop.png'"/></xsl:attribute>
			</xsl:when>
			<xsl:otherwise>
				<xsl:attribute name="src"><xsl:value-of select="'/template/images/scheduler-play.png'"/></xsl:attribute>
			</xsl:otherwise>
		</xsl:choose>
	</img>
	</td>
</xsl:template>
<xsl:template match="item/caption" mode="table">
	<td><a>	
		<xsl:attribute name="href"><xsl:value-of select="../@link"/></xsl:attribute>
		<xsl:value-of select="."/></a> 
	</td>
</xsl:template>
<xsl:template match="definition">
	<td><a> 
                <xsl:attribute name="href"><xsl:value-of select="@link"/></xsl:attribute>
                <img src='/template/images/edit.png'/></a> 
        </td>

</xsl:template>
<xsl:template match="item" mode="table_edit">
	<xsl:apply-templates select="definition"/>
</xsl:template>

<xsl:template match="itemList">
	<!--<div id="title" class="title"> <xsl:value-of select="@entity"/> </div>-->
	<div class="page-list">
	<div class="page-list-title">
		<xsl:value-of select="@entity" />
	</div>
	<div id="element">
		<xsl:apply-templates select="link">
			<xsl:with-param name="mainSearchElements" select="'ify:enabled,searchTerms,count,ify:sort'"/>
		</xsl:apply-templates>
		<br />
		<xsl:if test="os:totalResults != 0">

		<form action="" method="get" id="ifySelectPagingControl">
			<xsl:apply-templates select="." mode="table"/>
	
			<xsl:apply-templates select="operations"/>
			<input type="hidden" class="ifyHiddenIdElement" name="id" id="ifySelectPagingControlIds" value=""/>
			<p/>
		</form>
		<xsl:if test="@entity='Task'">
		<div id="my-timeline" style="height: 150px; border: 1px solid #aaa">empty</div>
		</xsl:if>
		<script type="text/javascript">
	$(document).ready(function() {

		<xsl:apply-templates select="fields" mode="javascript"/>
		<xsl:apply-templates select="operations" mode="javascript"/>
		<!--
		 var tl;
 {
   var myEventSource = new Timeline.DefaultEventSource();

   var timeline_json = {  
	'wiki-url':"<xsl:value-of select="@link"/>",
	'dateTimeFormat':'iso8601',
	'events':[
	<xsl:for-each select="items/item"><xsl:if test="inputStartTime!='' and inputEndTime!=''">
	{
	'start':'<xsl:value-of select="inputStartTime"/>',
	'end':'<xsl:value-of select="inputEndTime"/>',
	'durationEvent':true,
	'title':"<xsl:value-of select="@caption"/>",
	'color' :"<xsl:value-of select="status"/>",
	'link':"<xsl:value-of select="@link"/>"}
	<xsl:if test="position() &lt; last()">,</xsl:if>
	</xsl:if></xsl:for-each>
	]}

   var bandInfos = [
     Timeline.createBandInfo({
         width:          "70%", 
         eventSource : myEventSource,
	 intervalUnit:   Timeline.DateTime.MONTH, 
         intervalPixels: 100
     }),
     Timeline.createBandInfo({
         width:          "30%", 
         eventSource : myEventSource,
         intervalUnit:   Timeline.DateTime.YEAR, 
         intervalPixels: 200
     })
   ];
   bandInfos[1].syncWith = 0;
   bandInfos[1].highlight = true;
   tl = Timeline.create(document.getElementById("my-timeline"), bandInfos);
   myEventSource.loadJSON(timeline_json, document.location.href)
//	tl.loadJSON(timeline_json,".");
//   var ampersandChar=String.fromCharCode(38);
//   tl.loadXML("<xsl:value-of select='@link'/>" + ampersandChar + "_format=timeline");
 }

 var resizeTimerID = null;
 function onResize() {
     if (resizeTimerID == null) {
         resizeTimerID = window.setTimeout(function() {
             resizeTimerID = null;
             tl.layout();
         }, 500);
     }
 }
-->
	})
		</script>
	</xsl:if>
	</div>
	</div>
</xsl:template>

</xsl:stylesheet>
