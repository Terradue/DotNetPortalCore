USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "wpsprovider" (adding tags) ... \
ALTER TABLE wpsprovider
	ADD COLUMN tags VARCHAR(150) NULL DEFAULT NULL COMMENT 'Tags describing the provider';
-- RESULT

/*****************************************************************************/
