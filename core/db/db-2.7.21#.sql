USE $MAIN$;

/*****************************************************************************/

-- Changing structure of table "feature" (adding is dotted) ... \
ALTER TABLE feature ADD COLUMN is_dotted boolean NOT NULL DEFAULT false COMMENT 'If true, feature is dotted';
-- RESULT

/*****************************************************************************/