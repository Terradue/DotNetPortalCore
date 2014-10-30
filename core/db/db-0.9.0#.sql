/*****************************************************************************/

-- Updating configuration variables
UPDATE config SET caption='Installed database schema', pos=1 WHERE name='DbVersion';
DELETE FROM config WHERE name='DbVersionCheckpoint';
INSERT INTO config (name, caption, pos) VALUES ('DbVersionCheckpoint', 'Database schema checkpoint (internal)', 2);
DELETE FROM config WHERE name='UnavailabilityStartDate';
UPDATE config SET pos=pos-1 WHERE id_section=1 AND pos>2;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "usr" (simplified GUI) ... \
ALTER TABLE usr
    ADD COLUMN simple_gui boolean default false COMMENT 'True if simplified GUI is selected' AFTER debug_level
;
-- RESULT

/*****************************************************************************/
