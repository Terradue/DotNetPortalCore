USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "service" (adding terms and conditions) ... \
ALTER TABLE service
    ADD COLUMN termsconditions_url varchar(200) COMMENT 'service termsconditions url'
;
ALTER TABLE service
    ADD COLUMN termsconditions_text text COMMENT 'service termsconditions text'
;
-- RESULT

/*****************************************************************************/