USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "service" (adding validation URL) ... \
ALTER TABLE service
    ADD COLUMN validation_url varchar(200) COMMENT 'service validation url'
;
-- RESULT

/*****************************************************************************/