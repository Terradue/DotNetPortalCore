/*****************************************************************************/

-- Changing structure of table "config" ... \
ALTER TABLE config
    ADD COLUMN source varchar(25) COMMENT 'Value source identifier' AFTER type
;

UPDATE config SET source='logLevel', hint='Select the desired degree of detail for the notification log file' WHERE name='NotificationLogLevel';
UPDATE config SET caption='Task and job status request method', source='statusRequest', hint='Select the method for requesting the status information when a task or job is displayed' WHERE name='StatusRequestMethod';
UPDATE config SET hint='Enter the base interval in seconds between two executions of the background daemon' WHERE name='ActionInterval';
UPDATE config SET hint='Enter the name for the log output of the background daemon (use "$DATE" as placeholder for the current date)' WHERE name='ActionLogFile';
UPDATE config SET source='logLevel', hint='Select the desired degree of detail for the background daemon log file' WHERE name='ActionLogLevel';
-- RESULT

/*****************************************************************************/

-- Changing structure of table "application" ... \
ALTER TABLE application
    CHANGE COLUMN available enabled boolean COMMENT 'True if application is available'
;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "scheduler" ... \
ALTER TABLE scheduler
    ADD COLUMN auto_register boolean COMMENT 'True if automatic registration at task completion is desired' AFTER compression
;
-- RESULT

/*****************************************************************************/

CREATE TABLE lookuplist (
    id smallint unsigned NOT NULL auto_increment,
    system boolean default false COMMENT 'True if list is predefined and locked',
    name varchar(25) NOT NULL COMMENT 'Name of lookup list',
    max_length smallint COMMENT 'Maximum string length of contained values',
    PRIMARY KEY (id, system)
) Engine=InnoDB COMMENT 'Configurable lookup lists';

-- Initializing lookup lists ... \
INSERT INTO lookuplist (id, system, name) VALUES
    (1, true, 'userLevel'),
    (2, true, 'language'),
    (3, true, 'timeZone'),
    (4, true, 'logLevel'),
    (5, true, 'statusRequest')
;
-- RESULT

/*****************************************************************************/

CREATE TABLE lookup (
    id_list smallint unsigned NOT NULL,
    pos smallint unsigned COMMENT 'Position for ordering',
    caption varchar(50) NOT NULL COMMENT 'Name of list',
    value text NOT NULL COMMENT 'Name of list',
    FOREIGN KEY (id_list) REFERENCES lookuplist(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Values in lookup lists';

-- Initializing lookup values ... \
INSERT INTO lookup (id_list, pos, caption, value) VALUES
    (1, 1, 'User', '1'),
    (1, 2, 'Developer', '2'),
    (1, 3, 'Administrator', '3'),
    (2, 1, 'English', 'en'),
    (2, 2, 'French (not supported yet)', 'fr'),
    (2, 3, 'German (not supported yet)', 'de'),
    (3, 1, 'UTC/GMT', 'UTC'),
    (3, 2, 'Rome (IT)', 'CE(S)T'),
    (4, 1, 'Logging disabled', '0'),
    (4, 2, 'Errors only', '1'),
    (4, 3, 'Errors and warnings', '2'),
    (4, 4, 'Errors, warnings and infos', '3'),
    (4, 5, 'Debugging (low detail)', '4'),
    (4, 6, 'Debugging (medium detail)', '5'),
    (4, 7, 'Debugging (high detail)', '6'),
    (5, 1, 'Status requesting disabled', '1'),
    (5, 2, 'Use Grid engine web service (wsServer)', '2'),
    (5, 3, 'Use XML files on Grid engine', '3')
;
-- RESULT

/*****************************************************************************/
