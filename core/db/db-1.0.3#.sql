/*****************************************************************************/

-- Adding additional fields for WPS tasks ... \
-- TRY START
ALTER TABLE wpstask
    ADD COLUMN ref_output boolean COMMENT 'Value of "asReference" attribute' AFTER lineage_xml,
    ADD COLUMN xsl_name varchar(50) COMMENT 'Name of XSL for output transformation' AFTER ref_output
;
-- TRY END
-- RESULT

/*****************************************************************************/
