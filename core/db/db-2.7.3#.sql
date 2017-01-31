USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "usrreg" (adding date and origin) ... \
ALTER TABLE usrreg 
	ADD COLUMN reg_date DATETIME NULL,
	ADD COLUMN reg_origin VARCHAR(50) NULL DEFAULT NULL;
-- RESULT

UPDATE usrreg SET usrreg.reg_date = (SELECT log_time FROM usrsession WHERE usrreg.id_usr=usrsession.id_usr ORDER BY usrsession.log_time ASC LIMIT 1);
UPDATE usrreg SET reg_date = '2017-01-01 00:00:00' WHERE reg_date IS NULL;

/*****************************************************************************/


