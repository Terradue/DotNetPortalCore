<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl
 Name:          schedulers_timeline.xsl
 Version:       0.1 
 Description:   Transformation of scheduler's task into time line format http://code.google.com/p/simile-widgets/wiki/Timeline_EventSources

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:os="http://a9.com/-/spec/opensearch/1.1/" xmlns:ify="http://www.terradue.com/ify" >
<xsl:template match="item">
	<event>
	<xsl:attribute name="start"><xsl:value-of select="inputStartTime"/></xsl:attribute>
	<xsl:attribute name="end"><xsl:value-of select="inputEndTime"/></xsl:attribute>
	<xsl:attribute name="isDuration"><xsl:value-of select="'true'"/></xsl:attribute>
	<xsl:attribute name="title"><xsl:value-of select="@caption"/></xsl:attribute>
	<xsl:attribute name="link"><xsl:value-of select="@link"/></xsl:attribute>
	<xsl:attribute name="color"><xsl:value-of select="status"/></xsl:attribute>
    </event>

</xsl:template>


<xsl:template match="itemList">
	<data>
	<xsl:attribute name="wiki-url"><xsl:value-of select="@link"/></xsl:attribute>
	<xsl:attribute name="date-time-format">iso8601</xsl:attribute>
	<xsl:apply-templates select="items/item"/>
	</data>
</xsl:template>

<xsl:template match="content">
	<xsl:apply-templates select="itemList"/>
</xsl:template>
</xsl:stylesheet>
