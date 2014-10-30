/*****************************************************************************/

ALTER TABLE action CHANGE COLUMN enabled enabled boolean COMMENT 'True if action is executed automatically';

/*****************************************************************************/

-- Changing structure of table "usr" ... \
ALTER TABLE usr
    ADD COLUMN active boolean DEFAULT true COMMENT "True if user account is active" AFTER id,
    CHANGE COLUMN local_only local_only boolean COMMENT 'True if only local access is allowed (e.g. generated users)',
    DROP COLUMN session,
    DROP COLUMN session_expire_time
;

UPDATE usr SET active=true;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usrsession" ... \
ALTER TABLE usrsession DROP COLUMN session;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "pubserver" ... \
ALTER TABLE pubserver ADD COLUMN caption varchar(50) NOT NULL COMMENT 'Caption' AFTER id_usr;
UPDATE pubserver SET caption=hostname;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "cedir" ... \
ALTER TABLE cedir CHANGE COLUMN available available boolean default true COMMENT 'True if directory is available';
-- RESULT

/*****************************************************************************/

-- Changing structure of table "producttype" ... \
ALTER TABLE producttype
    CHANGE COLUMN rolling rolling boolean COMMENT 'True if product type is part of a rolling list',
    CHANGE COLUMN public public boolean COMMENT 'True if product type is viewable by the public'
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "service" and adding custom columns ... \
ALTER TABLE service
    CHANGE COLUMN available available boolean COMMENT 'True if service is available',
    ADD COLUMN custom_field_1 varchar(255),
    ADD COLUMN custom_field_2 varchar(255),
    ADD COLUMN custom_field_3 varchar(255),
    ADD COLUMN custom_field_4 varchar(255),
    ADD COLUMN custom_field_5 varchar(255)
;
-- RESULT

/*****************************************************************************/

CREATE TABLE servicecategory (
    id int unsigned NOT NULL auto_increment,
    name varchar(50) NOT NULL COMMENT 'Unique name',
    caption varchar(100) NOT NULL COMMENT 'Service category caption',
    PRIMARY KEY (id),
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Categories of processing services';

-- Initializing categories of processing services ... \
/*!40000 ALTER TABLE servicecategory DISABLE KEYS */;
INSERT INTO servicecategory (id, name, caption) VALUES
    (1, 'LAND', 'Land'),
    (2, 'MARINE', 'Marine'),
    (3, 'ATMOSPHERE', 'Atmosphere'),
    (4, 'SECURITY', 'Security'),
    (5, 'EMERGENCY', 'Emergency Response')
;
/*!40000 ALTER TABLE servicecategory ENABLE KEYS */;
-- RESULT

/*****************************************************************************/

CREATE TABLE service_category (
    id_service int unsigned NOT NULL COMMENT 'FK: Service',
    id_category int unsigned NOT NULL COMMENT 'FK: Service category',
    PRIMARY KEY (id_service, id_category),
    FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    FOREIGN KEY (id_category) REFERENCES servicecategory(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of services to service categories';

/*****************************************************************************/

-- Changing structure of table "service_series" ... \
ALTER TABLE service_series CHANGE COLUMN is_default is_default boolean COMMENT 'True if series is default for service';
-- RESULT

-- Changing structure of table "service_ce" ... \
ALTER TABLE service_ce CHANGE COLUMN is_default is_default boolean COMMENT 'True if Computing Element is default for service';
-- RESULT

-- Changing structure of table "privilege" ... \
ALTER TABLE privilege CHANGE COLUMN allow allow boolean default true COMMENT 'Allow (true) or deny (false) usage of resource combination';
-- RESULT

-- Changing structure of table "taskgroup" ... \
ALTER TABLE taskgroup CHANGE COLUMN one_task one_task boolean COMMENT 'True if task group has only one task';
-- RESULT

-- Changing structure of table "scheduler" ... \
ALTER TABLE scheduler CHANGE COLUMN active active boolean COMMENT 'True if scheduler is active';
-- RESULT

-- Changing structure of table "taskparam" ... \
ALTER TABLE taskparam CHANGE COLUMN metadata metadata boolean COMMENT 'True if parameter is relevant metadata for users';
-- RESULT



-- Changing structure of table "task" ... \
ALTER TABLE task CHANGE COLUMN uid uid varchar(50) COMMENT 'Unique task ID (automatically generated UID)';
-- RESULT

/*****************************************************************************/

-- Changing configuration settings ... \
UPDATE config SET pos=pos+1 WHERE id_configsection=2 AND pos>=3 OR id_configsection=5;
-- NORESULT

INSERT INTO config (id_configsection, pos, name, type, caption, hint, value) VALUES
    (2, 3, 'ServiceParamFileRoot', 'string', 'Service Parameter Root Folder', 'Enter the root folder containing the service configurable parameter values in files on the web portal machine''s file system (in the pattern attribute of a service''s <param> element, the "$PARAMROOT" placeholder represents this value)', '$SERVICEROOT$'),
    (2, 8, 'ChangeLogValidity', 'timespan', 'Change Log Conservation Time', 'Enter the maximum conservation time of change log records (e.g. 1M, 10D)', '3M'),
    (5, 1, 'GridStatusUrl', 'url', 'Grid Status URL (Ganglia)', 'Enter the URL of the Grid status XML generated by Ganglia', NULL)
;
UPDATE config SET name='SyncTaskOperations', caption='Perform Task Operations Synchronously', hint='If checked tasks are submitted directly to the Grid Engine or deleted upon user request, otherwise the background daemon does the actual operation later', value=NOT value WHERE name='SubmitToGridEngine';
UPDATE config SET value='NOCMP:None;COMPRESS:Single File;TGZ:Unique Package' WHERE name='CompressionValues';
-- RESULT

/*****************************************************************************/

-- Adding log cleanup action ... \
INSERT INTO action (id, name, caption) VALUES
    (5, 'logdelete', 'Log cleanup')
;
-- RESULT

/*****************************************************************************/

CREATE TABLE usr_log (
    /* log table specific fields */
    log_id int unsigned NOT NULL auto_increment,
    log_type char(1) NOT NULL COMMENT 'Operation type: ''I'': insert, ''U'': update, ''D'': delete',
    log_time datetime COMMENT 'Log insertion date/time',
    active_change char(1) COMMENT 'Active state change type: ''d'': disable, ''e'': enable',
    /* ------------------------- */
    id int unsigned NOT NULL,
    active tinyint(1) unsigned COMMENT "User account active state",
    username varchar(50) NOT NULL COMMENT 'Username',
    password varchar(50) COMMENT 'Password',
    firstname varchar(50) COMMENT 'First name',
    lastname varchar(50) COMMENT 'Last name',
    email varchar(100) COMMENT 'Email address',
    affiliation varchar(100) COMMENT 'Affiliation, organization etc.',
    country varchar(100) COMMENT 'Country',
    level tinyint unsigned default '1' COMMENT '1: User, 2: Developer, 3: Admin',
    local_only tinyint NOT NULL default '0' COMMENT 'Allow only local access (e.g. generated users)',
    resources int unsigned default '0' COMMENT 'Maximum resource credits for the user',
    proxy_username varchar(50) COMMENT 'Proxy username',
    proxy_password varchar(50) COMMENT 'Proxy password',
    cert_subject varchar(200) COMMENT 'Certificate subject for password-less login',
    task_storage_period smallint unsigned COMMENT 'Maximum lifetime of concluded tasks, 0 if endless',
    publish_folder_size int unsigned COMMENT 'Maximum size of user''s publish folder',
    session int(12) unsigned,
    last_login_time datetime COMMENT 'Most recent login date/time',
    session_expire_time datetime COMMENT 'Session validity end date/time',
    PRIMARY KEY (log_id)
) Engine=InnoDB COMMENT 'Users log table';

-- Creating triggers for user change logging ... \
CREATE TRIGGER usr_log_trigger_insert AFTER INSERT ON usr FOR EACH ROW
BEGIN
    INSERT INTO usr_log (log_type, log_time, id, active, username, password, firstname, lastname, email, affiliation, country, level, local_only, resources, proxy_username, proxy_password, cert_subject, task_storage_period, publish_folder_size, session, last_login_time, session_expire_time) VALUES ('I', now(), NEW.id, NEW.active, NEW.username, NEW.password, NEW.firstname, NEW.lastname, NEW.email, NEW.affiliation, NEW.country, NEW.level, NEW.local_only, NEW.resources, NEW.proxy_username, NEW.proxy_password, NEW.cert_subject, NEW.task_storage_period, NEW.publish_folder_size, NEW.session, NEW.last_login_time, NEW.session_expire_time);
END;

CREATE TRIGGER usr_log_trigger_update AFTER UPDATE ON usr FOR EACH ROW
BEGIN
    INSERT INTO usr_log (log_type, log_time, active_change, id, active, username, password, firstname, lastname, email, affiliation, country, level, local_only, resources, proxy_username, proxy_password, cert_subject, task_storage_period, publish_folder_size, session, last_login_time, session_expire_time) VALUES ('U', now(), CASE WHEN NEW.active=true AND OLD.active=false THEN 'e' WHEN NEW.active=false AND OLD.active=true THEN 'd' END, NEW.id, NEW.active, NEW.username, NEW.password, NEW.firstname, NEW.lastname, NEW.email, NEW.affiliation, NEW.country, NEW.level, NEW.local_only, NEW.resources, NEW.proxy_username, NEW.proxy_password, NEW.cert_subject, NEW.task_storage_period, NEW.publish_folder_size, NEW.session, NEW.last_login_time, NEW.session_expire_time);
END;

CREATE TRIGGER usr_log_trigger_delete AFTER DELETE ON usr FOR EACH ROW
BEGIN
    INSERT INTO usr_log (log_type, log_time, id, active, username, password, firstname, lastname, email, affiliation, country, level, local_only, resources, proxy_username, proxy_password, cert_subject, task_storage_period, publish_folder_size, session, last_login_time, session_expire_time) VALUES ('D', now(), OLD.id, OLD.active, OLD.username, OLD.password, OLD.firstname, OLD.lastname, OLD.email, OLD.affiliation, OLD.country, OLD.level, OLD.local_only, OLD.resources, OLD.proxy_username, OLD.proxy_password, OLD.cert_subject, OLD.task_storage_period, OLD.publish_folder_size, OLD.session, OLD.last_login_time, OLD.session_expire_time);
END;
-- RESULT

/*****************************************************************************/

CREATE TABLE grp_log (
    /* log table specific fields */
    log_id int unsigned NOT NULL auto_increment,
    log_type char(1) NOT NULL COMMENT 'Operation type: ''I'': insert, ''U'': update, ''D'': delete',
    log_time datetime COMMENT 'Log insertion date/time',
    /* ------------------------- */    
    id int unsigned NOT NULL,
    name varchar(50) NOT NULL COMMENT 'Unique identifier',
    descr text COMMENT 'Description',
    PRIMARY KEY (log_id)
) Engine=InnoDB COMMENT 'User groups log table';

-- Creating triggers for user group change logging ... \
CREATE TRIGGER grp_log_trigger_insert AFTER INSERT ON grp FOR EACH ROW
BEGIN
    INSERT INTO grp_log (log_type, log_time, id, name, descr) VALUES ('I', now(), NEW.id, NEW.name, NEW.descr);
END;

CREATE TRIGGER grp_log_trigger_update AFTER UPDATE ON grp FOR EACH ROW
BEGIN
    INSERT INTO grp_log (log_type, log_time, id, name, descr) VALUES ('U', now(), NEW.id, NEW.name, NEW.descr);
END;

CREATE TRIGGER grp_log_trigger_delete AFTER DELETE ON grp FOR EACH ROW
BEGIN
    INSERT INTO grp_log (log_type, log_time, id, name, descr) VALUES ('D', now(), OLD.id, OLD.name, OLD.descr);
END;
-- RESULT

/*****************************************************************************/

CREATE TABLE ce_log (
    /* log table specific fields */
    log_id int unsigned NOT NULL auto_increment,
    log_type char(1) NOT NULL COMMENT 'Operation type: ''I'': insert, ''U'': update, ''D'': delete',
    log_time datetime COMMENT 'Log insertion date/time',
    /* ------------------------- */
    id int unsigned NOT NULL,
    id_ce_monitor int unsigned COMMENT 'FK: URL of monitoring Computing Element',
    caption varchar(100) NOT NULL COMMENT 'Caption',
    descr text COMMENT 'Description',
    address varchar(100) NOT NULL COMMENT 'Hostname',
    ce_port smallint unsigned COMMENT 'CE port',
    gsi_port smallint unsigned COMMENT 'GSI port',
    max_weight int unsigned NOT NULL default '0' COMMENT 'Maximum weight in %',
    job_manager varchar(100) COMMENT 'Job manager',
    flags varchar(100) COMMENT 'Flags',
    logo_url varchar(200) COMMENT 'URL of logo image',
    grid_type varchar(200) COMMENT 'Grid type',
    job_queue varchar(200) COMMENT 'Job queue',
    PRIMARY KEY (log_id)
) Engine=InnoDB COMMENT 'Computing Elements log table';

-- Creating triggers for Computing Element change logging ... \
CREATE TRIGGER ce_log_trigger_insert AFTER INSERT ON ce FOR EACH ROW
BEGIN
    INSERT INTO ce_log (log_type, log_time, id, id_ce_monitor, caption, descr, address, ce_port, gsi_port, max_weight, job_manager, flags, logo_url, grid_type, job_queue) VALUES ('I', now(), NEW.id, NEW.id_ce_monitor, NEW.caption, NEW.descr, NEW.address, NEW.ce_port, NEW.gsi_port, NEW.max_weight, NEW.job_manager, NEW.flags, NEW.logo_url, NEW.grid_type, NEW.job_queue);
END;

CREATE TRIGGER ce_log_trigger_update AFTER UPDATE ON ce FOR EACH ROW
BEGIN
    INSERT INTO ce_log (log_type, log_time, id, id_ce_monitor, caption, descr, address, ce_port, gsi_port, max_weight, job_manager, flags, logo_url, grid_type, job_queue) VALUES ('U', now(), NEW.id, NEW.id_ce_monitor, NEW.caption, NEW.descr, NEW.address, NEW.ce_port, NEW.gsi_port, NEW.max_weight, NEW.job_manager, NEW.flags, NEW.logo_url, NEW.grid_type, NEW.job_queue);
END;

CREATE TRIGGER ce_log_trigger_delete AFTER DELETE ON ce FOR EACH ROW
BEGIN
    INSERT INTO ce_log (log_type, log_time, id, id_ce_monitor, caption, descr, address, ce_port, gsi_port, max_weight, job_manager, flags, logo_url, grid_type, job_queue) VALUES ('D', now(), OLD.id, OLD.id_ce_monitor, OLD.caption, OLD.descr, OLD.address, OLD.ce_port, OLD.gsi_port, OLD.max_weight, OLD.job_manager, OLD.flags, OLD.logo_url, OLD.grid_type, OLD.job_queue);
END;
-- RESULT

/*****************************************************************************/

CREATE TABLE service_log (
    /* log table specific fields */
    log_id int unsigned NOT NULL auto_increment,
    log_type char(1) NOT NULL COMMENT 'Operation type: ''I'': insert, ''U'': update, ''D'': delete',
    log_time datetime COMMENT 'Log insertion date/time',
    /* ------------------------- */
    id int unsigned NOT NULL,
    available tinyint NOT NULL default '0' COMMENT 'Service is available',
    name varchar(50) NOT NULL COMMENT 'Unique identifier',
    caption varchar(200) NOT NULL COMMENT 'Caption',
    descr text NOT NULL COMMENT 'Description',
    root varchar(200) NOT NULL default 'Service root dir, location of service.xml, $SERVICEROOT is placeholder for service root dir',
    logo_url varchar(200) COMMENT 'Logo/icon URL',
    view_url varchar(200) COMMENT 'View URL',
    rating tinyint COMMENT 'Rating in stars (0 to 5)',
    class char(1) COMMENT 'Class (''A'', ''B'', ''C'')',
    PRIMARY KEY (log_id)
) Engine=InnoDB COMMENT 'Processing services log table';

-- Creating triggers for processing service change logging ... \
CREATE TRIGGER service_log_trigger_insert AFTER INSERT ON service FOR EACH ROW
BEGIN
    INSERT INTO service_log (log_type, log_time, id, available, name, caption, descr, root, logo_url, view_url, rating, class) VALUES ('I', now(), NEW.id, NEW.available, NEW.name, NEW.caption, NEW.descr, NEW.root, NEW.logo_url, NEW.view_url, NEW.rating, NEW.class);
END;

CREATE TRIGGER service_log_trigger_update AFTER UPDATE ON service FOR EACH ROW
BEGIN        INSERT INTO service_log (log_type, log_time, id, available, name, caption, descr, root, logo_url, view_url, rating, class) VALUES ('U', now(), NEW.id, NEW.available, NEW.name, NEW.caption, NEW.descr, NEW.root, NEW.logo_url, NEW.view_url, NEW.rating, NEW.class);
END;

CREATE TRIGGER service_log_trigger_delete AFTER DELETE ON service FOR EACH ROW
BEGIN
    INSERT INTO service_log (log_type, log_time, id, available, name, caption, descr, root, logo_url, view_url, rating, class) VALUES ('D', now(), OLD.id, OLD.available, OLD.name, OLD.caption, OLD.descr, OLD.root, OLD.logo_url, OLD.view_url, OLD.rating, OLD.class);
END;
-- RESULT

/*****************************************************************************/

CREATE TABLE task_log (
    /* log table specific fields */
    log_id int unsigned NOT NULL auto_increment,
    log_type char(1) NOT NULL COMMENT 'Operation type: ''I'': insert, ''U'': update, ''D'': delete',
    log_time datetime COMMENT 'Log insertion date/time',
    /* ------------------------- */
    id int unsigned NOT NULL,
    id_usr int unsigned COMMENT 'FK: Owning user',
    id_service int unsigned COMMENT 'FK: Service',
    id_ce int unsigned COMMENT 'FK: Master Computing Element',
    id_pubserver int unsigned COMMENT 'FK: Publish server',
    id_scheduler int unsigned COMMENT 'FK: Related service scheduler (optional)',
    id_taskgroup int unsigned COMMENT 'FK: Related task group (optional)',
    uid varchar(50) NOT NULL COMMENT 'Unique task ID (automatically generated UID)',
    caption varchar(200) NOT NULL COMMENT 'Task caption',
    priority float COMMENT 'Priority value',
    resources double COMMENT 'Consumed resources',
    compression VARCHAR(10) COMMENT 'Compression value',
    status tinyint unsigned COMMENT 'Most recent status',
    next_status tinyint unsigned COMMENT 'Desired next status',
    ge_session varchar(50) COMMENT 'Grid session ID',
    creation_time datetime NOT NULL COMMENT 'Date/time of task creation',
    scheduled_time datetime COMMENT 'Date/time of scheduled execution',
    start_time datetime COMMENT 'Date/time of submission',
    end_time datetime COMMENT 'Date/time of completion or failure',
    access_time datetime COMMENT 'Date/time of last access',
    PRIMARY KEY (log_id)
) Engine=InnoDB COMMENT 'Processing tasks log table';

-- Creating triggers for processing task change logging ... \
CREATE TRIGGER task_log_trigger_insert AFTER INSERT ON task FOR EACH ROW
BEGIN
     INSERT INTO task_log (log_type, log_time, id, id_usr, id_service, id_ce, id_pubserver, id_scheduler, id_taskgroup, uid, caption, priority, resources, compression, status, next_status, ge_session, creation_time, scheduled_time, start_time, end_time, access_time) VALUES ('I', now(), NEW.id, NEW.id_usr, NEW.id_service, NEW.id_ce, NEW.id_pubserver, NEW.id_scheduler, NEW.id_taskgroup, NEW.uid, NEW.caption, NEW.priority, NEW.resources, NEW.compression, NEW.status, NEW.next_status, NEW.ge_session, NEW.creation_time, NEW.scheduled_time, NEW.start_time, NEW.end_time, NEW.access_time);
END;

CREATE TRIGGER task_log_trigger_update AFTER UPDATE ON task FOR EACH ROW
BEGIN
    IF NEW.status != OLD.status THEN
        INSERT INTO task_log (log_type, log_time, id, id_usr, id_service, id_ce, id_pubserver, id_scheduler, id_taskgroup, uid, caption, priority, resources, compression, status, next_status, ge_session, creation_time, scheduled_time, start_time, end_time, access_time) VALUES ('U', now(), NEW.id, NEW.id_usr, NEW.id_service, NEW.id_ce, NEW.id_pubserver, NEW.id_scheduler, NEW.id_taskgroup, NEW.uid, NEW.caption, NEW.priority, NEW.resources, NEW.compression, NEW.status, NEW.next_status, NEW.ge_session, NEW.creation_time, NEW.scheduled_time, NEW.start_time, NEW.end_time, NEW.access_time);
    END IF;
END;

CREATE TRIGGER task_log_trigger_delete AFTER DELETE ON task FOR EACH ROW
BEGIN
     INSERT INTO task_log (log_type, log_time, id, id_usr, id_service, id_ce, id_pubserver, id_scheduler, id_taskgroup, uid, caption, priority, resources, compression, status, next_status, ge_session, creation_time, scheduled_time, start_time, end_time, access_time) VALUES ('D', now(), OLD.id, OLD.id_usr, OLD.id_service, OLD.id_ce, OLD.id_pubserver, OLD.id_scheduler, OLD.id_taskgroup, OLD.uid, OLD.caption, OLD.priority, OLD.resources, OLD.compression, OLD.status, OLD.next_status, OLD.ge_session, OLD.creation_time, OLD.scheduled_time, OLD.start_time, OLD.end_time, OLD.access_time);
END;
-- RESULT

/*****************************************************************************/