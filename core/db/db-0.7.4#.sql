/*****************************************************************************/

-- Changing structure of table "producttype" (catalogue reference) ... \
ALTER TABLE producttype
    ADD COLUMN cat_description varchar(200) COMMENT 'OpenSearch description URL of series' AFTER descr,
    ADD COLUMN cat_template varchar(1000) COMMENT 'OpenSearch template URL of series' AFTER cat_description
;
-- RESULT

/*****************************************************************************/
