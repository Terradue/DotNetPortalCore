USE $MAIN$;

/*****************************************************************************/

-- Adding creation timestamp to resource sets ... \
ALTER TABLE resourceset
    ADD COLUMN creation_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP AFTER access_key
;
-- RESULT

/*****************************************************************************/