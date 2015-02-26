USE $MAIN$;

/*****************************************************************************/

-- Alter "resourceset" table ... \
ALTER TABLE resourceset ADD COLUMN `creation_date` DATETIME NULL DEFAULT NOW() AFTER `access_key`;
-- RESULT

/*****************************************************************************/