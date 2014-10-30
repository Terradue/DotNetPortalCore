<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Name:          series.xsl
 Context:       template/xsl
 Version:       0.1 
 Description:   Generic template list of series available to the user 

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:str="http://exslt.org/strings"
	 xmlns:msxsl="urn:schemas-microsoft-com:xslt"  
    xmlns:dclite4g="http://dclite4g.xmlns.com/"
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">

<xsl:import href='elements.xsl'/>

<xsl:include href="./opensearch.xsl"/>


<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<xsl:if test="count(//singleItem)=0">
	<script src="/js/paging.js">{}</script>
	</xsl:if>
	<xsl:if test="count(//singleItem)!=0">
		<xsl:apply-templates select="msxsl:node-set($template)" mode="head"/>
			<script src="/js/series.js">{}</script>
		<link rel="stylesheet" type="text/css" href="/template/css/series.css" />
<!-- 		<xsl:call-template name="OpenSearchDescriptionHead"/> -->
	</xsl:if>		
</xsl:template>

<xsl:variable name="template">
	<xsl:call-template name="str:split">
		<xsl:with-param name="string" select="substring-after(content/singleItem/item/catalogueTemplate,'?')" />
		<xsl:with-param name="pattern" select="'&amp;'" />
	</xsl:call-template>
</xsl:variable> 



<xsl:template match="singleItem">
	<div class='page-item-title'>
		<xsl:value-of select="item/caption" disable-output-escaping="yes" />
	</div>
	<div class="itemAbstract">
		<xsl:value-of select="item/descr" disable-output-escaping="yes" />
		<xsl:if test="item/descr=''">
			_
		</xsl:if>
	</div>
	<div class='page-item-content'>
	<xsl:choose>
		<xsl:when test="item/catalogueTemplate='no'">
			<xsl:variable name="urlEl">
				<os:Url type="application/rdf+xml" indexOffset="0" pageOffset="0">
					<xsl:attribute name="template"><xsl:value-of select="item/catalogueTemplate"/></xsl:attribute>
				</os:Url>
			</xsl:variable>
			
			<xsl:apply-templates select="msxsl:node-set($urlEl)"/>
		</xsl:when>
		<xsl:otherwise>
			<xsl:variable name="temp" select="item/catalogueDescription"/>
			<!-- <xsl:variable name="temp" select="concat('http://localhost/catalogue/gpod',substring-after(item/catalogueDescription,'$CATALOGUE'))"/>-->
			<xsl:variable name="description" select="document(concat(substring-before(//content/@link,'//'),'//',substring-before(substring-after(//content/@link,'//'),'/'),'/proxy4.aspx?url=',$temp))"/>
			<xsl:apply-templates select="$description/os:OpenSearchDescription/os:Url"/>
		</xsl:otherwise>
	</xsl:choose>
	<!--
	<xsl:if test="item/catalogueTemplate=''">Template is empty </xsl:if>
	<xsl:if test="item/catalogueTemplate!=''">
		<xsl:variable name="description" select="document(concat('','http://localhost/catalogue/gpod/MER_RR__1P/description'))"/>
		<xsl:choose>
			<xsl:when test="concat($description,'')=''">
				unable to load <xsl:value-of select="concat('/proxy4.aspx?url=',item/catalogueDescription)"/>
			</xsl:when>
			<xsl:otherwise>
			<xsl:apply-templates select="$description/os:OpenSearchDescription/os:Url"/>
			</xsl:otherwise>
		</xsl:choose>	
	</xsl:if>
	-->
	</div>
</xsl:template>

<xsl:template match="item">
	<div class="item">		
		<a><xsl:attribute name="href"><xsl:value-of select="concat(../../@link,'?id=',@id)"/></xsl:attribute>
			<img class="icon" src="/template/images/iconborder.gif">
				<xsl:attribute name="style">background-image:url('<xsl:value-of select="logo_url"/>')</xsl:attribute>
			</img>
			<xsl:value-of select="*[name()=//fields/field[@type='string']/@name]"/> 					
		</a>
		<div class="itemDescription">
			<xsl:value-of select="*[name()=//fields/field[@type='text']/@name]" disable-output-escaping="no" /> 
			<xsl:if test="*[name()=//fields/field[@type='text']/@name]=''">
				please insert series description on control panel  
			</xsl:if>
		</div>
	</div>
</xsl:template>


<xsl:template match="itemList">
	<div id="title" class="page-list-title"> <xsl:value-of select="@entity"/> </div>
	<xsl:apply-templates select="link"/>
	<div class="page-list" id="elements">
		<xsl:apply-templates select="items/item"/>
	</div>
</xsl:template>


<xsl:template match="field">
	<xsl:variable select="@type" name="type"/>
		<xsl:choose>
			<xsl:when test="concat($opensearch/items[@type=$type]/@caption,'')!=''">
				<xsl:apply-imports/>
			</xsl:when>
			<xsl:when test="concat($config/labels/label[@type=$type],'')!=''">
 				<xsl:apply-imports/> 
			</xsl:when>
			<xsl:otherwise>
			</xsl:otherwise>
		</xsl:choose> 

</xsl:template>

</xsl:stylesheet>
