/*****************************************************************************/

-- Changing structure of table "pubserver" ... \
ALTER TABLE pubserver
    CHANGE COLUMN path path varchar(100) NOT NULL default '/' COMMENT 'Task result root path on host',
    CHANGE COLUMN upload_url upload_url varchar(200) NOT NULL COMMENT 'URL for task result upload',
    CHANGE COLUMN download_url download_url varchar(200) COMMENT 'URL for task result download (optional)',
    ADD COLUMN file_root varchar(200) COMMENT 'Absolute filesystem path of a task result root (optional)' AFTER download_url, 
    ADD COLUMN delete_files boolean default false COMMENT 'If true, delete also task result files when task is deleted' AFTER metadata 
;
-- RESULT

/*****************************************************************************/
