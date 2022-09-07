USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "service" (adding terms and conditions) ... \
ALTER TABLE service
    ADD COLUMN tutorial_url varchar(200) COMMENT 'service tutorial url'
;
ALTER TABLE service
    ADD COLUMN spec_url varchar(200) COMMENT 'service specification text'
;
-- RESULT

/*****************************************************************************/