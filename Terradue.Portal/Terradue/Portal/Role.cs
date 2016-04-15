using System;
using System.Collections.Generic;
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

        private const string PrivilegeBaseJoinSql = "priv AS p INNER JOIN role_priv AS rp ON p.id=rp.id_priv INNER JOIN role AS r ON rp.id_role=r.id INNER JOIN role_grant AS rg ON rp.id_role=rg.id_role LEFT JOIN usr_grp AS ug ON rg.id_usr={0} AND ug.id_usr IS NULL OR rg.id_grp=ug.id_grp AND ug.id_usr={0}";
        private const string PrivilegeValueSelectSql = "CASE WHEN int_value IS NULL THEN 0 ELSE int_value END AS v1";
        //private const string PrivilegeBaseQuery = "SELECT p.id, CASE WHEN int_value IS NULL THEN 0 ELSE int_value END AS v1 FROM usr AS u LEFT JOIN usr_grp AS ug ON u.id=ug.id_usr LEFT JOIN grp AS g ON ug.id_grp=g.id INNER JOIN role_grant AS rg ON rg.id_usr=u.id OR rg.id_grp=g.id INNER JOIN role_priv AS rp ON rp.id_role=rg.id_role INNER JOIN priv AS p ON rp.id_priv=p.id";

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

        public void IncludePrivilege(Privilege privilege) {
            IncludePrivileges(new int[] { privilege.Id });
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void IncludePrivileges(IEnumerable<Privilege> privileges) {
            List<int> privilegeIds = new List<int>();
            foreach (Privilege privilege in privileges) {
                if (privilegeIds.Contains(privilege.Id)) continue;
                privilegeIds.Add(privilege.Id);
            }
            IncludePrivileges(privilegeIds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void IncludePrivileges(IEnumerable<int> privilegeIds) {
            if (privilegeIds == null) return;
            string valuesStr = String.Empty;
            bool hasIds = false;
            foreach (int privilegeId in privilegeIds) {
                if (hasIds) valuesStr += ", ";
                valuesStr += String.Format("({0},{1})", Id, privilegeId);
                hasIds = true;
            }
            if (hasIds) {
                context.Execute(String.Format("DELETE FROM role_priv WHERE id_role={0} AND id_priv IN ({1});", Id, String.Join(",", privilegeIds))); // avoid duplicates
                context.Execute(String.Format("INSERT INTO role_priv (id_role, id_priv) VALUES {0};", valuesStr));
            }
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
            return DoesUserHavePrivilegeInternal(context, userId, domainId, String.Format("p.id_type={0} AND p.operation='{1}'", entityType.TopTypeId, operation.ToString()));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static Privilege[] GetUserPrivileges(IfyContext context, int userId, Entity item) {
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT DISTINCT p.id FROM ");
            sql.Append(PrivilegeBaseJoinSql);
            sql.Replace("{0}", userId.ToString());
            sql.Append(String.Format(" WHERE p.id_type={0} AND ", item.EntityType.TopTypeId));
            sql.Append(String.Format("(rg.id_usr={0} OR ug.id_usr={0}) AND ", userId));
            sql.Append(item.DomainId == 0 ? "rg.id_domain IS NULL" : String.Format("(rg.id_domain IS NULL OR rg.id_domain={0});", item.DomainId));

            List<Privilege> result = new List<Privilege>();
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql.ToString(), dbConnection);
            while (reader.Read()) {
                Privilege privilege = Privilege.Get(reader.GetInt32(0));
                result.Add(Privilege.Get(reader.GetInt32(0)));
            }
            context.CloseQueryResult(reader, dbConnection);

            return result.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user in question.</param>
        /// <param name="domainId">The database ID of the domain in question. If the domain is <em>null<em>, the check is.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        private static bool DoesUserHavePrivilegeInternal(IfyContext context, int userId, int domainId, string condition) {
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT ");
            sql.Append(PrivilegeValueSelectSql);
            sql.Append(" FROM ");
            sql.Append(PrivilegeBaseJoinSql);
            sql.Replace("{0}", userId.ToString());
            sql.Append(" WHERE ");
            sql.Append(String.Format("(rg.id_usr={0} OR ug.id_usr={0}) AND ", userId));
            sql.Append(domainId == 0 ? "rg.id_domain IS NULL" : String.Format("(rg.id_domain IS NULL OR rg.id_domain={0})", domainId));
            if (condition != null) sql.Append(String.Format(" AND {0};", condition));

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql.ToString(), dbConnection);
            bool result = reader.Read();
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to specified user for the specified domain or globally.</summary>
        /// <param name="user">The user who is the beneficary.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the users obtain this role globally.</param>
        public void GrantToUser(User user, Domain domain) {
            Grant(false, new int[] {user.Id}, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified users for the specified domain or globally.</summary>
        /// <param name="users">An array of the users who are the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the users obtain this role globally.</param>
        public void GrantToUsers(IEnumerable<User> users, Domain domain) {
            List<int> userIds = new List<int>();
            foreach (User user in users) userIds.Add(user.Id);
            Grant(false, userIds, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified users for the specified domain or globally.</summary>
        /// <param name="userIds">An array of the database IDs of the users who are the beneficiaries.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the users obtain this role globally.</param>
        public void GrantToUsers(IEnumerable<int> userIds, int domainId) {
            Grant(false, userIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to specified user for the specified domain or globally.</summary>
        /// <param name="user">The user who is the beneficary.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the users obtain this role globally.</param>
        public void GrantToGroup(Group group, Domain domain) {
            Grant(true, new int[] {group.Id}, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified groups for the specified domain or globally.</summary>
        /// <param name="groups">An array of the groups whose users are the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the groups obtain this role globally.</param>
        public void GrantToGroups(IEnumerable<Group> groups, Domain domain) {
            List<int> groupIds = new List<int>();
            foreach (Group group in groups) groupIds.Add(group.Id);
            Grant(true, groupIds, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified groups for the specified domain or globally.</summary>
        /// <param name="groupIds">An array of the database IDs of the groups whose users are the beneficiaries.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the groups obtain this role globally.</param>
        public void GrantToGroups(IEnumerable<int> groupIds, int domainId) {
            Grant(true, groupIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns this role to the specified users or groups for the specified domain or globally.</summary>
        /// <param name="forGroup">If <c>true</c>, the methods considers the given IDs as group IDs, otherwise as user IDs.</param>
        /// <param name="ids">An array of the database IDs of the users or groups.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the beneficiaries obtain this role globally.</param>
        protected void Grant(bool forGroup, IEnumerable<int> ids, int domainId) {
            if (ids == null) return;
            string valuesStr = String.Empty;
            bool hasIds = false;
            foreach (int id in ids) {
                if (hasIds) valuesStr += ", ";
                valuesStr += String.Format("({0},{1},{2})", id, Id, domainId == 0 ? "NULL" : domainId.ToString());
                hasIds = true;
            }
            if (hasIds) {
                context.Execute(String.Format("DELETE FROM role_grant WHERE id_role={0} AND {1} IN ({2}) AND id_domain{3};", Id, forGroup ? "id_grp" : "id_usr", String.Join(",", ids), domainId == 0 ? " IS NULL" : String.Format("={0}", domainId))); // avoid duplicates
                context.Execute(String.Format("INSERT INTO role_grant ({0}, id_role, id_domain) VALUES {1};", forGroup ? "id_grp" : "id_usr", valuesStr));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

    }

}

