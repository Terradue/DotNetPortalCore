USE $MAIN$;

/*****************************************************************************/

-- Update table wpsprovider ... \
ALTER TABLE wpsprovider 
ADD COLUMN autosync boolean NOT NULL DEFAULT false COMMENT 'If true, wps is automatically synchronized';
-- RESULT