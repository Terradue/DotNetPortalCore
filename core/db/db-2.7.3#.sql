USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "usrreg" (adding date and origin) ... \
ALTER TABLE usrreg 
	ADD COLUMN reg_date DATETIME NULL,
	ADD COLUMN reg_origin VARCHAR(50) NULL DEFAULT NULL;
-- RESULT

/*****************************************************************************/


