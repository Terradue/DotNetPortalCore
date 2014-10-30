<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" 
        xmlns="http://www.w3.org/2005/Atom"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
        xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"  
        xmlns:dc="http://purl.org/dc/elements/1.1/" 
        xmlns:dct="http://purl.org/dc/terms/" 
        xmlns:geo="http://a9.com/-/opensearch/extensions/geo/1.0/" 
        xmlns:dclite4g="http://xmlns.com/2008/dclite4g#" 
        xmlns:ical="http://www.w3.org/2002/12/cal/ical#" 
        xmlns:envisat="http://www.example.com/schemas/envisat.rdf#" 
        xmlns:owl="http://www.w3.org/2002/07/owl#" 
        xmlns:fp="http://downlode.org/Code/RDF/file-properties/" 
        xmlns:ws="http://dclite4g.xmlns.com/ws.rdf#" 
        xmlns:jers="http://www.eorc.jaxa.jp/JERS-1/en/" 
        xmlns:os="http://a9.com/-/spec/opensearch/1.1/" 
        xmlns:atom="http://www.w3.org/2005/Atom"  
        xmlns:eop="http://www.genesi-dr.eu/spec/opensearch/extensions/eop/1.0/"
        xmlns:msxsl="urn:schemas-microsoft-com:xslt"
        exclude-result-prefixes="atom jers dclite4g fp ical envisat dc dct rdf"
>


<xsl:template match="rdf:RDF">
<xsl:variable name="opensearch" select="document(concat(substring-before(rdf:Description/atom:link[@atom:rel='self']/@atom:href,'/html'),'/description'))"/>


<xsl:variable name="urltemplate" select="$opensearch/os:OpenSearchDescription/os:Url[@type='application/rdf+xml']/@template"/>
        <xsl:variable name="url" select="concat($urltemplate,'')"/>
        <xsl:variable name="server" select="substring-before($url,'?')"/>

        <xsl:variable name="template">
                <xsl:call-template name="split">
                        <xsl:with-param name="string" select="substring-after($url,'?')" />
                        <xsl:with-param name="pattern" select="'&amp;'" />
                        <xsl:with-param name="os_query" select="rdf:Description/os:Query[@role='request']"/>
                </xsl:call-template>
        </xsl:variable> 

<div class='content' id='table'>
<xsl:value-of select="rdf:Description/os:totalResults "/> results, showing from <xsl:value-of select="rdf:Description/os:startIndex + 1 "/> to <xsl:choose>
        <xsl:when test="rdf:Description/os:itemsPerPage &gt; rdf:Description/os:totalResults">
                <xsl:value-of select="rdf:Description/os:totalResults"/>
        </xsl:when>
        <xsl:otherwise>
                <xsl:value-of select="rdf:Description/os:startIndex + rdf:Description/os:itemsPerPage"/> 
        </xsl:otherwise>
</xsl:choose>
<table id="table" class="display">
<thead><tr>
<th>identifier</th>
<th>Geometry</th>
<th>Title</th>
<th>Abstract</th>
<th>Subject</th>
<th>Publisher</th>
<th>Resources</th>

</tr></thead>
<tbody>
        <xsl:for-each select="dclite4g:Series">
                <xsl:variable name="about" select="@rdf:about"/>
                
                <tr>
                <td><xsl:value-of select="dc:identifier"/></td>
                <td><xsl:choose><xsl:when test="count(dct:spatial)!=0"><xsl:value-of select="dct:spatial"/></xsl:when><xsl:otherwise>POLYGON((-180 -90,-180 90,180 90,180 -90,-180 -90))</xsl:otherwise></xsl:choose></td>
                <td><xsl:value-of select="dc:title"/></td>
                <td><xsl:value-of select="dc:abstract"/></td>
                <td><xsl:value-of select="dc:subject"/></td>
                <td><xsl:value-of select="dc:publisher/@rdf:resource"/></td>
                <td><xsl:value-of select="$about"/></td>
                </tr>
        </xsl:for-each>
</tbody>
</table>

<xsl:if test="rdf:Description/atom:link[@atom:rel='previous']/@atom:href!=''">
        <a><xsl:attribute name="href"><xsl:value-of select="rdf:Description/atom:link[@atom:rel='previous']/@atom:href"/></xsl:attribute>Previous Page</a>
</xsl:if>
 - 
<xsl:if test="rdf:Description/atom:link[@atom:rel='next']/@atom:href!=''">
        <a><xsl:attribute name="href"><xsl:value-of select="rdf:Description/atom:link[@atom:rel='next']/@atom:href"/></xsl:attribute>Next Page</a>
</xsl:if>

</div>
<div id="ajax-indicator" style="display: none;">
</div>
<div id="footer">
</div>

</xsl:template>
</xsl:stylesheet>