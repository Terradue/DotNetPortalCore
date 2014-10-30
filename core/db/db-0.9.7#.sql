/*****************************************************************************/

-- Adding configuration variable for mail sender display name ... \
SET @section = (SELECT id FROM configsection WHERE caption='Server');
SET @mail = (SELECT value FROM config WHERE name='MailSender');
SET @pos1=LOCATE('<', @mail);
SET @pos2=LOCATE('>', @mail);

UPDATE config SET pos=pos+1 WHERE id_section=@section AND pos>=5;
-- NORESULT
INSERT INTO config (id_section, pos, name, type, source, caption, hint, value, optional) VALUES
    (@section, 5, 'MailSenderAddress', 'email', NULL, 'Mail Sender Address', 'Enter the default sender address for e-mails (e.g. alerts for administrators or account registration mails for users)', CASE WHEN @pos1!=0 AND @pos2>@pos1 THEN SUBSTRING(@mail, @pos1+1, @pos2-@pos1-1) ELSE @mail END, false)
;
UPDATE config SET caption='Mail Sender Display Name', hint='Enter the default sender display name for e-mails (e.g. alerts for administrators or account registration mails for users)', value=CASE WHEN @pos1>1 THEN TRIM(SUBSTRING(@mail, 1, @pos1-1)) ELSE @mail END WHERE name='MailSender';
-- RESULT

/*****************************************************************************/
