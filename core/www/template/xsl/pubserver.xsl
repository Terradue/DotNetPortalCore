<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/admin
 Name:          pubserver.xsl
 Version:       0.1 
 Description:   Generic publication server control panel display  

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">

<xsl:import href='admin/admin.xsl'/>

<xsl:template match="content/singleItem">
	<xsl:variable name="itemName" select="item/caption"/>
	<div class="siteNavigation">
	<a href="?"><xsl:value-of select="@entity"/></a> / <xsl:value-of select="$itemName"/>
	</div>
	<xsl:apply-imports/>
</xsl:template>

<xsl:template match="itemList">
	<xsl:apply-imports/>
</xsl:template>


</xsl:stylesheet>