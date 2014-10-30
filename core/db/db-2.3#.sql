/*****************************************************************************/

CREATE TABLE exttype (
    id int unsigned NOT NULL auto_increment,
    id_module int unsigned COMMENT 'FK: Implementing module',
    entity_code smallint unsigned COMMENT 'Entity type code to distinguish different entity types',
    pos smallint unsigned COMMENT 'Position for ordering',
    fullname varchar(100) NOT NULL COMMENT 'Fully qualified name of Mono class',
    caption varchar(50) COMMENT 'Caption',
    CONSTRAINT pk_module PRIMARY KEY (id),
    CONSTRAINT fk_exttype_module FOREIGN KEY (id_module) REFERENCES module(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Classes for entity type extensions';
-- CHECKPOINT 2.2-01a

-- Initializing basic entity type extensions ... \ 
INSERT INTO exttype (entity_code, pos, fullname, caption) VALUES
    (2, 1, 'Terradue.Portal.TimeDrivenScheduler, portal-core-library', 'Time-driven scheduler'),
    (2, 2, 'Terradue.Portal.DataDrivenScheduler, portal-core-library', 'Data-driven scheduler'),
    (4, 1, 'Terradue.Portal.GlobusComputingElement, portal-core-library', 'LGE/Globus Computing Element')
;
-- RESULT
-- CHECKPOINT 2.2-01b

/*****************************************************************************/

-- Changing structure of table "grp" (flag for administrator group) ... \
ALTER TABLE grp
    ADD COLUMN all_resources boolean default false COMMENT 'If true, new resources are automatically added to group'
;
-- RESULT

-- Setting administrator flag for administrator groups ... \
UPDATE grp SET all_resources = (name LIKE 'Admin%');
-- RESULT ... \

/*****************************************************************************/

-- Changing structure of table "series" (flag for manual assignment to services) ... \
ALTER TABLE series
    ADD COLUMN manual_assign boolean COMMENT 'If true, series is not assigned automatically services' AFTER logo_url
;
-- RESULT
-- CHECKPOINT 2.2-02

/*****************************************************************************/

-- Changing structure of table "service" (flag for automatical assignment of all non-manual series) ... \
ALTER TABLE service
    ADD COLUMN all_input boolean COMMENT 'If true, service accepts all non-manual series as input' AFTER rating
;
-- RESULT
-- CHECKPOINT 2.2-03

/*****************************************************************************/

CREATE PROCEDURE link_service_category(IN service_id int unsigned, IN category_name varchar(50), IN category_caption varchar(100))
COMMENT 'Inserts/updates a service category and links it to a service'
BEGIN
    DECLARE category_id int unsigned;
    DECLARE c int;

    SELECT id FROM servicecategory WHERE name = category_name INTO category_id;
    IF category_id IS NULL THEN
        INSERT INTO servicecategory (name, caption) VALUES (category_name, category_caption);
        SELECT LAST_INSERT_ID() INTO category_id;
    END IF;

    SELECT COUNT(*) FROM service_category AS t WHERE t.id_service = service_id AND t.id_category = category_id INTO c;
    IF c = 0 THEN
        INSERT INTO service_category (id_service, id_category) VALUES (service_id, category_id);
    END IF;
END;
-- CHECKPOINT 2.2-04a

CREATE PROCEDURE unlink_service_category(IN service_id int unsigned, IN category_name varchar(50))
COMMENT 'Unlinks a service category from a service'
BEGIN
    DELETE FROM service_category WHERE id_service = service_id AND id_category = (SELECT id FROM servicecategory WHERE name = category_name); 
END;
-- CHECKPOINT 2.2-04b

/*****************************************************************************/

CREATE PROCEDURE link_service_series(IN service_id int unsigned, IN series_name varchar(50), IN series_caption varchar(200), IN series_cat_description varchar(200), IN is_default_series boolean)
COMMENT 'Inserts/updates an input series and links it to a service'
BEGIN
    DECLARE series_id int unsigned;
    DECLARE c int;

    SELECT id FROM series WHERE name = series_name INTO series_id;
    IF series_id IS NULL THEN
        INSERT INTO series (name, caption, cat_description) VALUES (series_name, series_caption, series_cat_description);
        SELECT LAST_INSERT_ID() INTO series_id;
    END IF;

    SELECT COUNT(*) FROM service_series AS t WHERE t.id_service = service_id AND t.id_series = series_id INTO c;
    IF c = 0 THEN
        INSERT INTO service_series (id_service, id_series) VALUES (service_id, series_id);
    END IF;
    
    IF is_default_series THEN
        UPDATE service_series SET is_default = (id_series = series_id) WHERE id_service = service_id; 
    END IF;
END;
-- CHECKPOINT 2.2-05a

CREATE PROCEDURE unlink_service_series(IN service_id int unsigned, IN series_name varchar(50))
COMMENT 'Unlinks an input series from a service'
BEGIN
    DELETE FROM service_series WHERE id_service = service_id AND id_series = (SELECT id FROM series WHERE name = series_name); 
END;
-- CHECKPOINT 2.2-05b

/*****************************************************************************/

-- Changing structure of table "usr" ... \ 
ALTER TABLE usr
    CHANGE COLUMN enabled status tinyint default 4 COMMENT 'Account status, see lookup list "accountStatus"',
    CHANGE COLUMN ext_login allow_ext_login boolean COMMENT 'If true, external authentication is allowed' AFTER allow_password,
    CHANGE COLUMN sessionless allow_sessionless boolean COMMENT 'If true, sessionless requests from trusted hosts are allowed' AFTER allow_ext_login,
    CHANGE COLUMN trusted_only force_trusted boolean COMMENT 'If true, only connections from trusted hosts are allowed' AFTER allow_sessionless,
    ADD COLUMN force_ssl boolean default '0' COMMENT 'If true, accept only SSL authentication' AFTER force_trusted,
    CHANGE COLUMN task_storage_period task_storage_period smallint unsigned COMMENT 'Maximum lifetime of concluded tasks, 0 if endless' AFTER credits,
    CHANGE COLUMN publish_folder_size publish_folder_size int unsigned COMMENT 'Maximum size of user''s publish folder' AFTER task_storage_period,
    DROP COLUMN last_login_time
;
-- RESULT
-- CHECKPOINT 2.2-06

/*****************************************************************************/

-- Changing structure of table "pubserver" (removing not-null constraint for upload URL) ... \ 
ALTER TABLE pubserver
    CHANGE COLUMN upload_url upload_url varchar(200) COMMENT 'URL for task result upload'
;
-- RESULT
-- CHECKPOINT 2.2-07

/*****************************************************************************/

-- Changing account status lookup list ... \ 
SET @list = (SELECT id FROM lookuplist WHERE name='enabledStatus'); 
UPDATE lookuplist SET name='accountStatus' WHERE id=@list;
UPDATE lookup SET pos=5, value='4' WHERE id_list=@list AND value='2';
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list, 3, 'Waiting for activation', '2'),
    (@list, 4, 'Password reset requested', '3')
;
-- RESULT
-- CHECKPOINT 2.2-08

-- TRY START
DROP TRIGGER IF EXISTS usr_log_trigger_insert;
DROP TRIGGER IF EXISTS usr_log_trigger_update;
DROP TRIGGER IF EXISTS usr_log_trigger_delete;
-- TRY END

-- Adjusting user account status values ... \
UPDATE usr SET status=4 WHERE status=2;
-- RESULT
-- CHECKPOINT 2.2-09

/*****************************************************************************/

-- Renaming table "ce" to "cr" ... \
ALTER TABLE cedir DROP FOREIGN KEY fk_cedir_ce;
ALTER TABLE cestate DROP FOREIGN KEY fk_cestate_ce;
ALTER TABLE product DROP FOREIGN KEY fk_product_ce;
ALTER TABLE service_ce DROP PRIMARY KEY, DROP FOREIGN KEY fk_service_ce_service, DROP FOREIGN KEY fk_service_ce_ce;
ALTER TABLE privilege DROP FOREIGN KEY fk_privilege_ce;
ALTER TABLE scheduler DROP FOREIGN KEY fk_scheduler_ce;
ALTER TABLE task DROP FOREIGN KEY fk_task_ce;
-- CHECKPOINT 2.2-10a
ALTER TABLE ce RENAME TO cr;
-- CHECKPOINT 2.2-10b
ALTER TABLE cr CHANGE COLUMN id id int unsigned NOT NULL;
-- CHECKPOINT 2.2-10c
ALTER TABLE cr ADD COLUMN id_exttype int unsigned AFTER id;
-- CHECKPOINT 2.2-10d
UPDATE cr SET id_exttype=(SELECT id FROM exttype WHERE fullname='Terradue.Portal.GlobusComputingElement, portal-core-library');
-- CHECKPOINT 2.2-10e
ALTER TABLE cr CHANGE COLUMN id_exttype id_exttype int unsigned NOT NULL COMMENT 'FK: Entity type extension', ADD CONSTRAINT fk_cr_exttype FOREIGN KEY (id_exttype) REFERENCES exttype(id) ON DELETE CASCADE;
-- CHECKPOINT 2.2-10f
ALTER TABLE cr DROP PRIMARY KEY, CHANGE COLUMN id id int unsigned NOT NULL auto_increment, ADD CONSTRAINT pk_cr PRIMARY KEY (id);
-- CHECKPOINT 2.2-10g
ALTER TABLE cr COMMENT 'Computing resources';
-- RESULT
-- CHECKPOINT 2.2-10h

/*****************************************************************************/

CREATE TABLE lge (
    id int unsigned NOT NULL auto_increment,
    caption varchar(50) COMMENT 'Caption',
    ws_url varchar(100) NOT NULL COMMENT 'wsServer web service access point',
    myproxy_address varchar(100) NOT NULL COMMENT 'Hostname of MyProxy server',
    status_method tinyint COMMENT 'Task and job status request method',
    task_status_url varchar(100) COMMENT 'URL of task status document',
    job_status_url varchar(100) COMMENT 'URL of job status document',
    ce_status_url varchar(100) COMMENT 'Grid status document URL',
    conf_file varchar(100) COMMENT 'Location of configuration file',
    CONSTRAINT pk_ce PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'LGE instances for Globus Computing Elements';
-- CHECKPOINT 2.2-11

SET @ws_url = (SELECT value FROM config WHERE name='GridEngineAccessPoint');
SET @myproxy_address = (SELECT value FROM config WHERE name='MyProxyServer');
SET @status_method = (SELECT value FROM config WHERE name='StatusRequestMethod');
SET @task_status_url = (SELECT value FROM config WHERE name='TaskDetailsUrl');
SET @job_status_url = (SELECT value FROM config WHERE name='JobDetailsUrl');
SET @ce_status_url = (SELECT value FROM config WHERE name='GridStatusUrl');
SET @conf_file = (SELECT value FROM config WHERE name='SiteConfigFile');

INSERT INTO lge (caption, ws_url, myproxy_address, status_method, task_status_url, job_status_url, ce_status_url, conf_file) VALUES
    ('Default LGE', @ws_url, @myproxy_address, @status_method, @task_status_url, @job_status_url, @ce_status_url, @conf_file)
;

SET @id_lge = (SELECT LAST_INSERT_ID());
-- CHECKPOINT 2.2-12

/*****************************************************************************/

-- Renaming configuration variables and removing obsolete configuration variables ... \
UPDATE config SET name='ComputingResourceStatusValidity', caption='Computing Resource Status Validity', hint='Enter the time period after which the capacity information of a computing resource expires' WHERE name='ComputingElementStatusValidity';
DELETE FROM config WHERE name IN ('DbVersion', 'DbVersionCheckpoint', 'GridEngineAccessPoint', 'MyProxyServer', 'StatusRequestMethod', 'TaskDetailsUrl', 'JobDetailsUrl', 'GridStatusUrl', 'SiteConfigFile');
-- RESULT
UPDATE config SET pos=pos-3 WHERE id_section=(SELECT id FROM configsection WHERE caption='Tasks') AND pos>3;
UPDATE config SET pos=pos-1 WHERE id_section=(SELECT id FROM configsection WHERE caption='Tasks') AND pos>4;
UPDATE config SET pos=pos-2 WHERE id_section=(SELECT id FROM configsection WHERE caption='Processing') AND pos>2;
-- NORESULT
-- CHECKPOINT 2.2-13

/*****************************************************************************/

CREATE TABLE ce (
    id int unsigned NOT NULL,
    id_lge int unsigned,
    ce_port smallint unsigned COMMENT 'CE port',
    gsi_port smallint unsigned COMMENT 'GSI port',
    job_manager varchar(100) COMMENT 'Job manager',
    flags varchar(100) COMMENT 'Flags',
    grid_type varchar(200) COMMENT 'Grid type',
    job_queue varchar(200) COMMENT 'Job queue',
    status_method tinyint default 0 COMMENT 'Status request method',
    status_url varchar(100) COMMENT 'URL for status information',
    CONSTRAINT pk_ce PRIMARY KEY (id),
    CONSTRAINT fk_ce_cr FOREIGN KEY (id) REFERENCES cr(id) ON DELETE CASCADE,
    CONSTRAINT fk_ce_lge FOREIGN KEY (id_lge) REFERENCES lge(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Globus Computing Elements';
-- CHECKPOINT 2.2-14

/*****************************************************************************/

-- Copying data into Globus Computing Element extension table ... \
SET @id_lge = CASE WHEN @id_lge IS NULL THEN (SELECT MAX(id) FROM lge) ELSE @id_lge END;
/*!40000 ALTER TABLE ce DISABLE KEYS */;
INSERT INTO ce (id, id_lge, ce_port, gsi_port, job_manager, flags, grid_type, job_queue, status_method, status_url)
SELECT id, @id_lge, ce_port, gsi_port, job_manager, flags, grid_type, job_queue, status_method, status_url FROM cr;
/*!40000 ALTER TABLE ce ENABLE KEYS */;
-- RESULT
-- CHECKPOINT 2.2-15

-- Changing structure of table "cr" ... \
ALTER TABLE cr
    DROP FOREIGN KEY fk_ce_ce_monitor,
    DROP COLUMN id_ce_monitor,
    CHANGE COLUMN address hostname varchar(100) NOT NULL COMMENT 'Hostname',
    DROP COLUMN ce_port,
    DROP COLUMN gsi_port,
    DROP COLUMN job_manager,
    DROP COLUMN flags,
    DROP COLUMN grid_type,
    DROP COLUMN job_queue,
    DROP COLUMN status_method,
    DROP COLUMN status_url
;
-- RESULT
-- CHECKPOINT 2.2-16

/*****************************************************************************/

-- Changing structure of table "cedir" ... \
ALTER TABLE cedir
    CHANGE COLUMN id_ce id_ce int unsigned NOT NULL COMMENT 'Globus Computing Element (FK)',
    ADD CONSTRAINT fk_cedir_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE CASCADE
;
ALTER TABLE cedir COMMENT 'Globus Computing Element special directories';
-- RESULT
-- CHECKPOINT 2.2-17

/*****************************************************************************/

-- Renaming table "cestate" to "crstate" ... \
ALTER TABLE cestate RENAME TO crstate;
-- RESULT
-- CHECKPOINT 2.2-18

-- Changing structure of table "cestate" ... \
ALTER TABLE crstate COMMENT 'Computing resource state information';
ALTER TABLE crstate
    CHANGE COLUMN id_ce id_cr int unsigned NOT NULL COMMENT 'Computing resource (PK+FK)',
    CHANGE COLUMN total_nodes total_nodes int unsigned COMMENT 'Total capacity available on computing resource',
    CHANGE COLUMN free_nodes free_nodes int unsigned COMMENT 'Free capacity available on computing resource',
    DROP PRIMARY KEY,
    ADD CONSTRAINT pk_crstate PRIMARY KEY (id_cr),
    ADD CONSTRAINT fk_crstate_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT 2.2-19

/*****************************************************************************/

-- Changing triggers for table "cestate" ... \
DROP TRIGGER IF EXISTS cestate_insert;
CREATE TRIGGER crstate_insert BEFORE INSERT ON crstate FOR EACH ROW
BEGIN
    SET NEW.modified=utc_timestamp();
END;

DROP TRIGGER IF EXISTS cestate_update;
CREATE TRIGGER crstate_update BEFORE UPDATE ON crstate FOR EACH ROW
BEGIN
    SET NEW.modified=utc_timestamp();
END;
--RESULT
-- CHECKPOINT 2.2-20

/*****************************************************************************/

-- Changing structure of table "product" ... \
ALTER TABLE product
    CHANGE COLUMN id_ce id_cr int unsigned COMMENT 'FK: Computing resource',
    CHANGE COLUMN ge_session remote_id varchar(100) NOT NULL COMMENT 'Original remote identifier',
    ADD CONSTRAINT fk_product_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT 2.2-21

/*****************************************************************************/

-- Renaming "service_ce" to "service_cr" ... \
ALTER TABLE service_ce RENAME TO service_cr;
-- RESULT
-- CHECKPOINT 2.2-22

-- Changing structure of table "service_cr" ... \
ALTER TABLE service_cr
    CHANGE COLUMN id_ce id_cr int unsigned NOT NULL COMMENT 'FK: Computing resource',
    CHANGE COLUMN is_default is_default boolean COMMENT 'If true, computing resource is default for service',
    ADD CONSTRAINT pk_service_cr PRIMARY KEY (id_service, id_cr),
    ADD CONSTRAINT fk_service_cr_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_service_cr_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT 2.2-23

/*****************************************************************************/

-- Changing structure of table "privilege" ... \
ALTER TABLE privilege
    CHANGE COLUMN id_ce id_cr int unsigned COMMENT 'FK: Computing resource',
    ADD CONSTRAINT fk_privilege_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT 2.2-24

/*****************************************************************************/

-- Converting scheduler modes to entity type extensions ... \ 
UPDATE scheduler SET mode=(SELECT id FROM exttype WHERE fullname='Terradue.Portal.TimeDrivenScheduler, portal-core-library') WHERE mode=1;
UPDATE scheduler SET mode=(SELECT id FROM exttype WHERE fullname='Terradue.Portal.DataDrivenScheduler, portal-core-library') WHERE mode=2;
-- RESULT
-- CHECKPOINT 2.2-25a

-- Changing structure of table "scheduler" ... \
ALTER TABLE scheduler
    CHANGE COLUMN mode id_exttype int unsigned NOT NULL COMMENT 'FK: Entity type extension' AFTER id,
    CHANGE COLUMN id_ce id_cr int unsigned COMMENT 'FK: Computing resource',
    CHANGE COLUMN max_ce_usage max_cr_usage tinyint default 0 COMMENT 'Maximum usage (in %) of computing resource for tasks of the scheduler',
    CHANGE COLUMN ignore_ce_load ignore_cr_load boolean COMMENT 'If true, scheduler is supposed to ignore the existing load on the computing resource',
    ADD CONSTRAINT fk_scheduler_exttype FOREIGN KEY (id_exttype) REFERENCES exttype(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_scheduler_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE SEt NULL
;
-- RESULT
-- CHECKPOINT 2.2-25b

/*****************************************************************************/

-- Changing structure of table "task" ... \
ALTER TABLE task
    CHANGE COLUMN id_ce id_cr int unsigned COMMENT 'FK: Computing resource',
    CHANGE COLUMN ge_session remote_id varchar(50) COMMENT 'Original remote identifier',
    ADD CONSTRAINT fk_task_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE SET NULL
;
-- RESULT
-- CHECKPOINT 2.2-26

/*****************************************************************************/

-- Changing structure of table "job" ... \
ALTER TABLE job
    DROP COLUMN grid_type
;
-- RESULT
-- CHECKPOINT 2.2-27

/*****************************************************************************/

-- Changing structure of table "jobnode" ... \
ALTER TABLE jobnode COMMENT 'Job processings on a computing resource node';
-- RESULT
-- CHECKPOINT 2.2-28

/*****************************************************************************/

CREATE TABLE usrcert (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    cert_content varbinary(10000) COMMENT 'Binary certificate content (P12)',
    cert_content_hex varchar(10000) COMMENT 'Hexadecimal certificate content (P12)',
    CONSTRAINT pk_usrcert PRIMARY KEY (id_usr),
    CONSTRAINT fk_usrcert_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User proxy certificates';

/*****************************************************************************/
