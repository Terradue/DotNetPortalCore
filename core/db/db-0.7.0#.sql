/*****************************************************************************/

-- TODO Execute the following query on the portal database:\nUPDATE schedulerparam SET value=REPLACE(value, '$EXECDATE', '$(EXECDATE)');

-- Adding action for Computing Element status refresh ... \
INSERT INTO action (pos, name, caption, description) VALUES
    (6, 'ce', 'Computing Element status refresh', 'This action refreshes the information on the capacity and load of Computing Elements.')
;
-- RESULT

/*****************************************************************************/

CREATE TABLE cestate (
    id_ce int unsigned NOT NULL COMMENT 'Computing Element (PK+FK)',
    total_nodes int unsigned COMMENT 'Total capacity available on Computing Element',
    free_nodes int unsigned COMMENT 'Free capacity available on Computing Element',
    modified datetime,
    CONSTRAINT pk_cestate PRIMARY KEY (id_ce),
    CONSTRAINT fk_cestate_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Computing Element state information';

CREATE TRIGGER cestate_insert BEFORE INSERT ON cestate FOR EACH ROW
BEGIN
    SET NEW.modified=utc_timestamp();
END;

CREATE TRIGGER cestate_update BEFORE UPDATE ON cestate FOR EACH ROW
BEGIN
    SET NEW.modified=utc_timestamp();
END;

/*****************************************************************************/

-- Changing triggers (use UTC timestamp) ... \
DROP TRIGGER IF EXISTS service_insert;
DROP TRIGGER IF EXISTS service_update;

CREATE TRIGGER service_insert BEFORE INSERT ON service FOR EACH ROW
BEGIN
    SET NEW.created=utc_timestamp(), NEW.modified=utc_timestamp();
END;

CREATE TRIGGER service_update BEFORE UPDATE ON service FOR EACH ROW
BEGIN
    SET NEW.modified=utc_timestamp();
END;
-- RESULT

/*****************************************************************************/
