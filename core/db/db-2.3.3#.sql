USE $MAIN$;

/*****************************************************************************/

-- Create/replace stored procedures
DROP PROCEDURE IF EXISTS add_series;
DROP PROCEDURE IF EXISTS add_servicecategory;
DROP PROCEDURE IF EXISTS link_service_series;
DROP PROCEDURE IF EXISTS unlink_service_series;
DROP PROCEDURE IF EXISTS link_service_category;
DROP PROCEDURE IF EXISTS unlink_service_category;

CREATE PROCEDURE add_series(IN p_identifier varchar(50), IN p_name varchar(200), IN p_description text, IN p_cat_description varchar(200))
COMMENT 'Inserts or updates a series'
BEGIN
    DECLARE series_id int;
    SELECT id FROM series WHERE name = p_identifier INTO series_id;
    IF series_id IS NULL THEN
        INSERT INTO series (name, caption, description, cat_description) VALUES (p_identifier, p_name, p_description, p_cat_description);
    END IF;
END;

CREATE PROCEDURE add_servicecategory(IN p_identifier varchar(50), IN p_name varchar(100))
COMMENT 'Inserts or updates a service category'
BEGIN
    DECLARE category_id int;
    SELECT id FROM servicecategory WHERE name = p_identifier INTO category_id;
    IF category_id IS NULL THEN
        INSERT INTO servicecategory (name, caption) VALUES (p_identifier, p_name);
    ELSE
        UPDATE servicecategory SET caption = p_name WHERE id = category_id;
    END IF;
END;

CREATE PROCEDURE link_service_series(IN service_identifier varchar(50), IN series_identifier varchar(50), IN is_default_series boolean)
COMMENT 'Links an input series to a service'
BEGIN
    DECLARE series_id int unsigned;
    DECLARE service_id int unsigned;
    DECLARE c int;

    SELECT id FROM service WHERE name = service_identifier INTO service_id;
    SELECT id FROM series WHERE name = series_identifier INTO series_id;

    IF service_id IS NOT NULL AND series_id IS NOT NULL THEN
        SELECT COUNT(*) FROM service_series AS t WHERE t.id_service = service_id AND t.id_series = series_id INTO c;
        IF c = 0 THEN
            INSERT INTO service_series (id_service, id_series) VALUES (service_id, series_id);
        END IF;
    
        IF is_default_series THEN
            UPDATE service_series SET is_default = (id_series = series_id) WHERE id_service = service_id;
        END IF;
    END IF;
END;

CREATE PROCEDURE unlink_service_series(IN service_identifier varchar(50), IN series_identifier varchar(50))
COMMENT 'Unlinks an input series from a service'
BEGIN
    DELETE FROM service_series WHERE id_service = (SELECT id FROM service WHERE name = service_identifier) AND id_series = (SELECT id FROM series WHERE name = series_identifier);
END;

CREATE PROCEDURE link_service_category(IN service_identifier varchar(50), IN category_identifier varchar(50))
COMMENT 'Adds a service/category assignment'
BEGIN
    DECLARE c int;
    SELECT COUNT(*) FROM service_category AS t INNER JOIN service AS t1 ON t.id_service=t1.id INNER JOIN servicecategory AS t2 ON t.id_category=t2.id WHERE t1.name = service_identifier AND t2.name = category_identifier INTO c;
    IF c = 0 THEN
        INSERT INTO service_category (id_service, id_category) SELECT t1.id, t2.id FROM service AS t1 INNER JOIN servicecategory AS t2 WHERE t1.name = service_identifier AND t2.name = category_identifier; 
    END IF;
END;

CREATE PROCEDURE unlink_service_category(IN service_identifier varchar(50), IN category_identifier varchar(50))
COMMENT 'Removes a service/category assignment'
BEGIN
    DELETE FROM service_category WHERE id_service = (SELECT id FROM service WHERE name = service_identifier) AND id_category = (SELECT id FROM servicecategory WHERE name = category_identifier); 
END;

/*****************************************************************************/
