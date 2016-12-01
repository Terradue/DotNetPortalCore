using System;
using System.Collections.Generic;
using System.Data;
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
    public class Domain : Entity {
        
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
        public Domain(IfyContext context) : base(context) {}
        
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
            if (Id == 0 && !string.IsNullOrEmpty (Identifier)) return String.Format ("t.name={0}", StringUtils.EscapeSql (Identifier));
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
        public static int[] GetGrantScope(IfyContext context, int userId, int[] groupIds, int[] roleIds) {
            if (roleIds == null) return null;
            if (roleIds.Length == 0) return new int[] {0};

            if (groupIds == null || groupIds.Length == 0) groupIds = new int[] {0};

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

