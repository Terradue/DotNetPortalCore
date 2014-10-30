/*****************************************************************************/

-- Changing structure of table "taskparam" (additional indexes) ... \
ALTER TABLE taskparam
    ADD INDEX (id_task DESC, id_job DESC, type),
    ADD INDEX (id_job DESC, type)
;
-- RESULT

/*****************************************************************************/
