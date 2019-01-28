USE $MAIN$;

/*****************************************************************************/

-- Update table service ... \
ALTER TABLE service 
ADD COLUMN commercial boolean NOT NULL DEFAULT false COMMENT 'If true, service is defined as commercial';
-- RESULT