<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/services
 Name:          const.xsl
 Version:       0.1 
 Description:   Generic template for service's const in classic service style like the g-pod 1.0

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/"
	 xmlns:str="http://exslt.org/strings"
	 xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	 xmlns:ify="http://www.terradue.com/ify"
	 >

<!--################################################################################################ CONST -->
<xsl:template match="const"/>
<xsl:template match="const" mode="head"/>


<!--################################################################################################ CONST/* -->
<xsl:template match="const/*" mode="head"/>
<xsl:template match="const/*" mode="javascript"/>
<xsl:template match="const/*" />


<!--################################################################################################ CONST @NAME=LOOKUP -->
<xsl:template match="const[@name='lookup' and @owner!='']">
	<div class="ifyLookup">
		<xsl:attribute name="id">_<xsl:value-of select="@owner"/>_lookup</xsl:attribute>
		<xsl:if test="count(element[@name='caption'])!=0">		
		<label><xsl:value-of select="element[@name='caption']"/></label> 		
		</xsl:if>		
		<input type="text"/>
		<input type="button" value="Search">
			<xsl:attribute name="id"><xsl:value-of select="@owner"/>_lookup_button_query</xsl:attribute>
		</input>
		<div class="query_message">
			<xsl:attribute name="id"><xsl:value-of select="@owner"/>_lookup_query_status</xsl:attribute>
			_
		</div>
		<select>
			<xsl:attribute name="id"><xsl:value-of select="@owner"/>_lookup</xsl:attribute>
			<option>---</option>
		</select>
	</div>
</xsl:template>

<xsl:template match="const[@name='lookup' and @owner!='']" mode="javascript">
	<xsl:variable name="owner" select="@owner"/>
	<xsl:for-each select="element"><xsl:choose>
		<xsl:when test="count(element)!=0">
		$("#<xsl:value-of select="$owner"/>_lookup").data("<xsl:value-of select="@name"/>",new Array(<xsl:for-each select="element">new Array("<xsl:value-of select="@value"/>","<xsl:value-of select="."/>","<xsl:value-of select="@name"/>","<xsl:value-of select="@hidden"/>")<xsl:if test="position() &lt; last()">,</xsl:if></xsl:for-each>));
		</xsl:when>
		<xsl:otherwise>
		$("#<xsl:value-of select="$owner"/>_lookup").data("<xsl:value-of select="@name"/>","<xsl:value-of select="."/>");</xsl:otherwise>
		</xsl:choose>
	</xsl:for-each>
	$("#<xsl:value-of select="$owner"/>_lookup").data("owner","<xsl:value-of select="$owner"/>");
	$("#<xsl:value-of select="$owner"/>_lookup").data("scope","#_<xsl:value-of select="$owner"/>_lookup :input");
	$("#_<xsl:value-of select="$owner"/>_lookup input[type='text']").data("ext","searchTerms");
</xsl:template>


<!--########################################################################### CONST/AOI  -->
<!-- 
removed with the new widget for the configurable values  
<xsl:template match="const/AOI"  mode="javascript">
		$("#mapPanel .ifyServiceAoi").data("owner","<xsl:value-of select="@owner"/>");
		$("#mapPanel .ifyServiceAoi").data("AOI","<xsl:value-of select="@file"/>");
</xsl:template>

<xsl:template match="const/AOI[@owner!='']">
		<div class='ifyServiceAoiDiv'>
			<label><xsl:value-of select="@caption"/> </label>
			<select class='ifyServiceAoi'>
				<option value="">-</option>
			</select>
		</div>
</xsl:template>
-->


<!--################################################################################################ CONST @NAME=MAP -->
<xsl:template match="const[@name='map']" mode="head">
	<xsl:variable name="this" select="."/>
	<xsl:variable name="map">
		<xsl:for-each select="$services/map/*">
		<xsl:variable name="elname" select="name()"/>		
		<xsl:choose>
			<xsl:when test="count($this/*[name()=$elname])!=0">
				<xsl:copy-of select="$this/*[name()=$elname]"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:copy-of select="."/>
			</xsl:otherwise>
		</xsl:choose>
		</xsl:for-each>
	</xsl:variable>
	<script src="/js/OpenLayers.js">{}</script>
	<script src="/js/services/classic/map.js">{}</script> 
	<xsl:apply-templates select="msxsl:node-set($map)/mapControls" mode="head"/> 
	<xsl:apply-templates select="*" mode="head"/>
</xsl:template>

<xsl:template match="const[@name='map']">	
	<xsl:variable name="id"><xsl:choose><xsl:when test="concat(@id,'')!=''"><xsl:value-of select="@id"/></xsl:when><xsl:otherwise>ifyServiceMap</xsl:otherwise></xsl:choose></xsl:variable>
	<div id='mapPanel'>
		<div id='paneldiv' class='olControlPanel'>_</div>
		<div class='ifyOSMap' ><xsl:attribute name="id"><xsl:value-of select="$id"/></xsl:attribute>This page needs javascript</div>
		<xsl:apply-templates select="*"/>
	</div>
</xsl:template>

<xsl:template match="const[@name='map']" mode="javascript">
	<xsl:variable name="id"><xsl:choose><xsl:when test="concat(@id,'')!=''"><xsl:value-of select="@id"/></xsl:when><xsl:otherwise>ifyServiceMap</xsl:otherwise></xsl:choose></xsl:variable>	
	<xsl:variable name="projection" select="element[@name='projection']"/>
	<xsl:variable name="this" select="."/>
	<xsl:variable name="map">
		<xsl:for-each select="$services/map/*">
		<xsl:variable name="elname" select="name()"/>		
		<xsl:choose>
			<xsl:when test="count($this/*[name()=$elname])!=0">
				<xsl:copy-of select="$this/*[name()=$elname]"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:copy-of select="."/>
			</xsl:otherwise>
		</xsl:choose>
		</xsl:for-each>
	</xsl:variable>
		$("#<xsl:value-of select="$id"/>").data("OLMap",
				new OpenLayers.Map('<xsl:value-of select="$id"/>', 
					<xsl:value-of select="msxsl:node-set($map)/mapOptions" disable-output-escaping="yes"/>
				)
		);
		
		$("#<xsl:value-of select="$id"/>").data("OLMap").addLayers(<xsl:value-of select="msxsl:node-set($map)/wmsLayers" disable-output-escaping="yes"/>);
		$("#<xsl:value-of select="$id"/>").data("OLMap").zoomToMaxExtent();
		
		// patch to have the OSMap variable 
		OSMap= $("#<xsl:value-of select="$id"/>").data("OLMap");
		
		SetMapControls($("#<xsl:value-of select="$id"/>").data("OLMap").layers.length, $("#<xsl:value-of select="$id"/>").data("OLMap"));
		<xsl:apply-templates select="msxsl:node-set($map)/mapControls/element" mode="javascript">
			<xsl:with-param name="this" select="$this"/>
		</xsl:apply-templates>
		<xsl:apply-templates select="*"  mode="javascript"/>
</xsl:template>


<!--################################################################################################ MapControls -->
<xsl:template match="mapControls/element" mode="javascript"/> 

<xsl:template match="mapControls/element" mode="head"/> 

<xsl:template match="mapControls/element[@type='geo:box']" mode="head">
	<script src="/js/geoselect.js">{}</script></xsl:template>

<xsl:template match="mapControls/element[@type='geo:box']" mode="javascript">
	<xsl:param name="this" select="."/>	
	<xsl:variable name="id"><xsl:choose><xsl:when test="concat(../@id,'')!=''"><xsl:value-of select="../@id"/></xsl:when><xsl:otherwise>ifyServiceMap</xsl:otherwise></xsl:choose></xsl:variable>
	
	<xsl:variable name="owner" select="$this/@owner"/>
	var Sel = startSelectGeoBox($("#<xsl:value-of select="$id"/>").data("OLMap"),".geo_box")
	<xsl:variable name="el" select="$this/../field[@name=$owner]"/>
	
	<xsl:if test="not( (concat($el/@range,'')='closed' or concat($el/@range,'')='') and concat($el/@scope,'')!='')">
		OSIconPanel.addControls([Sel]);
		selectionBox.activate();
	</xsl:if>
</xsl:template>

</xsl:stylesheet>
