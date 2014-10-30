<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/services
 Name:          classic.xsl
 Version:       0.1 
 Description:   Generic template for classic service style like the g-pod 1.0 

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
	 xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	 xmlns:ify="http://www.terradue.com/ify"
	 >
<xsl:import href='../elements.xsl'/>

<xsl:include href="const.xsl"/>
<xsl:include href="field.xsl"/>


<xsl:template match="content" mode="head">
	<xsl:apply-imports/>
	<xsl:apply-templates select="//singleItem/fields/const" mode="head"/>
	<script src="/js/services/classic/service.js">{}</script>
	<script src="/js/services/classic/dataset.js">{}</script>
	<link rel="stylesheet" type="text/css" href="/template/css/services/classic.css"/>
	<xsl:apply-templates select="//singleItem/fields" mode="head"/>

	<script type="text/javascript">$(document).ready(function() {pageInit()})</script>

</xsl:template>



<xsl:template match="caption">
	<div id="title" class="title"> <xsl:value-of select="."/> </div>
</xsl:template>

<xsl:template match="descr">
	<div id="description" class="description"> <xsl:value-of select="."/> </div>
</xsl:template>

<xsl:template match="fields" mode="AdditionalParameters">
	<div id='additionalParameters'>	
		<h2>Additional Parameters</h2>
		<xsl:for-each select="field[concat(@source,'')='' and not(@ext!='') and @type!='caption' and @type!='startdate' and @type!='map']">
			<xsl:variable name="name" select="@name"/>
			<xsl:apply-templates select=".">
				<xsl:with-param name="value" select="../../item/*[name()=$name]"/>		
			</xsl:apply-templates>
		</xsl:for-each>
	</div>

</xsl:template>

<xsl:template match="fields">
	<!--
	<xsl:apply-templates select="field[@type='map']">
		<xsl:with-param name="value" select="../item/bbox"/>
	</xsl:apply-templates>
	<xsl:apply-templates select="field[@type='startdate']"/>
	-->
	<xsl:apply-templates select="const"/>	
	
	<xsl:apply-templates select="field[@ext!='']">
		<xsl:sort select="@order"/>		
	</xsl:apply-templates>
	
	<!--
	<xsl:for-each select="field[@ext!='']">
		<xsl:variable name="name" select="@name"/>
		<xsl:value-of select="$name"/> : 
		<xsl:value-of select="../../item/*[name()=$name]"/>
		<br/> code red
		<xsl:apply-templates select=".">
			<xsl:with-param name="value" select="../../item/*[name()=$name]"/>
		</xsl:apply-templates>
	</xsl:for-each>
	--> 
	
	<xsl:apply-templates select="field[@source='series']"/>

	<xsl:apply-templates select="field[@source='dataset']"/>
	<xsl:if test="../@entity='Scheduler'">
	<div id="ifyScheduler">
	<h2>Scheduler</h2>
	<xsl:apply-templates select="field[@source='scheduler']"/>
	</div>
	</xsl:if>
	<div id='mainParameters'>
		<h2>Parameters</h2>
		<xsl:variable name="caption" select="field[@type='caption' and concat('',@source)!='scheduler']/@name"/>
		<xsl:variable name="publishServer" select="field[@source='publishServer']/@name"/>
		<xsl:variable name="compress" select="field[@source='compress']/@name"/>
		<xsl:variable name="computingResource" select="field[@source='computingResource']/@name"/>
		<xsl:variable name="priority" select="field[@source='priority']/@name"/>
		<xsl:apply-templates select="field[@type='caption' and concat('',@source)!='scheduler']">
			<xsl:with-param name="value" select="../item/*[name()=$caption]"/>		
		</xsl:apply-templates>
		
		<xsl:apply-templates select="field[@source='publishServer']">
			<xsl:with-param name="value" select="../item/*[name()=$publishServer]"/>		
		</xsl:apply-templates>
		
		<xsl:apply-templates select="field[@source='compress']">
			<xsl:with-param name="value" select="../item/*[name()=$compress]"/>		
		</xsl:apply-templates>
		<xsl:apply-templates select="field[@source='computingResource']">
			<xsl:with-param name="value" select="../item/*[name()=$computingResource]"/>		
		</xsl:apply-templates>
		<xsl:apply-templates select="field[@source='priority']">
			<xsl:with-param name="value" select="../item/*[name()=$priority]"/>		
		</xsl:apply-templates>
		
	</div>	
	<xsl:apply-templates select="." mode="AdditionalParameters"/>
</xsl:template>


<!--<xsl:template match="operation" mode="javascript">-->
<!--	<xsl:variable name="myid" select="concat(name(../../.),translate(../../@entity,' ',''),'_',@name)"/>-->
<!--	$("#<xsl:value-of select="$myid"/>").click(function(){-->
<!--		$(this).trigger("submit","<xsl:value-of disable-output-escaping="yes" select="@link"/>");-->
<!--	});-->
<!--</xsl:template>-->

<xsl:template match="service">	
	<xsl:apply-templates select="caption"/>
<!-- 	<xsl:apply-templates select="descr"/>
 -->	
 </xsl:template>

 <xsl:template match="singleItem">	
	<xsl:variable name="serviceid"><xsl:value-of select="../service/@id"/></xsl:variable>
	<div class="service">
		<xsl:variable name="url"><xsl:value-of select="url"/><xsl:if test="contains(url,'?')=false()">?</xsl:if></xsl:variable>
		
		<form method="post">
			<xsl:attribute name="id"><xsl:value-of select="@entity"/></xsl:attribute>
			<xsl:attribute name="action"><xsl:value-of select="url"/></xsl:attribute>
			<xsl:apply-templates select="fields"/>
			<div>
				<xsl:attribute name="id"><xsl:value-of select="@entity"/>_buttons</xsl:attribute>
				<xsl:attribute name="class">serviceCreate</xsl:attribute>
				<span class="serviceFieldValidity">
					<xsl:attribute name="id"><xsl:value-of select="@entity"/>_status</xsl:attribute>
					<xsl:text> _ </xsl:text>
				</span>
				<input type="hidden" id="ifyServiceName">
					<xsl:attribute name="value"><xsl:value-of select="@entity"/></xsl:attribute>
				</input>
				<input type="hidden" id="ifyServiceUrl">
					<xsl:attribute name="value"><xsl:value-of select="$url"/></xsl:attribute>
				</input>
				<xsl:apply-templates select="operations"/>
			</div>
		</form>
		
		<script type="text/javascript">
	//$(document).ready(function() {
	function pageInit(){
		// link the html form elements to their opensearch extensions 
		<xsl:for-each select="fields/*[concat('',@ext)!='']">
		$("#<xsl:value-of select='@name'/>").addClass("<xsl:value-of select='@ext'/>".replace(':','_'));
		$("#<xsl:value-of select='@name'/>").data("ext", "<xsl:value-of select='@ext'/>");</xsl:for-each>
		<xsl:apply-templates select="fields/const" mode="javascript"/>
		<xsl:for-each select="fields/*[@scope!='']">		
		$("#<xsl:value-of select="concat(@name,'_configurable')"/>").data("configures", "<xsl:value-of select='@name'/>");
		$("#<xsl:value-of select="concat(@name,'_configurable')"/>").data("serviceid", "<xsl:value-of select='$serviceid'/>");
		<xsl:if test="concat(@range,'')='closed' or concat(@range,'')='' ">$("#<xsl:value-of select="@name"/>").attr('disabled', 'disabled');</xsl:if></xsl:for-each>
		
		<xsl:for-each select="fields/*[concat('',@optional)='true']">
		$("#<xsl:value-of select='@name'/>").addClass("ifyOptional");</xsl:for-each>
		<xsl:apply-templates select="fields/field" mode="javascript"/>
		<xsl:apply-templates select="operations" mode="javascript"/>
		<xsl:if test="@entity='Scheduler'">
		$(".time_start, .time_end").unbind("change",TimeValidityFunction);
		$('#datePanel div').unbind();
		if ($(".time_start").val() == "" ) $(".time_start").val("$(EXECDATE)-10D").change();
		if ($(".time_end").val() == "" )$(".time_end").val("$(EXECDATE)-1D").change();
		</xsl:if>


	}
		</script>
	</div>
</xsl:template>


</xsl:stylesheet>
