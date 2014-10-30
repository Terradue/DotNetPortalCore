<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->

<!-- Generic display templates for all control panel elements -->

<xsl:stylesheet version="2.0" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:os="http://a9.com/-/spec/opensearch/1.1/" xmlns:ify="http://www.terradue.com/ify" >

<xsl:import href='../elements.xsl'/>

<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<xsl:choose>
	<xsl:when test="count(//content/singleItem)=0">
	<xsl:copy-of select="//content/itemList/link"/>
	<script src="/js/paging.js">{}</script>
	</xsl:when>
	<xsl:otherwise>
	</xsl:otherwise>
	</xsl:choose>
</xsl:template>


<xsl:template match="itemList">
	<div id="title" class="title"> <xsl:value-of select="@entity"/> </div>
	<xsl:apply-templates select="link">
		<xsl:with-param name="mainSearchElements" select="'ify:enabled,ify:accountStatus,searchTerms,count,ify:sort'"/>
	</xsl:apply-templates>
	<form action="" method="get" id="ifySelectPagingControl">
	<xsl:apply-templates select="." mode="table"/>

 	<xsl:apply-templates select="operations"/>
		<input type="hidden" class="ifyHiddenIdElement" name="id" id="ifySelectPagingControlIds" value=""/>
		<p/>
	</form>
	<script type="text/javascript">
	$(document).ready(function() {
		<xsl:apply-templates select="operations" mode="javascript"/>
	})
	</script>

</xsl:template>

<xsl:template match="content/singleItem">
	
	<div class="page-item-content">
		<xsl:choose>
			<xsl:when test="item/@id != '' ">Change the <xsl:value-of select="@entity"/> settings here</xsl:when>
			<xsl:otherwise>Create a new <xsl:value-of select="@entity"/> settings here</xsl:otherwise>
		</xsl:choose>

	</div> 
<!--	<br/> 	
	/* Styles for this page  */
	<br/>
	<xsl:for-each select="item/*">
		#_<xsl:value-of select="name(.)"/> {}<br/>
		#<xsl:value-of select="name(.)"/> {}<br/>
	</xsl:for-each>
 -->
	<div id="elements">
		<div>
			<form action="" method="get" id="myform" accept-charset="utf-8">
				<xsl:for-each select="fields/field">
					<xsl:variable name="name" select="@name"/>
					<xsl:apply-templates select=".">
						<xsl:with-param name="value" select="../../item/*[name()=$name]"/>
					</xsl:apply-templates>
				</xsl:for-each>
				<input type="hidden" class="ifyHiddenIdElement" name="id"><xsl:attribute name="value"><xsl:value-of select="item/@id"/></xsl:attribute></input>
				<xsl:apply-templates select="operations"/>
				 <p/>
			</form>
	<script type="text/javascript">
	$(document).ready(function() {
		<xsl:apply-templates select="fields" mode="javascript"/>
		<xsl:apply-templates select="operations" mode="javascript"/>
		<xsl:for-each select="fields/*[concat('',@optional)='true']">
		$("#<xsl:value-of select='@name'/>").addClass("ifyOptional");</xsl:for-each>
	})
	</script>

		</div>
	</div>
</xsl:template>


</xsl:stylesheet>
