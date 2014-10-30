/*****************************************************************************/

-- Adding debug level lookup list and values ... \
INSERT INTO lookuplist (system, name) VALUES (true, 'resourceAvailability');
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 1, 'Disabled', '0' FROM lookuplist WHERE name='resourceAvailability';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 2, 'Only administrators', '1' FROM lookuplist WHERE name='resourceAvailability';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 3, 'Administrators and developers', '2' FROM lookuplist WHERE name='resourceAvailability';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 4, 'All authorized users', '3' FROM lookuplist WHERE name='resourceAvailability';
-- RESULT

-- Changing structure of table "ce" (availability) ... \
ALTER TABLE ce
    ADD COLUMN availability tinyint default 3 COMMENT 'Availability (0..3)' AFTER id_ce_monitor
;
-- RESULT

/*****************************************************************************/
