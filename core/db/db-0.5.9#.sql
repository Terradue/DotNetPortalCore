/*****************************************************************************/

-- Adding entries to table "config" (external authentication) ... \
UPDATE config SET pos=pos+2 WHERE id_section=1 AND pos>=10;
-- NORESULT
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (1, 10, 'ExternalAuthentication', 'bool', NULL, 'Allow external authentication', 'If checked, users authenticated by a trustable external authentication mechanism (defined in a configuration file) can use the web portal without using the inbuilt user/password authentication', 'false', true),
    (1, 11, 'AutoUserGeneration', 'bool', NULL, 'Automatic generation of user accounts', 'If checked, new user accounts can be generated automatically when trustable authentication information of an unknown user is received', 'false', true)
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" (external authentication) ... \
ALTER TABLE usr
    ADD COLUMN ext_login boolean COMMENT 'True if external authentication is allowed' AFTER level
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "grp" (default user groups for new users) ... \
ALTER TABLE grp
    ADD COLUMN is_default boolean default false COMMENT 'True if group is automatically selected for new users'
;
-- RESULT

/*****************************************************************************/
