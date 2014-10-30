/*****************************************************************************/

-- Changing structure of table "serviceconfig" ... \
ALTER TABLE action
    ADD COLUMN immediate boolean COMMENT 'Execute immediately in next agent cycle' AFTER next_execution_time
;
-- RESULT


-- Changing structure of table "serviceconfig" ... \
DELETE FROM serviceconfig WHERE id_service IS NOT NULL AND id_service NOT IN (SELECT id FROM service);
ALTER TABLE serviceconfig ADD FOREIGN KEY fk_serviceconfig_service (id_service) REFERENCES service(id) ON DELETE CASCADE;
ALTER TABLE serviceconfig ADD INDEX idx_serviceconfig_name (id_service, name);
-- RESULT



/*****************************************************************************/

CREATE TABLE wpstask (
    id_task int unsigned NOT NULL COMMENT 'FK: Related task',
    id_application int unsigned COMMENT 'FK: WPS application',
    template_name varchar(50) NOT NULL COMMENT 'Task template name',
    store boolean COMMENT 'Value of "storeExecuteResponse" attribute',
    status boolean COMMENT 'Value of "status" attribute',
    lineage_xml text COMMENT 'XML to be included in response if "lineage" attribute was "true"',
    CONSTRAINT pk_wpstask PRIMARY KEY (id_task),
    CONSTRAINT fk_wpstask_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpstask_application FOREIGN KEY (id_application) REFERENCES application(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Additional fields for tasks created by a WPS';

/*****************************************************************************/
