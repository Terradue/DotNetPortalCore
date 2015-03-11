USE $MAIN$;

/*****************************************************************************/

-- Adding activation rule to authentication types ... \
ALTER TABLE auth
    ADD COLUMN activation_rule int NOT NULL DEFAULT 0 COMMENT 'Rule for account activation' AFTER enabled,
    CHANGE COLUMN rule normal_rule int NOT NULL DEFAULT 2 COMMENT 'Rule for normal accounts'
;
-- RESULT

/*****************************************************************************/
