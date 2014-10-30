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
	<script src="/js/jquery.progressbar.min.js">{}</script>
	<script src="/js/cal.js">{}</script>
	<script src="/js/tasks.js">{}</script>
	<script src="/js/dates.js">{}</script>
	<script src="/js/paging.js">{}</script>
</xsl:template>

<xsl:template match="content" mode="head">
	<script src="/js/jquery.progressbar.min.js">{}</script>
	<script src="/js/tasks.js">{}</script>
</xsl:template>

<xsl:template match="job">
	<xsl:variable name="status" select="status"/>
	<div class="jobInfo">
		<xsl:attribute name="id"><xsl:value-of select="name"/></xsl:attribute>
		<input type="hidden">
			<xsl:attribute name="value"><xsl:value-of select="@link"/></xsl:attribute>
		</input>
		<div class="jobName"><xsl:value-of select="name"/></div>
		<div class="jobTab">
			<div >
			<ul class="ifyShadetabs">
				<li class="ifyShadeTab ifyShadeTabSelected jobDetails" >
					Details
				</li>
				<xsl:if test="dependencies=''">
				<li class="jobInput ifyShadeTab">Input
				<input type="hidden">
					<xsl:attribute name="value"><xsl:value-of select="parameters/@link"/></xsl:attribute>
				</input></li>
				</xsl:if>
				<li class="jobParameters ifyShadeTab">
				Parameters
				<input type="hidden">
					<xsl:attribute name="value"><xsl:value-of select="parameters/@link"/></xsl:attribute>
				</input>
				</li>
				<li class="jobProcessingInfo ifyShadeTab">
				Processing Nodes
				<input type="hidden">
					<xsl:attribute name="value"><xsl:value-of select="details/@link"/></xsl:attribute>
				</input>
				</li>
			    </ul>

	  		<div class="jobDetailsContent">				
				<div class="jobStatus">
					<label>Status:</label><xsl:value-of select="$opensearch/items[@type='ify:status']/*[@value = $status]"/><br/>
					<label>Progress:</label><xsl:if test="arguments/@total">[<xsl:value-of select="arguments/@done"/>/<xsl:value-of select="arguments/@total"/>]
						</xsl:if>
					<span class="progressBarJob">
						<xsl:attribute name="id">pb<xsl:value-of select="name"/></xsl:attribute>
						0%
					</span><br/>
					<label>Resources:</label>
					<xsl:choose>
						<xsl:when test="status &gt; 10">
							<xsl:value-of select="sum(processingCount)"></xsl:value-of> computing nodes<br/>
						</xsl:when>
						<xsl:otherwise>
							not yet allocated
						</xsl:otherwise>
					</xsl:choose><br/>
					<label>Additional Information:</label>
					<xsl:value-of select="statusMessage"/>
					<xsl:if test="statusMessage/@type='error'">
						<br/><label>Debug Information:</label><xsl:value-of select="debugMessage"/>
					</xsl:if>
					<xsl:if test="arguments/@total">
					<script type="text/javascript">
							<xsl:choose>
								<xsl:when test="status &lt; 40">
							$("#pb<xsl:value-of select="name"/>").progressBar(<xsl:value-of select="(arguments/@done div arguments/@total) * 100"/>,{
									boxImage		: '/js/img/progressbar.gif',
									barImage		: {
										0:  '/js/img/progressbg_red.gif',
										30: '/js/img/progressbg_orange.gif',
										70: '/js/img/progressbg_green.gif'
										}
							});
							$("#pb<xsl:value-of select="name"/>").data('pc',<xsl:value-of select="(arguments/@done div arguments/@total) * 100"/>);
								</xsl:when>
								<xsl:otherwise>
							$("#pb<xsl:value-of select="name"/>").progressBar(100,{
							boxImage		: '/js/img/progressbar.gif',
							barImage		: {
								0:  '/js/img/progressbg_red.gif',
								30: '/js/img/progressbg_orange.gif',
								70: '/js/img/progressbg_green.gif'
								}
							});
							$("#pb<xsl:value-of select="name"/>").data('pc',100);
								</xsl:otherwise>
							</xsl:choose>
					</script>
					</xsl:if>
				</div>
				<div class="jobDetailsIcon">
					<img><xsl:attribute name="src"><xsl:value-of select="$config/icons/status/*[@value = $status]"/></xsl:attribute></img>
				</div>			
			</div>
          </div>
		</div>	
	</div>
</xsl:template>


</xsl:stylesheet>
