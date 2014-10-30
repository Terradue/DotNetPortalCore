<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<!-- Generic news control panel display  -->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">
<xsl:import href='admin.xsl'/>

<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<link rel="stylesheet" type="text/css" href="/template/css/cal.css" />
	<script src="/js/cal.js">{}</script>
</xsl:template>

<xsl:template match="content/singleItem">
	<xsl:variable name="itemName" select="item/title"/>
	<div class="siteNavigation">
	<a href="/admin/">Control Panel </a> /	<a href="?"><xsl:value-of select="@entity"/></a> / <xsl:value-of select="$itemName"/>
	</div>
	<xsl:apply-imports/>
</xsl:template>


<xsl:template match="itemList">
	<div class="siteNavigation">
	<a href="/admin/">Control Panel </a> /	<xsl:value-of select="@entity"/>
	</div>
	<xsl:apply-imports/>
</xsl:template>

</xsl:stylesheet>