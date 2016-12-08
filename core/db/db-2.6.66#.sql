USE $MAIN$;

/*****************************************************************************/
CREATE TABLE cookie (
    session VARCHAR(100) NOT NULL COMMENT 'Session',
    identifier VARCHAR(100) NOT NULL COMMENT 'Identifier',
    value TEXT NULL COMMENT 'Value',
    expire datetime,    
    creation_date datetime,
    UNIQUE INDEX (session,identifier)
) Engine=InnoDB COMMENT 'DB Cookies';
-- RESULT 

/*****************************************************************************/
