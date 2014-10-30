<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl
 Name:          project.xsl
 Version:       0.1 
 Description:   Generic template list of projects

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">

<xsl:import href='elements.xsl'/>

<xsl:template match="content" mode="head">
	<script src="/js/paging.js">{}</script>
</xsl:template>

<xsl:template match="singleItem">
	<div class='page-item'>
	<div class='page-item-title'>
		<xsl:value-of select="item/title" disable-output-escaping="yes" />
	</div>
	<div class="itemAbstract">
	
	<xsl:value-of select="item/short_descr" disable-output-escaping="yes" />
	</div>
	<div class='page-item-content'>
		<xsl:value-of select="item/long_descr" disable-output-escaping="yes" />
		<label>Status: </label><xsl:value-of select="item/status"/>
		<label>Contact: </label><xsl:value-of select="item/contact"/>
	</div>
	</div>
</xsl:template>

<xsl:template match="item">
	<div class='page-list-item'>
	<div class="page-list-item-title">
		<xsl:value-of select="title" disable-output-escaping="yes" />
	</div>
	<div class="page-list-item-content">
		<xsl:value-of select="short_descr" disable-output-escaping="yes" />
		<a class="page-list-item-link"><xsl:attribute name="href"><xsl:value-of select="concat(../../@link,'?id=',@id)"/></xsl:attribute>Full Story</a>
	</div> 
	</div>
</xsl:template>


<!-- this is for a full list display when no single item is present -->
<xsl:template match="itemList[count(../singleItem)=0]">
	<div class="page-list" id="elements">
		<div class="page-list-title">Related Projects </div>
		<xsl:apply-templates select="link"/>
		<xsl:apply-templates select="items/item"/>
	</div>
</xsl:template>

<!-- this is for a full list display when a top item is present -->
<xsl:template match="itemList">
	<div class="page-list" id="elements">
		<div class="page-list-title">Related Projects </div>
		<xsl:apply-templates select="items/item"/>
		<a class="page-list-all-link"><xsl:attribute name="href"><xsl:value-of select="@link"/></xsl:attribute>All news</a>
	</div>
</xsl:template>


</xsl:stylesheet>