/*****************************************************************************/

-- Correcting working and result directories of Computing Elements ... \
UPDATE cedir SET path=TRIM(REPLACE(path, '\n', ''));
-- RESULT

/*****************************************************************************/
