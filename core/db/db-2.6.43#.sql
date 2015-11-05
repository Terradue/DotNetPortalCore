USE $MAIN$;

/*****************************************************************************/
-- Alter table feature... \
ALTER TABLE feature 
ADD COLUMN image_credits VARCHAR(150) NULL DEFAULT NULL AFTER image_style;
-- RESULT

/*****************************************************************************/
