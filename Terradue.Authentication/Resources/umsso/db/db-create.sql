-- VERSION 1.5

USE $MAIN$;

/*****************************************************************************/

-- Adding configuration variables for UM-SSO ... \
INSERT INTO configsection (name, pos) VALUES
    ('UM-SSO', 8)
;

SET @section = (SELECT LAST_INSERT_ID());

INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section, 1, 'UmssoIdpBindingUrl', 'string', NULL, 'UM-SSO IDP Binding Web Service Endpoint', 'Enter the SOAP endpoint URL of the binding web service', NULL, true),
    (@section, 2, 'UmssoSpCertFile', 'string', NULL, 'Location of the UM-SSO SP Certificate File (P12)', 'Enter the location of the PKCS12-formatted UM-SSO SP certificate file to be used for authentication with the UM-SSO IDP', NULL, true)
;

SET @pos = (SELECT MAX(pos) FROM auth);
INSERT INTO auth (pos, identifier, name, description, type, enabled) VALUES 
    (@pos + 1, 'umsso', 'UM-SSO authentication', 'UM-SSO authentication allows users to identify themselves using ESA um-sso provider.', 'Terradue.Authentication.Umsso.UmssoAuthenticationType, Terradue.Authentication', '1')
;
-- RESULT

/*****************************************************************************/
