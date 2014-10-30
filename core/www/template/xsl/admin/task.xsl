<?xml version="1.0" encoding="UTF-8"?>
<!--
 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->

<!-- Generic template for task lists and task display -->

<xsl:stylesheet version="2.0" xmlns="http://www.w3.org/1999/xhtml"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:os="http://a9.com/-/spec/opensearch/1.1/"
	xmlns:str="http://exslt.org/strings" xmlns:ify="http://www.terradue.com/ify"
	xmlns:msxsl="urn:schemas-microsoft-com:xslt">

<xsl:import href="../task.xsl"/>

	<xsl:template match="content[itemList/@entity='Task']" mode="head">
		<xsl:apply-imports/>
		<link rel="stylesheet" type="text/css" href="/template/css/admin/task.css" />
	</xsl:template>


</xsl:stylesheet>
