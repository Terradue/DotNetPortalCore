/*****************************************************************************/

CREATE TABLE temporaltask (
    id_task int unsigned NOT NULL COMMENT 'FK: Concerned task',
    start_time datetime COMMENT 'Start date/time of selection period',
    end_time datetime COMMENT 'End date/time of selection period',
    PRIMARY KEY (id_task),
    FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Temporal parameters of processing tasks';

-- Adding temporal task parameters into table "temporaltask" ... \
SET SESSION sql_mode='ALLOW_INVALID_DATES';
INSERT INTO temporaltask (id_task, start_time, end_time) SELECT t.id, CAST(t1.value AS datetime), CAST(t2.value AS datetime) FROM task AS t INNER JOIN taskparam AS t1 ON t.id=t1.id_task INNER JOIN taskparam AS t2 ON t.id=t2.id_task WHERE t1.name='startdate' AND t2.name='stopdate' AND t1.id_job IS NULL AND t2.id_job IS NULL AND t1.type IS NULL AND t2.type IS NULL; 
INSERT INTO temporaltask (id_task, start_time, end_time) SELECT t.id, CAST(t1.value AS datetime), CAST(t2.value AS datetime) FROM task AS t INNER JOIN taskparam AS t1 ON t.id=t1.id_task INNER JOIN taskparam AS t2 ON t.id=t2.id_task WHERE t1.id_job IS NULL AND t2.id_job IS NULL AND t1.type='startdate' AND t2.type IN ('enddate', 'stopdate');   
-- RESULT

/*****************************************************************************/

-- Changing structure of table "taskparam" (remove additional indexes) ... \
ALTER TABLE taskparam
    DROP INDEX id_task_2,
    DROP INDEX id_job_2
;
-- RESULT

/*****************************************************************************/
