USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "type" (multi-domain) ... \
ALTER TABLE type
    ADD COLUMN multi_domain boolean NOT NULL default false COMMENT 'If true, entity can be assigned to multiple domains' AFTER  custom_class
;
-- RESULT
-- CHECKPOINT C-01

/*****************************************************************************/

CREATE TABLE domainassign (
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type',
    id int unsigned NOT NULL COMMENT 'FK: Entity ID',
    id_domain int unsigned NOT NULL COMMENT 'FK: Domain'
) Engine=InnoDB COMMENT 'Assignments of entities to multiple domains';
-- CHECKPOINT C-02

/*****************************************************************************/
