-- VERSION 2.6.35

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
    (5, 5, 'Terradue.Portal.ManagerRole, Terradue.Portal', NULL, 'Manager Role', 'Manager Roles', 'manager-roles'),
    (6, 6, 'Terradue.Portal.OpenIdProvider, Terradue.Portal', NULL, 'OpenID Provider', 'OpenID Providers', 'openid-providers'),
    (7, 7, 'Terradue.Portal.LookupList, Terradue.Portal', NULL, 'Shared Lookup List', 'Shared Lookup Lists', 'lookup-lists'),
    (8, 8, 'Terradue.Portal.ServiceClass, Terradue.Portal', NULL, 'Service Class', 'Service Classes', 'service-classes'),
    (9, 9, 'Terradue.Portal.ServiceCategory, Terradue.Portal', NULL, 'Service Category', 'Service Categories', 'service-categories'),
    (10, 10, 'Terradue.Portal.SchedulerClass, Terradue.Portal', NULL, 'Scheduler Class', 'Scheduler Classes', 'scheduler-classes'),
    (11, 11, 'Terradue.Portal.User, Terradue.Portal', NULL, 'User', 'Users', 'users'),
    (12, 12, 'Terradue.Portal.Group, Terradue.Portal', NULL, 'Group', 'Groups', 'groups'),
    (13, 13, 'Terradue.Portal.LightGridEngine, Terradue.Portal', NULL, 'LGE Instance', 'LGE Instances', 'lge-instances'),
    (14, 14, 'Terradue.Portal.ComputingResource, Terradue.Portal', NULL, 'Computing Resource', 'Computing Resources', 'computing-resources'),
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
    (26, 26, 'Terradue.Portal.Project, Terradue.Portal', NULL, 'Project', 'Projects', 'projects')
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
    INSERT INTO type (id_module, id_super, pos, class, caption_sg, caption_pl, keyword) VALUES (p_module_id, type_id, type_pos, p_class, p_caption_sg, p_caption_pl, p_keyword);
END;
-- CHECKPOINT C-01d

/*****************************************************************************/

CREATE TABLE priv (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned COMMENT 'FK: Entity type',
    operation char(1) NOT NULL COLLATE latin1_general_cs COMMENT 'Operation type (one-letter code: c|m|a|p|d|o|V|A)',
    pos smallint unsigned COMMENT 'Position for ordering',
    name varchar(50) NOT NULL,
    enable_log boolean NOT NULL default false COMMENT 'If true, activity related to this privilege are logged',
    CONSTRAINT pk_priv PRIMARY KEY (id),
    CONSTRAINT fk_priv_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Manager privileges';
-- CHECKPOINT C-02a

-- Initializing manager privileges ... \
INSERT INTO priv (id_type, operation, pos, name) VALUES
    (11, 'v', 1, 'User: view'),
    (11, 'c', 2, 'User: create'),
    (11, 'm', 3, 'User: change'),
    (11, 'p', 4, 'User: make public'),
    (11, 'd', 5, 'User: delete'),
    (11, 'V', 6, 'User: view related'),
    (11, 'A', 7, 'User: assign global'),
    (12, 'v', 8, 'Group: view'),
    (12, 'c', 9, 'Group: create'),
    (12, 'm', 10, 'Group: change'),
    (12, 'p', 11, 'Group: make public'),
    (12, 'd', 12, 'Group: delete'),
    (12, 'V', 13, 'Group: view public'),
    (12, 'A', 14, 'Group: assign public'),
    (14, 'v', 15, 'Computing resource: view'),
    (14, 'c', 16, 'Computing resource: create'),
    (14, 'm', 17, 'Computing resource: change'),
    (14, 'p', 18, 'Computing resource: make public'),
    (14, 'd', 19, 'Computing resource: delete'),
    (14, 'V', 20, 'Computing resource: view public'),
    (14, 'A', 21, 'Computing resource: assign public'),
    (15, 'v', 22, 'Catalogue: view'),
    (15, 'c', 23, 'Catalogue: create'),
    (15, 'm', 24, 'Catalogue: change'),
    (15, 'p', 25, 'Catalogue: make public'),
    (15, 'd', 26, 'Catalogue: delete'),
    (15, 'V', 27, 'Catalogue: view public'),
    (15, 'A', 28, 'Catalogue: assign public'),
    (16, 'v', 29, 'Series: view'),
    (16, 'c', 30, 'Series: create'),
    (16, 'm', 31, 'Series: change'),
    (16, 'p', 32, 'Series: make public'),
    (16, 'd', 33, 'Series: delete'),
    (16, 'V', 34, 'Series: view public'),
    (16, 'A', 35, 'Series: assign public'),
    (17, 'v', 36, 'Product type: view'),
    (17, 'c', 37, 'Product type: create'),
    (17, 'm', 38, 'Product type: change'),
    (17, 'p', 39, 'Product type: make public'),
    (17, 'd', 40, 'Product type: delete'),
    (17, 'V', 41, 'Product type: view public'),
    (17, 'A', 42, 'Product type: assign public'),
    (18, 'v', 43, 'Publish server: view'),
    (18, 'c', 44, 'Publish server: create'),
    (18, 'm', 45, 'Publish server: change'),
    (18, 'p', 46, 'Publish server: make public'),
    (18, 'd', 47, 'Publish server: delete'),
    (18, 'V', 48, 'Publish server: view public'),
    (18, 'A', 49, 'Publish server: assign public'),
    (19, 'v', 50, 'Service: view'),
    (19, 'c', 51, 'Service: create'),
    (19, 'm', 52, 'Service: change'),
    (19, 'p', 53, 'Service: make public'),
    (19, 'd', 54, 'Service: delete'),
    (19, 'V', 55, 'Service: view public'),
    (19, 'A', 56, 'Service: assign public'),
    (20, 'm', 57, 'Scheduler: control'),
    (21, 'm', 58, 'Task: control'),
    (22, 'v', 59, 'Article: view'),
    (22, 'c', 60, 'Article: create'),
    (22, 'm', 61, 'Article: change'),
    (22, 'd', 62, 'Article: delete'),
    (23, 'v', 63, 'Image: view'),
    (23, 'c', 64, 'Image: create'),
    (23, 'm', 65, 'Image: change'),
    (23, 'd', 66, 'Image: delete'),
    (24, 'v', 67, 'FAQ: view'),
    (24, 'c', 68, 'FAQ: create'),
    (24, 'm', 69, 'FAQ: change'),
    (24, 'd', 70, 'FAQ: delete'),
    (25, 'v', 71, 'Project: view'),
    (25, 'c', 72, 'Project: create'),
    (25, 'm', 73, 'Project: change'),
    (25, 'd', 74, 'Project: delete')
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
    name varchar(100) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    CONSTRAINT pk_domain PRIMARY KEY (id),
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Domains';
-- CHECKPOINT C-07

/*****************************************************************************/

CREATE TABLE role (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Managed domain',
    name varchar(100) NOT NULL COMMENT 'Unique name',
    description text COMMENT 'Description',
    count INT NULL COMMENT 'number of products',
    CONSTRAINT pk_role PRIMARY KEY (id),
    CONSTRAINT fk_role_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL,
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Manager roles';
-- CHECKPOINT C-08

/*****************************************************************************/

CREATE TABLE role_priv (
    id_role int unsigned NOT NULL COMMENT 'FK: Manager role',
    id_priv int unsigned NOT NULL COMMENT 'FK: Manager privilege',
    CONSTRAINT pk_role_priv PRIMARY KEY (id_role, id_priv),
    CONSTRAINT fk_role_priv_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_priv_priv FOREIGN KEY (id_priv) REFERENCES priv(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of manager privileges to roles';
-- CHECKPOINT C-09

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
    (4, true, 'language'),
    (5, true, 'timeZone'),
    (6, true, 'logLevel'),
    (7, true, 'debugLevel'),
    (8, true, 'taskSubmitRetry'),
    (9, true, 'taskStatusRequest'),
    (10, true, 'ceStatusRequest'),
    (11, true, 'protocol'),
    (12, true, 'rating'),
    (13, true, 'rule'),
    (14, true, 'userActRule'),
    (15, true, 'openIdRule'),
    (16, true, 'clientCertVerifyRule')
;
-- RESULT
-- CHECKPOINT C-11b

/*****************************************************************************/

CREATE TABLE lookup (
    id_list smallint unsigned NOT NULL COMMENT 'FK: Lookup list',
    pos smallint unsigned COMMENT 'Position for ordering',
    caption varchar(50) NOT NULL COMMENT 'Caption for value',
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
    (3, 5, 'All authorized users', '4'),
    (4, 1, 'English', 'en'),
    (4, 2, 'French (not supported yet)', 'fr'),
    (4, 3, 'German (not supported yet)', 'de')
;
-- CHECKPOINT C-12b

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 0, '[+00:00] Coordinated Universal Time (UTC)', 'UTC'),
    (5, 1, '[-11:00] Pacific/Apia', 'Pacific/Apia'),
    (5, 2, '[-11:00] Pacific/Midway', 'Pacific/Midway'),
    (5, 3, '[-11:00] Pacific/Niue', 'Pacific/Niue'),
    (5, 4, '[-11:00] Pacific/Pago_Pago', 'Pacific/Pago_Pago'),
    (5, 5, '[-11:00] Pacific/Samoa', 'Pacific/Samoa'),
    (5, 6, '[-11:00] US/Samoa', 'US/Samoa'),
    (5, 7, '[-10:00] America/Adak', 'America/Adak'),
    (5, 8, '[-10:00] America/Atka', 'America/Atka'),
    (5, 9, '[-10:00] Pacific/Fakaofo', 'Pacific/Fakaofo'),
    (5, 10, '[-10:00] Pacific/Honolulu', 'Pacific/Honolulu'),
    (5, 11, '[-10:00] Pacific/Johnston', 'Pacific/Johnston'),
    (5, 12, '[-10:00] Pacific/Rarotonga', 'Pacific/Rarotonga'),
    (5, 13, '[-10:00] Pacific/Tahiti', 'Pacific/Tahiti'),
    (5, 14, '[-10:00] US/Aleutian', 'US/Aleutian'),
    (5, 15, '[-10:00] US/Hawaii', 'US/Hawaii'),
    (5, 16, '[-09:30] Pacific/Marquesas', 'Pacific/Marquesas'),
    (5, 17, '[-09:00] America/Anchorage', 'America/Anchorage'),
    (5, 18, '[-09:00] America/Juneau', 'America/Juneau'),
    (5, 19, '[-09:00] America/Nome', 'America/Nome'),
    (5, 20, '[-09:00] America/Yakutat', 'America/Yakutat'),
    (5, 21, '[-09:00] Pacific/Gambier', 'Pacific/Gambier'),
    (5, 22, '[-09:00] US/Alaska', 'US/Alaska'),
    (5, 23, '[-08:00] America/Dawson', 'America/Dawson'),
    (5, 24, '[-08:00] America/Ensenada', 'America/Ensenada'),
    (5, 25, '[-08:00] America/Los_Angeles', 'America/Los_Angeles'),
    (5, 26, '[-08:00] America/Tijuana', 'America/Tijuana'),
    (5, 27, '[-08:00] America/Vancouver', 'America/Vancouver'),
    (5, 28, '[-08:00] America/Whitehorse', 'America/Whitehorse'),
    (5, 29, '[-08:00] Canada/Pacific', 'Canada/Pacific'),
    (5, 30, '[-08:00] Canada/Yukon', 'Canada/Yukon'),
    (5, 31, '[-08:00] Mexico/BajaNorte', 'Mexico/BajaNorte'),
    (5, 32, '[-08:00] Pacific/Pitcairn', 'Pacific/Pitcairn'),
    (5, 33, '[-08:00] US/Pacific', 'US/Pacific'),
    (5, 34, '[-07:00] America/Boise', 'America/Boise'),
    (5, 35, '[-07:00] America/Cambridge_Bay', 'America/Cambridge_Bay'),
    (5, 36, '[-07:00] America/Chihuahua', 'America/Chihuahua'),
    (5, 37, '[-07:00] America/Dawson_Creek', 'America/Dawson_Creek'),
    (5, 38, '[-07:00] America/Denver', 'America/Denver'),
    (5, 39, '[-07:00] America/Edmonton', 'America/Edmonton'),
    (5, 40, '[-07:00] America/Hermosillo', 'America/Hermosillo'),
    (5, 41, '[-07:00] America/Inuvik', 'America/Inuvik'),
    (5, 42, '[-07:00] America/Mazatlan', 'America/Mazatlan'),
    (5, 43, '[-07:00] America/Phoenix', 'America/Phoenix'),
    (5, 44, '[-07:00] America/Shiprock', 'America/Shiprock'),
    (5, 45, '[-07:00] America/Yellowknife', 'America/Yellowknife'),
    (5, 46, '[-07:00] Canada/Mountain', 'Canada/Mountain'),
    (5, 47, '[-07:00] Mexico/BajaSur', 'Mexico/BajaSur'),
    (5, 48, '[-07:00] US/Arizona', 'US/Arizona'),
    (5, 49, '[-07:00] US/Mountain', 'US/Mountain')
;
-- CHECKPOINT C-12c

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 50, '[-06:00] America/Belize', 'America/Belize'),
    (5, 51, '[-06:00] America/Cancun', 'America/Cancun'),
    (5, 52, '[-06:00] America/Chicago', 'America/Chicago'),
    (5, 53, '[-06:00] America/Costa_Rica', 'America/Costa_Rica'),
    (5, 54, '[-06:00] America/El_Salvador', 'America/El_Salvador'),
    (5, 55, '[-06:00] America/Guatemala', 'America/Guatemala'),
    (5, 56, '[-06:00] America/Knox_IN', 'America/Knox_IN'),
    (5, 57, '[-06:00] America/Managua', 'America/Managua'),
    (5, 58, '[-06:00] America/Menominee', 'America/Menominee'),
    (5, 59, '[-06:00] America/Merida', 'America/Merida'),
    (5, 60, '[-06:00] America/Mexico_City', 'America/Mexico_City'),
    (5, 61, '[-06:00] America/Monterrey', 'America/Monterrey'),
    (5, 62, '[-06:00] America/Rainy_River', 'America/Rainy_River'),
    (5, 63, '[-06:00] America/Rankin_Inlet', 'America/Rankin_Inlet'),
    (5, 64, '[-06:00] America/Regina', 'America/Regina'),
    (5, 65, '[-06:00] America/Swift_Current', 'America/Swift_Current'),
    (5, 66, '[-06:00] America/Tegucigalpa', 'America/Tegucigalpa'),
    (5, 67, '[-06:00] America/Winnipeg', 'America/Winnipeg'),
    (5, 68, '[-06:00] Canada/Central', 'Canada/Central'),
    (5, 69, '[-06:00] Canada/East-Saskatchewan', 'Canada/East-Saskatchewan'),
    (5, 70, '[-06:00] Canada/Saskatchewan', 'Canada/Saskatchewan'),
    (5, 71, '[-06:00] Chile/EasterIsland', 'Chile/EasterIsland'),
    (5, 72, '[-06:00] Mexico/General', 'Mexico/General'),
    (5, 73, '[-06:00] Pacific/Easter', 'Pacific/Easter'),
    (5, 74, '[-06:00] Pacific/Galapagos', 'Pacific/Galapagos'),
    (5, 75, '[-06:00] US/Central', 'US/Central'),
    (5, 76, '[-06:00] US/Indiana-Starke', 'US/Indiana-Starke'),
    (5, 77, '[-05:00] America/Atikokan', 'America/Atikokan'),
    (5, 78, '[-05:00] America/Bogota', 'America/Bogota'),
    (5, 79, '[-05:00] America/Cayman', 'America/Cayman'),
    (5, 80, '[-05:00] America/Coral_Harbour', 'America/Coral_Harbour'),
    (5, 81, '[-05:00] America/Detroit', 'America/Detroit'),
    (5, 82, '[-05:00] America/Fort_Wayne', 'America/Fort_Wayne'),
    (5, 83, '[-05:00] America/Grand_Turk', 'America/Grand_Turk'),
    (5, 84, '[-05:00] America/Guayaquil', 'America/Guayaquil'),
    (5, 85, '[-05:00] America/Havana', 'America/Havana'),
    (5, 86, '[-05:00] America/Indianapolis', 'America/Indianapolis'),
    (5, 87, '[-05:00] America/Iqaluit', 'America/Iqaluit'),
    (5, 88, '[-05:00] America/Jamaica', 'America/Jamaica'),
    (5, 89, '[-05:00] America/Lima', 'America/Lima'),
    (5, 90, '[-05:00] America/Louisville', 'America/Louisville'),
    (5, 91, '[-05:00] America/Montreal', 'America/Montreal'),
    (5, 92, '[-05:00] America/Nassau', 'America/Nassau'),
    (5, 93, '[-05:00] America/New_York', 'America/New_York'),
    (5, 94, '[-05:00] America/Nipigon', 'America/Nipigon'),
    (5, 95, '[-05:00] America/Panama', 'America/Panama'),
    (5, 96, '[-05:00] America/Pangnirtung', 'America/Pangnirtung'),
    (5, 97, '[-05:00] America/Port-au-Prince', 'America/Port-au-Prince'),
    (5, 98, '[-05:00] America/Resolute', 'America/Resolute'),
    (5, 99, '[-05:00] America/Thunder_Bay', 'America/Thunder_Bay')
;
-- CHECKPOINT C-12d

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 100, '[-05:00] America/Toronto', 'America/Toronto'),
    (5, 101, '[-05:00] Canada/Eastern', 'Canada/Eastern'),
    (5, 102, '[-05:00] US/Eastern', 'US/Eastern'),
    (5, 103, '[-05:00] US/East-Indiana', 'US/East-Indiana'),
    (5, 104, '[-05:00] US/Michigan', 'US/Michigan'),
    (5, 105, '[-04:30] America/Caracas', 'America/Caracas'),
    (5, 106, '[-04:00] America/Anguilla', 'America/Anguilla'),
    (5, 107, '[-04:00] America/Antigua', 'America/Antigua'),
    (5, 108, '[-04:00] America/Aruba', 'America/Aruba'),
    (5, 109, '[-04:00] America/Asuncion', 'America/Asuncion'),
    (5, 110, '[-04:00] America/Barbados', 'America/Barbados'),
    (5, 111, '[-04:00] America/Blanc-Sablon', 'America/Blanc-Sablon'),
    (5, 112, '[-04:00] America/Boa_Vista', 'America/Boa_Vista'),
    (5, 113, '[-04:00] America/Campo_Grande', 'America/Campo_Grande'),
    (5, 114, '[-04:00] America/Cuiaba', 'America/Cuiaba'),
    (5, 115, '[-04:00] America/Curacao', 'America/Curacao'),
    (5, 116, '[-04:00] America/Dominica', 'America/Dominica'),
    (5, 117, '[-04:00] America/Eirunepe', 'America/Eirunepe'),
    (5, 118, '[-04:00] America/Glace_Bay', 'America/Glace_Bay'),
    (5, 119, '[-04:00] America/Goose_Bay', 'America/Goose_Bay'),
    (5, 120, '[-04:00] America/Grenada', 'America/Grenada'),
    (5, 121, '[-04:00] America/Guadeloupe', 'America/Guadeloupe'),
    (5, 122, '[-04:00] America/Guyana', 'America/Guyana'),
    (5, 123, '[-04:00] America/Halifax', 'America/Halifax'),
    (5, 124, '[-04:00] America/La_Paz', 'America/La_Paz'),
    (5, 125, '[-04:00] America/Manaus', 'America/Manaus'),
    (5, 126, '[-04:00] America/Marigot', 'America/Marigot'),
    (5, 127, '[-04:00] America/Martinique', 'America/Martinique'),
    (5, 128, '[-04:00] America/Moncton', 'America/Moncton'),
    (5, 129, '[-04:00] America/Montserrat', 'America/Montserrat'),
    (5, 130, '[-04:00] America/Port_of_Spain', 'America/Port_of_Spain'),
    (5, 131, '[-04:00] America/Porto_Acre', 'America/Porto_Acre'),
    (5, 132, '[-04:00] America/Porto_Velho', 'America/Porto_Velho'),
    (5, 133, '[-04:00] America/Puerto_Rico', 'America/Puerto_Rico'),
    (5, 134, '[-04:00] America/Rio_Branco', 'America/Rio_Branco'),
    (5, 135, '[-04:00] America/Santiago', 'America/Santiago'),
    (5, 136, '[-04:00] America/Santo_Domingo', 'America/Santo_Domingo'),
    (5, 137, '[-04:00] America/St_Barthelemy', 'America/St_Barthelemy'),
    (5, 138, '[-04:00] America/St_Kitts', 'America/St_Kitts'),
    (5, 139, '[-04:00] America/St_Lucia', 'America/St_Lucia'),
    (5, 140, '[-04:00] America/St_Thomas', 'America/St_Thomas'),
    (5, 141, '[-04:00] America/St_Vincent', 'America/St_Vincent'),
    (5, 142, '[-04:00] America/Thule', 'America/Thule'),
    (5, 143, '[-04:00] America/Tortola', 'America/Tortola'),
    (5, 144, '[-04:00] America/Virgin', 'America/Virgin'),
    (5, 145, '[-04:00] Antarctica/Palmer', 'Antarctica/Palmer'),
    (5, 146, '[-04:00] Atlantic/Bermuda', 'Atlantic/Bermuda'),
    (5, 147, '[-04:00] Atlantic/Stanley', 'Atlantic/Stanley'),
    (5, 148, '[-04:00] Brazil/Acre', 'Brazil/Acre'),
    (5, 149, '[-04:00] Brazil/West', 'Brazil/West')
;
-- CHECKPOINT C-12e

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 150, '[-04:00] Canada/Atlantic', 'Canada/Atlantic'),
    (5, 151, '[-04:00] Chile/Continental', 'Chile/Continental'),
    (5, 152, '[-03:30] America/St_Johns', 'America/St_Johns'),
    (5, 153, '[-03:30] Canada/Newfoundland', 'Canada/Newfoundland'),
    (5, 154, '[-03:00] America/Araguaina', 'America/Araguaina'),
    (5, 155, '[-03:00] America/Bahia', 'America/Bahia'),
    (5, 156, '[-03:00] America/Belem', 'America/Belem'),
    (5, 157, '[-03:00] America/Buenos_Aires', 'America/Buenos_Aires'),
    (5, 158, '[-03:00] America/Catamarca', 'America/Catamarca'),
    (5, 159, '[-03:00] America/Cayenne', 'America/Cayenne'),
    (5, 160, '[-03:00] America/Cordoba', 'America/Cordoba'),
    (5, 161, '[-03:00] America/Fortaleza', 'America/Fortaleza'),
    (5, 162, '[-03:00] America/Godthab', 'America/Godthab'),
    (5, 163, '[-03:00] America/Jujuy', 'America/Jujuy'),
    (5, 164, '[-03:00] America/Maceio', 'America/Maceio'),
    (5, 165, '[-03:00] America/Mendoza', 'America/Mendoza'),
    (5, 166, '[-03:00] America/Miquelon', 'America/Miquelon'),
    (5, 167, '[-03:00] America/Montevideo', 'America/Montevideo'),
    (5, 168, '[-03:00] America/Paramaribo', 'America/Paramaribo'),
    (5, 169, '[-03:00] America/Recife', 'America/Recife'),
    (5, 170, '[-03:00] America/Rosario', 'America/Rosario'),
    (5, 171, '[-03:00] America/Santarem', 'America/Santarem'),
    (5, 172, '[-03:00] America/Sao_Paulo', 'America/Sao_Paulo'),
    (5, 173, '[-03:00] Antarctica/Rothera', 'Antarctica/Rothera'),
    (5, 174, '[-03:00] Brazil/East', 'Brazil/East'),
    (5, 175, '[-02:00] America/Noronha', 'America/Noronha'),
    (5, 176, '[-02:00] Atlantic/South_Georgia', 'Atlantic/South_Georgia'),
    (5, 177, '[-02:00] Brazil/DeNoronha', 'Brazil/DeNoronha'),
    (5, 178, '[-01:00] America/Scoresbysund', 'America/Scoresbysund'),
    (5, 179, '[-01:00] Atlantic/Azores', 'Atlantic/Azores'),
    (5, 180, '[-01:00] Atlantic/Cape_Verde', 'Atlantic/Cape_Verde'),
    (5, 181, '[+00:00] Africa/Abidjan', 'Africa/Abidjan'),
    (5, 182, '[+00:00] Africa/Accra', 'Africa/Accra'),
    (5, 183, '[+00:00] Africa/Bamako', 'Africa/Bamako'),
    (5, 184, '[+00:00] Africa/Banjul', 'Africa/Banjul'),
    (5, 185, '[+00:00] Africa/Bissau', 'Africa/Bissau'),
    (5, 186, '[+00:00] Africa/Casablanca', 'Africa/Casablanca'),
    (5, 187, '[+00:00] Africa/Conakry', 'Africa/Conakry'),
    (5, 188, '[+00:00] Africa/Dakar', 'Africa/Dakar'),
    (5, 189, '[+00:00] Africa/El_Aaiun', 'Africa/El_Aaiun'),
    (5, 190, '[+00:00] Africa/Freetown', 'Africa/Freetown'),
    (5, 191, '[+00:00] Africa/Lome', 'Africa/Lome'),
    (5, 192, '[+00:00] Africa/Monrovia', 'Africa/Monrovia'),
    (5, 193, '[+00:00] Africa/Nouakchott', 'Africa/Nouakchott'),
    (5, 194, '[+00:00] Africa/Ouagadougou', 'Africa/Ouagadougou'),
    (5, 195, '[+00:00] Africa/Sao_Tome', 'Africa/Sao_Tome'),
    (5, 196, '[+00:00] Africa/Timbuktu', 'Africa/Timbuktu'),
    (5, 197, '[+00:00] America/Danmarkshavn', 'America/Danmarkshavn'),
    (5, 198, '[+00:00] Atlantic/Canary', 'Atlantic/Canary'),
    (5, 199, '[+00:00] Atlantic/Faeroe', 'Atlantic/Faeroe')
;
-- CHECKPOINT C-12f

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 200, '[+00:00] Atlantic/Faroe', 'Atlantic/Faroe'),
    (5, 201, '[+00:00] Atlantic/Madeira', 'Atlantic/Madeira'),
    (5, 202, '[+00:00] Atlantic/Reykjavik', 'Atlantic/Reykjavik'),
    (5, 203, '[+00:00] Atlantic/St_Helena', 'Atlantic/St_Helena'),
    (5, 204, '[+00:00] Europe/Belfast', 'Europe/Belfast'),
    (5, 205, '[+00:00] Europe/Dublin', 'Europe/Dublin'),
    (5, 206, '[+00:00] Europe/Guernsey', 'Europe/Guernsey'),
    (5, 207, '[+00:00] Europe/Isle_of_Man', 'Europe/Isle_of_Man'),
    (5, 208, '[+00:00] Europe/Jersey', 'Europe/Jersey'),
    (5, 209, '[+00:00] Europe/Lisbon', 'Europe/Lisbon'),
    (5, 210, '[+00:00] Europe/London', 'Europe/London'),
    (5, 211, '[+01:00] Africa/Algiers', 'Africa/Algiers'),
    (5, 212, '[+01:00] Africa/Bangui', 'Africa/Bangui'),
    (5, 213, '[+01:00] Africa/Brazzaville', 'Africa/Brazzaville'),
    (5, 214, '[+01:00] Africa/Ceuta', 'Africa/Ceuta'),
    (5, 215, '[+01:00] Africa/Douala', 'Africa/Douala'),
    (5, 216, '[+01:00] Africa/Kinshasa', 'Africa/Kinshasa'),
    (5, 217, '[+01:00] Africa/Lagos', 'Africa/Lagos'),
    (5, 218, '[+01:00] Africa/Libreville', 'Africa/Libreville'),
    (5, 219, '[+01:00] Africa/Luanda', 'Africa/Luanda'),
    (5, 220, '[+01:00] Africa/Malabo', 'Africa/Malabo'),
    (5, 221, '[+01:00] Africa/Ndjamena', 'Africa/Ndjamena'),
    (5, 222, '[+01:00] Africa/Niamey', 'Africa/Niamey'),
    (5, 223, '[+01:00] Africa/Porto-Novo', 'Africa/Porto-Novo'),
    (5, 224, '[+01:00] Africa/Tunis', 'Africa/Tunis'),
    (5, 225, '[+01:00] Africa/Windhoek', 'Africa/Windhoek'),
    (5, 226, '[+01:00] Arctic/Longyearbyen', 'Arctic/Longyearbyen'),
    (5, 227, '[+01:00] Atlantic/Jan_Mayen', 'Atlantic/Jan_Mayen'),
    (5, 228, '[+01:00] Europe/Amsterdam', 'Europe/Amsterdam'),
    (5, 229, '[+01:00] Europe/Andorra', 'Europe/Andorra'),
    (5, 230, '[+01:00] Europe/Belgrade', 'Europe/Belgrade'),
    (5, 231, '[+01:00] Europe/Berlin', 'Europe/Berlin'),
    (5, 232, '[+01:00] Europe/Bratislava', 'Europe/Bratislava'),
    (5, 233, '[+01:00] Europe/Brussels', 'Europe/Brussels'),
    (5, 234, '[+01:00] Europe/Budapest', 'Europe/Budapest'),
    (5, 235, '[+01:00] Europe/Copenhagen', 'Europe/Copenhagen'),
    (5, 236, '[+01:00] Europe/Gibraltar', 'Europe/Gibraltar'),
    (5, 237, '[+01:00] Europe/Ljubljana', 'Europe/Ljubljana'),
    (5, 238, '[+01:00] Europe/Luxembourg', 'Europe/Luxembourg'),
    (5, 239, '[+01:00] Europe/Madrid', 'Europe/Madrid'),
    (5, 240, '[+01:00] Europe/Malta', 'Europe/Malta'),
    (5, 241, '[+01:00] Europe/Monaco', 'Europe/Monaco'),
    (5, 242, '[+01:00] Europe/Oslo', 'Europe/Oslo'),
    (5, 243, '[+01:00] Europe/Paris', 'Europe/Paris'),
    (5, 244, '[+01:00] Europe/Podgorica', 'Europe/Podgorica'),
    (5, 245, '[+01:00] Europe/Prague', 'Europe/Prague'),
    (5, 246, '[+01:00] Europe/Rome', 'Europe/Rome'),
    (5, 247, '[+01:00] Europe/San_Marino', 'Europe/San_Marino'),
    (5, 248, '[+01:00] Europe/Sarajevo', 'Europe/Sarajevo'),
    (5, 249, '[+01:00] Europe/Skopje', 'Europe/Skopje')
;
-- CHECKPOINT C-12g

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 250, '[+01:00] Europe/Stockholm', 'Europe/Stockholm'),
    (5, 251, '[+01:00] Europe/Tirane', 'Europe/Tirane'),
    (5, 252, '[+01:00] Europe/Vaduz', 'Europe/Vaduz'),
    (5, 253, '[+01:00] Europe/Vatican', 'Europe/Vatican'),
    (5, 254, '[+01:00] Europe/Vienna', 'Europe/Vienna'),
    (5, 255, '[+01:00] Europe/Warsaw', 'Europe/Warsaw'),
    (5, 256, '[+01:00] Europe/Zagreb', 'Europe/Zagreb'),
    (5, 257, '[+01:00] Europe/Zurich', 'Europe/Zurich'),
    (5, 258, '[+02:00] Africa/Blantyre', 'Africa/Blantyre'),
    (5, 259, '[+02:00] Africa/Bujumbura', 'Africa/Bujumbura'),
    (5, 260, '[+02:00] Africa/Cairo', 'Africa/Cairo'),
    (5, 261, '[+02:00] Africa/Gaborone', 'Africa/Gaborone'),
    (5, 262, '[+02:00] Africa/Harare', 'Africa/Harare'),
    (5, 263, '[+02:00] Africa/Johannesburg', 'Africa/Johannesburg'),
    (5, 264, '[+02:00] Africa/Kigali', 'Africa/Kigali'),
    (5, 265, '[+02:00] Africa/Lubumbashi', 'Africa/Lubumbashi'),
    (5, 266, '[+02:00] Africa/Lusaka', 'Africa/Lusaka'),
    (5, 267, '[+02:00] Africa/Maputo', 'Africa/Maputo'),
    (5, 268, '[+02:00] Africa/Maseru', 'Africa/Maseru'),
    (5, 269, '[+02:00] Africa/Mbabane', 'Africa/Mbabane'),
    (5, 270, '[+02:00] Africa/Tripoli', 'Africa/Tripoli'),
    (5, 271, '[+02:00] Asia/Amman', 'Asia/Amman'),
    (5, 272, '[+02:00] Asia/Beirut', 'Asia/Beirut'),
    (5, 273, '[+02:00] Asia/Damascus', 'Asia/Damascus'),
    (5, 274, '[+02:00] Asia/Gaza', 'Asia/Gaza'),
    (5, 275, '[+02:00] Asia/Istanbul', 'Asia/Istanbul'),
    (5, 276, '[+02:00] Asia/Jerusalem', 'Asia/Jerusalem'),
    (5, 277, '[+02:00] Asia/Nicosia', 'Asia/Nicosia'),
    (5, 278, '[+02:00] Asia/Tel_Aviv', 'Asia/Tel_Aviv'),
    (5, 279, '[+02:00] Europe/Athens', 'Europe/Athens'),
    (5, 280, '[+02:00] Europe/Bucharest', 'Europe/Bucharest'),
    (5, 281, '[+02:00] Europe/Chisinau', 'Europe/Chisinau'),
    (5, 282, '[+02:00] Europe/Helsinki', 'Europe/Helsinki'),
    (5, 283, '[+02:00] Europe/Istanbul', 'Europe/Istanbul'),
    (5, 284, '[+02:00] Europe/Kaliningrad', 'Europe/Kaliningrad'),
    (5, 285, '[+02:00] Europe/Kiev', 'Europe/Kiev'),
    (5, 286, '[+02:00] Europe/Mariehamn', 'Europe/Mariehamn'),
    (5, 287, '[+02:00] Europe/Minsk', 'Europe/Minsk'),
    (5, 288, '[+02:00] Europe/Nicosia', 'Europe/Nicosia'),
    (5, 289, '[+02:00] Europe/Riga', 'Europe/Riga'),
    (5, 290, '[+02:00] Europe/Simferopol', 'Europe/Simferopol'),
    (5, 291, '[+02:00] Europe/Sofia', 'Europe/Sofia'),
    (5, 292, '[+02:00] Europe/Tallinn', 'Europe/Tallinn'),
    (5, 293, '[+02:00] Europe/Tiraspol', 'Europe/Tiraspol'),
    (5, 294, '[+02:00] Europe/Uzhgorod', 'Europe/Uzhgorod'),
    (5, 295, '[+02:00] Europe/Vilnius', 'Europe/Vilnius'),
    (5, 296, '[+02:00] Europe/Zaporozhye', 'Europe/Zaporozhye'),
    (5, 297, '[+03:00] Africa/Addis_Ababa', 'Africa/Addis_Ababa'),
    (5, 298, '[+03:00] Africa/Asmara', 'Africa/Asmara'),
    (5, 299, '[+03:00] Africa/Asmera', 'Africa/Asmera')
;
-- CHECKPOINT C-12h

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 300, '[+03:00] Africa/Dar_es_Salaam', 'Africa/Dar_es_Salaam'),
    (5, 301, '[+03:00] Africa/Djibouti', 'Africa/Djibouti'),
    (5, 302, '[+03:00] Africa/Kampala', 'Africa/Kampala'),
    (5, 303, '[+03:00] Africa/Khartoum', 'Africa/Khartoum'),
    (5, 304, '[+03:00] Africa/Mogadishu', 'Africa/Mogadishu'),
    (5, 305, '[+03:00] Africa/Nairobi', 'Africa/Nairobi'),
    (5, 306, '[+03:00] Antarctica/Syowa', 'Antarctica/Syowa'),
    (5, 307, '[+03:00] Asia/Aden', 'Asia/Aden'),
    (5, 308, '[+03:00] Asia/Baghdad', 'Asia/Baghdad'),
    (5, 309, '[+03:00] Asia/Bahrain', 'Asia/Bahrain'),
    (5, 310, '[+03:00] Asia/Kuwait', 'Asia/Kuwait'),
    (5, 311, '[+03:00] Asia/Qatar', 'Asia/Qatar'),
    (5, 312, '[+03:00] Asia/Riyadh', 'Asia/Riyadh'),
    (5, 313, '[+03:00] Europe/Moscow', 'Europe/Moscow'),
    (5, 314, '[+03:00] Europe/Volgograd', 'Europe/Volgograd'),
    (5, 315, '[+03:00] Indian/Antananarivo', 'Indian/Antananarivo'),
    (5, 316, '[+03:00] Indian/Comoro', 'Indian/Comoro'),
    (5, 317, '[+03:00] Indian/Mayotte', 'Indian/Mayotte'),
    (5, 318, '[+03:30] Asia/Tehran', 'Asia/Tehran'),
    (5, 319, '[+04:00] Asia/Baku', 'Asia/Baku'),
    (5, 320, '[+04:00] Asia/Dubai', 'Asia/Dubai'),
    (5, 321, '[+04:00] Asia/Muscat', 'Asia/Muscat'),
    (5, 322, '[+04:00] Asia/Tbilisi', 'Asia/Tbilisi'),
    (5, 323, '[+04:00] Asia/Yerevan', 'Asia/Yerevan'),
    (5, 324, '[+04:00] Europe/Samara', 'Europe/Samara'),
    (5, 325, '[+04:00] Indian/Mahe', 'Indian/Mahe'),
    (5, 326, '[+04:00] Indian/Mauritius', 'Indian/Mauritius'),
    (5, 327, '[+04:00] Indian/Reunion', 'Indian/Reunion'),
    (5, 328, '[+04:30] Asia/Kabul', 'Asia/Kabul'),
    (5, 329, '[+05:00] Asia/Aqtau', 'Asia/Aqtau'),
    (5, 330, '[+05:00] Asia/Aqtobe', 'Asia/Aqtobe'),
    (5, 331, '[+05:00] Asia/Ashgabat', 'Asia/Ashgabat'),
    (5, 332, '[+05:00] Asia/Ashkhabad', 'Asia/Ashkhabad'),
    (5, 333, '[+05:00] Asia/Dushanbe', 'Asia/Dushanbe'),
    (5, 334, '[+05:00] Asia/Karachi', 'Asia/Karachi'),
    (5, 335, '[+05:00] Asia/Oral', 'Asia/Oral'),
    (5, 336, '[+05:00] Asia/Samarkand', 'Asia/Samarkand'),
    (5, 337, '[+05:00] Asia/Tashkent', 'Asia/Tashkent'),
    (5, 338, '[+05:00] Asia/Yekaterinburg', 'Asia/Yekaterinburg'),
    (5, 339, '[+05:00] Indian/Kerguelen', 'Indian/Kerguelen'),
    (5, 340, '[+05:00] Indian/Maldives', 'Indian/Maldives'),
    (5, 341, '[+05:30] Asia/Calcutta', 'Asia/Calcutta'),
    (5, 342, '[+05:30] Asia/Colombo', 'Asia/Colombo'),
    (5, 343, '[+05:30] Asia/Kolkata', 'Asia/Kolkata'),
    (5, 344, '[+05:45] Asia/Kathmandu', 'Asia/Kathmandu'),
    (5, 345, '[+05:45] Asia/Katmandu', 'Asia/Katmandu'),
    (5, 346, '[+06:00] Antarctica/Mawson', 'Antarctica/Mawson'),
    (5, 347, '[+06:00] Antarctica/Vostok', 'Antarctica/Vostok'),
    (5, 348, '[+06:00] Asia/Almaty', 'Asia/Almaty'),
    (5, 349, '[+06:00] Asia/Bishkek', 'Asia/Bishkek')
;
-- CHECKPOINT C-12i

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 350, '[+06:00] Asia/Dacca', 'Asia/Dacca'),
    (5, 351, '[+06:00] Asia/Dhaka', 'Asia/Dhaka'),
    (5, 352, '[+06:00] Asia/Novosibirsk', 'Asia/Novosibirsk'),
    (5, 353, '[+06:00] Asia/Omsk', 'Asia/Omsk'),
    (5, 354, '[+06:00] Asia/Qyzylorda', 'Asia/Qyzylorda'),
    (5, 355, '[+06:00] Asia/Thimbu', 'Asia/Thimbu'),
    (5, 356, '[+06:00] Asia/Thimphu', 'Asia/Thimphu'),
    (5, 357, '[+06:00] Indian/Chagos', 'Indian/Chagos'),
    (5, 358, '[+06:30] Asia/Rangoon', 'Asia/Rangoon'),
    (5, 359, '[+06:30] Indian/Cocos', 'Indian/Cocos'),
    (5, 360, '[+07:00] Antarctica/Davis', 'Antarctica/Davis'),
    (5, 361, '[+07:00] Asia/Bangkok', 'Asia/Bangkok'),
    (5, 362, '[+07:00] Asia/Ho_Chi_Minh', 'Asia/Ho_Chi_Minh'),
    (5, 363, '[+07:00] Asia/Hovd', 'Asia/Hovd'),
    (5, 364, '[+07:00] Asia/Jakarta', 'Asia/Jakarta'),
    (5, 365, '[+07:00] Asia/Krasnoyarsk', 'Asia/Krasnoyarsk'),
    (5, 366, '[+07:00] Asia/Phnom_Penh', 'Asia/Phnom_Penh'),
    (5, 367, '[+07:00] Asia/Pontianak', 'Asia/Pontianak'),
    (5, 368, '[+07:00] Asia/Saigon', 'Asia/Saigon'),
    (5, 369, '[+07:00] Asia/Vientiane', 'Asia/Vientiane'),
    (5, 370, '[+07:00] Indian/Christmas', 'Indian/Christmas'),
    (5, 371, '[+08:00] Antarctica/Casey', 'Antarctica/Casey'),
    (5, 372, '[+08:00] Asia/Brunei', 'Asia/Brunei'),
    (5, 373, '[+08:00] Asia/Choibalsan', 'Asia/Choibalsan'),
    (5, 374, '[+08:00] Asia/Chongqing', 'Asia/Chongqing'),
    (5, 375, '[+08:00] Asia/Chungking', 'Asia/Chungking'),
    (5, 376, '[+08:00] Asia/Harbin', 'Asia/Harbin'),
    (5, 377, '[+08:00] Asia/Hong_Kong', 'Asia/Hong_Kong'),
    (5, 378, '[+08:00] Asia/Irkutsk', 'Asia/Irkutsk'),
    (5, 379, '[+08:00] Asia/Kashgar', 'Asia/Kashgar'),
    (5, 380, '[+08:00] Asia/Kuala_Lumpur', 'Asia/Kuala_Lumpur'),
    (5, 381, '[+08:00] Asia/Kuching', 'Asia/Kuching'),
    (5, 382, '[+08:00] Asia/Macao', 'Asia/Macao'),
    (5, 383, '[+08:00] Asia/Macau', 'Asia/Macau'),
    (5, 384, '[+08:00] Asia/Makassar', 'Asia/Makassar'),
    (5, 385, '[+08:00] Asia/Manila', 'Asia/Manila'),
    (5, 386, '[+08:00] Asia/Shanghai', 'Asia/Shanghai'),
    (5, 387, '[+08:00] Asia/Singapore', 'Asia/Singapore'),
    (5, 388, '[+08:00] Asia/Taipei', 'Asia/Taipei'),
    (5, 389, '[+08:00] Asia/Ujung_Pandang', 'Asia/Ujung_Pandang'),
    (5, 390, '[+08:00] Asia/Ulaanbaatar', 'Asia/Ulaanbaatar'),
    (5, 391, '[+08:00] Asia/Ulan_Bator', 'Asia/Ulan_Bator'),
    (5, 392, '[+08:00] Asia/Urumqi', 'Asia/Urumqi'),
    (5, 393, '[+09:00] Asia/Dili', 'Asia/Dili'),
    (5, 394, '[+09:00] Asia/Jayapura', 'Asia/Jayapura'),
    (5, 395, '[+09:00] Asia/Pyongyang', 'Asia/Pyongyang'),
    (5, 396, '[+09:00] Asia/Seoul', 'Asia/Seoul'),
    (5, 397, '[+09:00] Asia/Tokyo', 'Asia/Tokyo'),
    (5, 398, '[+09:00] Asia/Yakutsk', 'Asia/Yakutsk'),
    (5, 399, '[+09:00] Pacific/Palau', 'Pacific/Palau')
;
-- CHECKPOINT C-12j

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (5, 400, '[+10:00] Antarctica/DumontDUrville', 'Antarctica/DumontDUrville'),
    (5, 401, '[+10:00] Asia/Sakhalin', 'Asia/Sakhalin'),
    (5, 402, '[+10:00] Asia/Vladivostok', 'Asia/Vladivostok'),
    (5, 403, '[+10:00] Pacific/Guam', 'Pacific/Guam'),
    (5, 404, '[+10:00] Pacific/Port_Moresby', 'Pacific/Port_Moresby'),
    (5, 405, '[+10:00] Pacific/Saipan', 'Pacific/Saipan'),
    (5, 406, '[+10:00] Pacific/Truk', 'Pacific/Truk'),
    (5, 407, '[+10:00] Pacific/Yap', 'Pacific/Yap'),
    (5, 408, '[+11:00] Asia/Magadan', 'Asia/Magadan'),
    (5, 409, '[+11:00] Pacific/Efate', 'Pacific/Efate'),
    (5, 410, '[+11:00] Pacific/Guadalcanal', 'Pacific/Guadalcanal'),
    (5, 411, '[+11:00] Pacific/Kosrae', 'Pacific/Kosrae'),
    (5, 412, '[+11:00] Pacific/Noumea', 'Pacific/Noumea'),
    (5, 413, '[+11:00] Pacific/Ponape', 'Pacific/Ponape'),
    (5, 414, '[+11:30] Pacific/Norfolk', 'Pacific/Norfolk'),
    (5, 415, '[+12:00] Antarctica/McMurdo', 'Antarctica/McMurdo'),
    (5, 416, '[+12:00] Antarctica/South_Pole', 'Antarctica/South_Pole'),
    (5, 417, '[+12:00] Asia/Anadyr', 'Asia/Anadyr'),
    (5, 418, '[+12:00] Asia/Kamchatka', 'Asia/Kamchatka'),
    (5, 419, '[+12:00] Pacific/Auckland', 'Pacific/Auckland'),
    (5, 420, '[+12:00] Pacific/Fiji', 'Pacific/Fiji'),
    (5, 421, '[+12:00] Pacific/Funafuti', 'Pacific/Funafuti'),
    (5, 422, '[+12:00] Pacific/Kwajalein', 'Pacific/Kwajalein'),
    (5, 423, '[+12:00] Pacific/Majuro', 'Pacific/Majuro'),
    (5, 424, '[+12:00] Pacific/Nauru', 'Pacific/Nauru'),
    (5, 425, '[+12:00] Pacific/Tarawa', 'Pacific/Tarawa'),
    (5, 426, '[+12:00] Pacific/Wake', 'Pacific/Wake'),
    (5, 427, '[+12:00] Pacific/Wallis', 'Pacific/Wallis'),
    (5, 428, '[+12:45] Pacific/Chatham', 'Pacific/Chatham'),
    (5, 429, '[+13:00] Pacific/Enderbury', 'Pacific/Enderbury'),
    (5, 430, '[+13:00] Pacific/Tongatapu', 'Pacific/Tongatapu'),
    (5, 431, '[+14:00] Pacific/Kiritimati', 'Pacific/Kiritimati')
;
-- CHECKPOINT C-12k

INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (6, 1, 'Logging disabled', '0'),
    (6, 2, 'Errors only', '1'),
    (6, 3, 'Errors and warnings', '2'),
    (6, 4, 'Errors, warnings and infos', '3'),
    (6, 5, 'Debugging (low detail)', '4'),
    (6, 6, 'Debugging (medium detail)', '5'),
    (6, 7, 'Debugging (high detail)', '6'),
    (7, 1, 'Disabled', '0'),
    (7, 2, 'Low detail', '1'),
    (7, 3, 'Medium detail', '2'),
    (7, 4, 'High detail', '3'),
    (8, 1, 'Never retry submissions', '1'),
    (8, 2, 'Ask user (specifiy period below)', '2'),
    (8, 3, 'Always retry submissions (specifiy period below)', '3'),
    (9, 1, 'Status requesting disabled', '1'),
    (9, 2, 'Web service', '2'),
    (9, 3, 'XML file', '3'),
    (10, 1, 'Status requesting disabled', '1'),
    (10, 2, 'Ganglia XML', '2'),
    (10, 3, 'Globus MDS XML', '3'),
    (11, 1, 'FTP', 'ftp'),
    (11, 2, 'SFTP', 'sftp'),
    (11, 3, 'SCP', 'scp'),
    (11, 4, 'GSIFTP', 'gsiftp'),
    (12, 1, '1', '1'),
    (12, 2, '2', '2'),
    (12, 3, '3', '3'),
    (12, 4, '4', '4'),
    (12, 5, '5', '5'),
    (13, 1, '[no general rule]', '0'),
    (13, 2, 'Never', '1'),
    (13, 3, 'Always', '2'),
    (14, 1, 'Disabled', '0'),
    (14, 2, 'Approval by administrator', '1'),
    (14, 3, 'Activation via e-mail link', '2'),
    (14, 4, 'Immediate activation', '3'),
    (15, 1, 'Disabled', '0'),
    (15, 2, 'Only configured OpenID providers', '1'),
    (15, 3, 'All OpenID providers', '2'),
    (16, 1, 'Do not use client certificates', ''),
    (16, 2, 'Check certificate subject', 'CERT_SUBJECT'),
    (16, 3, 'Compare PEM-formatted certificate', 'PROXY_SSL_CLIENT_CERT')
;
-- CHECKPOINT C-12l
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

CREATE TABLE usr_role (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    id_role int unsigned NOT NULL COMMENT 'FK: Manager role to which the user is assigned',
    CONSTRAINT pk_usr_role PRIMARY KEY (id_usr, id_role),
    CONSTRAINT fk_usr_role_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_usr_role_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of users to manager roles';
-- CHECKPOINT C-24

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

CREATE TABLE cr_priv (
    id_cr int unsigned NOT NULL COMMENT 'FK: Computing resource',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    credits int unsigned NOT NULL DEFAULT 0 COMMENT 'Maximum resource credits for the user',
    CONSTRAINT fk_cr_priv_cr FOREIGN KEY (id_cr) REFERENCES cr(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on computing resources';
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
    url varchar(100) COMMENT 'Base WPS access point',
    proxy boolean NOT NULL DEFAULT false COMMENT 'If true, wps is proxied',
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

CREATE TABLE series_priv (
    id_series int unsigned NOT NULL COMMENT 'FK: Series',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_series_priv_series FOREIGN KEY (id_series) REFERENCES series(id) ON DELETE CASCADE,
    CONSTRAINT fk_series_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_series_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on series';
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

CREATE TABLE producttype_priv (
    id_producttype int unsigned NOT NULL COMMENT 'FK: Product type',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_producttype_priv_producttype FOREIGN KEY (id_producttype) REFERENCES producttype(id) ON DELETE CASCADE,
    CONSTRAINT fk_producttype_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_producttype_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on product types';
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
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    name varchar(50) COMMENT 'Name',
    is_default boolean DEFAULT false COMMENT 'If true, resource set is selected by default',
    access_key varchar(50) COMMENT 'Access key',
    creation_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of resource set creation',
    CONSTRAINT pk_resourceset PRIMARY KEY (id),
    UNIQUE INDEX (identifier),
    CONSTRAINT fk_resourceset_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Sets of remote resources';
-- CHECKPOINT C-41

/*****************************************************************************/

CREATE TABLE resourceset_priv (
    id_resourceset int unsigned NOT NULL COMMENT 'FK: Resource set',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_resourceset_priv_resourceset FOREIGN KEY (id_resourceset) REFERENCES resourceset(id) ON DELETE CASCADE,
    CONSTRAINT fk_resourceset_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_resourceset_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on resource sets';
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

CREATE TABLE pubserver_priv (
    id_pubserver int unsigned NOT NULL COMMENT 'FK: Publish server',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_pubserver_priv_pubserver FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE CASCADE,
    CONSTRAINT fk_pubserver_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_pubserver_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on publish servers';
-- CHECKPOINT C-45

/*****************************************************************************/

CREATE TABLE service (
    id int unsigned NOT NULL auto_increment,
    id_type int unsigned NOT NULL COMMENT 'FK: Entity type extension',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_class int unsigned COMMENT 'FK: Service class',
    conf_deleg boolean NOT NULL DEFAULT false COMMENT 'If true, service can be configured by other domains',
    available boolean NOT NULL DEFAULT true COMMENT 'If true, service is available',
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'Name',
    description text NOT NULL COMMENT 'Description',
    version varchar(10) COMMENT 'Version',
    c_version varchar(10) COMMENT 'Cleanup version (in case of interrupted upgrade)',
    url varchar(200) NOT NULL COMMENT 'Access point of service (relative URL)',
    icon_url varchar(200) COMMENT 'Relative URL of logo/icon',
    view_url varchar(200) COMMENT 'View URL',
    rating tinyint COMMENT 'Rating in stars (0 to 5)',
    all_input boolean COMMENT 'If true, service accepts all non-manual series as input',
    created datetime,
    modified datetime,
    CONSTRAINT pk_service PRIMARY KEY (id),
    UNIQUE INDEX (identifier),
    CONSTRAINT fk_service_type FOREIGN KEY (id_type) REFERENCES type(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_class FOREIGN KEY (id_class) REFERENCES serviceclass(id) ON DELETE SET NULL
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

CREATE TABLE service_priv (
    id_service int unsigned NOT NULL COMMENT 'FK: Service',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    allow_scheduling boolean COMMENT 'If true, user can schedule the service',
    CONSTRAINT fk_service_priv_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_service_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on services';
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

CREATE TABLE safe_priv (
    id_safe int unsigned NOT NULL COMMENT 'FK: Safe',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_safe_priv_safe FOREIGN KEY (id_safe) REFERENCES safe(id) ON DELETE CASCADE,
    CONSTRAINT fk_safe_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_safe_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on safe';
-- CHECKPOINT C-71

/*****************************************************************************/

CREATE TABLE activity (
    id int unsigned NOT NULL auto_increment,
    id_entity int unsigned COMMENT 'Entity associated to the activity',
    id_usr int unsigned COMMENT 'User doing the activity',
    id_priv int unsigned COMMENT 'Privilege associated',
    id_type int unsigned COMMENT 'Entity type',
    id_owner int unsigned COMMENT 'User owning the entity related to the activity',
    log_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of activity creation',
    CONSTRAINT pk_activity PRIMARY KEY (id),
    INDEX (`id_usr`),
    INDEX (`creation_time`)
) Engine=InnoDB COMMENT 'User activities';
-- CHECKPOINT C-72

CREATE TABLE priv_score (
    id_priv int unsigned COMMENT 'FK: Privilege associated',
    score_usr int unsigned COMMENT 'Score associated to a user',
    score_owner int unsigned COMMENT 'Score associated to an owner',
    CONSTRAINT fk_priv_score_priv FOREIGN KEY (id_priv) REFERENCES priv(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Privilege scores';
-- CHECKPOINT C-73

/*****************************************************************************/

USE $NEWS$;

/*****************************************************************************/

CREATE TABLE feature (
    id int unsigned NOT NULL auto_increment,
    pos int unsigned COMMENT 'Feature position',
    title varchar(40) NOT NULL,
    description varchar(150),
    image_url varchar(100),
    image_style varchar(100),
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

