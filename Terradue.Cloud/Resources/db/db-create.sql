-- VERSION 2.0

USE $MAIN$;

/*****************************************************************************/

SET @priv_pos = (SELECT MAX(pos) FROM priv);

-- Adding extended enity type for Oozie Cloud Computing Resource ... \
CALL add_type($ID$, 'Terradue.Cloud.OozieComputingResource, Terradue.Cloud', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'Oozie Cloud Computing Resource', 'Oozie Cloud Computing Resources', NULL);
-- RESULT

/*****************************************************************************/

-- Adding basic entity type for cloud providers and extended entity type for OCCI cloud providers ... \
CALL add_type($ID$, 'Terradue.Cloud.CloudProvider, Terradue.Cloud', NULL, 'Cloud Provider', 'Cloud Providers', 'cloud-providers');
SET @type_id = (SELECT LAST_INSERT_ID());
CALL add_type($ID$, 'Terradue.Cloud.OcciCloudProvider, Terradue.Cloud', 'Terradue.Cloud.CloudProvider, Terradue.Cloud', 'OCCI Cloud Provider', 'OCCI Cloud Providers', NULL);
CALL add_type($ID$, 'Terradue.Cloud.OneCloudProvider, Terradue.Cloud', 'Terradue.Cloud.CloudProvider, Terradue.Cloud', 'OpenNebula Cloud Provider', 'OpenNebula Cloud Providers', NULL);
-- RESULT

/*****************************************************************************/

-- Adding privileges for cloud providers ... \
INSERT INTO priv (identifier, id_type, operation, pos, name) VALUES
    ('cloudprov-c', @type_id, 'c', @priv_pos + 1, 'Cloud providers: create'),
    ('cloudprov-s', @type_id, 's', @priv_pos + 2, 'Cloud providers: search'),
    ('cloudprov-v', @type_id, 'v', @priv_pos + 3, 'Cloud providers: view'),
    ('cloudprov-m', @type_id, 'm', @priv_pos + 4, 'Cloud providers: change'),
    ('cloudprov-M', @type_id, 'M', @priv_pos + 5, 'Cloud providers: control'),
    ('cloudprov-d', @type_id, 'd', @priv_pos + 6, 'Cloud providers: delete')
;
-- RESULT

/*****************************************************************************/

-- Adding basic entity type for cloud appliances and extended enity type for OCCI cloud appliances ... \
CALL add_type($ID$, 'Terradue.Cloud.CloudAppliance, Terradue.Cloud', NULL, 'Cloud Appliances', 'cloud-appliances', 'cloud-appliances');
SET @type_id = (SELECT LAST_INSERT_ID());
CALL add_type($ID$, 'Terradue.Cloud.OcciCloudAppliance, Terradue.Cloud', 'Terradue.Cloud.CloudAppliance, Terradue.Cloud', 'OCCI Cloud Appliance', 'OCCI Cloud Appliances', NULL);
CALL add_type($ID$, 'Terradue.Cloud.OneCloudAppliance, Terradue.Cloud', 'Terradue.Cloud.CloudAppliance, Terradue.Cloud', 'OpenNebula Cloud Appliance', 'OpenNebula Cloud Appliances', NULL);
-- RESULT

-- Adding privileges for cloud appliances ... \
INSERT INTO priv (identifier, id_type, operation, pos, name) VALUES
    ('cloud-c', @type_id, 'c', @priv_pos + 1, 'Cloud appliances: create'),
    ('cloud-s', @type_id, 's', @priv_pos + 2, 'Cloud appliances: search'),
    ('cloud-v', @type_id, 'v', @priv_pos + 3, 'Cloud appliances: view'),
    ('cloud-m', @type_id, 'm', @priv_pos + 4, 'Cloud appliances: change'),
    ('cloud-M', @type_id, 'M', @priv_pos + 5, 'Cloud appliances: control'),
    ('cloud-d', @type_id, 'd', @priv_pos + 6, 'Cloud appliances: delete')
;
-- RESULT

/*****************************************************************************/

CREATE TABLE cloudprov (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    conf_deleg boolean default false COMMENT 'If true, cloud provider can be configured by other domains',
    caption varchar(100) NOT NULL COMMENT 'Caption',
    address varchar(100) NOT NULL COMMENT 'Access point (e.g. web service)',
    web_admin_url varchar(100) NOT NULL COMMENT 'Web administration URL',
    CONSTRAINT pk_cloudprov PRIMARY KEY (id),
    CONSTRAINT fk_cloudprov_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    CONSTRAINT fk_cloudprov_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Cloud providers';

/*****************************************************************************/

CREATE TABLE occicloudprov (
    id int unsigned NOT NULL,
    occi_version varchar(25) NOT NULL COMMENT 'OCCI version',
    CONSTRAINT pk_occicloudprov PRIMARY KEY (id),
    CONSTRAINT fk_occicloudprov_cloudprov FOREIGN KEY (id) REFERENCES cloudprov(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'OCCI cloud providers';

/*****************************************************************************/

CREATE TABLE onecloudprov (
    id int unsigned NOT NULL,
    admin_usr varchar(25) NOT NULL COMMENT 'XML-RPC admin username',
    admin_pwd varchar(100) NOT NULL COMMENT 'XML-RPC admin password',
    CONSTRAINT pk_onecloudprov PRIMARY KEY (id),
    CONSTRAINT fk_onecloudprov_cloudprov FOREIGN KEY (id) REFERENCES cloudprov(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'OpenNebula cloud providers';

/*****************************************************************************/

CREATE TABLE cloud (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    id_cloudprov int unsigned NOT NULL COMMENT 'FK: Cloud provider',
    remote_id varchar(100) NOT NULL COMMENT 'Remote identifier',
    caption varchar(100) NOT NULL COMMENT 'Caption',
    description text COMMENT 'Description',
    hostname varchar(100) COMMENT 'Hostname',
    CONSTRAINT pk_cloud PRIMARY KEY (id),
    CONSTRAINT fk_cloud_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    CONSTRAINT fk_cloud_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE SET NULL,
    CONSTRAINT fk_cloud_cloudprov FOREIGN KEY (id_cloudprov) REFERENCES cloudprov(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Cloud appliances';

/*****************************************************************************/

CREATE TABLE cloudcr (
    id int unsigned NOT NULL,
    id_cloud int unsigned COMMENT 'FK: hosting cloud appliance',
    CONSTRAINT pk_cloudcr PRIMARY KEY (id),
    CONSTRAINT fk_cloudcr_cr FOREIGN KEY (id) REFERENCES cr(id) ON DELETE CASCADE,
    CONSTRAINT fk_cloudcr_cloud FOREIGN KEY (id_cloud) REFERENCES cloud(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Cloud computing resources';

/*****************************************************************************/

CREATE TABLE ooziecr (
    id int unsigned NOT NULL,
    oozie_address varchar(100) NOT NULL COMMENT 'Base address of Oozie web service',
    job_tracker varchar(100) NOT NULL COMMENT 'Job tracker address',
    env_vars varchar(1000) COMMENT 'Streaming environment variables',
    queue_name varchar(100) NOT NULL COMMENT 'Job queue name',
    proc_user varchar(100) NOT NULL COMMENT 'User account for processing',
    hdfs_name_node varchar(100) NOT NULL COMMENT 'HDFS name node address',
    hdfs_ws_type tinyint unsigned NOT NULL COMMENT 'Type of HDFS web service',
    hdfs_ws_address varchar(100) NOT NULL COMMENT 'Base address of HDFS web service',
    task_base_dir varchar(100) NOT NULL COMMENT 'Task base directory in HDFS',
    task_proc_dir varchar(100) NOT NULL COMMENT 'Task processing directory in HDFS',
    app_doc_url varchar(100) NOT NULL COMMENT 'Job application document address',
    CONSTRAINT pk_ooziecr PRIMARY KEY (id),
    CONSTRAINT fk_ooziecr_cloudcr FOREIGN KEY (id) REFERENCES cloudcr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Oozie cloud computing resources';

/*****************************************************************************/

CREATE TABLE usr_cloud (
    id int unsigned NOT NULL COMMENT 'FK: User',
    id_provider int unsigned NOT NULL COMMENT 'FK: Provider',
    username varchar(50) NULL DEFAULT NULL COMMENT 'Username for cloud authentication',
    CONSTRAINT pk_usr_cloud PRIMARY KEY (id, id_provider),
    CONSTRAINT fk_usr_cloud_usr FOREIGN KEY (id) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_usr_cloud_provider FOREIGN KEY (id_provider) REFERENCES cloudprov(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'user cloud information';

/*****************************************************************************/

CALL add_type($ID$, 'Terradue.Cloud.CloudUser, Terradue.Cloud', 'Terradue.Portal.User, Terradue.Portal', 'Cloud user information', 'Cloud user information', NULL);

/*****************************************************************************/
