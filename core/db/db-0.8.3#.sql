/*****************************************************************************/

-- Updating data of table "filter" (external filters) ... \
UPDATE filter SET token = CONCAT('id=', id) WHERE token IS NULL; 
UPDATE filter SET script = '/tasks/' WHERE script IS NULL; 
-- RESULT
-- CHECKPOINT 0.8.3-1

-- Changing structure of table "filter" (external filters) ... \
DROP PROCEDURE IF EXISTS drop_foreign_key;
CREATE PROCEDURE drop_foreign_key(IN tbl VARCHAR(20), IN fkey VARCHAR(20)) BEGIN IF EXISTS(SELECT NULL FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_NAME = fkey) THEN SET @q = CONCAT('ALTER TABLE ', tbl, ' DROP FOREIGN KEY ', fkey); PREPARE stmt FROM @q; EXECUTE stmt; END IF; END;

CALL drop_foreign_key('filter', 'ibfk_filter_1');
CALL drop_foreign_key('filter', 'filter_ibfk_1');
CALL drop_foreign_key('filter', 'fk_fiter_usr');

DROP PROCEDURE IF EXISTS drop_foreign_key;
-- CHECKPOINT 0.8.3-2

DROP INDEX id_usr ON filter;
-- CHECKPOINT 0.8.3-3

ALTER TABLE filter
    CHANGE COLUMN token token varchar(50) NOT NULL COMMENT 'Unique token (automatically generated UID)',
    CHANGE COLUMN caption caption varchar(50) COMMENT 'Caption',
    CHANGE COLUMN script url TEXT NOT NULL COMMENT 'Absolute or relative URL for listing'
;
ALTER TABLE filter ADD CONSTRAINT fk_filter_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE;
-- RESULT

/*****************************************************************************/
