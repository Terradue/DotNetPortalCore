<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<!--  Description:   Generic computer element control panel display  -->

<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">
<xsl:import href='admin.xsl'/>


<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<xsl:if test="count(//content/singleItem)!=0">
	<script src="/js/admin_ce.js">{}</script>
	</xsl:if>
</xsl:template>

<xsl:template match="content/singleItem">
	<xsl:variable name="itemName" select="item/caption"/>
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

<xsl:template match="field[@type='entities' and ( @name='wdirs' or @name='rdirs')]">
	<xsl:param name="value" select="."/>
	<xsl:variable name="name" select="@name"/>
	<div class="ifyEntities">		
		<xsl:attribute name="id">_<xsl:value-of select="$name"/></xsl:attribute>
		<xsl:apply-templates select="." mode="label"/>
		<select class="ifyEntitiesSelect">
			<xsl:attribute name="multiple">multiple</xsl:attribute>
			<xsl:attribute name="id">entity_<xsl:value-of select="$name"/></xsl:attribute>
			<xsl:for-each select="$value/item">
				<option>
					<xsl:if test="available='0'"><xsl:attribute name="class">ifyEntitiesDisabled</xsl:attribute></xsl:if>
					<xsl:attribute name="value"><xsl:value-of select="@value"/></xsl:attribute>
					<xsl:value-of select="path"/>
				</option>
			</xsl:for-each>
		</select>		
		<xsl:for-each select="fields/field">
			<xsl:variable name="element" select="@name"/>
			<input type='hidden'>
				<xsl:attribute name="class">entity_<xsl:value-of select="$element"/></xsl:attribute>
				<xsl:attribute name="name"><xsl:value-of select="concat($name,':',$element)"/></xsl:attribute>
				<xsl:attribute name="id"><xsl:value-of select="concat($name,':',$element)"/></xsl:attribute>
				<xsl:attribute name="value"><xsl:for-each select="../../item/*[name()=$element]"><xsl:value-of select="."/><xsl:if test="position() &lt; last()">,</xsl:if></xsl:for-each></xsl:attribute>
			</input>
		</xsl:for-each>
		<input type='hidden' class="ifyEntitiesDelete">
			<xsl:attribute name="name"><xsl:value-of select="concat($name,':delete')"/></xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="concat($name,':delete')"/></xsl:attribute>
		</input>
		<input type='hidden' class="ifyEntitiesUpdate">
			<xsl:attribute name="name"><xsl:value-of select="concat($name,':modify')"/></xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="concat($name,':modify')"/></xsl:attribute>
		</input>

		<input type="button" value="Enable" class="ifyEntitiesButton ifyEntitiesEnableButton"/>
		<input type="button" value="Disable" class="ifyEntitiesButton ifyEntitiesDisableButton"/>	
		<input type="button" value="Delete" class="ifyEntitiesButton ifyEntitiesDeleteButton"/>
		<input class="ifyEntitiesAddName" type="text"/>
		<input type="button" value="Add" class="ifyEntitiesAddButton"/>
	</div>
</xsl:template> 


</xsl:stylesheet>