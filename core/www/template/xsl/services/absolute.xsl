<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/services
 Name:          absolute.xsl
 Version:       0.1 
 Description:   Generic template for absolute position service style

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
	 xmlns:msxsl="urn:schemas-microsoft-com:xslt">
<xsl:import href='./classic.xsl'/>

<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<link rel="stylesheet" type="text/css" href="/template/css/services/absolute.css" />
</xsl:template>

<xsl:template match="fields">
		<xsl:for-each select="*">
			<xsl:variable name="name" select="@name"/>
			<xsl:apply-templates select=".">
				<xsl:with-param name="value" select="../item/*[name()=$name]"/>		
			</xsl:apply-templates>
		</xsl:for-each>
		<div id='mainParameters'><h2>Parameters</h2></div>
		<div id='additionalParameters'><h3>Additional Parameters</h3></div>

</xsl:template>



</xsl:stylesheet>