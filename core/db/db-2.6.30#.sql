USE $MAIN$;

/*****************************************************************************/

-- feature table
ALTER TABLE feature
CHANGE COLUMN `title` `title` VARCHAR(40) NOT NULL ,
CHANGE COLUMN `description` `description` VARCHAR(150) NULL DEFAULT NULL ,
CHANGE COLUMN `button_link` `button_link` VARCHAR(1000) NULL DEFAULT NULL ;
-- RESULT

/*****************************************************************************/