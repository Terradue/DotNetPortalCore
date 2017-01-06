USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "service" (adding tags) ... \
ALTER TABLE service
	ADD COLUMN tags VARCHAR(150) NULL DEFAULT NULL COMMENT 'Tags describing the service';
-- RESULT

/*****************************************************************************/
