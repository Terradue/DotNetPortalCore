USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "cloudprov" (add new type reference) ... \
ALTER TABLE cloud
    ADD COLUMN id_usr int unsigned COMMENT 'FK: Owning user (optional)' AFTER id_type,
    ADD CONSTRAINT fk_cloud_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE SET NULL
;
-- RESULT

-- Update priv for cloud ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Cloud.CloudAppliance, Terradue.Cloud');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
DELETE FROM priv WHERE id_type=@type_id AND operation='V';
DELETE FROM priv WHERE id_type=@type_id AND operation='A';
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'cloud-s', 's', @priv_pos + 1, 'Cloud appliances: search', 1);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'cloud-c', 'c', @priv_pos + 1, 'Cloud appliances: control', 1);
UPDATE priv SET identifier='cloud-v' WHERE id_type=@type_id AND operation='v';
UPDATE priv SET identifier='cloud-m' WHERE id_type=@type_id AND operation='m';
-- RESULT

-- Update priv for cloudprov ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Cloud.CloudProvider, Terradue.Cloud');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
DELETE FROM priv WHERE id_type=@type_id AND operation='V';
DELETE FROM priv WHERE id_type=@type_id AND operation='A';
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'cloudprov-s', 's', @priv_pos + 1, 'Cloud providers: search', 1);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'cloudprov-c', 'c', @priv_pos + 1, 'Cloud providers: control', 1);
UPDATE priv SET identifier='cloudprov-v' WHERE id_type=@type_id AND operation='v';
UPDATE priv SET identifier='cloudprov-m' WHERE id_type=@type_id AND operation='m';
-- RESULT

/*****************************************************************************/