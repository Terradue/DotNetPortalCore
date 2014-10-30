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
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/">

<xsl:import href='elements.xsl'/>

<xsl:template match="content" mode="head">
	<xsl:apply-imports/>

	<script type="text/javascript">
		$(document).ready(function(){
		$("form").attr("autocomplete","off");
		$("#username").val("");
		$("#password").val("");
		});		
	</script>

</xsl:template>

<xsl:template match="singleItem">
	<div id="title" class="page-item-title"> <xsl:value-of select="@entity"/> </div>
	<div id="description" class="description">
		<xsl:choose>
			<xsl:when test="item/@id != '' ">Change the <xsl:value-of select="@entity"/> settings here</xsl:when>
			<xsl:otherwise>Create a new <xsl:value-of select="@entity"/> settings here</xsl:otherwise>
		</xsl:choose>

	</div> 
<!--	<br/> 	
	/* Styles for this page  */
	<br/>
	<xsl:for-each select="item/*">
		#_<xsl:value-of select="name(.)"/> {}<br/>
		#<xsl:value-of select="name(.)"/> {}<br/>
	</xsl:for-each>
 -->
	<div id="elements">
		<div style="">
			<form action="" method="get" id="myform" accept-charset="utf-8">
				<xsl:for-each select="fields/field">
					<xsl:variable name="name" select="@name"/>
					<xsl:apply-templates select=".">
						<xsl:with-param name="value" select="../../item/*[name()=$name]"/>
					</xsl:apply-templates>
				</xsl:for-each>
				<input type="hidden" class="ifyHiddenIdElement" name="id"><xsl:attribute name="value"><xsl:value-of select="item/@id"/></xsl:attribute></input>
				<xsl:apply-templates select="operations"/>
				 <p/>
			</form>
	<script type="text/javascript">
	$(document).ready(function() {
		<xsl:apply-templates select="fields" mode="javascript"/>
		<xsl:apply-templates select="operations" mode="javascript"/>

	})
	</script>

		</div>
	</div>
</xsl:template>


<!-- this was to block the display of the username and password
<xsl:template match="field[@name='username']">
	<xsl:if test="count(../../item)=0">
		<xsl:apply-imports/>
	</xsl:if>
</xsl:template>-->

<!--- this was to block the display of the password 
<xsl:template match="field[@name='password']">
	<xsl:if test="count(../../item)=0">
		<xsl:apply-imports/>
	</xsl:if>
</xsl:template>
-->

</xsl:stylesheet>