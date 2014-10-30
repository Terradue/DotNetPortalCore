<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<!-- Generic control panel display   -->

<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">

<xsl:import href='../elements.xsl'/>

<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
</xsl:template>


<xsl:template match="itemList">
	<br/>
	<div id="title" class="page-list-title"><xsl:value-of select="title"/> </div>
	<div id="description"><xsl:value-of select="description"/> </div>

	<div class="elements">
	<xsl:for-each select="items/item">
		<div class="item">
		<xsl:attribute name="onClick">document.location='<xsl:value-of select="@link"/>'</xsl:attribute>
		<img><xsl:attribute name="src"><xsl:value-of select="icon"/></xsl:attribute>
		<xsl:attribute name="class">icon</xsl:attribute></img>
		<xsl:value-of select="caption"/> 
		</div>
	</xsl:for-each>
	<br clear="all"/>
	</div>

</xsl:template>
</xsl:stylesheet>