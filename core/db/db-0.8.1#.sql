/*****************************************************************************/

-- Changing structure of table "application" ... \
ALTER TABLE application
    CHANGE COLUMN enabled available boolean COMMENT 'True if application is available to users'
;
-- RESULT

/*****************************************************************************/
