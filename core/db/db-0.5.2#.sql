/*****************************************************************************/

-- Renaming table "client" to application and changing its structure ... \
ALTER TABLE taskgroup DROP FOREIGN KEY taskgroup_ibfk_1;
RENAME TABLE client TO application;
ALTER TABLE application COMMENT 'External client applications, such as web services';
ALTER TABLE application
    ADD COLUMN available boolean COMMENT 'True if application is available' AFTER id,
    ADD COLUMN caption varchar(100) NOT NULL COMMENT 'Caption' AFTER name,
    ADD COLUMN config_file varchar(100) COMMENT 'Location of application configuration file' AFTER caption
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" ... \
ALTER TABLE usr
    CHANGE COLUMN active enabled boolean DEFAULT true COMMENT 'True if user account is active'
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "taskgroup" ... \
ALTER TABLE taskgroup
    CHANGE COLUMN id_client id_application int unsigned NOT NULL COMMENT 'FK: Application that created the task group',
    CHANGE COLUMN config_name template varchar(50) NOT NULL COMMENT 'Name of template used at task group creation',
    ADD FOREIGN KEY (id_application) REFERENCES application(id) ON DELETE CASCADE
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "scheduler" ... \
ALTER TABLE scheduler
    CHANGE COLUMN active enabled boolean COMMENT 'True if scheduler is enabled'
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "task" ... \
ALTER TABLE task
    ADD COLUMN auto_register boolean COMMENT 'True if automatic registration at task completion is desired' AFTER compression
;
-- RESULT

/*****************************************************************************/
