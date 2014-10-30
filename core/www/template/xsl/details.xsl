<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<!-- Specific template for task lists -->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/"
	 xmlns:str="http://exslt.org/strings"
	 xmlns:ify="http://www.terradue.com/ify"
	 xmlns:msxsl="urn:schemas-microsoft-com:xslt">

<xsl:import href='elements.xsl'/>

<xsl:template match="content[itemList/@entity='Task']" mode="head">
	<script src="/js/cal.js">{}</script>
	<script src="/js/tasks.js">{}</script>
	<script src="/js/dates.js">{}</script>
	<script src="/js/paging.js">{}</script>
</xsl:template>

<xsl:template match="content" mode="head">
	<script src="/js/tasks.js">{}</script>
</xsl:template>

<xsl:template match="job" mode="draw">
	<div class="jobNodes">
		<xsl:apply-templates select="processings/processing"/>
	</div>
	<script type="text/javascript">
		$(document).ready(function() {	
			$("select.jobParameter").change(function(){
				var newVal = $(this).parent().find("div." + $(this).val()).html();
				$(this).parent().find("textarea").val(newVal);
			})
		})
	</script>
</xsl:template>

<xsl:template match="processing">
		<div class="node">
			<div class="info">
				<xsl:attribute name="id"><xsl:value-of select="@pid"/></xsl:attribute>
				<img><xsl:attribute name="src">/template/images/node.gif</xsl:attribute></img>
				<label>[<xsl:value-of select="@pid"/>]</label><br/>
				<xsl:if test="arguments/@total">[<xsl:value-of select="arguments/@done"/>/<xsl:value-of select="arguments/@total"/>]
				<br/></xsl:if>
				<xsl:value-of select="hostname"/><br/>
			</div>
			<div class="log">
				<label>Last notification:</label>
				[<xsl:value-of select="notification/time"/>] <xsl:value-of select="notification/message"/><br/>
				<xsl:for-each select="logs/log">
						<a target="_blank"><xsl:attribute name="href">/proxy4.aspx?url=<xsl:value-of select="@link"/></xsl:attribute>
						<xsl:call-template name="basename">
							<xsl:with-param name="path"><xsl:value-of select="@link"/></xsl:with-param> 
						</xsl:call-template>
						</a><br/>
					</xsl:for-each>
				
			</div>
		</div>
</xsl:template>


<xsl:template match="job">
	<xsl:variable name="status" select="status"/>
	<div class="jobInfo">
		<xsl:attribute name="id"><xsl:value-of select="name"/></xsl:attribute>
		<input type="hidden">
			<xsl:attribute name="value"><xsl:value-of select="@link"/></xsl:attribute>
		</input>
		<div class="jobName"><xsl:value-of select="name"/></div>
	  		<div class="jobDetailsContent">							
				<xsl:apply-templates select="." mode="draw"/>
				<xsl:apply-templates select="../singleItem[@entity='Job']"/>
			</div>					
	</div>
</xsl:template>


</xsl:stylesheet>