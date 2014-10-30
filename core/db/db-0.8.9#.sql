/*****************************************************************************/

-- Converting task filter definitions ... \
UPDATE filter SET definition=REPLACE(REPLACE(REPLACE(REPLACE(definition, '?', ''), '&', '\t'), '/admin/task.aspx', ''), 'user=', 'owner=');
-- RESULT

/*****************************************************************************/
