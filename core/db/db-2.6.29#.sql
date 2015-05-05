USE $MAIN$;

/*****************************************************************************/

-- feature table
ALTER TABLE feature
ADD COLUMN `pos` INT UNSIGNED NULL AFTER `id`;
ALTER TABLE feature 
CHANGE COLUMN `button_link` `button_link` VARCHAR(1000) NULL DEFAULT NULL ;
-- RESULT

/*****************************************************************************/