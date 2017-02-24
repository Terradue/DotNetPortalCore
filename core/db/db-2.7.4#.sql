USE $MAIN$;

/*****************************************************************************/

-- TRY START
ALTER TABLE domain 
DROP INDEX name;
-- TRY END

-- TRY START
ALTER TABLE domain 
DROP INDEX name_UNIQUE;
-- TRY END

-- Update table domain ... \
ALTER TABLE domain 
CHANGE COLUMN name name VARCHAR(200) NULL COMMENT 'Unique name' ,
ADD COLUMN identifier VARCHAR(100) NOT NULL AFTER `id`,
ADD UNIQUE INDEX identifier_UNIQUE (`identifier` ASC);

UPDATE domain SET identifier=name;
-- RESULT