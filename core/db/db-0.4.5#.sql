/*****************************************************************************/

-- Adding configuration for task and job details ... \
UPDATE config SET pos=pos+2 WHERE id_configsection=4 AND pos>=2;
-- NORESULT
INSERT INTO config (name, id_configsection, pos, type, caption, hint) VALUES
('TaskDetailsUrl', 4, 2, 'url', 'Task Details URL', 'Enter the URL for the retrieval of detailed task information from the Grid Engine, use the "$SESSION" or "$UID" placeholders'),
('JobDetailsUrl', 4, 3, 'url', 'Job Details URL', 'Enter the URL for the retrieval of detailed job information from the Grid Engine, use the "$SESSION", "$UID" or "$JOB" placeholders');
-- RESULT

/*****************************************************************************/

-- Changing structure of table "pubserver" ... \
ALTER TABLE pubserver ADD COLUMN caption varchar(50) NOT NULL COMMENT 'Caption' AFTER id_usr;
-- RESULT

-- Setting captions of publish servers ... \
UPDATE pubserver AS t SET caption=CONCAT(CASE WHEN t.username IS NULL THEN '' ELSE CONCAT(t.username, '@') END, t.hostname, CASE WHEN t.port IS NULL THEN '' ELSE CONCAT(':', t.port) END);
-- RESULT

/*****************************************************************************/

CREATE TABLE serviceclass (
    id int unsigned NOT NULL auto_increment,
    name varchar(50) NOT NULL COMMENT 'Unique name',
    caption varchar(100) NOT NULL COMMENT 'Service class caption',
    PRIMARY KEY (id),
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Service classes';

/*****************************************************************************/

-- Initializing classes of processing services ... \
/*!40000 ALTER TABLE serviceclass DISABLE KEYS */;
INSERT INTO serviceclass (id, name, caption) VALUES
    (1, 'A', 'A'),
    (2, 'B', 'B'),
    (3, 'C', 'C'),
    (4, 'D', 'D')
;
/*!40000 ALTER TABLE serviceclass ENABLE KEYS */;
-- RESULT

/*****************************************************************************/

-- Adding service class reference to table "service" ... \
ALTER TABLE service 
    ADD COLUMN id_class int unsigned COMMENT 'FK: Service class' AFTER id,
    ADD FOREIGN KEY (id_class) REFERENCES serviceclass(id) ON DELETE SET NULL
;
-- NORESULT
UPDATE service SET id_class=CASE WHEN class='A' THEN 1 WHEN class='B' THEN 2 WHEN class='C' THEN 3 WHEN class='D' THEN 4 END;
-- RESULT
ALTER TABLE service DROP COLUMN class;
-- NORESULT

/*****************************************************************************/


-- Renaming table "scheduler" for recreation ... \
ALTER TABLE scheduler RENAME TO scheduler_old;
-- RESULT

CREATE TABLE scheduler (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned NOT NULL COMMENT 'FK: Owning user',
    id_service int unsigned COMMENT 'FK: Scheduled service',
    id_ce int unsigned COMMENT 'FK: Master Computing Element',
    id_pubserver int unsigned COMMENT 'FK: Publish server',
    id_class int unsigned COMMENT 'FK: Scheduler class',
    active boolean COMMENT 'True if scheduler is active',
    name varchar(100) NOT NULL COMMENT 'Unique name (unique for user)',
    caption varchar(100) NOT NULL COMMENT 'Caption',
    priority float COMMENT 'Priority value',
    compression VARCHAR(10) COMMENT 'Compression value',
    custom_url varchar(200) COMMENT 'Task definition URL (used if id_service is NULL)',
    ce_weight int unsigned default 0 COMMENT 'Computing Element weight',
    resource_perc tinyint default 0 COMMENT 'Resource percentage',
    mode tinyint unsigned COMMENT 'Scheduler mode: 0: parameter-driven, 1: data-driven',
    validity_start datetime COMMENT 'Validity start date/time',
    validity_end datetime COMMENT 'Validity end date/time',
    time_interval varchar(10) COMMENT 'Time interval length',
    min_files smallint unsigned default 1 COMMENT 'Minimum number of files to process',
    max_files smallint unsigned default 1 COMMENT 'Maximum number of files to process',
    last_processed_time datetime COMMENT 'Last date/time processed (between validity start and end)',
    last_exec_time datetime COMMENT 'Last execution time (actual time of execution)',
    last_status text COMMENT 'Last status message',
    PRIMARY KEY (id),
    UNIQUE INDEX (id_usr, name),
    FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE SET NULL,
    FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE SET NULL,
    FOREIGN KEY (id_class) REFERENCES schedulerclass(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Task schedulers';

-- Copy scheduler data ... \
INSERT INTO scheduler (id, id_usr, id_service, id_ce, id_pubserver, id_class, active, name, caption, priority, compression, custom_url, ce_weight, resource_perc, mode, validity_start, validity_end, time_interval, min_files, max_files, last_processed_time, last_exec_time, last_status)
SELECT id, id_usr, id_service, id_ce, id_pubserver, id_schedulerclass, active, name, caption, priority, compression, custom_url, ce_weight, resource_perc, type, validity_start, validity_end, time_interval, min_files, max_files, last_processed_time, last_exec_time, last_status FROM scheduler_old;
-- RESULT

-- Remove original table ... \
ALTER TABLE schedulerparam
  DROP FOREIGN KEY schedulerparam_ibfk_1,
    ADD FOREIGN KEY (id_scheduler) REFERENCES scheduler(id) ON DELETE CASCADE
;
ALTER TABLE task
  DROP FOREIGN KEY task_ibfk_5,
    ADD FOREIGN KEY (id_scheduler) REFERENCES scheduler(id) ON DELETE CASCADE
;
DROP TABLE scheduler_old;
-- RESULT

/*****************************************************************************/