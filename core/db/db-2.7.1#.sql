USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "domain" (adding kind and icon URL) ... \
ALTER TABLE domain
    ADD COLUMN kind tinyint unsigned COMMENT 'Kind of domain',
    ADD COLUMN icon_url varchar(200) COMMENT 'Icon URL'
;
-- RESULT

/*****************************************************************************/