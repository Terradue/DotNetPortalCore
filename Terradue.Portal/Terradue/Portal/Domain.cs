using System;
using System.Collections.Generic;
using System.Data;





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

        [EntityDataField("description")]
        /// <summary>Description</summary>
        /// <description>Human readable description of the domain</description>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string Description { get; set; } 

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

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines the scope (global or domain-restricted) for which the specified user has has been granted at least one of the specified roles.</summary>
        /// <returns>
        ///     <para>The domains for user. The code using this method for privilege-based authorisation checks, has to distinguish the following cases:</para>
        ///     <list type="bullet">
        ///         <item>An empty array means that the user is not authorised.</item>
        ///         <item>An array containing one or more IDs means that the user is authorised for items that belong to the domains with these database IDs.</item>
        ///         <item>If the array is <c>null</c>, the user is authorised globally.</item>
        ///     </list>
        /// </returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user for which the domain restriction check is performed.</param>
        /// <param name="roleIds">An array of database IDs for the roles that are to be checked in relation to the user. If the array is <c>null</c> or empty, the check is skipped resulting in no domain restriction.</param>
        public static int[] GetGrantScopeForUser(IfyContext context, int userId, int[] roleIds) {
            if (roleIds == null || roleIds.Length == 0) return new int[] {0};

            List<int> domainIds = new List<int>();
            string sql = String.Format("SELECT DISTINCT rg.id_domain FROM role_grant AS rg LEFT JOIN usr_grp AS ug ON rg.id_role IN ({1}) AND rg.id_grp=ug.id_grp WHERE rg.id_usr={0} OR ug.id_usr={0} ORDER BY rg.id_domain IS NULL, rg.id_domain;", userId, String.Join(",", roleIds));
            Console.WriteLine("DOMAINS: {0}", sql);
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

}

