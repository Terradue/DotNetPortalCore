<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/
 Name:          task.xsl
 Version:       0.1 
 Description:   Specific template for task lists

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

<xsl:template match="singleItem"/>

<xsl:template match="singleItem[@entity='JobParameterSet']" mode="draw">
	
	<div class="jobParameter">
	<form action="" method="get" id="jobform">
		<xsl:attribute name="id"><xsl:value-of select="item/@id"/></xsl:attribute>
	<label><xsl:attribute name="for">param_<xsl:value-of select="item/@id"/></xsl:attribute>Parameter Name</label>
		<select class="jobParameter">
		<xsl:attribute name="id">param_<xsl:value-of select="item/@id"/></xsl:attribute>
		<option value="">---</option>
		<xsl:for-each select="fields/*">
			<option>
				<xsl:attribute name="value"><xsl:value-of select="@name"/></xsl:attribute>
				<xsl:value-of select="@caption"/>
			</option>
		</xsl:for-each>
		</select>
		<textarea id="param_value"/>
		<xsl:for-each select="item/*">
		<input type="hidden">
			<xsl:attribute name="class">ifyInvisible</xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="name()"/></xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="name()"/></xsl:attribute>
			<xsl:attribute name="value"><xsl:value-of disable-output-escaping="yes" select="."/></xsl:attribute>
		</input>
		</xsl:for-each>	
		<xsl:apply-templates select="operations"/>
	</form>		
	</div>

<!--	<script type="text/javascript">-->
<!--		$(document).ready(function() {	-->
<!--			$("select.jobParameter").change(function(){-->
<!--				alert($(this).val());-->
<!--				var newVal = $(this).parent().find("#"+$(this).val()).val();-->
<!--				$(this).parent().find("textarea").html($(this).val());-->
<!--			})-->
<!--		})-->
<!--	</script>-->
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
				<xsl:apply-templates select="../singleItem[@entity='JobParameterSet']" mode="draw"/>
			</div>					
	</div>
</xsl:template>


</xsl:stylesheet>