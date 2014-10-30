/*****************************************************************************/

-- Removing configuration settings for notifications ... \
DELETE FROM config WHERE name in ('NotificationAccessPoint', 'NotificationLogFile', 'NotificationLogLevel');
-- RESULT
UPDATE config SET pos=pos-3 WHERE id_section=2 AND pos>=8;
-- NORESULT

/*****************************************************************************/

-- Removing notification table ... \
DROP TABLE notification;
-- RESULT

/*****************************************************************************/
