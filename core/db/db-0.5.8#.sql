/*****************************************************************************/

-- Changing structure of table "action" and adding new action for task status refresh ... \
ALTER TABLE action
    ADD COLUMN pos smallint unsigned COMMENT 'Position for ordering' AFTER id,
    ADD COLUMN description text COMMENT 'Caption of action' AFTER caption
;
UPDATE action SET pos=1, description='This action performs operations involving the Grid engine (submission and abortion of tasks and jobs) that have been delayed for faster web portal response times. Task and job operations are delayed when at the moment of the request the Control Panel setting "Synchronous Task Operations" is not active.' WHERE name='task';
UPDATE action SET pos=2, description='This action manages the creation and submission of tasks for the active task schedulers according to the resource limitations.' WHERE name='scheduler';
INSERT INTO action (pos, name, caption, description) VALUES
    (3, 'taskstatus', 'Task status refresh', 'This action refreshes task, job and node status information for active tasks.')
;
UPDATE action SET pos=4, description='This action deletes the tasks that have been marked for deletion from the database by a deletion. Tasks are marked for deletion when at the moment of their deletion request from the web portal the Control Panel setting "Synchronous Task Operations" is not active.' WHERE name='taskdelete';
UPDATE action SET pos=5, description='This action sends requests to all series catalogue description URLs defined in the Control Panel and refreshes the corresponding catalogue URL templates.' WHERE name='series';
;
-- RESULT

/*****************************************************************************/
