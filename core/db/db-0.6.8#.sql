/*****************************************************************************/

-- Changing structure of table "config" ... \
ALTER TABLE config
    DROP FOREIGN KEY config_ibfk_2,
    DROP INDEX id_section,
    ADD CONSTRAINT fk_config_section FOREIGN KEY (id_section) REFERENCES configsection(id) ON DELETE SET NULL
;
-- RESULT

-- Changing structure of table "usrsession" ... \
ALTER TABLE usrsession
    DROP FOREIGN KEY usrsession_ibfk_1,
    DROP INDEX id_usr,
    ADD CONSTRAINT fk_usrsession_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "pubserver" ... \
ALTER TABLE pubserver
    DROP FOREIGN KEY pubserver_ibfk_1,
    DROP INDEX id_usr,
    ADD CONSTRAINT fk_pubserver_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "filter" ... \
ALTER TABLE filter
    DROP FOREIGN KEY filter_ibfk_1,
    DROP INDEX id_usr,
    ADD CONSTRAINT fk_fiter_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "usr_grp" ... \
ALTER TABLE usr_grp
    DROP FOREIGN KEY usr_grp_ibfk_1,
    DROP FOREIGN KEY usr_grp_ibfk_2,
    DROP INDEX id_grp,
    ADD CONSTRAINT fk_usr_grp_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_usr_grp_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "ce" ... \
ALTER TABLE ce
    DROP FOREIGN KEY ce_ibfk_1,
    DROP INDEX id_ce_monitor,
    ADD CONSTRAINT fk_ce_ce_monitor FOREIGN KEY (id_ce_monitor) REFERENCES ce(id) ON DELETE SET NULL
;
-- RESULT

-- Changing structure of table "cedir" ... \
ALTER TABLE cedir
    DROP FOREIGN KEY cedir_ibfk_1,
    DROP INDEX id_ce,
    ADD CONSTRAINT fk_cedir_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "product" ... \
ALTER TABLE product
    DROP FOREIGN KEY product_ibfk_1,
    DROP FOREIGN KEY product_ibfk_2,
    DROP INDEX id_producttype,
    DROP INDEX id_ce,
    ADD CONSTRAINT fk_product_producttype FOREIGN KEY (id_producttype) REFERENCES producttype(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_product_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "productdata" ... \
ALTER TABLE productdata
    DROP FOREIGN KEY productdata_ibfk_1,
    DROP INDEX id_product,
    ADD CONSTRAINT fk_productdata_product FOREIGN KEY (id_product) REFERENCES product(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "service" ... \
ALTER TABLE service
    DROP FOREIGN KEY service_ibfk_1,
    DROP INDEX id_class,
    ADD CONSTRAINT fk_service_class FOREIGN KEY (id_class) REFERENCES serviceclass(id) ON DELETE SET NULL
;
-- RESULT
    
-- Changing structure of table "service_category" ... \
ALTER TABLE service_category
    DROP FOREIGN KEY service_category_ibfk_1,
    DROP FOREIGN KEY service_category_ibfk_2,
    DROP INDEX id_category,
    ADD CONSTRAINT fk_service_category_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_service_category_category FOREIGN KEY (id_category) REFERENCES servicecategory(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "serviceconfig" ... \
ALTER TABLE serviceconfig
    DROP FOREIGN KEY serviceconfig_ibfk_1,
    DROP FOREIGN KEY serviceconfig_ibfk_2,
    DROP INDEX id_grp,
    DROP INDEX id_usr,
    ADD CONSTRAINT fk_serviceconfig_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_serviceconfig_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "service_series" ... \
ALTER TABLE service_series
    DROP FOREIGN KEY service_series_ibfk_1,
    DROP FOREIGN KEY service_series_ibfk_2,
    DROP INDEX id_series,
    ADD CONSTRAINT fk_service_series_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_service_series_series FOREIGN KEY (id_series) REFERENCES series(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "service_ce" ... \
ALTER TABLE service_ce
    DROP FOREIGN KEY service_ce_ibfk_1,
    DROP FOREIGN KEY service_ce_ibfk_2,
    DROP INDEX id_ce,
    ADD CONSTRAINT fk_service_ce_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_service_ce_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "privilege" ... \
ALTER TABLE privilege
    DROP FOREIGN KEY privilege_ibfk_1,
    DROP FOREIGN KEY privilege_ibfk_2,
    DROP FOREIGN KEY privilege_ibfk_3,
    DROP FOREIGN KEY privilege_ibfk_4,
    DROP FOREIGN KEY privilege_ibfk_5,
    DROP FOREIGN KEY privilege_ibfk_6,
    DROP INDEX id_grp,
    DROP INDEX id_usr,
    DROP INDEX id_series,
    DROP INDEX id_producttype,
    DROP INDEX id_service,
    DROP INDEX id_ce,
    ADD CONSTRAINT fk_privilege_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_privilege_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_privilege_series FOREIGN KEY (id_series) REFERENCES series(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_privilege_producttype FOREIGN KEY (id_producttype) REFERENCES producttype(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_privilege_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_privilege_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "taskgroup" ... \
ALTER TABLE taskgroup
    DROP FOREIGN KEY taskgroup_ibfk_1,
    DROP INDEX id_application,
    ADD CONSTRAINT fk_taskgroup_application FOREIGN KEY (id_application) REFERENCES application(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "scheduler" ... \
ALTER TABLE scheduler
    DROP FOREIGN KEY scheduler_ibfk_1,
    DROP FOREIGN KEY scheduler_ibfk_2,
    DROP FOREIGN KEY scheduler_ibfk_3,
    DROP FOREIGN KEY scheduler_ibfk_4,
    DROP FOREIGN KEY scheduler_ibfk_6,
    DROP INDEX id_usr,
    DROP INDEX id_service,
    DROP INDEX id_ce,
    DROP INDEX id_pubserver,
    DROP INDEX id_class,
    ADD CONSTRAINT fk_scheduler_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_scheduler_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_scheduler_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_scheduler_pubserver FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_scheduler_class FOREIGN KEY (id_class) REFERENCES schedulerclass(id) ON DELETE SET NULL
;
-- RESULT

-- Changing structure of table "schedulerparam" ... \
ALTER TABLE schedulerparam
    DROP FOREIGN KEY schedulerparam_ibfk_1,
    DROP INDEX id_scheduler,
    ADD CONSTRAINT fk_schedulerparam_scheduler FOREIGN KEY (id_scheduler) REFERENCES scheduler(id) ON DELETE CASCADE
;
-- RESULT
    
-- Changing structure of table "schedulerloop" ... \
ALTER TABLE schedulerloop
    DROP FOREIGN KEY schedulerloop_ibfk_1,
    DROP INDEX id_scheduler,
    ADD CONSTRAINT fk_schedulerloop_scheduler FOREIGN KEY (id_scheduler) REFERENCES scheduler(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "task" ... \
ALTER TABLE task
    DROP FOREIGN KEY task_ibfk_1,
    DROP FOREIGN KEY task_ibfk_2,
    DROP FOREIGN KEY task_ibfk_3,
    DROP FOREIGN KEY task_ibfk_4,
    DROP FOREIGN KEY task_ibfk_5,
    DROP FOREIGN KEY task_ibfk_6,
    DROP INDEX id_usr,
    DROP INDEX id_service,
    DROP INDEX id_ce,
    DROP INDEX id_pubserver,
    DROP INDEX id_scheduler,
    DROP INDEX id_taskgroup,
    ADD CONSTRAINT fk_task_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_task_service FOREIGN KEY (id_service) REFERENCES service(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_task_ce FOREIGN KEY (id_ce) REFERENCES ce(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_task_pubserver FOREIGN KEY (id_pubserver) REFERENCES pubserver(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_task_scheduler FOREIGN KEY (id_scheduler) REFERENCES scheduler(id) ON DELETE SET NULL,
    ADD CONSTRAINT fk_task_taskgroup FOREIGN KEY (id_taskgroup) REFERENCES taskgroup(id) ON DELETE SET NULL
;
-- RESULT

-- Changing structure of table "taskpart" ... \
ALTER TABLE taskpart
    DROP FOREIGN KEY taskpart_ibfk_1,
    DROP INDEX id_task,
    ADD CONSTRAINT fk_taskpart_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "job" ... \
ALTER TABLE job
    DROP FOREIGN KEY job_ibfk_1,
    DROP INDEX id_task,
    ADD CONSTRAINT fk_job_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "jobnode" ... \
ALTER TABLE jobnode
    DROP FOREIGN KEY jobnode_ibfk_1,
    ADD CONSTRAINT fk_jobnode_job FOREIGN KEY (id_job) REFERENCES job(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "jobdependency" ... \
ALTER TABLE jobdependency
    DROP FOREIGN KEY jobdependency_ibfk_1,
    DROP FOREIGN KEY jobdependency_ibfk_2,
    DROP INDEX id_job,
    DROP INDEX id_job_input,
    ADD CONSTRAINT fk_jobdependency_job FOREIGN KEY (id_job) REFERENCES job(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_jobdependency_job_input FOREIGN KEY (id_job_input) REFERENCES job(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "taskparam" ... \
ALTER TABLE taskparam
    DROP FOREIGN KEY taskparam_ibfk_1,
    DROP FOREIGN KEY taskparam_ibfk_2,
    DROP INDEX id_task,
    DROP INDEX id_job,
    ADD INDEX idx_taskparam_task (id_task DESC, name),
    ADD INDEX idx_taskparam_job (id_job DESC, name),
    ADD CONSTRAINT fk_taskparam_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_taskparam_job FOREIGN KEY (id_job) REFERENCES job(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "temporaltask" ... \
ALTER TABLE temporaltask
    DROP FOREIGN KEY temporaltask_ibfk_1,
    ADD CONSTRAINT fk_temporaltask_task FOREIGN KEY (id_task) REFERENCES task(id) ON DELETE CASCADE
;
-- RESULT

-- Changing structure of table "lookup" ... \
ALTER TABLE lookup
    DROP FOREIGN KEY lookup_ibfk_1,
    DROP INDEX id_list,
    ADD CONSTRAINT fk_lookup_list FOREIGN KEY (id_list) REFERENCES lookuplist(id) ON DELETE CASCADE
;
-- RESULT
    
/*****************************************************************************/
