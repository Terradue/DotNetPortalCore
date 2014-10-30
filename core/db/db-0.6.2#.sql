/*****************************************************************************/

-- Changing order of daemon actions ... \
UPDATE action SET pos=1 WHERE name='taskstatus';
UPDATE action SET pos=2 WHERE name='task';
UPDATE action SET pos=3 WHERE name='taskdelete';
UPDATE action SET pos=4 WHERE name='scheduler';
UPDATE action SET pos=5 WHERE name='series';
-- RESULT

/*****************************************************************************/

-- Changing structure of table "filter" ... \
ALTER TABLE filter
    DROP COLUMN descr,
    CHANGE COLUMN url definition text COMMENT 'Query string parameters defining the filter'
;
-- RESULT

-- Changing filter URLs (remove script name) ... \
UPDATE filter SET definition=REPLACE(definition, '/admin/task.filter.aspx?', '?');
-- RESULT

/*****************************************************************************/
