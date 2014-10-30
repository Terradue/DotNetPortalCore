<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/
 Name:          search.xsl
 Version:       0.1 
 Description:   Generic GUI element for search support in all pages 

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

<xsl:template match="field" mode="main_search"/>


<xsl:template match="field[@type='searchTerms']" mode="main_search">
	<xsl:apply-templates select="."/>
</xsl:template>

<xsl:template match="field[@type='count']" mode="main_search">
	<xsl:apply-templates select="."/>
</xsl:template>

<xsl:template match="field[@type='ify:sort']" mode="main_search">
	<xsl:apply-templates select="."/>
</xsl:template>

<xsl:template match="link[@rel='search' and @type='application/opensearchdescription+xml']">
	<xsl:param name="unusedSearchElements" select="'[{searchTerms},{role},{count},{startIndex},{startPage},{ify:sort},{ify:enabled},{ify:accessTime}]'"/>
	<xsl:param name="mainSearchElements" select="'ify:status,searchTerms,count,ify:sort'"/>

	<xsl:variable name="mainSearchElementsArr"><xsl:call-template name="str:split">
		<xsl:with-param name="string" select="$mainSearchElements" />
		<xsl:with-param name="pattern" select="','" />
	</xsl:call-template></xsl:variable>	
	<xsl:variable name="url" select="../os:Url[@type='text/html']/@template"/>
	<xsl:variable name="server" select="substring-before($url,'?')"/>
	<xsl:variable name="template" select="substring-after($url,'?')"/>
	
	<xsl:variable name="urlArr_">		
			<xsl:call-template name="str:split">
				<xsl:with-param name="string" select="$template" />
				<xsl:with-param name="pattern" select="'&amp;'" />
			</xsl:call-template>
	</xsl:variable>	
	<xsl:variable name="urlArr" select="msxsl:node-set($urlArr_)"/>
<!--	<xsl:copy-of select="$urlArr"/> -->
	<xsl:variable name="osQuery" select="../os:Query[@role='request']"/>
	<xsl:variable name="osQueryCount" select="../os:itemsPerPage"/>


	<!-- this variable is to see if the extra search should be visible -->
	<xsl:variable name="OSrequest"><xsl:for-each select="../os:Query[@role='request']/@*[substring-after(concat($mainSearchElements,$unusedSearchElements),name()) = '']"><xsl:value-of select="."/></xsl:for-each></xsl:variable>
	
	<!-- this variable is to have the url link without the paging and sorting 
		that will then be replaced by the paging widget -->
	<xsl:variable name="originalRequest">
		<xsl:for-each select="$urlArr/*"><xsl:variable name="type" select="@type"/><xsl:choose><xsl:when test=".!=''"><xsl:value-of select="."/></xsl:when><xsl:otherwise><xsl:value-of select="@name"/>=<xsl:choose><xsl:when test="$type='count'"><xsl:value-of select="$osQueryCount"/></xsl:when><xsl:when test="$type='startPage'">##PAGE##</xsl:when><xsl:when test="$type='ify:sort'">##SORT##</xsl:when><xsl:otherwise>
			<xsl:value-of select="$osQuery/@*[name()=$type]"/></xsl:otherwise></xsl:choose></xsl:otherwise></xsl:choose>&amp;</xsl:for-each>
	</xsl:variable>

	<div id="ifySearchBox">
		<!--<span class="ifyTextButton" style='float:right' onclick="$('#ifySearchForm').toggle()"> search options</span>-->
		<div id="ifySearchFormDiv">
		<form action="" method="get" id="ifySearchForm">
			<!--<xsl:if test="concat($OSrequest,../os:Query[@role='request']/@searchTerms,'')=''">
				<xsl:attribute name="style">display:none</xsl:attribute>
			</xsl:if>-->
			<div class="ifyDefaultSearch"> 
				<input type="hidden" id="ifySearchOriginalRequest">
					<xsl:attribute name="value"><xsl:value-of select="$originalRequest"/></xsl:attribute>
				</input>
				<xsl:variable name="this" select="." />
				<xsl:for-each select="msxsl:node-set($mainSearchElementsArr)/token">
					<xsl:variable name="elName" select="."/>
					<xsl:apply-templates select="$urlArr/*[@type=$elName]">
						<xsl:with-param select="$this/../../itemList" name="element"/>
						<xsl:with-param select="$this/../os:Query[@role='request']/@*[name()=$elName]" name="value"/>
					</xsl:apply-templates>
				</xsl:for-each>
			</div>
			<div id="_searchButton">
			<input type="submit" value="Search"/>
			</div>

			<!-- This variable is to identify the queriables of the secondary search 
				ATTENTION: this is different from OSrequest because we want to have 
				them visible be they used or not on the query	-->
			<xsl:variable name="extraSearch" select="$urlArr/*[substring-after(concat($mainSearchElementsArr,$unusedSearchElements),@type) = ''] "/>
			 
			<xsl:if test="count($extraSearch) &gt; 0">
				<br/>
				<span id="ifyExtendedSearchButton" class='ifyTextButton' style='cursor:pointer'>
				<xsl:choose><xsl:when test="$OSrequest!=''">less..</xsl:when><xsl:otherwise>more...</xsl:otherwise></xsl:choose></span>
				<div id="ifyExtendedSearch">					
					<xsl:if test="$OSrequest!=''">
						<xsl:attribute name="style">display:block</xsl:attribute>
					</xsl:if>
					<xsl:variable name="this" select="." />
					<xsl:for-each select="$extraSearch">
						<xsl:variable name="type" select="@type"/>
						<xsl:apply-templates select=".">
							<xsl:with-param select="$this/../../itemList" name="element"/>
							<xsl:with-param select="$this/../os:Query[@role='request']/@*[name()=$type]" name="value"/>
							<xsl:with-param name="originalRequest" select="$originalRequest"/>
						</xsl:apply-templates>
					</xsl:for-each>
				</div>
			</xsl:if>
			<xsl:for-each select="$urlArr/token">
				<input type='hidden'>
					<xsl:attribute name="name"><xsl:value-of select="substring-before(.,'=')"/></xsl:attribute>
					<xsl:attribute name="value"><xsl:value-of select="substring-after(.,'=')"/></xsl:attribute>
				</input>
			</xsl:for-each>
		</form>
		</div>
		<div id="ifySearchPagingBox">
			<xsl:apply-templates select="$urlArr/*[@type='startPage']">
				<xsl:with-param select="../../itemList" name="element"/>
			</xsl:apply-templates>
			
		</div>
	</div>

</xsl:template>
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=COUNT-->
<xsl:template match="field[@type='count']"  mode="draw">
	<xsl:param name="element" select="."/>
	<xsl:variable select="@type" name="type"/>
	<xsl:variable name="value" select="$element/*[name()=$opensearch/items[@type=$type]/@value]"/>		
		<xsl:call-template name="inputSelect">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="values" select="$opensearch/items[@type=$type]/*"/>
			<xsl:with-param name="selected" select="$value"/>
		</xsl:call-template>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=STARTINDEX -->
<xsl:template match="field[@type='startIndex']" >
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=STARTPAGE -->
<xsl:template match="field[@type='startPage']" >
 	<xsl:param name="element" select="."/>
<!-- 	<xsl:param name="value">
		<xsl:choose>
			<xsl:when test="concat($element/os:startPage,'')=''">
				<xsl:value-of select="($element/os:startIndex)" />
			</xsl:when>
			<xsl:otherwise>

			</xsl:otherwise>
		</xsl:choose>
	</xsl:param> -->
	<xsl:choose>
	<xsl:when test='$element/os:itemsPerPage &lt; $element/os:totalResults'>
		<div id='ifySelectPagingControlLabel'>Showing <xsl:value-of select="count($element/items/item)"/> of the <xsl:value-of select="$element/os:totalResults"/> results found <span id='ifySelectPagingControlSelectLabel'/></div>
		
		<div id='ifySelectPagingControlNav'>
			 <xsl:call-template name="Pages">
				<xsl:with-param name="count" select="$element/os:totalResults div $element/os:itemsPerPage"/>
				<xsl:with-param name="page" select="$element/os:startIndex div $element/os:itemsPerPage + 1"/>
			</xsl:call-template>
		</div>
	</xsl:when>
	<xsl:when test="$element/os:totalResults='0'">
		No items found 
	</xsl:when>
	<xsl:otherwise>
		<div id='ifySelectPagingControlLabel'>Showing the <xsl:value-of select="count($element/items/item)"/> results found.</div>
	</xsl:otherwise> 
</xsl:choose>

	<!-- <xsl:call-template name="nav"/> -->
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=IFY:SORT-->
<xsl:template match="field[@type='ify:sort']">
	<xsl:param name="originalRequest" select="''"/>
	<xsl:param name="value" select="."/>

	<input type="hidden" value="" id="ifySelectPagingControlSortField">
		<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
	</input>
	<script type="text/javascript">$(document).ready(function() { ifySelectPagingControlBindSort()})</script>
</xsl:template>

<xsl:template match="field[@type='ify:sort']" mode="javascript">
	<!-- TO-DO javascrip should be placed here -->
</xsl:template>
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=IFY:SERVICE-->
<xsl:template match="field[@type='ify:service']" mode="draw">
	<xsl:param name="element" select="."/>
	<xsl:param name="value" select="''"/>
	<xsl:call-template name="inputSelect">
		<xsl:with-param name="name" select="@name"/>
		<xsl:with-param name="values" select="$element/fields/field[@name='service']/*"/>
		<xsl:with-param name="addempty" select="true()"/>
		<xsl:with-param name="selected" select="$value"/>
	</xsl:call-template>
</xsl:template>
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=IFY:USER -->
<xsl:template match="field[@type='ify:user']" mode="draw">
	<xsl:param name="element" select="."/>
	<xsl:param name="value" select="''"/>
	<xsl:call-template name="inputSelect">
		<xsl:with-param name="name" select="@name"/>
		<xsl:with-param name="values" select="$element/fields/field[@name='owner']/*"/>
		<xsl:with-param name="addempty" select="true()"/>
		<xsl:with-param name="selected" select="$value"/>
	</xsl:call-template>
</xsl:template>
<!-- ###################################################################################################################### -->


<!-- ################################################################################################  FIELD TYPE=IFY:CE -->
<!-- <xsl:template match="field[@type='ify:ce']"/> -->
<xsl:template match="field[@type='ify:ce']" mode="draw">
	<xsl:param name="element" select="."/>
	<xsl:param name="value" select="''"/>
	<xsl:call-template name="inputSelect">
		<xsl:with-param name="name" select="@name"/>
		<xsl:with-param name="values" select="$element/fields/field[@name='ce']/*"/>
		<xsl:with-param name="addempty" select="true()"/>
		<xsl:with-param name="selected" select="$value"/>
	</xsl:call-template>
</xsl:template>

<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=IFY:CREATION -->
<!--################################################################################################  FIELD TYPE=IFY:SUBMISSION -->
<!--################################################################################################  FIELD TYPE=IFY:COMPLETION -->
<xsl:template match="field[@type='ify:creation' or @type='ify:submission' or @type='ify:completion']" >
	<xsl:param name="element" select="."/>
	<xsl:param name="value" select="'2'"/>
	<xsl:variable name="type" select="@type"/>
	<xsl:variable name="value2" select="$element/os:Query[@role='request']/@*[name()=$type]"/>
		<!-- value: '<xsl:value-of select="$value"/>'<br/> -->
	<div class="ifyDateRangeDiv">
		<xsl:attribute name="id">_<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:apply-templates select="." mode="label"/>
		<input type='text'>
				<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
				<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
				<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
				<xsl:attribute name="class"><xsl:value-of select="'ifyDateRange'"/></xsl:attribute>
			</input>
		<xsl:call-template name="inputSelect">
			<xsl:with-param name="id" select="''"/>
			<xsl:with-param name="class" select="'ifyDateRangeList'"/>
			<xsl:with-param name="values" select="$config/dates/*"/>
			<xsl:with-param name="selected" select="$value"/>
		</xsl:call-template>
		<span class='ifyDateRangeListFromTo'> 
			From 
			<input type='text' class='ifyDateRangeListFrom'>
				<xsl:attribute name="value"><xsl:value-of select="substring-before($value,'/')"/></xsl:attribute>
			</input>
			to 
			<input type='text' class='ifyDateRangeListTo'>
				<xsl:attribute name="value"><xsl:value-of select="substring-after($value,'/')"/></xsl:attribute>
			</input>
		</span>
	</div>
</xsl:template> 
<!-- ###################################################################################################################### -->

</xsl:stylesheet>