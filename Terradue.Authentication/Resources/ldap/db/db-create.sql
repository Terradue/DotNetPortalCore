-- VERSION 1.4

USE $MAIN$;

/*****************************************************************************/

SET @pos = (SELECT MAX(pos) FROM auth);
INSERT INTO auth (pos, identifier, name, description, type, enabled) VALUES 
    (@pos + 1, 'ldap', 'LDAP authentication', 'LDAP authentication allows users to identify themselves using Terradue LDAP provider.', 'Terradue.Authentication.Ldap.LdapAuthenticationType, Terradue.Authentication.Ldap', '4')
;
-- RESULT

INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('ldap-baseurl', 'string', 'Json2Ldap baseurl', 'Json2Ldap baseurl', 'http://ldap.terradue.int:8080/json2ldap/', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('ldapauth-baseurl', 'string', 'LdapAuth baseurl', 'LdapAuth baseurl', 'http://ldap.terradue.int:8080/ldapauth/', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('ldap-admin-dn', 'string', 'Ldap admin distinguished name', 'Ldap admin distinguished name', '', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('ldap-admin-pwd', 'string', 'Ldap admin distinguished name', 'Ldap admin distinguished name', '', '0');
-- RESULT

/*****************************************************************************/
