/*****************************************************************************/

UPDATE config SET pos=pos+1 WHERE id_section=3;
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (3, 1, 'SelfDefUsers', 'int', 'userDefRule', 'Self-defined User Accounts', 'Select the rule for the creation of user-accounts by unauthenticated/unregistered users', '0', true)
;
-- NORESULT

/*****************************************************************************/

-- Adding user account self-definition rule lookup list and values ... \
INSERT INTO lookuplist (system, name) VALUES (true, 'userDefRule');
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 1, 'Disabled', '0' FROM lookuplist WHERE name='userDefRule';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 2, 'Activate accounts after administrator approval', '1' FROM lookuplist WHERE name='userDefRule';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 3, 'Activate accounts immediately', '2' FROM lookuplist WHERE name='userDefRule';
-- RESULT

/*****************************************************************************/
