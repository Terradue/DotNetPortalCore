using System;
using System.Data;
using System.Text;
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



    /// <summary>Represents a user group</summary>
    [EntityTable("role", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name")]
    public class Role : Entity {

        private const string PrivilegeBaseQuery = "SELECT CASE WHEN int_value IS NULL THEN 0 ELSE int_value END AS v1 FROM usr AS u LEFT JOIN usr_grp AS ug ON u.id=ug.id_usr LEFT JOIN grp AS g ON ug.id_grp=g.id INNER JOIN role_grant AS rg ON rg.id_usr=u.id OR rg.id_grp=g.id INNER JOIN role_priv AS rp ON rp.id_role=rg.id_role INNER JOIN priv AS p ON rp.id_priv=p.id";

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ManagerRole instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Role(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ManagerRole instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created ManagerRole object</returns>
        public static Role GetInstance(IfyContext context) {
            return new Role(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            if (Name == null) Name = Identifier;
            base.Store();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="user">The user in question.</param>
        /// <param name="domain">The domain in question. If the domain is <em>null<em>, it is calculated whether the user has the specified privilege globally for the entire system.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        public static bool DoesUserHavePrivilege(IfyContext context, User user, Domain domain, string identifier) {
            return DoesUserHavePrivilegeInternal(context, user.Id, domain == null ? 0 : domain.Id, String.Format("p.identifier={0}", StringUtils.EscapeSql(identifier)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user in question.</param>
        /// <param name="domainId">The database ID of the domain in question. If the domain is <em>null<em>, it is calculated whether the user has the specified privilege globally for the entire system.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        public static bool DoesUserHavePrivilege(IfyContext context, int userId, int domainId, string identifier) {
            return DoesUserHavePrivilegeInternal(context, userId, domainId, String.Format("p.identifier={0}", StringUtils.EscapeSql(identifier)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user in question.</param>
        /// <param name="domainId">The database ID of the domain in question. If the domain is <em>null<em>, it is calculated whether the user has the specified privilege globally for the entire system.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        public static bool DoesUserHavePrivilege(IfyContext context, int userId, int domainId, EntityType entityType, EntityOperationType operation) {
            return DoesUserHavePrivilegeInternal(context, userId, domainId, String.Format("p.id_type={0} AND p.operation='{1}'", entityType.Id, operation.ToString()));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user in question.</param>
        /// <param name="domainId">The database ID of the domain in question. If the domain is <em>null<em>, the check is.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        private static bool DoesUserHavePrivilegeInternal(IfyContext context, int userId, int domainId, string condition) {
            // TODO To be implemented

            StringBuilder sql = new StringBuilder(PrivilegeBaseQuery);
            sql.Append(String.Format(" WHERE u.id={0}", userId));
            sql.Append(domainId == 0 ? " AND rg.id_domain IS NULL" : String.Format(" AND (rg.id_domain IS NULL OR rg.id_domain={0})", domainId));
            if (condition != null) sql.Append(String.Format(" AND {0}", condition));
            sql.Append(" ORDER BY v1;");

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql.ToString(), dbConnection);
            bool result = reader.Read();
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified users for the specified domain or globally.</summary>
        /// <param name="userIds">An array of the database IDs of the users.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the users obtain this role globally.</param>
        public void AssignUsers(int[] userIds, int domainId) {
            AssignUsersOrGroups(true, userIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified groups for the specified domain or globally.</summary>
        /// <param name="groupIds">An array of the database IDs of the groups.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the groups obtain this role globally.</param>
        public void AssignGroups(int[] groupIds, int domainId) {
            AssignUsersOrGroups(false, groupIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified users or groups for the specified domain or globally.</summary>
        /// <param name="addUsers">If <c>true</c>, the methods considers the given IDs as users IDs, otherwise as group IDs.</param>
        /// <param name="ids">An array of the database IDs of the users or groups.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the beneficiaries obtain this role globally.</param>
        public void AssignUsersOrGroups(bool addUsers, int[] ids, int domainId) {
            if (ids == null || ids.Length == 0) return;
            context.Execute(String.Format("DELETE FROM role_grant WHERE {0} IN ({1}) AND id_domain{2};", addUsers ? "id_usr" : "id_grp", String.Join(",", ids), domainId == 0 ? " IS NULL" : String.Format("={0}", domainId))); // avoid duplicates
            string valuesStr = String.Empty;
            for (int i = 0; i < ids.Length; i++) {
                if (i != 0) valuesStr += ", ";
                valuesStr += String.Format("({0},{1},{2})", ids[i], Id, domainId == 0 ? "NULL" : domainId.ToString());
            }
            context.Execute(String.Format("INSERT INTO role_grant ({0}, id_role, id_domain) VALUES {1};", addUsers ? "id_usr" : "id_grp", valuesStr));
        }

        //---------------------------------------------------------------------------------------------------------------------

    }

}

