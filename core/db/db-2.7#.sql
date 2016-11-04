USE $MAIN$;

/*****************************************************************************/

-- Changing basic types ... \
UPDATE type SET class='Terradue.Portal.Role, Terradue.Portal', caption_sg='Role', caption_pl='Roles', keyword='roles' WHERE class='Terradue.Portal.ManagerRole, Terradue.Portal';
UPDATE type SET generic_class='Terradue.Portal.GenericComputingResource' WHERE class='Terradue.Portal.ComputingResource, Terradue.Portal'; 
-- RESULT
-- CHECKPOINT C-01

/*****************************************************************************/

-- Correcting type references for privileges ... \
UPDATE priv SET id_type = (SELECT id FROM type WHERE class = 'Terradue.Portal.Scheduler, Terradue.Portal') WHERE name LIKE 'Scheduler:%';
UPDATE priv SET id_type = (SELECT id FROM type WHERE class = 'Terradue.Portal.Task, Terradue.Portal') WHERE name LIKE 'Task:%';
UPDATE priv SET id_type = (SELECT id FROM type WHERE class = 'Terradue.Portal.Article, Terradue.Portal') WHERE name LIKE 'Article:%';
UPDATE priv SET id_type = (SELECT id FROM type WHERE class = 'Terradue.Portal.Image, Terradue.Portal') WHERE name LIKE 'Image:%';
UPDATE priv SET id_type = (SELECT id FROM type WHERE class = 'Terradue.Portal.Faq, Terradue.Portal') WHERE name LIKE 'FAQ:%';
UPDATE priv SET id_type = (SELECT id FROM type WHERE class = 'Terradue.Portal.Project, Terradue.Portal') WHERE name LIKE 'Project:%';
-- RESULT
-- CHECKPOINT C-02a

-- Adjusting table "priv" ... \
ALTER TABLE priv
    DROP INDEX name,
    ADD COLUMN identifier varchar(50) CHARACTER SET utf8 COLLATE utf8_bin AFTER id,
    CHANGE COLUMN name name varchar(100) NOT NULL COMMENT 'Human-readable name' AFTER identifier,
    CHANGE COLUMN operation operation char(1) COLLATE latin1_general_cs COMMENT 'Operation type (one-letter code)',
    ADD UNIQUE INDEX (identifier)
;
-- RESULT
-- CHECKPOINT C-02b

-- Changing existing and adding new privileges ... \
DROP PROCEDURE IF EXISTS add_priv;
CREATE PROCEDURE add_priv(IN p_identifier varchar(50), IN p_name varchar(50), IN p_entity_class varchar(100), IN p_operation_old char(1) CHARACTER SET latin1 COLLATE latin1_general_cs, IN p_operation_new char(1) CHARACTER SET latin1 COLLATE latin1_general_cs)
BEGIN
    DECLARE type_id int;
    DECLARE priv_id int;
    SELECT id FROM type WHERE class = p_entity_class INTO type_id;
    SELECT id FROM priv WHERE id_type = type_id AND operation = p_operation_old INTO priv_id;
    IF priv_id IS NULL OR p_entity_class IS NULL THEN
        INSERT INTO priv (identifier, name, id_type, operation) VALUES (p_identifier, p_name, type_id, p_operation_new);
    ELSE
        UPDATE priv SET identifier = p_identifier, name = p_name, operation = p_operation_new WHERE id = priv_id;
    END IF;
END;

CALL add_priv('usr-c', 'Users: create', 'Terradue.Portal.User, Terradue.Portal', 'c', 'c');
CALL add_priv('usr-s', 'Users: search', 'Terradue.Portal.User, Terradue.Portal', 's', 's');
CALL add_priv('usr-v', 'Users: view', 'Terradue.Portal.User, Terradue.Portal', 'v', 'v');
CALL add_priv('usr-m', 'Users: change', 'Terradue.Portal.User, Terradue.Portal', 'm', 'm');
CALL add_priv('usr-M', 'Users: manage', 'Terradue.Portal.User, Terradue.Portal', 'A', 'M');
CALL add_priv('usr-d', 'Users: delete', 'Terradue.Portal.User, Terradue.Portal', 'd', 'd');
CALL add_priv('grp-c', 'Groups: create', 'Terradue.Portal.Group, Terradue.Portal', 'c', 'c');
CALL add_priv('grp-s', 'Groups: search', 'Terradue.Portal.Group, Terradue.Portal', 's', 's');
CALL add_priv('grp-v', 'Groups: view', 'Terradue.Portal.Group, Terradue.Portal', 'v', 'v');
CALL add_priv('grp-m', 'Groups: change', 'Terradue.Portal.Group, Terradue.Portal', 'm', 'm');
CALL add_priv('grp-M', 'Groups: manage', 'Terradue.Portal.Group, Terradue.Portal', 'A', 'M');
CALL add_priv('grp-d', 'Groups: delete', 'Terradue.Portal.Group, Terradue.Portal', 'd', 'd');
CALL add_priv('cr-c', 'Computing resources: create', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'c', 'c');
CALL add_priv('cr-s', 'Computing resources: search', 'Terradue.Portal.ComputingResource, Terradue.Portal', 's', 's');
CALL add_priv('cr-v', 'Computing resources: view', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'v', 'v');
CALL add_priv('cr-u', 'Computing resources: use', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'u', 'u');
CALL add_priv('cr-m', 'Computing resources: change', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'm', 'm');
CALL add_priv('cr-M', 'Computing resources: manage', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'A', 'M');
CALL add_priv('cr-d', 'Computing resources: delete', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'd', 'd');
CALL add_priv('catalogue-c', 'Catalogues: create', 'Terradue.Portal.Catalogue, Terradue.Portal', 'c', 'c');
CALL add_priv('catalogue-s', 'Catalogues: search', 'Terradue.Portal.Catalogue, Terradue.Portal', 's', 's');
CALL add_priv('catalogue-v', 'Catalogues: view', 'Terradue.Portal.Catalogue, Terradue.Portal', 'v', 'v');
CALL add_priv('catalogue-u', 'Catalogues: use', 'Terradue.Portal.Catalogue, Terradue.Portal', 'u', 'u');
CALL add_priv('catalogue-m', 'Catalogues: change', 'Terradue.Portal.Catalogue, Terradue.Portal', 'm', 'm');
CALL add_priv('catalogue-M', 'Catalogues: manage', 'Terradue.Portal.Catalogue, Terradue.Portal', 'A', 'M');
CALL add_priv('catalogue-d', 'Catalogues: delete', 'Terradue.Portal.Catalogue, Terradue.Portal', 'd', 'd');
CALL add_priv('series-c', 'Series: create', 'Terradue.Portal.Series, Terradue.Portal', 'c', 'c');
CALL add_priv('series-s', 'Series: search', 'Terradue.Portal.Series, Terradue.Portal', 's', 's');
CALL add_priv('series-v', 'Series: view', 'Terradue.Portal.Series, Terradue.Portal', 'v', 'v');
CALL add_priv('series-u', 'Series: use', 'Terradue.Portal.Series, Terradue.Portal', 'u', 'u');
CALL add_priv('series-m', 'Series: change', 'Terradue.Portal.Series, Terradue.Portal', 'm', 'm');
CALL add_priv('series-M', 'Series: manage', 'Terradue.Portal.Series, Terradue.Portal', 'A', 'M');
CALL add_priv('series-d', 'Series: delete', 'Terradue.Portal.Series, Terradue.Portal', 'd', 'd');
CALL add_priv('pubserver-c', 'Publish servers: create', 'Terradue.Portal.PublishServer, Terradue.Portal', 'c', 'c');
CALL add_priv('pubserver-s', 'Publish servers: search', 'Terradue.Portal.PublishServer, Terradue.Portal', 's', 's');
CALL add_priv('pubserver-v', 'Publish servers: view', 'Terradue.Portal.PublishServer, Terradue.Portal', 'v', 'v');
CALL add_priv('pubserver-u', 'Publish servers: use', 'Terradue.Portal.PublishServer, Terradue.Portal', 'u', 'u');
CALL add_priv('pubserver-m', 'Publish servers: change', 'Terradue.Portal.PublishServer, Terradue.Portal', 'm', 'm');
CALL add_priv('pubserver-M', 'Publish servers: manage', 'Terradue.Portal.PublishServer, Terradue.Portal', 'A', 'M');
CALL add_priv('pubserver-d', 'Publish servers: delete', 'Terradue.Portal.PublishServer, Terradue.Portal', 'd', 'd');
CALL add_priv('service-c', 'Processing services: create', 'Terradue.Portal.Service, Terradue.Portal', 'c', 'c');
CALL add_priv('service-s', 'Processing services: search', 'Terradue.Portal.Service, Terradue.Portal', 's', 's');
CALL add_priv('service-v', 'Processing services: view', 'Terradue.Portal.Service, Terradue.Portal', 'v', 'v');
CALL add_priv('service-u', 'Processing services: use', 'Terradue.Portal.Service, Terradue.Portal', 'u', 'u');
CALL add_priv('service-m', 'Processing services: change', 'Terradue.Portal.Service, Terradue.Portal', 'm', 'm');
CALL add_priv('service-M', 'Processing services: manage', 'Terradue.Portal.Service, Terradue.Portal', 'A', 'M');
CALL add_priv('service-d', 'Processing services: delete', 'Terradue.Portal.Service, Terradue.Portal', 'd', 'd');
CALL add_priv('scheduler-M', 'Schedulers: control', 'Terradue.Portal.Scheduler, Terradue.Portal', 'm', 'M');
CALL add_priv('task-M', 'Tasks: control', 'Terradue.Portal.Task, Terradue.Portal', 'm', 'M');
CALL add_priv('news-c', 'News items: create', NULL, 'c', 'c');
CALL add_priv('news-s', 'News items: search', NULL, 's', 's');
CALL add_priv('news-v', 'News items: view', NULL, 'v', 'v');
CALL add_priv('news-m', 'News items: change', NULL, 'm', 'm');
CALL add_priv('news-d', 'News items: delete', NULL, 'd', 'd');
-- RESULT
-- CHECKPOINT C-02c

DROP PROCEDURE IF EXISTS add_priv;

-- Marking old privileges as potentially obsolete and assign unique identifier ... \
UPDATE priv SET name = CONCAT('-- obsolete? -- ', name), identifier = CONCAT('priv-', id) WHERE identifier IS NULL;
-- RESULT
-- -> Manual intervention required: check privileges marked as "-- obsolete? --" (priv.name), rename or delete records as appropriate.
-- TODO Manual intervention required: check privileges marked as "-- obsolete? --" (priv.name), rename or delete records as appropriate.
-- CHECKPOINT C-02d


-- Making unique privilege identifier mandatory ... \
ALTER TABLE priv
    CHANGE COLUMN identifier identifier varchar(50) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL COMMENT 'Unique identifier'
;
-- RESULT
-- CHECKPOINT C-02e

/*****************************************************************************/

-- Adjusting table "role" ... \
ALTER TABLE role
    DROP FOREIGN KEY fk_role_domain,
    DROP COLUMN id_domain,
    DROP INDEX name,
    ADD COLUMN identifier varchar(50) COMMENT 'Unique identifier' AFTER id,
    CHANGE COLUMN name name varchar(100) NOT NULL COMMENT 'Human-readable name' AFTER identifier,
    ADD UNIQUE INDEX (identifier)
;
-- RESULT
-- CHECKPOINT C-03a

-- Adding unique identifiers to roles ... \
UPDATE role SET identifier = CONCAT('role-', id) WHERE identifier IS NULL;
-- RESULT
-- CHECKPOINT C-03b

-- Making unique role identifier mandatory ... \
ALTER TABLE role
    CHANGE COLUMN identifier identifier varchar(50) NOT NULL COMMENT 'Unique identifier'
;
-- RESULT
-- CHECKPOINT C-03c

/*****************************************************************************/

-- Adding optional integer column to role/privilege association ... \
ALTER TABLE role_priv
    CHANGE COLUMN id_role id_role int unsigned NOT NULL COMMENT 'FK: Role',
    CHANGE COLUMN id_priv id_priv int unsigned NOT NULL COMMENT 'FK: Privilege',
    ADD COLUMN int_value int COMMENT 'Value (optional)'
;
-- CHECKPOINT C-04
-- RESULT

/*****************************************************************************/

CREATE TABLE rolegrant (
    id_usr int unsigned COMMENT 'FK: User (id_usr or id_grp must be set)',
    id_grp int unsigned COMMENT 'FK: Group (id_usr or id_grp must be set)',
    id_role int unsigned NOT NULL COMMENT 'FK: Role to which the user/group is assigned',
    id_domain int unsigned COMMENT 'FK: Domain for which the user/group has the role',
    CONSTRAINT fk_rolegrant_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of users/groups to roles for domains';
-- CHECKPOINT C-05a

-- Copying existing role grants ... \
INSERT INTO rolegrant (id_usr, id_role) SELECT id_usr, id_role FROM usr_role;
-- RESULT
-- CHECKPOINT C-05b

-- Removing obsolete table "usr_role" ... \
DROP TABLE usr_role;
-- RESULT
-- CHECKPOINT C-05c

/*****************************************************************************/

-- Adding new permissions for series ... \
ALTER TABLE series_priv
    ADD COLUMN can_search boolean COMMENT 'If true, user/group has product search permission',
    ADD COLUMN can_download boolean COMMENT 'If true, user/group has download permission',
    ADD COLUMN can_process boolean COMMENT 'If true, user/group has processing permission'
;
-- RESULT
-- CHECKPOINT C-06

/*****************************************************************************/

-- Changing table comments for new terminology (roles, privilege/permission) ... \
ALTER TABLE priv COMMENT = 'Privileges';
ALTER TABLE role COMMENT = 'Roles for users or groups';
ALTER TABLE role_priv COMMENT = 'Associations of privileges to roles';
ALTER TABLE cr_priv COMMENT = 'User/group permissions on computing resources';
ALTER TABLE series_priv COMMENT = 'User/group permissions on series';
ALTER TABLE producttype_priv COMMENT = 'User/group permissions on product types';
ALTER TABLE resourceset_priv COMMENT = 'User/group permissions on resource sets';
ALTER TABLE pubserver_priv COMMENT = 'User/group permissions on publish servers';
ALTER TABLE service_priv COMMENT = 'User/group permissions on services';
ALTER TABLE safe_priv COMMENT = 'User/group permissions on safes';
-- NORESULT
-- OK

/*****************************************************************************/

-- Add domain for resourceset table
ALTER TABLE resourceset ADD COLUMN id_domain INT UNSIGNED NULL;
ALTER TABLE resourceset ADD CONSTRAINT fk_resourceset_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL;
-- RESULT
