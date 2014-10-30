/*****************************************************************************/

-- Changing structure of table "scheduler" (status, 1) ... \
ALTER TABLE scheduler
    CHANGE COLUMN status status tinyint unsigned COMMENT 'Most recent status' AFTER id_class
;
-- RESULT

-- Changing data for scheduler status ... \
UPDATE scheduler SET status=CASE WHEN enabled THEN 20 ELSE 10 END;
-- RESULT

-- Changing structure of table "scheduler" (status, 1) ... \
ALTER TABLE scheduler DROP COLUMN enabled;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "task" (asynchronous operations, 1) ... \
ALTER TABLE task
    ADD COLUMN async_op tinyint unsigned COMMENT 'Requested asynchronous operation' AFTER status
;
-- RESULT

-- Changing data for asynchronous operations ... \
UPDATE task SET async_op=CASE WHEN next_status=11 THEN 20 WHEN next_status=10 THEN 10 WHEN next_status=20 THEN 21 WHEN next_status=255 THEN 255 END;
-- RESULT

-- Changing structure of table "task" (asynchronous operations, 2) ... \
ALTER TABLE task
    DROP COLUMN next_status
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "job" (asynchronous operations, 1) ... \
ALTER TABLE job
    ADD COLUMN async_op tinyint unsigned COMMENT 'Requested asynchronous operation' AFTER status
;
-- RESULT

-- Changing data for asynchronous operations ... \
UPDATE job SET async_op=CASE WHEN next_status=11 THEN 20 WHEN next_status=10 THEN 10 WHEN next_status=20 THEN 21 WHEN next_status=255 THEN 255 END;
-- RESULT

-- Changing structure of table "job" (asynchronous operations, 2) ... \
ALTER TABLE job
    DROP COLUMN next_status
;
-- RESULT

/*****************************************************************************/
