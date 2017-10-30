USE $MAIN$;

/*****************************************************************************/

-- Update usr for unique email ... \
ALTER TABLE usr ADD UNIQUE INDEX email_UNIQUE (email ASC);
-- RESULT


