USE $MAIN$;

/*****************************************************************************/

-- Update auth umsso ... \
UPDATE auth SET type = 'Terradue.Authentication.Umsso.UmssoAuthenticationType, Terradue.Authentication' WHERE (identifier = 'umsso');
-- RESULT

/*****************************************************************************/