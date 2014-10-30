USE $MAIN$;

/*****************************************************************************/

-- Temporarily removing stored procedures ... \
DROP PROCEDURE IF EXISTS add_servicecategory;
DROP PROCEDURE IF EXISTS add_series;
DROP PROCEDURE IF EXISTS link_service_series;
DROP PROCEDURE IF EXISTS unlink_service_series;
DROP PROCEDURE IF EXISTS link_service_category;
DROP PROCEDURE IF EXISTS unlink_service_category;
-- RESULT

CREATE TABLE basetype (
    id int unsigned NOT NULL auto_increment,
    id_module int unsigned COMMENT 'FK: Implementing module',
    pos smallint unsigned COMMENT 'Position for ordering',
    name varchar(50) NOT NULL COMMENT 'Unique name',
    class_name varchar(100) NOT NULL COMMENT 'Fully qualified name of the non-abstract .NET/Mono class',
    generic_class varchar(100) COMMENT 'Class to be instantiated if original class is abstract',
    custom_class varchar(100) COMMENT 'Class to be instantiated instead of original class',
    caption varchar(100) NOT NULL COMMENT 'Caption displayed in admin index page',
    keyword varchar(50) NOT NULL COMMENT 'Keyword used in admin interface URLs',
    is_composite bool NOT NULL default 0 COMMENT 'True if the item is a composite of several records',
    has_domain bool NOT NULL default 0 COMMENT 'True if items of type can be owned by a domain',
    has_owner bool NOT NULL default 0 COMMENT 'True if items of type are or can be owned by a user',
    is_assigned bool NOT NULL default 0 COMMENT 'True if items of type can be assigned to users and groups',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon for admin index page',
    CONSTRAINT pk_basetype PRIMARY KEY (id), 
    CONSTRAINT fk_basetype_module FOREIGN KEY (id_module) REFERENCES module(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Entity base types';
-- CHECKPOINT C-01a

-- Initializing entity base types ... \
INSERT INTO basetype (id, pos, name, class_name, generic_class, caption, keyword, is_composite, has_domain, has_owner, is_assigned) VALUES
    (1, 1, 'config', 'Terradue.Portal.Configuration, portal-core-library', NULL, 'General Configuration', 'config', true, false, false, false),
    (2, 2, 'action', 'Terradue.Portal.Action, portal-core-library', NULL, 'Agent Actions', 'actions', false, false, false, false),
    (3, 3, 'application', 'Terradue.Portal.Application, portal-core-library', NULL, 'External Applications', 'applications', false, false, false, false),
    (4, 4, 'domain', 'Terradue.Portal.Domain, portal-core-library', NULL, 'Domains', 'domains', false, false, false, false),
    (5, 5, 'role', 'Terradue.Portal.ManagerRole, portal-core-library', NULL, 'Manager Roles', 'manager-roles', false, false, false, false),
    (6, 6, 'openidprovider', 'Terradue.Portal.OpenIdProvider, portal-core-library', NULL, 'OpenID Providers', 'openid-providers', false, false, false, false),
    (7, 7, 'lookuplist', 'Terradue.Portal.LookupList, portal-core-library', NULL, 'Shared Lookup Lists', 'lookup-lists', false, false, false, false),
    (8, 8, 'serviceclass', 'Terradue.Portal.ServiceClass, portal-core-library', NULL, 'Service Classes', 'service-classes', false, false, false, false),
    (9, 9, 'servicecategory', 'Terradue.Portal.ServiceCategory, portal-core-library', NULL, 'Service Categories', 'service-categories', false, false, false, false),
    (10, 10, 'schedulerclass', 'Terradue.Portal.SchedulerClass, portal-core-library', NULL, 'Scheduler Classes', 'scheduler-classes', false, false, false, false),
    (11, 11, 'usr', 'Terradue.Portal.User, portal-core-library', NULL, 'Users', 'users', false, true, false, false),
    (12, 12, 'grp', 'Terradue.Portal.Group, portal-core-library', NULL, 'Groups', 'groups', false, true, false, false),
    (13, 13, 'lge', 'Terradue.Portal.LightGridEngine, portal-core-library', NULL, 'LGE Instances', 'lge-instances', false, false, false, false),
    (14, 14, 'cr', 'Terradue.Portal.ComputingResource, portal-core-library', 'Terradue.Portal.GenericComputingResource, portal-core-library', 'Computing Resources', 'computing-resources', false, true, false, true),
    (15, 15, 'catalogue', 'Terradue.Portal.Catalogue, portal-core-library', NULL, 'Metadata catalogues', 'catalogues', false, true, false, false),
    (16, 16, 'series', 'Terradue.Portal.Series, portal-core-library', NULL, 'Dataset Series', 'series', false, true, false, true),
    (17, 17, 'producttype', 'Terradue.Portal.ProductType, portal-core-library', NULL, 'Product Types', 'product-types', false, true, false, true),
    (18, 18, 'pubserver', 'Terradue.Portal.PublishServer, portal-core-library', NULL, 'Publish Servers', 'publish-servers', false, true, true, true),
    (19, 19, 'service', 'Terradue.Portal.Service, portal-core-library', NULL, 'Services', 'services', false, true, false, true),
    (20, 20, 'scheduler', 'Terradue.Portal.Scheduler, portal-core-library', 'Terradue.Portal.GenericScheduler, portal-core-library', 'Task Schedulers', 'schedulers', false, false, true, false),
    (21, 21, 'task', 'Terradue.Portal.Task, portal-core-library', NULL, 'Tasks', 'tasks', false, false, true, false),
    (22, 22, 'article', 'Terradue.Portal.Article, portal-core-library', NULL, 'News', 'news', false, false, false, false),
    (23, 23, 'image', 'Terradue.Portal.Image, portal-core-library', NULL, 'Images', 'images', false, false, false, false),
    (24, 24, 'faq', 'Terradue.Portal.Faq, portal-core-library', NULL, 'F.A.Q.', 'faqs', false, false, false, false),
    (25, 25, 'project', 'Terradue.Portal.Project, portal-core-library', NULL, 'Projects', 'projects', false, false, false, false)
;
-- RESULT
-- CHECKPOINT C-01b

/*****************************************************************************/

-- Changing structure of table "exttype" (base type reference, class name) ... \
ALTER TABLE exttype
    ADD COLUMN id_basetype int unsigned COMMENT 'FK: Entity base type' AFTER id_module,
    CHANGE COLUMN fullname class_name varchar(100) NOT NULL COMMENT 'Fully qualified name of .NET/Mono class',
    ADD CONSTRAINT fk_exttype_basetype FOREIGN KEY (id_basetype) REFERENCES basetype(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-02

/*****************************************************************************/

-- Removing duplicate entity type extensions ... \
UPDATE exttype SET class_name='Terradue.Portal.GlobusComputingElement, portal-core-library' WHERE entity_code=4 AND pos=1;
UPDATE exttype SET class_name='Terradue.Portal.TimeDrivenScheduler, portal-core-library' WHERE entity_code=2 AND pos=1;
UPDATE exttype SET class_name='Terradue.Portal.DataDrivenScheduler, portal-core-library' WHERE entity_code=2 AND pos=2;
CREATE TEMPORARY TABLE temp_exttype (id int, entity_code int, class_name varchar(100));
CREATE TEMPORARY TABLE temp_exttype_duplicate (id int, id_new int, entity_code int);
INSERT INTO temp_exttype (id, entity_code, class_name) SELECT MIN(id), MIN(entity_code), class_name FROM exttype WHERE entity_code IN (1, 2, 3, 4, 5, 6, 7) GROUP BY class_name;
INSERT INTO temp_exttype_duplicate (id, id_new, entity_code) SELECT t.id, t1.id, t.entity_code FROM exttype AS t INNER JOIN temp_exttype AS t1 ON t.class_name=t1.class_name;
UPDATE cr AS t LEFT JOIN temp_exttype_duplicate AS t1 ON t1.entity_code=4 AND t.id_exttype=t1.id SET t.id_exttype=t1.id_new WHERE t1.id_new!=t1.id;
UPDATE scheduler AS t LEFT JOIN temp_exttype_duplicate AS t1 ON t1.entity_code=4 AND t.id_exttype=t1.id SET t.id_exttype=t1.id_new WHERE t1.id_new!=t1.id;
-- NORESULT
DELETE FROM exttype WHERE id IN (SELECT id FROM temp_exttype_duplicate WHERE id != id_new);
DROP TABLE temp_exttype;
DROP TABLE temp_exttype_duplicate;
-- RESULT
-- CHECKPOINT C-03

/*****************************************************************************/

-- Replacing entity codes with base type references ... \
UPDATE exttype SET id_basetype = 11 WHERE entity_code = 7;
UPDATE exttype SET id_basetype = 14 WHERE entity_code = 4;
UPDATE exttype SET id_basetype = 16 WHERE entity_code = 6;
UPDATE exttype SET id_basetype = 18 WHERE entity_code = 5;
UPDATE exttype SET id_basetype = 19 WHERE entity_code = 3;
UPDATE exttype SET id_basetype = 20 WHERE entity_code = 2;
UPDATE exttype SET id_basetype = 21 WHERE entity_code = 1;
-- RESULT
-- CHECKPOINT C-04

/*****************************************************************************/

CREATE TABLE priv (
    id int unsigned NOT NULL auto_increment,
    id_basetype int unsigned COMMENT 'FK: Entity type',
    operation char(1) NOT NULL COLLATE latin1_general_cs COMMENT 'Operation type (one-letter code: c|m|a|p|d|o|V|A)',
    pos smallint unsigned COMMENT 'Position for ordering',
    name varchar(50) NOT NULL,
    CONSTRAINT pk_priv PRIMARY KEY (id),
    CONSTRAINT fk_priv_basetype FOREIGN KEY (id_basetype) REFERENCES basetype(id) ON DELETE CASCADE,
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Manager privileges';
-- CHECKPOINT C-05a

-- Initializing manager privileges ... \
INSERT INTO priv (id_basetype, operation, pos, name) VALUES
    (11, 'v', 1, 'User: view'),
    (11, 'c', 2, 'User: create'),
    (11, 'm', 3, 'User: change'),
    (11, 'p', 4, 'User: make public'),
    (11, 'd', 5, 'User: delete'),
    (11, 'V', 6, 'User: view related'),
    (11, 'A', 7, 'User: assign global'),
    (12, 'v', 8, 'Group: view'),
    (12, 'c', 9, 'Group: create'),
    (12, 'm', 10, 'Group: change'),
    (12, 'p', 11, 'Group: make public'),
    (12, 'd', 12, 'Group: delete'),
    (12, 'V', 13, 'Group: view public'),
    (12, 'A', 14, 'Group: assign public'),
    (14, 'v', 15, 'Computing resource: view'),
    (14, 'c', 16, 'Computing resource: create'),
    (14, 'm', 17, 'Computing resource: change'),
    (14, 'p', 18, 'Computing resource: make public'),
    (14, 'd', 19, 'Computing resource: delete'),
    (14, 'V', 20, 'Computing resource: view public'),
    (14, 'A', 21, 'Computing resource: assign public'),
    (15, 'v', 22, 'Catalogue: view'),
    (15, 'c', 23, 'Catalogue: create'),
    (15, 'm', 24, 'Catalogue: change'),
    (15, 'p', 25, 'Catalogue: make public'),
    (15, 'd', 26, 'Catalogue: delete'),
    (15, 'V', 27, 'Catalogue: view public'),
    (15, 'A', 28, 'Catalogue: assign public'),
    (16, 'v', 29, 'Series: view'),
    (16, 'c', 30, 'Series: create'),
    (16, 'm', 31, 'Series: change'),
    (16, 'p', 32, 'Series: make public'),
    (16, 'd', 33, 'Series: delete'),
    (16, 'V', 34, 'Series: view public'),
    (16, 'A', 35, 'Series: assign public'),
    (17, 'v', 36, 'Product type: view'),
    (17, 'c', 37, 'Product type: create'),
    (17, 'm', 38, 'Product type: change'),
    (17, 'p', 39, 'Product type: make public'),
    (17, 'd', 40, 'Product type: delete'),
    (17, 'V', 41, 'Product type: view public'),
    (17, 'A', 42, 'Product type: assign public'),
    (18, 'v', 43, 'Publish server: view'),
    (18, 'c', 44, 'Publish server: create'),
    (18, 'm', 45, 'Publish server: change'),
    (18, 'p', 46, 'Publish server: make public'),
    (18, 'd', 47, 'Publish server: delete'),
    (18, 'V', 48, 'Publish server: view public'),
    (18, 'A', 49, 'Publish server: assign public'),
    (19, 'v', 50, 'Service: view'),
    (19, 'c', 51, 'Service: create'),
    (19, 'm', 52, 'Service: change'),
    (19, 'p', 53, 'Service: make public'),
    (19, 'd', 54, 'Service: delete'),
    (19, 'V', 55, 'Service: view public'),
    (19, 'A', 56, 'Service: assign public'),
    (20, 'm', 57, 'Scheduler: control'),
    (21, 'm', 58, 'Task: control'),
    (22, 'v', 59, 'Article: view'),
    (22, 'c', 60, 'Article: create'),
    (22, 'm', 61, 'Article: change'),
    (22, 'd', 62, 'Article: delete'),
    (23, 'v', 63, 'Image: view'),
    (23, 'c', 64, 'Image: create'),
    (23, 'm', 65, 'Image: change'),
    (23, 'd', 66, 'Image: delete'),
    (24, 'v', 67, 'FAQ: view'),
    (24, 'c', 68, 'FAQ: create'),
    (24, 'm', 69, 'FAQ: change'),
    (24, 'd', 70, 'FAQ: delete'),
    (25, 'v', 71, 'Project: view'),
    (25, 'c', 72, 'Project: create'),
    (25, 'm', 73, 'Project: change'),
    (25, 'd', 74, 'Project: delete')
;
-- RESULT
-- CHECKPOINT C-05b

/*****************************************************************************/

-- Changing structure of table "configsection" (names) ... \
ALTER TABLE configsection
    CHANGE COLUMN caption name varchar(50) NOT NULL COMMENT 'Unique name'
;
-- RESULT
-- CHECKPOINT C-06

/*****************************************************************************/

-- Removing obsolete database version values ... \
DELETE FROM config WHERE name IN ('DbVersion', 'DbVersionCheckpoint');
-- RESULT

-- Adding configuration variables for virtual URLs managed by centralized scripts ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Site');
UPDATE config SET pos = pos + 7 WHERE id_section = @section_id AND pos >= 14;
-- NORESULT
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section_id, 14, 'AdminRootUrl', 'string', NULL, 'Control Panel Root URL', 'Enter the relative URL of the control panel main page', '/admin', true),
    (@section_id, 15, 'AccountRootUrl', 'string', NULL, 'Account Functionality Root URL', 'Enter the relative URL of the account main page', '/account', true),
    (@section_id, 16, 'TaskWorkspaceRootUrl', 'string', NULL, 'Task Workspace URL', 'Enter the relative URL of the task workspace main page', '/tasks', true),
    (@section_id, 17, 'TaskWorkspaceJobDir', 'string', NULL, 'Task Workspace Job Directory Name', 'Enter the name of the directory containing job information for a workspace task', 'jobs', true),
    (@section_id, 18, 'SchedulerWorkspaceRootUrl', 'string', NULL, 'Scheduler Workspace URL', 'Enter the relative URL of the scheduler workspace main page', '/schedulers', true),
    (@section_id, 19, 'HostCertFile', 'string', NULL, 'Host Certificate File (PKCS#12)', 'Enter the full path of the host certificate file (PKCS#12 format)', NULL, true),
    (@section_id, 20, 'HostCertPassword', 'password', NULL, 'Host Certificate Password [currently ignored]', '[Do not use] Enter the password for the host certificate file', NULL, true)
;
-- RESULT

-- Correct account activation rule variable ... \
UPDATE config SET type = 'int', source = 'userActRule' WHERE name = 'AccountActivation';
-- RESULT

-- Change task flow URL ... \
UPDATE config SET value = '/core/task.flow.aspx' WHERE name = 'TaskFlowUrl';
-- RESULT

-- Add configuration for agent user ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Background Agent');
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section_id, 4, 'AgentUser', 'string', NULL, 'Agent Username', 'Enter the username of the user on whose behalf the agent is running', 'admin', true)
;
-- RESULT
-- CHECKPOINT C-07

/*****************************************************************************/

-- Changing structure of table "action" (names, agent method) ... \
ALTER TABLE action
    CHANGE COLUMN name identifier varchar(25) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(50) NOT NULL COMMENT 'Name',
    ADD COLUMN class_name varchar(100) COMMENT 'Fully qualified name of class implementing action method' AFTER description,
    ADD COLUMN method_name varchar(50) COMMENT 'Name of action method' AFTER class_name
;
-- RESULT

-- Adding agent action methods ... \
UPDATE action SET class_name = 'Terradue.Portal.Task, portal-core-library', method_name = 'ExecuteTaskStatusRefresh' WHERE identifier = 'taskstatus';
UPDATE action SET class_name = 'Terradue.Portal.Task, portal-core-library', method_name = 'ExecuteTaskPendingOperations' WHERE identifier = 'task';
UPDATE action SET class_name = 'Terradue.Portal.Task, portal-core-library', method_name = 'ExecuteCleanup' WHERE identifier = 'cleanup';
UPDATE action SET class_name = 'Terradue.Portal.Scheduler, portal-core-library', method_name = 'ExecuteTaskScheduler' WHERE identifier = 'scheduler';
UPDATE action SET class_name = 'Terradue.Portal.Series, portal-core-library', method_name = 'ExecuteCatalogueSeriesRefresh' WHERE identifier = 'series';
UPDATE action SET identifier = 'cr', class_name = 'Terradue.Portal.ComputingResource, portal-core-library', method_name = 'ExecuteComputingResourceStatusRefresh' WHERE identifier IN ('ce', 'cr');
UPDATE action SET class_name = 'Terradue.Portal.User, portal-core-library', method_name = 'ExecutePasswordExpirationCheck' WHERE identifier = 'password';
-- RESULT

/*****************************************************************************/

-- Changing structure of table "application" (names) ... \
ALTER TABLE application
    CHANGE COLUMN name identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Name'
;
-- RESULT

/*****************************************************************************/

CREATE TABLE domain (
    id int unsigned NOT NULL auto_increment,
    name varchar(100) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    CONSTRAINT pk_domain PRIMARY KEY (id),
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Domains';
-- CHECKPOINT C-08

/*****************************************************************************/

CREATE TABLE role (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Managed domain',
    name varchar(100) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    CONSTRAINT pk_role PRIMARY KEY (id),
    CONSTRAINT fk_role_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Manager roles';
-- CHECKPOINT C-09

/*****************************************************************************/

CREATE TABLE role_priv (
    id_role int unsigned NOT NULL COMMENT 'FK: Manager role',
    id_priv int unsigned NOT NULL COMMENT 'FK: Manager privilege',
    CONSTRAINT pk_role_priv PRIMARY KEY (id_role, id_priv),
    CONSTRAINT fk_role_priv_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_priv_priv FOREIGN KEY (id_priv) REFERENCES priv(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of manager privileges to roles';
-- CHECKPOINT C-10

/*****************************************************************************/

-- Changing structure of table "openidprovider" (domain and catalogue references) ... \
ALTER TABLE openidprovider
    CHANGE COLUMN caption name varchar(50) NOT NULL COMMENT 'Unique name',
    CHANGE COLUMN logo_url icon_url varchar(200) COMMENT 'Relative URL of logo/icon' 
;
-- RESULT
-- CHECKPOINT C-11

/*****************************************************************************/

-- Correcting column comments in table "lookup" ... \
ALTER TABLE lookup
    CHANGE COLUMN caption caption varchar(50) NOT NULL COMMENT 'Caption for value',
    CHANGE COLUMN value value text NOT NULL COMMENT 'Value'
;
-- RESULT
-- CHECKPOINT C-12

/*****************************************************************************/

-- Changing user level of administrators from 3 to 4 ... \
SET @list_id = (SELECT id FROM lookuplist WHERE name = 'userLevel');
UPDATE lookup SET pos = 4, value = '4' WHERE id_list = @list_id AND value = '3';
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list_id, 3, 'Manager', '3')
; 
UPDATE usr SET level = 4 WHERE level = 3;
-- RESULT
-- CHECKPOINT C-13a

-- Changing account activation rules ... \
SET @list_id = (SELECT id FROM lookuplist WHERE name = 'userActRule');
DELETE FROM lookup WHERE id_list = @list_id;
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list_id, 1, 'Disabled', '0'),
    (@list_id, 2, 'Approval by administrator', '1'),
    (@list_id, 3, 'Activation via e-mail link', '2'),
    (@list_id, 4, 'Immediate activation', '3')
; 
-- RESULT
-- CHECKPOINT C-13b

/*****************************************************************************/

-- Changing structure of table "serviceclass" (names) ... \
ALTER TABLE serviceclass
    CHANGE COLUMN name identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Name'
;
-- RESULT
-- CHECKPOINT C-14

/*****************************************************************************/

-- Changing structure of table "servicecategory" (names) ... \
ALTER TABLE servicecategory
    CHANGE COLUMN name identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Name'
;
-- RESULT
-- CHECKPOINT C-15

CREATE PROCEDURE add_servicecategory(IN p_identifier varchar(50), IN p_name varchar(100))
COMMENT 'Inserts or updates a service category'
BEGIN
    DECLARE category_id int;
    SELECT id FROM servicecategory WHERE identifier = p_identifier INTO category_id;
    IF category_id IS NULL THEN
        INSERT INTO servicecategory (identifier, name) VALUES (p_identifier, p_name);
    ELSE
        UPDATE servicecategory SET name = p_name WHERE id = category_id;
    END IF;
END;
-- CHECKPOINT C-16

/*****************************************************************************/

-- Changing structure of table "schedulerclass" (names) ... \
ALTER TABLE schedulerclass
    CHANGE COLUMN name identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Name'
;
-- RESULT
-- CHECKPOINT C-17

/*****************************************************************************/

-- Changing structure of table "usr" (domain reference) ... \
ALTER TABLE usr
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id,
    ADD CONSTRAINT fk_usr_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
;
-- RESULT
-- CHECKPOINT C-18

/*****************************************************************************/

-- Changing structure of table "filter" ... \
ALTER TABLE filter
    ADD COLUMN id_basetype int unsigned COMMENT 'FK: Entity base type' AFTER id_usr,
    CHANGE COLUMN caption name varchar(50) NOT NULL COMMENT 'Unique name',
    ADD CONSTRAINT fk_filter_basetype FOREIGN KEY (id_basetype) REFERENCES basetype(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-19

-- Replacing entity codes with base type references ... \
UPDATE filter SET id_basetype = 11 WHERE entity_code = 7;
UPDATE filter SET id_basetype = 14 WHERE entity_code = 4;
UPDATE filter SET id_basetype = 16 WHERE entity_code = 6;
UPDATE filter SET id_basetype = 18 WHERE entity_code = 5;
UPDATE filter SET id_basetype = 19 WHERE entity_code = 3;
UPDATE filter SET id_basetype = 20 WHERE entity_code = 2;
UPDATE filter SET id_basetype = 21 WHERE entity_code = 1;
-- RESULT
-- CHECKPOINT C-20

/*****************************************************************************/

CREATE TABLE usr_role (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    id_role int unsigned NOT NULL COMMENT 'FK: Manager role to which the user is assigned',
    CONSTRAINT pk_usr_role PRIMARY KEY (id_usr, id_role),
    CONSTRAINT fk_usr_role_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_usr_role_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of users to manager roles';
-- CHECKPOINT C-21

/*****************************************************************************/

-- Changing structure of table "grp" (domain reference) ... \
ALTER TABLE grp
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id,
    ADD COLUMN conf_deleg boolean default false COMMENT 'If true, group can be configured by other domains' AFTER id_domain,
    CHANGE COLUMN name name varchar(50) NOT NULL COMMENT 'Unique name',
    CHANGE COLUMN descr description text COMMENT 'Description',
    ADD CONSTRAINT fk_grp_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
;
-- RESULT
-- CHECKPOINT C-22

/*****************************************************************************/

-- Changing structure of table "lge" (names) ... \
ALTER TABLE lge
    CHANGE COLUMN caption name varchar(50) NOT NULL COMMENT 'Unique name'
;
-- RESULT
-- CHECKPOINT C-23

/*****************************************************************************/

-- Changing structure of table "cr" (domain reference, availability, credit control) ... \
ALTER TABLE cr
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id_exttype,
    ADD COLUMN conf_deleg boolean default false COMMENT 'If true, computing resource can be configured by other domains' AFTER id_domain,
    CHANGE COLUMN availability availability tinyint default 4 COMMENT 'Availability (0..4)',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Unique name',
    CHANGE COLUMN descr description text COMMENT 'Description',
    CHANGE COLUMN logo_url icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    ADD COLUMN credit_control boolean default false COMMENT 'If true, computing resource controls user credits',
    ADD CONSTRAINT fk_cr_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
;
-- RESULT
-- CHECKPOINT C-24

/*****************************************************************************/

CREATE TABLE cr_priv (
    id_cr int unsigned NOT NULL COMMENT 'FK: Computing resource',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    credits int unsigned default '0' COMMENT 'Maximum resource credits for the user',
    CONSTRAINT fk_cr_priv_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on computing resources';
-- CHECKPOINT C-25a

-- Moving cr privileges to dedicated table ... \
INSERT INTO cr_priv (id_cr, id_usr, id_grp) SELECT id_cr, id_usr, id_grp FROM privilege WHERE id_cr IS NOT NULL;
-- RESULT
-- CHECKPOINT C-25b

/*****************************************************************************/

CREATE TABLE catalogue (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    conf_deleg boolean default false COMMENT 'If true, catalogue can be configured by other domains',
    name varchar(100) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    osd_url varchar(100) NOT NULL COMMENT 'OpenSearch description URL',
    base_url varchar(100) NOT NULL COMMENT 'Base URL (prefix for relative URLs)',
    series_rel_url varchar(100) COMMENT 'Relative URL for series list/ingestion',
    dataset_rel_url varchar(100) COMMENT 'Relative URL for data set list/ingestion',
    CONSTRAINT pk_catalogue PRIMARY KEY (id),
    CONSTRAINT fk_catalogue_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Catalogues';
-- CHECKPOINT C-26

/*****************************************************************************/

SET @base_url = (SELECT value FROM config WHERE name = 'DefaultCatalogueBaseUrl');
SET @osd_url = CONCAT(@base_url, '/description');
SET @series_rel_url = (SELECT value FROM config WHERE name = 'CatalogueSeriesBaseUrl');
SET @dataset_rel_url = (SELECT value FROM config WHERE name = 'CatalogueDataSetBaseUrl');

-- Adding default catalogue ... \
INSERT INTO catalogue (conf_deleg, name, description, osd_url, base_url, series_rel_url, dataset_rel_url) VALUES
    (true, 'Default catalogue', 'Default catalogue (created automatically)', CASE WHEN @osd_url IS NULL THEN '' ELSE @osd_url END, CASE WHEN @base_url IS NULL THEN '' ELSE @base_url END, @series_rel_url, @dataset_rel_url)
;
-- RESULT
-- CHECKPOINT C-27

/*****************************************************************************/

-- Removing configuration variables for default catalogue ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Catalogue');
SET @section_pos = (SELECT pos FROM configsection WHERE id = @section_id);
DELETE FROM config WHERE id_section = @section_id;
DELETE FROM configsection WHERE id = @section_id;
-- RESULT
UPDATE configsection SET pos = pos - 1 WHERE pos > @section_pos;
-- NORESULT
-- CHECKPOINT C-28

/*****************************************************************************/

-- Changing structure of table "series" (domain and catalogue references) ... \
ALTER TABLE series
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id,
    ADD COLUMN id_catalogue int unsigned COMMENT 'FK: Containing catalogue' AFTER id_domain,
    ADD COLUMN conf_deleg boolean default false COMMENT 'If true, series can be configured by other domains' AFTER id_catalogue,
    CHANGE COLUMN name identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Name',
    CHANGE COLUMN descr description text COMMENT 'Description',
    CHANGE COLUMN logo_url icon_url varchar(200) COMMENT 'Relative URL of logo/icon', 
    ADD COLUMN dataset_count int COMMENT 'Number of data sets belonging to the series' AFTER manual_assign,
    ADD COLUMN last_update_time datetime COMMENT 'Last update time of the series' AFTER dataset_count,
    ADD CONSTRAINT fk_series_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_series_catalogue FOREIGN KEY (id_catalogue) REFERENCES catalogue(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-29

CREATE PROCEDURE add_series(IN p_identifier varchar(50), IN p_name varchar(200), IN p_description text, IN p_cat_description varchar(200))
COMMENT 'Inserts or updates a series'
BEGIN
    DECLARE series_id int;
    SELECT id FROM series WHERE identifier = p_identifier INTO series_id;
    IF series_id IS NULL THEN
        INSERT INTO series (identifier, name, description, cat_description) VALUES (p_identifier, p_name, p_description, p_cat_description);
    END IF;
END;
-- CHECKPOINT C-30

-- Adding all existing series to new catalogue ... \
SET @catalogue_id = (SELECT id FROM catalogue WHERE name = 'Default catalogue');
UPDATE series SET id_catalogue = @catalogue_id;
-- RESULT
-- CHECKPOINT C-31

/*****************************************************************************/

CREATE TABLE series_priv (
    id_series int unsigned NOT NULL COMMENT 'FK: Series',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_series_priv_series FOREIGN KEY (id_series) REFERENCES series(id) ON DELETE CASCADE,
    CONSTRAINT fk_series_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_series_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on series';
-- CHECKPOINT C-32a

-- Moving series privileges to dedicated table ... \
INSERT INTO series_priv (id_series, id_usr, id_grp) SELECT id_series, id_usr, id_grp FROM privilege WHERE id_series IS NOT NULL;
-- RESULT
-- CHECKPOINT C-32b

/*****************************************************************************/

-- Changing structure of table "producttype" (catalogue reference) ... \
ALTER TABLE producttype
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id,
    ADD COLUMN id_catalogue int unsigned COMMENT 'FK: Containing catalogue' AFTER id_domain,
    ADD COLUMN conf_deleg boolean default false COMMENT 'If true, product type can be configured by other domains' AFTER id_catalogue,
    CHANGE COLUMN name identifier varchar(100) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(50) NOT NULL COMMENT 'Name',
    CHANGE COLUMN descr description text COMMENT 'Description',
    CHANGE COLUMN logo_url icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    ADD CONSTRAINT fk_producttype_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_producttype_catalogue FOREIGN KEY (id_catalogue) REFERENCES catalogue(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-33

/*****************************************************************************/

-- Adding all existing producttype to new catalogue ... \
UPDATE producttype SET id_catalogue = @catalogue_id;
-- RESULT
-- CHECKPOINT C-34

/*****************************************************************************/

CREATE TABLE producttype_priv (
    id_producttype int unsigned NOT NULL COMMENT 'FK: Product type',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_producttype_priv_producttype FOREIGN KEY (id_producttype) REFERENCES producttype(id) ON DELETE CASCADE,
    CONSTRAINT fk_producttype_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_producttype_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on product types';
-- CHECKPOINT C-35a

-- Moving product type privileges to dedicated table ... \
INSERT INTO producttype_priv (id_producttype, id_usr, id_grp) SELECT id_producttype, id_usr, id_grp FROM privilege WHERE id_producttype IS NOT NULL;
-- RESULT
-- CHECKPOINT C-35b

/*****************************************************************************/

-- Changing structure of table "pubserver" (domain reference, public_key) ... \
ALTER TABLE pubserver
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id,
    ADD COLUMN conf_deleg boolean default false COMMENT 'If true, publish server can be configured by other domains' AFTER id_usr,
    CHANGE COLUMN caption name varchar(50) NOT NULL COMMENT 'Unique name',
    CHANGE COLUMN public_key public_key varchar(200) COMMENT 'User''s public key',
    ADD CONSTRAINT fk_pubserver_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
;
-- RESULT
-- CHECKPOINT C-36

/*****************************************************************************/

CREATE TABLE pubserver_priv (
    id_pubserver int unsigned NOT NULL COMMENT 'FK: Publish server',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_pubserver_priv_pubserver FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE CASCADE,
    CONSTRAINT fk_pubserver_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_pubserver_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on publish servers';
-- CHECKPOINT C-37a

-- Initializing publish server privileges ... \
INSERT INTO pubserver_priv (id_pubserver, id_grp) SELECT t1.id, t2.id FROM pubserver AS t1 INNER JOIN grp AS t2;
-- RESULT
-- CHECKPOINT C-37b

/*****************************************************************************/

-- Changing structure of table "service" (domain reference, root) ... \
ALTER TABLE service
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id,
    ADD COLUMN conf_deleg boolean default false COMMENT 'If true, service can be configured by other domains' AFTER id_class,
    CHANGE COLUMN name identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Name',
    CHANGE COLUMN descr description text NOT NULL COMMENT 'Description',
    CHANGE COLUMN logo_url icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    CHANGE COLUMN root root varchar(200) NOT NULL COMMENT 'Directory containing service''s service.xml',
    ADD CONSTRAINT fk_service_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
;
-- RESULT
-- CHECKPOINT C-38

CREATE PROCEDURE link_service_series(IN service_identifier varchar(50), IN series_identifier varchar(50), IN is_default_series boolean)
COMMENT 'Links an input series to a service'
BEGIN
    DECLARE series_id int unsigned;
    DECLARE service_id int unsigned;
    DECLARE c int;

    SELECT id FROM service WHERE identifier = service_identifier INTO service_id;
    SELECT id FROM series WHERE identifier = series_identifier INTO series_id;

    IF service_id IS NOT NULL AND series_id IS NOT NULL THEN
        SELECT COUNT(*) FROM service_series AS t WHERE t.id_service = service_id AND t.id_series = series_id INTO c;
        IF c = 0 THEN
            INSERT INTO service_series (id_service, id_series) VALUES (service_id, series_id);
        END IF;
    
        IF is_default_series THEN
            UPDATE service_series SET is_default = (id_series = series_id) WHERE id_service = service_id;
        END IF;
    END IF;
END;
-- CHECKPOINT C-39

CREATE PROCEDURE unlink_service_series(IN service_identifier varchar(50), IN series_identifier varchar(50))
COMMENT 'Unlinks an input series from a service'
BEGIN
    DELETE FROM service_series WHERE id_service = (SELECT id FROM service WHERE identifier = service_identifier) AND id_series = (SELECT id FROM series WHERE identifier = series_identifier);
END;
-- CHECKPOINT C-40

CREATE PROCEDURE link_service_category(IN service_identifier varchar(50), IN category_identifier varchar(50))
COMMENT 'Adds a service/category assignment'
BEGIN
    DECLARE c int;
    SELECT COUNT(*) FROM service_category AS t INNER JOIN service AS t1 ON t.id_service=t1.id INNER JOIN servicecategory AS t2 ON t.id_category=t2.id WHERE t1.identifier = service_identifier AND t2.identifier = category_identifier INTO c;
    IF c = 0 THEN
        INSERT INTO service_category (id_service, id_category) SELECT t1.id, t2.id FROM service AS t1 INNER JOIN servicecategory AS t2 WHERE t1.identifier = service_identifier AND t2.identifier = category_identifier; 
    END IF;
END;
-- CHECKPOINT C-41

CREATE PROCEDURE unlink_service_category(IN service_identifier varchar(50), IN category_identifier varchar(50))
COMMENT 'Removes a service/category assignment'
BEGIN
    DELETE FROM service_category WHERE id_service = (SELECT id FROM service WHERE identifier = service_identifier) AND id_category = (SELECT id FROM servicecategory WHERE identifier = category_identifier); 
END;
-- CHECKPOINT C-42

/*****************************************************************************/

CREATE TABLE service_priv (
    id_service int unsigned NOT NULL COMMENT 'FK: Service',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    allow_scheduling boolean COMMENT 'If true, user can schedule the service',
    CONSTRAINT fk_service_priv_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on services';
-- CHECKPOINT C-43a

-- Moving service privileges to dedicated table ... \
INSERT INTO service_priv (id_service, id_usr, id_grp) SELECT id_service, id_usr, id_grp FROM privilege WHERE id_service IS NOT NULL;
-- RESULT
-- CHECKPOINT C-43b

/*****************************************************************************/

-- Changing structure of table "scheduler" (names) ... \
ALTER TABLE scheduler
    CHANGE COLUMN name identifier varchar(100) NOT NULL COMMENT 'Unique identifier',
    CHANGE COLUMN caption name varchar(100) NOT NULL COMMENT 'Caption'
;
-- RESULT
-- CHECKPOINT C-44

/*****************************************************************************/

-- Changing structure of table "task" (names) ... \
ALTER TABLE task
    CHANGE COLUMN uid identifier varchar(50) COMMENT 'Unique identifier (automatically generated UID)',
    CHANGE COLUMN caption name varchar(200) NOT NULL COMMENT 'Name or caption'
;
-- RESULT
-- CHECKPOINT C-45

/*****************************************************************************/

USE $NEWS$;

/*****************************************************************************/

-- Changing structure of table "image" (names) ... \
ALTER TABLE image
    CHANGE COLUMN descr description text COMMENT 'Description'
;
-- RESULT
-- CHECKPOINT C-46

/*****************************************************************************/

-- Changing structure of table "project" (names) ... \
ALTER TABLE project
    CHANGE COLUMN short_descr short_description text COMMENT 'Short description',
    CHANGE COLUMN long_descr long_description text COMMENT 'Long description'
;
-- RESULT
-- CHECKPOINT C-47

/*****************************************************************************/

CREATE TABLE log (
    id int unsigned NOT NULL auto_increment,
    log_time datetime NOT NULL,
    thread varchar(255) NOT NULL,
    level varchar(20) NOT NULL,
    logger varchar(255) NOT NULL,
    message varchar(4000) NOT NULL,
    post_parameters text,
    CONSTRAINT pk_log PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'System log entries';
-- CHECKPOINT C-48

/*****************************************************************************/
