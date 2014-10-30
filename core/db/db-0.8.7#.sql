/*****************************************************************************/

-- Changing structure of table "usr" ... \
UPDATE usr SET time_zone='UTC';
ALTER TABLE usr 
    CHANGE COLUMN time_zone time_zone char(25) NOT NULL default 'UTC' COMMENT 'Time zone'
;
-- RESULT

/*****************************************************************************/
