/*****************************************************************************/

-- Change structure of table "taskfilter" ... \
ALTER TABLE taskfilter CHANGE COLUMN name caption varchar(50) NOT NULL COMMENT 'Caption', DROP FOREIGN KEY taskfilter_ibfk_1, DROP INDEX id_usr;
-- RESULT

/*****************************************************************************/

-- Removing duplicate series ... \
DELETE FROM series AS s WHERE id>(SELECT MIN(id) FROM series WHERE name=s.name);
-- RESULT

-- Removing unnecessary series fields ... \
ALTER TABLE series DROP COLUMN legend, DROP COLUMN search_attr, DROP COLUMN collection_name, DROP COLUMN browse_name, DROP COLUMN start_date, DROP COLUMN stop_date, DROP COLUMN company, DROP COLUMN satellite, DROP COLUMN sensor, CHANGE COLUMN logo_url logo_url varchar(200) COMMENT 'Logo/icon URL' AFTER cat_template;
-- RESULT

-- Creating unique index on series name ... \
ALTER TABLE series ADD UNIQUE INDEX (name);
-- RESULT

/*****************************************************************************/

-- Changing structure of table "task" ... \
ALTER TABLE task CHANGE COLUMN new_status next_status tinyint unsigned COMMENT 'Desired next status', ADD COLUMN access_time datetime COMMENT 'Date/time of last access';
-- RESULT

/*****************************************************************************/

-- Encrypting user passwords ... \
UPDATE usr SET password=PASSWORD(password);
-- RESULT

/*****************************************************************************/

UPDATE action SET caption='Grid engine pending operations' WHERE id=1;
UPDATE action SET caption='Task cleanup' WHERE id=2;
UPDATE action SET caption='Task scheduler' WHERE id=3;
UPDATE action SET caption='Catalogue series refresh' WHERE id=4;

/*****************************************************************************/

