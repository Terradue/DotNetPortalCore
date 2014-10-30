/*****************************************************************************/

-- Adding configuration for default XSL file ... \
SET @section = (SELECT id FROM configsection WHERE caption='Site');
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section, 14, 'DefaultXslFile', 'string', NULL, 'Default XSLT file', 'Enter the full path of the XSLT file for the default transformation of portal''s XML output to HTML', NULL, true)
;
-- RESULT

/*****************************************************************************/
