USE $MAIN$;

/*****************************************************************************/

-- Add feature position
ALTER TABLE feature
ADD COLUMN `pos` INT UNSIGNED NULL AFTER `id`;
-- RESULT

/*****************************************************************************/