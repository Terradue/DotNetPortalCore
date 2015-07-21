USE $MAIN$;

/*****************************************************************************/

CREATE TABLE activity (
    id int unsigned NOT NULL auto_increment,
    id_entity int unsigned COMMENT 'Entity associated to the activity',
    id_usr int unsigned COMMENT 'User doing the activity',
    id_priv int unsigned COMMENT 'Privilege associated',
    id_type int unsigned COMMENT 'Entity type',
    id_owner int unsigned COMMENT 'User owning the entity related to the activity',
    log_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of activity creation',
    CONSTRAINT pk_activity PRIMARY KEY (id),
    INDEX (`id_usr`),
    INDEX (`log_time`)
) Engine=InnoDB COMMENT 'User activities';
-- RESULT

CREATE TABLE priv_score (
    id_priv int unsigned COMMENT 'FK: Privilege associated',
    score_usr int unsigned COMMENT 'Score associated to a user',
    score_owner int unsigned COMMENT 'Score associated to an owner',
    CONSTRAINT fk_priv_score_priv FOREIGN KEY (id_priv) REFERENCES priv(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Privilege scores';
-- RESULT

ALTER TABLE priv
ADD COLUMN enable_log boolean NOT NULL default false COMMENT 'If true, activity related to this privilege are logged';
-- RESULT

/*****************************************************************************/

