/*****************************************************************************/

-- Changing entries in "configsection" ... \
UPDATE configsection SET caption='Background Daemon' WHERE caption='Actions';
-- RESULT


-- Changing entries in "config" ... \
UPDATE config SET caption='Allow automatic generation of user accounts', hint='If checked, new user accounts can be generated when trustable authentication information of an unknown user is received' WHERE name='AutoUserGeneration';
UPDATE config SET hint='Enter the absolute URL of the folder containing the site''s homepage (or simply the hostname if the site''s main access point is in the web server root folder)' WHERE name='BaseUrl';
UPDATE config SET hint='Enter a list of supported task priorities, each consisting of a multiplier value (unsigned real number) for the cost calculation and a caption (syntax: weight1:caption1;weight2:caption2;...)' WHERE name='PriorityValues';
UPDATE config SET name='DefaultMaxNodesPerJob', caption='Default Maximum Number of Nodes per Job' WHERE name='DefaultJobMaxNodes';
UPDATE config SET name='DefaultMinArgumentsPerNode', caption='Default Minimum Number of Arguments per Node' WHERE name='DefaultNodeMinArguments';
UPDATE config SET name='DaemonInterval', caption='Daemon Execution Interval (sec)' WHERE name='ActionInterval';
UPDATE config SET name='DaemonLogFile', caption='Daemon Log File' WHERE name='ActionLogFile';
UPDATE config SET name='DaemonLogLevel', caption='Daemon Log Level' WHERE name='ActionLogLevel';
-- RESULT


-- Adding entries to table "config" (mailing) ... \
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (2, 9, 'MailSender', 'string', NULL, 'Mail Sender Address', 'Enter the default sender address for e-mails (e.g. alerts for administrators or account registration mails for users)', NULL, true),
    (2, 10, 'SmtpHostname', 'string', NULL, 'SMTP Server Hostname', 'Enter the hostname that is used for sending e-mails', NULL, true),
    (2, 11, 'SmtpUsername', 'string', NULL, 'SMTP Username', 'Enter the username on the SMTP server account that is used for sending e-mails', NULL, true),
    (2, 12, 'SmtpPassword', 'password', NULL, 'SMTP Password', 'Enter the password for the SMTP server account that is used for sending e-mails', NULL, true),
    (3, 3, 'RegistrationMailSubject', 'string', NULL, 'Mail Subject for User Registration', 'Enter the subject for the e-mail to be sent to new users', 'Registration', true),
    (3, 4, 'RegistrationMailHtml', 'bool', NULL, 'Format User Registration Mail as HTML', 'If checked, the e-mail to be sent to new users is formatted as HTML mail, use appropriate markup if formatting is desired', 'false', true),
    (3, 5, 'RegistrationMailBody', 'text', NULL, 'Mail Body for User Registration', 'Enter the body for the e-mail to be sent to new users, use the $USERNAME, $PASSWORD, $SERVICES, $SERIES placeholders in the appropriate places', 'Username: $USERNAME\nPassword: $PASSWORD\nAvailable services: $SERVICES\nAvailable series: $SERIES', true),
    (3, 6, 'PasswordResetMailSubject', 'string', NULL, 'Mail Subject for Password Reset', 'Enter the subject for the e-mail to be sent to sent to users that have requested a new password', 'New password', true),
    (3, 7, 'PasswordResetMailHtml', 'bool', NULL, 'Format Password Reset Mail as HTML', 'If checked, the e-mail to be sent to users that have requested a new password is formatted as HTML mail, use appropriate markup if formatting is desired', 'false', true),
    (3, 8, 'PasswordResetMailBody', 'text', NULL, 'Mail Body for Password Reset', 'Enter the body for the e-mail to be sent to users that have requested a new password, use the $USERNAME, $PASSWORD, $SERVICES, $SERIES placeholders in the appropriate places', 'Username: $USERNAME\nNew password: $PASSWORD\nAvailable services: $SERVICES\nAvailable series: $SERIES', true)
;
-- RESULT

-- Changing structure of table "usr" ... \
ALTER TABLE usr
  CHANGE COLUMN resources credits int unsigned default '0' COMMENT 'Maximum resource credits for the user'
;
-- RESULT

/*****************************************************************************/
