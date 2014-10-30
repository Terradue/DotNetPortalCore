/*****************************************************************************/

-- Adding new Computing Element status request method ... \
SET @list = (SELECT id FROM lookuplist WHERE name='ceStatusRequest');
INSERT INTO lookup (id_list, pos, caption, value) VALUES
	(@list, 3, 'Globus MDS XML', '3')
;
-- RESULT

/*****************************************************************************/
