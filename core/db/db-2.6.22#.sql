USE $MAIN$;

/*****************************************************************************/

CREATE TABLE safe (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned COMMENT 'FK: Owning user (optional)',
    public_key varchar(10000) COMMENT 'Public key',
    private_key varchar(10000) COMMENT 'Private key',
    creation_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of safe creation',
    update_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of safe creation',
    CONSTRAINT pk_safe PRIMARY KEY (id),
    CONSTRAINT fk_safe_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Sets of safes';

/*****************************************************************************/

CREATE TABLE safe_priv (
    id_safe int unsigned NOT NULL COMMENT 'FK: Safe',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_safe_priv_safe FOREIGN KEY (id_safe) REFERENCES safe(id) ON DELETE CASCADE,
    CONSTRAINT fk_safe_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_safe_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on safe';
-- RESULT

/*****************************************************************************/
