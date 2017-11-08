USE $MAIN$;

/*****************************************************************************/

-- Update table service ... \
ALTER TABLE service 
ADD COLUMN geometry varchar(200) DEFAULT NULL COMMENT 'Geometry describing the AOI of the service';
-- RESULT