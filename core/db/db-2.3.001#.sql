/*****************************************************************************/

-- Changing configuration variables ... \
UPDATE config SET pos=1 WHERE name='OpenIdAuth';
UPDATE config SET pos=2 WHERE name='OpenIdNonceValidity';
UPDATE config SET pos=3 WHERE name='SelfDefUsers';
UPDATE config SET pos=4 WHERE name='DefaultAllowPasswords';
UPDATE config SET pos=5 WHERE name='ForceStrongPasswords';
UPDATE config SET pos=6 WHERE name='PasswordExpireTime';
UPDATE config SET pos=7 WHERE name='MaxFailedLogins';
UPDATE config SET pos=8 WHERE name='DefaultResultFolderSize';
UPDATE config SET pos=9 WHERE name='DefaultTaskLifeTime';
UPDATE config SET pos=10 WHERE name='RegistrationMailSubject';
UPDATE config SET pos=11 WHERE name='RegistrationMailHtml';
UPDATE config SET pos=12 WHERE name='RegistrationMailBody';
UPDATE config SET pos=13 WHERE name='PasswordResetMailSubject';
UPDATE config SET pos=14 WHERE name='PasswordResetMailHtml';
UPDATE config SET pos=15 WHERE name='PasswordResetMailBody';
-- NORESULT

SET @section = (SELECT id FROM configsection WHERE caption='Users');
UPDATE config SET pos = pos + 5 WHERE id_section=@section AND pos>=8;
UPDATE config SET pos = pos + 4 WHERE id_section=@section AND pos>=5 AND pos<8;
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section, 2, 'AccountActivation', 'int', 'userActRule', 'User Account Activation', 'Select the rule for the activation of user accounts', '0', false),
    (@section, 3, 'DisabledProfileFields', 'string', NULL, 'Disabled User Profile Fields', 'Enter the names of user profile fields that users cannot change (separated by comma)', NULL, true),
    (@section, 6, 'AllowSessionless', 'int', 'rule', 'Allow Trusted Sessionless Authentication for Normal Accounts', 'Select when to allow sessionless requests from trusted hosts for normal accounts (needed for task scheduling etc.)', '0', false),
    (@section, 7, 'ForceTrusted', 'int', 'rule', 'Require Requests from Trusted Hosts for Normal Accounts', 'Select when to require requests from trusted hosts for normal accounts', '0', false),
    (@section, 8, 'ForceSsl', 'int', 'rule', 'Require Client Certificate for Normal Accounts', 'Select when to require a client certificate for normal accounts', '0', false)
;
SET @allow_password = (SELECT CASE WHEN value = 'true' THEN '1' ELSE '0' END FROM config WHERE name='AllowPassword');
SET @account_act = (SELECT value FROM config WHERE name='AllowSelfRegistration'); 
SET @allow_self_reg = (SELECT CASE WHEN value = 0 THEN 'false' ELSE 'true' END FROM config WHERE name='AllowSelfRegistration');
UPDATE config SET value=@account_act WHERE name='AccountActivation';
UPDATE config SET pos=1, name='AllowSelfRegistration', type='bool', source=NULL, caption='Allow Self-registration for Everybody', hint='If checked, users may register on the portal without being trusted by a central identity provider', value=@allow_self_reg WHERE name='SelfDefUsers';
UPDATE config SET pos=4, name='AllowPassword', type='int', source='rule', caption='Allow Password Authentication for Normal Accounts', hint='Select when to allow password authentication for normal accounts', value=@allow_password, optional=false WHERE name='DefaultAllowPasswords';
UPDATE config SET pos=5, name='AllowOpenId', caption='Allow OpenID Authentication for Normal Accounts', hint='Select when to allow OpenID authentication for normal accounts' WHERE name='OpenIdAuth';
UPDATE config SET pos=12, caption='Validity of OpenID Response Nonces', hint='Select the maximum validity of a response nonce in a positive authentication assertion, use quantifiers h (hours), m (minutes), s (seconds), e.g. 10m' WHERE name='OpenIdNonceValidity';
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" ... \
ALTER TABLE usr
    ADD COLUMN normal_account boolean NOT NULL default false COMMENT 'If true, auth/n settings are made in general configuration' AFTER status,
    DROP COLUMN allow_ext_login
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usrcert" ... \
ALTER TABLE usrcert
    ADD COLUMN cert_subject varchar(255) COMMENT 'Certificate subject',
    ADD COLUMN cert_content_pem varchar(10000) COMMENT 'Certificate content (PEM)',
    ADD COLUMN cert_content_base64 varchar(10000) COMMENT 'Certificate content (Base64-encoded P12)',
    DROP COLUMN cert_content,
    DROP COLUMN cert_content_hex
;
-- RESULT

/*****************************************************************************/

-- Add lookup list for general rules ... \
INSERT INTO lookuplist (system, name) VALUES
    (true, 'rule')
;

SET @list = (SELECT LAST_INSERT_ID());
 
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list, 1, '[no general rule]', '0'),
    (@list, 2, 'Never', '1'),
    (@list, 3, 'Always', '2')
;
-- RESULT

-- Change name of lookup list for account activation ... \
UPDATE lookuplist SET name='userActRule' WHERE name='userDefRule';
-- RESULT

/*****************************************************************************/
