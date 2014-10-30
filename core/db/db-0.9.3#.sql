/*****************************************************************************/

-- Changing structure of table "scheduler" ... \
ALTER TABLE scheduler
    ADD COLUMN ahead boolean COMMENT 'True if the current task must still be created'
;
-- RESULT

/*****************************************************************************/
