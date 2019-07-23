-- VERSION 2.7.17

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

-- Initializing basic entity types ... \
INSERT INTO type (id, pos, class, generic_class, caption_sg, caption_pl, keyword) VALUES
    (1, 1, 'Terradue.Portal.Configuration, Terradue.Portal', NULL, 'General Configuration', 'General Configuration', 'config'),
    (2, 2, 'Terradue.Portal.Action, Terradue.Portal', NULL, 'Agent Action', 'Agent Actions', 'actions'),
    (3, 3, 'Terradue.Portal.Application, Terradue.Portal', NULL, 'External Application', 'External Applications', 'applications'),
    (4, 4, 'Terradue.Portal.Domain, Terradue.Portal', NULL, 'Domains', 'Domain', 'domains'),
    (5, 5, 'Terradue.Portal.Role, Terradue.Portal', NULL, 'Role', 'Roles', 'roles'),
    (6, 6, 'Terradue.Portal.OpenIdProvider, Terradue.Portal', NULL, 'OpenID Provider', 'OpenID Providers', 'openid-providers'),
    (7, 7, 'Terradue.Portal.LookupList, Terradue.Portal', NULL, 'Shared Lookup List', 'Shared Lookup Lists', 'lookup-lists'),
    (8, 8, 'Terradue.Portal.ServiceClass, Terradue.Portal', NULL, 'Service Class', 'Service Classes', 'service-classes'),
    (9, 9, 'Terradue.Portal.ServiceCategory, Terradue.Portal', NULL, 'Service Category', 'Service Categories', 'service-categories'),
    (10, 10, 'Terradue.Portal.SchedulerClass, Terradue.Portal', NULL, 'Scheduler Class', 'Scheduler Classes', 'scheduler-classes'),
    (11, 11, 'Terradue.Portal.User, Terradue.Portal', NULL, 'User', 'Users', 'users'),
    (12, 12, 'Terradue.Portal.Group, Terradue.Portal', NULL, 'Group', 'Groups', 'groups'),
    (13, 13, 'Terradue.Portal.LightGridEngine, Terradue.Portal', NULL, 'LGE Instance', 'LGE Instances', 'lge-instances'),
    (14, 14, 'Terradue.Portal.ComputingResource, Terradue.Portal', 'Terradue.Portal.GenericComputingResource, Terradue.Portal', 'Computing Resource', 'Computing Resources', 'computing-resources'),
    (15, 15, 'Terradue.Portal.Catalogue, Terradue.Portal', NULL, 'Metadata catalogue', 'Metadata catalogues', 'catalogues'),
    (16, 16, 'Terradue.Portal.Series, Terradue.Portal', NULL, 'Dataset Series', 'Dataset Series', 'series'),
    (17, 17, 'Terradue.Portal.ProductType, Terradue.Portal', NULL, 'Product Type', 'Product Types', 'product-types'),
    (18, 18, 'Terradue.Portal.PublishServer, Terradue.Portal', NULL, 'Publish Server', 'Publish Servers', 'publish-servers'),
    (19, 19, 'Terradue.Portal.Service, Terradue.Portal', 'Terradue.Portal.GenericService, Terradue.Portal', 'Service', 'Services', 'services'),
    (20, 20, 'Terradue.Portal.Scheduler, Terradue.Portal', NULL, 'Scheduler', 'Schedulers', 'schedulers'),
    (21, 21, 'Terradue.Portal.SchedulerRunConfiguration, Terradue.Portal', NULL, 'Scheduler run configuration', 'Schedulers run configurations', 'schedulers-run-configs'),
    (22, 22, 'Terradue.Portal.Task, Terradue.Portal', NULL, 'Task', 'Tasks', 'tasks'),
    (23, 23, 'Terradue.Portal.Article, Terradue.Portal', NULL, 'News Article', 'News', 'news'),
    (24, 24, 'Terradue.Portal.Image, Terradue.Portal', NULL, 'Image', 'Images', 'images'),
    (25, 25, 'Terradue.Portal.Faq, Terradue.Portal', NULL, 'F.A.Q.', 'F.A.Q.', 'faqs'),
    (26, 26, 'Terradue.Portal.Project, Terradue.Portal', NULL, 'Project', 'Projects', 'projects'),
    (27, 27, 'Terradue.Portal.Activity, Terradue.Portal', NULL, 'Activity', 'Activities', 'activity')
;
-- RESULT
-- CHECKPOINT C-01b

-- Initializing standard extended entity types ... \
INSERT INTO type (id_super, pos, class, caption_sg, caption_pl) VALUES
    (14, 1, 'Terradue.Portal.GlobusComputingElement, Terradue.Portal', 'LGE/Globus Computing Element', 'LGE/Globus Computing Elements'),
    (14, 2, 'Terradue.Portal.WpsProvider, Terradue.Portal', 'Web Processing Service Provider', 'Web Processing Service Providers'),
    (19, 1, 'Terradue.Portal.ScriptBasedService, Terradue.Portal', 'Script-based service', 'Script-based services'),
    (19, 2, 'Terradue.Portal.WpsProcessOffering, Terradue.Portal', 'WPS process offering', 'WPS process offerings'),
    (20, 1, 'Terradue.Portal.CustomScheduler, Terradue.Portal', 'Custom action scheduler', 'Custom action schedulers'),
    (21, 1, 'Terradue.Portal.TimeDrivenRunConfiguration, Terradue.Portal', 'Time-driven scheduler run configuration', 'Time-driven scheduler run configurations'),
    (21, 2, 'Terradue.Portal.DataDrivenRunConfiguration, Terradue.Portal', 'Data-driven scheduler run configuration', 'Data-driven scheduler run configurations')
;
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
    INSERT INTO type (id_module, id_super, pos, class, caption_sg, caption_pl, keyword) VALUES (p_module_id, type_id, type_pos + 1, p_class, p_caption_sg, p_caption_pl, p_keyword);
END;
-- CHECKPOINT C-01b

CREATE PROCEDURE change_type(IN p_class varchar(100), IN p_generic_class varchar(100), IN p_pos int unsigned)
COMMENT 'Changes the generic class and/or the position of an entity type'
BEGIN
    DECLARE type_id int;
    DECLARE super_id int;
    DECLARE type_pos int;
    SELECT id, id_super, pos FROM type WHERE class = p_class INTO type_id, super_id, type_pos;
    UPDATE type SET generic_class = p_generic_class WHERE id = type_id;
    IF p_pos > 0 THEN
        UPDATE type SET pos = pos + 1 WHERE CASE WHEN super_id IS NULL THEN id_super IS NULL ELSE id_super = super_id END AND pos >= p_pos AND CASE WHEN type_pos IS NULL THEN true ELSE pos < type_pos END;
        UPDATE type SET pos = p_pos WHERE id = type_id;
    END IF;
END;
-- CHECKPOINT C-01d

/*****************************************************************************/

CREATE TABLE priv (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL COMMENT 'Unique identifier',
    name varchar(50) NOT NULL COMMENT 'Human-readable name',
    id_type int unsigned COMMENT 'FK: Entity type',
    operation char(1) COLLATE latin1_general_cs COMMENT 'Operation type (one-letter code)',
    pos smallint unsigned COMMENT 'Position for ordering',
    enable_log boolean NOT NULL default false COMMENT 'If true, activity related to this privilege are logged',
    CONSTRAINT pk_priv PRIMARY KEY (id),
    CONSTRAINT fk_priv_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Privileges';
-- CHECKPOINT C-02a

-- Initializing privileges ... \
INSERT INTO priv (identifier, name, id_type, operation, pos) VALUES
    ('usr-c', 'Users: create', 11, 'c', 1),
    ('usr-s', 'Users: search', 11, 's', 2),
    ('usr-v', 'Users: view', 11, 'v', 3),
    ('usr-m', 'Users: change', 11, 'm', 4),
    ('usr-M', 'Users: manage', 11, 'M', 5),
    ('usr-d', 'Users: delete', 11, 'd', 6),
    ('grp-c', 'Groups: create', 12, 'c', 7),
    ('grp-s', 'Groups: search', 12, 's', 8),
    ('grp-v', 'Groups: view', 12, 'v', 9),
    ('grp-m', 'Groups: change', 12, 'm', 10),
    ('grp-M', 'Groups: manage', 12, 'M', 11),
    ('grp-d', 'Groups: delete', 12, 'd', 12),
    ('cr-c', 'Computing resources: create', 14, 'c', 13),
    ('cr-s', 'Computing resources: search', 14, 's', 14),
    ('cr-v', 'Computing resources: view', 14, 'v', 15),
    ('cr-u', 'Computing resources: use', 14, 'u', 16),
    ('cr-m', 'Computing resources: change', 14, 'm', 17),
    ('cr-M', 'Computing resources: manage', 14, 'M', 18),
    ('cr-d', 'Computing resources: delete', 14, 'd', 19),
    ('catalogue-c', 'Catalogues: create', 15, 'c', 20),
    ('catalogue-s', 'Catalogues: search', 15, 's', 21),
    ('catalogue-v', 'Catalogues: view', 15, 'v', 22),
    ('catalogue-u', 'Catalogues: use', 15, 'u', 23),
    ('catalogue-m', 'Catalogues: change', 15, 'm', 24),
    ('catalogue-M', 'Catalogues: manage', 15, 'M', 25),
    ('catalogue-d', 'Catalogues: delete', 15, 'd', 26),
    ('series-c', 'Series: create', 16, 'c', 27),
    ('series-s', 'Series: search', 16, 's', 28),
    ('series-v', 'Series: view', 16, 'v', 29),
    ('series-u', 'Series: use', 16, 'u', 30),
    ('series-m', 'Series: change', 16, 'm', 31),
    ('series-M', 'Series: manage', 16, 'M', 32),
    ('series-d', 'Series: delete', 16, 'd', 33),
    ('pubserver-c', 'Publish servers: create', 18, 'c', 34),
    ('pubserver-s', 'Publish servers: search', 18, 's', 35),
    ('pubserver-v', 'Publish servers: view', 18, 'v', 36),
    ('pubserver-u', 'Publish servers: use', 18, 'u', 37),
    ('pubserver-m', 'Publish servers: change', 18, 'm', 38),
    ('pubserver-M', 'Publish servers: manage', 18, 'M', 39),
    ('pubserver-d', 'Publish servers: delete', 18, 'd', 40),
    ('service-c', 'Processing services: create', 19, 'c', 41),
    ('service-s', 'Processing services: search', 19, 's', 42),
    ('service-v', 'Processing services: view', 19, 'v', 43),
    ('service-u', 'Processing services: use', 19, 'u', 44),
    ('service-m', 'Processing services: change', 19, 'm', 45),
    ('service-M', 'Processing services: manage', 19, 'M', 46),
    ('service-d', 'Processing services: delete', 19, 'd', 47),
    ('scheduler-M', 'Schedulers: control', 20, 'M', 48),
    ('task-M', 'Tasks: control', 22, 'M', 49),
    ('news-c', 'News items: create', NULL, 'c', 50),
    ('news-s', 'News items: search', NULL, 's', 51),
    ('news-v', 'News items: view', NULL, 'v', 52),
    ('news-m', 'News items: change', NULL, 'm', 53),
    ('news-d', 'News items: delete', NULL, 'd', 54)
;
-- RESULT
-- CHECKPOINT C-02b

/*****************************************************************************/

CREATE TABLE configsection (
    id smallint unsigned NOT NULL auto_increment,
    name varchar(50) NOT NULL COMMENT 'Unique name',
    pos smallint unsigned COMMENT 'Position for ordering',
    CONSTRAINT pk_configsection PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Sections of global configuration settings';
-- CHECKPOINT C-03a

-- Initializing sections of global configuration settings ... \
/*!40000 ALTER TABLE configsection DISABLE KEYS */;
INSERT INTO configsection (id, name, pos) VALUES
    (1, 'Site', 1),
    (2, 'Server', 2),
    (3, 'Users', 3),
    (4, 'Tasks', 4),
    (5, 'Processing', 5),
    (6, 'Background Agent', 6)
;
/*!40000 ALTER TABLE configsection DISABLE KEYS */;
-- RESULT
-- CHECKPOINT C-03b

/*****************************************************************************/

CREATE TABLE config (
    name varchar(50) NOT NULL COMMENT 'PK: Name of configuration parameter',
    id_section smallint unsigned COMMENT 'FK: Configuration section',
    pos smallint unsigned COMMENT 'Position for ordering',
    internal boolean NOT NULL default false COMMENT 'If true, variable is hidden',
    type varchar(25) COMMENT 'Type identifier',
    source varchar(25) COMMENT 'Value source identifier',
    caption varchar(100) COMMENT 'Parameter (default) caption',
    hint varchar(500) COMMENT 'Parameter (default) tooltip hint',
    value text COMMENT 'Value of configuration parameter',
    optional boolean NOT NULL DEFAULT false COMMENT 'If true, no value is required',
    CONSTRAINT pk_config PRIMARY KEY (name),
    CONSTRAINT fk_config_section FOREIGN KEY (id_section) REFERENCES configsection(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Global configuration settings';
-- CHECKPOINT C-03c

-- Initializing global configuration settings ... \
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (1, 0, 'ForceReload', 'bool', NULL, 'Force Configuration Reload by Agent', 'If checked, the configuration is reloaded by the agent on its next run', 'true', true),
    (1, 1, 'Available', 'bool', NULL, 'Web Site Available', 'If checked, this web site is available to all users, otherwise only to administrators', 'true', true),
    (1, 2, 'UnavailabilityMessage', 'text', NULL, 'Message for Site Unavailability', 'Enter the message that is displayed in case of site unavailability', NULL, true),
    (1, 3, 'SiteName', 'string', NULL, 'Site Name', 'Enter the main title for this web portal displayed on the home page', NULL, false),
    (1, 4, 'CompanyName', 'string', NULL, 'Company Name', 'Enter the name of the company/organization of the web portal', NULL, false),
    (1, 5, 'CompanyShortName', 'string', NULL, 'Company Short Name', 'Enter a short or abbreviated name of the company/organization', NULL, false),
    (1, 6, 'CompanyUrl', 'url', NULL, 'Company URL', 'Enter the URL of your company''s/organization''s web site', NULL, false),
    (1, 7, 'CompanyEmail', 'email', NULL, 'Company E-mail Contact', 'Enter a contact e-mail address of your company/organization', NULL, false),
    (1, 8, 'CopyrightText', 'string', NULL, 'Copyright Text', 'Enter the text of the copyright', NULL, false),
    (1, 9, 'BaseUrl', 'url', NULL, 'Site Base URL', 'Enter the absolute URL of the folder containing the site''s homepage (or simply the hostname if the site''s main access point is in the web server root folder)', NULL, false),
    (1, 10, 'SignInRedirectUrl', 'url', NULL, 'Sign In Redirection URL', 'Enter the URL of the page to which users are redirected after successful login', '/services', false),
    (1, 11, 'AdminRootUrl', 'string', NULL, 'Control Panel Root URL', 'Enter the relative URL of the control panel main page', '/admin', true),
    (1, 12, 'AccountRootUrl', 'string', NULL, 'Account Functionality Root URL', 'Enter the relative URL of the account main page', '/account', true),
    (1, 13, 'ServiceRootUrl', 'string', NULL, 'Service Root URL', 'Enter the root URL containing the service access points (in the definition field of a service the "$(SERVICEROOT)" placeholder represents this value)', '/services', false),
    (1, 14, 'TaskWorkspaceRootUrl', 'string', NULL, 'Workspace URL', 'Enter the relative URL of the task workspace main page', '/tasks', true),
    (1, 15, 'TaskWorkspaceJobDir', 'string', NULL, 'Workspace Job Directory Name', 'Enter the name of the directory containing job information for a workspace task', 'jobs', true),
    (1, 16, 'SchedulerWorkspaceRootUrl', 'string', NULL, 'Scheduler Workspace URL', 'Enter the relative URL of the scheduler workspace main page', '/schedulers', true),
    (1, 17, 'DefaultXslFile', 'string', NULL, 'Default XSLT file', 'Enter the full path of the XSLT file for the default transformation of portal''s XML output to HTML', NULL, true),
    (2, 1, 'TrustedHosts', 'string', NULL, 'Trusted hosts', 'Enter the IP addresses of the hosts trusted by this site separated by comma', '127.0.0.1', true),
    (2, 2, 'HostCertFile', 'string', NULL, 'Host Certificate File (PKCS#12)', 'Enter the full path of the host certificate file (PKCS#12 format)', NULL, true),
    (2, 3, 'HostCertPassword', 'password', NULL, 'Host Certificate Password [currently ignored]', '[Do not use] Enter the password for the host certificate file', NULL, true),
    (2, 4, 'ServiceFileRoot', 'string', NULL, 'Service Root Folder', 'Enter the root folder containing the service folders on the web portal machine''s file system (in the definition field of a service the "$(SERVICEROOT)" placeholder represents this value)', '$SERVICEROOT$', false),
    (2, 5, 'ServiceParamFileRoot', 'string', NULL, 'Service Parameter Root Folder', 'Enter the root folder containing the service configurable parameter values in files on the web portal machine''s file system (in the pattern attribute of a service''s <param> element, the "$(PARAMROOT)" placeholder represents this value)', '$SERVICEROOT$', true),
    (2, 6, 'FtpRoot', 'string', NULL, 'FTP Root Folder', 'Enter the root folder for the task result files on the web portal machine''s file system', NULL, false),
    (2, 7, 'MailSenderAddress', 'email', NULL, 'Mail Sender Address', 'Enter the default sender address for e-mails (e.g. alerts for administrators or account registration mails for users)', NULL, false),
    (2, 8, 'MailSender', 'string', NULL, 'Mail Sender Display Name', 'Enter the default sender display name for e-mails (e.g. alerts for administrators or account registration mails for users)', NULL, true),
    (2, 9, 'SmtpHostname', 'string', NULL, 'SMTP Server Hostname', 'Enter the hostname that is used for sending e-mails', NULL, true),
    (2, 10, 'SmtpUsername', 'string', NULL, 'SMTP Username', 'Enter the username on the SMTP server account that is used for sending e-mails', NULL, true),
    (2, 11, 'SmtpPassword', 'password', NULL, 'SMTP Password', 'Enter the password for the SMTP server account that is used for sending e-mails', NULL, true),
    (2, 12, 'ChangeLogStart', 'string', NULL, 'Change Log Start Time', 'Enter the start date/time of the period for which the changelog must be conserved, relative to the current date(e.g. -3M, -2M D=1)', '-3M', true),
    (3, 1, 'ExternalAuthentication', 'bool', NULL, 'Allow external authentication', 'If checked, users authenticated by a trustable external authentication mechanism (defined in a configuration file) can use the web portal without using the inbuilt user/password authentication', 'false', true),
    (3, 2, 'AllowSelfRegistration', 'bool', NULL, 'Allow Self-registration for Everybody', 'If checked, users may register on the portal without being trusted by a central identity provider', 'false', true),
    (3, 3, 'AccountActivation', 'int', 'userActRule', 'User Account Activation', 'Select the rule for the activation of user accounts', '0', false),
    (3, 4, 'DisabledProfileFields', 'string', NULL, 'Disabled User Profile Fields', 'Enter the names of user profile fields that users cannot change (separated by comma)', NULL, true),
    (3, 5, 'AllowPassword', 'int', 'rule', 'Allow Password Authentication for Normal Accounts', 'Select when to allow password authentication for normal accounts', '0', false),
    (3, 6, 'AllowOpenId', 'int', 'openIdRule', 'Allow OpenID Authentication for Normal Accounts', 'Select when to allow OpenID authentication for normal accounts', '0', true),
    (3, 7, 'AllowSessionless', 'int', 'rule', 'Allow Trusted Sessionless Authentication for Normal Accounts', 'Select when to allow sessionless requests from trusted hosts for normal accounts (needed for task scheduling etc.)', '0', false),
    (3, 8, 'ForceTrusted', 'int', 'rule', 'Require Requests from Trusted Hosts for Normal Accounts', 'Select when to require requests from trusted hosts for normal accounts', '0', false),
    (3, 9, 'ForceSsl', 'int', 'rule', 'Require Client Certificate for Normal Accounts', 'Select when to require a client certificate for normal accounts', '0', false),
    (3, 10, 'ForceStrongPasswords', 'bool', NULL, 'Force Users to Use Strong Passwords', 'If checked, the user accounts must have a password containing at least 8 characters and at least one upper-case character, one lower-case character, one digit and one special character', 'false', true),
    (3, 11, 'PasswordExpireTime', 'timespan', NULL, 'Password Expiration Time', 'Enter the period after which a password expires and has to be changed; use quantifiers D (days), W (weeks), M (months), e.g. 3M; leave empty if password does not expire', '3M', true),
    (3, 12, 'MaxFailedLogins', 'int', NULL, 'Maximum Failed Login Attempts', 'Enter the number of unsuccessful authentication attempts for a user account after which the account is blocked; leave empty if account should never be blocked', '5', true),
    (3, 13, 'OpenIdNonceValidity', 'timespan', NULL, 'Validity of OpenID Response Nonces', 'Select the maximum validity of a response nonce in a positive authentication assertion, use quantifiers h (hours), m (minutes), s (seconds), e.g. 10m', '10m', true),
    (3, 14, 'DefaultResultFolderSize', 'int', NULL, 'Default Result Folder Size Per User (MB)', 'Enter the default size of the task result folder per user', NULL, true),
    (3, 15, 'DefaultTaskLifeTime', 'int', NULL, 'Default Lifetime of Tasks (days)', 'Enter the number of days that a task and its results are kept by default after conclusion', '10', true),
    (3, 16, 'RegistrationMailSubject', 'string', NULL, 'Mail Subject for User Registration', 'Enter the subject for the e-mail to be sent to new users', 'Registration', true),
    (3, 17, 'RegistrationMailHtml', 'bool', NULL, 'Format User Registration Mail as HTML', 'If checked, the e-mail to be sent to new users is formatted as HTML mail, use appropriate markup if formatting is desired', 'false', true),
    (3, 18, 'RegistrationMailBody', 'text', NULL, 'Mail Body for User Registration', 'Enter the body for the e-mail to be sent to new users, use the $(USERNAME), $(PASSWORD), $(SERVICES), $(SERIES) placeholders in the appropriate places', 'Username: $(USERNAME)\nPassword: $(PASSWORD)\nAvailable services: $(SERVICES)\nAvailable series: $(SERIES)', true),
    (3, 19, 'PasswordResetMailSubject', 'string', NULL, 'Mail Subject for Password Reset', 'Enter the subject for the e-mail to be sent to sent to users that have requested a new password', 'New password', true),
    (3, 20, 'PasswordResetMailHtml', 'bool', NULL, 'Format Password Reset Mail as HTML', 'If checked, the e-mail to be sent to users that have requested a new password is formatted as HTML mail, use appropriate markup if formatting is desired', 'false', true),
    (3, 21, 'PasswordResetMailBody', 'text', NULL, 'Mail Body for Password Reset', 'Enter the body for the e-mail to be sent to users that have requested a new password, use the $(USERNAME), $(PASSWORD), $(SERVICES), $(SERIES) placeholders in the appropriate places', 'Username: $(USERNAME)\nNew password: $(PASSWORD)\nAvailable services: $(SERVICES)\nAvailable series: $(SERIES)', true),
    (3, 22, 'EmailConfirmationUrl', 'text', NULL, 'URL for Account Activation and E-mail Confirmation', 'Enter the URL that is used as access point for URLs sent as actication or e-mail confirmations, use the $(BASEURL) and $(TOKEN) placeholders in the appropriate places', '$(BASEURL)/confirm/$(TOKEN)', true),
    (4, 1, 'SyncTaskOperations', 'bool', NULL, 'Perform Task Operations Synchronously', 'If checked, tasks are submitted, aborted or deleted directly on the processing environment upon user request, otherwise the background agent performs the requested operation later', 'true', false),
    (4, 2, 'TaskRetry', 'int', 'taskSubmitRetry', 'Submission Retrying', 'Select the policy for interactively created tasks that cannot be submitted immediately (e.g. for capacity reasons)', '1', true),
    (4, 3, 'TaskRetryPeriod', 'timespan', NULL, 'Default Submission Retrying Period', 'Enter the default length of the time period after the submission of a task during which the background agent tries to submit the task again', '1h', true),
    (4, 4, 'PriorityValues', 'string', NULL, 'Task Priority Multipliers', 'Enter a list of supported task priorities, each consisting of a multiplier value (unsigned real number) for the cost calculation and a caption (syntax: weight1:caption1;weight2:caption2;...)', '0.25:Very low;0.5:Low;1:Normal;2:High;4:Very high', false),
    (4, 5, 'CompressionValues', 'string', NULL, 'Task Result Compression Types', 'Enter a list of supported compression types for task results, each consisting of a name and a caption (syntax: name1:caption1;name2:caption2;...)', 'NOCMP:None;COMPRESS:Single File;TGZ:Unique Package', false),
    (4, 6, 'TaskFlowUrl', 'url', NULL, 'Task Flow URL', 'Enter the URL of the script creating a task''s flow graphics', '/core/task.flow.aspx', false),
    (4, 7, 'ComputingResourceStatusValidity', 'timespan', NULL, 'Computing Resource Status Validity', 'Enter the time period after which the capacity information of a computing resource expires', '5m', false),
    (5, 1, 'DefaultMaxNodesPerJob', 'int', NULL, 'Default Maximum Number of Nodes per Job', 'Enter the default maximum number of single nodes onto which a job can split', '4', false),
    (5, 2, 'DefaultMinArgumentsPerNode', 'int', NULL, 'Default Minimum Number of Arguments per Node', 'Enter the default minimum number of arguments (input files) to be processed on a single node', '8', false),
    (5, 3, 'PublishRetryWaitTime', 'int', NULL, 'Result Publishing Waiting Time Before Retry (sec)', 'Enter the interval before a retry if the publishing of the task results failed', NULL, true),
    (5, 4, 'PublishRetryTimes', 'int', NULL, 'Result Publishing Retries', 'Enter the number of retries if the publishing of the task results failed', NULL, true),
    (5, 5, 'SeriesInfoValidityTime', 'timespan', NULL, 'Series Info Cache Validity Time', 'Enter the time the series or data set information (total product count) is kept in the cache before it is requeried', '6h', false),
    (5, 6, 'SiteConfigFile', 'string', NULL, 'Site Config File', 'Enter the name of the site configuration file', NULL, false),
    (5, 7, 'VirtualOrganization', 'string', NULL, 'Virtual Organization', 'Enter the name of the virtual organization', NULL, false),
    (5, 8, 'wpsrequest-timeout', 'int', NULL, 'WPS request timeout', 'Enter the WPS request timeout', '10000', false),
    (6, 1, 'AgentInterval', 'int', NULL, 'Agent Execution Interval (sec)', 'Enter the base interval in seconds between two executions of the background agent', '30', false),
    (6, 2, 'AgentLogFile', 'string', NULL, 'Agent Log File', 'Enter the name for the log output of the background agent (use "$DATE" as placeholder for the current date)', NULL, false),
    (6, 3, 'AgentLogLevel', 'int', 'logLevel', 'Agent Log Level', 'Select the desired degree of detail for the background agent log file', '1', false),
    (6, 4, 'AgentUser', 'string', NULL, 'Agent Username', 'Enter the username of the user on whose behalf the agent is running', 'admin', true)
;
-- RESULT
-- CHECKPOINT C-03d

/*****************************************************************************/

CREATE TABLE auth (
    id int unsigned NOT NULL auto_increment,
    pos smallint unsigned COMMENT 'Position for ordering',
    identifier varchar(25) NOT NULL COMMENT 'Unique identifier',
    name varchar(50) NOT NULL COMMENT 'Name',
    description text COMMENT 'Description of authentication type',
    type varchar(100) NOT NULL COMMENT 'Fully qualified name of class implementing authentication type',
    enabled boolean NOT NULL DEFAULT true COMMENT 'If true, authentication type is enabled',
    activation_rule int NOT NULL DEFAULT 0 COMMENT 'Rule for account activation',
    normal_rule int NOT NULL DEFAULT 2 COMMENT 'Rule for normal accounts',
    refresh_period int NOT NULL DEFAULT 0 COMMENT 'Refresh period for external authentication',
    config varchar(200) COMMENT 'Path to configuration file',
    timeout int COMMENT 'HTTP session timeout',
    CONSTRAINT pk_auth PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'User authentication types';
-- CHECKPOINT C-04a

-- Initializing basic user authentication types ... \
INSERT INTO auth (pos, identifier, name, description, type, enabled) VALUES
    (1, 'password', 'Password authentication', 'Password authentication allows users to identify themselves by username and password.', 'Terradue.Portal.PasswordAuthenticationType, Terradue.Portal', true),
    (2, 'token', 'Token authentication', 'With token authentication a user presents an automatically generated random token that is only known to him and can only be used once. This authentication type is used in very specific situations, e.g. for resetting passwords.', 'Terradue.Portal.TokenAuthenticationType, Terradue.Portal', true),
    (3, 'certificate', 'Certificate authentication', 'Certificate authentication allows users to identify themselves by presenting a client certificate. The client certificate subject must match the subject configured for the user account. The certificate authenticity must be guaranteed by the web server configuration.', 'Terradue.Portal.CertificateAuthenticationType, Terradue.Portal', true)
;
-- RESULT
-- CHECKPOINT C-04b

/*****************************************************************************/

CREATE TABLE action (
    id int unsigned NOT NULL auto_increment,
    pos smallint unsigned COMMENT 'Position for ordering',
    identifier varchar(25) NOT NULL COMMENT 'Unique identifier',
    name varchar(50) NOT NULL COMMENT 'Name',
    description text COMMENT 'Description of action',
    class varchar(100) COMMENT 'Fully qualified name of class implementing action method',
    method varchar(50) COMMENT 'Name of action method',
    enabled boolean COMMENT 'If true, action is executed automatically',
    time_interval varchar(10) COMMENT 'Execution interval: e.g.: 1s, 5m, 2h',
    next_execution_time datetime COMMENT 'Next execution time',
    immediate boolean COMMENT 'Execute immediately in next agent cycle',
    CONSTRAINT pk_action PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Actions for automatic periodical execution';
-- CHECKPOINT C-05a

-- Initializing actions for automatic periodical execution ... \
/*!40000 ALTER TABLE action DISABLE KEYS */;
INSERT INTO action (pos, identifier, name, description, class, method) VALUES
    (1, 'taskstatus', 'Task status refresh', 'This action refreshes task, job and node status information for active tasks.', 'Terradue.Portal.Task, Terradue.Portal', 'ExecuteTaskStatusRefresh'),
    (2, 'task', 'Pending operations', 'This action performs operations involving the computing resource (submission and abortion of tasks and jobs) that have been delayed for faster web portal response times. Task and job operations are delayed when at the moment of the request the Control Panel setting "Synchronous Task Operations" is not active.', 'Terradue.Portal.Task, Terradue.Portal', 'ExecuteTaskPendingOperations'),
    (3, 'cleanup', 'Task and scheduler cleanup', 'This action removes the tasks and schedulers that have been marked for deletion from the database by a deletion. Tasks and schedulers are marked for deletion when at the moment of their deletion request from the web portal the Control Panel setting "Synchronous Task Operations" is not active.', 'Terradue.Portal.Task, Terradue.Portal', 'ExecuteCleanup'),
    (4, 'scheduler', 'Task scheduler', 'This action manages the creation and submission of tasks for the active task schedulers according to the resource limitations.', 'Terradue.Portal.Scheduler, Terradue.Portal', 'ExecuteTaskScheduler'),
    (5, 'series', 'Catalogue series refresh', 'This action sends requests to all series catalogue description URLs defined in the Control Panel and refreshes the corresponding catalogue URL templates.', 'Terradue.Portal.Series, Terradue.Portal', 'ExecuteCatalogueSeriesRefresh'),
    (6, 'cr', 'Computing resource status refresh', 'This action refreshes the information on the capacity and load of computing resources.', 'Terradue.Portal.ComputingResource, Terradue.Portal', 'ExecuteComputingResourceStatusRefresh'),
    (7, 'password', 'Password expiration check', 'This action temporarily deactivates user accounts with expired passwords. Users can reactivete their accounts through the recovery function.', 'Terradue.Portal.PasswordAuthenticationType, Terradue.Portal', 'ExecutePasswordExpirationCheck')
;
/*!40000 ALTER TABLE action ENABLE KEYS */;
-- RESULT
-- CHECKPOINT C-05b

/*****************************************************************************/

CREATE TABLE application (
    id int unsigned NOT NULL auto_increment,
    available boolean COMMENT 'If true, application is available to users',
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Name',
    config_file varchar(100) COMMENT 'Location of application configuration file',
    CONSTRAINT pk_application PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'External client applications, such as web services';
-- CHECKPOINT C-06

/*****************************************************************************/

CREATE TABLE domain (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(100) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) COMMENT 'name',
    description text COMMENT 'Description',
    kind tinyint unsigned COMMENT 'Kind of domain',
    icon_url varchar(200) COMMENT 'Icon URL',
    CONSTRAINT pk_domain PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Domains';
-- CHECKPOINT C-07

/*****************************************************************************/

CREATE TABLE role (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Human-readable name',
    description text COMMENT 'Description',
    count INT NULL COMMENT 'number of products',
    CONSTRAINT pk_role PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Roles for users or groups';
-- CHECKPOINT C-08

/*****************************************************************************/

CREATE TABLE role_priv (
    id_role int unsigned NOT NULL COMMENT 'FK: Role',
    id_priv int unsigned NOT NULL COMMENT 'FK: Privilege',
    int_value int COMMENT 'Value (optional)',
    CONSTRAINT pk_role_priv PRIMARY KEY (id_role, id_priv),
    CONSTRAINT fk_role_priv_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_priv_priv FOREIGN KEY (id_priv) REFERENCES priv(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Associations of privileges to roles';
-- CHECKPOINT C-09

/*****************************************************************************/

CREATE TABLE usr (
    id int unsigned NOT NULL auto_increment,
    username varchar(50) NOT NULL COMMENT 'Username',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    status tinyint NOT NULL DEFAULT 4 COMMENT 'Account status, see lookup list "accountStatus"',
    level tinyint unsigned NOT NULL DEFAULT 1 COMMENT '1: User, 2: Developer, 3: Admin',
    email varchar(100) COMMENT 'Email address',
    password varchar(50) COMMENT 'Password',
    firstname varchar(50) COMMENT 'First name',
    lastname varchar(50) COMMENT 'Last name',
    affiliation varchar(100) COMMENT 'Affiliation, organization etc.',
    country varchar(100) COMMENT 'Country',
    language char(2) COMMENT 'Preferred language',
    time_zone char(25) NOT NULL DEFAULT 'UTC' COMMENT 'Time zone',
    normal_account boolean NOT NULL DEFAULT false COMMENT 'If true, auth/n settings are made in general configuration',
    allow_password boolean NOT NULL DEFAULT true COMMENT 'If true, password authentication is allowed',
    allow_sessionless boolean COMMENT 'If true, sessionless requests from trusted hosts are allowed',
    force_trusted boolean COMMENT 'If true, only connections from trusted hosts are allowed',
    force_ssl boolean NOT NULL DEFAULT false COMMENT 'If true, accept only SSL authentication',
    debug_level tinyint unsigned NOT NULL DEFAULT 0 COMMENT 'Debug level (admins only), 3..6',
    simple_gui boolean NOT NULL DEFAULT false COMMENT 'If true, simplified GUI is selected',
    credits int unsigned NOT NULL DEFAULT 0 COMMENT 'Maximum resource credits for the user',
    task_storage_period smallint unsigned COMMENT 'Maximum lifetime of concluded tasks, 0 if endless',
    publish_folder_size int unsigned COMMENT 'Maximum size of user''s publish folder',
    proxy_username varchar(50) COMMENT 'Proxy username',
    proxy_password varchar(50) COMMENT 'Proxy password',
    cert_subject varchar(200) COMMENT 'Certificate subject',
    last_password_change_time datetime COMMENT 'Date/time of last password change',
    failed_logins int NOT NULL DEFAULT 0 COMMENT 'Number of failed login attempts after last successful login',
    CONSTRAINT pk_usr PRIMARY KEY (id),
    UNIQUE INDEX (username)
) Engine=InnoDB COMMENT 'User accounts';
-- CHECKPOINT C-16a

-- Adding initial administrator user (username admin, password changeme) ... \
INSERT INTO usr (allow_password, allow_sessionless, username, password, firstname, lastname, level, credits, task_storage_period, publish_folder_size) VALUES
    (true, true, 'admin', PASSWORD('changeme'), 'Admin', 'Admin', 4, 100, 0, 1000)
;
-- RESULT
-- CHECKPOINT C-16b

/*****************************************************************************/

CREATE TABLE grp (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, group can be configured by other domains',
    name varchar(50) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    priority smallint COMMENT 'Priority (optional)',
    is_default boolean NOT NULL DEFAULT false COMMENT 'If true, group is automatically selected for new users',
    all_resources boolean NOT NULL DEFAULT false COMMENT 'If true, new resources are automatically added to group',
    CONSTRAINT pk_grp PRIMARY KEY (id),
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'User groups';
-- CHECKPOINT C-25a

-- Adding initial administrator group ... \
INSERT INTO grp (name, description, all_resources) VALUES ('Administrators', 'Portal administrators', true);
-- RESULT
-- CHECKPOINT C-25b

/*****************************************************************************/

CREATE TABLE usr_grp (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    id_grp int unsigned NOT NULL COMMENT 'FK: Group to which the user is assigned',
    temp boolean NOT NULL DEFAULT false COMMENT 'True if record is temporary (for current session)',
    CONSTRAINT pk_usr_grp PRIMARY KEY (id_usr, id_grp),
    CONSTRAINT fk_usr_grp_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_usr_grp_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of users to groups';
-- CHECKPOINT C-26a

-- Assigning administror user the administrator group ... \
INSERT INTO usr_grp (id_usr, id_grp) SELECT t.id, t1.id FROM usr AS t INNER JOIN grp AS t1 WHERE t.username='admin' AND t1.name='Administrators';
-- RESULT
-- CHECKPOINT C-26b

/*****************************************************************************/

CREATE TABLE rolegrant (
    id_usr int unsigned COMMENT 'FK: User (id_usr or id_grp must be set)',
    id_grp int unsigned COMMENT 'FK: Group (id_usr or id_grp must be set)',
    id_role int unsigned NOT NULL COMMENT 'FK: Role to which the user/group is assigned',
    id_domain int unsigned COMMENT 'FK: Domain for which the user/group has the role',
    CONSTRAINT fk_rolegrant_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of users/groups to roles for domains';
-- CHECKPOINT C-24

/*****************************************************************************/

CREATE TABLE openidprovider (
    id int unsigned NOT NULL auto_increment,
    name varchar(50) NOT NULL COMMENT 'Unique name',
    op_identifier varchar(100) COMMENT 'OpenID provider identifier',
    endpoint_url varchar(100) COMMENT 'Endpoint URL',
    pattern varchar(100) COMMENT 'Pattern for converting user input to identifier',
    input_caption varchar(100) COMMENT 'Caption for user input or user-specified identifier',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    CONSTRAINT pk_openidprovider PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'OpenID providers';
-- CHECKPOINT C-10

/*****************************************************************************/

CREATE TABLE lookuplist (
    id smallint unsigned NOT NULL auto_increment,
    system boolean NOT NULL DEFAULT false COMMENT 'If true, list is predefined and locked',
    name varchar(25) NOT NULL COMMENT 'Name of lookup list',
    max_length smallint COMMENT 'Maximum string length of contained values',
    CONSTRAINT pk_lookuplist PRIMARY KEY (id, system)
) Engine=InnoDB COMMENT 'Configurable lookup lists';
-- CHECKPOINT C-11a

-- Initializing lookup lists ... \
INSERT INTO lookuplist (id, system, name) VALUES
    (1, true, 'userLevel'),
    (2, true, 'accountStatus'),
    (3, true, 'resourceAvailability'),
    (4, true, 'isoCountries'),
    (5, true, 'language'),
    (6, true, 'timeZone'),
    (7, true, 'logLevel'),
    (8, true, 'debugLevel'),
    (9, true, 'taskSubmitRetry'),
    (10, true, 'taskStatusRequest'),
    (11, true, 'ceStatusRequest'),
    (12, true, 'protocol'),
    (13, true, 'rating'),
    (14, true, 'rule'),
    (15, true, 'userActRule'),
    (16, true, 'openIdRule'),
    (17, true, 'clientCertVerifyRule')
;
-- RESULT
-- CHECKPOINT C-11b

/*****************************************************************************/

CREATE TABLE lookup (
    id_list smallint unsigned NOT NULL COMMENT 'FK: Lookup list',
    pos smallint unsigned COMMENT 'Position for ordering',
    caption varchar(70) NOT NULL COMMENT 'Caption for value',
    value text NOT NULL COMMENT 'Value',
    CONSTRAINT fk_lookup_list FOREIGN KEY (id_list) REFERENCES lookuplist(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Values in lookup lists';
-- CHECKPOINT C-12a

-- Initializing lookup values ... \
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (1, 1, 'User', '1'),
    (1, 2, 'Developer', '2'),
    (1, 3, 'Manager', '3'),
    (1, 4, 'Administrator', '4'),
    (2, 1, 'Disabled (not recoverable by user)', '0'),
    (2, 2, 'Deactivated (recoverable by user)', '1'),
    (2, 3, 'Waiting for activation', '2'),
    (2, 4, 'Password reset requested', '3'),
    (2, 5, 'Enabled', '4'),
    (3, 1, 'Disabled', '0'),
    (3, 2, 'Only administrators', '1'),
    (3, 3, 'Administrators and managers', '2'),
    (3, 4, 'Administrators, managers and developers', '3'),
    (3, 5, 'All authorized users', '4')
;
-- CHECKPOINT C-12b

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (4, 1, 'Afghanistan', 'AF'),
    (4, 2, 'Åland Islands', 'AX'),
    (4, 3, 'Albania', 'AL'),
    (4, 4, 'Algeria', 'DZ'),
    (4, 5, 'American Samoa', 'AS'),
    (4, 6, 'Andorra', 'AD'),
    (4, 7, 'Angola', 'AO'),
    (4, 8, 'Anguilla', 'AI'),
    (4, 9, 'Antarctica', 'AQ'),
    (4, 10, 'Antigua and Barbuda', 'AG'),
    (4, 11, 'Argentina', 'AR'),
    (4, 12, 'Armenia', 'AM'),
    (4, 13, 'Aruba', 'AW'),
    (4, 14, 'Australia', 'AU'),
    (4, 15, 'Austria', 'AT'),
    (4, 16, 'Azerbaijan', 'AZ'),
    (4, 17, 'Bahamas', 'BS'),
    (4, 18, 'Bahrain', 'BH'),
    (4, 19, 'Bangladesh', 'BD'),
    (4, 20, 'Barbados', 'BB'),
    (4, 21, 'Belarus', 'BY'),
    (4, 22, 'Belgium', 'BE'),
    (4, 23, 'Belize', 'BZ'),
    (4, 24, 'Benin', 'BJ'),
    (4, 25, 'Bermuda', 'BM'),
    (4, 26, 'Bhutan', 'BT'),
    (4, 27, 'Bolivia (Plurinational State of)', 'BO'),
    (4, 28, 'Bonaire, Sint Eustatius and Saba', 'BQ'),
    (4, 29, 'Bosnia and Herzegovina', 'BA'),
    (4, 30, 'Botswana', 'BW'),
    (4, 31, 'Bouvet Island', 'BV'),
    (4, 32, 'Brazil', 'BR'),
    (4, 33, 'British Indian Ocean Territory', 'IO'),
    (4, 34, 'Brunei Darussalam', 'BN'),
    (4, 35, 'Bulgaria', 'BG'),
    (4, 36, 'Burkina Faso', 'BF'),
    (4, 37, 'Burundi', 'BI'),
    (4, 38, 'Cambodia', 'KH'),
    (4, 39, 'Cameroon', 'CM'),
    (4, 40, 'Canada', 'CA'),
    (4, 41, 'Cabo Verde', 'CV'),
    (4, 42, 'Cayman Islands', 'KY'),
    (4, 43, 'Central African Republic', 'CF'),
    (4, 44, 'Chad', 'TD'),
    (4, 45, 'Chile', 'CL'),
    (4, 46, 'China', 'CN'),
    (4, 47, 'Christmas Island', 'CX'),
    (4, 48, 'Cocos (Keeling) Islands', 'CC'),
    (4, 49, 'Colombia', 'CO'),
    (4, 50, 'Comoros', 'KM')
;
-- CHECKPOINT C-12c

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (4, 51, 'Congo', 'CG'),
    (4, 52, 'Congo (Democratic Republic of the)', 'CD'),
    (4, 53, 'Cook Islands', 'CK'),
    (4, 54, 'Costa Rica', 'CR'),
    (4, 55, 'Côte d''Ivoire', 'CI'),
    (4, 56, 'Croatia', 'HR'),
    (4, 57, 'Cuba', 'CU'),
    (4, 58, 'Curaçao', 'CW'),
    (4, 59, 'Cyprus', 'CY'),
    (4, 60, 'Czech Republic', 'CZ'),
    (4, 61, 'Denmark', 'DK'),
    (4, 62, 'Djibouti', 'DJ'),
    (4, 63, 'Dominica', 'DM'),
    (4, 64, 'Dominican Republic', 'DO'),
    (4, 65, 'Ecuador', 'EC'),
    (4, 66, 'Egypt', 'EG'),
    (4, 67, 'El Salvador', 'SV'),
    (4, 68, 'Equatorial Guinea', 'GQ'),
    (4, 69, 'Eritrea', 'ER'),
    (4, 70, 'Estonia', 'EE'),
    (4, 71, 'Ethiopia', 'ET'),
    (4, 72, 'Falkland Islands (Malvinas)', 'FK'),
    (4, 73, 'Faroe Islands', 'FO'),
    (4, 74, 'Fiji', 'FJ'),
    (4, 75, 'Finland', 'FI'),
    (4, 76, 'France', 'FR'),
    (4, 77, 'French Guiana', 'GF'),
    (4, 78, 'French Polynesia', 'PF'),
    (4, 79, 'French Southern Territories', 'TF'),
    (4, 80, 'Gabon', 'GA'),
    (4, 81, 'Gambia', 'GM'),
    (4, 82, 'Georgia', 'GE'),
    (4, 83, 'Germany', 'DE'),
    (4, 84, 'Ghana', 'GH'),
    (4, 85, 'Gibraltar', 'GI'),
    (4, 86, 'Greece', 'GR'),
    (4, 87, 'Greenland', 'GL'),
    (4, 88, 'Grenada', 'GD'),
    (4, 89, 'Guadeloupe', 'GP'),
    (4, 90, 'Guam', 'GU'),
    (4, 91, 'Guatemala', 'GT'),
    (4, 92, 'Guernsey', 'GG'),
    (4, 93, 'Guinea', 'GN'),
    (4, 94, 'Guinea-Bissau', 'GW'),
    (4, 95, 'Guyana', 'GY'),
    (4, 96, 'Haiti', 'HT'),
    (4, 97, 'Heard Island and McDonald Islands', 'HM'),
    (4, 98, 'Holy See', 'VA'),
    (4, 99, 'Honduras', 'HN'),
    (4, 100, 'Hong Kong', 'HK')
;
-- CHECKPOINT C-12d

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (4, 101, 'Hungary', 'HU'),
    (4, 102, 'Iceland', 'IS'),
    (4, 103, 'India', 'IN'),
    (4, 104, 'Indonesia', 'ID'),
    (4, 105, 'Iran (Islamic Republic of)', 'IR'),
    (4, 106, 'Iraq', 'IQ'),
    (4, 107, 'Ireland', 'IE'),
    (4, 108, 'Isle of Man', 'IM'),
    (4, 109, 'Israel', 'IL'),
    (4, 110, 'Italy', 'IT'),
    (4, 111, 'Jamaica', 'JM'),
    (4, 112, 'Japan', 'JP'),
    (4, 113, 'Jersey', 'JE'),
    (4, 114, 'Jordan', 'JO'),
    (4, 115, 'Kazakhstan', 'KZ'),
    (4, 116, 'Kenya', 'KE'),
    (4, 117, 'Kiribati', 'KI'),
    (4, 118, 'Korea (Democratic People''s Republic of)', 'KP'),
    (4, 119, 'Korea (Republic of)', 'KR'),
    (4, 120, 'Kuwait', 'KW'),
    (4, 121, 'Kyrgyzstan', 'KG'),
    (4, 122, 'Lao People''s Democratic Republic', 'LA'),
    (4, 123, 'Latvia', 'LV'),
    (4, 124, 'Lebanon', 'LB'),
    (4, 125, 'Lesotho', 'LS'),
    (4, 126, 'Liberia', 'LR'),
    (4, 127, 'Libya', 'LY'),
    (4, 128, 'Liechtenstein', 'LI'),
    (4, 129, 'Lithuania', 'LT'),
    (4, 130, 'Luxembourg', 'LU'),
    (4, 131, 'Macao', 'MO'),
    (4, 132, 'Macedonia (the former Yugoslav Republic of)', 'MK'),
    (4, 133, 'Madagascar', 'MG'),
    (4, 134, 'Malawi', 'MW'),
    (4, 135, 'Malaysia', 'MY'),
    (4, 136, 'Maldives', 'MV'),
    (4, 137, 'Mali', 'ML'),
    (4, 138, 'Malta', 'MT'),
    (4, 139, 'Marshall Islands', 'MH'),
    (4, 140, 'Martinique', 'MQ'),
    (4, 141, 'Mauritania', 'MR'),
    (4, 142, 'Mauritius', 'MU'),
    (4, 143, 'Mayotte', 'YT'),
    (4, 144, 'Mexico', 'MX'),
    (4, 145, 'Micronesia (Federated States of)', 'FM'),
    (4, 146, 'Moldova (Republic of)', 'MD'),
    (4, 147, 'Monaco', 'MC'),
    (4, 148, 'Mongolia', 'MN'),
    (4, 149, 'Montenegro', 'ME'),
    (4, 150, 'Montserrat', 'MS')
;
-- CHECKPOINT C-12e

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (4, 151, 'Morocco', 'MA'),
    (4, 152, 'Mozambique', 'MZ'),
    (4, 153, 'Myanmar', 'MM'),
    (4, 154, 'Namibia', 'NA'),
    (4, 155, 'Nauru', 'NR'),
    (4, 156, 'Nepal', 'NP'),
    (4, 157, 'Netherlands', 'NL'),
    (4, 158, 'New Caledonia', 'NC'),
    (4, 159, 'New Zealand', 'NZ'),
    (4, 160, 'Nicaragua', 'NI'),
    (4, 161, 'Niger', 'NE'),
    (4, 162, 'Nigeria', 'NG'),
    (4, 163, 'Niue', 'NU'),
    (4, 164, 'Norfolk Island', 'NF'),
    (4, 165, 'Northern Mariana Islands', 'MP'),
    (4, 166, 'Norway', 'NO'),
    (4, 167, 'Oman', 'OM'),
    (4, 168, 'Pakistan', 'PK'),
    (4, 169, 'Palau', 'PW'),
    (4, 170, 'Palestine, State of', 'PS'),
    (4, 171, 'Panama', 'PA'),
    (4, 172, 'Papua New Guinea', 'PG'),
    (4, 173, 'Paraguay', 'PY'),
    (4, 174, 'Peru', 'PE'),
    (4, 175, 'Philippines', 'PH'),
    (4, 176, 'Pitcairn', 'PN'),
    (4, 177, 'Poland', 'PL'),
    (4, 178, 'Portugal', 'PT'),
    (4, 179, 'Puerto Rico', 'PR'),
    (4, 180, 'Qatar', 'QA'),
    (4, 181, 'Réunion', 'RE'),
    (4, 182, 'Romania', 'RO'),
    (4, 183, 'Russian Federation', 'RU'),
    (4, 184, 'Rwanda', 'RW'),
    (4, 185, 'Saint Barthélemy', 'BL'),
    (4, 186, 'Saint Helena, Ascension and Tristan da Cunha', 'SH'),
    (4, 187, 'Saint Kitts and Nevis', 'KN'),
    (4, 188, 'Saint Lucia', 'LC'),
    (4, 189, 'Saint Martin (French part)', 'MF'),
    (4, 190, 'Saint Pierre and Miquelon', 'PM'),
    (4, 191, 'Saint Vincent and the Grenadines', 'VC'),
    (4, 192, 'Samoa', 'WS'),
    (4, 193, 'San Marino', 'SM'),
    (4, 194, 'Sao Tome and Principe', 'ST'),
    (4, 195, 'Saudi Arabia', 'SA'),
    (4, 196, 'Senegal', 'SN'),
    (4, 197, 'Serbia', 'RS'),
    (4, 198, 'Seychelles', 'SC'),
    (4, 199, 'Sierra Leone', 'SL')
;
-- CHECKPOINT C-12f

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (4, 200, 'Singapore', 'SG'),
    (4, 201, 'Sint Maarten (Dutch part)', 'SX'),
    (4, 202, 'Slovakia', 'SK'),
    (4, 203, 'Slovenia', 'SI'),
    (4, 204, 'Solomon Islands', 'SB'),
    (4, 205, 'Somalia', 'SO'),
    (4, 206, 'South Africa', 'ZA'),
    (4, 207, 'South Georgia and the South Sandwich Islands', 'GS'),
    (4, 208, 'South Sudan', 'SS'),
    (4, 209, 'Spain', 'ES'),
    (4, 210, 'Sri Lanka', 'LK'),
    (4, 211, 'Sudan', 'SD'),
    (4, 212, 'Suriname', 'SR'),
    (4, 213, 'Svalbard and Jan Mayen', 'SJ'),
    (4, 214, 'Swaziland', 'SZ'),
    (4, 215, 'Sweden', 'SE'),
    (4, 216, 'Switzerland', 'CH'),
    (4, 217, 'Syrian Arab Republic', 'SY'),
    (4, 218, 'Taiwan, Province of China', 'TW'),
    (4, 219, 'Tajikistan', 'TJ'),
    (4, 220, 'Tanzania, United Republic of', 'TZ'),
    (4, 221, 'Thailand', 'TH'),
    (4, 222, 'Timor-Leste', 'TL'),
    (4, 223, 'Togo', 'TG'),
    (4, 224, 'Tokelau', 'TK'),
    (4, 225, 'Tonga', 'TO'),
    (4, 226, 'Trinidad and Tobago', 'TT'),
    (4, 227, 'Tunisia', 'TN'),
    (4, 228, 'Turkey', 'TR'),
    (4, 229, 'Turkmenistan', 'TM'),
    (4, 230, 'Turks and Caicos Islands', 'TC'),
    (4, 231, 'Tuvalu', 'TV'),
    (4, 232, 'Uganda', 'UG'),
    (4, 233, 'Ukraine', 'UA'),
    (4, 234, 'United Arab Emirates', 'AE'),
    (4, 235, 'United Kingdom of Great Britain and Northern Ireland', 'GB'),
    (4, 236, 'United States of America', 'US'),
    (4, 237, 'United States Minor Outlying Islands', 'UM'),
    (4, 238, 'Uruguay', 'UY'),
    (4, 239, 'Uzbekistan', 'UZ'),
    (4, 240, 'Vanuatu', 'VU'),
    (4, 241, 'Venezuela (Bolivarian Republic of)', 'VE'),
    (4, 242, 'Viet Nam', 'VN'),
    (4, 243, 'Virgin Islands (British)', 'VG'),
    (4, 244, 'Virgin Islands (U.S.)', 'VI'),
    (4, 245, 'Wallis and Futuna', 'WF'),
    (4, 246, 'Western Sahara', 'EH'),
    (4, 247, 'Yemen', 'YE'),
    (4, 248, 'Zambia', 'ZM'),
    (4, 249, 'Zimbabwe', 'ZW')
;
-- CHECKPOINT C-12g

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 1, 'English', 'en'),
    (5, 2, 'French (not supported yet)', 'fr'),
    (5, 3, 'German (not supported yet)', 'de')
;
-- CHECKPOINT C-12h

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 0, '[+00:00] Coordinated Universal Time (UTC)', 'UTC'),
    (6, 1, '[-11:00] Pacific/Apia', 'Pacific/Apia'),
    (6, 2, '[-11:00] Pacific/Midway', 'Pacific/Midway'),
    (6, 3, '[-11:00] Pacific/Niue', 'Pacific/Niue'),
    (6, 4, '[-11:00] Pacific/Pago_Pago', 'Pacific/Pago_Pago'),
    (6, 5, '[-11:00] Pacific/Samoa', 'Pacific/Samoa'),
    (6, 6, '[-11:00] US/Samoa', 'US/Samoa'),
    (6, 7, '[-10:00] America/Adak', 'America/Adak'),
    (6, 8, '[-10:00] America/Atka', 'America/Atka'),
    (6, 9, '[-10:00] Pacific/Fakaofo', 'Pacific/Fakaofo'),
    (6, 10, '[-10:00] Pacific/Honolulu', 'Pacific/Honolulu'),
    (6, 11, '[-10:00] Pacific/Johnston', 'Pacific/Johnston'),
    (6, 12, '[-10:00] Pacific/Rarotonga', 'Pacific/Rarotonga'),
    (6, 13, '[-10:00] Pacific/Tahiti', 'Pacific/Tahiti'),
    (6, 14, '[-10:00] US/Aleutian', 'US/Aleutian'),
    (6, 15, '[-10:00] US/Hawaii', 'US/Hawaii'),
    (6, 16, '[-09:30] Pacific/Marquesas', 'Pacific/Marquesas'),
    (6, 17, '[-09:00] America/Anchorage', 'America/Anchorage'),
    (6, 18, '[-09:00] America/Juneau', 'America/Juneau'),
    (6, 19, '[-09:00] America/Nome', 'America/Nome'),
    (6, 20, '[-09:00] America/Yakutat', 'America/Yakutat'),
    (6, 21, '[-09:00] Pacific/Gambier', 'Pacific/Gambier'),
    (6, 22, '[-09:00] US/Alaska', 'US/Alaska'),
    (6, 23, '[-08:00] America/Dawson', 'America/Dawson'),
    (6, 24, '[-08:00] America/Ensenada', 'America/Ensenada'),
    (6, 25, '[-08:00] America/Los_Angeles', 'America/Los_Angeles'),
    (6, 26, '[-08:00] America/Tijuana', 'America/Tijuana'),
    (6, 27, '[-08:00] America/Vancouver', 'America/Vancouver'),
    (6, 28, '[-08:00] America/Whitehorse', 'America/Whitehorse'),
    (6, 29, '[-08:00] Canada/Pacific', 'Canada/Pacific'),
    (6, 30, '[-08:00] Canada/Yukon', 'Canada/Yukon'),
    (6, 31, '[-08:00] Mexico/BajaNorte', 'Mexico/BajaNorte'),
    (6, 32, '[-08:00] Pacific/Pitcairn', 'Pacific/Pitcairn'),
    (6, 33, '[-08:00] US/Pacific', 'US/Pacific'),
    (6, 34, '[-07:00] America/Boise', 'America/Boise'),
    (6, 35, '[-07:00] America/Cambridge_Bay', 'America/Cambridge_Bay'),
    (6, 36, '[-07:00] America/Chihuahua', 'America/Chihuahua'),
    (6, 37, '[-07:00] America/Dawson_Creek', 'America/Dawson_Creek'),
    (6, 38, '[-07:00] America/Denver', 'America/Denver'),
    (6, 39, '[-07:00] America/Edmonton', 'America/Edmonton'),
    (6, 40, '[-07:00] America/Hermosillo', 'America/Hermosillo'),
    (6, 41, '[-07:00] America/Inuvik', 'America/Inuvik'),
    (6, 42, '[-07:00] America/Mazatlan', 'America/Mazatlan'),
    (6, 43, '[-07:00] America/Phoenix', 'America/Phoenix'),
    (6, 44, '[-07:00] America/Shiprock', 'America/Shiprock'),
    (6, 45, '[-07:00] America/Yellowknife', 'America/Yellowknife'),
    (6, 46, '[-07:00] Canada/Mountain', 'Canada/Mountain'),
    (6, 47, '[-07:00] Mexico/BajaSur', 'Mexico/BajaSur'),
    (6, 48, '[-07:00] US/Arizona', 'US/Arizona'),
    (6, 49, '[-07:00] US/Mountain', 'US/Mountain')
;
-- CHECKPOINT C-12i

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 50, '[-06:00] America/Belize', 'America/Belize'),
    (6, 51, '[-06:00] America/Cancun', 'America/Cancun'),
    (6, 52, '[-06:00] America/Chicago', 'America/Chicago'),
    (6, 53, '[-06:00] America/Costa_Rica', 'America/Costa_Rica'),
    (6, 54, '[-06:00] America/El_Salvador', 'America/El_Salvador'),
    (6, 55, '[-06:00] America/Guatemala', 'America/Guatemala'),
    (6, 56, '[-06:00] America/Knox_IN', 'America/Knox_IN'),
    (6, 57, '[-06:00] America/Managua', 'America/Managua'),
    (6, 58, '[-06:00] America/Menominee', 'America/Menominee'),
    (6, 59, '[-06:00] America/Merida', 'America/Merida'),
    (6, 60, '[-06:00] America/Mexico_City', 'America/Mexico_City'),
    (6, 61, '[-06:00] America/Monterrey', 'America/Monterrey'),
    (6, 62, '[-06:00] America/Rainy_River', 'America/Rainy_River'),
    (6, 63, '[-06:00] America/Rankin_Inlet', 'America/Rankin_Inlet'),
    (6, 64, '[-06:00] America/Regina', 'America/Regina'),
    (6, 65, '[-06:00] America/Swift_Current', 'America/Swift_Current'),
    (6, 66, '[-06:00] America/Tegucigalpa', 'America/Tegucigalpa'),
    (6, 67, '[-06:00] America/Winnipeg', 'America/Winnipeg'),
    (6, 68, '[-06:00] Canada/Central', 'Canada/Central'),
    (6, 69, '[-06:00] Canada/East-Saskatchewan', 'Canada/East-Saskatchewan'),
    (6, 70, '[-06:00] Canada/Saskatchewan', 'Canada/Saskatchewan'),
    (6, 71, '[-06:00] Chile/EasterIsland', 'Chile/EasterIsland'),
    (6, 72, '[-06:00] Mexico/General', 'Mexico/General'),
    (6, 73, '[-06:00] Pacific/Easter', 'Pacific/Easter'),
    (6, 74, '[-06:00] Pacific/Galapagos', 'Pacific/Galapagos'),
    (6, 75, '[-06:00] US/Central', 'US/Central'),
    (6, 76, '[-06:00] US/Indiana-Starke', 'US/Indiana-Starke'),
    (6, 77, '[-05:00] America/Atikokan', 'America/Atikokan'),
    (6, 78, '[-05:00] America/Bogota', 'America/Bogota'),
    (6, 79, '[-05:00] America/Cayman', 'America/Cayman'),
    (6, 80, '[-05:00] America/Coral_Harbour', 'America/Coral_Harbour'),
    (6, 81, '[-05:00] America/Detroit', 'America/Detroit'),
    (6, 82, '[-05:00] America/Fort_Wayne', 'America/Fort_Wayne'),
    (6, 83, '[-05:00] America/Grand_Turk', 'America/Grand_Turk'),
    (6, 84, '[-05:00] America/Guayaquil', 'America/Guayaquil'),
    (6, 85, '[-05:00] America/Havana', 'America/Havana'),
    (6, 86, '[-05:00] America/Indianapolis', 'America/Indianapolis'),
    (6, 87, '[-05:00] America/Iqaluit', 'America/Iqaluit'),
    (6, 88, '[-05:00] America/Jamaica', 'America/Jamaica'),
    (6, 89, '[-05:00] America/Lima', 'America/Lima'),
    (6, 90, '[-05:00] America/Louisville', 'America/Louisville'),
    (6, 91, '[-05:00] America/Montreal', 'America/Montreal'),
    (6, 92, '[-05:00] America/Nassau', 'America/Nassau'),
    (6, 93, '[-05:00] America/New_York', 'America/New_York'),
    (6, 94, '[-05:00] America/Nipigon', 'America/Nipigon'),
    (6, 95, '[-05:00] America/Panama', 'America/Panama'),
    (6, 96, '[-05:00] America/Pangnirtung', 'America/Pangnirtung'),
    (6, 97, '[-05:00] America/Port-au-Prince', 'America/Port-au-Prince'),
    (6, 98, '[-05:00] America/Resolute', 'America/Resolute'),
    (6, 99, '[-05:00] America/Thunder_Bay', 'America/Thunder_Bay')
;
-- CHECKPOINT C-12j

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 100, '[-05:00] America/Toronto', 'America/Toronto'),
    (6, 101, '[-05:00] Canada/Eastern', 'Canada/Eastern'),
    (6, 102, '[-05:00] US/Eastern', 'US/Eastern'),
    (6, 103, '[-05:00] US/East-Indiana', 'US/East-Indiana'),
    (6, 104, '[-05:00] US/Michigan', 'US/Michigan'),
    (6, 105, '[-04:30] America/Caracas', 'America/Caracas'),
    (6, 106, '[-04:00] America/Anguilla', 'America/Anguilla'),
    (6, 107, '[-04:00] America/Antigua', 'America/Antigua'),
    (6, 108, '[-04:00] America/Aruba', 'America/Aruba'),
    (6, 109, '[-04:00] America/Asuncion', 'America/Asuncion'),
    (6, 110, '[-04:00] America/Barbados', 'America/Barbados'),
    (6, 111, '[-04:00] America/Blanc-Sablon', 'America/Blanc-Sablon'),
    (6, 112, '[-04:00] America/Boa_Vista', 'America/Boa_Vista'),
    (6, 113, '[-04:00] America/Campo_Grande', 'America/Campo_Grande'),
    (6, 114, '[-04:00] America/Cuiaba', 'America/Cuiaba'),
    (6, 115, '[-04:00] America/Curacao', 'America/Curacao'),
    (6, 116, '[-04:00] America/Dominica', 'America/Dominica'),
    (6, 117, '[-04:00] America/Eirunepe', 'America/Eirunepe'),
    (6, 118, '[-04:00] America/Glace_Bay', 'America/Glace_Bay'),
    (6, 119, '[-04:00] America/Goose_Bay', 'America/Goose_Bay'),
    (6, 120, '[-04:00] America/Grenada', 'America/Grenada'),
    (6, 121, '[-04:00] America/Guadeloupe', 'America/Guadeloupe'),
    (6, 122, '[-04:00] America/Guyana', 'America/Guyana'),
    (6, 123, '[-04:00] America/Halifax', 'America/Halifax'),
    (6, 124, '[-04:00] America/La_Paz', 'America/La_Paz'),
    (6, 125, '[-04:00] America/Manaus', 'America/Manaus'),
    (6, 126, '[-04:00] America/Marigot', 'America/Marigot'),
    (6, 127, '[-04:00] America/Martinique', 'America/Martinique'),
    (6, 128, '[-04:00] America/Moncton', 'America/Moncton'),
    (6, 129, '[-04:00] America/Montserrat', 'America/Montserrat'),
    (6, 130, '[-04:00] America/Port_of_Spain', 'America/Port_of_Spain'),
    (6, 131, '[-04:00] America/Porto_Acre', 'America/Porto_Acre'),
    (6, 132, '[-04:00] America/Porto_Velho', 'America/Porto_Velho'),
    (6, 133, '[-04:00] America/Puerto_Rico', 'America/Puerto_Rico'),
    (6, 134, '[-04:00] America/Rio_Branco', 'America/Rio_Branco'),
    (6, 135, '[-04:00] America/Santiago', 'America/Santiago'),
    (6, 136, '[-04:00] America/Santo_Domingo', 'America/Santo_Domingo'),
    (6, 137, '[-04:00] America/St_Barthelemy', 'America/St_Barthelemy'),
    (6, 138, '[-04:00] America/St_Kitts', 'America/St_Kitts'),
    (6, 139, '[-04:00] America/St_Lucia', 'America/St_Lucia'),
    (6, 140, '[-04:00] America/St_Thomas', 'America/St_Thomas'),
    (6, 141, '[-04:00] America/St_Vincent', 'America/St_Vincent'),
    (6, 142, '[-04:00] America/Thule', 'America/Thule'),
    (6, 143, '[-04:00] America/Tortola', 'America/Tortola'),
    (6, 144, '[-04:00] America/Virgin', 'America/Virgin'),
    (6, 145, '[-04:00] Antarctica/Palmer', 'Antarctica/Palmer'),
    (6, 146, '[-04:00] Atlantic/Bermuda', 'Atlantic/Bermuda'),
    (6, 147, '[-04:00] Atlantic/Stanley', 'Atlantic/Stanley'),
    (6, 148, '[-04:00] Brazil/Acre', 'Brazil/Acre'),
    (6, 149, '[-04:00] Brazil/West', 'Brazil/West')
;
-- CHECKPOINT C-12k

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 150, '[-04:00] Canada/Atlantic', 'Canada/Atlantic'),
    (6, 151, '[-04:00] Chile/Continental', 'Chile/Continental'),
    (6, 152, '[-03:30] America/St_Johns', 'America/St_Johns'),
    (6, 153, '[-03:30] Canada/Newfoundland', 'Canada/Newfoundland'),
    (6, 154, '[-03:00] America/Araguaina', 'America/Araguaina'),
    (6, 155, '[-03:00] America/Bahia', 'America/Bahia'),
    (6, 156, '[-03:00] America/Belem', 'America/Belem'),
    (6, 157, '[-03:00] America/Buenos_Aires', 'America/Buenos_Aires'),
    (6, 158, '[-03:00] America/Catamarca', 'America/Catamarca'),
    (6, 159, '[-03:00] America/Cayenne', 'America/Cayenne'),
    (6, 160, '[-03:00] America/Cordoba', 'America/Cordoba'),
    (6, 161, '[-03:00] America/Fortaleza', 'America/Fortaleza'),
    (6, 162, '[-03:00] America/Godthab', 'America/Godthab'),
    (6, 163, '[-03:00] America/Jujuy', 'America/Jujuy'),
    (6, 164, '[-03:00] America/Maceio', 'America/Maceio'),
    (6, 165, '[-03:00] America/Mendoza', 'America/Mendoza'),
    (6, 166, '[-03:00] America/Miquelon', 'America/Miquelon'),
    (6, 167, '[-03:00] America/Montevideo', 'America/Montevideo'),
    (6, 168, '[-03:00] America/Paramaribo', 'America/Paramaribo'),
    (6, 169, '[-03:00] America/Recife', 'America/Recife'),
    (6, 170, '[-03:00] America/Rosario', 'America/Rosario'),
    (6, 171, '[-03:00] America/Santarem', 'America/Santarem'),
    (6, 172, '[-03:00] America/Sao_Paulo', 'America/Sao_Paulo'),
    (6, 173, '[-03:00] Antarctica/Rothera', 'Antarctica/Rothera'),
    (6, 174, '[-03:00] Brazil/East', 'Brazil/East'),
    (6, 175, '[-02:00] America/Noronha', 'America/Noronha'),
    (6, 176, '[-02:00] Atlantic/South_Georgia', 'Atlantic/South_Georgia'),
    (6, 177, '[-02:00] Brazil/DeNoronha', 'Brazil/DeNoronha'),
    (6, 178, '[-01:00] America/Scoresbysund', 'America/Scoresbysund'),
    (6, 179, '[-01:00] Atlantic/Azores', 'Atlantic/Azores'),
    (6, 180, '[-01:00] Atlantic/Cape_Verde', 'Atlantic/Cape_Verde'),
    (6, 181, '[+00:00] Africa/Abidjan', 'Africa/Abidjan'),
    (6, 182, '[+00:00] Africa/Accra', 'Africa/Accra'),
    (6, 183, '[+00:00] Africa/Bamako', 'Africa/Bamako'),
    (6, 184, '[+00:00] Africa/Banjul', 'Africa/Banjul'),
    (6, 185, '[+00:00] Africa/Bissau', 'Africa/Bissau'),
    (6, 186, '[+00:00] Africa/Casablanca', 'Africa/Casablanca'),
    (6, 187, '[+00:00] Africa/Conakry', 'Africa/Conakry'),
    (6, 188, '[+00:00] Africa/Dakar', 'Africa/Dakar'),
    (6, 189, '[+00:00] Africa/El_Aaiun', 'Africa/El_Aaiun'),
    (6, 190, '[+00:00] Africa/Freetown', 'Africa/Freetown'),
    (6, 191, '[+00:00] Africa/Lome', 'Africa/Lome'),
    (6, 192, '[+00:00] Africa/Monrovia', 'Africa/Monrovia'),
    (6, 193, '[+00:00] Africa/Nouakchott', 'Africa/Nouakchott'),
    (6, 194, '[+00:00] Africa/Ouagadougou', 'Africa/Ouagadougou'),
    (6, 195, '[+00:00] Africa/Sao_Tome', 'Africa/Sao_Tome'),
    (6, 196, '[+00:00] Africa/Timbuktu', 'Africa/Timbuktu'),
    (6, 197, '[+00:00] America/Danmarkshavn', 'America/Danmarkshavn'),
    (6, 198, '[+00:00] Atlantic/Canary', 'Atlantic/Canary'),
    (6, 199, '[+00:00] Atlantic/Faeroe', 'Atlantic/Faeroe')
;
-- CHECKPOINT C-12l

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 200, '[+00:00] Atlantic/Faroe', 'Atlantic/Faroe'),
    (6, 201, '[+00:00] Atlantic/Madeira', 'Atlantic/Madeira'),
    (6, 202, '[+00:00] Atlantic/Reykjavik', 'Atlantic/Reykjavik'),
    (6, 203, '[+00:00] Atlantic/St_Helena', 'Atlantic/St_Helena'),
    (6, 204, '[+00:00] Europe/Belfast', 'Europe/Belfast'),
    (6, 205, '[+00:00] Europe/Dublin', 'Europe/Dublin'),
    (6, 206, '[+00:00] Europe/Guernsey', 'Europe/Guernsey'),
    (6, 207, '[+00:00] Europe/Isle_of_Man', 'Europe/Isle_of_Man'),
    (6, 208, '[+00:00] Europe/Jersey', 'Europe/Jersey'),
    (6, 209, '[+00:00] Europe/Lisbon', 'Europe/Lisbon'),
    (6, 210, '[+00:00] Europe/London', 'Europe/London'),
    (6, 211, '[+01:00] Africa/Algiers', 'Africa/Algiers'),
    (6, 212, '[+01:00] Africa/Bangui', 'Africa/Bangui'),
    (6, 213, '[+01:00] Africa/Brazzaville', 'Africa/Brazzaville'),
    (6, 214, '[+01:00] Africa/Ceuta', 'Africa/Ceuta'),
    (6, 215, '[+01:00] Africa/Douala', 'Africa/Douala'),
    (6, 216, '[+01:00] Africa/Kinshasa', 'Africa/Kinshasa'),
    (6, 217, '[+01:00] Africa/Lagos', 'Africa/Lagos'),
    (6, 218, '[+01:00] Africa/Libreville', 'Africa/Libreville'),
    (6, 219, '[+01:00] Africa/Luanda', 'Africa/Luanda'),
    (6, 220, '[+01:00] Africa/Malabo', 'Africa/Malabo'),
    (6, 221, '[+01:00] Africa/Ndjamena', 'Africa/Ndjamena'),
    (6, 222, '[+01:00] Africa/Niamey', 'Africa/Niamey'),
    (6, 223, '[+01:00] Africa/Porto-Novo', 'Africa/Porto-Novo'),
    (6, 224, '[+01:00] Africa/Tunis', 'Africa/Tunis'),
    (6, 225, '[+01:00] Africa/Windhoek', 'Africa/Windhoek'),
    (6, 226, '[+01:00] Arctic/Longyearbyen', 'Arctic/Longyearbyen'),
    (6, 227, '[+01:00] Atlantic/Jan_Mayen', 'Atlantic/Jan_Mayen'),
    (6, 228, '[+01:00] Europe/Amsterdam', 'Europe/Amsterdam'),
    (6, 229, '[+01:00] Europe/Andorra', 'Europe/Andorra'),
    (6, 230, '[+01:00] Europe/Belgrade', 'Europe/Belgrade'),
    (6, 231, '[+01:00] Europe/Berlin', 'Europe/Berlin'),
    (6, 232, '[+01:00] Europe/Bratislava', 'Europe/Bratislava'),
    (6, 233, '[+01:00] Europe/Brussels', 'Europe/Brussels'),
    (6, 234, '[+01:00] Europe/Budapest', 'Europe/Budapest'),
    (6, 235, '[+01:00] Europe/Copenhagen', 'Europe/Copenhagen'),
    (6, 236, '[+01:00] Europe/Gibraltar', 'Europe/Gibraltar'),
    (6, 237, '[+01:00] Europe/Ljubljana', 'Europe/Ljubljana'),
    (6, 238, '[+01:00] Europe/Luxembourg', 'Europe/Luxembourg'),
    (6, 239, '[+01:00] Europe/Madrid', 'Europe/Madrid'),
    (6, 240, '[+01:00] Europe/Malta', 'Europe/Malta'),
    (6, 241, '[+01:00] Europe/Monaco', 'Europe/Monaco'),
    (6, 242, '[+01:00] Europe/Oslo', 'Europe/Oslo'),
    (6, 243, '[+01:00] Europe/Paris', 'Europe/Paris'),
    (6, 244, '[+01:00] Europe/Podgorica', 'Europe/Podgorica'),
    (6, 245, '[+01:00] Europe/Prague', 'Europe/Prague'),
    (6, 246, '[+01:00] Europe/Rome', 'Europe/Rome'),
    (6, 247, '[+01:00] Europe/San_Marino', 'Europe/San_Marino'),
    (6, 248, '[+01:00] Europe/Sarajevo', 'Europe/Sarajevo'),
    (6, 249, '[+01:00] Europe/Skopje', 'Europe/Skopje')
;
-- CHECKPOINT C-12m

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 250, '[+01:00] Europe/Stockholm', 'Europe/Stockholm'),
    (6, 251, '[+01:00] Europe/Tirane', 'Europe/Tirane'),
    (6, 252, '[+01:00] Europe/Vaduz', 'Europe/Vaduz'),
    (6, 253, '[+01:00] Europe/Vatican', 'Europe/Vatican'),
    (6, 254, '[+01:00] Europe/Vienna', 'Europe/Vienna'),
    (6, 255, '[+01:00] Europe/Warsaw', 'Europe/Warsaw'),
    (6, 256, '[+01:00] Europe/Zagreb', 'Europe/Zagreb'),
    (6, 257, '[+01:00] Europe/Zurich', 'Europe/Zurich'),
    (6, 258, '[+02:00] Africa/Blantyre', 'Africa/Blantyre'),
    (6, 259, '[+02:00] Africa/Bujumbura', 'Africa/Bujumbura'),
    (6, 260, '[+02:00] Africa/Cairo', 'Africa/Cairo'),
    (6, 261, '[+02:00] Africa/Gaborone', 'Africa/Gaborone'),
    (6, 262, '[+02:00] Africa/Harare', 'Africa/Harare'),
    (6, 263, '[+02:00] Africa/Johannesburg', 'Africa/Johannesburg'),
    (6, 264, '[+02:00] Africa/Kigali', 'Africa/Kigali'),
    (6, 265, '[+02:00] Africa/Lubumbashi', 'Africa/Lubumbashi'),
    (6, 266, '[+02:00] Africa/Lusaka', 'Africa/Lusaka'),
    (6, 267, '[+02:00] Africa/Maputo', 'Africa/Maputo'),
    (6, 268, '[+02:00] Africa/Maseru', 'Africa/Maseru'),
    (6, 269, '[+02:00] Africa/Mbabane', 'Africa/Mbabane'),
    (6, 270, '[+02:00] Africa/Tripoli', 'Africa/Tripoli'),
    (6, 271, '[+02:00] Asia/Amman', 'Asia/Amman'),
    (6, 272, '[+02:00] Asia/Beirut', 'Asia/Beirut'),
    (6, 273, '[+02:00] Asia/Damascus', 'Asia/Damascus'),
    (6, 274, '[+02:00] Asia/Gaza', 'Asia/Gaza'),
    (6, 275, '[+02:00] Asia/Istanbul', 'Asia/Istanbul'),
    (6, 276, '[+02:00] Asia/Jerusalem', 'Asia/Jerusalem'),
    (6, 277, '[+02:00] Asia/Nicosia', 'Asia/Nicosia'),
    (6, 278, '[+02:00] Asia/Tel_Aviv', 'Asia/Tel_Aviv'),
    (6, 279, '[+02:00] Europe/Athens', 'Europe/Athens'),
    (6, 280, '[+02:00] Europe/Bucharest', 'Europe/Bucharest'),
    (6, 281, '[+02:00] Europe/Chisinau', 'Europe/Chisinau'),
    (6, 282, '[+02:00] Europe/Helsinki', 'Europe/Helsinki'),
    (6, 283, '[+02:00] Europe/Istanbul', 'Europe/Istanbul'),
    (6, 284, '[+02:00] Europe/Kaliningrad', 'Europe/Kaliningrad'),
    (6, 285, '[+02:00] Europe/Kiev', 'Europe/Kiev'),
    (6, 286, '[+02:00] Europe/Mariehamn', 'Europe/Mariehamn'),
    (6, 287, '[+02:00] Europe/Minsk', 'Europe/Minsk'),
    (6, 288, '[+02:00] Europe/Nicosia', 'Europe/Nicosia'),
    (6, 289, '[+02:00] Europe/Riga', 'Europe/Riga'),
    (6, 290, '[+02:00] Europe/Simferopol', 'Europe/Simferopol'),
    (6, 291, '[+02:00] Europe/Sofia', 'Europe/Sofia'),
    (6, 292, '[+02:00] Europe/Tallinn', 'Europe/Tallinn'),
    (6, 293, '[+02:00] Europe/Tiraspol', 'Europe/Tiraspol'),
    (6, 294, '[+02:00] Europe/Uzhgorod', 'Europe/Uzhgorod'),
    (6, 295, '[+02:00] Europe/Vilnius', 'Europe/Vilnius'),
    (6, 296, '[+02:00] Europe/Zaporozhye', 'Europe/Zaporozhye'),
    (6, 297, '[+03:00] Africa/Addis_Ababa', 'Africa/Addis_Ababa'),
    (6, 298, '[+03:00] Africa/Asmara', 'Africa/Asmara'),
    (6, 299, '[+03:00] Africa/Asmera', 'Africa/Asmera')
;
-- CHECKPOINT C-12n

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 300, '[+03:00] Africa/Dar_es_Salaam', 'Africa/Dar_es_Salaam'),
    (6, 301, '[+03:00] Africa/Djibouti', 'Africa/Djibouti'),
    (6, 302, '[+03:00] Africa/Kampala', 'Africa/Kampala'),
    (6, 303, '[+03:00] Africa/Khartoum', 'Africa/Khartoum'),
    (6, 304, '[+03:00] Africa/Mogadishu', 'Africa/Mogadishu'),
    (6, 305, '[+03:00] Africa/Nairobi', 'Africa/Nairobi'),
    (6, 306, '[+03:00] Antarctica/Syowa', 'Antarctica/Syowa'),
    (6, 307, '[+03:00] Asia/Aden', 'Asia/Aden'),
    (6, 308, '[+03:00] Asia/Baghdad', 'Asia/Baghdad'),
    (6, 309, '[+03:00] Asia/Bahrain', 'Asia/Bahrain'),
    (6, 310, '[+03:00] Asia/Kuwait', 'Asia/Kuwait'),
    (6, 311, '[+03:00] Asia/Qatar', 'Asia/Qatar'),
    (6, 312, '[+03:00] Asia/Riyadh', 'Asia/Riyadh'),
    (6, 313, '[+03:00] Europe/Moscow', 'Europe/Moscow'),
    (6, 314, '[+03:00] Europe/Volgograd', 'Europe/Volgograd'),
    (6, 315, '[+03:00] Indian/Antananarivo', 'Indian/Antananarivo'),
    (6, 316, '[+03:00] Indian/Comoro', 'Indian/Comoro'),
    (6, 317, '[+03:00] Indian/Mayotte', 'Indian/Mayotte'),
    (6, 318, '[+03:30] Asia/Tehran', 'Asia/Tehran'),
    (6, 319, '[+04:00] Asia/Baku', 'Asia/Baku'),
    (6, 320, '[+04:00] Asia/Dubai', 'Asia/Dubai'),
    (6, 321, '[+04:00] Asia/Muscat', 'Asia/Muscat'),
    (6, 322, '[+04:00] Asia/Tbilisi', 'Asia/Tbilisi'),
    (6, 323, '[+04:00] Asia/Yerevan', 'Asia/Yerevan'),
    (6, 324, '[+04:00] Europe/Samara', 'Europe/Samara'),
    (6, 325, '[+04:00] Indian/Mahe', 'Indian/Mahe'),
    (6, 326, '[+04:00] Indian/Mauritius', 'Indian/Mauritius'),
    (6, 327, '[+04:00] Indian/Reunion', 'Indian/Reunion'),
    (6, 328, '[+04:30] Asia/Kabul', 'Asia/Kabul'),
    (6, 329, '[+05:00] Asia/Aqtau', 'Asia/Aqtau'),
    (6, 330, '[+05:00] Asia/Aqtobe', 'Asia/Aqtobe'),
    (6, 331, '[+05:00] Asia/Ashgabat', 'Asia/Ashgabat'),
    (6, 332, '[+05:00] Asia/Ashkhabad', 'Asia/Ashkhabad'),
    (6, 333, '[+05:00] Asia/Dushanbe', 'Asia/Dushanbe'),
    (6, 334, '[+05:00] Asia/Karachi', 'Asia/Karachi'),
    (6, 335, '[+05:00] Asia/Oral', 'Asia/Oral'),
    (6, 336, '[+05:00] Asia/Samarkand', 'Asia/Samarkand'),
    (6, 337, '[+05:00] Asia/Tashkent', 'Asia/Tashkent'),
    (6, 338, '[+05:00] Asia/Yekaterinburg', 'Asia/Yekaterinburg'),
    (6, 339, '[+05:00] Indian/Kerguelen', 'Indian/Kerguelen'),
    (6, 340, '[+05:00] Indian/Maldives', 'Indian/Maldives'),
    (6, 341, '[+05:30] Asia/Calcutta', 'Asia/Calcutta'),
    (6, 342, '[+05:30] Asia/Colombo', 'Asia/Colombo'),
    (6, 343, '[+05:30] Asia/Kolkata', 'Asia/Kolkata'),
    (6, 344, '[+05:45] Asia/Kathmandu', 'Asia/Kathmandu'),
    (6, 345, '[+05:45] Asia/Katmandu', 'Asia/Katmandu'),
    (6, 346, '[+06:00] Antarctica/Mawson', 'Antarctica/Mawson'),
    (6, 347, '[+06:00] Antarctica/Vostok', 'Antarctica/Vostok'),
    (6, 348, '[+06:00] Asia/Almaty', 'Asia/Almaty'),
    (6, 349, '[+06:00] Asia/Bishkek', 'Asia/Bishkek')
;
-- CHECKPOINT C-12o

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 350, '[+06:00] Asia/Dacca', 'Asia/Dacca'),
    (6, 351, '[+06:00] Asia/Dhaka', 'Asia/Dhaka'),
    (6, 352, '[+06:00] Asia/Novosibirsk', 'Asia/Novosibirsk'),
    (6, 353, '[+06:00] Asia/Omsk', 'Asia/Omsk'),
    (6, 354, '[+06:00] Asia/Qyzylorda', 'Asia/Qyzylorda'),
    (6, 355, '[+06:00] Asia/Thimbu', 'Asia/Thimbu'),
    (6, 356, '[+06:00] Asia/Thimphu', 'Asia/Thimphu'),
    (6, 357, '[+06:00] Indian/Chagos', 'Indian/Chagos'),
    (6, 358, '[+06:30] Asia/Rangoon', 'Asia/Rangoon'),
    (6, 359, '[+06:30] Indian/Cocos', 'Indian/Cocos'),
    (6, 360, '[+07:00] Antarctica/Davis', 'Antarctica/Davis'),
    (6, 361, '[+07:00] Asia/Bangkok', 'Asia/Bangkok'),
    (6, 362, '[+07:00] Asia/Ho_Chi_Minh', 'Asia/Ho_Chi_Minh'),
    (6, 363, '[+07:00] Asia/Hovd', 'Asia/Hovd'),
    (6, 364, '[+07:00] Asia/Jakarta', 'Asia/Jakarta'),
    (6, 365, '[+07:00] Asia/Krasnoyarsk', 'Asia/Krasnoyarsk'),
    (6, 366, '[+07:00] Asia/Phnom_Penh', 'Asia/Phnom_Penh'),
    (6, 367, '[+07:00] Asia/Pontianak', 'Asia/Pontianak'),
    (6, 368, '[+07:00] Asia/Saigon', 'Asia/Saigon'),
    (6, 369, '[+07:00] Asia/Vientiane', 'Asia/Vientiane'),
    (6, 370, '[+07:00] Indian/Christmas', 'Indian/Christmas'),
    (6, 371, '[+08:00] Antarctica/Casey', 'Antarctica/Casey'),
    (6, 372, '[+08:00] Asia/Brunei', 'Asia/Brunei'),
    (6, 373, '[+08:00] Asia/Choibalsan', 'Asia/Choibalsan'),
    (6, 374, '[+08:00] Asia/Chongqing', 'Asia/Chongqing'),
    (6, 375, '[+08:00] Asia/Chungking', 'Asia/Chungking'),
    (6, 376, '[+08:00] Asia/Harbin', 'Asia/Harbin'),
    (6, 377, '[+08:00] Asia/Hong_Kong', 'Asia/Hong_Kong'),
    (6, 378, '[+08:00] Asia/Irkutsk', 'Asia/Irkutsk'),
    (6, 379, '[+08:00] Asia/Kashgar', 'Asia/Kashgar'),
    (6, 380, '[+08:00] Asia/Kuala_Lumpur', 'Asia/Kuala_Lumpur'),
    (6, 381, '[+08:00] Asia/Kuching', 'Asia/Kuching'),
    (6, 382, '[+08:00] Asia/Macao', 'Asia/Macao'),
    (6, 383, '[+08:00] Asia/Macau', 'Asia/Macau'),
    (6, 384, '[+08:00] Asia/Makassar', 'Asia/Makassar'),
    (6, 385, '[+08:00] Asia/Manila', 'Asia/Manila'),
    (6, 386, '[+08:00] Asia/Shanghai', 'Asia/Shanghai'),
    (6, 387, '[+08:00] Asia/Singapore', 'Asia/Singapore'),
    (6, 388, '[+08:00] Asia/Taipei', 'Asia/Taipei'),
    (6, 389, '[+08:00] Asia/Ujung_Pandang', 'Asia/Ujung_Pandang'),
    (6, 390, '[+08:00] Asia/Ulaanbaatar', 'Asia/Ulaanbaatar'),
    (6, 391, '[+08:00] Asia/Ulan_Bator', 'Asia/Ulan_Bator'),
    (6, 392, '[+08:00] Asia/Urumqi', 'Asia/Urumqi'),
    (6, 393, '[+09:00] Asia/Dili', 'Asia/Dili'),
    (6, 394, '[+09:00] Asia/Jayapura', 'Asia/Jayapura'),
    (6, 395, '[+09:00] Asia/Pyongyang', 'Asia/Pyongyang'),
    (6, 396, '[+09:00] Asia/Seoul', 'Asia/Seoul'),
    (6, 397, '[+09:00] Asia/Tokyo', 'Asia/Tokyo'),
    (6, 398, '[+09:00] Asia/Yakutsk', 'Asia/Yakutsk'),
    (6, 399, '[+09:00] Pacific/Palau', 'Pacific/Palau')
;
-- CHECKPOINT C-12p

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 400, '[+10:00] Antarctica/DumontDUrville', 'Antarctica/DumontDUrville'),
    (6, 401, '[+10:00] Asia/Sakhalin', 'Asia/Sakhalin'),
    (6, 402, '[+10:00] Asia/Vladivostok', 'Asia/Vladivostok'),
    (6, 403, '[+10:00] Pacific/Guam', 'Pacific/Guam'),
    (6, 404, '[+10:00] Pacific/Port_Moresby', 'Pacific/Port_Moresby'),
    (6, 405, '[+10:00] Pacific/Saipan', 'Pacific/Saipan'),
    (6, 406, '[+10:00] Pacific/Truk', 'Pacific/Truk'),
    (6, 407, '[+10:00] Pacific/Yap', 'Pacific/Yap'),
    (6, 408, '[+11:00] Asia/Magadan', 'Asia/Magadan'),
    (6, 409, '[+11:00] Pacific/Efate', 'Pacific/Efate'),
    (6, 410, '[+11:00] Pacific/Guadalcanal', 'Pacific/Guadalcanal'),
    (6, 411, '[+11:00] Pacific/Kosrae', 'Pacific/Kosrae'),
    (6, 412, '[+11:00] Pacific/Noumea', 'Pacific/Noumea'),
    (6, 413, '[+11:00] Pacific/Ponape', 'Pacific/Ponape'),
    (6, 414, '[+11:30] Pacific/Norfolk', 'Pacific/Norfolk'),
    (6, 415, '[+12:00] Antarctica/McMurdo', 'Antarctica/McMurdo'),
    (6, 416, '[+12:00] Antarctica/South_Pole', 'Antarctica/South_Pole'),
    (6, 417, '[+12:00] Asia/Anadyr', 'Asia/Anadyr'),
    (6, 418, '[+12:00] Asia/Kamchatka', 'Asia/Kamchatka'),
    (6, 419, '[+12:00] Pacific/Auckland', 'Pacific/Auckland'),
    (6, 420, '[+12:00] Pacific/Fiji', 'Pacific/Fiji'),
    (6, 421, '[+12:00] Pacific/Funafuti', 'Pacific/Funafuti'),
    (6, 422, '[+12:00] Pacific/Kwajalein', 'Pacific/Kwajalein'),
    (6, 423, '[+12:00] Pacific/Majuro', 'Pacific/Majuro'),
    (6, 424, '[+12:00] Pacific/Nauru', 'Pacific/Nauru'),
    (6, 425, '[+12:00] Pacific/Tarawa', 'Pacific/Tarawa'),
    (6, 426, '[+12:00] Pacific/Wake', 'Pacific/Wake'),
    (6, 427, '[+12:00] Pacific/Wallis', 'Pacific/Wallis'),
    (6, 428, '[+12:45] Pacific/Chatham', 'Pacific/Chatham'),
    (6, 429, '[+13:00] Pacific/Enderbury', 'Pacific/Enderbury'),
    (6, 430, '[+13:00] Pacific/Tongatapu', 'Pacific/Tongatapu'),
    (6, 431, '[+14:00] Pacific/Kiritimati', 'Pacific/Kiritimati')
;
-- CHECKPOINT C-12q

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (7, 1, 'Logging disabled', '0'),
    (7, 2, 'Errors only', '1'),
    (7, 3, 'Errors and warnings', '2'),
    (7, 4, 'Errors, warnings and infos', '3'),
    (7, 5, 'Debugging (low detail)', '4'),
    (7, 6, 'Debugging (medium detail)', '5'),
    (7, 7, 'Debugging (high detail)', '6'),
    (8, 1, 'Disabled', '0'),
    (8, 2, 'Low detail', '1'),
    (8, 3, 'Medium detail', '2'),
    (8, 4, 'High detail', '3'),
    (9, 1, 'Never retry submissions', '1'),
    (9, 2, 'Ask user (specifiy period below)', '2'),
    (9, 3, 'Always retry submissions (specifiy period below)', '3'),
    (10, 1, 'Status requesting disabled', '1'),
    (10, 2, 'Web service', '2'),
    (10, 3, 'XML file', '3'),
    (11, 1, 'Status requesting disabled', '1'),
    (11, 2, 'Ganglia XML', '2'),
    (11, 3, 'Globus MDS XML', '3'),
    (12, 1, 'FTP', 'ftp'),
    (12, 2, 'SFTP', 'sftp'),
    (12, 3, 'SCP', 'scp'),
    (12, 4, 'GSIFTP', 'gsiftp'),
    (13, 1, '1', '1'),
    (13, 2, '2', '2'),
    (13, 3, '3', '3'),
    (13, 4, '4', '4'),
    (13, 5, '5', '5'),
    (14, 1, '[no general rule]', '0'),
    (14, 2, 'Never', '1'),
    (14, 3, 'Always', '2'),
    (15, 1, 'Disabled', '0'),
    (15, 2, 'Approval by administrator', '1'),
    (15, 3, 'Activation via e-mail link', '2'),
    (15, 4, 'Immediate activation', '3'),
    (16, 1, 'Disabled', '0'),
    (16, 2, 'Only configured OpenID providers', '1'),
    (16, 3, 'All OpenID providers', '2'),
    (17, 1, 'Do not use client certificates', ''),
    (17, 2, 'Check certificate subject', 'CERT_SUBJECT'),
    (17, 3, 'Compare PEM-formatted certificate', 'PROXY_SSL_CLIENT_CERT')
;
-- CHECKPOINT C-12r
-- RESULT

/*****************************************************************************/

CREATE TABLE serviceclass (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Name',
    CONSTRAINT pk_serviceclass PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Classes of processing services';
-- CHECKPOINT C-13

/*****************************************************************************/

CREATE TABLE servicecategory (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Name',
    CONSTRAINT pk_servicecategory PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Categories of processing services';
-- CHECKPOINT C-14a

CREATE PROCEDURE add_servicecategory(IN p_identifier varchar(50), IN p_name varchar(100))
COMMENT 'Inserts or updates a service category'
BEGIN
    DECLARE category_id int;
    SELECT id FROM servicecategory WHERE identifier = p_identifier INTO category_id;
    IF category_id IS NULL THEN
        INSERT INTO servicecategory (identifier, name) VALUES (p_identifier, p_name);
    ELSE
        UPDATE servicecategory SET name = p_name WHERE id = category_id;
    END IF;
END;
-- CHECKPOINT C-14b

/*****************************************************************************/

CREATE TABLE schedulerclass (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Name',
    CONSTRAINT pk_schedulerclass PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Classes of task schedulers';
-- CHECKPOINT C-15

/*****************************************************************************/

CREATE TABLE usr_auth (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    id_auth int unsigned NOT NULL COMMENT 'FK: Authentication type',
    username varchar(50) NOT NULL COMMENT 'Username for authentication type',
    active boolean NOT NULL DEFAULT true COMMENT 'True if account is active',
    CONSTRAINT pk_usr_auth PRIMARY KEY (id_usr, id_auth),
    CONSTRAINT fk_usr_auth_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_usr_auth_auth FOREIGN KEY (id_auth) REFERENCES auth(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Recognised user accounts';
-- CHECKPOINT C-17

/*****************************************************************************/

CREATE TABLE usrcert (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    own_cert boolean NOT NULL DEFAULT false COMMENT 'If true, user makes his own certificate settings',
    key_password varchar(50) COMMENT 'Password for private key and download',
    cert_subject varchar(255) COMMENT 'Certificate subject',
    cert_content_pem varchar(10000) COMMENT 'Certificate content (PEM)',
    cert_content_pub varchar(1000) COMMENT 'Certificate content (PUB)',
    cert_content_base64 varchar(10000) COMMENT 'Certificate content (Base64-encoded P12)',
    CONSTRAINT pk_usrcert PRIMARY KEY (id_usr),
    CONSTRAINT fk_usrcert_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User proxy certificates';
-- CHECKPOINT C-18

/*****************************************************************************/

CREATE TABLE usropenid (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    id_provider int unsigned COMMENT 'FK: OpenID provider (optional)',
    user_input varchar(100) COMMENT 'User input or user-specified identifier',
    claimed_id varchar(100) COMMENT 'Verified claimed identifier',
    CONSTRAINT pk_usropenid PRIMARY KEY (id),
    CONSTRAINT fk_usropenid_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_usropenid_provider FOREIGN KEY (id_provider) REFERENCES openidprovider(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Open ID identifiers for users';
-- CHECKPOINT C-19

/*****************************************************************************/

CREATE TABLE openidnonce (
    time_part datetime NOT NULL COMMENT 'Nonce UTC time',
    random_part varchar(50) COMMENT 'Random characters after time part'
) Engine=InnoDB COMMENT 'Open ID nonces';
-- CHECKPOINT C-20

/*****************************************************************************/

CREATE TABLE usrreg (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    token varchar(50) COMMENT 'Unique activation token (automatically generated UID)',
    reset boolean NOT NULL DEFAULT false COMMENT 'If true, password reset was requested',
    reg_date DATETIME NULL,
    reg_origin VARCHAR(50) NULL DEFAULT NULL,
    CONSTRAINT pk_usr PRIMARY KEY (id_usr),
    CONSTRAINT fk_usrreg_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User registration or password reset requests';
-- CHECKPOINT C-21

/*****************************************************************************/

CREATE TABLE usrsession (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    log_time datetime NOT NULL COMMENT 'Login time',
    CONSTRAINT fk_usrsession_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User sessions';
-- CHECKPOINT C-22

/*****************************************************************************/

CREATE TABLE filter (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned COMMENT 'FK: Owning user',
    id_type int unsigned COMMENT 'FK: Entity type',
    token varchar(50) NOT NULL COMMENT 'Unique token (automatically generated UID)',
    name varchar(50) COMMENT 'Unique name',
    url text NOT NULL COMMENT 'Absolute or relative URL for listing',
    definition text COMMENT 'Query string parameters defining the filter',
    CONSTRAINT pk_filter PRIMARY KEY (id),
    CONSTRAINT fk_filter_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_filter_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User-defined filters on entities';
-- CHECKPOINT C-23

/*****************************************************************************/

CREATE TABLE lge (
    id int unsigned NOT NULL auto_increment,
    name varchar(50) NOT NULL COMMENT 'Unique name',
    ws_url varchar(100) NOT NULL COMMENT 'wsServer web service access point',
    myproxy_address varchar(100) NOT NULL COMMENT 'Hostname of MyProxy server',
    status_method tinyint COMMENT 'Task and job status request method',
    task_status_url varchar(100) COMMENT 'URL of task status document',
    job_status_url varchar(100) COMMENT 'URL of job status document',
    ce_status_url varchar(100) COMMENT 'Grid status document URL',
    conf_file varchar(100) COMMENT 'Location of configuration file',
    CONSTRAINT pk_ce PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'LGE instances for Globus Computing Elements';
-- CHECKPOINT C-27

/*****************************************************************************/

CREATE TABLE cr (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, computing resource can be configured by other domains',
    availability tinyint NOT NULL DEFAULT 4 COMMENT 'Availability (0..4)',
    name varchar(100) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    hostname varchar(100) COMMENT 'Hostname',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    capacity int unsigned NOT NULL DEFAULT 0 COMMENT 'Maximum processing capacity',
    credit_control boolean NOT NULL DEFAULT false COMMENT 'If true, computing resource controls user credits',
    CONSTRAINT pk_cr PRIMARY KEY (id),
    CONSTRAINT fk_cr_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Computing resources';
-- CHECKPOINT C-28

/*****************************************************************************/

CREATE TABLE cr_perm (
    id_cr int unsigned NOT NULL COMMENT 'FK: Computing resource',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    credits int unsigned NOT NULL DEFAULT 0 COMMENT 'Maximum resource credits for the user',
    CONSTRAINT fk_cr_perm_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on computing resources';
-- CHECKPOINT C-29

/*****************************************************************************/

CREATE TABLE crstate (
    id_cr int unsigned NOT NULL COMMENT 'Computing resource (PK+FK)',
    total_nodes int unsigned COMMENT 'Total capacity available on computing resource',
    free_nodes int unsigned COMMENT 'Free capacity available on computing resource',
    modified datetime,
    CONSTRAINT pk_crstate PRIMARY KEY (id_cr),
    CONSTRAINT fk_crstate_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Computing resource state information';
-- CHECKPOINT C-30a

CREATE TRIGGER crstate_insert BEFORE INSERT ON crstate FOR EACH ROW
BEGIN
    SET NEW.modified = utc_timestamp();
END;
-- CHECKPOINT C-30b

CREATE TRIGGER crstate_update BEFORE UPDATE ON crstate FOR EACH ROW
BEGIN
    SET NEW.modified = utc_timestamp();
END;
-- CHECKPOINT C-30c

/*****************************************************************************/

CREATE TABLE ce (
    id int unsigned NOT NULL,
    id_lge int unsigned,
    ce_port smallint unsigned COMMENT 'CE port',
    gsi_port smallint unsigned COMMENT 'GSI port',
    job_manager varchar(100) COMMENT 'Job manager',
    flags varchar(100) COMMENT 'Flags',
    grid_type varchar(200) COMMENT 'Grid type',
    job_queue varchar(200) COMMENT 'Job queue',
    status_method tinyint NOT NULL DEFAULT 0 COMMENT 'Status request method',
    status_url varchar(100) COMMENT 'URL for status information',
    CONSTRAINT pk_ce PRIMARY KEY (id),
    CONSTRAINT fk_ce_cr FOREIGN KEY (id) REFERENCES cr(id) ON DELETE CASCADE,
    CONSTRAINT fk_ce_lge FOREIGN KEY (id_lge) REFERENCES lge(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Globus Computing Elements';
-- CHECKPOINT C-31

/*****************************************************************************/

CREATE TABLE cedir (
    id int unsigned NOT NULL auto_increment,
    id_ce int unsigned NOT NULL COMMENT 'Globus Computing Element (FK)',
    available boolean NOT NULL DEFAULT true COMMENT 'If true, directory is available',
    dir_type char(1) NOT NULL COMMENT '''W'': working dir, ''R'': result dir',
    path varchar(50) NOT NULL COMMENT 'Directory absolute path',
    CONSTRAINT pk_cedir PRIMARY KEY (id),
    CONSTRAINT fk_cedir_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Globus Computing Element special directories';
-- CHECKPOINT C-32

/*****************************************************************************/

CREATE TABLE wpsprovider (
    id int unsigned NOT NULL,
    url varchar(300) COMMENT 'Base WPS access point',
    proxy boolean NOT NULL DEFAULT false COMMENT 'If true, wps is proxied',
    contact varchar(200) COMMENT 'WPS contact point (link or email)',
    autosync boolean NOT NULL DEFAULT false COMMENT 'If true, wps is automatically synchronized',
    stage boolean NOT NULL DEFAULT true COMMENT 'If true, wps jobs are automatically staged',
    tags VARCHAR(150) NULL DEFAULT NULL COMMENT 'Tags describing the provider',
    CONSTRAINT pk_wpsprovider PRIMARY KEY (id),
    CONSTRAINT fk_wpsprovider_cr FOREIGN KEY (id) REFERENCES cr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Web Processing Service (WPS) providers';
-- CHECKPOINT C-33

/*****************************************************************************/

CREATE TABLE catalogue (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, catalogue can be configured by other domains',
    name varchar(100) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    osd_url varchar(100) NOT NULL COMMENT 'OpenSearch description URL',
    base_url varchar(100) NOT NULL COMMENT 'Base URL (prefix for relative URLs)',
    series_rel_url varchar(100) COMMENT 'Relative URL for series list/ingestion',
    dataset_rel_url varchar(100) COMMENT 'Relative URL for data set list/ingestion',
    CONSTRAINT pk_catalogue PRIMARY KEY (id),
    CONSTRAINT fk_catalogue_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Catalogues';
-- CHECKPOINT C-34

/*****************************************************************************/

CREATE TABLE series (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_catalogue int unsigned COMMENT 'FK: Containing catalogue',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, series can be configured by other domains',
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Name',
    description text COMMENT 'Description',
    cat_description varchar(200) COMMENT 'OpenSearch description URL of series',
    cat_template varchar(1000) COMMENT 'OpenSearch template URL of series',
    default_mime_type varchar(50) NOT NULL DEFAULT 'application/atom+xml' COMMENT 'Default MimeType for OpenSearch',
    auto_refresh boolean NOT NULL DEFAULT true COMMENT 'If true, template is refreshed by the background agent',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    manual_assign boolean COMMENT 'If true, series must be assigned manually to services',
    dataset_count int COMMENT 'Number of data sets belonging to the series',
    last_update_time datetime COMMENT 'Last update time of the series',
    CONSTRAINT pk_series PRIMARY KEY (id),
    CONSTRAINT fk_series_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    CONSTRAINT fk_series_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    CONSTRAINT fk_series_catalogue FOREIGN KEY (id_catalogue) REFERENCES catalogue(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Dataset series';
-- CHECKPOINT C-35a

CREATE PROCEDURE add_series(IN p_type_id int unsigned, IN p_identifier varchar(50), IN p_name varchar(200), IN p_description text, IN p_cat_description varchar(200))
COMMENT 'Inserts or updates a series'
BEGIN
    DECLARE series_id int;
    SELECT id FROM series WHERE identifier = p_identifier INTO series_id;
    IF series_id IS NULL THEN
        INSERT INTO series (id_type, identifier, name, description, cat_description) VALUES (p_type_id, p_identifier, p_name, p_description, p_cat_description);
    END IF;
END;
-- CHECKPOINT C-35b

/*****************************************************************************/

CREATE TABLE series_perm (
    id_series int unsigned NOT NULL COMMENT 'FK: Series',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    can_search boolean COMMENT 'If true, user/group has product search permission',
    can_download boolean COMMENT 'If true, user/group has download permission',
    can_process boolean COMMENT 'If true, user/group has processing permission',
    CONSTRAINT fk_series_perm_series FOREIGN KEY (id_series) REFERENCES series(id) ON DELETE CASCADE,
    CONSTRAINT fk_series_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_series_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on series';
-- CHECKPOINT C-36

/*****************************************************************************/

CREATE TABLE producttype (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_catalogue int unsigned COMMENT 'FK: Containing catalogue',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, product type can be configured by other domains',
    identifier varchar(100) NOT NULL COMMENT 'Unique identifier',
    name varchar(50) NOT NULL COMMENT 'Name',
    description text COMMENT 'Description',
    cat_description varchar(200) COMMENT 'OpenSearch description URL of series',
    cat_template varchar(1000) COMMENT 'OpenSearch template URL of series',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    legend varchar(100) COMMENT 'Legend',
    rolling boolean COMMENT 'If true, product type is part of a rolling list',
    public boolean COMMENT 'If true, product type is viewable by the public',
    CONSTRAINT pk_producttype PRIMARY KEY (id),
    CONSTRAINT fk_producttype_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    CONSTRAINT fk_producttype_catalogue FOREIGN KEY (id_catalogue) REFERENCES catalogue(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Product types';
-- CHECKPOINT C-37

/*****************************************************************************/

CREATE TABLE producttype_perm (
    id_producttype int unsigned NOT NULL COMMENT 'FK: Product type',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_producttype_perm_producttype FOREIGN KEY (id_producttype) REFERENCES producttype(id) ON DELETE CASCADE,
    CONSTRAINT fk_producttype_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_producttype_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on product types';
-- CHECKPOINT C-38

/*****************************************************************************/

CREATE TABLE product (
    id int unsigned NOT NULL auto_increment,
    id_producttype int unsigned NOT NULL COMMENT 'FK: Product type',
    id_cr int unsigned COMMENT 'FK: Computing resource',
    name varchar(100) NOT NULL COMMENT 'Unique identifier',
    path varchar(200) NOT NULL COMMENT 'Path to result',
    preview_file varchar(200) COMMENT 'Location of preview file in file system',
    remote_id varchar(100) NOT NULL COMMENT 'Original remote identifier',
    CONSTRAINT pk_product PRIMARY KEY (id),
    -- UNIQUE INDEX (name),
    CONSTRAINT fk_product_producttype FOREIGN KEY (id_producttype) REFERENCES producttype(id) ON DELETE CASCADE,
    CONSTRAINT fk_product_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Products';
-- CHECKPOINT C-39

/*****************************************************************************/

CREATE TABLE productdata (
    id_product int unsigned NOT NULL COMMENT 'Related product',
    name varchar(50) NOT NULL COMMENT 'Parameter name',
    value longtext COMMENT 'Value',
    CONSTRAINT fk_productdata_product FOREIGN KEY (id_product) REFERENCES product(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Product metadata';
-- CHECKPOINT C-40

/*****************************************************************************/

CREATE TABLE resourceset (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    name varchar(50) COMMENT 'Name',
    kind TINYINT(4) NULL DEFAULT '0' COMMENT 'resource set kind',
    access_key varchar(50) COMMENT 'Access key',
    creation_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of resource set creation',
    CONSTRAINT pk_resourceset PRIMARY KEY (id),
    CONSTRAINT fk_resourceset_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    UNIQUE INDEX (identifier),
    CONSTRAINT fk_resourceset_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Sets of remote resources';
-- CHECKPOINT C-41

/*****************************************************************************/

CREATE TABLE resourceset_perm (
    id_resourceset int unsigned NOT NULL COMMENT 'FK: Resource set',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_resourceset_perm_resourceset FOREIGN KEY (id_resourceset) REFERENCES resourceset(id) ON DELETE CASCADE,
    CONSTRAINT fk_resourceset_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_resourceset_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on resource sets';
-- CHECKPOINT C-42

/*****************************************************************************/

CREATE TABLE resource (
    id int unsigned NOT NULL auto_increment,
    id_set int unsigned NOT NULL COMMENT 'FK: Owning resource set',
    location varchar(200) NOT NULL COMMENT 'Resource location, e.g. URI',
    name varchar(100) NULL DEFAULT NULL COMMENT 'Resource name',
    CONSTRAINT pk_resource PRIMARY KEY (id),
    CONSTRAINT fk_resource_set FOREIGN KEY (id_set) REFERENCES resourceset(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Remote resources';
-- CHECKPOINT C-43

/*****************************************************************************/

CREATE TABLE pubserver (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, publish server can be configured by other domains',
    name varchar(50) NOT NULL COMMENT 'Unique name',
    protocol varchar(10) NOT NULL COMMENT 'Connection protocol',
    hostname varchar(100) NOT NULL COMMENT 'Hostname',
    port smallint unsigned COMMENT 'Port on host',
    path varchar(100) NOT NULL DEFAULT '/' COMMENT 'Task result root path on host',
    username varchar(50) COMMENT 'Username (optional)',
    password varchar(50) COMMENT 'Password (optional)',
    public_key varchar(200) COMMENT 'User''s public key',
    options varchar(200) COMMENT 'Additional options for publishing',
    upload_url varchar(200) COMMENT 'URL for task result upload',
    download_url varchar(200) COMMENT 'URL for task result download (optional)',
    file_root varchar(200) COMMENT 'Absolute filesystem path of a task result root (optional)',
    is_default boolean NOT NULL DEFAULT false COMMENT 'If true, publish server is selected by default',
    metadata boolean NOT NULL DEFAULT false COMMENT 'If true, publish server is used for task result metadata, e.g. previews',
    delete_files boolean NOT NULL DEFAULT false COMMENT 'If true, delete also task result files when task is deleted',
    CONSTRAINT pk_pubserver PRIMARY KEY (id),
    CONSTRAINT fk_pubserver_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Publish servers';
-- CHECKPOINT C-44

/*****************************************************************************/

CREATE TABLE pubserver_perm (
    id_pubserver int unsigned NOT NULL COMMENT 'FK: Publish server',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_pubserver_perm_pubserver FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE CASCADE,
    CONSTRAINT fk_pubserver_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_pubserver_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on publish servers';
-- CHECKPOINT C-45

/*****************************************************************************/

CREATE TABLE service (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_class int unsigned COMMENT 'FK: Service class',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, service can be configured by other domains',
    available boolean NOT NULL DEFAULT true COMMENT 'If true, service is available',
    id_usr INT(10) UNSIGNED NULL COMMENT 'FK: User',
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Name',
    description text NOT NULL COMMENT 'Description',
    version varchar(10) COMMENT 'Version',
    c_version varchar(10) COMMENT 'Cleanup version (in case of interrupted upgrade)',
    url varchar(400) NOT NULL COMMENT 'Access point of service (relative URL)',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    view_url varchar(200) COMMENT 'View URL',
    rating tinyint COMMENT 'Rating in stars (0 to 5)',
    all_input boolean COMMENT 'If true, service accepts all non-manual series as input',
    created datetime,
    modified datetime,
    tags varchar(150) NULL DEFAULT NULL COMMENT 'Tags describing the service',
    quotable boolean DEFAULT false,
    geometry varchar(200) DEFAULT NULL COMMENT 'Geometry describing the AOI of the service',
    commercial boolean NOT NULL DEFAULT false COMMENT 'If true, service is defined as commercial',
    CONSTRAINT pk_service PRIMARY KEY (id),
    UNIQUE INDEX (identifier),
    CONSTRAINT fk_service_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_class FOREIGN KEY (id_class) REFERENCES serviceclass(id) ON DELETE SET NULL,
    CONSTRAINT fk_service_user FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Processing services';
-- CHECKPOINT C-46a

CREATE TRIGGER service_insert BEFORE INSERT ON service FOR EACH ROW
BEGIN
    SET NEW.created=utc_timestamp(), NEW.modified=utc_timestamp();
END;
-- CHECKPOINT C-46b

CREATE TRIGGER service_update BEFORE UPDATE ON service FOR EACH ROW
BEGIN
    SET NEW.modified=utc_timestamp();
END;
-- CHECKPOINT C-46c

/*****************************************************************************/

CREATE TABLE service_perm (
    id_service int unsigned NOT NULL COMMENT 'FK: Service',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    allow_scheduling boolean COMMENT 'If true, user can schedule the service',
    CONSTRAINT fk_service_perm_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on services';
-- CHECKPOINT C-47

/*****************************************************************************/

CREATE TABLE service_series (
    id_service int unsigned NOT NULL COMMENT 'FK: Service',
    id_series int unsigned NOT NULL COMMENT 'FK: Dataset series usable with service',
    is_default boolean COMMENT 'If true, series is default for service',
    CONSTRAINT pk_service_series PRIMARY KEY (id_service, id_series),
    CONSTRAINT fk_service_series_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_series_series FOREIGN KEY (id_series) REFERENCES series(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Compatibility map of services and dataset series';
-- CHECKPOINT C-48a

CREATE PROCEDURE link_service_series(IN service_identifier varchar(50), IN series_identifier varchar(50), IN is_default_series boolean)
COMMENT 'Links an input series to a service'
BEGIN
    DECLARE series_id int unsigned;
    DECLARE service_id int unsigned;
    DECLARE c int;

    SELECT id FROM service WHERE identifier = service_identifier INTO service_id;
    SELECT id FROM series WHERE identifier = series_identifier INTO series_id;

    IF service_id IS NOT NULL AND series_id IS NOT NULL THEN
        SELECT COUNT(*) FROM service_series AS t WHERE t.id_service = service_id AND t.id_series = series_id INTO c;
        IF c = 0 THEN
            INSERT INTO service_series (id_service, id_series) VALUES (service_id, series_id);
        END IF;

        IF is_default_series THEN
            UPDATE service_series SET is_default = (id_series = series_id) WHERE id_service = service_id;
        END IF;
    END IF;
END;
-- CHECKPOINT C-48b

CREATE PROCEDURE unlink_service_series(IN service_identifier varchar(50), IN series_identifier varchar(50))
COMMENT 'Unlinks an input series from a service'
BEGIN
    DELETE FROM service_series WHERE id_service = (SELECT id FROM service WHERE identifier = service_identifier) AND id_series = (SELECT id FROM series WHERE identifier = series_identifier);
END;
-- CHECKPOINT C-48c

/*****************************************************************************/

CREATE TABLE service_cr (
    id_service int unsigned NOT NULL COMMENT 'FK: Service',
    id_cr int unsigned NOT NULL COMMENT 'FK: Computing resource compatible with service',
    is_default boolean COMMENT 'If true, computing resource is default for service',
    CONSTRAINT pk_service_cr PRIMARY KEY (id_service, id_cr),
    CONSTRAINT fk_service_cr_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_cr_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Compatibility map of services and computing resources';
-- CHECKPOINT C-49

/*****************************************************************************/

CREATE TABLE service_category (
    id_service int unsigned NOT NULL COMMENT 'FK: Service',
    id_category int unsigned NOT NULL COMMENT 'FK: Service category',
    CONSTRAINT pk_service_category PRIMARY KEY (id_service, id_category),
    CONSTRAINT fk_service_category_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_category_category FOREIGN KEY (id_category) REFERENCES servicecategory(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of services to service categories';
-- CHECKPOINT C-50a

CREATE PROCEDURE link_service_category(IN service_identifier varchar(50), IN category_identifier varchar(50))
COMMENT 'Adds a service/category assignment'
BEGIN
    DECLARE c int;
    SELECT COUNT(*) FROM service_category AS t INNER JOIN service AS t1 ON t.id_service=t1.id INNER JOIN servicecategory AS t2 ON t.id_category=t2.id WHERE t1.identifier = service_identifier AND t2.identifier = category_identifier INTO c;
    IF c = 0 THEN
        INSERT INTO service_category (id_service, id_category) SELECT t1.id, t2.id FROM service AS t1 INNER JOIN servicecategory AS t2 WHERE t1.identifier = service_identifier AND t2.identifier = category_identifier;
    END IF;
END;
-- CHECKPOINT C-50b

CREATE PROCEDURE unlink_service_category(IN service_identifier varchar(50), IN category_identifier varchar(50))
COMMENT 'Removes a service/category assignment'
BEGIN
    DELETE FROM service_category WHERE id_service = (SELECT id FROM service WHERE identifier = service_identifier) AND id_category = (SELECT id FROM servicecategory WHERE identifier = category_identifier);
END;
-- CHECKPOINT C-50c

/*****************************************************************************/

CREATE TABLE serviceconfig (
    id_grp int unsigned COMMENT 'FK: Group (NULL for user config)',
    id_usr int unsigned COMMENT 'FK: User (NULL for group config)',
    id_service int unsigned COMMENT 'FK: Service parameter or constant (optional)',
    name varchar(50) NOT NULL COMMENT 'Parameter or constant name',
    caption varchar(50) COMMENT 'Caption for value',
    value text NOT NULL COMMENT 'Value of parameter or constant',
    INDEX idx_serviceconfig_name (id_service, name),
    CONSTRAINT fk_serviceconfig_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE,
    CONSTRAINT fk_serviceconfig_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_serviceconfig_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Service configuration settings for users and groups';
-- CHECKPOINT C-51

/*****************************************************************************/

CREATE TABLE scriptservice (
    id int unsigned NOT NULL,
    root varchar(200) NOT NULL COMMENT 'Directory containing service''s service.xml',
    CONSTRAINT pk_scriptservice PRIMARY KEY (id),
    CONSTRAINT fk_scriptservice_service FOREIGN KEY (id) REFERENCES service(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Task schedulers';
-- CHECKPOINT C-52

/*****************************************************************************/

CREATE TABLE wpsproc (
    id int unsigned NOT NULL,
    id_provider int unsigned NOT NULL COMMENT 'FK: WPS provider',
    remote_id varchar(100) COMMENT 'Process identifier on WPS provider',
    CONSTRAINT pk_wpsproc PRIMARY KEY (id),
    CONSTRAINT fk_wpsproc_service FOREIGN KEY (id) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpsproc_provider FOREIGN KEY (id_provider) REFERENCES wpsprovider(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'WPS process offerings';
-- CHECKPOINT C-53

/*****************************************************************************/

CREATE TABLE scheduler (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    id_usr int unsigned NOT NULL COMMENT 'FK: Owning user',
    identifier varchar(100) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Caption',
    has_tasks boolean NOT NULL DEFAULT false COMMENT 'True if scheduler triggers persistent runs (tasks)',
    max_runs smallint NOT NULL DEFAULT 1 COMMENT 'Maximum number of runs per scheduling cycle; 0: all',
    no_fail boolean COMMENT 'Interrupt execution if a run has failed',
    id_class int unsigned COMMENT 'FK: Scheduler class',
    status tinyint unsigned COMMENT 'Most recent status',
    last_message text COMMENT 'Last status message',
    CONSTRAINT pk_scheduler PRIMARY KEY (id),
    UNIQUE INDEX (identifier),
    CONSTRAINT fk_scheduler_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    CONSTRAINT fk_scheduler_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_scheduler_class FOREIGN KEY (id_class) REFERENCES schedulerclass(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Schedulers';
-- CHECKPOINT C-54

/*****************************************************************************/

CREATE TABLE schedulertaskconf (
    id int unsigned NOT NULL,
    id_service int unsigned COMMENT 'FK: Scheduled service',
    priority float COMMENT 'Priority value',
    compression VARCHAR(10) COMMENT 'Compression value',
    custom_url varchar(200) COMMENT 'Task definition URL (used if id_service is NULL)',
    id_cr int unsigned COMMENT 'FK: Computing resource',
    max_cr_usage tinyint NOT NULL DEFAULT 0 COMMENT 'Maximum usage (in %) of computing resource for tasks of the scheduler',
    ignore_cr_load boolean COMMENT 'If true, scheduler is supposed to ignore the existing load on the computing resource',
    id_pubserver int unsigned COMMENT 'FK: Publish server',
    CONSTRAINT pk_schedulertaskconf PRIMARY KEY (id),
    CONSTRAINT fk_schedulertaskconf_scheduler FOREIGN KEY (id) REFERENCES scheduler(id) ON DELETE CASCADE,
    CONSTRAINT fk_schedulertaskconf_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_schedulertaskconf_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE SET NULL,
    CONSTRAINT fk_schedulertaskconf_pubserver FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Task configurations for schedulers';
-- CHECKPOINT C-55

/*****************************************************************************/

CREATE TABLE schedulerrunconf (
    id int unsigned NOT NULL,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    CONSTRAINT pk_schedulerrunconf PRIMARY KEY (id),
    CONSTRAINT fk_schedulerrunconf_scheduler FOREIGN KEY (id) REFERENCES scheduler(id) ON DELETE CASCADE,
    CONSTRAINT fk_schedulerrunconf_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Run configurations for schedulers';
-- CHECKPOINT C-56

/*****************************************************************************/

CREATE TABLE timeschedulerrunconf (
    id int unsigned NOT NULL,
    validity_start datetime COMMENT 'Validity start date/time',
    validity_end datetime COMMENT 'Validity end date/time',
    time_interval varchar(10) COMMENT 'Time interval length (time-driven)',
    shifting boolean COMMENT 'Shift selection period',
    past_only boolean COMMENT 'Keep execution date in past',
    ref_time datetime COMMENT 'Last date/time processed (between validity start and end)',
    CONSTRAINT pk_timeschedulerrunconf PRIMARY KEY (id),
    CONSTRAINT fk_timeschedulerrunconf_schedulerrunconf FOREIGN KEY (id) REFERENCES schedulerrunconf(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Time-driven run configurations';
-- CHECKPOINT C-57

/*****************************************************************************/

CREATE TABLE dataschedulerrunconf (
    id int unsigned NOT NULL,
    validity_start datetime COMMENT 'Validity start date/time',
    validity_end datetime COMMENT 'Validity end date/time',
    back_dir boolean COMMENT 'If true, moving backward in time (data driven)',
    min_files smallint unsigned NOT NULL DEFAULT 1 COMMENT 'Minimum number of files to process (data-driven)',
    max_files smallint unsigned NOT NULL DEFAULT 1 COMMENT 'Maximum number of files to process (data-driven)',
    last_time datetime COMMENT 'Last product date/time (no ms in MySQL datetime type)',
    last_time_ms smallint COMMENT 'ms part of last product date/time',
    last_identifier varchar(100) COMMENT 'Last processed product file identifier',
    CONSTRAINT pk_dataschedulerrunconf PRIMARY KEY (id),
    CONSTRAINT fk_dataschedulerrunconf_schedulerrunconf FOREIGN KEY (id) REFERENCES schedulerrunconf(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Data-driven run configurations';
-- CHECKPOINT C-58

/*****************************************************************************/

CREATE TABLE schedulerparam (
    id_scheduler int unsigned NOT NULL COMMENT 'FK: Concerned scheduler',
    name varchar(50) NOT NULL COMMENT 'Parameter name',
    type varchar(25) COMMENT 'Type identifier',
    value text COMMENT 'Parameter value',
    CONSTRAINT fk_schedulerparam_scheduler FOREIGN KEY (id_scheduler) REFERENCES scheduler(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Parameters of task schedulers';
-- CHECKPOINT C-59

/*****************************************************************************/

CREATE TABLE taskgroup (
    id int unsigned NOT NULL auto_increment,
    id_application int unsigned NOT NULL COMMENT 'FK: Application that created the task group',
    template varchar(50) NOT NULL COMMENT 'Name of template used at task group creation',
    one_task boolean COMMENT 'If true, task group has only one task',
    token varchar(50) NOT NULL COMMENT 'Unique random ID for external access',
    requests int unsigned NOT NULL DEFAULT 0 COMMENT 'Number of result requests received',
    CONSTRAINT pk_taskgroup PRIMARY KEY (id),
    UNIQUE INDEX (token),
    CONSTRAINT fk_taskgroup_application FOREIGN KEY (id_application) REFERENCES application(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Groups of externally related tasks';
-- CHECKPOINT C-60

/*****************************************************************************/

CREATE TABLE task (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned COMMENT 'FK: Owning user',
    id_service int unsigned COMMENT 'FK: Service',
    id_cr int unsigned COMMENT 'FK: Computing resource',
    id_pubserver int unsigned COMMENT 'FK: Publish server',
    id_scheduler int unsigned COMMENT 'FK: Related service scheduler (optional)',
    id_taskgroup int unsigned COMMENT 'FK: Related task group (optional)',
    identifier varchar(50) COMMENT 'Unique identifier (automatically generated UID)',
    name varchar(200) NOT NULL COMMENT 'Name or caption',
    priority float COMMENT 'Priority value',
    resources double COMMENT 'Consumed resources',
    compression VARCHAR(10) COMMENT 'Compression value',
    auto_register boolean COMMENT 'If true, automatic registration at task completion is desired',
    retry_period smallint COMMENT 'Retry period (in min) after first pending submission',
    status tinyint unsigned COMMENT 'Most recent status',
    async_op tinyint unsigned COMMENT 'Requested asynchronous operation',
    message_code tinyint unsigned COMMENT 'Code of error/warning message',
    message_text varchar(255) COMMENT 'Text of error/warning message',
    empty boolean NOT NULL DEFAULT false COMMENT 'Task has no input files',
    remote_id varchar(50) COMMENT 'Remote identifier',
    status_url varchar(200) COMMENT 'Status URL if CR provides them explicitely (e.g. WPS)',
    creation_time datetime NOT NULL COMMENT 'Date/time of task creation',
    scheduled_time datetime COMMENT 'Date/time of scheduled execution',
    start_time datetime COMMENT 'Date/time of submission',
    end_time datetime COMMENT 'Date/time of completion or failure',
    access_time datetime COMMENT 'Date/time of last access',
    CONSTRAINT pk_task PRIMARY KEY (id),
    UNIQUE INDEX (identifier),
    CONSTRAINT fk_task_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_task_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_pubserver FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_scheduler FOREIGN KEY (id_scheduler) REFERENCES scheduler(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_taskgroup FOREIGN KEY (id_taskgroup) REFERENCES taskgroup(id) ON DELETE SET NULL
) Engine=InnoDB COMMENT 'Processing tasks';
-- CHECKPOINT C-61

/*****************************************************************************/

CREATE TABLE temporaltask (
    id_task int unsigned NOT NULL COMMENT 'FK: Concerned task',
    start_time datetime COMMENT 'Start date/time of selection period',
    end_time datetime COMMENT 'End date/time of selection period',
    CONSTRAINT pk_temporaltask PRIMARY KEY (id_task),
    CONSTRAINT fk_temporaltask_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Temporal parameters of processing tasks';
-- CHECKPOINT C-62

/*****************************************************************************/

CREATE TABLE taskpart (
    id int unsigned NOT NULL auto_increment,
    id_task int unsigned NOT NULL COMMENT 'FK: Containing task',
    name varchar(50) NOT NULL COMMENT 'Unique part identifier',
    CONSTRAINT pk_taskpart PRIMARY KEY (id),
    CONSTRAINT fk_taskpart_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Externally related task parts';
-- CHECKPOINT C-63

/*****************************************************************************/

CREATE TABLE job (
    id int unsigned NOT NULL auto_increment,
    id_task int unsigned NOT NULL COMMENT 'FK: Owning task',
    name varchar(50) NOT NULL COMMENT 'Job name',
    job_type varchar(50) COMMENT 'Job type (as defined in grid engine)',
    max_nodes int COMMENT 'Maximum number of nodes',
    min_args int COMMENT 'Minimum number of arguments per node',
    publish boolean COMMENT 'Job is publishing job',
    forced_exit boolean COMMENT 'Execution is interrupted after job environment creation',
    status tinyint unsigned COMMENT 'Most recent status',
    async_op tinyint unsigned COMMENT 'Requested asynchronous operation',
    seq_exec_id varchar(25) COMMENT 'Sequential execution identifier',
    start_time datetime COMMENT 'Date/time of submission',
    end_time datetime COMMENT 'Date/time of completion or failure',
    CONSTRAINT pk_job PRIMARY KEY (id),
    CONSTRAINT fk_job_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Processing jobs';
-- CHECKPOINT C-64

/*****************************************************************************/

CREATE TABLE jobnode (
    id_job int unsigned NOT NULL COMMENT 'FK: Initiating job',
    pid int unsigned NOT NULL COMMENT 'PID',
    status tinyint unsigned COMMENT 'Most recent status',
    result_size int unsigned COMMENT 'Size of output',
    result_size_unit char(1) COMMENT 'Unit of output size (K: KByte, M: MByte)',
    arg_total int unsigned COMMENT 'Total number of input arguments',
    arg_done int unsigned COMMENT 'Number of processed input arguments',
    hostname varchar(200) COMMENT 'Node hostname',
    start_time datetime COMMENT 'Date/time of processing start',
    end_time datetime COMMENT 'Date/time of end',
    CONSTRAINT pk_jobnode PRIMARY KEY (id_job, pid),
    CONSTRAINT fk_jobnode_job FOREIGN KEY (id_job) REFERENCES job(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Job processings on a computing resource node';
-- CHECKPOINT C-65

/*****************************************************************************/

CREATE TABLE jobdependency (
    id_job int unsigned NOT NULL COMMENT 'FK: Job that depends on another job',
    id_job_input int unsigned NOT NULL COMMENT 'FK: Job that is input for the other job',
    CONSTRAINT pk_jobdependency PRIMARY KEY (id_job, id_job_input),
    INDEX (id_job DESC),
    CONSTRAINT fk_jobdependency_job FOREIGN KEY (id_job) REFERENCES job(id) ON DELETE CASCADE,
    CONSTRAINT fk_jobdependency_job_input FOREIGN KEY (id_job_input) REFERENCES job(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Processing job dependencies';
-- CHECKPOINT C-66

/*****************************************************************************/

CREATE TABLE taskparam (
    id_task int unsigned NOT NULL COMMENT 'FK: Concerned task',
    id_job int unsigned COMMENT 'FK: Concerned job (NULL for task parameter)',
    name varchar(50) NOT NULL COMMENT 'Parameter name',
    type varchar(25) COMMENT 'Type identifier',
    value mediumtext COMMENT 'Parameter value',
    metadata boolean COMMENT 'If true, parameter is relevant metadata for users',
    INDEX (id_task DESC, name),
    INDEX (id_job DESC, name),
    CONSTRAINT fk_taskparam_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE,
    CONSTRAINT fk_taskparam_job FOREIGN KEY (id_job) REFERENCES job(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Parameters of processing tasks and jobs';
-- CHECKPOINT C-67

/*****************************************************************************/

CREATE TABLE wpstask (
    id_task int unsigned NOT NULL COMMENT 'FK: Related task',
    id_application int unsigned COMMENT 'FK: WPS application',
    template_name varchar(50) NOT NULL COMMENT 'Task template name',
    store boolean COMMENT 'Value of "storeExecuteResponse" attribute',
    status boolean COMMENT 'Value of "status" attribute',
    lineage_xml text COMMENT 'XML to be included in response if "lineage" attribute was "true"',
    ref_output boolean COMMENT 'Value of "asReference" attribute',
    xsl_name varchar(50) COMMENT 'Name of XSL for output transformation',
    CONSTRAINT pk_wpstask PRIMARY KEY (id_task),
    CONSTRAINT fk_wpstask_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpstask_application FOREIGN KEY (id_application) REFERENCES application(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Additional fields for tasks created by a WPS';
-- CHECKPOINT C-68

/*****************************************************************************/

CREATE TABLE log (
    id int unsigned NOT NULL auto_increment,
    log_time datetime NOT NULL,
    thread varchar(255) NOT NULL,
    level varchar(20) NOT NULL,
    logger varchar(255) NOT NULL,
    message varchar(4000) NOT NULL,
    post_parameters text,
    originator varchar(255),
    user varchar(255),
    url varchar(255),
    action varchar(255),
    CONSTRAINT pk_log PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'System log entries';
-- CHECKPOINT C-69

/*****************************************************************************/

CREATE TABLE safe (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    public_key varchar(10000) COMMENT 'Public key',
    private_key varchar(10000) COMMENT 'Private key',
    creation_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of safe creation',
    update_time datetime COMMENT 'Date/time of safe creation',
    CONSTRAINT pk_safe PRIMARY KEY (id),
    CONSTRAINT fk_safe_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Sets of safes';
-- CHECKPOINT C-70

/*****************************************************************************/

CREATE TABLE safe_perm (
    id_safe int unsigned NOT NULL COMMENT 'FK: Safe',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_safe_perm_safe FOREIGN KEY (id_safe) REFERENCES safe(id) ON DELETE CASCADE,
    CONSTRAINT fk_safe_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_safe_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on safes';
-- CHECKPOINT C-71

/*****************************************************************************/

CREATE TABLE activity (
    id int unsigned NOT NULL auto_increment,
    id_entity int unsigned COMMENT 'Entity associated to the activity',
    identifier_entity varchar(50) COMMENT 'Identifier associated to the activity',
    id_usr int unsigned COMMENT 'User doing the activity',
    id_priv int unsigned COMMENT 'Privilege associated',
    id_type int unsigned COMMENT 'Entity type',
    id_owner int unsigned COMMENT 'User owning the entity related to the activity',
    id_domain int unsigned COMMENT 'Domain of the activity',
    log_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of activity creation',
    CONSTRAINT pk_activity PRIMARY KEY (id),
    INDEX (id_usr),
    INDEX (log_time)
) Engine=InnoDB COMMENT 'User activities';
-- CHECKPOINT C-72

CREATE TABLE priv_score (
    id_priv int unsigned COMMENT 'FK: Privilege associated',
    score_usr int unsigned COMMENT 'Score associated to a user',
    score_owner int unsigned COMMENT 'Score associated to an owner',
    CONSTRAINT fk_priv_score_priv FOREIGN KEY (id_priv) REFERENCES priv(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Privilege scores';
-- CHECKPOINT C-73


CREATE TABLE cookie (
    session VARCHAR(100) NOT NULL COMMENT 'Session',
    identifier VARCHAR(100) NOT NULL COMMENT 'Identifier',
    value TEXT NULL COMMENT 'Value',
    expire datetime,    
    creation_date datetime,
    UNIQUE INDEX (session,identifier)
) Engine=InnoDB COMMENT 'DB Cookies';
-- RESULT 
-- CHECKPOINT C-74

/*****************************************************************************/

USE $NEWS$;

/*****************************************************************************/

CREATE TABLE feature (
    id int unsigned NOT NULL auto_increment,
    pos int unsigned COMMENT 'Feature position',
    title varchar(40) NOT NULL,
    description varchar(200),
    image_url varchar(100),
    image_style varchar(100),
    image_credits VARCHAR(150) NULL DEFAULT NULL,
    button_text varchar(15),
    button_link varchar(1000),
    PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Web portal featured content';
-- CHECKPOINT CN-01

/*****************************************************************************/

CREATE TABLE article (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(25) NOT NULL COMMENT 'Unique identifier',
    title varchar(200) NOT NULL COMMENT 'Article title',
    abstract text COMMENT 'Abstract',
    content text COMMENT 'Full content',
    time datetime COMMENT 'Publication date/time',
    url varchar(200) COMMENT 'External URL',
    author varchar(100) COMMENT 'Author name',
    author_img varchar(100) COMMENT 'Author image',
    tags varchar(100) COMMENT 'Descriptive tags',
    id_type int unsigned COMMENT 'FK: Entity type',
    CONSTRAINT pk_article PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'News articles';
-- CHECKPOINT CN-02

/*****************************************************************************/

CREATE TABLE articlecomment (
    id int unsigned NOT NULL auto_increment,
    id_article int unsigned NOT NULL COMMENT 'FK: Related news article',
    author varchar(50) NOT NULL COMMENT 'Commentator name',
    email varchar(100) COMMENT 'Commentator email address',
    country varchar(50) COMMENT 'Commentator country',
    ip varchar(20) COMMENT 'Commentator IP address',
    time datetime COMMENT 'Publication date/time',
    comments text COMMENT 'Comment',
    CONSTRAINT pk_articlecomment PRIMARY KEY (id),
    CONSTRAINT fk_articlecomment_article FOREIGN KEY (id_article) REFERENCES article(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Comments on news articles';
-- CHECKPOINT CN-03

/*****************************************************************************/

CREATE TABLE image (
    id int unsigned NOT NULL auto_increment,
    caption varchar(100) NOT NULL COMMENT 'Image caption',
    description text COMMENT 'Description',
    url varchar(200) COMMENT 'Image URL',
    small_url varchar(200) COMMENT 'Image preview URL',
    CONSTRAINT pk_image PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Images';
-- CHECKPOINT CN-04

/*****************************************************************************/

CREATE TABLE faq (
    id int unsigned NOT NULL auto_increment,
    question varchar(200) NOT NULL COMMENT 'Question',
    answer text COMMENT 'Answer to question',
    CONSTRAINT pk_faq PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Frequently asked questions';
-- CHECKPOINT CN-05

/*****************************************************************************/

CREATE TABLE project (
    id int unsigned NOT NULL auto_increment,
    title varchar(200) NOT NULL COMMENT 'Project title',
    short_description text COMMENT 'Short description',
    long_description text COMMENT 'Long description',
    contact varchar(100) COMMENT 'Contact information',
    status tinyint NOT NULL DEFAULT 0 COMMENT 'Project status (1 to 4)',
    CONSTRAINT pk_project PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Projects';
-- CHECKPOINT CN-06

/*****************************************************************************/

-- Create table gitrepo ... \
CREATE TABLE gitrepo (
    id int unsigned NOT NULL auto_increment,
    url VARCHAR(300) NOT NULL COMMENT 'Git Repository URL',
    id_usr int unsigned COMMENT 'User creating the repo',
    id_domain int unsigned COMMENT 'Domain of the repo',
    CONSTRAINT pk_gitrepo PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'GIR repository';
-- CHECKPOINT CN-07

/*****************************************************************************/

-- Create table gitrepo_perm ... \
CREATE TABLE gitrepo_perm (
    id_gitrepo int unsigned NOT NULL COMMENT 'FK: Git repository',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_gitrepo_perm_gitrepo FOREIGN KEY (id_gitrepo) REFERENCES gitrepo(id) ON DELETE CASCADE,
    CONSTRAINT fk_gitrepo_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_gitrepo_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on GIT repository';
-- CHECKPOINT CN-08
