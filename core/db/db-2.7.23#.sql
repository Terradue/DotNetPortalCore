USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "service" ... \
ALTER TABLE service
    ADD COLUMN media_url varchar(200) COMMENT 'service media url'
;
-- RESULT

/*****************************************************************************/