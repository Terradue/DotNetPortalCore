/*****************************************************************************/

-- Adding configuration variable for Computing Element status validity ... \
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (4, 11, 'ComputingElementStatusValidity', 'timespan', NULL, 'Computing Element Status Validity', 'Enter the time period after which the capacity information of a Computing Element expires', '5m', false)
;
-- RESULT

/*****************************************************************************/

-- Setting default status request methods for Computing Elements ... \
UPDATE ce SET status_method=1 WHERE status_method IS NULL OR status_method!=2;
-- RESULT

/*****************************************************************************/

-- Renaming lookup lists ... \
UPDATE lookuplist SET name='taskStatusRequest' WHERE name='statusRequest';
UPDATE config SET type='int', source='taskStatusRequest' WHERE source='statusRequest';
-- RESULT

-- Adding lookup list for Computing Element status request methods ... \
INSERT INTO lookuplist (system, name) VALUES (true, 'ceStatusRequest');
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 1, 'Status requesting disabled', '1' FROM lookuplist WHERE name='ceStatusRequest'; 
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 2, 'Ganglia XML', '2' FROM lookuplist WHERE name='ceStatusRequest'; 
-- RESULT

/*****************************************************************************/
