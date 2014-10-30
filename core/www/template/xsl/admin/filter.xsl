<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<!-- Generic template for task lists -->

<xsl:stylesheet version="2.0" xmlns="http://www.w3.org/1999/xhtml"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:os="http://a9.com/-/spec/opensearch/1.1/"
	xmlns:str="http://exslt.org/strings" xmlns:ify="http://www.terradue.com/ify"
	xmlns:msxsl="urn:schemas-microsoft-com:xslt">


	<xsl:template match="content[singleItem/@entity='TaskFilter']"
		mode="head">
		<script src="/js/taskfilter.js">{}</script>
		<title>
			<xsl:value-of select="$config/title" />
			::
			<xsl:value-of select="task/description" />
		</title>
		<link rel="stylesheet" type="text/css" href="/template/css/taskfilter.css" />
	</xsl:template>

	<xsl:template match="content[itemList/@entity='TaskFilter']"
		mode="head">
		<title>
			<xsl:value-of select="$config/title" />
			:: Task Filters
		</title>
		<link rel="stylesheet" type="text/css" href="/template/css/table.css" />
		<link rel="stylesheet" type="text/css" href="/template/css/taskfilter.css" />
		<link rel="stylesheet" type="text/css" href="/template/css/cal.css" />
		<script src="/js/cal.js">{}</script>
		<script src="/js/dates.js">{}</script>
		<script src="/js/paging.js">{}</script>
	</xsl:template>
	
	<xsl:template match="itemList">
	<div class="siteNavigation">
	<a href="/admin/">Control Panel </a> /	<xsl:value-of select="@entity"/>
	</div>
	<xsl:apply-imports/>
</xsl:template>

		<xsl:template match="itemList2">
		<div class="page-list">
			<div class="page-list-title">
				<xsl:value-of select="@entity" />
			</div>
			<div id="element">
				<xsl:apply-templates select="link">
					<xsl:with-param name="mainSearchElements"
						select="'ify:status,searchTerms,count,ify:sort'" />
				</xsl:apply-templates>
				<form action="" method="get" id="ifySelectPagingControl">
					<table class="elements" border="0" id="elements">
						<tr>
							<th />
							<xsl:apply-templates select="fields/field[@name='caption']"
								mode="header" />
						</tr>

						<xsl:for-each select="items/item">
							<tr>
								<xsl:attribute name="id">ifyRow_<xsl:value-of
									select="@id" /></xsl:attribute>
								<xsl:variable name="id" select="@id" />
								<td class="ifySelectPagingControlCheckbox">
									<input type="checkbox"
										onclick="ifySelectPagingControlSelectElement(this.value)">
										<xsl:attribute name="value"><xsl:value-of
											select="@id" /></xsl:attribute>
										<xsl:attribute name="id">c<xsl:value-of
											select="@id" /></xsl:attribute>
										<xsl:attribute name="name">checkBoxId</xsl:attribute>
									</input>
								</td>

								<td>
									<xsl:value-of select="caption" />
								</td>
								<td>
									<a>
										<xsl:attribute name="href"><xsl:value-of
											select="@link" /></xsl:attribute>
										<img src='/template/images/edit.png' />
									</a>
								</td>
							</tr>
						</xsl:for-each>
					</table>
					<input type="hidden" class="ifyHiddenIdElement" name="id"
						id="ifySelectPagingControlIds" />
					<xsl:apply-templates select="operations" />
				</form>
			</div>
		</div>

	</xsl:template>

</xsl:stylesheet>