/*****************************************************************************/

-- Changing structure of table "scheduler" (task submission limit) ... \
ALTER TABLE scheduler
    ADD COLUMN max_submit smallint default 1 COMMENT 'Maximum number of tasks submitted per scheduler cycle' AFTER resource_perc
;
-- RESULT

/*****************************************************************************/
