USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "cookie" (adding username) ... \
ALTER TABLE cookie ADD COLUMN `username` VARCHAR(100) NULL DEFAULT NULL AFTER `session`;
-- RESULT

/*****************************************************************************/