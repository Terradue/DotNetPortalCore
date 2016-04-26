USE $MAIN$;

/*****************************************************************************/
-- Alter table service... \
ALTER TABLE service 
CHANGE COLUMN description description TEXT NULL DEFAULT NULL COMMENT 'Description' ;
ALTER TABLE service 
CHANGE COLUMN version version VARCHAR(30) NULL DEFAULT NULL COMMENT 'Version' ;

-- RESULT

/*****************************************************************************/
