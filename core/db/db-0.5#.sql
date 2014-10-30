/*****************************************************************************/

-- Renaming configuration variables ... \
UPDATE config SET name='StatusRequestMethod' WHERE name='StatusRefreshMethod';
-- RESULT

/*****************************************************************************/

-- Adding language and time zone fields to table "usr" ... \
ALTER TABLE usr
    ADD COLUMN language char(2) COMMENT 'Preferred language' AFTER country,
    ADD COLUMN timezone char(6) COMMENT 'Time zone' AFTER language
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "cedir" ... \
ALTER TABLE cedir
    CHANGE COLUMN name path varchar(50) NOT NULL COMMENT 'Directory absolute path'
;
-- RESULT

/*****************************************************************************/

-- Adding creation and modification triggers for table "service" ... \
ALTER TABLE service
    ADD COLUMN created datetime,
    ADD COLUMN modified datetime
;

CREATE TRIGGER service_insert BEFORE INSERT ON service FOR EACH ROW
BEGIN
    SET NEW.created=now(), NEW.modified=now();
END;

CREATE TRIGGER service_update BEFORE UPDATE ON service FOR EACH ROW
BEGIN
    SET NEW.modified=now();
END;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "schedulerparam" ... \
ALTER TABLE schedulerparam
    ADD COLUMN type varchar(25) COMMENT 'Type identifier' AFTER name
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "job" ... \
ALTER TABLE job
    ADD COLUMN next_status tinyint unsigned COMMENT 'Desired next status' AFTER status
;
-- RESULT

/*****************************************************************************/
