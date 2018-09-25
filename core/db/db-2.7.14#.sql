USE $MAIN$;

/*****************************************************************************/

-- Update table provider ... \
ALTER TABLE wpsprovider ADD COLUMN `stage` boolean NOT NULL DEFAULT true AFTER `autosync`;
-- RESULT
