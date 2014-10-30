<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/
 Name:          operations.xsl
 Version:       0.1 
 Description:   Generic GUI elements to manage the operations available on all pages 

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" 
	 xmlns="http://www.w3.org/1999/xhtml" 
	 xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	 xmlns:os="http://a9.com/-/spec/opensearch/1.1/" 
	 xmlns:msxsl="urn:schemas-microsoft-com:xslt"  
	 xmlns:js="urn:javascript" 
	 xmlns:str="http://exslt.org/strings" 
	 xmlns:ify="http://www.terradue.com/ify"
	 exclude-result-prefixes="msxsl js">


<xsl:template match="operation[@owner!='']" mode="javascript">
	<xsl:variable name="myid" select="concat(name(../../.),translate(../../@entity,' ',''),'_',@name)"/>
	$("#<xsl:value-of select="$myid"/>").data("link","<xsl:value-of select="@link"/>");
	$('#<xsl:value-of select="$myid"/>').click(function(){
		if (($(this).attr("checked")==true) || ($(this).attr("checked")=="checked")){
			$(this).parent().find(":checked").attr("checked","");
			$(this).attr("checked","checked");
		}		
	});
	if (!$('#<xsl:value-of select="$myid"/>').parent().hasClass("ifyOperationWithOption")){$('#<xsl:value-of select="$myid"/>').parent().addClass("ifyOperationWithOption")};
</xsl:template>
<!-- ######################################################################################################################

<xsl:template match="operation[@method='GET' and concat('',@owner)='']" mode="javascript">
	<xsl:variable name="myid" select="concat(name(../../.),translate(../../@entity,' ',''),'_',@name)"/>
	$('#<xsl:value-of select="$myid"/>').click(function(){
		window.location.href='<xsl:value-of disable-output-escaping="yes" select="@link"/>';
	})
</xsl:template>

<xsl:template match="operation[@method='POST']" mode="javascript">
	<xsl:variable name="myid" select="concat(name(../../.),translate(../../@entity,' ',''),'_',@name)"/>
	$("#<xsl:value-of select="$myid"/>").click(function(){
		SubmitOperation (this, '<xsl:value-of disable-output-escaping="yes" select="@link"/>','<xsl:value-of select="@method"/>', '<xsl:value-of select="name(./../..)"/>');
	})
</xsl:template>
 ###################################################################################################################### -->


<xsl:template match="operation[concat(@owner,'')!='']" mode="draw">
	<xsl:variable name="myid" select="concat(name(../../.),translate(../../@entity,' ',''),'_',@name)"/>
	<input type='checkbox'><xsl:attribute name="id"><xsl:value-of select="$myid"/></xsl:attribute><xsl:value-of select="@caption"/></input>
</xsl:template>

<xsl:template match="operation[concat(@owner,'')='']">
	<xsl:variable name="myid" select="concat(name(../../.),translate(../../@entity,' ',''),'_',@name)"/>
	<xsl:variable name="name" select="@name"/>
	<div class="ifyOperation">
		<xsl:attribute name="id">_<xsl:value-of select="$myid"/></xsl:attribute>
	<input type='button'>
		<xsl:attribute name="value"><xsl:value-of select="@caption"/></xsl:attribute>
		<xsl:attribute name="id"><xsl:value-of select="$myid"/></xsl:attribute>
		<xsl:attribute name="class">ify_<xsl:value-of select="name(.)"/> ify_<xsl:value-of select="name(.)"/>_<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="onClick">operationClick(this,'<xsl:value-of disable-output-escaping="yes" select="@link"/>','<xsl:value-of select="@method"/>', '<xsl:value-of select="name(./../..)"/>');</xsl:attribute>
	</input>
	<xsl:apply-templates select="../operation[@owner=$name]" mode="draw"/>	
	</div>
</xsl:template>
<!-- ###################################################################################################################### -->


<xsl:template match="operations">
	<xsl:if test="count(*)!=0">
	<div class="ifyOperations">
		<span class="ifyStatus"> _</span>
		<xsl:apply-templates select="operation"/>
	</div>
	<br clear="all"/>
	</xsl:if>
</xsl:template>

<xsl:template match="operations" mode="javascript">	
	<xsl:apply-templates select="operation" mode="javascript"/>
</xsl:template>
<!-- ###################################################################################################################### -->


</xsl:stylesheet>
