<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/services
 Name:          field.xsl
 Version:       0.1 
 Description:   Generic template for service's fields in the classic service style like the g-pod 1.0

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



<!--################################################################################################  CONFIGURABLE=TRUE -->
<xsl:template match="field[@scope!='']" mode="configurable">
	<div class="ifyConfigurable">
		<xsl:attribute name="id"><xsl:value-of select="concat('_',@name,'_','configurable')"/></xsl:attribute>
		<xsl:attribute name="class"><xsl:value-of select="concat('_',translate(@ext,':','_'),'_','configurable')"/> _ifyConfigurable<xsl:if test="@range='extensible' and @type='textfile'"> _ifyConfigurableFromTextFile</xsl:if></xsl:attribute>
	<select>
		<xsl:attribute name="id"><xsl:value-of select="concat(@name,'_','configurable')"/></xsl:attribute>
		<xsl:attribute name="class"><xsl:value-of select="'ifyConfigurable'"/></xsl:attribute>
		<option value=''><xsl:choose><xsl:when test="@prompt!=''"><xsl:value-of select="@prompt"/></xsl:when><xsl:otherwise>--</xsl:otherwise></xsl:choose> </option>
	</select>
	<xsl:if test="@range='extensible'">
	<input type="text" class="ifyInvisible"/>
	<span class="ifyIcon ifyIcon-save ifyInvisible">+</span>
	<span class="ifyIcon ifyIcon-del">+</span>
	<span class="ifyIcon ifyIcon-add ">+</span>
	</xsl:if>
	</div>
</xsl:template>

<!-- ###################################################################################################################### -->


<!--################################################################################################ FIELD @SOURCE=SERIES -->
<xsl:template match="field[@source='series']">
	<xsl:param name="value" select="."/>
		<xsl:apply-imports/>
 </xsl:template>
<xsl:template match="field[@source='series']" mode="javascript">
		// now we have to check the linkage between the series select list parameters and the opensearch extensions
		<xsl:for-each select="*">
      $("#<xsl:value-of select='../@name'/>").find("option[value='<xsl:value-of select='@value'/>']").data('urltemplate','<xsl:value-of select='@template'/>');<xsl:if test="concat(@template,'')=''">$("#<xsl:value-of select='../@name'/>").find("option[value='<xsl:value-of select='@value'/>']").data('description','<xsl:value-of select='@description'/>');</xsl:if></xsl:for-each>

		$("#<xsl:value-of select='@name'/>").change();
</xsl:template>

<!-- ###################################################################################################################### -->


<!--################################################################################################ FIELD @SOURCE=COMPRESS -->

<xsl:template match="field[@source='compress']" mode="draw">
	<xsl:param name="value" select="@default"/>
	<xsl:param name="name" select="@name"/>
	<xsl:param name="radio" select="'radio'"/>
	<xsl:param name="default" select="@default"/>
	<xsl:variable name="myvalue">
		<xsl:choose>
			<xsl:when test="count($value/@value)!=0 and concat($value/@valid,'')!='false'">
				<xsl:value-of select="$value/@value"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="@default"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
<!--
	value= '<xsl:value-of select="$myvalue"/>'
	valid= '<xsl:value-of select="$value/@valid"/>'
-->
	<div>
	<xsl:for-each select="*">
	<xsl:if test="@value!=' '">
		<input>
		<xsl:attribute name="type"><xsl:value-of select="$radio"/></xsl:attribute>
		<xsl:attribute name="value"><xsl:value-of select="@value"/></xsl:attribute>
		<xsl:attribute name="name"><xsl:value-of select="$name"/></xsl:attribute>
		
		<xsl:if test="$myvalue=@value">
			<xsl:attribute name="checked"><xsl:value-of select="'checked'"/></xsl:attribute>
		</xsl:if>

		<xsl:value-of select="."/>
		</input>
	</xsl:if>
	</xsl:for-each>
	</div>
</xsl:template>

<!--################################################################################################ FIELD @SOURCE=DATASET -->

<xsl:template match="field[@source='dataset']" mode="head">
	<xsl:if test="@type='table'">
		<script src="/js/table/jquery.dataTables.min.js">{}</script>
		<script src="/js/services/table/dataset.js">{}</script>
		<script src="/js/services/table/service.js">{}</script>
		<link rel="stylesheet" type="text/css" href="/template/css/services/maxfilelist.css"/>
		<link rel="stylesheet" type="text/css" href="/js/table/css/classic.css" />
	</xsl:if>
	<xsl:if test="@type='maxsize'">
		<link rel="stylesheet" type="text/css" href="/template/css/services/maxfilelist.css"/>
	</xsl:if>
</xsl:template>


<xsl:template match="field[@source='dataset']" mode="element">
	<xsl:variable name="name" select="@name"/>
	<xsl:variable name="templateType"><xsl:choose><xsl:when test="count(../const[@name='dataset' and @owner=$name]/element[@name='template'])!=0"><xsl:value-of select="../const[@name='dataset' and @owner=$name]/element[@name='template']"/></xsl:when><xsl:otherwise><xsl:value-of select="$services/dataset/template"/></xsl:otherwise></xsl:choose></xsl:variable>
	<label>
		<xsl:attribute name="for"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:value-of select="@caption"/>
	</label>	
	<select>
      <xsl:if test="concat(@type,'')!='single'">
      <xsl:attribute name="multiple">multiple</xsl:attribute>
      </xsl:if>
		<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="class"><xsl:value-of select="@source"/></xsl:attribute>
		<xsl:text> </xsl:text>
	</select>
	<xsl:for-each select="$services/dataset/catalogue[@type=$templateType]/*[@submit='true']">
	<input type="hidden" value="">
		<xsl:attribute name="name"><xsl:value-of select="$name"/>:<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="id"><xsl:value-of select="$name"/>_<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="class">dataset_aux_info</xsl:attribute>
	</input>

	</xsl:for-each>
	<xsl:for-each select="../metadata[@owner=$name]/element[@name!='']">
	<input type="hidden" value="">
		<xsl:attribute name="name"><xsl:value-of select="$name"/>:<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="id"><xsl:value-of select="$name"/>_<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="class">dataset_aux_info</xsl:attribute>
	</input>
	</xsl:for-each>

	<xsl:if test="@type='table'">
	<table border="0">
		<!-- <xsl:attribute name="id">_table_<xsl:value-of select="$name"/></xsl:attribute> -->
		<xsl:attribute name="class">display table_<xsl:value-of select="@source"/></xsl:attribute>
		<thead><tr><th class="no_sort">id</th><th class="no_sort">_</th><xsl:for-each select="../metadata[@owner=$name]/*[concat('',@hidden)!='true']"><th><xsl:value-of select="."/></th></xsl:for-each></tr></thead>
		<tbody></tbody>
	</table>
	</xsl:if>

</xsl:template>

<xsl:template match="field[@source='dataset']" mode="buttons_query">
	<xsl:choose>
		<xsl:when test="../const[@name='DescopeButtons']/@value='false'">		
	<input type='button' value='Query' class="dataset_button_query">
		<xsl:attribute name="id"><xsl:value-of select="@name"/>_button_query</xsl:attribute>
	</input>
	<span>
		<xsl:attribute name="id"><xsl:value-of select="@name"/>_query_status</xsl:attribute>
		<xsl:attribute name="class">query_message</xsl:attribute>
		 ready
	</span>	
		</xsl:when>
		<xsl:otherwise>
	<div class="dataset_button_query">
		<input type='button' value='Query'>
			<xsl:attribute name="id"><xsl:value-of select="@name"/>_button_query</xsl:attribute>
			<xsl:attribute name="class"><xsl:value-of select="@name"/>_button dataset_button_query</xsl:attribute>
		</input>
	</div>
	<div class="dataset_query_status">
		<span>
			<xsl:attribute name="id"><xsl:value-of select="@name"/>_query_status</xsl:attribute>
			<xsl:attribute name="class">query_message</xsl:attribute>
		</span>
	</div>		
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<xsl:template match="field[@source='dataset']" mode="buttons_dataset">
	<xsl:variable name="name" select="@name"/>
	<xsl:variable name="operations">
	<xsl:choose>
		<xsl:when test="count(../const[@name='dataset' and @owner=$name]/operations)!=0">
			<xsl:copy-of select="../const[@name='dataset' and @owner=$name]/operations/*"/>
		</xsl:when>
		<xsl:otherwise>
			<xsl:copy-of select="$services/dataset/operations/*"/>
		</xsl:otherwise>
	</xsl:choose>
	</xsl:variable>
	<xsl:choose>
		<xsl:when test="../const[@name='DescopeButtons']/@value='false'">
			<xsl:for-each select="msxsl:node-set($operations)/*">
	<input type='button'>
		<xsl:attribute name="value"><xsl:value-of select="@caption"/></xsl:attribute>
		<xsl:attribute name="class"><xsl:value-of select="$name"/>_button dataset_<xsl:value-of select="@type"/></xsl:attribute>
		<xsl:attribute name="id"><xsl:value-of select="$name"/>_<xsl:value-of select="@type"/></xsl:attribute>
	</input>
			</xsl:for-each>
		</xsl:when>
		<xsl:otherwise>
	<div>
		<xsl:attribute name="class">_buttons_dataset</xsl:attribute>
		<xsl:attribute name="id">_<xsl:value-of select="$name"/>_buttons_dataset</xsl:attribute>

			<xsl:for-each select="msxsl:node-set($operations)/*">
	<div>
		<xsl:attribute name="class">dataset_button dataset_<xsl:value-of select="@type"/></xsl:attribute>
		<input type='button'>
			<xsl:attribute name="value"><xsl:value-of select="@caption"/></xsl:attribute>
			<xsl:attribute name="class"><xsl:value-of select="$name"/>_button dataset_<xsl:value-of select="@type"/></xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="$name"/>_<xsl:value-of select="@type"/></xsl:attribute>
		</input>
	</div>
			</xsl:for-each>
	</div>
	</xsl:otherwise>
	</xsl:choose>
</xsl:template>



<xsl:template match="field[@source='dataset']">
	<xsl:param name="value" select="."/>
	<xsl:variable name="name" select="@name"/>
	<!--- make div and elements for files input -->
	<xsl:choose>
		<xsl:when test="../const[@name='DescopeButtons']/@value='false'">
	<div>
		<xsl:attribute name="id">_<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="class">_<xsl:value-of select="@source"/> _<xsl:value-of select="@source"/>_<xsl:value-of select="@type"/></xsl:attribute>  
		<xsl:apply-templates select="." mode="element"/>
		<xsl:apply-templates select="." mode="buttons_dataset"/>
		<xsl:apply-templates select="." mode="buttons_query"/>
	</div>
		</xsl:when>
		<xsl:otherwise>
	<div>
		<xsl:attribute name="id">_<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="class">_<xsl:value-of select="@source"/> _<xsl:value-of select="@source"/>_<xsl:value-of select="@type"/></xsl:attribute>
		<xsl:apply-templates select="." mode="element"/>
	</div>
	<xsl:apply-templates select="." mode="buttons_dataset"/>
	<xsl:apply-templates select="." mode="buttons_query"/>
		</xsl:otherwise>
	</xsl:choose>

</xsl:template>


<xsl:template match="field[@source='dataset']" mode="javascript">
	<xsl:variable name="name" select="@name"/>

      // link result files to layer index
      <xsl:variable name="layerIndex"><xsl:choose><xsl:when test="concat(@layerIndex,'')!=''"><xsl:value-of select="@layerIndex"/></xsl:when><xsl:otherwise><xsl:value-of select="$services/map/layerIndex"/></xsl:otherwise></xsl:choose></xsl:variable>
      $("#<xsl:value-of select='@name'/>").data("layerIndex", "<xsl:value-of select='$layerIndex'/>");

		// link query button to the dataset select 
		<xsl:variable name="templateType"><xsl:choose><xsl:when test="count(../const[@name='dataset' and @owner=$name]/element[@name='template'])!=0"><xsl:value-of select="../const[@name='dataset' and @owner=$name]/element[@name='template']"/></xsl:when><xsl:otherwise><xsl:value-of select="$services/dataset/template"/></xsl:otherwise></xsl:choose></xsl:variable>
		<xsl:variable name="resultCount"><xsl:choose><xsl:when test="count(../const[@name='dataset' and @owner=$name]/element[@name='count'])!=0"><xsl:value-of select="../const[@name='dataset' and @owner=$name]/element[@name='count']"/></xsl:when><xsl:otherwise><xsl:value-of select="$services/dataset/count"/></xsl:otherwise></xsl:choose></xsl:variable>	
		$(".<xsl:value-of select='@name'/>_button").data("dataset", "#<xsl:value-of select='@name'/>");
		<!--$("#<xsl:value-of select='@name'/>_button_delete").data("dataset", "#<xsl:value-of select='@name'/>");-->
      $("#<xsl:value-of select='@name'/>")
         .data("series", "#<xsl:value-of select='@owner'/>")
         .data("template","<xsl:value-of select="$templateType"/>")
         .data("count","<xsl:value-of select="$resultCount"/>");
		<!-- check if we want to add the more info pane -->
		<xsl:if test="count(../const[@name='dataset' and @owner=$name]/element[@name='moreinfo' and .='true'])!=0">$('.table_dataset').addClass("ifyMoreInfo");
		</xsl:if>
		<xsl:variable name="this" select="."/>				
		<!-- these calls map the catalogue parameters to the elements on the xml response (how to read the response) -->
      $("#<xsl:value-of select='$name'/>")<xsl:for-each select="$services/dataset/catalogue[@type=$templateType]/*[concat('',@submit)!='true']">
			<xsl:variable name="typeName" select="@name"/>
			.data("catalogue:<xsl:value-of select='$typeName'/>","<xsl:choose>
			<xsl:when test="count($this/../const[@name='catalogue' and @owner=$name]/element[@name=$typeName])!=0">
				<xsl:value-of select="$this/../const[@name='catalogue' and @owner=$name]/element[@name=$typeName]"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="."/>
			</xsl:otherwise>
			</xsl:choose>")</xsl:for-each>;
		<!-- $("#<xsl:value-of select='@name'/>").data("map", "#map"); or  <xsl:value-of select='@map'/>"); -->		

		<!-- link the extra metadata to the dataset select
		// Array of xml element, Caption, submit element name, hidden, don't retrieve the value from series
		// first are the mandaroty parameters from the dataset catalogue configuration then 
		// the parameters from the service -->
		$('#<xsl:value-of select="$name"/>').data("metadataDef", new Array(
			// configuration values for the catalogue
			<xsl:for-each select="$services/dataset/catalogue[@type=$templateType]/*[@submit='true']"><xsl:variable name="typeName" select="@name"/>new Array("<xsl:choose>
			<xsl:when test="count($this/../const[@name='catalogue' and @owner=$name]/element[@name=$typeName])!=0">
				<xsl:value-of select="$this/../const[@name='catalogue' and @owner=$name]/element[@name=$typeName]"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="."/>
			</xsl:otherwise>
			</xsl:choose>","","<xsl:value-of select='$typeName'/>","true","")<xsl:if test="position() &lt; last()">,
			</xsl:if>
			</xsl:for-each><xsl:if test="count(../metadata[@owner=$name]/element)!=0">,
			// metadata parameters defined in the service page</xsl:if>
			<xsl:for-each select="../metadata[@owner=$name]/element">
			<!-- 
			now we need to add the metadata parameters from the service configuration
			removed the required 
			new Array("<xsl:value-of select="@value"/>","<xsl:value-of select="."/>","<xsl:value-of select="@name"/>","<xsl:value-of select="@hidden"/>","<xsl:value-of select="@inherit"/>","<xsl:value-of select="@required"/>")<xsl:if test="position() &lt; last()">,</xsl:if></xsl:for-each>)
			-->
			new Array("<xsl:value-of select="@value"/>","<xsl:value-of select="."/>","<xsl:value-of select="@name"/>","<xsl:value-of select="@hidden"/>","<xsl:value-of select="@inherit"/>4")<xsl:if test="position() &lt; last()">,</xsl:if></xsl:for-each>)
		);
		<xsl:for-each select="../metadata[@owner=$name]/element[@name!='']">
		$('#<xsl:value-of select="$name"/>_<xsl:value-of select="@name"/>').data("metadata","<xsl:value-of select="@value"/>");</xsl:for-each>
		<xsl:for-each select="$services/dataset/catalogue[@type=$templateType]/*[@submit='true']">
		<xsl:variable name="typeName" select="@name"/>
		$('#<xsl:value-of select="$name"/>_<xsl:value-of select="@name"/>').data("metadata","<xsl:choose>
			<xsl:when test="count($this/../const[@name='catalogue' and @owner=$name]/element[@name=$typeName])!=0">
				<xsl:value-of select="$this/../const[@name='catalogue' and @owner=$name]/element[@name=$typeName]"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="."/>
			</xsl:otherwise>
			</xsl:choose>");</xsl:for-each>
      // connection to map
      <xsl:choose>
         <xsl:when test="concat(@map,'')!=''">$('#<xsl:value-of select="$name"/>').data('mapId',"<xsl:value-of select='@map'/>");
         </xsl:when>
         <xsl:when test="count(../const[@name='map'])!=0 and concat(../const[@name='map']/@id,'')=''">$('#<xsl:value-of select="$name"/>').data('mapId',"ifyServiceMap");</xsl:when>
      </xsl:choose>
</xsl:template>


<!--################################################################################################ FIELD @EXT=GEO:BOX -->
<!-- todo: this has to be more generic and use the logic of owner (the bbox input element) -->  
<xsl:template match="field[@ext='geo:box']">
	<xsl:apply-imports/>	
	<xsl:variable name="name" select="@name"/>
	<div id='boxPanel'>
		<input type='text' id='bbox_maxX' class='float geo_maxX'/>
		<input type='text' id='bbox_maxY' class='float geo_maxY'/>
		<input type='text' id='bbox_minX' class='float geo_minX'/>
		<input type='text' id='bbox_minY' class='float geo_minY'/>
	</div>
</xsl:template>

<xsl:template match="field[@ext='geo:box']" mode="javascript">
      <xsl:variable name="name" select="@name"/>
      <xsl:variable name="mapid"><xsl:choose><xsl:when test="concat(../const[@name='map' and owner=$name]/@id,'')!=''"><xsl:value-of select="../const[@name='map' and owner=$name]/@id"/></xsl:when><xsl:otherwise>ifyServiceMap</xsl:otherwise></xsl:choose></xsl:variable>

      $("#<xsl:value-of select='$name'/>").data("OLMap",$("#<xsl:value-of select='$mapid'/>").data("OLMap"));
      $("#<xsl:value-of select='$name'/>").change();

      <xsl:if test="concat(@scope,'')!='' and (concat(@range,'')='closed' or concat(@range,'')='')">
         $("#boxPanel input").attr('disabled', 'disabled');
         //$("#<xsl:value-of select='@name'/>").val('<xsl:value-of select="element/@value"/>');
         //$("#<xsl:value-of select='@name'/>").change();
      </xsl:if>

</xsl:template>



<!--################################################################################################ FIELD @EXT=GEO:GEOMETRY -->
<xsl:template match="field[@ext='geo:geometry']">
   <xsl:apply-imports/>
</xsl:template>

<xsl:template match="field[@ext='geo:geometry']" mode="javascript">
   <xsl:variable name="name" select="@name"/>
   <xsl:variable name="mapid"><xsl:choose><xsl:when test="concat(../const[@name='map' and owner=$name]/@id,'')!=''"><xsl:value-of select="../const[@name='map' and owner=$name]/@id"/></xsl:when><xsl:otherwise>ifyServiceMap</xsl:otherwise></xsl:choose></xsl:variable>
   InputGeometryName = "#<xsl:value-of select='$name'/>"; 
   var geometrySelection = new OpenLayers.Control.DrawFeature(
                           selectionBoxLayer, OpenLayers.Handler.Polygon,
                           { displayClass: "olControlGeometrySelection",
                              callbacks: {done: geometrySelectionEndDragEvent }
                        });
   OSIconPanel.addControls([geometrySelection]);

   $("#<xsl:value-of select='$name'/>").data("OLMap",$("#<xsl:value-of select='$mapid'/>").data("OLMap"));
   //$("#<xsl:value-of select='$name'/>").change();

</xsl:template>



<!--################################################################################################  FIELD @EXT=TIME:START OR END -->
<xsl:template match="field[@ext='time:start']" mode="head">
		<script src="/js/cal.js">{}</script>
		<link rel="stylesheet" type="text/css" href="/template/css/cal.css" />
</xsl:template>	

<xsl:template match="field[@ext='time:start' or @ext='time:end']" mode="draw">
	<xsl:variable name="name" select="@name"/>
	<xsl:variable name="value" select="../../item/*[name()=$name]"/>
	<div>
		<xsl:attribute name="id">_<xsl:value-of select="$name"/></xsl:attribute>
		<xsl:attribute name="class">ify_<xsl:value-of select="translate(@ext,':','_')"/></xsl:attribute>
		
		<xsl:apply-templates select="." mode="status"/>
		<input type='text'>
			<xsl:attribute name="id"><xsl:value-of select="$name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="$name"/></xsl:attribute>
			<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
			<xsl:attribute name="class"><xsl:value-of select="@type"/><xsl:text> </xsl:text><xsl:value-of select="translate(@ext,':','_')"/></xsl:attribute>
		</input>
	</div>
</xsl:template>

<xsl:template match="field[@ext='time:end']">
</xsl:template>
<xsl:template match="field[ @ext='time:end']" mode="javascript">
//		$("#<xsl:value-of select="@name"/>").change(function (){if ($(this).val().indexOf("T")==-1){$(this).val($(this).val() + "T23:59:59")}})
</xsl:template>

<xsl:template match="field[ @ext='time:start']">
	<div id='datePanel' class='catalogue_query_extension'>		
		<xsl:apply-templates select="../field[@type='startdate' or @type='enddate']" mode="draw"/>
	</div>
</xsl:template> 


<xsl:template match="field[ @ext='time:start']" mode="javascript">
	// link the calendars to the dates 
	$('#datePanel div').simpleDatepicker({startdate: "1999-01-01", enddate: "2011-12-31", x:0 , y:20 });
//	$("#<xsl:value-of select="@name"/>").change(function (){if ($(this).val().indexOf("T")==-1){$(this).val($(this).val() + "T00:00:00")}})

</xsl:template> 



<!--################################################################################################  FIELD @TYPE=TEXTFILE -->

<xsl:template match="field[@type='textfile']" mode="draw">
	<xsl:param name="value" select="."/>
		<textarea>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:value-of select="$value"/>
		</textarea>
</xsl:template> 

<xsl:template match="field[@type='textfile']" mode="javascript">

$("#<xsl:value-of select="concat(@name,'_configurable')"/>").change(function (){
	$("#<xsl:value-of select="@name"/>").val('');
	getConfigurableValue($(this),$(this).val());
});

</xsl:template> 
	
<xsl:template match="field[@type='customBinary']" mode="draw">
	<div>
		<xsl:attribute name="id"><xsl:value-of select="@name" />_binaryDiv</xsl:attribute>
		<textarea>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="class">ifyInvisible</xsl:attribute>
		</textarea>
		<div>
			<xsl:attribute name="id">_<xsl:value-of select="@name" />UploadDiv</xsl:attribute>
			<input>
				<xsl:attribute name="id">_<xsl:value-of select="@name" />File</xsl:attribute>
				<xsl:attribute name="type">file</xsl:attribute>
				<xsl:attribute name="class">ifyOptional</xsl:attribute>
			</input>
			<Button>
				<xsl:attribute name="id"><xsl:value-of select="@name" />_base64UploadButton</xsl:attribute>
				<xsl:attribute name="type">button</xsl:attribute>
				Upload
			</Button>
		</div>
		<p>
			<xsl:attribute name="id"><xsl:value-of select="@name" />_summary</xsl:attribute>
			No file selected
		</p>
	</div>
</xsl:template>

</xsl:stylesheet>

