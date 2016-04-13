-- VERSION 2.0

USE $MAIN$;

/*****************************************************************************/

SET @priv_pos = (SELECT MAX(pos) FROM priv);

-- Adding basic entity type for laboratories ... \
CALL add_type($ID$, 'Terradue.Sandbox.Laboratory, Terradue.Sandbox', NULL, 'Laboratory', 'Laboratories', 'laboratories');
SET @type_id = (SELECT LAST_INSERT_ID());
-- RESULT

-- Adding privileges for laboratories ... \
INSERT INTO priv (identifier, id_type, operation, pos, name) VALUES
    ('laboratory-c', @type_id, 'c', @priv_pos + 1, 'Laboratories: create'),
    ('laboratory-s', @type_id, 's', @priv_pos + 2, 'Laboratories: search'),
    ('laboratory-v', @type_id, 'v', @priv_pos + 3, 'Laboratories: view'),
    ('laboratory-m', @type_id, 'm', @priv_pos + 4, 'Laboratories: change'),
    ('laboratory-M', @type_id, 'M', @priv_pos + 5, 'Laboratories: control'),
    ('laboratory-d', @type_id, 'd', @priv_pos + 6, 'Laboratories: delete')
;
-- RESULT

/*****************************************************************************/

-- Adding basic entity type for sandboxes ... \
CALL add_type($ID$, 'Terradue.Sandbox.Sandbox, Terradue.Sandbox', NULL, 'Sandbox', 'Sandboxes', 'sandboxes');
SET @type_id = (SELECT LAST_INSERT_ID());
-- RESULT

-- Adding privileges for sandboxes... \
INSERT INTO priv (identifier, id_type, operation, pos, name) VALUES
    ('sandbox-c', @type_id, 'c', @priv_pos + 1, 'Sandboxes: create'),
    ('sandbox-s', @type_id, 's', @priv_pos + 2, 'Sandboxes: search'),
    ('sandbox-v', @type_id, 'v', @priv_pos + 3, 'Sandboxes: view'),
    ('sandbox-m', @type_id, 'm', @priv_pos + 4, 'Sandboxes: change'),
    ('sandbox-M', @type_id, 'M', @priv_pos + 5, 'Sandboxes: control'),
    ('sandbox-d', @type_id, 'd', @priv_pos + 6, 'Sandboxes: delete')
;
-- RESULT

/*****************************************************************************/

CREATE TABLE laboratory (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_cloudprov int unsigned NOT NULL COMMENT 'FK: Cloud provider',
    conf_deleg boolean default false COMMENT 'If true, laboratory can be configured by other domains',
    caption varchar(100) NOT NULL COMMENT 'Caption',
    description text COMMENT 'Description',
    virtual_network varchar(50) COMMENT 'Virtual network name',
    CONSTRAINT pk_laboratory PRIMARY KEY (id),
    CONSTRAINT fk_laboratory_cloudprov FOREIGN KEY (id_cloudprov) REFERENCES cloudprov(id) ON DELETE CASCADE,
    CONSTRAINT fk_laboratory_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Laboratories';

/*****************************************************************************/

CREATE TABLE laboratory_priv (
    id_laboratory int unsigned NOT NULL COMMENT 'FK: Laboratory',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    is_manager bool COMMENT 'If true, user has manager privilages on the laboratory',
    CONSTRAINT fk_laboratory_priv_laboratory FOREIGN KEY (id_laboratory) REFERENCES laboratory(id) ON DELETE CASCADE,
    CONSTRAINT fk_laboratory_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_laboratory_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on laboratories';

/*****************************************************************************/

CREATE TABLE sandbox (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    id_laboratory int unsigned NOT NULL COMMENT 'FK: Laboratory',
    id_cloudcr int unsigned COMMENT 'FK: Cloud computing resource',
    caption varchar(100) NOT NULL COMMENT 'Caption',
    description text COMMENT 'Description',
    vm_template varchar(50) COMMENT 'Virtual machine template name',
    virtual_disks varchar(200) COMMENT 'Virtual disk names (separated by tab)',
    CONSTRAINT pk_sandbox PRIMARY KEY (id),
    CONSTRAINT fk_sandbox_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    CONSTRAINT fk_sandbox_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE SET NULL,
    CONSTRAINT fk_sandbox_laboratory FOREIGN KEY (id_laboratory) REFERENCES laboratory(id) ON DELETE CASCADE,
    CONSTRAINT fk_sandbox_cloudcr FOREIGN KEY (id_cloudcr) REFERENCES cloudcr(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Sandboxes';

/*****************************************************************************/
