<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.06.2010
 Element:       ify web portal
 Context:       template/xsl
 Name:          statistics.xsl
 Version:       0.1 
 Description:   Generic display templates for all control panel elements 

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:os="http://a9.com/-/spec/opensearch/1.1/" xmlns:ify="http://www.terradue.com/ify" >
<xsl:import href='elements.xsl'/>


<xsl:template match="content" mode="head">
	<xsl:choose>
	<xsl:when test="count(//content/singleItem)=0">
	<xsl:copy-of select="//content/itemList/link"/>
	<script src="/js/paging.js">{}</script>
	</xsl:when>
	<xsl:otherwise>
	</xsl:otherwise>
	</xsl:choose>
</xsl:template>


<xsl:template match="item_List">
	<div id="title" class="title"> <xsl:value-of select="@entity"/> </div>
	<xsl:apply-templates select="link"/>
	<form action="" method="get" id="ifySelectPagingControl">
 	<xsl:call-template name="table"/>
 	<xsl:apply-templates select="operations"/>
		<input type="hidden" class="ifyHiddenIdElement" name="id" id="ifySelectPagingControlIds" value=""/>
		<p/>
	</form>
	<script type="text/javascript">
	$(document).ready(function() {

		<xsl:apply-templates select="fields" mode="javascript"/>
		<xsl:apply-templates select="operations" mode="javascript"/>

	})
	</script>

</xsl:template>


</xsl:stylesheet>
