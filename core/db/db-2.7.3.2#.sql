USE $MAIN$;

/*****************************************************************************/

-- Update table remote resource ... \
ALTER TABLE resourceset 
CHANGE COLUMN `is_default` `kind` TINYINT(4) NULL DEFAULT '0' COMMENT 'resource set kind' ;
-- RESULT

-- Update table activity ... \
ALTER TABLE activity 
ADD COLUMN id_domain int unsigned;
-- RESULT