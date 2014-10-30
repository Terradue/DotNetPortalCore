/*****************************************************************************/

-- Changing structure of table "config" ... \
ALTER TABLE config
    DROP FOREIGN KEY config_ibfk_1,
    CHANGE COLUMN id_configsection id_section smallint unsigned COMMENT 'FK: Configuration section',
    ADD FOREIGN KEY (id_section) REFERENCES configsection(id) ON DELETE SET NULL
;
-- RESULT

/*****************************************************************************/

-- Adding configuration for trusted hosts ... \
UPDATE config SET pos=pos+1 WHERE id_section=1 AND pos>=9;
-- NORESULT
INSERT INTO config (id_section, pos, name, type, caption, hint, value) VALUES
    (1, 9, 'TrustedHosts', 'string', 'Trusted hosts', 'Enter the IP addresses of the hosts trusted by this site separated by comma', '127.0.0.1')
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" ... \
ALTER TABLE usr
    CHANGE COLUMN timezone time_zone char(6) COMMENT 'Time zone',
    CHANGE COLUMN local_only trusted_only boolean COMMENT 'True if only connections from trusted hosts are allowed',
    ADD COLUMN sessionless boolean COMMENT 'True if sessionless requests from trusted hosts are allowed' AFTER trusted_only
;
-- RESULT

/*****************************************************************************/
