/*****************************************************************************/

-- Adding protocol lookup list and values ... \
INSERT INTO lookuplist (system, name) VALUES (true, 'protocol');
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 1, 'FTP', 'ftp' FROM lookuplist WHERE name='protocol';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 2, 'SFTP', 'sftp' FROM lookuplist WHERE name='protocol';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 3, 'SCP', 'scp' FROM lookuplist WHERE name='protocol';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 4, 'GSIFTP', 'gsiftp' FROM lookuplist WHERE name='protocol';
-- RESULT

/*****************************************************************************/

-- Adding service rating lookup list and values ... \
INSERT INTO lookuplist (system, name) VALUES (true, 'rating');
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 1, '1', '1' FROM lookuplist WHERE name='rating';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 2, '2', '2' FROM lookuplist WHERE name='rating';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 3, '3', '3' FROM lookuplist WHERE name='rating';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 4, '4', '4' FROM lookuplist WHERE name='rating';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 5, '5', '5' FROM lookuplist WHERE name='rating';
-- RESULT

/*****************************************************************************/
