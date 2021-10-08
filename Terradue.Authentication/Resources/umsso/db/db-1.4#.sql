/*****************************************************************************/
-- add auth entry
INSERT INTO auth (`identifier`, `name`, `description`, `type`, `enabled`) VALUES ('umsso', 'UM-SSO authentication', 'UM-SSO authentication allows users to identify themselves using ESA um-sso provider.', 'Terradue.Umsso.UmssoAuthenticationType, Terradue.Umsso', '1');

-- RESULT
/*****************************************************************************/
