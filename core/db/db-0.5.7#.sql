/*****************************************************************************/

-- Changing structure of table "job" ... \
ALTER TABLE job
    ADD COLUMN max_nodes int COMMENT 'Maximum number of nodes' AFTER grid_type,
    ADD COLUMN min_args int COMMENT 'Minimum number of arguments per node' AFTER max_nodes
;
-- RESULT

/*****************************************************************************/
