USE $MAIN$;

/*****************************************************************************/

CREATE TABLE type (
    id int unsigned NOT NULL auto_increment,
    id_module int unsigned COMMENT 'FK: Implementing module',
    id_super int unsigned COMMENT 'FK: Super-type',
    pos smallint unsigned COMMENT 'Position for ordering',
    class varchar(100) NOT NULL COMMENT 'Fully qualified name of the class',
    generic_class varchar(100) COMMENT 'Fully qualified name of the .NET/Mono class replacing abstract classes',
    custom_class varchar(100) COMMENT 'Fully qualified name of the alternative .NET/Mono class',
    caption_sg varchar(100) COMMENT 'Caption (singular)',
    caption_pl varchar(100) COMMENT 'Caption (plural) displayed in admin index page',
    keyword varchar(50) COMMENT 'Keyword used in admin interface URLs',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon for admin index page',
    CONSTRAINT pk_type PRIMARY KEY (id),
    CONSTRAINT fk_type_module FOREIGN KEY (id_module) REFERENCES module(id) ON DELETE CASCADE,
    CONSTRAINT fk_type_super FOREIGN KEY (id_super) REFERENCES type(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Entity types';
-- CHECKPOINT C-01a

-- Adding basic entity types ... \
INSERT INTO type (id_module, pos, class, generic_class, custom_class, caption_pl, keyword, icon_url) SELECT id_module, pos, class_name, generic_class, custom_class, caption, keyword, icon_url FROM basetype;
-- RESULT
-- CHECKPOINT C-01b

-- Adding entity type extensions ... \
INSERT INTO type (id_module, id_super, pos, class, caption_sg) SELECT t.id_module, t2.id, t.pos, t.class_name, t.caption FROM exttype AS t INNER JOIN basetype AS t1 ON t.id_basetype = t1.id INNER JOIN type AS t2 ON t1.class_name = t2.class;
-- RESULT
-- CHECKPOINT C-01c

CREATE PROCEDURE add_type(IN p_module_id int unsigned, IN p_class varchar(100), IN p_super_class varchar(100), IN p_caption_sg varchar(100), IN p_caption_pl varchar(100), IN p_keyword varchar(100))
COMMENT 'Inserts or updates a basic entity type'
BEGIN
    DECLARE type_id int;
    DECLARE type_pos int;
    IF p_super_class IS NOT NULL THEN
        SELECT id FROM type WHERE class = p_super_class INTO type_id;
        SELECT CASE WHEN MAX(pos) IS NULL THEN 0 ELSE MAX(pos) END FROM type WHERE id_super = type_id INTO type_pos;
    ELSE
        SELECT CASE WHEN MAX(pos) IS NULL THEN 0 ELSE MAX(pos) END FROM type INTO type_pos;
    END IF;
    INSERT INTO type (id_module, id_super, pos, class, caption_sg, caption_pl) VALUES (p_module_id, type_id, type_pos, p_class, p_caption_sg, p_caption_pl);
END;
-- CHECKPOINT C-01d

-- Adding Web Processing Service Provider type ... \
CALL add_type (NULL, 'Terradue.Portal.WpsProvider, Terradue.Portal', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'Web Processing Service Provider', 'Web Processing Service Providers', NULL);
-- RESULT
-- CHECKPOINT C-01e

-- Adding script-based portal service type ... \
CALL add_type (NULL, 'Terradue.Portal.ScriptBasedService, Terradue.Portal', 'Terradue.Portal.Service, Terradue.Portal', 'Script-based service', 'Script-based services', NULL);
-- RESULT
-- CHECKPOINT C-01f

-- Adding WPS process offering service type ... \
CALL add_type (NULL, 'Terradue.Portal.WpsProcessOffering, Terradue.Portal', 'Terradue.Portal.Service, Terradue.Portal', 'WPS process offering', 'WPS process offerings', NULL);
-- RESULT
-- CHECKPOINT C-01g

/*****************************************************************************/

-- Changing structure of table "priv" (add new type reference) ... \
ALTER TABLE priv
    ADD COLUMN id_type int unsigned AFTER id,
    ADD CONSTRAINT fk_priv_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-02a

-- Changing manager privilege type referencess ... \
UPDATE priv AS t SET id_type = (SELECT t1.id FROM type AS t1 INNER JOIN basetype AS t2 ON t1.class = t2.class_name WHERE t2.id = t.id_basetype);
-- RESULT
-- CHECKPOINT C-02b

-- Changing structure of table "priv" (complete new and remove old type reference) ... \
ALTER TABLE priv
    CHANGE COLUMN id_type id_type int unsigned COMMENT 'FK: Entity type',
    DROP FOREIGN KEY fk_priv_basetype,
    DROP COLUMN id_basetype
;
-- RESULT
-- CHECKPOINT C-02c

/*****************************************************************************/

-- Changing structure of table "config" (alignment) ... \
ALTER TABLE config
    CHANGE COLUMN optional optional boolean NOT NULL DEFAULT false COMMENT 'If true, no value is required'
;
-- RESULT
-- CHECKPOINT C-03a

-- Aligning configuration variables (Site) ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Site');
UPDATE config SET id_section = @section_id, pos = 1, type = 'bool', source = NULL, caption = 'Web Site Available', hint = 'If checked, this web site is available to all users, otherwise only to administrators', optional = true WHERE name = 'Available';
UPDATE config SET id_section = @section_id, pos = 2, type = 'text', source = NULL, caption = 'Message for Site Unavailability', hint = 'Enter the message that is displayed in case of site unavailability', optional = true WHERE name = 'UnavailabilityMessage';
UPDATE config SET id_section = @section_id, pos = 3, type = 'string', source = NULL, caption = 'Site Name', hint = 'Enter the main title for this web portal displayed on the home page', optional = false WHERE name = 'SiteName';
UPDATE config SET id_section = @section_id, pos = 4, type = 'string', source = NULL, caption = 'Company Name', hint = 'Enter the name of the company/organization of the web portal', optional = false WHERE name = 'CompanyName';
UPDATE config SET id_section = @section_id, pos = 5, type = 'string', source = NULL, caption = 'Company Short Name', hint = 'Enter a short or abbreviated name of the company/organization', optional = false WHERE name = 'CompanyShortName';
UPDATE config SET id_section = @section_id, pos = 6, type = 'url', source = NULL, caption = 'Company URL', hint = 'Enter the URL of your company''s/organization''s web site', optional = false WHERE name = 'CompanyUrl';
UPDATE config SET id_section = @section_id, pos = 7, type = 'email', source = NULL, caption = 'Company E-mail Contact', hint = 'Enter a contact e-mail address of your company/organization', optional = false WHERE name = 'CompanyEmail';
UPDATE config SET id_section = @section_id, pos = 8, type = 'string', source = NULL, caption = 'Copyright Text', hint = 'Enter the text of the copyright', optional = false WHERE name = 'CopyrightText';
UPDATE config SET id_section = @section_id, pos = 9, type = 'url', source = NULL, caption = 'Site Base URL', hint = 'Enter the absolute URL of the folder containing the site''s homepage (or simply the hostname if the site''s main access point is in the web server root folder)', optional = false WHERE name = 'BaseUrl';
UPDATE config SET id_section = @section_id, pos = 10, type = 'url', source = NULL, caption = 'Sign In Redirection URL', hint = 'Enter the URL of the page to which users are redirected after successful login', optional = false WHERE name = 'SignInRedirectUrl';
UPDATE config SET id_section = @section_id, pos = 11, type = 'string', source = NULL, caption = 'Control Panel Root URL', hint = 'Enter the relative URL of the control panel main page', optional = true WHERE name = 'AdminRootUrl';
UPDATE config SET id_section = @section_id, pos = 12, type = 'string', source = NULL, caption = 'Account Functionality Root URL', hint = 'Enter the relative URL of the account main page', optional = true WHERE name = 'AccountRootUrl';
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (1, 13, 'ServiceRootUrl', 'string', NULL, 'Service Root URL', 'Enter the root URL containing the service access points (in the definition field of a service the "$(SERVICEROOT)" placeholder represents this value)', '/services', false)
;
UPDATE config SET id_section = @section_id, pos = 14, type = 'string', source = NULL, caption = 'Workspace URL', hint = 'Enter the relative URL of the task workspace main page', optional = true WHERE name = 'TaskWorkspaceRootUrl';
UPDATE config SET id_section = @section_id, pos = 15, type = 'string', source = NULL, caption = 'Workspace Job Directory Name', hint = 'Enter the name of the directory containing job information for a workspace task', optional = true WHERE name = 'TaskWorkspaceJobDir';
UPDATE config SET id_section = @section_id, pos = 16, type = 'string', source = NULL, caption = 'Scheduler Workspace URL', hint = 'Enter the relative URL of the scheduler workspace main page', optional = true WHERE name = 'SchedulerWorkspaceRootUrl';
UPDATE config SET id_section = @section_id, pos = 17, type = 'string', source = NULL, caption = 'Default XSLT file', hint = 'Enter the full path of the XSLT file for the default transformation of portal''s XML output to HTML', optional = true WHERE name = 'DefaultXslFile';
-- RESULT
-- CHECKPOINT C-03b

-- Aligning configuration variables (Server) ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Server');
UPDATE config SET id_section = @section_id, pos = 1, type = 'string', source = NULL, caption = 'Trusted hosts', hint = 'Enter the IP addresses of the hosts trusted by this site separated by comma', optional = true WHERE name = 'TrustedHosts';
UPDATE config SET id_section = @section_id, pos = 2, type = 'string', source = NULL, caption = 'Host Certificate File (PKCS#12)', hint = 'Enter the full path of the host certificate file (PKCS#12 format)', optional = true WHERE name = 'HostCertFile';
UPDATE config SET id_section = @section_id, pos = 3, type = 'password', source = NULL, caption = 'Host Certificate Password [currently ignored]', hint = '[Do not use] Enter the password for the host certificate file', optional = true WHERE name = 'HostCertPassword';
UPDATE config SET id_section = @section_id, pos = 4, type = 'string', source = NULL, caption = 'Service Root Folder', hint = 'Enter the root folder containing the service folders on the web portal machine''s file system (in the definition field of a service the "$(SERVICEROOT)" placeholder represents this value)', optional = false WHERE name = 'ServiceFileRoot';
UPDATE config SET id_section = @section_id, pos = 5, type = 'string', source = NULL, caption = 'Service Parameter Root Folder', hint = 'Enter the root folder containing the service configurable parameter values in files on the web portal machine''s file system (in the pattern attribute of a service''s <param> element, the "$(PARAMROOT)" placeholder represents this value)', optional = true WHERE name = 'ServiceParamFileRoot';
UPDATE config SET id_section = @section_id, pos = 6, type = 'string', source = NULL, caption = 'FTP Root Folder', hint = 'Enter the root folder for the task result files on the web portal machine''s file system', optional = false WHERE name = 'FtpRoot';
UPDATE config SET id_section = @section_id, pos = 7, type = 'email', source = NULL, caption = 'Mail Sender Address', hint = 'Enter the default sender address for e-mails (e.g. alerts for administrators or account registration mails for users)', optional = false WHERE name = 'MailSenderAddress';
UPDATE config SET id_section = @section_id, pos = 8, type = 'string', source = NULL, caption = 'Mail Sender Display Name', hint = 'Enter the default sender display name for e-mails (e.g. alerts for administrators or account registration mails for users)', optional = true WHERE name = 'MailSender';
UPDATE config SET id_section = @section_id, pos = 9, type = 'string', source = NULL, caption = 'SMTP Server Hostname', hint = 'Enter the hostname that is used for sending e-mails', optional = true WHERE name = 'SmtpHostname';
UPDATE config SET id_section = @section_id, pos = 10, type = 'string', source = NULL, caption = 'SMTP Username', hint = 'Enter the username on the SMTP server account that is used for sending e-mails', optional = true WHERE name = 'SmtpUsername';
UPDATE config SET id_section = @section_id, pos = 11, type = 'password', source = NULL, caption = 'SMTP Password', hint = 'Enter the password for the SMTP server account that is used for sending e-mails', optional = true WHERE name = 'SmtpPassword';
UPDATE config SET id_section = @section_id, pos = 12, type = 'string', source = NULL, caption = 'Change Log Start Time', hint = 'Enter the start date/time of the period for which the changelog must be conserved, relative to the current date(e.g. -3M, -2M D=1)', optional = true WHERE name = 'ChangeLogStart';
-- RESULT
-- CHECKPOINT C-03c

-- Aligning configuration variables (Users) ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Users');
UPDATE config SET id_section = @section_id, pos = 1, type = 'bool', source = NULL, caption = 'Allow external authentication', hint = 'If checked, users authenticated by a trustable external authentication mechanism (defined in a configuration file) can use the web portal without using the inbuilt user/password authentication', optional = true WHERE name = 'ExternalAuthentication';
UPDATE config SET id_section = @section_id, pos = 2, type = 'bool', source = NULL, caption = 'Allow Self-registration for Everybody', hint = 'If checked, users may register on the portal without being trusted by a central identity provider', optional = true WHERE name = 'AllowSelfRegistration';
UPDATE config SET id_section = @section_id, pos = 3, type = 'int', source = 'userActRule', caption = 'User Account Activation', hint = 'Select the rule for the activation of user accounts', optional = false WHERE name = 'AccountActivation';
UPDATE config SET id_section = @section_id, pos = 4, type = 'string', source = NULL, caption = 'Disabled User Profile Fields', hint = 'Enter the names of user profile fields that users cannot change (separated by comma)', optional = true WHERE name = 'DisabledProfileFields';
UPDATE config SET id_section = @section_id, pos = 5, type = 'int', source = 'rule', caption = 'Allow Password Authentication for Normal Accounts', hint = 'Select when to allow password authentication for normal accounts', optional = false WHERE name = 'AllowPassword';
UPDATE config SET id_section = @section_id, pos = 6, type = 'int', source = 'openIdRule', caption = 'Allow OpenID Authentication for Normal Accounts', hint = 'Select when to allow OpenID authentication for normal accounts', optional = true WHERE name = 'AllowOpenId';
UPDATE config SET id_section = @section_id, pos = 7, type = 'int', source = 'rule', caption = 'Allow Trusted Sessionless Authentication for Normal Accounts', hint = 'Select when to allow sessionless requests from trusted hosts for normal accounts (needed for task scheduling etc.)', optional = false WHERE name = 'AllowSessionless';
UPDATE config SET id_section = @section_id, pos = 8, type = 'int', source = 'rule', caption = 'Require Requests from Trusted Hosts for Normal Accounts', hint = 'Select when to require requests from trusted hosts for normal accounts', optional = false WHERE name = 'ForceTrusted';
UPDATE config SET id_section = @section_id, pos = 9, type = 'int', source = 'rule', caption = 'Require Client Certificate for Normal Accounts', hint = 'Select when to require a client certificate for normal accounts', optional = false WHERE name = 'ForceSsl';
UPDATE config SET id_section = @section_id, pos = 10, type = 'bool', source = NULL, caption = 'Force Users to Use Strong Passwords', hint = 'If checked, the user accounts must have a password containing at least 8 characters and at least one upper-case character, one lower-case character, one digit and one special character', optional = true WHERE name = 'ForceStrongPasswords';
UPDATE config SET id_section = @section_id, pos = 11, type = 'timespan', source = NULL, caption = 'Password Expiration Time', hint = 'Enter the period after which a password expires and has to be changed; use quantifiers D (days), W (weeks), M (months), e.g. 3M; leave empty if password does not expire', optional = true WHERE name = 'PasswordExpireTime';
UPDATE config SET id_section = @section_id, pos = 12, type = 'int', source = NULL, caption = 'Maximum Failed Login Attempts', hint = 'Enter the number of unsuccessful authentication attempts for a user account after which the account is blocked; leave empty if account should never be blocked', optional = true WHERE name = 'MaxFailedLogins';
UPDATE config SET id_section = @section_id, pos = 13, type = 'timespan', source = NULL, caption = 'Validity of OpenID Response Nonces', hint = 'Select the maximum validity of a response nonce in a positive authentication assertion, use quantifiers h (hours), m (minutes), s (seconds), e.g. 10m', optional = true WHERE name = 'OpenIdNonceValidity';
UPDATE config SET id_section = @section_id, pos = 14, type = 'int', source = NULL, caption = 'Default Result Folder Size Per User (MB)', hint = 'Enter the default size of the task result folder per user', optional = true WHERE name = 'DefaultResultFolderSize';
UPDATE config SET id_section = @section_id, pos = 15, type = 'int', source = NULL, caption = 'Default Lifetime of Tasks (days)', hint = 'Enter the number of days that a task and its results are kept by default after conclusion', optional = true WHERE name = 'DefaultTaskLifeTime';
UPDATE config SET id_section = @section_id, pos = 16, type = 'string', source = NULL, caption = 'Mail Subject for User Registration', hint = 'Enter the subject for the e-mail to be sent to new users', optional = true WHERE name = 'RegistrationMailSubject';
UPDATE config SET id_section = @section_id, pos = 17, type = 'bool', source = NULL, caption = 'Format User Registration Mail as HTML', hint = 'If checked, the e-mail to be sent to new users is formatted as HTML mail, use appropriate markup if formatting is desired', optional = true WHERE name = 'RegistrationMailHtml';
UPDATE config SET id_section = @section_id, pos = 18, type = 'text', source = NULL, caption = 'Mail Body for User Registration', hint = 'Enter the body for the e-mail to be sent to new users, use the $(USERNAME), $(PASSWORD), $(SERVICES), $(SERIES) placeholders in the appropriate places', optional = true WHERE name = 'RegistrationMailBody';
UPDATE config SET id_section = @section_id, pos = 19, type = 'string', source = NULL, caption = 'Mail Subject for Password Reset', hint = 'Enter the subject for the e-mail to be sent to sent to users that have requested a new password', optional = true WHERE name = 'PasswordResetMailSubject';
UPDATE config SET id_section = @section_id, pos = 20, type = 'bool', source = NULL, caption = 'Format Password Reset Mail as HTML', hint = 'If checked, the e-mail to be sent to users that have requested a new password is formatted as HTML mail, use appropriate markup if formatting is desired', optional = true WHERE name = 'PasswordResetMailHtml';
UPDATE config SET id_section = @section_id, pos = 21, type = 'text', source = NULL, caption = 'Mail Body for Password Reset', hint = 'Enter the body for the e-mail to be sent to users that have requested a new password, use the $(USERNAME), $(PASSWORD), $(SERVICES), $(SERIES) placeholders in the appropriate places', optional = true WHERE name = 'PasswordResetMailBody';
DELETE FROM config WHERE name = 'AutoUserGeneration';
-- RESULT
-- CHECKPOINT C-03d

-- Aligning configuration variables (Tasks) ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Tasks');
UPDATE config SET id_section = @section_id, pos = 1, type = 'bool', source = NULL, caption = 'Perform Task Operations Synchronously', hint = 'If checked, tasks are submitted, aborted or deleted directly on the processing environment upon user request, otherwise the background agent performs the requested operation later', optional = false WHERE name = 'SyncTaskOperations';
UPDATE config SET id_section = @section_id, pos = 2, type = 'int', source = 'taskSubmitRetry', caption = 'Submission Retrying', hint = 'Select the policy for interactively created tasks that cannot be submitted immediately (e.g. for capacity reasons)', optional = true WHERE name = 'TaskRetry';
UPDATE config SET id_section = @section_id, pos = 3, type = 'timespan', source = NULL, caption = 'Default Submission Retrying Period', hint = 'Enter the default length of the time period after the submission of a task during which the background agent tries to submit the task again', optional = true WHERE name = 'TaskRetryPeriod';
UPDATE config SET id_section = @section_id, pos = 4, type = 'string', source = NULL, caption = 'Task Priority Multipliers', hint = 'Enter a list of supported task priorities, each consisting of a multiplier value (unsigned real number) for the cost calculation and a caption (syntax: weight1:caption1;weight2:caption2;...)', optional = false WHERE name = 'PriorityValues';
UPDATE config SET id_section = @section_id, pos = 5, type = 'string', source = NULL, caption = 'Task Result Compression Types', hint = 'Enter a list of supported compression types for task results, each consisting of a name and a caption (syntax: name1:caption1;name2:caption2;...)', optional = false WHERE name = 'CompressionValues';
UPDATE config SET id_section = @section_id, pos = 6, type = 'url', source = NULL, caption = 'Task Flow URL', hint = 'Enter the URL of the script creating a task''s flow graphics', optional = false WHERE name = 'TaskFlowUrl';
UPDATE config SET id_section = @section_id, pos = 7, type = 'timespan', source = NULL, caption = 'Computing Resource Status Validity', hint = 'Enter the time period after which the capacity information of a computing resource expires', optional = false WHERE name = 'ComputingResourceStatusValidity';
-- RESULT
-- CHECKPOINT C-03e

-- Aligning configuration variables (Processing) ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Processing');
UPDATE config SET id_section = @section_id, pos = 1, type = 'int', source = NULL, caption = 'Default Maximum Number of Nodes per Job', hint = 'Enter the default maximum number of single nodes onto which a job can split', optional = false WHERE name = 'DefaultMaxNodesPerJob';
UPDATE config SET id_section = @section_id, pos = 2, type = 'int', source = NULL, caption = 'Default Minimum Number of Arguments per Node', hint = 'Enter the default minimum number of arguments (input files) to be processed on a single node', optional = false WHERE name = 'DefaultMinArgumentsPerNode';
UPDATE config SET id_section = @section_id, pos = 3, type = 'int', source = NULL, caption = 'Result Publishing Waiting Time Before Retry (sec)', hint = 'Enter the interval before a retry if the publishing of the task results failed', optional = true WHERE name = 'PublishRetryWaitTime';
UPDATE config SET id_section = @section_id, pos = 4, type = 'int', source = NULL, caption = 'Result Publishing Retries', hint = 'Enter the number of retries if the publishing of the task results failed', optional = true WHERE name = 'PublishRetryTimes';
UPDATE config SET id_section = @section_id, pos = 5, type = 'string', source = NULL, caption = 'Site Config File', hint = 'Enter the name of the site configuration file', optional = false WHERE name = 'SiteConfigFile';
UPDATE config SET id_section = @section_id, pos = 6, type = 'string', source = NULL, caption = 'Virtual Organization', hint = 'Enter the name of the virtual organization', optional = false WHERE name = 'VirtualOrganization';
-- RESULT
-- CHECKPOINT C-03f

-- Aligning configuration variables (Background Agent) ... \
SET @section_id = (SELECT id FROM configsection WHERE name = 'Background Agent');
UPDATE config SET id_section = @section_id, pos = 1, type = 'int', source = NULL, caption = 'Agent Execution Interval (sec)', hint = 'Enter the base interval in seconds between two executions of the background agent', optional = false WHERE name = 'AgentInterval';
UPDATE config SET id_section = @section_id, pos = 2, type = 'string', source = NULL, caption = 'Agent Log File', hint = 'Enter the name for the log output of the background agent (use "$DATE" as placeholder for the current date)', optional = false WHERE name = 'AgentLogFile';
UPDATE config SET id_section = @section_id, pos = 3, type = 'int', source = 'logLevel', caption = 'Agent Log Level', hint = 'Select the desired degree of detail for the background agent log file', optional = false WHERE name = 'AgentLogLevel';
UPDATE config SET id_section = @section_id, pos = 4, type = 'string', source = NULL, caption = 'Agent Username', hint = 'Enter the username of the user on whose behalf the agent is running', optional = true WHERE name = 'AgentUser';
-- RESULT
-- CHECKPOINT C-03g

/*****************************************************************************/

-- Changing structure of table "action" (column names) ... \
ALTER TABLE action
    CHANGE COLUMN class_name class varchar(100) COMMENT 'Fully qualified name of class implementing action method',
    CHANGE COLUMN method_name method varchar(50) COMMENT 'Name of action method'
;
-- RESULT
-- CHECKPOINT C-04a

/*****************************************************************************/

-- Changing structure of table "lookuplist" (alignment) ... \
ALTER TABLE lookuplist
    CHANGE COLUMN system system boolean NOT NULL DEFAULT false COMMENT 'If true, list is predefined and locked'
;
-- RESULT
-- CHECKPOINT C-05a

-- Changing resource availability values ... \
SET @list_id = (SELECT id FROM lookuplist WHERE name = 'resourceAvailability');
DELETE FROM lookup WHERE id_list = @list_id;
-- NORESULT
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list_id, 1, 'Disabled', '0'),
    (@list_id, 2, 'Only administrators', '1'),
    (@list_id, 3, 'Administrators and managers', '2'),
    (@list_id, 4, 'Administrators, managers and developers', '3'),
    (@list_id, 5, 'All authorized users', '4')
;
-- RESULT
-- CHECKPOINT C-05b

/*****************************************************************************/

-- Changing structure of table "usr" (alignment) ... \
UPDATE usr SET status = 4 WHERE status IS NULL;
UPDATE usr SET normal_account = false WHERE normal_account IS NULL;
UPDATE usr SET allow_password = true WHERE allow_password IS NULL;
UPDATE usr SET force_ssl = false WHERE force_ssl IS NULL;
UPDATE usr SET time_zone = 'UTC' WHERE time_zone IS NULL;
UPDATE usr SET level = 1 WHERE level IS NULL;
UPDATE usr SET debug_level = 0 WHERE debug_level IS NULL;
UPDATE usr SET simple_gui = false WHERE simple_gui IS NULL;
UPDATE usr SET credits = 0 WHERE credits IS NULL;
UPDATE usr SET failed_logins = 0 WHERE failed_logins IS NULL;

ALTER TABLE usr
    CHANGE COLUMN status status tinyint NOT NULL DEFAULT 4 COMMENT 'Account status, see lookup list "accountStatus"',
    CHANGE COLUMN normal_account normal_account boolean NOT NULL DEFAULT false COMMENT 'If true, auth/n settings are made in general configuration',
    CHANGE COLUMN allow_password allow_password boolean NOT NULL DEFAULT true COMMENT 'If true, password authentication is allowed',
    CHANGE COLUMN force_ssl force_ssl boolean NOT NULL DEFAULT false COMMENT 'If true, accept only SSL authentication',
    CHANGE COLUMN time_zone time_zone char(25) NOT NULL DEFAULT 'UTC' COMMENT 'Time zone',
    CHANGE COLUMN level level tinyint unsigned NOT NULL DEFAULT 1 COMMENT '1: User, 2: Developer, 3: Admin',
    CHANGE COLUMN debug_level debug_level tinyint unsigned NOT NULL DEFAULT 0 COMMENT 'Debug level (admins only), 3..6',
    CHANGE COLUMN simple_gui simple_gui boolean NOT NULL DEFAULT false COMMENT 'If true, simplified GUI is selected',
    CHANGE COLUMN credits credits int unsigned NOT NULL DEFAULT 0 COMMENT 'Maximum resource credits for the user',
    CHANGE COLUMN failed_logins failed_logins int NOT NULL DEFAULT 0 COMMENT 'Number of failed login attempts after last successful login'
;
-- RESULT
-- CHECKPOINT C-06a

/*****************************************************************************/

-- Changing structure of table "usrcert" (alignment) ... \
ALTER TABLE usrcert
    CHANGE COLUMN own_cert own_cert boolean NOT NULL DEFAULT false COMMENT 'If true, user makes his own certificate settings'
;
-- RESULT
-- CHECKPOINT C-07a

/*****************************************************************************/

-- Changing structure of table "usrreg" (alignment) ... \
ALTER TABLE usrreg
    CHANGE COLUMN reset reset boolean NOT NULL DEFAULT false COMMENT 'If true, password reset was requested'
;
-- RESULT
-- CHECKPOINT C-08a

/*****************************************************************************/

-- Changing structure of table "filter" (add new type reference) ... \
ALTER TABLE filter
    ADD COLUMN id_type int unsigned AFTER id_usr,
    ADD CONSTRAINT fk_filter_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-09a

-- Changing filter type references ... \
UPDATE filter AS t SET id_type = (SELECT t1.id FROM type AS t1 INNER JOIN basetype AS t2 ON t1.class = t2.class_name WHERE t2.id = t.id_basetype);
-- RESULT
-- CHECKPOINT C-09b

-- Changing structure of table "filter" (complete new and remove old type reference) ... \
ALTER TABLE filter
    CHANGE COLUMN id_type id_type int unsigned NOT NULL COMMENT 'FK: Entity type',
    DROP FOREIGN KEY fk_filter_basetype,
    DROP COLUMN id_basetype
;
-- RESULT
-- CHECKPOINT C-09c

/*****************************************************************************/

-- Changing structure of table "grp" (alignment) ... \
ALTER TABLE grp
    CHANGE COLUMN conf_deleg conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, group can be configured by other domains',
    CHANGE COLUMN is_default is_default boolean NOT NULL DEFAULT false COMMENT 'If true, group is automatically selected for new users',
    CHANGE COLUMN all_resources all_resources boolean NOT NULL DEFAULT false COMMENT 'If true, new resources are automatically added to group'
;
-- RESULT
-- CHECKPOINT C-10a

/*****************************************************************************/

-- Changing structure of table "cr" (add new type reference) ... \
ALTER TABLE cr
    ADD COLUMN id_type int unsigned AFTER id,
    ADD CONSTRAINT fk_cr_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-11a

-- Changing computing resource type references ... \
UPDATE cr AS t SET id_type = (SELECT t1.id FROM type AS t1 INNER JOIN exttype AS t2 ON t1.class = t2.class_name WHERE t2.id = t.id_exttype);
-- RESULT
-- CHECKPOINT C-11b

-- Adjusting availability values ... \
UPDATE cr SET availability = availability + 1 WHERE availability in (2, 3);
-- RESULT
-- CHECKPOINT C-11c

-- Changing structure of table "cr" (complete new and remove old type reference, alignment) ... \
ALTER TABLE cr
    CHANGE COLUMN id_type id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    CHANGE COLUMN conf_deleg conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, computing resource can be configured by other domains',
    CHANGE COLUMN availability availability tinyint NOT NULL DEFAULT 4 COMMENT 'Availability (0..4)',
    CHANGE COLUMN hostname hostname varchar(100) COMMENT 'Hostname',
    CHANGE COLUMN capacity capacity int unsigned NOT NULL DEFAULT 0 COMMENT 'Maximum processing capacity',
    CHANGE COLUMN credit_control credit_control boolean NOT NULL DEFAULT false COMMENT 'If true, computing resource controls user credits',
    DROP FOREIGN KEY fk_cr_exttype,
    DROP COLUMN id_exttype
;
-- RESULT
-- CHECKPOINT C-11d

-- Changing structure of table "cr_priv" (alignment) ... \
ALTER TABLE cr_priv
    CHANGE COLUMN credits credits int unsigned NOT NULL DEFAULT 0 COMMENT 'Maximum resource credits for the user'
;
-- RESULT
-- CHECKPOINT C-11e

/*****************************************************************************/

-- Changing structure of table "ce" (alignment) ... \
ALTER TABLE ce
    CHANGE COLUMN status_method status_method tinyint NOT NULL DEFAULT 0 COMMENT 'Status request method'
;
-- RESULT
-- CHECKPOINT C-12a

/*****************************************************************************/

-- Changing structure of table "cedir" (alignment) ... \
ALTER TABLE cedir
    CHANGE COLUMN available available boolean NOT NULL DEFAULT true COMMENT 'If true, directory is available'
;
-- RESULT
-- CHECKPOINT C-13a

/*****************************************************************************/

CREATE TABLE wpsprovider (
    id int unsigned NOT NULL,
    url varchar(100) COMMENT 'Base WPS access point',
    CONSTRAINT pk_wpsprovider PRIMARY KEY (id),
    CONSTRAINT fk_wpsprovider_cr FOREIGN KEY (id) REFERENCES cr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Web Processing Service (WPS) providers';
-- CHECKPOINT C-14a

/*****************************************************************************/

-- Changing structure of table "catalogue" (alignment) ... \
ALTER TABLE catalogue
    CHANGE COLUMN conf_deleg conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, catalogue can be configured by other domains'
;
-- RESULT
-- CHECKPOINT C-15a

/*****************************************************************************/

-- Changing structure of table "series" (add type reference) ... \
ALTER TABLE series
    ADD COLUMN id_type int unsigned AFTER id,
    ADD COLUMN default_mime_type varchar(50) NOT NULL DEFAULT 'application/atom+xml' COMMENT 'Default MimeType for OpenSearch' AFTER cat_template,
    ADD CONSTRAINT fk_series_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    DROP INDEX name
;
-- RESULT
-- CHECKPOINT C-16a

-- Changing series type references ... \
SET @type_id = (SELECT id FROM type WHERE class = 'Terradue.Portal.Series, Terradue.Portal');
UPDATE series SET id_type = @type_id;
-- RESULT
-- CHECKPOINT C-16b

-- Changing structure of table "series" (complete type reference) ... \
ALTER TABLE series
    CHANGE COLUMN id_type id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    CHANGE COLUMN conf_deleg conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, series can be configured by other domains',
    CHANGE COLUMN auto_refresh auto_refresh boolean NOT NULL DEFAULT true COMMENT 'If true, template is refreshed by the background agent',
    ADD UNIQUE INDEX (identifier)
;
-- RESULT
-- CHECKPOINT C-16c

DROP PROCEDURE IF EXISTS add_series;

CREATE PROCEDURE add_series(IN p_type_id int unsigned, IN p_identifier varchar(50), IN p_name varchar(200), IN p_description text, IN p_cat_description varchar(200))
COMMENT 'Inserts or updates a series'
BEGIN
    DECLARE series_id int;
    SELECT id FROM series WHERE identifier = p_identifier INTO series_id;
    IF series_id IS NULL THEN
        INSERT INTO series (id_type, identifier, name, description, cat_description) VALUES (p_type_id, p_identifier, p_name, p_description, p_cat_description);
    END IF;
END;
-- CHECKPOINT C-16d

/*****************************************************************************/

-- TRY START
ALTER TABLE producttype
    DROP INDEX name
;
-- TRY END

-- Changing structure of table "producttype" (alignment) ... \
ALTER TABLE producttype
    ADD UNIQUE INDEX (identifier)
;
-- RESULT
-- CHECKPOINT C-17a

/*****************************************************************************/

-- Changing structure of table "product" (alignment) ... \
ALTER TABLE product
    CHANGE COLUMN id_producttype id_producttype int unsigned NOT NULL COMMENT 'FK: Product type'
;
-- RESULT
-- CHECKPOINT C-18a

/*****************************************************************************/

-- Changing structure of table "productdata" (alignment) ... \
ALTER TABLE productdata
    CHANGE COLUMN id_product id_product int unsigned NOT NULL COMMENT 'Related product'
;
-- RESULT
-- CHECKPOINT C-19a

/*****************************************************************************/

-- TRY START
CREATE TABLE resourceset (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    name varchar(50) COMMENT 'Name',
    is_default boolean DEFAULT false COMMENT 'If true, resource set is selected by default',
    CONSTRAINT pk_resourceset PRIMARY KEY (id),
    UNIQUE INDEX (identifier),
    CONSTRAINT fk_resourceset_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Sets of remote resources';
-- TRY END

CREATE TABLE resourceset_priv (
    id_resourceset int unsigned NOT NULL COMMENT 'FK: Resource set',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_resourceset_priv_resourceset FOREIGN KEY (id_resourceset) REFERENCES resourceset(id) ON DELETE CASCADE,
    CONSTRAINT fk_resourceset_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_resourceset_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on resource sets';
-- CHECKPOINT C-20a

/*****************************************************************************/

-- Changing structure of table "pubserver" (alignment) ... \
ALTER TABLE pubserver
    CHANGE COLUMN conf_deleg conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, publish server can be configured by other domains',
    CHANGE COLUMN path path varchar(100) NOT NULL DEFAULT '/' COMMENT 'Task result root path on host',
    CHANGE COLUMN is_default is_default boolean NOT NULL DEFAULT false COMMENT 'If true, publish server is selected by default',
    CHANGE COLUMN metadata metadata boolean NOT NULL DEFAULT false COMMENT 'If true, publish server is used for task result metadata, e.g. previews',
    CHANGE COLUMN delete_files delete_files boolean NOT NULL DEFAULT false COMMENT 'If true, delete also task result files when task is deleted'
;
-- RESULT
-- CHECKPOINT C-21a

/*****************************************************************************/

-- Changing structure of table "service" (add type reference) ... \
ALTER TABLE service
    ADD COLUMN id_type int unsigned AFTER id,
    ADD CONSTRAINT fk_service_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    DROP INDEX name
;
-- RESULT
-- CHECKPOINT C-22a

-- Changing service type references ... \
SET @type_id = (SELECT id FROM type WHERE class = 'Terradue.Portal.ScriptBasedService, Terradue.Portal');
UPDATE service SET id_type = @type_id;
-- RESULT
-- CHECKPOINT C-22b

CREATE TABLE scriptservice (
    id int unsigned NOT NULL,
    root varchar(200) NOT NULL COMMENT 'Directory containing service''s service.xml',
    CONSTRAINT pk_scriptservice PRIMARY KEY (id),
    CONSTRAINT fk_scriptservice_service FOREIGN KEY (id) REFERENCES service(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Script-based portal services';
-- CHECKPOINT C-22c

-- Adding existing script-based services ... \
INSERT INTO scriptservice (id, root) SELECT id, root FROM service;
-- RESULT
-- CHECKPOINT C-22d

-- Changing structure of table "service" (complete type reference and remove outsourced field) ... \
ALTER TABLE service
    CHANGE COLUMN id_type id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    CHANGE COLUMN conf_deleg conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, service can be configured by other domains',
    CHANGE COLUMN root url varchar(200) NOT NULL COMMENT 'Access point of service (relative URL)' AFTER version,
    ADD UNIQUE INDEX (identifier)
;
-- RESULT
-- CHECKPOINT C-22e

/*****************************************************************************/

CREATE TABLE wpsproc (
    id int unsigned NOT NULL,
    id_provider int unsigned NOT NULL COMMENT 'FK: WPS provider',
    process_id varchar(100) NOT NULL COMMENT 'Process identifier on WPS provider',
    CONSTRAINT pk_wpsproc PRIMARY KEY (id),
    CONSTRAINT fk_wpsproc_service FOREIGN KEY (id) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpsproc_provider FOREIGN KEY (id_provider) REFERENCES wpsprovider(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'WPS process offerings';

-- CHECKPOINT C-23a

/*****************************************************************************/

-- Changing structure of table "scheduler" (add new type reference) ... \
ALTER TABLE scheduler
    ADD COLUMN id_type int unsigned AFTER id,
    ADD CONSTRAINT fk_scheduler_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
;
-- RESULT
-- CHECKPOINT C-24a

-- Changing scheduler type references ... \
UPDATE scheduler AS t SET id_type = (SELECT t1.id FROM type AS t1 INNER JOIN exttype AS t2 ON t1.class = t2.class_name WHERE t2.id = t.id_exttype);
-- RESULT
-- CHECKPOINT C-24b

-- Changing structure of table "scheduler" (complete new and remove old type reference, alignment) ... \
ALTER TABLE scheduler
    CHANGE COLUMN id_type id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    CHANGE COLUMN max_cr_usage max_cr_usage tinyint NOT NULL DEFAULT 0 COMMENT 'Maximum usage (in %) of computing resource for tasks of the scheduler',
    CHANGE COLUMN max_submit max_submit smallint NOT NULL DEFAULT 1 COMMENT 'Maximum number of tasks submitted per scheduler cycle',
    CHANGE COLUMN min_files min_files smallint unsigned NOT NULL DEFAULT 1 COMMENT 'Minimum number of files to process (data-driven)',
    CHANGE COLUMN max_files max_files smallint unsigned NOT NULL DEFAULT 1 COMMENT 'Maximum number of files to process (data-driven)',
    DROP FOREIGN KEY fk_scheduler_exttype,
    DROP COLUMN id_exttype
;
-- RESULT
-- CHECKPOINT C-24c

/*****************************************************************************/

-- Changing structure of table "taskgroup" (alignment) ... \
ALTER TABLE taskgroup
    CHANGE COLUMN requests requests int unsigned NOT NULL DEFAULT 0 COMMENT 'Number of result requests received'
;
-- RESULT
-- CHECKPOINT C-25a

/*****************************************************************************/

-- Changing structure of table "task" (alignment) ... \
ALTER TABLE task
    CHANGE COLUMN empty empty boolean NOT NULL DEFAULT false COMMENT 'Task has no input files',
    ADD COLUMN status_url varchar(200) COMMENT 'Status URL if CR provides them explicitely (e.g. WPS)' AFTER remote_id
;
-- RESULT
-- CHECKPOINT C-26a

/*****************************************************************************/

-- TRY START
CREATE TABLE log (
    id int unsigned NOT NULL auto_increment,
    log_time datetime NOT NULL,
    thread varchar(255) NOT NULL,
    level varchar(20) NOT NULL,
    logger varchar(255) NOT NULL,
    message varchar(4000) NOT NULL,
    post_parameters text,
    CONSTRAINT pk_log PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'System log entries';
-- TRY END

-- Changing structure of table "log" (column names) ... \
ALTER TABLE log
    CHANGE COLUMN log_time timestamp datetime NOT NULL,
    CHANGE COLUMN logger reporter varchar(255) NOT NULL,
    ADD COLUMN originator varchar(255) AFTER message,
    CHANGE COLUMN post_parameters parameters text,
    ADD COLUMN user varchar(255),
    ADD COLUMN url varchar(255),
    ADD COLUMN action varchar(255)
;
-- RESULT
-- CHECKPOINT C-27a

/*****************************************************************************/

USE $NEWS$;

/*****************************************************************************/

-- Changing structure of table "project" (alignment) ... \
ALTER TABLE project
    CHANGE COLUMN status status tinyint NOT NULL DEFAULT 0 COMMENT 'Project status (1 to 4)'
;
-- RESULT
-- CHECKPOINT C-28a

/*****************************************************************************/
