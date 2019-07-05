USE $MAIN$;

/*****************************************************************************/

-- Create table wpsprovider_dev ... \
CREATE TABLE wpsprovider_dev (
    id_wpsprovider int unsigned NOT NULL COMMENT 'FK: WPS provider',
    id_usr int unsigned COMMENT 'FK: User',
    
    CONSTRAINT fk_wpsprovider_dev_wpsprovider FOREIGN KEY (id_wpsprovider) REFERENCES wpsprovider(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpsprovider_dev_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User developers of WPS providers';
-- RESULT 