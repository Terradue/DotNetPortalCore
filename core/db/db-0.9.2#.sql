/*****************************************************************************/

-- Removing news database connection string from control panel ... \
SET @section = (SELECT id_section FROM config WHERE name='NewsDatabase');
SET @pos = (SELECT pos FROM config WHERE name='NewsDatabase');
DELETE FROM config WHERE name='NewsDatabase';
UPDATE config SET pos=pos-1 WHERE id_section=@section AND pos>@pos;
-- RESULT

-- TODO Open the file sites/sitname/root/web.config and add the key "NewsDatabaseConnection" with the connection string for the news database

-- Adding configuration variable for OpenID authentication ... \
SET @section = (SELECT id FROM configsection WHERE caption='Users');
UPDATE config SET pos = pos + 2 WHERE id_section=@section;
-- NORESULT
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section, 1, 'OpenIdAuth', 'int', 'openIdRule', 'OpenID Authentication', 'Select the rule for OpenID authentication', '0', true),
    (@section, 2, 'OpenIdNonceValidity', 'string', NULL, 'Validity of OpenID Responce Nonces', 'Select the maximum validity of a responce nonce in a positive authentication assertion, use quantifiers h (hours), m (minutes), s (seconds), e.g. 10m', '10m', true)
;
-- RESULT

/*****************************************************************************/

CREATE TABLE openidprovider (
    id int unsigned NOT NULL auto_increment,
    caption varchar(50) NOT NULL COMMENT 'Caption',
    op_identifier varchar(100) COMMENT 'OpenID provider identifier',
    endpoint_url varchar(100) COMMENT 'Endpoint URL',
    pattern varchar(100) COMMENT 'Pattern for converting user input to identifier',
    input_caption varchar(100) COMMENT 'Caption for user input or user-specified identifier',
    logo_url varchar(200) COMMENT 'URL of logo image',
    CONSTRAINT pk_openidprovider PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'OpenID providers';

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

/*****************************************************************************/

CREATE TABLE openidnonce (
    time_part datetime NOT NULL COMMENT 'Nonce UTC time',
    random_part varchar(50) COMMENT 'Random characters after time part'
) Engine=InnoDB COMMENT 'Open ID nonces';

/*****************************************************************************/

INSERT INTO lookuplist (system, name) VALUES (true, 'openIdRule');
SET @list = (SELECT LAST_INSERT_ID());
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (@list, 1, 'Disabled', '0'),
    (@list, 2, 'Only configured OpenID providers', '1'),
    (@list, 3, 'All OpenID providers', '2')
;

/*****************************************************************************/
