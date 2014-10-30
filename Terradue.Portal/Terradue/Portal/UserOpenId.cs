using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
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

    

    public class CertificateAcceptor : ICertificatePolicy {
        
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) {
            return true;
        }
        
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    [EntityTable("usropenid", EntityTableConfiguration.Custom, HasOwnerReference = true)]
    public class UserOpenId : Entity {
        
        private int openIdAuth;
        private OpenIdProvider provider;
        private int sessionOpenIdId;
        
        [EntityDataField("id_provider", IsForeignKey = true)]
        public int ProviderId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("user_input")]
        public string UserInput { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool MissingIdentifier { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance.</summary>
        /*!
        /// <param name="context">the execution environment context</param>
        */
        public UserOpenId(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance.</summary>
        /*!
        /// <param name="context">the execution environment context</param>
        /// <returns>the created User object</returns>
        */
        public static new UserOpenId GetInstance(IfyContext context) {
            return new UserOpenId(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance with the specified data structure.</summary>
        /*!
        /// <param name="context">the execution environment context</param>
        /// <param name="data">the entity data structure information</param>
        /// <returns>the created User object</returns>
        */
        public static new UserOpenId GetInstance(IfyContext context, EntityData data) {
            UserOpenId result = UserOpenId.GetInstance(context);
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
        public static UserOpenId FromId(IfyContext context, int id) {
            UserOpenId result = new UserOpenId(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the default data structure for the user entity.</summary>
        protected override void SetStructure() {
            //openIdAuth = webContext.GetConfigIntegerValue("OpenIdAuth");
            //if (openIdAuth == 0) context.ReturnError("OpenID authentication not permitted");
            
            SetRestrictedStructure("Sign in with an OpenID", "usropenid", "usropenid AS t");            
            
            SingleReferenceField openIdProviderField = new SingleReferenceField("provider", "openidprovider", "@.caption", "id_provider", "OpenID Provider", null, FieldFlags.Both | FieldFlags.Unique | (openIdAuth == 2 ? FieldFlags.Optional : 0));
            openIdProviderField.NullCaption = "[detect from identifier]";
            Data.AppendFields(
                    null,
                    new FieldExpression[] {
                        new SingleValueField("openid", "user_input", "caption", "Full OpenID or Account Name", null, FieldFlags.List),
                        openIdProviderField
                    }
            );
            if (FilterCondition == null) FilterCondition = String.Empty; else FilterCondition += " AND ";
            FilterCondition += "claimed_id IS NOT NULL";
            CanCreate = false;
            ShowItemLinks = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Processes a request regarding OpenIDs.</summary>
        /*!
        */
        public bool ProcessRequest() {
            string operationName = context.GetParamValue(IfyWebContext.OperationParameterName);
            
            openIdAuth = webContext.AllowOpenId;
            if (openIdAuth == 0) context.ReturnError("OpenID authentication not permitted");

            switch (operationName) {
                /*case "list" :
                    if (context.AuthenticatedUser) {
                        WriteProviderList(false);
                        return true;
                    } else {
                        return false;
                    }*/

                case "signin" :
                    // Sign-in modes:
                    // (1) (Positive) OpenID authentication assertion 
                    // (2) Request for OpenID authentication
                    // (3) Username/password login
                    // (4) Display sign-in form

                    try {
                        ServicePointManager.ServerCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
                            return true;
                        };
                        
                        //ServicePointManager.CertificatePolicy = new CertificateAcceptor();
                        
                        int providerId;
                        Int32.TryParse(context.GetParamValue("provider"), out providerId);
                        ProviderId = providerId;
    
                        if (context.GetParamValue("openid.mode") != null) { // (1)
                            OpenIdRelyingParty openId = null;
                            openId = OpenIdRelyingParty.RecoverFromAssociation();
                            openId.ResponseNonceStorage = webContext;
                            if (!openId.VerifyAuthenticationAssertion()) {
                                context.AddWarning("OpenID authentication was canceled", "openIdCanceled");
                                return false;
                            }
                            webContext.AuthenticateWithOpenId(openId);
                            webContext.CheckAvailability(false);
                            context.WriteInfo("You are now logged in", "openIdAuthenticated");
                            
                        } else if ((UserInput = context.GetParamValue("openid")) != null || ProviderId != 0) { // (2)
                            if (ProviderId != 0) provider = OpenIdProvider.FromId(context, ProviderId);
        
                            if (UserInput == null && !provider.UseOpenIdProviderIdentifier) {
                                MissingIdentifier = true;
                                break;
                            }
    
                            // Get a OpenIdRelyingParty instance, either the generic one or the one corresponding to the requested OpenID provider
                            OpenIdRelyingParty openId = (provider == null ? OpenIdProvider.GetGenericRelyingParty(UserInput) : provider.GetRelyingParty(UserInput));

                            // Get an OpenID association
                            openId.ReturnToUrl = String.Format("{0}?{1}=signin{2}",
                                    webContext.ScriptUrl,
                                    IfyWebContext.OperationParameterName,
                                    webContext.Format == null ? String.Empty : "&" + IfyWebContext.FormatParameterName + "=" + webContext.Format
                            );
                            openId.RealmPattern = webContext.HostUrl;
                            openId.GetAssociation();

                            if (context.GetParamValue("_debug") == "3") {
                                context.WriteInfo("openid.op_endpoint:" + openId.EndpointUrl);
                                context.WriteInfo("openid.assoc_type:" + openId.AssociationType);
                                context.WriteInfo("openid.assoc_handle:" + openId.AssociationHandle);
                                context.WriteInfo("openid.mac_key:" + openId.Mac);
                                context.WriteInfo("openid.expires_in:" + openId.ExpiresIn.ToString());
                                context.WriteInfo(openId.AuthenticationUrl);
                            } else {
                                openId.RequestAuthentication();
                            }
                            
                        } else {
                            if (openIdAuth != 2) {
                                context.AddError("Invalid request");
                                return false;
                            }
                        }
                        
                    } catch (Exception e) {
                        if (!context.ErrorHandled) context.AddError(e.Message, null);
                    }
                    
                    break;

                case "assign" :
                    try {
                        ServicePointManager.ServerCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
                            return true;
                        };
    
                        //ServicePointManager.CertificatePolicy = new CertificateAcceptor();
    
                        if (!context.IsUserAuthenticated) webContext.RejectUnauthenticatedRequest();
                        bool success = false;

                        if (context.GetParamValue("openid.mode") == null) {
                            UserInput = context.GetParamValue("openid");
                            int providerId;
                            Int32.TryParse(context.GetParamValue("provider"), out providerId);
                            ProviderId = providerId;
                            
                            if (UserInput != null || ProviderId != 0) {
                                // Store current OpenID's database ID in session (to remember it after the returning redirect from the OpenID provider)
            
                                if (ProviderId != 0) provider = OpenIdProvider.FromId(context, ProviderId);
            
                                if (UserInput == null && !provider.UseOpenIdProviderIdentifier) {
                                    WriteSignInForm(true);
                                    return true;
                                    //context.ReturnError("Missing identifier");
                                }
                                
                                context.Execute(String.Format("INSERT INTO usropenid (id_usr, id_provider, user_input) VALUES ({0}, {1}, {2});",
                                        context.UserId,
                                        ProviderId == 0 ? "NULL" : ProviderId.ToString(),
                                        StringUtils.EscapeSql(UserInput)
                                ));
                                sessionOpenIdId = context.GetInsertId();
                                HttpContext.Current.Session["openid.id"] = sessionOpenIdId;
            
                                try {
                                    // Get a OpenIdRelyingParty instance, either the generic one or the one corresponding to the requested OpenID provider
                                    OpenIdRelyingParty openId = (provider == null ? OpenIdProvider.GetGenericRelyingParty(UserInput) : provider.GetRelyingParty(UserInput));
                                    openId.ReturnToUrl = String.Format("{0}?id={1}&{2}=assign{3}",
                                            webContext.ScriptUrl,
                                            sessionOpenIdId,
                                            IfyWebContext.OperationParameterName,
                                            webContext.Format == null ? String.Empty : "&" + IfyWebContext.FormatParameterName + "=" + webContext.Format
                                    );
                                    openId.RealmPattern = webContext.HostUrl;
                                    openId.GetAssociation();
            
                                    if (context.GetParamValue("_debug") == "3") {
                                        context.WriteInfo("openid.op_endpoint:" + openId.EndpointUrl);
                                        context.WriteInfo("openid.assoc_type:" + openId.AssociationType);
                                        context.WriteInfo("openid.assoc_handle:" + openId.AssociationHandle);
                                        context.WriteInfo("openid.mac_key:" + openId.Mac);
                                        context.WriteInfo("openid.expires_in:" + openId.ExpiresIn.ToString());
                                        context.WriteInfo(openId.AuthenticationUrl);
                                    } else {
                                        openId.RequestAuthentication();
                                    }
                                    
                                } catch (Exception e) {
                                    if (!context.ErrorHandled) context.AddError(e.Message, null);
                                }
                                if (!success && sessionOpenIdId != 0) context.Execute(String.Format("DELETE FROM usropenid WHERE id={0};", sessionOpenIdId));
            
                            } else {
                                MissingIdentifier = true;
                                if (openIdAuth == 2) WriteSignInForm(true);
                            }

                        } else {
                            try {
                                success = Verify();
                            } catch (Exception e) {
                                if (!context.ErrorHandled) context.AddError(e.Message, null);
                            }
                            
                            // In case of failure, delete concerned user OpenID record, but only if not yet verified
                            if (!success && sessionOpenIdId != 0 && context.GetQueryBooleanValue(String.Format("SELECT claimed_id IS NULL FROM usropenid WHERE id={0};", sessionOpenIdId))) {
                                context.Execute(String.Format("DELETE FROM usropenid WHERE id={0};", sessionOpenIdId));
                            }

                        }

                    } catch (Exception e) {
                        if (!context.ErrorHandled) context.AddError(e.Message, null);
                    }
                    
                    break;
                
                case "delete" :
                    GetIdsFromRequest("id");
                    if (Ids.Length != 0) DeleteItems(Ids);
                    break;

            }
            
            //if (MissingIdentifier) context.AddError("You must specify an OpenID", "missingInput");
                
            return false;

            //if (context.AuthenticatedUser) userOpenId.WriteItemList();
            //return context.AuthenticatedUser;
        }
        
        protected bool Verify() {
            Id = webContext.GetIdFromRequest();
            
            Load();
            
            object obj;
            sessionOpenIdId = 0;
            if ((obj = HttpContext.Current.Session["openid.id"]) != null) Int32.TryParse(obj.ToString(), out sessionOpenIdId);
            if (sessionOpenIdId == 0) context.ReturnError("Could not verify OpenID authentication assertion: missing reference");
            if (Id == 0) context.ReturnError("No current OpenID");

            if (sessionOpenIdId != Id) {
                context.AddWarning("Could not verify OpenID authentication assertion: reference not matching");
                return false;
            }

            OpenIdRelyingParty openId = null;

            openId = OpenIdRelyingParty.RecoverFromAssociation();
            openId.ResponseNonceStorage = webContext;
            if (!openId.VerifyAuthenticationAssertion()) {
                context.AddWarning("OpenID authentication was canceled", "openIdCanceled");
                return false;
            }
            string claimedIdentifier = openId.ClaimedIdentifier;
            
            int conflictUserId = context.GetQueryIntegerValue(String.Format("SELECT id_usr FROM usropenid WHERE claimed_id={0};", StringUtils.EscapeSql(claimedIdentifier)));
            
            if (conflictUserId == context.UserId) {
                context.AddWarning("This OpenID has already been assigned to your user account");
                return false;
            } else if (conflictUserId != 0) {
                context.ReturnError("This OpenID belongs already to another user account");
            }
                
            
            if (UserInput == null && ProviderId != 0) UserInput = context.GetQueryStringValue(String.Format("SELECT caption FROM openidprovider WHERE id={0};", ProviderId));
            if (openId.Email != null) {
                if (UserInput == null) UserInput = openId.Email;
                else UserInput = UserInput + " (" + openId.Email + ")";
            } else if (UserInput == null) UserInput = claimedIdentifier;

            context.Execute(String.Format("UPDATE usropenid SET user_input={1}, claimed_id={2} WHERE id={0};", Id, StringUtils.EscapeSql(UserInput), StringUtils.EscapeSql(claimedIdentifier)));
            
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemProcessed(OperationType type, int itemId) {
            switch (type) {
                case OperationType.Create :
                case OperationType.Modify :
                    
                    return false;
            }
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public void WriteProviderList() {
            if (provider == null) provider = OpenIdProvider.GetInstance(context);
            provider.SignInList = true;
            provider.AllowAll = (openIdAuth == 2);
            provider.WriteItemList();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public void WriteSignInForm(bool assign) {
            if (provider == null) provider = OpenIdProvider.GetInstance(context);
            provider.SignInForm = true;
            provider.AssignOpenId = assign;
            provider.WriteEmptyItem();
        }
        
    }

}

