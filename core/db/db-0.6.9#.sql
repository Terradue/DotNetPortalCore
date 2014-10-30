/*****************************************************************************/

INSERT INTO config (name) VALUES ('DbVersionCheckpoint');

/*****************************************************************************/

-- Changing structure of table "ce" (status request) ... \
ALTER TABLE ce
    CHANGE COLUMN max_weight capacity int unsigned NOT NULL default '0' COMMENT 'Maximum processing capacity',
    ADD COLUMN status_method tinyint default 0 COMMENT 'Status request method',
    ADD COLUMN status_url varchar(100) COMMENT 'URL for status information'
;
-- RESULT

-- TODO In the Control Panel, add the status information URLs for all Computing Elements
-- CHECKPOINT 0.6.9-1

/*****************************************************************************/

-- Changing structure of table "scheduler" (Computing Element status verification) ... \
ALTER TABLE scheduler
    DROP COLUMN custom_url,
    DROP COLUMN ce_weight,
    CHANGE COLUMN resource_perc max_ce_usage tinyint default 0 COMMENT 'Maximum usage (in %) of Computing Element for tasks of the scheduler',
    ADD COLUMN ignore_ce_load boolean COMMENT 'True if scheduler is supposed to ignore the existing load on the Computing Element' AFTER max_ce_usage
;
-- RESULT
    
/*****************************************************************************/
