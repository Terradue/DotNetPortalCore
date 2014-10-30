/*****************************************************************************/

-- Changing structure of table "series" ... \
ALTER TABLE series
    ADD COLUMN auto_refresh boolean default true COMMENT 'If true, template is refreshed by the background agent' AFTER cat_template
;
-- RESULT

/*****************************************************************************/
