USE $MAIN$;

/*****************************************************************************/

-- Adding extended entity types for activity... \
CALL add_type($ID$, 'Terradue.Portal.Activity, Terradue.Portal', NULL, 'activity', 'activities', 'activity');
-- RESULT

ALTER TABLE activity
ADD COLUMN identifier_entity varchar(50) default NULL COMMENT 'Identifier';
-- RESULT

/*****************************************************************************/