USE $MAIN$;

/*****************************************************************************/

DROP PROCEDURE IF EXISTS add_type;

CREATE PROCEDURE add_type(IN p_module_id int unsigned, IN p_class varchar(100), IN p_super_class varchar(100), IN p_caption_sg varchar(100), IN p_caption_pl varchar(100), IN p_keyword varchar(100))
COMMENT 'Creates a new entity type'
BEGIN
    DECLARE type_id int;
    DECLARE type_pos int;
    IF p_super_class IS NULL THEN
        SELECT CASE WHEN MAX(pos) IS NULL THEN 0 ELSE MAX(pos) END FROM type INTO type_pos;
    ELSE
        SELECT id FROM type WHERE class = p_super_class INTO type_id;
        SELECT CASE WHEN MAX(pos) IS NULL THEN 0 ELSE MAX(pos) END FROM type WHERE id_super = type_id INTO type_pos;
    END IF;
    INSERT INTO type (id_module, id_super, pos, class, caption_sg, caption_pl, keyword) VALUES (p_module_id, type_id, type_pos + 1, p_class, p_caption_sg, p_caption_pl, p_keyword);
END;
-- CHECKPOINT C-01a

DROP PROCEDURE IF EXISTS change_type;

CREATE PROCEDURE change_type(IN p_class varchar(100), IN p_generic_class varchar(100), IN p_pos int unsigned)
COMMENT 'Changes the generic class and/or the position of an entity type'
BEGIN
    DECLARE type_id int;
    DECLARE super_id int;
    DECLARE type_pos int;
    SELECT id, id_super, pos FROM type WHERE class = p_class INTO type_id, super_id, type_pos;
    UPDATE type SET generic_class = p_generic_class WHERE id = type_id;
    IF p_pos > 0 THEN
        UPDATE type SET pos = pos + 1 WHERE CASE WHEN super_id IS NULL THEN id_super IS NULL ELSE id_super = super_id END AND pos >= p_pos AND CASE WHEN type_pos IS NULL THEN true ELSE pos < type_pos END;
		UPDATE type SET pos = p_pos WHERE id = type_id;
    END IF;
END;
-- CHECKPOINT C-01b

/*****************************************************************************/

-- Changing structure of table "wpsprovider" (adding contact) ... \
ALTER TABLE wpsprovider 
    ADD COLUMN contact varchar(200) COMMENT 'Contact information'
;
-- RESULT

/*****************************************************************************/