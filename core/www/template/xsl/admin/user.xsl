<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:ify="http://www.terradue.com/ify"
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">
<xsl:import href='admin.xsl'/>

<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<script type="text/javascript">
		$(document).ready(function(){
		$("form").attr("autocomplete","off");
		//$("#username").val("");
		$("#password").val("");
		});		
	</script>
</xsl:template>

<xsl:template match="content/singleItem">	
	<xsl:variable name="itemName" select="item/username"/>
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



<!-- this was to block the display of the username 
<xsl:template match="field[@name='username']">
	<xsl:if test="count(../../item)=0">
		<xsl:apply-imports/>
	</xsl:if>
</xsl:template>
-->
<!-- this was to block the display of the password 
<xsl:template match="field[@name='password']">
	<xsl:if test="count(../../item)=0">
		<xsl:apply-imports/>
	</xsl:if>
</xsl:template>
-->

<!-- <xsl:template match="field[@type='ify:enabled' or @type='ify:accountStatus']">
	<xsl:param name="element" select="."/>
	<xsl:variable name="name" select="@name"/>
	<xsl:variable name="status_query" select="concat($element/os:Query/@ify:enabled,'',$element/os:Query/@ify:accountStatus)"/>
	<div id="user_status_tabs">
	<ul class="ifyShadetabs">
		<xsl:for-each select="$config/active/*">
			<li><xsl:attribute name="class">ifyShadeTab<xsl:if test="$status_query = @value "> ifyShadeTabSelected</xsl:if></xsl:attribute><a>
			<xsl:attribute name="href">?<xsl:value-of select="$name"/>=<xsl:value-of select="@value"/></xsl:attribute>			
			<xsl:value-of select="."/></a></li>
		</xsl:for-each>
	</ul>
	<input type="hidden">
		<xsl:attribute name="value"><xsl:value-of select="$status_query"/></xsl:attribute>
		<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
	</input>
	</div>
</xsl:template> -->
</xsl:stylesheet>
