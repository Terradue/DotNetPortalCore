USE $MAIN$;

/*****************************************************************************/

-- Alter "resourceset" table ... \
ALTER TABLE resourceset ADD COLUMN `creation_date` TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP AFTER `access_key`;
-- RESULT

/*****************************************************************************/