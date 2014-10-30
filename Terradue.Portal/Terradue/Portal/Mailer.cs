using System;
using System.Net.Mail;
using System.Collections.Generic;
using Terradue.Util;
using System.Net.Mime;



namespace Terradue.Portal {

	public class Mailer {

		protected IfyContext context;

        //---------------------------------------------------------------------------------------------------------------------

		public Mailer(IfyContext context) {
			this.context=context;
		}

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Use Send (upper-case S)")]
        public bool send(string subject, string body, List <string> to, List <string> cc, List <string> bcc) {
            return Send(subject, body, to, cc, bcc);
        }

        //---------------------------------------------------------------------------------------------------------------------

		// subject + body + adressee + ccAdressee + ccnAdressee
		public bool Send(string subject, string body, List<string> to, List<string> cc, List<string> bcc) {
			return Send(subject, body, to, cc, bcc, null);
		}

        //---------------------------------------------------------------------------------------------------------------------

        public bool Send(string subject, string body, List<string> to, List<string> cc, List<string> bcc, string contentType) {
            string smtpHostname = context.GetConfigValue("SmtpHostname");
            string smtpUsername = context.GetConfigValue("SmtpUsername");
            string smtpPassword = context.GetConfigValue("SmtpPassword");
            string mailSenderAddress = context.GetConfigValue("MailSenderAddress");
            string mailSender = context.GetConfigValue("MailSender");
            if (mailSender == null) mailSender = mailSenderAddress;

            if (smtpHostname == null || smtpUsername == null || mailSenderAddress == null) {
                string message = String.Format("Invalid mailing settings: smtpHostname = {0}\tsmtpUsername = {1}\t,mailSenderAddress = {2}", smtpHostname, smtpUsername, mailSenderAddress);
                context.LogError(this, message);
                throw new ArgumentNullException(message);
            }

            // create the mail and setting parameters
            MailMessage mail = new MailMessage();

            mail.Body = body;
            mail.Subject = subject;
            mail.From = new MailAddress(mailSenderAddress, mailSender);

            // Add main recipients
            foreach (string recipient in to) mail.To.Add(new MailAddress(recipient, recipient));

            // Add CC recipients
            if (cc != null) {
                foreach (string recipient in cc) mail.CC.Add(new MailAddress(recipient, recipient));
            }

            // Add BCC recipients
            if (bcc != null) {
                foreach (string recipient in bcc) mail.Bcc.Add(new MailAddress(recipient, recipient));
            }

            SmtpClient client = new SmtpClient(smtpHostname);

            // Add credentials if the SMTP server requires them.
            client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);

            // Add alternate view if content type is defined
            if (contentType != null) {
                AlternateView alternateView = AlternateView.CreateAlternateViewFromString(body, new ContentType(contentType));
                mail.AlternateViews.Add(alternateView);
            }

            try {
                client.Send(mail);
                return true;
            } catch (Exception e) {
                string message;
                if (e.Message.Contains("CDO.Message") || e.Message.Contains("535")) {
                    message = "Mail could not be sent, this is a site administration issue (probably caused by an invalid SMTP hostname or wrong SMTP server credentials)";
                } else {
                    message = String.Format("Mail could not be sent, this is a site administration issue: {0}{1}", e.Message, e.InnerException == null ? String.Empty : String.Format("({0})", e.InnerException.Message));
                }
                context.AddError(message);
                context.LogError(this, message);
                throw;
            }
        }
	}

}

