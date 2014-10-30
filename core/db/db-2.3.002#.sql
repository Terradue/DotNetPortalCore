/*****************************************************************************/

-- Changing structure of table "usrcert" (flag for own certificate) ... \
ALTER TABLE usrcert
    ADD COLUMN own_cert boolean default false COMMENT 'If true, user makes his own certificate settings' AFTER id_usr
;
-- RESULT

-- Setting all user certificates defined so far as own certificates ... \
UPDATE usrcert SET own_cert=true;
-- RESULT

/*****************************************************************************/
