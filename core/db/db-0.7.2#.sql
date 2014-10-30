/*****************************************************************************/

-- Removing configuration variable for portal publish URL  ... \
DELETE FROM config WHERE name='PortalPublishUrl';
-- RESULT
UPDATE config SET pos=pos-1 WHERE id_section=4 AND pos>10;
-- NORESULT

/*****************************************************************************/

-- Changing structure or table "pubserver" (flag for result metadata publication on portal) ... \
ALTER TABLE pubserver
    ADD COLUMN metadata boolean default false COMMENT 'If true, publish server is used for task result metadata, e.g. previews'
;
-- RESULT
-- TODO In "Control Panel" > "Publish Servers", set the flag "Use for task result metadata" for the publish server that receives the result metadata information used by the portal

/*****************************************************************************/

-- Changing structure or table "filter" ... \
ALTER TABLE filter
    ADD COLUMN token varchar(50) COMMENT 'Unique token (automatically generated UID)' AFTER id_usr,
    CHANGE COLUMN entity_ref entity_code smallint unsigned COMMENT 'Entity type code to distinguish different entity types',
    ADD COLUMN script varchar(100) COMMENT 'Script URL for listing' AFTER caption
;
-- RESULT

/*****************************************************************************/
