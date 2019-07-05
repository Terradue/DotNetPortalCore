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

/*****************************************************************************/

-- Create table gitrepo ... \
CREATE TABLE gitrepo (
    id int unsigned NOT NULL auto_increment,
    url VARCHAR(300) NOT NULL COMMENT 'Git Repository URL',
    id_usr int unsigned COMMENT 'User creating the repo',
    id_domain int unsigned COMMENT 'Domain of the repo',
    kind int unsigned COMMENT 'Git repo kind (gitlab, github, ...)',
    CONSTRAINT pk_gitrepo PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'GIT repository';
-- RESULT 

/*****************************************************************************/

-- Create table gitrepo_perm ... \
CREATE TABLE gitrepo_perm (
    id_gitrepo int unsigned NOT NULL COMMENT 'FK: Git repository',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_gitrepo_perm_gitrepo FOREIGN KEY (id_gitrepo) REFERENCES gitrepo(id) ON DELETE CASCADE,
    CONSTRAINT fk_gitrepo_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_gitrepo_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on GIT repository';
-- RESULT 

/*****************************************************************************/

