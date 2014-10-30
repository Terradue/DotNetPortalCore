/*****************************************************************************/

-- Changing structure of table "task" (error messages) ... \
ALTER TABLE task
    ADD COLUMN message_text varchar(255) COMMENT 'Text of error/warning message' AFTER message_code
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "job" (sequential execution identifier) ... \
ALTER TABLE job
    ADD COLUMN seq_exec_id varchar(25) COMMENT 'Sequential execution identifier' AFTER async_op
;
-- RESULT

/*****************************************************************************/
