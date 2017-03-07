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

-- Update table domain (1) ... \
ALTER TABLE domain 
CHANGE COLUMN name name VARCHAR(200) NULL COMMENT 'name' ,
ADD COLUMN identifier VARCHAR(100) NULL AFTER `id`,
ADD UNIQUE INDEX identifier_UNIQUE (`identifier` ASC);
-- RESULT

-- Update table domain (2) ... \
UPDATE domain SET identifier=name;
-- RESULT

-- Update table domain (3) ... \
ALTER TABLE domain 
CHANGE COLUMN identifier identifier VARCHAR(100) NOT NULL COMMENT 'Unique identifier';
-- RESULT
