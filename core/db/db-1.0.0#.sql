/*****************************************************************************/

-- Changing structure of table "task" (empty tasks) ... \
ALTER TABLE task
    ADD COLUMN empty boolean NOT NULL default false COMMENT 'True if task has no input files' AFTER message_code
;
-- RESULT

/*****************************************************************************/
