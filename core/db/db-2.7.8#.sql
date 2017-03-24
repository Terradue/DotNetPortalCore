USE $MAIN$;

/*****************************************************************************/

-- Update table service ... \
ALTER TABLE service 
ADD COLUMN id_usr INT(10) UNSIGNED NULL AFTER available,
ADD COLUMN quotable boolean DEFAULT false,
ADD CONSTRAINT fk_service_user FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE SET NULL;
-- RESULT