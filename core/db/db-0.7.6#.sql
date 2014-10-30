/*****************************************************************************/

-- Changing structure of table "job" ... \
ALTER TABLE job
    ADD COLUMN publish boolean COMMENT 'Job is publishing job' AFTER min_args,
    ADD COLUMN forced_exit boolean COMMENT 'Execution is interrupted after job environment creation' AFTER publish
;
-- RESULT
-- CHECKPOINT 0.7.6-1

-- Setting flag for publishing jobs ... \
UPDATE job SET publish=(job_type='Publish');
-- RESULT

/*****************************************************************************/

-- Changing structure of table "filter" ... \
CREATE TABLE temp_filter (id int);
INSERT INTO temp_filter (id) SELECT id FROM filter AS t WHERE id!=(SELECT MIN(id) FROM filter AS t1 WHERE t1.id_usr=t.id_usr AND t1.caption=t.caption);
-- NORESULT
UPDATE filter SET caption=CONCAT(caption, '(', id, ')') WHERE id IN (SELECT id FROM temp_filter);
DROP TABLE temp_filter;
ALTER TABLE filter ADD UNIQUE INDEX (id_usr, caption);
-- RESULT

/*****************************************************************************/
