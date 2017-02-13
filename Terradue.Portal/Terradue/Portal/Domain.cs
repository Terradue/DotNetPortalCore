using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using Terradue.OpenSearch.Result;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Syndication;
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



    /// <summary>Domain</summary>
    /// <description>A Domain is an organizational unit to regroup \ref Entity</description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("domain", EntityTableConfiguration.Custom, IdentifierField = "name")]
    public class Domain : EntitySearchable {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Description</summary>
        /// <description>Human readable description of the domain</description>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("description")]
        public string Description { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the type of this domain.</summary>
        [EntityDataField("kind")]
        public DomainKind Kind { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the icon URL of this domain.</summary>
        [EntityDataField("icon_url")]
        public string IconUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Domain instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Domain(IfyContext context) : base(context) { }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Domain instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created Domain object.</returns>
        public static Domain GetInstance(IfyContext context) {
            return new Domain(context);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Domain instance representing the domain with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The domain ID.</param>
        /// <returns>The created Domain object.</returns>
        public static Domain FromId(IfyContext context, int id) {
            Domain result = new Domain(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>Creates a new Domain instance representing the domain with the specified Identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The domain Identifier.</param>
        /// <returns>The created Domain object.</returns>
        public static Domain FromIdentifier(IfyContext context, string identifier) {
            Domain result = new Domain(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        /// <summary>
        /// Gets the identifying condition sql.
        /// </summary>
        /// <returns>The identifying condition sql.</returns>
        public override string GetIdentifyingConditionSql() {
            if (Id == 0 && !string.IsNullOrEmpty(Identifier)) return String.Format("t.name={0}", StringUtils.EscapeSql(Identifier));
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines the scope (global or domain-restricted) for which the specified user or any of the specified groups has been granted at least one of the specified roles.</summary>
        /// <returns>
        ///     <para>The database IDs of the domains for which the user has at least one of the roles. The code using this method for privilege-based authorisation checks, has to distinguish the following cases:</para>
        ///     <list type="bullet">
        ///         <item>An empty array means that the user is not authorised.</item>
        ///         <item>An array containing one or more IDs means that the user is authorised for items that belong to the domains with these database IDs.</item>
        ///         <item>If the array is <c>null</c>, the user is authorised globally.</item>
        ///     </list>
        /// </returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user for which the domain restriction check is performed.</param>
        /// <param name="roleIds">An array of database IDs for the roles that are to be checked in relation to the user. If the array is <c>null</c> or empty, the check is skipped resulting in no domain restriction (return value <c>null</c>. If the array is empty, the grant is empty (return value is an empty array).</param>
        public static int [] GetGrantScope(IfyContext context, int userId, int [] groupIds, int [] roleIds) {
            if (roleIds == null) return null;
            if (roleIds.Length == 0) return new int [] { 0 };

            if (groupIds == null || groupIds.Length == 0) groupIds = new int [] { 0 };

            List<int> domainIds = new List<int>();
            string sql = String.Format("SELECT DISTINCT rg.id_domain FROM rolegrant AS rg LEFT JOIN usr_grp AS ug ON rg.id_grp=ug.id_grp WHERE rg.id_role IN ({2}) AND (rg.id_usr={0} OR ug.id_usr={0} OR rg.id_grp IN ({1})) ORDER BY rg.id_domain IS NULL, rg.id_domain;", userId, String.Join(",", groupIds), String.Join(",", roleIds));
            //Console.WriteLine("DOMAINS: {0}", sql);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            bool globallyAuthorized = false;
            while (reader.Read()) {
                // The domain ID NULL means that the user has the privilege globally and other any additional domains do not matter
                if (reader.GetValue(0) == DBNull.Value) {
                    globallyAuthorized = true;
                    break;
                }
                domainIds.Add(reader.GetInt32(0));
            }
            context.CloseQueryResult(reader, dbConnection);
            if (globallyAuthorized) return null;
            return domainIds.ToArray();
        }

        public KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            //case "member":
            //    return new KeyValuePair<string, string> ("EntityTypeId", t.Id.ToString ());
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }

        public override AtomItem ToAtomItem(NameValueCollection parameters) {
            bool ispublic = this.Kind == DomainKind.Public;
            bool isprivate = this.Kind == DomainKind.Private;

            //we only want thematic groups domains (public or private)
            if (!ispublic && !isprivate) return null;

            //if private, lets check the current user can access it (have a role in the domain)
            if (isprivate) {
                var proles = Role.GetUserRolesForDomain(context, context.UserId, this.Id);
                if (proles == null || proles.Length == 0) return null;
            }

            var entityType = EntityType.GetEntityType(typeof(Domain));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            if (!string.IsNullOrEmpty(parameters ["q"])) {
                string q = parameters ["q"].ToLower();
                if (!this.Identifier.ToLower().Contains(q) && !(Description != null && Description.ToLower().Contains(q)))
                    return null;
            }

            AtomItem result = new AtomItem();

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(Identifier);
            result.Content = new TextSyndicationContent(Identifier);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            result.Summary = new TextSyndicationContent(Description);
            result.ReferenceData = this;

            result.PublishDate = new DateTimeOffset(DateTime.UtcNow);

            //members
            var roles = new EntityList<Role>(context);
            roles.Load();
            foreach (var role in roles) {
                var usrs = role.GetUsers(this.Id);
                foreach (var usrId in usrs) {
                    User usr = User.FromId(context, usrId);
                    SyndicationPerson author = new SyndicationPerson(usr.Email, usr.FirstName + " " + usr.LastName, "");
                    author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", usr.Username));
                    author.ElementExtensions.Add(new SyndicationElementExtension("role", "http://purl.org/dc/elements/1.1/", role.Identifier));
                    result.Authors.Add(author);
                }
            }

            result.Links.Add(new SyndicationLink(id, "self", Identifier, "application/atom+xml", 0));
            if (!string.IsNullOrEmpty(IconUrl)) {

                Uri uri;
                if (IconUrl.StartsWith("http")) {
                    uri = new Uri(IconUrl);
                } else {
                    var urib = new UriBuilder(System.Web.HttpContext.Current.Request.Url);
                    urib.Path = IconUrl;
                    uri = urib.Uri;
                }

                result.Links.Add(new SyndicationLink(uri, "icon", "", GetImageMimeType(IconUrl), 0));
            }

            result.Categories.Add(new SyndicationCategory("visibility", null, ispublic ? "public" : "private"));

            return result;
        }

        private string GetImageMimeType(string filename) {
            string extension = filename.Substring(filename.LastIndexOf(".") + 1);
            string result;

            switch (extension.ToLower()) {
            case "gif":
                result = "image/gif";
                break;
            case "gtiff":
                result = "image/tiff";
                break;
            case "jpeg":
                result = "image/jpg";
                break;
            case "png":
                result = "image/png";
                break;
            default:
                result = "application/octet-stream";
                break;
            }
            return result;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Kinds of domains.</summary>
    public enum DomainKind {

        None = 0,

        User = 1,

        Group = 2,

        Private = 3,

        Public = 4
    }

}

