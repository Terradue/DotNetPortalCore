/*****************************************************************************/

-- Renaming daemon to agent ... \
UPDATE configsection SET caption='Background Agent' WHERE caption='Background Daemon';
UPDATE config SET hint='If checked, tasks are submitted directly to the Grid Engine or deleted upon user request, otherwise the background agent does the actual operation later' WHERE name='SyncTaskOperations';
UPDATE config SET name='AgentInterval', caption='Agent Execution Interval (sec)', hint='Enter the base interval in seconds between two executions of the background agent' WHERE name='DaemonInterval';
UPDATE config SET name='AgentLogFile', caption='Agent Log File', hint='Enter the name for the log output of the background agent (use "$DATE" as placeholder for the current date)' WHERE name='DaemonLogFile';
UPDATE config SET name='AgentLogLevel', caption='Agent Log Level', hint='Select the desired degree of detail for the background agent log file' WHERE name='DaemonLogLevel';
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" (debug level) ... \
ALTER TABLE usr
    ADD COLUMN debug_level tinyint unsigned default '0' COMMENT 'Debug level (admins only), 3..6)' AFTER level
;
-- RESULT

/*****************************************************************************/

-- Adding debug level lookup list and values ... \
INSERT INTO lookuplist (system, name) VALUES (true, 'debugLevel');
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 1, 'Disabled', '0' FROM lookuplist WHERE name='debugLevel';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 2, 'Low detail', '1' FROM lookuplist WHERE name='debugLevel';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 3, 'Medium detail', '2' FROM lookuplist WHERE name='debugLevel';
INSERT INTO lookup (id_list, pos, caption, value) SELECT id, 4, 'High detail', '3' FROM lookuplist WHERE name='debugLevel';
-- RESULT

/*****************************************************************************/
