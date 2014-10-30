<!--
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
	 xmlns:dclite4g="http://dclite4g.xmlns.com/"
	 exclude-result-prefixes="msxsl js">

<xsl:include href='./str.split.template.xsl'/>
<xsl:include href='./search.xsl'/>
<xsl:include href='./operations.xsl'/>
<xsl:include href='./fields.xsl'/>

<xsl:template match="itemList" />


<xsl:template match="content" mode="head">
	<script type="text/javascript" src="/js/jquery/js/jquery-1.4.4.min.js">{}</script>
	<script type="text/javascript" src="/js/base.js">{}</script>
	<script type="text/javascript" src="/js/wtooltip.min.js">{}</script>
</xsl:template>

<!-- blocks by default the display of the user information -->
<xsl:template match="user"/>

<!--
mixes two xml docs 
used when checking the configuration from a service (or a task result) 
and the default configuration
-->
<xsl:template name="inherit" >
	<xsl:param name="value" select="."/>
	<xsl:param name="default" select="."/>
	<xsl:for-each select="$default/*">
		<xsl:variable name="elname" select="name()"/>		
		<xsl:choose>
			<xsl:when test="count($value/*[name()=$elname])!=0">
				<xsl:copy-of select="$value/*[name()=$elname]"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:copy-of select="."/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:for-each>	
</xsl:template>

<xsl:template name="inputSelect">
	<xsl:param name="name" select="''"/>
	<xsl:param name="id" select="$name"/>
	<xsl:param name="selected" select="."/>
	<xsl:param name="values" select="."/>
	<xsl:param name="class" select="''"/>
	<xsl:param name="multiselect" select="false()"/>
	<xsl:param name="addempty" select="false()"/>
		<!-- selected="<xsl:copy-of select="$selected"/>"
		values="<xsl:copy-of select="$values"/>" -->
	<select>
		<xsl:if test="$multiselect"><xsl:attribute name="multiple">multiple</xsl:attribute></xsl:if>
		<xsl:if test="$name!=''">			
			<xsl:attribute name="name"><xsl:value-of select="$name"/></xsl:attribute>
		</xsl:if>
		<xsl:if test="$id!=''">			
			<xsl:attribute name="id"><xsl:value-of select="$id"/></xsl:attribute>
		</xsl:if>
		<xsl:if test="$class!=''"><xsl:attribute name="class"><xsl:value-of select="$class"/></xsl:attribute></xsl:if>
		<xsl:text> </xsl:text>
		<xsl:if test="$addempty"><option value="">---</option></xsl:if>
 		<xsl:for-each select="$values">
			<option>
				<xsl:variable name="myid" select="@value"/>
				<xsl:attribute name="value"><xsl:value-of select="$myid"/></xsl:attribute>
				<xsl:if test="$selected=@value">
					<xsl:attribute name="selected">selected</xsl:attribute>
				</xsl:if>
				<xsl:if test="$selected/@value=@value">
					<xsl:attribute name="selected">selected</xsl:attribute>
				</xsl:if>
				<xsl:if test="$selected/element[@value=$myid]=.">
					<xsl:attribute name="selected">selected</xsl:attribute>
				</xsl:if>
				<xsl:value-of select="."/></option>
		</xsl:for-each>
 	</select> 
</xsl:template>
<!-- ###################################################################################################################### -->


<xsl:template name="table"><!-- <xsl:template match="itemList">-->
	<xsl:param name="count" select="1"/>
	<xsl:param name="ajax" select="true()"/>
	<!-- <xsl:param name="operations" select=""/> check this pedro -->
	<table class="elements" border="0" id="elements">
	<tr ><th></th>
	<xsl:for-each select="fields/*[@name!='id']">
		<th><xsl:attribute name="id">ifyHeader_<xsl:value-of select="@name"/></xsl:attribute><xsl:value-of select="@caption"/></th>
	</xsl:for-each>
	</tr>
	<xsl:for-each select="items/item">
	<tr><xsl:attribute name="id">ifyRow_<xsl:value-of select="@id"/></xsl:attribute>
	<td class="ifySelectPagingControlCheckbox"><input type="checkbox" onclick="ifySelectPagingControlSelectElement(this.value)">
			<xsl:attribute name="value"><xsl:value-of select="@id"/></xsl:attribute>
			<xsl:attribute name="id">c<xsl:value-of select="@id"/></xsl:attribute>
			<xsl:attribute name="name">checkBoxId</xsl:attribute></input></td>
	<xsl:variable name="id" select="@id"/>
	<xsl:for-each select="*">
		<td>
			<xsl:variable name="name" select="name()"/>
			<xsl:variable name="value" select="@value"/>
			<xsl:variable name="text" select="."/>
			<xsl:choose>
				<xsl:when test='../../../fields/*[@name=$name]/@type="select"'>
					<xsl:value-of select="../../../fields/*[@name=$name]/*[@value=$value]"/>
				</xsl:when>
				<xsl:when test='../../../fields/*[@name=$name]/@type="text"'>
					<xsl:value-of select="." disable-output-escaping="yes" />
				</xsl:when>
				<xsl:when test='../../../fields/*[@name=$name]/@type="status"'>
					<xsl:value-of select="$opensearch/items[@type='ify:status']/*[@value=$value]" />
					<xsl:if test="concat($value,'')=''"> 
					<xsl:value-of select="$opensearch/items[@type='ify:status']/*[@value=$text]" />
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="." />
				</xsl:otherwise>
			</xsl:choose>
		</td>
	</xsl:for-each>
	<td><a><xsl:choose>
		<xsl:when test="concat(@link,'')!=''">
			<xsl:attribute name="href"><xsl:value-of select="@link"/></xsl:attribute>
		</xsl:when>
		<xsl:otherwise>
			<xsl:attribute name="href">?id=<xsl:value-of select="$id"/></xsl:attribute>
		</xsl:otherwise>
		</xsl:choose><img src='/template/images/edit.png'/></a> 
	<!-- <img src='/template/images/delete.png'/> -->
	</td>
	</tr>
	</xsl:for-each>
	</table>
</xsl:template>
<!-- ###################################################################################################################### -->

<xsl:template match="field[@name='id']" mode="ta_ble"/>

<xsl:template match="field" mode="table">
	<th><xsl:attribute name="id">ifyHeader_<xsl:value-of select="@name"/></xsl:attribute><xsl:value-of select="@caption"/></th>
</xsl:template>

<xsl:template match="item/*" mode="table">
		<td>
			<xsl:variable name="name" select="name()"/>
			<xsl:variable name="value" select="@value"/>
			<xsl:variable name="text" select="."/>
			<xsl:choose>
				<xsl:when test='../../../fields/*[@name=$name]/@type="select"'>
					<xsl:value-of select="../../../fields/*[@name=$name]/*[@value=$value]"/>
				</xsl:when>
				<xsl:when test="$name='caption'"><a><xsl:attribute name="href"><xsl:value-of select="../@link"/></xsl:attribute><xsl:value-of select="."/></a>
						</xsl:when>
				<xsl:when test="$name='title'"><a><xsl:attribute name="href"><xsl:value-of select="../@link"/></xsl:attribute><xsl:value-of select="."/></a>
						</xsl:when>
				<xsl:when test='../../../fields/*[@name=$name]/@type="text"'>
					<xsl:value-of select="." disable-output-escaping="yes" />
				</xsl:when>
				<xsl:when test='../../../fields/*[@name=$name]/@type="status"'>
					<xsl:attribute name="class">tdItemStatus<xsl:value-of select="."/></xsl:attribute>
					<xsl:value-of select="$opensearch/items[@type='ify:status']/*[@value=$value]" />
					<xsl:if test="concat($value,'')=''"> 
					<xsl:value-of select="$opensearch/items[@type='ify:status']/*[@value=$text]" />
					</xsl:if>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="."/>
				</xsl:otherwise>
			</xsl:choose>
		</td>

</xsl:template>



<xsl:template match="item" mode="table_edit">
	<xsl:variable name="id" select="@id"/>
	
	<td class="tdTableItemEdit"><a><xsl:choose>
		<xsl:when test="concat(@link,'')!=''">
			<xsl:attribute name="href"><xsl:value-of select="@link"/></xsl:attribute>
		</xsl:when>
		<xsl:otherwise>
			<xsl:attribute name="href">?id=<xsl:value-of select="$id"/></xsl:attribute>
		</xsl:otherwise>
		</xsl:choose><img src='/template/images/edit.png'/></a> 
	</td>
</xsl:template>

<xsl:template match="item" mode="table">
	<tr><xsl:attribute name="id">ifyRow_<xsl:value-of select="@id"/></xsl:attribute>
	<xsl:if test="../../operations/@multiple='true'">
	<td class="ifySelectPagingControlCheckbox"><input type="checkbox" onclick="ifySelectPagingControlSelectElement(this.value)">
			<xsl:attribute name="value"><xsl:value-of select="@id"/></xsl:attribute>
			<xsl:attribute name="id">c<xsl:value-of select="@id"/></xsl:attribute>
			<xsl:attribute name="name">checkBoxId</xsl:attribute></input></td>
	</xsl:if>
	<xsl:apply-templates select="*" mode="table"/>

	<xsl:apply-templates select="." mode="table_edit"/>

	</tr>
</xsl:template>


<xsl:template match="itemList" mode="table"><!-- <xsl:template match="itemList">-->
	<xsl:param name="count" select="1"/>
	<table class="elements" border="0" id="elements">
	<tbody>
	<tr>
	<xsl:if test="operations/@multiple='true'">
	<th onclick="javascript:$('input[id*=c][type=checkbox]').click();ifySelectPagingControlUpdateSelected();"></th>
	</xsl:if>
	<xsl:apply-templates select="fields/*" mode="table"/>
	</tr>

	<xsl:apply-templates select="items/item" mode="table"/>
	</tbody>
	</table>
</xsl:template>
<!-- ###################################################################################################################### -->




<xsl:template name="inputCheck">
	<xsl:param name="name" select="."/>
	<xsl:param name="selected" select="."/>
	<xsl:param name="values" select="."/>
	<xsl:param name="multiselect" select="false()"/>
 		<xsl:for-each select="$values">
			<input type="checkbox">
				<xsl:attribute name="name"><xsl:value-of select="$name"/></xsl:attribute>
				<xsl:variable name="myid" select="@value"/>
				<xsl:attribute name="value"><xsl:value-of select="$myid"/></xsl:attribute>
				<xsl:if test="$selected=@value">
					<xsl:attribute name="checked">checked</xsl:attribute>
				</xsl:if>
				<xsl:if test="$selected/@value=@value">
					<xsl:attribute name="checked">checked</xsl:attribute>
				</xsl:if>
				<xsl:if test="$selected/element[@value=$myid]=.">
					<xsl:attribute name="checked">checked</xsl:attribute>
				</xsl:if>
				<xsl:value-of select="."/></input>
		</xsl:for-each>
</xsl:template>

<!-- ###################################################################################################################### -->

<xsl:template name="inputRadio">
	<xsl:param name="name" select="."/>
	<xsl:param name="selected" select="."/>
	<xsl:param name="values" select="."/>
 		<xsl:for-each select="$values">
			<input type="radio">
				<xsl:attribute name="name"><xsl:value-of select="$name"/></xsl:attribute>
				<xsl:variable name="myid" select="@value"/>
				<xsl:attribute name="value"><xsl:value-of select="$myid"/></xsl:attribute>
				<xsl:if test="$selected=@value">
					<xsl:attribute name="checked">checked</xsl:attribute>
				</xsl:if>
				<xsl:if test="$selected/@value=@value">
					<xsl:attribute name="checked">checked</xsl:attribute>
				</xsl:if>
				<xsl:if test="$selected/element[@value=$myid]=.">
					<xsl:attribute name="checked">checked</xsl:attribute>
				</xsl:if>
				<xsl:value-of select="."/></input>
		</xsl:for-each>
</xsl:template>


<!-- ###################################################################################################################### -->

<xsl:template name="Pages">
	<xsl:param name="count" select="1"/>
	<xsl:param name="max" select="$count"/>
	<xsl:param name="page" select="1"/>
<!-- 	<xsl:param name="link" select=""/>-->
	<xsl:if test="$count &gt; 0">
		<xsl:text></xsl:text>
		<xsl:variable name="mypage" select="$max - $count + 1"/>
		<xsl:if test="$mypage>1 and $max &gt; 10 and ( ($mypage &lt; ($page - 4)) and ($mypage &gt; ($page - 10)))or ( ($mypage &gt; ($page + 10))  and ($mypage &lt; ($page + 15)) )">
			.
		</xsl:if>
		<xsl:if test="$max &lt; 10 or $mypage=1 or ( ($mypage &gt; ($page - 5)) and  ($mypage &lt; ($page + 10)) )">
		
			<span class="ifySelectPagingControlItem" onclick="ifySelectPagingControlExecute($(this).html())">
				<xsl:if test="$max - $count + 1 = $page">
					<xsl:attribute name="class">ifySelectPagingControlItemSelected</xsl:attribute>
				</xsl:if>
				<xsl:value-of select="$max - $count + 1 "/>	
			</span>
			
			 <!-- <span class="navCountSelectedItems"><xsl:attribute name="id">navCount<xsl:value-of select="$max - $count + 1"/></xsl:attribute><xsl:text>&#32;</xsl:text></span>  -->
		
		</xsl:if>

		<xsl:call-template name="Pages">
			<xsl:with-param name="count" select="$count - 1"/>
			<xsl:with-param name="max" select="$max"/>
			<xsl:with-param name="page" select="$page"/>
			<!-- <xsl:with-param name="link" select="$link"/> -->
			<!-- <xsl:with-param name="ajax" select="$ajax"/> -->
		</xsl:call-template>
	</xsl:if>
</xsl:template>
<!-- ###################################################################################################################### -->


<xsl:template match="message[@class='userLogOut']">
	<xsl:apply-imports/>
		<script type="text/javascript">
		$(document).ready(function(){
		window.location.href = '/';
		})
		</script>
</xsl:template>
<!-- ###################################################################################################################### -->


<xsl:template name="loginElements">		
	<form method="post" action="" id="login_form">
		<label style="margin-left:-4px" for="login_username">User Name:</label><br/>
		<input name="username" type="text" id="login_username" value=""/>
		<br/>
		<label style="margin-left:-4px" for="login_password">Password:</label><br/>
		<input name="password" type="password" id="login_password" value=""  />
		<br/>
		<input id="login_button" src="/template/images/sign_in.gif" value="Sign In" name="b1" type="image"/>
	<span id="msgbox"></span>		
	</form>
</xsl:template>
<!-- ###################################################################################################################### -->

<xsl:template name="basename">
  <xsl:param name="path"/>
  <xsl:choose>
     <xsl:when test="contains($path, '/')">
        <xsl:call-template name="basename">
           <xsl:with-param name="path"><xsl:value-of select="substring-after($path, '/')"/></xsl:with-param>
        </xsl:call-template>
     </xsl:when>
     <xsl:otherwise>
        <xsl:value-of select="$path"/>
     </xsl:otherwise>
  </xsl:choose>
</xsl:template> 
<!-- ###################################################################################################################### -->


<!-- ###################################################################################################################### -->
<xsl:variable name="link">
    <xsl:call-template name="str:replaceCharsInString">
      <xsl:with-param name="stringIn" select="/content/@link"/>
      <xsl:with-param name="charsIn" select="'_format=xml&amp;'"/>
      <xsl:with-param name="charsOut" select="''"/>
    </xsl:call-template>
</xsl:variable>
<!-- ###################################################################################################################### -->


</xsl:stylesheet>
