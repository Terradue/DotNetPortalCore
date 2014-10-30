<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Context:       template/xsl/
 Name:          fields.xsl
 Version:       0.1 
 Description:   Generic GUI elements for the field type for all pages 
	Each field element is called first to the head (building the necessary information for the html head element), followed by
	the configurable (to check if the element is configurable) and then to write the javascript section.


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

<!--################################################################################################  GENERIC FIELDS -->
<xsl:template match="fields" mode="head">
	<xsl:apply-templates select="field" mode="head"/>
</xsl:template>

<xsl:template match="fields" mode="configurable">
	<xsl:apply-templates select="field" mode="configurable"/>
</xsl:template>

<xsl:template match="fields" mode="javascript">
	<xsl:apply-templates select="field" mode="javascript"/>
</xsl:template>



<!--################################################################################################  GENERIC FIELD -->

<xsl:template match="field" mode="head"/>

<xsl:template match="field" mode="javascript"/>

<xsl:template match="field" mode="configurable"/>
<!-- ###################################################################################################################### -->


<!--################################################################################################  GENERIC FIELD -->
<xsl:template match="field" mode="elements">
	<xsl:param name="name" select="@name"/>
	<xsl:param name="value" select="../../item/*[name()=$name]"/>
	<xsl:param name="element" select="."/>
		<xsl:apply-templates select="." mode="label">
			<xsl:with-param name="value" select="$value"/>
			<xsl:with-param name="element" select="$element"/>
		</xsl:apply-templates>		
		<xsl:apply-templates select="." mode="draw">
			<xsl:with-param name="value" select="$value"/>
			<xsl:with-param name="element" select="$element"/>
		</xsl:apply-templates>		
</xsl:template>

<xsl:template match="field">
	<xsl:param name="name" select="@name"/>
	<xsl:param name="value" select="../../item/*[name()=$name]"/>
	<xsl:param name="element" select="."/>
	<div>		
		<xsl:attribute name="id">_<xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="title"><xsl:value-of select="@hint"/></xsl:attribute>
		<xsl:attribute name="class">
		<xsl:if test="concat(@source,'')!=''">_<xsl:value-of select="@source"/></xsl:if>
		<xsl:if test="concat(@ext,'')!=''">_<xsl:value-of select="translate(@ext,':','_')"/> catalogue_query_extension</xsl:if>
		</xsl:attribute>
		<xsl:apply-templates select="." mode="elements">
			<xsl:with-param name="value" select="$value"/>
			<xsl:with-param name="element" select="$element"/>
		</xsl:apply-templates>		
	</div>
 	<xsl:apply-templates select="." mode="configurable"/>
</xsl:template>

<!--######################################################################## GENERIC FIELD ## LABEL MODE -->
<xsl:template match="field" mode="label">
	<label>
		<xsl:variable select="@type" name="type"/>
		<xsl:variable select="@ext" name="ext"/>
		
		<xsl:attribute name="for"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:choose>
			<xsl:when test="concat(@caption,'')!=''">
				<xsl:value-of select="@caption"/>
			</xsl:when>
			<xsl:when test="concat($opensearch/items[@type=$type]/@caption,'')!=''">
				<xsl:value-of select="$opensearch/items[@type=$type]/@caption"/>
			</xsl:when>
			<xsl:when test="concat($opensearch/items[@type=$ext]/@caption,'')!=''">
				<xsl:value-of select="$opensearch/items[@type=$ext]/@caption"/>
			</xsl:when>
			<xsl:when test="concat($config/labels/label[@type=$type],'')!=''">
				<xsl:value-of select="$config/labels/label[@type=$type]"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="@name"/>
			</xsl:otherwise>
		</xsl:choose> 
	</label> 
</xsl:template>

<xsl:template match="field[@type='hidden']" mode="label"/>

<!--######################################################################## GENERIC FIELD ## DRAW MODE -->
	<!-- this type has no xsl defined (or else wouldn't be here) -->
<xsl:template match="field" mode="draw">
	<xsl:param name="element" select="."/>	
	<xsl:param name="name" select="@name"/>
	<xsl:param name="value" select="../../item/*[name()=$name]"/>

	<xsl:variable select="@type" name="type"/>	
	<xsl:variable select="@ext" name="ext"/>
	<xsl:choose>
		<xsl:when test="concat($opensearch/items[@type=$type]/@type,'')!='' and count($opensearch/items[@type=$type]/*)!=0">
		<!-- this is opensearch type defined  as a select list --> 
		<!-- and it is not defined in any template -->
			<xsl:variable name="selected"><xsl:choose><xsl:when test="concat($element/*[name()=$opensearch/items[@type=$type]/@value],'')!=''"><xsl:value-of select="$element/*[name()=$opensearch/items[@type=$type]/@value]/node()"/></xsl:when><xsl:otherwise><xsl:value-of select="$value"/></xsl:otherwise></xsl:choose></xsl:variable>			
			<xsl:call-template name="inputSelect">
				<xsl:with-param name="name" select="@name"/>
				<xsl:with-param name="values" select="$opensearch/items[@type=$type]/*"/>
				<xsl:with-param name="selected" select="msxsl:node-set($selected)"/>
				<xsl:with-param name="class" select="translate($type,':','_')"/>
			</xsl:call-template>
		</xsl:when>

		<xsl:when test="concat($opensearch/items[@type=$type]/@type,'')='' and concat($ext,'')=''">
		<!-- this type has no opensearch type defined nor is defined in any template --> 
		<!-- this is really the default behavior as an input text -->
			<input type='text'>
				<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
				<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
				<xsl:if test="$value!='?'">
				<xsl:attribute name="value"><xsl:value-of select="concat($value,$element/os:Query[@role='request']/@*[name()=$type])"/></xsl:attribute>
				</xsl:if>
				<xsl:attribute name="class"><xsl:value-of select="translate($type,':','_')"/></xsl:attribute>
			</input>  <!-- (<xsl:value-of select="@type"/>)  -->
		</xsl:when>
		<xsl:otherwise>
		<!-- this type has opensearch ext attribute type defined but that is not defined in the configuration --> 
		<!--  and without a template defined in the xsl -->
		<input type='text'>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:if test="$value!='?'">
			<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
			</xsl:if>
			<xsl:attribute name="class"><xsl:value-of select="translate(@type,':','_')"/><xsl:text> </xsl:text><xsl:value-of select="$opensearch/items[@type=$ext]/@id"/><xsl:text> </xsl:text><xsl:value-of select="translate($opensearch/items[@type=$ext]/@type,':','_')"/></xsl:attribute>
		</input>
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=HIDDEN-->
<xsl:template match="field[@type='hidden']" mode="draw">
	<xsl:param name="value" select="."/>
	<input type='hidden'>
		<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
	</input> 
</xsl:template>
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=CAPTION -->
<xsl:template match="field[@type='caption']" mode="draw">
	<xsl:param name="value" select="."/>
		<input type='text'>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
			<xsl:attribute name="class">_caption</xsl:attribute>
		</input>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=SELECT -->
<xsl:template match="field[@type='select']"  mode="draw">
	<xsl:param name="value" select="."/>
		 <xsl:call-template name="inputSelect">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="values" select="*"/>
			<xsl:with-param name="selected" select="$value"/>
			<xsl:with-param name="class" select="@source"/>
		</xsl:call-template>
</xsl:template> 

<xsl:template match="field[@type='select' and @ext!='' and count(*)=0]"  mode="draw">	
	<xsl:param name="value" select="."/>
	<!-- this is for select types with an link to a opensearch element 
		and that haven't any nested elements -->
	<!-- however we must check if the extension exists on the opensearch if not then 
		just draw an input text -->
	<xsl:variable name="type" select="@ext"/>
		 <xsl:call-template name="inputSelect">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="values" select="$opensearch/items[@type=$type]/*"/>
			<xsl:with-param name="selected" select="$value"/>
			<xsl:with-param name="class"><xsl:value-of select="$opensearch/items[@type=$type]/@id"/><xsl:text> </xsl:text></xsl:with-param>
		</xsl:call-template>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=MULTIPLE -->
<xsl:template match="field[@type='multiple']" mode="draw">
	<xsl:param name="value" select="."/>
		<xsl:call-template name="inputSelect">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="values" select="*"/>
			<xsl:with-param name="selected" select="$value/*"/>
			<xsl:with-param name="multiselect" select="true()"/>
		</xsl:call-template>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=ENTITITES -->
<!-- 
<xsl:template match="field[@type='entities' and ( @name='wdirs' or @name='rdirs')]">

NOTE: TO-DO 
currently this template is done for ce's working and result dir, 
this should be transformed into templates: one for that specific output and another for the more generic template 
the following code was moved to ce.xsl
-->
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=VALUES -->
<xsl:template match="field[@type='values']" mode="draw">
	<xsl:param name="value" select="."/>
		<select>
			<xsl:attribute name="multiple">multiple</xsl:attribute>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:text> </xsl:text>
 			<xsl:for-each select="$value/*">
				<option>
					<xsl:attribute name="value"><xsl:value-of select="."/></xsl:attribute>
					<xsl:attribute name="selected">selected</xsl:attribute>
					<xsl:value-of select="."/>
				</option>
			</xsl:for-each>
 		</select>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=DATETIME-->
<xsl:template match="field[@type='datetime']|param[@type='datetime']" mode="draw">
	<xsl:param name="value" select="."/>
 		<input type='text'>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
			<xsl:attribute name="class"><xsl:value-of select="@type"/></xsl:attribute>
		</input>
</xsl:template> 

<xsl:template match="field[@type='datetime']" mode="javascript">
				$("#<xsl:value-of select="@name"/>").simpleDatepicker({startdate: "1999-01-01", enddate: "2011-12-31", x:0 , y:0 });
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=ROLES -->
<xsl:template match="field[@type='roles']|param[@type='roles']"  mode="draw">
	<xsl:param name="value" select="''"/>
		<xsl:call-template name="inputSelect">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="values" select="$config/roles/*"/>
			<xsl:with-param name="selected" select="$value"/>
		</xsl:call-template>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=RATING -->
<xsl:template match="field[@source='rating']"  mode="draw">
	<xsl:param name="value" select="''"/>
		<xsl:call-template name="inputRadio">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="values" select="element"/>
			<xsl:with-param name="selected" select="$value"/>
		</xsl:call-template>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=BOOL -->
<!-- here the label and draw are inverted -->
<xsl:template match="field[@type='bool']" mode="elements">
	<xsl:param name="value" select="../../item/*[name()=$name]"/>
	<xsl:param name="element" select="."/>
		<xsl:apply-templates select="." mode="draw">
			<xsl:with-param name="value" select="$value"/>
			<xsl:with-param name="element" select="$element"/>
		</xsl:apply-templates>		
		<xsl:apply-templates select="." mode="label">
			<xsl:with-param name="value" select="$value"/>
			<xsl:with-param name="element" select="$element"/>
		</xsl:apply-templates>		
</xsl:template>
<xsl:template match="field[@type='bool']" mode="draw">
	<xsl:param name="value" select="'false'"/>
	<xsl:variable name="name" select="local-name()"/>
	<xsl:variable name="myvalue"><xsl:element name="{local-name()}">
	<xsl:choose><xsl:when test="$value='0'">false</xsl:when><xsl:when test="$value='1'">true</xsl:when><xsl:otherwise><xsl:value-of select="$value"/></xsl:otherwise></xsl:choose></xsl:element></xsl:variable>
 
		<xsl:call-template name="inputSelect">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="values" select="$config/boolean/*"/>
			<xsl:with-param name="selected" select="msxsl:node-set($myvalue)"/>
			<xsl:with-param name="class" select="'ifyBool'"/>
		</xsl:call-template>
		<input type="checkbox">
			<xsl:attribute name="id"><xsl:value-of select="@name"/>_check</xsl:attribute>
			<xsl:attribute name="class">ifyBoolCheckBox</xsl:attribute>
			<xsl:if test="$value!='false' and $value!='' and $value!='FALSE' and $value!='False' and $value!='0'">
			<xsl:attribute name="checked">checked</xsl:attribute>
			</xsl:if>
		</input>
</xsl:template>


<!-- ###################################################################################################################### -->



<!--################################################################################################  FIELD TYPE=TEXT -->
<xsl:template match="field[@type='text']|param[@type='text']" mode="draw">
	<xsl:param name="value" select="."/>
		<textarea>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:value-of select="$value"/>
		</textarea>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!--################################################################################################  FIELD TYPE=PASSWORD -->
<xsl:template match="field[@type='password']|param[@type='password']" mode="javascript">

</xsl:template> 

<xsl:template match="field[@type='password']|param[@type='password']" mode="draw">
	<xsl:param name="value" select="."/>
	<input type="password" class="ifyPassword">
		<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
		<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
	</input>
	  retype password 
	<input type="password" class="ifyPasswordCheck">
		<xsl:attribute name="id"><xsl:value-of select="@name"/>_recheck</xsl:attribute>
	</input>
</xsl:template> 
<!-- ###################################################################################################################### -->

<!--################################################################################################  FIELD TYPE=RANGE-->
<xsl:template match="field[@type='range']" mode="draw">
	<xsl:param name="value" select="."/>
	<input type='text'>
			<xsl:attribute name="id"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="name"><xsl:value-of select="@name"/></xsl:attribute>
			<xsl:attribute name="value"><xsl:value-of select="$value"/></xsl:attribute>
			<xsl:attribute name="type">text</xsl:attribute>
			<xsl:attribute name="class">ifyRange</xsl:attribute>
		</input>
	<div class="ifyRangeDiv">
			From 
			<input type='text' class='ifyRangeFrom'>
				<xsl:attribute name="value"><xsl:value-of select="substring-after(substring-before($value,','),'[')"/></xsl:attribute>
			</input>
			to 
			<input type='text' class='ifyRangeTo'>
				<xsl:attribute name="value"><xsl:value-of select="substring-before(substring-after($value,','),']')"/></xsl:attribute>
			</input>
	</div>
</xsl:template> 
<!-- ###################################################################################################################### -->

<!--################################################################################################  FIELD TYPE=READ-ONLY-->
<xsl:template match="field[@readonly='true']" mode="draw">
	<xsl:param name="value" select="."/>
		<xsl:value-of select="$value"/>
</xsl:template> 
<!-- ###################################################################################################################### -->

</xsl:stylesheet>
