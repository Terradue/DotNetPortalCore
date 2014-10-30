/*****************************************************************************/

-- Changing "cleanup" action ... \
UPDATE action SET
    name='cleanup',
    caption='Task and scheduler cleanup',
    description='This action removes the tasks and schedulers that have been marked for deletion from the database by a deletion. Tasks and schedulers are marked for deletion when at the moment of their deletion request from the web portal the Control Panel setting "Synchronous Task Operations" is not active.'
WHERE name='taskdelete';
-- RESULT

/*****************************************************************************/

-- Changing structure of table "scheduler" (status information) ... \
ALTER TABLE scheduler
    ADD COLUMN status tinyint unsigned COMMENT 'Most recent status' AFTER compression,
    CHANGE COLUMN last_status last_message text COMMENT 'Last status message'
;
-- RESULT

/*****************************************************************************/
