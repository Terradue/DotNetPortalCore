using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Terradue.Portal;
using Terradue.Util;

/*!
\defgroup Auth_Umsso EO-SSO plugin
@{

This module enables external authentication using EO-SSO mechanism. 
In the core, the \ref Context component provides with an interface
that allows using HTTP headers present in the HHTP context to authenticate the user.
Associated with a set of rules, the \ref Authentication is able to establish a protocol to authenticate user.

Typical code ruleset is declared with the method EO-SSO. accountType maps the rule to an account. 
The rule is applied only if the condition that specified that the header \c Umsso-Person-commonName
is present and not empty. Then the value present in \c Umsso-Person-commonName is used as login username
and user is registered automatically if not yet present in the database with register="true" 
and the user receives a account creation mail with the mail information found in header Umsso-Person-Email.

\ingroup Security


Following diagram depicts the User status when logging with EO-SSO.

\startuml "User login with EO-SSO activity diagram"

start
    if (secured service?) then (yes)
      if (EO-SSO logged?) then (yes)
        if (user in DB?) then (yes)
          if (user pending activation?) then (yes)
            :reinvite user to confirm email;
            stop
          endif 
        else (no)
          :create user account in db;
          :set account status to **Pending Activation**;
          :send confirmation email to user;
          :invite user to confirm email;
          stop
        endif
      else (no)
        :redirect user to EO-SSO IDP;
        stop
      endif
    endif
    :process service;
stop

\enduml


Next diagram depicts the scenarios that applies when a user perform an HTTP request to a web service protected by EO-SSO. This scenario is the “normal” case where user credentials are correct.

\startuml "EO-SSO protected HTTP request sequence diagram"

actor "User" as U
participant "Service Provider\ncheckpoint" as W
participant "Portal" as C
entity "EO-SSO Identity Provider" as I

autonumber

== EO-SSO authentication ==

U ->> W: HTTP request
activate W

alt "user not authenticated on EO-SSO"

W -->> U: HTTP redirect\nto IdP
deactivate W
activate U
U ->> I: login form URL
deactivate U
activate I
I -->> U: login form
deactivate I

U ->> I: username & password
activate I
I -> I:Authenticate user
I -->> U: user credentials (cookie, SAML token, validity period, redirection)
deactivate I

U -> U: Write cookie
U ->> W: HTTP redirect

end

activate W
W ->> I: check User attribute
activate I
I -->> W: Identity attributes in SAML
deactivate I
W -> W: Create a security context
W -->> U: HTTP redirection\nto original resources
deactivate W
activate U

== Web Server authentication ==

U ->> W: original HTTP request
deactivate U
activate W
W -> C: original HTTP request\n+ additional HTTP headers
deactivate W

activate C

C -> C: Read Authentication RuleSet
C -> C: Apply ruleset\nto HTTP Headers

alt "User not present in DB"

C -> C: Register new User\n(username, email)

end

C -> C: Initialize Local Context\nwith user space
C -> C: Perform request 

C --> W: HTTP response
deactivate C
W -->> U: HTTP response

\enduml

\xrefitem int "Interfaces" "Interfaces" implements \ref Authentication to enable EO-SSO Authentication.

\xrefitem norm "Normative References" "Normative References" EO op EO-SSO Interface Control Document [SIE-EO-OP-UM-SSO-ICD-002]

@}

*/

namespace Terradue.Umsso {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class UmssoUtils {
        
        /// <summary>Creates the instance on the cloud provider and stores the record in the local database.</summary>
        public static bool BindToUmssoIdp(IfyContext context, string idpUserAccount, string spUserAccount, string transactionId) {
            bool result = false;
            
            string url = context.GetConfigValue("UmssoIdpBindingUrl"); //"/home/umsso/sp/cert/portal-dev.gpod-sso.terradue.int.p12";
            //url = "http://portal-dev.gpod.terradue.int/analyze.aspx";
            string certFileName = context.GetConfigValue("UmssoSpCertFile"); //"/home/umsso/sp/cert/portal-dev.gpod-sso.terradue.int.p12";
            /*UmssoIdpServicesSOAP ws = new UmssoIdpServicesSOAP(url, certFileName);
            string result = ws.bind(idpUserAccount, spUserAccount, "b2cc8f0f-8fcf-479f-9153-93f7f6274596");
            context.AddInfo(result);*/
            try {
                HttpWebRequest request = GetSslRequest(url, "POST", "text/xml; charset=utf-8", certFileName);
                Stream requestStream = request.GetRequestStream();
                XmlTextWriter writer = new XmlTextWriter(requestStream, System.Text.Encoding.UTF8);
                
                writer.WriteStartDocument();
                writer.WriteStartElement("soap:Envelope");
                writer.WriteAttributeString("xmlns", "soap", null, "http://schemas.xmlsoap.org/soap/envelope/");
                //writer.WriteNamespaceDefinition("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                writer.WriteAttributeString("soap:encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/");
                writer.WriteStartElement("soap:Body");
                writer.WriteAttributeString("xmlns", "m", null, "http://interfaces.soap.umsso20.sde.siemens.com");
                //writer.WriteNamespaceDefinition("m", "http://interfaces.soap.umsso20.sde.siemens.com");
                writer.WriteStartElement("m:bind");
                writer.WriteElementString("m:args0", idpUserAccount);
                writer.WriteElementString("m:args1", spUserAccount);
                writer.WriteElementString("m:args2", transactionId);
                writer.WriteEndElement(); // </m:bind>
                writer.WriteEndElement(); // </soap:Body>
                writer.WriteEndElement(); // </soap:Envelope>
                writer.Close();
                requestStream.Close();

                // Get response stream.
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                response.Close();

                XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                nsm.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                nsm.AddNamespace("ns1", "http://interfaces.soap.umsso20.sde.siemens.com");
                nsm.AddNamespace("bind", "bind.xmlbeans.configuration.umsso20.sde.siemens.com");
                XmlElement responseElem = doc.SelectSingleNode("soapenv:Envelope/soapenv:Body/ns1:bindResponse/ns1:return", nsm) as XmlElement;
                if (responseElem == null) throw new Exception("Invalid response from binding service");
                doc.LoadXml(responseElem.InnerText); // encoded XML inside XML
                XmlElement codeElem = doc.SelectSingleNode("bind:bindServiceResponse/bind:code", nsm) as XmlElement;
                XmlElement messageElem = doc.SelectSingleNode("bind:bindServiceResponse/bind:message", nsm) as XmlElement;
                if (codeElem != null && codeElem.InnerText == "UMSSO_MSG_OK") result = true;
                else if (messageElem != null) throw new Exception(messageElem.InnerText);
                
                context.AddInfo(doc.OuterXml);

            } catch (Exception e) {
                context.LogError(context, e.Message);
                return false;
            }
            
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static HttpWebRequest GetSslRequest(string url, string method, string contentType, string certFileName) {
            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            if (contentType != null) request.ContentType = contentType;
            
            request.ClientCertificates.Add(new X509Certificate2(certFileName, String.Empty, X509KeyStorageFlags.DefaultKeySet));

            ServicePointManager.ServerCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
                return true;
            };
            
            return request;
        }
        
        
    }
    
}

