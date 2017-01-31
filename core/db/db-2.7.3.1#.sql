USE $MAIN$;

/*****************************************************************************/

-- Update config for email body/subjects ... \
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('EmailDuplicateAccountSubject', 'string', 'Duplicate account email body', 'Duplicate account email body', '[$(SITENAME)] - Duplicate account', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('EmailDuplicateAccountBody', 'string', 'Duplicate account email subject', 'Duplicate account email subject', 'Dear user,\n\nwe send you this email because you recently registered on our portal using the account \'$(USERNAME_NEW)\' with an email address already associated to the account of reference \'$(USERNAME_OLD)\'.\nSecondary accounts cannot be activated and we kindly ask you to use instead your account of reference \'$(USERNAME_OLD)\'.\nIf you need to recover your access to the account of reference \'$(USERNAME_OLD)\’, go to the ESA SSO portal (https://eo-sso-idp.eo.esa.int/idp/umsso20/admin) and proceed according to https://eo-sso-idp.eo.esa.int/idp/umsso20/registration?faq.\nIn case you are willing to dismiss your reference account, please contact $(CONTACT_EMAIL) keeping this email in reference.\n\nWith our best regards\nThe Operations Support team at Terradue', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('ContactEmail', 'string', 'Contact email', 'Contact email', 'support@terradue.com', '0');
-- RESULT