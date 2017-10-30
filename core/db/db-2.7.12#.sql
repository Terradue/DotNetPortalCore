USE $MAIN$;

/*****************************************************************************/

-- Adding HTTP sessiom timeout for authentication types ... \
ALTER TABLE auth ADD COLUMN timeout int COMMENT 'HTTP session timeout';
-- RESULT


