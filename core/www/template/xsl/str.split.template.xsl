<?xml version="1.0"?>
<!--
 Project:       ifynet
 Author:        Terradue Srl
 Last update:   23.04.2010
 Element:       ify web portal
 Name:          template/xsl/str.split.template.xsl
 Version:       0.1 
 Description:   Split function for xslt 1.0 

 This document is the property of Terradue and contains information directly 
 resulting from knowledge and experience of Terradue.
 Any changes to this code is forbidden without written consent from Terradue Srl
 
 Contact: info@terradue.com 
-->
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:str="http://exslt.org/strings"
                extension-element-prefixes="str">

<xsl:template name="str:split">
  <xsl:param name="string" select="''" />
  <xsl:param name="pattern" select="' '" />
  <xsl:choose>
    <xsl:when test="not($string)" />
    <xsl:when test="not($pattern)">
      <xsl:call-template name="str:_split-characters">
        <xsl:with-param name="string" select="$string" />
      </xsl:call-template>
    </xsl:when>
    <xsl:otherwise>
      <xsl:call-template name="str:_split-pattern">
        <xsl:with-param name="string" select="$string" />
        <xsl:with-param name="pattern" select="$pattern" />
      </xsl:call-template>
    </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:template name="str:split_get_last">
	<xsl:param name="string" select="''" />
  <xsl:param name="pattern" select="' '" />
  <xsl:choose>
    <xsl:when test="not($string)" />
    <xsl:when test="not($pattern)">
      <xsl:call-template name="str:_split-characters">
        <xsl:with-param name="string" select="$string" />
      </xsl:call-template>
    </xsl:when>
    <xsl:otherwise>
      <xsl:call-template name="str:_split-pattern_get_last">
        <xsl:with-param name="string" select="$string" />
        <xsl:with-param name="pattern" select="$pattern" />
      </xsl:call-template>
    </xsl:otherwise>
  </xsl:choose>

</xsl:template>

<xsl:template name="str:_split-characters">
  <xsl:param name="string" />
  <xsl:if test="$string">	
    <token><xsl:value-of select="substring($string, 1, 1)" /></token>
    <xsl:call-template name="str:_split-characters">
      <xsl:with-param name="string" select="substring($string, 2)" />
    </xsl:call-template>
  </xsl:if>

</xsl:template>

<xsl:template name="str:_split-pattern">
  <xsl:param name="string" />
  <xsl:param name="pattern" />
  <xsl:choose>
    <xsl:when test="contains($string, $pattern)">
      <xsl:if test="not(starts-with($string, $pattern))">
	<xsl:choose>
		<xsl:when test="contains(substring-before($string, $pattern),'={')">
			<field>
				<xsl:attribute name="name"><xsl:value-of select="substring-before(substring-before($string, $pattern),'=')"/></xsl:attribute>
				<xsl:variable name="type"><xsl:value-of select="translate(translate(substring-after(substring-before($string, $pattern),'={'),'}',''),'?','')"/></xsl:variable>
				<xsl:attribute name="type"><xsl:value-of select="$type"/></xsl:attribute>
				<xsl:attribute name="ext"><xsl:value-of select="$type"/></xsl:attribute>
				<xsl:attribute name="sort"><xsl:choose><xsl:when test="$opensearch/items[@type=$type]/@sort!=''"><xsl:value-of select="$opensearch/items[@type=$type]/@sort"/></xsl:when><xsl:otherwise>999</xsl:otherwise></xsl:choose></xsl:attribute>
				<xsl:attribute name="optional">
				<xsl:if test="substring-after(substring-before($string, $pattern),'?')='}'">true</xsl:if>
				</xsl:attribute>
				<xsl:attribute name="group"><xsl:choose><xsl:when test="$opensearch/items[@type=$type]/@group!=''"><xsl:value-of select="$opensearch/items[@type=$type]/@group"/></xsl:when><xsl:when test="count($opensearch/items[@type=$type])=0">unknown_queriables</xsl:when><xsl:otherwise>aux_queriables</xsl:otherwise></xsl:choose></xsl:attribute>
				
				<!-- <xsl:value-of select="substring-before($string, $pattern)" /> -->
			</field>
		</xsl:when>
		<xsl:otherwise>
			<token><xsl:value-of select="substring-before($string, $pattern)" /></token>
		</xsl:otherwise>
	</xsl:choose>
      </xsl:if>

      <xsl:call-template name="str:_split-pattern">
        <xsl:with-param name="string" select="substring-after($string, $pattern)" />
        <xsl:with-param name="pattern" select="$pattern" />
      </xsl:call-template>
    </xsl:when>
    <xsl:otherwise>
	<xsl:choose>
		<xsl:when test="contains($string,'={')">
			<field>
				<xsl:attribute name="name"><xsl:value-of select="substring-before($string,'=')"/></xsl:attribute>
				<xsl:variable name="type"><xsl:value-of select="translate(translate(substring-after($string,'={'),'}',''),'?','')"/></xsl:variable>
				<xsl:attribute name="type"><xsl:value-of select="$type"/></xsl:attribute>
				<xsl:attribute name="ext"><xsl:value-of select="$type"/></xsl:attribute>
				<xsl:attribute name="sort"><xsl:choose><xsl:when test="$opensearch/items[@type=$type]/@sort!=''"><xsl:value-of select="$opensearch/items[@type=$type]/@sort"/></xsl:when><xsl:otherwise>999</xsl:otherwise></xsl:choose></xsl:attribute>
				<xsl:attribute name="optional">
				<xsl:if test="substring-after(substring-after($string,'={'),'?')='}'">true</xsl:if>
				</xsl:attribute>
				<xsl:attribute name="group"><xsl:choose><xsl:when test="$opensearch/items[@type=$type]/@group!=''"><xsl:value-of select="$opensearch/items[@type=$type]/@group"/></xsl:when><xsl:when test="$opensearch/items[@type=$type]/@type=''">unknown_queriables</xsl:when><xsl:otherwise>aux_queriables</xsl:otherwise></xsl:choose></xsl:attribute>

			</field>
		</xsl:when>
		<xsl:otherwise>
			<token><xsl:value-of select="$string" /></token>
		</xsl:otherwise>
	</xsl:choose>

    </xsl:otherwise>
  </xsl:choose>

</xsl:template>

<xsl:template name="str:_split-pattern_get_last">
  <xsl:param name="string" />
  <xsl:param name="pattern" />
  <xsl:choose>
    <xsl:when test="contains($string, $pattern)">
      <xsl:call-template name="str:_split-pattern_get_last">
        <xsl:with-param name="string" select="substring-after($string, $pattern)" />
        <xsl:with-param name="pattern" select="$pattern" />
      </xsl:call-template>
    </xsl:when>
    <xsl:otherwise>
	<xsl:value-of select="$string" />
    </xsl:otherwise>
  </xsl:choose>

</xsl:template>

<xsl:template name="str:replaceCharsInString">
  <xsl:param name="stringIn"/>
  <xsl:param name="charsIn"/>
  <xsl:param name="charsOut"/>
  <xsl:choose>
    <xsl:when test="contains($stringIn,$charsIn)">
      <xsl:value-of select="concat(substring-before($stringIn,$charsIn),$charsOut)"/>
      <xsl:call-template name="str:replaceCharsInString">
        <xsl:with-param name="stringIn" select="substring-after($stringIn,$charsIn)"/>
        <xsl:with-param name="charsIn" select="$charsIn"/>
        <xsl:with-param name="charsOut" select="$charsOut"/>
      </xsl:call-template>
    </xsl:when>
    <xsl:otherwise>
      <xsl:value-of select="$stringIn"/>
    </xsl:otherwise>
  </xsl:choose>
</xsl:template>


</xsl:stylesheet>
