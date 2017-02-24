USE $MAIN$;

/*****************************************************************************/

-- Update table domain ... \
ALTER TABLE domain 
CHANGE COLUMN name name VARCHAR(200) NULL COMMENT 'Unique name' ,
ADD COLUMN identifier VARCHAR(100) NOT NULL AFTER `id`,
ADD UNIQUE INDEX identifier_UNIQUE (`identifier` ASC),
DROP INDEX name ;

UPDATE domain SET identifier=name;
-- RESULT