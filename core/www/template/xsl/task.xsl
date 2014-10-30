<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/
 Name:          task.xsl
 Version:       0.1 
 Description:   Generic template for task lists and task display 

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="2.0" xmlns="http://www.w3.org/1999/xhtml"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:os="http://a9.com/-/spec/opensearch/1.1/"
	xmlns:str="http://exslt.org/strings" xmlns:ify="http://www.terradue.com/ify"  xmlns:dclite4g="http://dclite4g.xmlns.com/"
	xmlns:msxsl="urn:schemas-microsoft-com:xslt">

<xsl:import href='elements.xsl'/>

	<xsl:include href='./job.xsl' />
	
	<!--
	we need get the parameters to define how to read the result xml, the widgets, table collumns 
	be default we use the one defined in the result configuration /config/results.xml
	and then we check if the service overrides any of those parameters
	-->

	<xsl:variable name="resultParameters">
		<xsl:call-template name="inherit">
			<xsl:with-param name="value" select="$servicexml/const[@name='results']"/>
			<xsl:with-param name="default" select="$results"/>
		</xsl:call-template>
	</xsl:variable>

	
	<xsl:template match="content[itemList/@entity='Task']" mode="head">
		<xsl:apply-imports/>
		<script src="/js/cal.js">{}</script>
		<script src="/js/dates.js">{}</script>
		<script src="/js/paging.js">{}</script>
	</xsl:template>

	<xsl:template match="content[singleItem/@entity='Task']" mode="head">
		<xsl:apply-imports/>
		<script src="/js/tasks.js">{}</script>
		<script src="/js/jquery.progressbar.min.js">{}</script>
		<xsl:apply-templates select="task/results" mode="head"/>
	</xsl:template>

	
	
<!-- templates to define the map on the results page --> 
	<xsl:template match="element[@type='map']" mode="head">
			<script src="/js/OpenLayers.js">{}</script>
			<script src="/js/services/classic/map.js">{}</script>
			<script src="/js/geoselect.js">{}</script>
	</xsl:template>
	<xsl:template match="element[@type='map']" mode="widget">
			<div id='mapPanel' >
				<div id='paneldiv' class='olControlPanel'>_</div>
				<div id='map' class='ifyOSMap' >map ... This page needs javascript</div>
			</div>
			<xsl:apply-imports/>
	</xsl:template>
	<xsl:template match="element[@type='map']" mode="javascript">
			$('#map').html('');
			$('#paneldiv').html('');
			var extent = new OpenLayers.Bounds(-180, -90, 180, 90);
			OSMap = new OpenLayers.Map('ifyServiceMap', 
				<xsl:value-of select="msxsl:node-set($resultParameters)/mapOptions" disable-output-escaping="yes"/>
			);
			OSMap.addLayers(<xsl:value-of select="msxsl:node-set($resultParameters)/wmsLayers" disable-output-escaping="yes"/>);
			OSMap.render("map");
			SetMapControls(OSMap.layers.length, OSMap);
	</xsl:template>

<!-- templates to define the table with the results list --> 
	<xsl:template match="element[@type='table']" mode="head">
			<script src="/js/table/jquery.dataTables.min.js">{}</script>
			<script src="/js/services/classic/dataset.js">{}</script>
			<script src="/js/services/table/dataset.js">{}</script>
			<script src="/js/services/classic/service.js">{}</script>
			<link rel="stylesheet" type="text/css" href="/template/css/services/classic.css" />
			<link rel="stylesheet" type="text/css" href="/template/css/services/maxfilelist.css" />
			<link rel="stylesheet" type="text/css" href="/js/table/css/classic.css" />
	</xsl:template>
	
	<xsl:template match="element[@type='table']" mode="widget">
			<select multiple='multiple' id='dataset' class='dataset'><xsl:text> </xsl:text></select>
			<div id='metadata'>
				<table id="table" class="display table_dataset">
				<thead><tr><th class="no_sort">id</th><th class="no_sort">_</th><xsl:for-each select="msxsl:node-set($resultParameters)/metadata/*[concat('',@hidden)!='true']"><th><xsl:value-of select="."/></th></xsl:for-each></tr></thead>
				<tbody></tbody>
				</table>
			</div>
	</xsl:template>
	
<!-- the trigger to the results is the results element --> 
	<xsl:template match="results" mode="head">
		<xsl:apply-imports/>
		<xsl:apply-templates select="msxsl:node-set($resultParameters)/widgets" mode="head"/>
	</xsl:template>
	<xsl:template match="results" mode="javascript">
			<xsl:apply-templates select="msxsl:node-set($resultParameters)/widgets/*" mode="javascript"/>
			
			<xsl:variable name="templateType" select="@type"/>
			$("#dataset")<xsl:for-each select="msxsl:node-set($resultParameters)/catalogue[@type=$templateType]/*[@name!='identifier']"><xsl:variable name="typeName" select="@name"/>
				.data("catalogue:<xsl:value-of select='@name'/>","<xsl:value-of select="."/>")</xsl:for-each>
				.data("catalogue:identifier","dc:identifier")
				.data("metadataDef", new Array(<xsl:for-each select="msxsl:node-set($resultParameters)/metadata/element">
					new Array("<xsl:value-of select="@value"/>","<xsl:value-of select="."/>","<xsl:value-of select="@name"/>","<xsl:value-of select="@hidden"/>","<xsl:value-of select="@inherited"/>","<xsl:value-of select="@required"/>")<xsl:if test="position() &lt; last()">,</xsl:if></xsl:for-each>))
				.data("series","#series")
				.data("template","<xsl:value-of select="$templateType"/>");

			<xsl:if test="count(msxsl:node-set($resultParameters)/metadata/element[@hidden='true'])!=0">
			$("#table").addClass("ifyMoreInfo");
			</xsl:if>
			ExecuteUrlQuery("<xsl:value-of select='@link'/>",$("#dataset"), null, true);
			
			<xsl:value-of select='msxsl:node-set($resultParameters)/javascript'/>
	</xsl:template>
	
	<xsl:template match="results">
		<xsl:apply-templates select="msxsl:node-set($resultParameters)/widgets/*" mode="widget"/>
	</xsl:template>
	
<!-- template to draw the task flow --> 
	<xsl:template match="flow">
		<xsl:param name="name" select="'flow'" />
		<img>
			<xsl:attribute name="id"><xsl:value-of select="$name" /></xsl:attribute>
			<xsl:attribute name="src"><xsl:value-of select="@link" /></xsl:attribute>
		</img>
	</xsl:template>

	<!-- <xsl:template match="field[@type='ify:status']" > </xsl:template> 

	<xsl:template match="field[@type='ify:hostname']">
	</xsl:template>
-->
	<!-- <xsl:template match="field[@type='ify:sort']" > </xsl:template> -->

<!-- template to draw the task flow with the svg type --> 
	<xsl:template match="flow[@type='image/svg+xml']">
		<xsl:param name="name" select="'flow'" />
		<object>
			<xsl:attribute name="id"><xsl:value-of select="$name" /></xsl:attribute>
			<xsl:attribute name="data"><xsl:value-of select="@link" /></xsl:attribute>
			<param name="tmp" value="1" />
		</object>
	</xsl:template>



	<xsl:template match="jobs">
		<input id="displayJobs" type="button" value="Jobs Information" />
		<div class="jobsInfo">
			<h3>Jobs Information</h3>
			<xsl:apply-templates select="job" />
			<script type="text/javascript">
				$(document).ready(function(){
				updateTaskProgress();
				});
		</script>
		</div>
	</xsl:template>

	<xsl:template match="task" mode="draw">
		<xsl:if test='status!=40'>
			<div class="taskFlow"><xsl:apply-templates select="flow" /></div>
		</xsl:if>
		<!-- <div class="taskUid"><label>Uid:</label> <xsl:value-of select="@uid"/></div> -->
		<!-- <div class="taskCaption"><label>Caption:</label> <xsl:value-of select="caption"/></div> -->
		<div class="taskInfoHead">
		<div class="taskUid">
					<label>Task ID:</label>
					<xsl:value-of select="@uid" />
				</div>
		
		<div class="taskServiceName">
			<label>Service:</label>
			<a><xsl:attribute name="href"><xsl:value-of select="service/@link" /></xsl:attribute><xsl:value-of select="service" /></a>
		</div>
		<xsl:variable name="status" select="status" />
		<div class="taskStatus">
			<label>Status:</label>
			<xsl:value-of
				select="$opensearch/items[@type='ify:status']/*[@value = $status]" />
			(
			<a>
				<xsl:attribute name="href"><xsl:value-of
					select="concat('?uid=',@uid)" /></xsl:attribute>
				refresh
			</a>
			)
		</div>
		<div class="taskResources">
			<label>Cost:</label>
			<xsl:value-of select="cost" />
		</div>
		<div class="taskProgress">
			<label>Progress:</label>
			<span class="progressBarTask" id="pbtask"></span>
		</div>
		<!-- <div class="taskGridSessionId"><label>Grid Session ID:</label> <xsl:value-of 
			select="gridSessionId"/></div> -->
		<div class="taskCreationTime">
			<label>Creation Time:</label>
			<xsl:value-of select="creationTime/@value" />
		</div>
		<div class="taskStartTime">
			<label>Submission Time:</label>
			<xsl:value-of select="startTime/@value" />
		</div>
		<div class="taskEndTime">
			<label>Completion Time:</label>
			<xsl:value-of select="endTime/@value" />
		</div>
		<xsl:if test='status>10'>
				<div class="lgeSessionID">
					<label>Processing ID:</label>
					<xsl:value-of select="sessionId" />
				</div>
				<div class="CE">
					<label>CE:</label>
					<xsl:value-of select="computingResource" />
				</div>
		</xsl:if>
		</div>
		<xsl:apply-templates select="results"/>
		<script type="text/javascript">
			$(document).ready(function() {
			<xsl:apply-templates select="results" mode="javascript" />
			})
		</script>

	</xsl:template>

	<xsl:template match="field" mode="header">
		<th>
			<xsl:attribute name="id">ifyHeader_<xsl:value-of
				select="@name" /></xsl:attribute>
			<xsl:value-of select="@caption" />
		</th>
	</xsl:template>

	<xsl:template match="task"></xsl:template>

	<xsl:template match="singleItem">
		<xsl:variable name="uid" select="item/@uid"/>
		<div class="siteNavigation">
			<a href="/tasks/">Tasks </a>
			/ 
			<xsl:value-of select="../task[@uid=$uid]/caption" />
		</div>
		<h2>
			<xsl:value-of select="../task[@uid=$uid]/caption" />
		</h2>
		<div class="taskInfo">
			<form action="" method="get" id="myform" class='_dataset _dataset_table'>
			<!--	
				<input type="hidden" class="ifyHiddenIdElement" name="uid">
					<xsl:attribute name="value"><xsl:value-of
						select="$uid" /></xsl:attribute>
				</input>
			-->
				<xsl:apply-templates select="../task[@uid=$uid]"
					mode="draw" />
				<div class="taskOps">
				<h3>Task Operations</h3>
				<xsl:for-each select="fields/field[@name!='user']">
					<xsl:variable name="name" select="@name" />
					<xsl:apply-templates select=".">
						<xsl:with-param name="value" select="../../item/*[name()=$name]" />
					</xsl:apply-templates>
				</xsl:for-each><br/>
				<xsl:apply-templates select="operations" />
				</div>

			</form>
			<xsl:apply-templates select="../task[@uid=$uid]/jobs" />
			<br />
			
		</div>

		<script type="text/javascript">
			$(document).ready(function() {
			<xsl:apply-templates select="fields" mode="javascript" />
			<xsl:apply-templates select="operations" mode="javascript" />
						
			})
		</script>
	</xsl:template>

	<xsl:template match="field[@type='ify:status']">
		<!-- <xsl:template name="ifystatus"> -->
		<xsl:param name="element" select="." />
		<xsl:variable name="status_query"
			select="concat($element/os:Query/@ify:status,'')" />
		<div id="task_status_tab">
			<ul class="ifyShadetabs">
				<xsl:for-each select="$opensearch/items[@type='ify:status']/*">
					<li>
						<xsl:attribute name="class">ifyShadeTab <xsl:if
							test="$status_query = @value">ifyShadeTabSelected</xsl:if></xsl:attribute>
						<a>
							<xsl:attribute name="href">?status=<xsl:value-of
								select="@value" /></xsl:attribute>
							<xsl:value-of select="." />
						</a>
					</li>
				</xsl:for-each>
			</ul>
			<!-- this was removed from here because now the ify:status appear on the 
				more (fabrice's request) ... i put this again because of the tab with all 
				the status -->
			<input type="hidden">
				<xsl:attribute name="value"><xsl:value-of select="$status_query" /></xsl:attribute>
				<xsl:attribute name="name"><xsl:value-of select="@name" /></xsl:attribute>
			</input>

		</div>
</xsl:template>

<xsl:template match="item/startTime | item/endTime | item/creationTime" mode="table">
	<td>
		<xsl:value-of select="substring(.,1,10)" />&#xA0;<xsl:value-of select="substring(.,12)" />
	</td>
</xsl:template>
								

<xsl:template match="itemList">
		<div class="page-list">
			<div class="page-list-title">
				<xsl:value-of select="@entity" />
			</div>
			<div id="element">
				<xsl:apply-templates select="link">
					<xsl:with-param name="mainSearchElements"
						select="'ify:status,searchTerms,count,ify:sort'" />
					<xsl:with-param name="unusedSearchElements" select="'[{searchTerms},{role},{count},{startIndex},{startPage},{ify:sort},{ify:enabled},{ify:accessTime}]'"/>
				</xsl:apply-templates>
				<xsl:if test="os:totalResults!=0">
				<form action="" method="get" id="ifySelectPagingControl">
					<xsl:apply-templates select="." mode="table"/>
					<input type="hidden" class="ifyHiddenIdElement" name="id"
						id="ifySelectPagingControlIds" />
					<xsl:apply-templates select="operations" />
				</form>
				</xsl:if>
			</div>
		</div>
		<script type="text/javascript">
			$(document).ready(function() {
			<xsl:apply-templates select="fields" mode="javascript" />
			<xsl:apply-templates select="operations" mode="javascript" />

			})
		</script>
	</xsl:template>
</xsl:stylesheet>
