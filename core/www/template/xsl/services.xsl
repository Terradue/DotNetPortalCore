<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl
 Name:          services.xsl
 Version:       0.1 
 Description:   Generic template for the available services list

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
	<xsl:apply-imports/>
	<script src="/js/paging.js">{}</script>
	<script src="/js/services.js">{}</script>
</xsl:template>

<xsl:template match="itemList">	
	<div id="title" class="title"> <xsl:value-of select="@entity"/> </div>
	<xsl:apply-templates select="link"/>
		<div id="elements">
		<span>  </span>
		<xsl:apply-templates select="items/item" />			
	</div>
	<script type="text/javascript">
			highlightOnLoad('elements');
		</script>
</xsl:template>

<xsl:template name="for.star">
	<xsl:param name="i"/>
	<xsl:param name="value" select="'0'"/>
	<xsl:choose>
		<xsl:when test="$i &gt; $value">
			<img src="/template/images/rating_empty.png"/>
		</xsl:when>
		<xsl:otherwise>
			<img src="/template/images/rating_hover.png"/>
		</xsl:otherwise>
	</xsl:choose>
	<xsl:if test="$i &lt; 5">
	<xsl:call-template name="for.star">
		<xsl:with-param name="i" select="$i + 1"/>
		<xsl:with-param name="value" select="$value"/>
	</xsl:call-template>
	</xsl:if>
</xsl:template>

<xsl:template match="item">
	<div class="item">
		<a><xsl:attribute name="href">
			<xsl:value-of select="@link"/>
			<!--
			<xsl:choose>
				<xsl:when test="substring-before(@link,'_format=xml')=''">
					<xsl:value-of select="@link"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="concat(substring-before(@link,'_format=xml'),substring-after(@link,'_format=xml'))"/>
				</xsl:otherwise>
			</xsl:choose>
			-->
		</xsl:attribute>
			<img class="icon" src="/template/images/iconborder.gif">
				<xsl:attribute name="style">background-image:url('<xsl:value-of select="logoUrl"/>')</xsl:attribute>
			</img>
			<xsl:value-of select="*[name()=//fields/field[@type='string']/@name]"/>
		</a>
		<xsl:variable name="rating">
			<xsl:choose>
				<xsl:when test="rating!=''">
					<xsl:value-of select="rating"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="'0'"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:call-template name="for.star">
			<xsl:with-param name="i" select="'1'"/>
			<xsl:with-param name="value" select="$rating"/>
		</xsl:call-template>
		
		<div class="itemDescription">
			<xsl:value-of select="*[name()=//fields/field[@type='text']/@name]" disable-output-escaping="yes" /> 
		</div>
		
	</div>
</xsl:template>


</xsl:stylesheet>
