/*****************************************************************************/

-- Changing structure of table "config" ... \
ALTER TABLE config
    CHANGE COLUMN name name varchar(50) NOT NULL COMMENT 'PK: Name of configuration parameter',
    CHANGE COLUMN value value text COMMENT 'Value of configuration parameter',
    ADD COLUMN optional boolean default 0 COMMENT 'True if parameter does not require a value'
;
UPDATE config SET optional = name IN ('TrustedHosts', 'NewsDatabase', 'ServiceParamFileRoot', 'ChangeLogValidity', 'DefaultResultFolderSize', 'DefaultTaskLifeTime', 'TaskDetailsUrl', 'JobDetailsUrl', 'DefaultCatalogueBaseUrl', 'PublishRetryWaitTime', 'PublishRetryTimes');    
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" ... \
CREATE TABLE temp_usr (id int);
INSERT INTO temp_usr (id) SELECT id FROM usr AS t WHERE id!=(SELECT MIN(id) FROM usr AS t1 WHERE t1.username=t.username);
-- NORESULT
UPDATE usr SET username=CONCAT(username, '(', id, ')') WHERE id IN (SELECT id FROM temp_usr);
DROP TABLE temp_usr;
ALTER TABLE usr ADD UNIQUE INDEX (username);
-- RESULT

/*****************************************************************************/
