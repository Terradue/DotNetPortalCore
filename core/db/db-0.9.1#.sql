/*****************************************************************************/

-- Updating configuration variables
SET @section = (SELECT id FROM configsection WHERE caption='Users');
UPDATE config SET pos=pos+4 WHERE id_section=@section AND pos>1;
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section, 2, 'DefaultAllowPasswords', 'bool', NULL, 'Allow Password Authentication as Default', 'If checked, new user accounts have password authentication enabled, otherwise it is disabled; this may be overridden for specific accounts', 'true', true),
    (@section, 3, 'ForceStrongPasswords', 'bool', NULL, 'Force Users to Use Strong Passwords', 'If checked, the user accounts must have a password containing at least 8 characters and at least one upper-case character, one lower-case character, one digit and one special character', NULL, true),
    (@section, 4, 'PasswordExpireTime', 'string', NULL, 'Password Expiration Time', 'Enter the period after which a password expires and has to be changed; use quantifiers D (days), W (weeks), M (months), e.g. 3M; leave empty if password does not expire', '3M', true),
    (@section, 5, 'MaxFailedLogins', 'int', NULL, 'Maximum Failed Login Attempts', 'Enter the number of unsuccessful authentication attempts for a user account after which the account is blocked; leave empty if account should never be blocked', '5', true)
;

-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" (enhanced security) ... \
ALTER TABLE usr
    CHANGE COLUMN enabled enabled tinyint default 2 COMMENT 'see lookup list "enabledStatus"',
    ADD COLUMN allow_password boolean default true COMMENT 'If true, password authentication is allowed' AFTER enabled,
    ADD COLUMN last_password_change_time datetime COMMENT 'Date/time of last password change',
    ADD COLUMN failed_logins int default 0 COMMENT 'Number of failed login attempts after last successful login'
;
UPDATE usr SET enabled=2 WHERE enabled=1;
-- RESULT

/*****************************************************************************/

INSERT INTO action (pos, name, caption, description) VALUES
    (7, 'password', 'Password expiration check', 'This action temporarily deactivates user accounts with expired passwords. Users can reactivete their accounts through the recovery function.')
;

/*****************************************************************************/

INSERT INTO lookuplist (system, name) VALUES (true, 'enabledStatus');
SET @list = (SELECT LAST_INSERT_ID());
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list, 1, 'Disabled (not recoverable by user)', '0'),
    (@list, 2, 'Deactivated (recoverable by user)', '1'),
    (@list, 3, 'Enabled', '2')
;

/*****************************************************************************/
