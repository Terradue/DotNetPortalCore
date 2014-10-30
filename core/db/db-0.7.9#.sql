/*****************************************************************************/

-- Adding new configuration section for catalogue settings ... \
UPDATE configsection SET pos = pos + 1 WHERE pos >= 4;
-- NORESULT

INSERT INTO configsection (id, caption, pos) VALUES
    (7, 'Catalogue', 4)
;
-- RESULT

-- Adding new configuration items for catalogue settings ... \
UPDATE config SET id_section=7, pos=1, caption='Default Catalogue Base URL' WHERE name='DefaultCatalogueBaseUrl';
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (7, 2, 'CatalogueSeriesBaseUrl', 'url', NULL, 'Base URL for Catalogue Series', 'Enter the URL at which a new series can be inserted or updated in a calagogue; you may use the $(CATALOGUE) placeholder for reusing the catalogue base URL (defined above)', '$(CATALOGUE)/rdf', true),
    (7, 3, 'CatalogueDataSetBaseUrl', 'url', NULL, 'Base URL for Catalogue Data Sets', 'Enter the URL at which a new data sets can be inserted or updated in a calagogue; use the $(SERIES) placeholder for the series identifier, you may also use the $(CATALOGUE) placeholder for reusing the catalogue base URL (defined above)', '$(CATALOGUE)/$(SERIES)/rdf', true)
;
-- RESULT

UPDATE config SET pos = pos - 1 WHERE id_section = 4 AND pos > 4;
-- NORESULT

/*****************************************************************************/

CREATE TABLE usrreg (
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    token varchar(50) COMMENT 'Unique activation token (automatically generated UID)',
    CONSTRAINT pk_usr PRIMARY KEY (id_usr),
    CONSTRAINT fk_usrreg_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User registration requests';

/*****************************************************************************/
