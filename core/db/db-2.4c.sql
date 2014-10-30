USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "exttype" (mandatory reference to entity base type) ... \
DELETE FROM exttype WHERE id_basetype IS NULL;
ALTER TABLE exttype
    DROP COLUMN entity_code,
    CHANGE COLUMN id_basetype id_basetype int unsigned NOT NULL COMMENT 'FK: Entity base type'
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "filter" (mandatory reference to entity base type) ... \
DELETE FROM filter WHERE id_basetype IS NULL;
ALTER TABLE filter
    DROP COLUMN entity_code,
    CHANGE COLUMN id_basetype id_basetype int unsigned NOT NULL COMMENT 'FK: Entity base type'
;
-- RESULT

/*****************************************************************************/

-- Removing table "privilege" ... \
DROP TABLE privilege;
-- RESULT

/*****************************************************************************/
