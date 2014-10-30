/*****************************************************************************/

-- Changing structure of table "usrreg" ... \
ALTER TABLE usrreg
    ADD COLUMN reset boolean default false COMMENT 'If true, password reset was requested'
;
ALTER TABLE usrreg COMMENT 'User registration or password reset requests';
-- RESULT

/*****************************************************************************/
