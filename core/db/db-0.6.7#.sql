/*****************************************************************************/

-- Changing value placeholders ... \
UPDATE config SET hint=REPLACE(hint, '$SERVICEROOT', '$(SERVICEROOT)'), value=REPLACE(value, '$SERVICEROOT', '$(SERVICEROOT)') WHERE name='ServiceFileRoot'; 
UPDATE config SET hint=REPLACE(hint, '$PARAMROOT', '$(PARAMROOT)'), value=REPLACE(value, '$PARAMROOT', '$(PARAMROOT)') WHERE name='ServiceParamFileRoot'; 
UPDATE config SET hint=REPLACE(REPLACE(REPLACE(REPLACE(hint, '$USERNAME', '$(USERNAME)'), '$PASSWORD', '$(PASSWORD)'), '$SERVICES', '$(SERVICES)'), '$SERIES', '$(SERIES)'), value=REPLACE(REPLACE(REPLACE(REPLACE(value, '$USERNAME', '$(USERNAME)'), '$PASSWORD', '$(PASSWORD)'), '$SERVICES', '$(SERVICES)'), '$SERIES', '$(SERIES)') WHERE name IN ('RegistrationMailBody', 'PasswordResetMailBody'); 
UPDATE config SET hint=REPLACE(REPLACE(REPLACE(hint, '$SESSION', '$(SESSION)'), '$UID', '$(UID)'), '$JOB', '$(JOB)'), value=REPLACE(REPLACE(REPLACE(value, '$SESSION', '$(SESSION)'), '$UID', '$(UID)'), '$JOB', '$(JOB)') WHERE name IN ('TaskDetailsUrl', 'JobDetailsUrl', 'PortalPublishUrl'); 
UPDATE config SET hint=REPLACE(hint, '$CATALOGUE', '$(CATALOGUE)'), value=REPLACE(value, '$CATALOGUE', '$(CATALOGUE)') WHERE name='DefaultCatalogueBaseUrl';  

ALTER TABLE pubserver ADD COLUMN temp_path varchar(100), ADD COLUMN temp_upload_url varchar(200), ADD COLUMN temp_download_url varchar(200);
UPDATE pubserver SET temp_path=path, temp_upload_url=upload_url, temp_download_url=download_url;
UPDATE pubserver SET path=REPLACE(temp_path, '$UID', '$(UID)'), upload_url=REPLACE(temp_upload_url, '$UID', '$(UID)'), download_url=REPLACE(temp_download_url, '$UID', '$(UID)');
ALTER TABLE pubserver DROP COLUMN temp_path, DROP COLUMN temp_upload_url, DROP COLUMN temp_download_url;

ALTER TABLE series ADD COLUMN temp_cat_description varchar(200), ADD COLUMN temp_cat_template varchar(1000);
UPDATE series SET temp_cat_description=cat_description, temp_cat_template=cat_template;
UPDATE series SET cat_description=REPLACE(temp_cat_description, '$CATALOGUE', '$(CATALOGUE)'), cat_template=REPLACE(temp_cat_template, '$CATALOGUE', '$(CATALOGUE)');
ALTER TABLE series DROP COLUMN temp_cat_description, DROP COLUMN temp_cat_template;

ALTER TABLE service ADD COLUMN temp_logo_url varchar(200), ADD COLUMN temp_view_url varchar(200), ADD COLUMN temp_root varchar(200);
UPDATE service SET temp_logo_url=logo_url, temp_view_url=view_url, temp_root=root;
UPDATE service SET logo_url=REPLACE(temp_logo_url, '$SERVICEROOT', '$(SERVICEROOT)'), view_url=REPLACE(temp_view_url, '$SERVICEROOT', '$(SERVICEROOT)'), root=REPLACE(temp_root, '$SERVICEROOT', '$(SERVICEROOT)');
ALTER TABLE service DROP COLUMN temp_logo_url, DROP COLUMN temp_view_url, DROP COLUMN temp_root;
ALTER TABLE service
    CHANGE COLUMN root root varchar(200) NOT NULL default 'Root dir of a single service (location of service.xml), use $(SERVICEROOT) as placeholder for service root folder'
;
-- RESULT

/*****************************************************************************/
