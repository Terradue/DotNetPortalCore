using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Terradue.Security;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    [EntityTable("openidprovider", EntityTableConfiguration.Custom, NameField = "name")]
    public class OpenIdProvider : Entity {
        
        private bool invalidPattern;

        //---------------------------------------------------------------------------------------------------------------------

        public bool UseOpenIdProviderIdentifier {
            get { return OpenIdProviderIdentifier != null || EndpointUrl != null; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("op_identifier")]
        private string OpenIdProviderIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("endpoint_url")]
        private string EndpointUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("pattern")]
        private string ConversionPattern { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("input_caption")]
        public string InputCaption { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("icon_url")]
        private string IconUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool SignInList { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool AllowAll { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool SignInForm { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AssignOpenId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance.</summary>
        /*!
        /// <param name="context">the execution environment context</param>
        */
        public OpenIdProvider(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance.</summary>
        /*!
        /// <param name="context">the execution environment context</param>
        /// <returns>the created User object</returns>
        */
        public static new OpenIdProvider GetInstance(IfyContext context) {
            return new OpenIdProvider(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance with the specified data structure.</summary>
        /*!
        /// <param name="context">the execution environment context</param>
        /// <param name="data">the entity data structure information</param>
        /// <returns>the created User object</returns>
        */
        public static new OpenIdProvider GetInstance(IfyContext context, EntityData data) {
            OpenIdProvider result = OpenIdProvider.GetInstance(context);
            result.Data = data;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance representing the user with the specified ID.</summary>
        /*!
        /// <param name="context">the execution environment context</param>
        /// <param name="id">the user ID</param>
        /// <returns>the created User object</returns>
        */
        public static OpenIdProvider FromId(IfyContext context, int id) {
            OpenIdProvider result = new OpenIdProvider(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the default data structure for the user entity.</summary>
        protected override void SetStructure() {
            int openIdAuth = webContext.AllowOpenId;

            if (SignInList) {
                ListUrl = String.Format("{0}?{1}=list", webContext.ScriptName, IfyWebContext.OperationParameterName); 
                Paging = false;
            }
            if (SignInList || SignInForm) {
                ShowItemIds = false;
                ShowItemLinks = false;
                CanCreate = false;
                CanModify = false;
                CanDelete = false;
            }
            
            if (SignInForm) {
                if (openIdAuth == 0) context.ReturnError("OpenID authentication not permitted");
                Data = new EntityData(
                        AssignOpenId ? "Assign OpenID to account" : "Sign in with an OpenID",
                        null,
                        new FieldExpression[] {
                            new SingleValueField("openid", null, "identifier", InputCaption, null, FieldFlags.Item)
                        }
                );
                if (AssignOpenId) AddOperation(OperationType.Other, "assign", "Assign OpenID", "POST", "_request=assign", null);
                else AddOperation(OperationType.Other, "signin", "Sign in", "POST", "_request=signin", null);

            } else {
                if (openIdAuth == 0) context.AddWarning("OpenID authentication not permitted");
                string linkSql = String.Format("CONCAT({0}, '?{1}={2}&provider=', t.id)",
                        StringUtils.EscapeSql(webContext.ScriptName),
                        IfyWebContext.OperationParameterName,
                        context.IsUserAuthenticated ? "assign" : "signin"
                );
                
                Data = new EntityData(
                        "OpenID provider",
                        "openidprovider",
                        new FieldExpression[] {
                            new SingleValueExpression("link", linkSql, "url", null, null, FieldFlags.List | FieldFlags.Attribute | (SignInList ? 0 : FieldFlags.Hidden)),
                            new SingleValueExpression("input", "t.endpoint_url IS NULL AND t.op_identifier IS NULL", "bool", null, null, FieldFlags.List | FieldFlags.Attribute | (SignInList ? 0 : FieldFlags.Hidden)),
                            new SingleValueField("caption", "name", "caption", "Caption", null, FieldFlags.Both),
                            new SingleValueField("opIdentifier", "op_identifier", "url", "Identifier (\"OP Identifier\")", null, FieldFlags.Item | FieldFlags.Optional),
                            new SingleValueField("endpointUrl", "endpoint_url", "url", "Endpoint URL (\"OP Endpoint URL\")", null, FieldFlags.Item | FieldFlags.Optional),
                            new SingleValueField("pattern", "pattern", "string", "Input conversion pattern", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom),
                            new SingleValueField("inputCaption", "input_caption", "caption", "Caption for user input", null, FieldFlags.Both | FieldFlags.Optional),
                            new SingleValueField("iconUrl", "icon_url", "url", "Icon/logo URL", null, FieldFlags.Both | FieldFlags.Optional)
                        }
                );
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public OpenIdRelyingParty GetRelyingParty(string input) {
            bool useOpenIdProviderIdentifier = (EndpointUrl != null || OpenIdProviderIdentifier != null);
            string identifier = input;
            if (!useOpenIdProviderIdentifier && identifier != null) {
                if (!Regex.IsMatch(identifier, "^[a-z]+://.+")) identifier = "http://" + identifier;
                
                // Add trailing slash if normalized identifier has no identifier component
                if (Regex.IsMatch(identifier, "^[a-z]+://[^/]+$")) identifier += "/";
                
                if (ConversionPattern != null) {
                    if (Regex.Match(ConversionPattern, "[a-z]+://[^/]+$").Success) ConversionPattern += "/";

                    int before = ConversionPattern.IndexOf('*'); // number of characters before asterisk
                    bool matches = true;
                    if (before == -1) {
                        matches = false;
                    } else {
                        int after = ConversionPattern.Length - before - 1; // number of characters after asterisk
                        if (
                            identifier.Length < ConversionPattern.Length || 
                            identifier.Substring(0, before) != ConversionPattern.Substring(0, before) || 
                            identifier.Substring(identifier.Length - after) != ConversionPattern.Substring(ConversionPattern.Length - after)
                        ) matches = false;
                    }

                    if (!matches) identifier = ConversionPattern.Replace("*", input);
                }
            }
            
            return new OpenIdRelyingParty(
                    useOpenIdProviderIdentifier,
                    useOpenIdProviderIdentifier ? OpenIdProviderIdentifier : identifier,
                    EndpointUrl
            );
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public static OpenIdRelyingParty GetGenericRelyingParty(string input) {
            return new OpenIdRelyingParty(false, input, null);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public void WriteInformation() {
            xmlWriter = webContext.StartXmlResponse();
            xmlWriter.WriteStartElement("provider");
            xmlWriter.WriteElementString("caption", Name);
            xmlWriter.WriteElementString("logoUrl", IconUrl);
            xmlWriter.WriteElementString("inputCaption", InputCaption);
            xmlWriter.WriteEndElement(); // </provider>
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public static void WriteGenericInformation(IfyWebContext webContext) {
            XmlWriter xmlWriter = webContext.StartXmlResponse();
            xmlWriter.WriteStartElement("provider");
            xmlWriter.WriteElementString("caption", "Sign in with an OpenID");
            xmlWriter.WriteElementString("inputCaption", "Full OpenID");
            xmlWriter.WriteEndElement(); // </provider>
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public static void WriteGenericSignInForm(IfyWebContext webContext, bool assign) {
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected override int WriteItemContents(int id, string query, FieldFlags flags, int level) {
            int count = base.WriteItemContents(id, query, flags, level);
            if (AllowAll) {
                xmlWriter.WriteStartElement("item");
                xmlWriter.WriteAttributeString("link", String.Format("{0}?{1}=assign", webContext.ScriptName, IfyWebContext.OperationParameterName));
                xmlWriter.WriteAttributeString("input", "true");
                xmlWriter.WriteElementString("caption", "Sign in with an OpenID");
                xmlWriter.WriteElementString("inputCaption", "Full OpenID");
                xmlWriter.WriteEndElement(); // </item>
                count++;
            }
            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        protected override void GetSpecificWriteValue(FieldExpression field) {
            switch (field.Name) {
                case "pattern" :
                    if (field.Value == null) break;
                    
                    field.Invalid = !Regex.IsMatch(field.Value, @"^[a-z]+://[^\*]*\*[^\*]*$");
                    if (field.Invalid) invalidPattern = true;
                    break;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected override bool OnItemNotProcessed(OperationType type, int itemId) {
            if (invalidPattern) {
                context.AddError("The input conversion pattern must contain exactly one asterisk and it must be convertible to a URL replacing the asterisk with an account name string");
                return true;
            }
            return false;
        }
        
    }

}

