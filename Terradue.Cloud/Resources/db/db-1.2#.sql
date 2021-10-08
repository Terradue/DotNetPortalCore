USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "cloudprov" (add new type reference) ... \
ALTER TABLE cloudprov
    ADD COLUMN id_type int unsigned AFTER id,
    ADD CONSTRAINT fk_cloudprov_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-01a

/*****************************************************************************/

-- Changing cloud provider type references ... \
UPDATE cloudprov AS t SET id_type = (SELECT t1.id FROM type AS t1 INNER JOIN exttype AS t2 ON t1.class_name = t2.class_name WHERE t2.id = t.id_exttype);
-- RESULT
-- CHECKPOINT C-01b

/*****************************************************************************/

-- Changing structure of table "cloudprov" (remove old type reference) ... \
ALTER TABLE cloudprov
    CHANGE COLUMN id_type id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    DROP FOREIGN KEY fk_cloudprov_exttype,
    DROP COLUMN id_exttype
;
-- RESULT
-- CHECKPOINT C-01c

/*****************************************************************************/

-- Changing structure of table "cloud" (add new type reference) ... \
ALTER TABLE cloud
    ADD COLUMN id_type int unsigned AFTER id,
    ADD CONSTRAINT fk_cloud_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-02a

/*****************************************************************************/

-- Changing cloud appliance type references ... \
UPDATE cloud AS t SET id_type = (SELECT t1.id FROM type AS t1 INNER JOIN exttype AS t2 ON t1.class_name = t2.class_name WHERE t2.id = t.id_exttype);
-- RESULT
-- CHECKPOINT C-02b

/*****************************************************************************/

-- Changing structure of table "cloud" (remove old type reference) ... \
ALTER TABLE cloud
    CHANGE COLUMN id_type id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    DROP FOREIGN KEY fk_cloud_exttype,
    DROP COLUMN id_exttype
;
-- RESULT
-- CHECKPOINT C-02c

/*****************************************************************************/
