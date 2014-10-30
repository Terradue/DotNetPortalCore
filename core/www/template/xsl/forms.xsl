<?xml version="1.0" encoding="UTF-8"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Name:          template/xsl/forms.xsl
 Version:       0.1 
 Description:   Generic forms GUI elements

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl

 Contact: info@terradue.com 
-->
1234567889
---> NOT USED 
123456789
<xsl:stylesheet version="2.0" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:os="http://a9.com/-/spec/opensearch/1.1/">
<xsl:output method="html" version="4.0" encoding="iso-8859-1" indent="yes" doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" 
doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd" media-type="application/xhtml+xml" omit-xml-declaration="yes"/>

<xsl:template name="login">		
	<form method="post" action="" id="login_form">
	<div style="margin:10px" > 1234
		<label style="margin-left:-4px" for="username">User Name:</label><br/>
		<input name="username" type="text" id="username" value="" maxlength="20" />
		<br/>
		<label style="margin-left:-4px" for="password">Password:</label><br/>
		<input name="password" type="password" id="password" value="" maxlength="20" />
		<br/>
		<input style="background-color:#B8DBE1;margin-left:25px;margin-top:5px;" src="/template/images/sign_in.gif" value="Sign In" name="b1" type="image"/>
	    <!-- <input name="Submit" type="submit" id="submit" value="Login" style="margin-left:-10px; height:23px"  /> -->
	</div>		
	    
	    <span id="msgbox"></span>		
	</form>
</xsl:template>
</xsl:stylesheet>