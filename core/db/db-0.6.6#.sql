/*****************************************************************************/

-- Changing value placeholders ... \
UPDATE config SET hint=REPLACE(hint, '$SERVICEROOT', '$(SERVICEROOT)'), value=REPLACE(value, '$SERVICEROOT', '$(SERVICEROOT)') WHERE name='ServiceFileRoot'; 
UPDATE config SET hint=REPLACE(hint, '$PARAMROOT', '$(PARAMROOT)'), value=REPLACE(value, '$PARAMROOT', '$(PARAMROOT)') WHERE name='ServiceParamFileRoot'; 
UPDATE config SET hint=REPLACE(REPLACE(REPLACE(REPLACE(hint, '$USERNAME', '$(USERNAME)'), '$PASSWORD', '$(PASSWORD)'), '$SERVICES', '$(SERVICES)'), '$SERIES', '$(SERIES)'), value=REPLACE(REPLACE(REPLACE(REPLACE(value, '$USERNAME', '$(USERNAME)'), '$PASSWORD', '$(PASSWORD)'), '$SERVICES', '$(SERVICES)'), '$SERIES', '$(SERIES)') WHERE name IN ('RegistrationMailBody', 'PasswordResetMailBody'); 
UPDATE config SET hint=REPLACE(REPLACE(REPLACE(hint, '$SESSION', '$(SESSION)'), '$UID', '$(UID)'), '$JOB', '$(JOB)'), value=REPLACE(REPLACE(REPLACE(value, '$SESSION', '$(SESSION)'), '$UID', '$(UID)'), '$JOB', '$(JOB)') WHERE name IN ('TaskDetailsUrl', 'JobDetailsUrl', 'PortalPublishUrl'); 
UPDATE config SET hint=REPLACE(hint, '$CATALOGUE', '$(CATALOGUE)'), value=REPLACE(value, '$CATALOGUE', '$(CATALOGUE)') WHERE name='DefaultCatalogueBaseUrl'; 

UPDATE pubserver SET path=REPLACE(path, '$UID', '$(UID)'), upload_url=REPLACE(upload_url, '$UID', '$(UID)'), download_url=REPLACE(download_url, '$UID', '$(UID)');
UPDATE series SET cat_description=REPLACE(cat_description, '$CATALOGUE', '$(CATALOGUE)'), cat_template=REPLACE(cat_template, '$CATALOGUE', '$(CATALOGUE)');
UPDATE service SET logo_url=REPLACE(logo_url, '$SERVICEROOT', '$(SERVICEROOT)'), view_url=REPLACE(view_url, '$SERVICEROOT', '$(SERVICEROOT)'), root=REPLACE(root, '$SERVICEROOT', '$(SERVICEROOT)');

ALTER TABLE service
    CHANGE COLUMN root root varchar(200) NOT NULL default 'Root dir of a single service (location of service.xml), use $(SERVICEROOT) as placeholder for service root folder'
;
-- RESULT

/*****************************************************************************/
