USE $MAIN$;

/*****************************************************************************/

-- Update config for email body/subjects ... \
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('wpsrequest-timeout', 'int', 'WPS request timeout', 'WPS request timeout', '10000', '0');
-- RESULT
