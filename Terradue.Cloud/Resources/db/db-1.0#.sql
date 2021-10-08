USE $MAIN$;

/*****************************************************************************/

-- Removing duplicate entity type extensions ... \
CREATE TEMPORARY TABLE temp_exttype (id int, entity_code int, class_name varchar(100));
CREATE TEMPORARY TABLE temp_exttype_duplicate (id int, id_new int, entity_code int);
INSERT INTO temp_exttype (id, entity_code, class_name) SELECT MIN(id), MIN(entity_code), class_name FROM exttype WHERE entity_code IN (16, 17) GROUP BY class_name;
INSERT INTO temp_exttype_duplicate (id, id_new, entity_code) SELECT t.id, t1.id, t.entity_code FROM exttype AS t INNER JOIN temp_exttype AS t1 ON t.class_name=t1.class_name;
UPDATE cloudprov AS t LEFT JOIN temp_exttype_duplicate AS t1 ON t1.entity_code=16 AND t.id_exttype=t1.id SET t.id_exttype=t1.id_new WHERE t1.id_new!=t1.id;
UPDATE cloud AS t LEFT JOIN temp_exttype_duplicate AS t1 ON t1.entity_code=17 AND t.id_exttype=t1.id SET t.id_exttype=t1.id_new WHERE t1.id_new!=t1.id;
-- NORESULT
DELETE FROM exttype WHERE id IN (SELECT id FROM temp_exttype_duplicate WHERE id != id_new);
-- RESULT
DROP TABLE temp_exttype;
DROP TABLE temp_exttype_duplicate;
-- NORESULT

/*****************************************************************************/

-- Correcting module references for Oozie cloud computing resources ... \
UPDATE exttype SET id_module = $ID$ WHERE class_name LIKE 'Terradue.Cloud.OozieComputingResource, %';
-- RESULT

/*****************************************************************************/

SET @priv_pos = (SELECT MAX(pos) FROM priv);

-- Adding entity base type for cloud providers and updating entity type extension for OCCI cloud providers ... \
INSERT INTO basetype (id_module, pos, name, class_name, generic_class, caption, keyword, is_composite, has_domain, has_owner, is_assigned) VALUES
    ($ID$, 1, 'cloudprov', 'Terradue.Cloud.CloudProvider, portal-modules-cloud-library', 'Terradue.Cloud.GenericCloudProvider, portal-modules-cloud-library', 'Cloud Providers', 'cloud-providers', false, true, false, false)
;
SET @basetype_id = (SELECT LAST_INSERT_ID());
UPDATE exttype SET id_module = $ID$, id_basetype = @basetype_id WHERE entity_code = 16;
UPDATE filter SET id_basetype = @basetype_id WHERE entity_code = 16;
-- RESULT

-- Adding manager privileges for cloud providers ... \
INSERT INTO priv (id_basetype, operation, pos, name) VALUES
    (@basetype_id, 'v', @priv_pos + 1, 'Cloud providers: view'),
    (@basetype_id, 'm', @priv_pos + 2, 'Cloud providers: control'),
    (@basetype_id, 'V', @priv_pos + 3, 'Cloud providers: view public'),
    (@basetype_id, 'A', @priv_pos + 4, 'Cloud providers: assign public')
;
-- RESULT

/*****************************************************************************/

-- Adding entity base type for cloud appliances and updating entity type extension for OCCI cloud appliances ... \
INSERT INTO basetype (id_module, pos, name, class_name, generic_class, caption, keyword, is_composite, has_domain, has_owner, is_assigned) VALUES
    ($ID$, 2, 'cloud', 'Terradue.Cloud.CloudAppliance, portal-modules-cloud-library', 'Terradue.Cloud.GenericCloudAppliance, portal-modules-cloud-library', 'Cloud Appliances', 'cloud-appliances', false, false, false, false)
;
SET @basetype_id = (SELECT LAST_INSERT_ID());
UPDATE exttype SET id_module = $ID$, id_basetype = @basetype_id WHERE entity_code = 17;
UPDATE filter SET id_basetype = @basetype_id WHERE entity_code = 17;
-- RESULT

-- Adding manager privileges for cloud appliances ... \
INSERT INTO priv (id_basetype, operation, pos, name) VALUES
    (@basetype_id, 'v', @priv_pos + 5, 'Cloud appliances: view'),
    (@basetype_id, 'm', @priv_pos + 6, 'Cloud appliances: control'),
    (@basetype_id, 'V', @priv_pos + 7, 'Cloud appliances: view public'),
    (@basetype_id, 'A', @priv_pos + 8, 'Cloud appliances: assign public')
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "cloudprov" (domain reference) ... \
ALTER TABLE cloudprov
    ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain' AFTER id,
    ADD COLUMN conf_deleg boolean default false COMMENT 'If true, cloud provider can be configured by other domains' AFTER id_exttype,
    ADD CONSTRAINT fk_cloudprov_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
;

/*****************************************************************************/

-- Changing structure of table "cloud" (names) ... \
ALTER TABLE cloud
    CHANGE COLUMN descr description text COMMENT 'Description'
;
-- RESULT

/*****************************************************************************/

-- Removing invalid cloud computing resources ... \
CREATE TEMPORARY TABLE temp_cr (id int);
SET @exttype_id = (SELECT id FROM exttype WHERE class_name LIKE 'Terradue.Cloud.OozieComputingResource, %');
INSERT INTO temp_cr (id) SELECT t.id FROM cr AS t LEFT JOIN cloudcr AS t1 ON t.id=t1.id LEFT JOIN ooziecr AS t2 ON t.id=t2.id WHERE t.id_exttype = @exttype_id AND (t1.id IS NULL OR t2.id IS NULL);
-- NORESULT
DELETE FROM cr WHERE id IN (SELECT id FROM temp_cr);
DROP TABLE temp_cr;
-- RESULT

/*****************************************************************************/
