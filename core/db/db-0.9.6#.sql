/*****************************************************************************/

-- Adding configuration variables task submission retrying ... \
SET @section = (SELECT id FROM configsection WHERE caption='Tasks');
UPDATE config SET pos = pos + 2 WHERE id_section=@section AND pos >= 5;
-- NORESULT
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section, 5, 'TaskRetry', 'int', 'taskSubmitRetry', 'Submission Retrying', 'Select the policy for interactively created tasks that cannot be submitted immediately (e.g. for capacity reasons)', '1', true),
    (@section, 6, 'TaskRetryPeriod', 'timespan', NULL, 'Default Submission Retrying Period', 'Enter the default length of the time period after the submission of a task during which the background agent tries to submit the task again', '1h', true)
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "scheduler" (additional time-driven settings) ... \
ALTER TABLE scheduler
    ADD COLUMN past_only boolean COMMENT 'Keep execution date in past' AFTER time_interval,
    ADD COLUMN no_fail boolean COMMENT 'Interrupt execution if a task has failed' AFTER past_only
;
UPDATE scheduler SET past_only=true WHERE mode=1;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "task" (submission retrying) ... \
ALTER TABLE task
    ADD COLUMN retry_period smallint COMMENT 'Retry period (in min) after first pending submission' AFTER auto_register,
    ADD COLUMN message_code tinyint unsigned COMMENT 'Code of error/warning message' AFTER async_op
;
-- RESULT

/*****************************************************************************/

-- Adding lookup list and values for task submission retrying ... \
INSERT INTO lookuplist (system, name) VALUES (true, 'taskSubmitRetry');
SET @list = (SELECT LAST_INSERT_ID());
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list, 1, 'Never retry submissions', '1'),
    (@list, 2, 'Ask user (specifiy period below)', '2'),
    (@list, 3, 'Always retry submissions (specifiy period below)', '3')
;
-- RESULT

/*****************************************************************************/
