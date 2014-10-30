/*****************************************************************************/

-- Renaming configuration settings and removing external catalogue setting ... \
UPDATE config SET name='UnavailabilityStartDate' WHERE name='NotAvailableDate';
UPDATE config SET name='UnavailabilityMessage' WHERE name='NotAvailableMessage';
DELETE FROM config WHERE name='ExternalCatalogueHost';
UPDATE config SET pos=pos-1 WHERE id_section=5 AND pos>3;
-- RESULT

/*****************************************************************************/

-- Changing structure of table "pubserver" ... \
ALTER TABLE pubserver
    ADD COLUMN is_default boolean default false COMMENT 'True if publish server is selected by default'
;
-- RESULT

/*****************************************************************************/
