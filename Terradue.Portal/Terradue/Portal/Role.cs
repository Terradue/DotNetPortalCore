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

        private const string PrivilegeBaseJoinSql = "priv AS p INNER JOIN role_priv AS rp ON p.id=rp.id_priv INNER JOIN role AS r ON rp.id_role=r.id INNER JOIN rolegrant AS rg ON rp.id_role=rg.id_role LEFT JOIN usr_grp AS ug ON rg.id_usr={0} AND ug.id_usr IS NULL OR rg.id_grp=ug.id_grp AND ug.id_usr={0}";
        private const string PrivilegeValueSelectSql = "CASE WHEN int_value IS NULL THEN 0 ELSE int_value END AS v1";

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Description of this role.</summary>
        [EntityDataField("description")]
        public string Description { get; set; }

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

        /// <summary>Creates a new Role instance representing the role with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The role ID.</param>
        /// <returns>The created Role object.</returns>
        public static Role FromId(IfyContext context, int id) {
            Role result = new Role(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>Creates a new Role instance representing the role with the specified Identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The role Identifier.</param>
        /// <returns>The created Role object.</returns>
        public static Role FromIdentifier(IfyContext context, string identifier) {
            Role result = new Role(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        /// <summary>
        /// Gets the identifying condition sql.
        /// </summary>
        /// <returns>The identifying condition sql.</returns>
        public override string GetIdentifyingConditionSql() {
            if (Id == 0 && !String.IsNullOrEmpty(Identifier)) return String.Format("t.identifier={0}", StringUtils.EscapeSql(Identifier));
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            if (Name == null) Name = Identifier;
            base.Store();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds the specified privilege to this role.</summary>
        /// <param name="privilege">The privilege to be added.</param>
        /// <param name="removeOthers">Whether or not all existing privileges are removed before adding the specified privilege is added. The default is <c>false</c>.</param>
        public void IncludePrivilege(Privilege privilege, bool removeOthers = false) {
            IncludePrivileges(new int[] { privilege.Id });
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds the specified privileges to this role.</summary>
        /// <param name="privileges">An IEnumerable of privileges to be added.</param>
        /// <param name="removeOthers">Whether or not all existing privileges are removed before adding the specified privileges are added. The default is <c>false</c>.</param>
        public void IncludePrivileges(IEnumerable<Privilege> privileges, bool removeOthers = false) {
            List<int> privilegeIds = new List<int>();
            foreach (Privilege privilege in privileges) {
                if (privilegeIds.Contains(privilege.Id)) continue;
                privilegeIds.Add(privilege.Id);
            }
            IncludePrivileges(privilegeIds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds the specified privileges to this role, optionally replacing the existing ones.</summary>
        /// <param name="privileges">An IEnumerable of database IDs of privileges to be included.</param>
        /// <param name="removeOthers">Whether or not all existing privileges are removed before adding the specified privileges are added. The default is <c>false</c>.</param>
        public void IncludePrivileges(IEnumerable<int> privilegeIds, bool removeOthers = false) {
            if (privilegeIds == null) return;
            string valuesStr = String.Empty;
            bool hasIds = false;
            foreach (int privilegeId in privilegeIds) {
                if (hasIds) valuesStr += ", ";
                valuesStr += String.Format("({0},{1})", Id, privilegeId);
                hasIds = true;
            }
            if (removeOthers) context.Execute(String.Format("DELETE FROM role_priv WHERE id_role={0};", Id));
            if (hasIds) {
                if (!removeOthers) context.Execute(String.Format("DELETE FROM role_priv WHERE id_role={0} AND id_priv IN ({1});", Id, String.Join(",", privilegeIds))); // avoid duplicates
                context.Execute(String.Format("INSERT INTO role_priv (id_role, id_priv) VALUES {0};", valuesStr));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes the specified privilege from this role.</summary>
        /// <param name="privilege">The privilege to be removed.</param>
        public void ExcludePrivilege(Privilege privilege) {
            ExcludePrivileges(new int[] { privilege.Id });
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes the specified privileges from this role.</summary>
        /// <param name="privileges">An IEnumerable of privileges to be added.</param>
        public void ExcludePrivileges(IEnumerable<Privilege> privileges) {
            List<int> privilegeIds = new List<int>();
            foreach (Privilege privilege in privileges) {
                if (privilegeIds.Contains(privilege.Id)) continue;
                privilegeIds.Add(privilege.Id);
            }
            ExcludePrivileges(privilegeIds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes the specified privileges from this role.</summary>
        /// <param name="privileges">An IEnumerable of database IDs of privileges to be included.</param>
        public void ExcludePrivileges(IEnumerable<int> privilegeIds) {
            if (privilegeIds == null) return;
            string valuesStr = String.Empty;
            bool hasIds = false;
            foreach (int privilegeId in privilegeIds) {
                if (hasIds) valuesStr += ", ";
                valuesStr += String.Format("({0},{1})", Id, privilegeId);
                hasIds = true;
            }
            if (hasIds) context.Execute(String.Format("DELETE FROM role_priv WHERE id_role={0} AND id_priv IN ({1});", Id, String.Join(",", privilegeIds))); // avoid duplicates
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

        /// <summary>Returns the privileges of a user for the specified entity item.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user in question.</param>
        /// <param name="item">The entity item for which the privileges are calculated.</param>
        /// <returns>An array of the privileges related to the entity type that contains only those privileges granted to the user for the item.</returns>
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
            while (reader.Read()) result.Add(Privilege.Get(reader.GetInt32(0)));
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

        /// <summary>
        /// Gets the user roles for a specific domain.
        /// </summary>
        /// <returns>The user roles for domain.</returns>
        /// <param name="context">Context.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="domainId">Domain identifier.</param>
        public static Role[] GetUserRolesForDomain(IfyContext context, int userId, int domainId) {
            List<Role> result = new List<Role> ();

            string sql = String.Format("SELECT id_role FROM rolegrant WHERE id_usr={0} AND id_domain={1};", userId, domainId);
            List<int> rolesId = new List<int> ();
            IDbConnection dbConnection = context.GetDbConnection ();
            IDataReader reader = context.GetQueryResult (sql, dbConnection);
            while (reader.Read ()) rolesId.Add (reader.GetInt32 (0));
            context.CloseQueryResult (reader, dbConnection);

            foreach (var id in rolesId) result.Add (Role.FromId (context, id));

            return result.ToArray ();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Grants this role to the specified user for the specified domain or globally.</summary>
        /// <param name="user">The user who is the beneficiary.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the user obtains this role globally.</param>
        public void GrantToUser(User user, Domain domain) {
            Grant(false, new int[] {user.Id}, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Grants this role to the specified users for the specified domain or globally.</summary>
        /// <param name="users">An array of the users who are the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the users obtain this role globally.</param>
        public void GrantToUsers(IEnumerable<User> users, Domain domain) {
            List<int> userIds = new List<int>();
            foreach (User user in users) userIds.Add(user.Id);
            Grant(false, userIds, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Grants this role to the specified users for the specified domain or globally.</summary>
        /// <param name="userIds">An array of the database IDs of the users who are the beneficiaries.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the users obtain this role globally.</param>
        public void GrantToUsers(IEnumerable<int> userIds, int domainId) {
            Grant(false, userIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Grants this role to the specified group for the specified domain or globally.</summary>
        /// <param name="user">The group whose users are the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the group obtains this role globally.</param>
        public void GrantToGroup(Group group, Domain domain) {
            Grant(true, new int[] {group.Id}, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Grants this role to the specified groups for the specified domain or globally.</summary>
        /// <param name="groups">An array of the groups whose users are the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is valid. If the value is <c>null</c>, the groups obtain this role globally.</param>
        public void GrantToGroups(IEnumerable<Group> groups, Domain domain) {
            List<int> groupIds = new List<int>();
            foreach (Group group in groups) groupIds.Add(group.Id);
            Grant(true, groupIds, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Grants this role to the specified groups for the specified domain or globally.</summary>
        /// <param name="groupIds">An array of the database IDs of the groups whose users are the beneficiaries.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is valid. If the value is <c>0</c>, the groups obtain this role globally.</param>
        public void GrantToGroups(IEnumerable<int> groupIds, int domainId) {
            Grant(true, groupIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Grants this role to the specified users or groups for the specified domain or globally.</summary>
        /// <param name="forGroup">If <c>true</c>, the methods considers the given IDs as group IDs, otherwise as user IDs.</param>
        /// <param name="ids">An array of the database IDs of the users or groups that are the beneficiaries.</param>
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
                context.Execute(String.Format("DELETE FROM rolegrant WHERE id_role={0} AND {1} IN ({2}) AND id_domain{3};", Id, forGroup ? "id_grp" : "id_usr", String.Join(",", ids), domainId == 0 ? " IS NULL" : String.Format("={0}", domainId))); // avoid duplicates
                context.Execute(String.Format("INSERT INTO rolegrant ({0}, id_role, id_domain) VALUES {1};", forGroup ? "id_grp" : "id_usr", valuesStr));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Revokes this role to the specified user for the specified domain or globally.</summary>
        /// <param name="user">The user who is no longer the beneficiary.</param>
        /// <param name="domain">The domain for which the role grant is no longer valid. If the value is <c>null</c>, the user loses the global grant of this role, but keeps it for individually assigned domains.</param>
        public void RevokeFromUser(User user, Domain domain) {
            Revoke(false, new int[] {user.Id}, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Revokes this role from the specified users for the specified domain or globally.</summary>
        /// <param name="users">An array of the users who are no longer the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is no longer valid. If the value is <c>null</c>, the users lose the global grant of this role, but keep it for individually assigned domains.</param>
        public void RevokeFromUsers(IEnumerable<User> users, Domain domain) {
            List<int> userIds = new List<int>();
            foreach (User user in users) userIds.Add(user.Id);
            Revoke(false, userIds, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Revokes this role from the specified users for the specified domain or globally.</summary>
        /// <param name="userIds">An array of the database IDs of the users who are no longer the beneficiaries.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is no longer valid. If the value is <c>0</c>, the users lose the global grant of this role, but keep it for individually assigned domains.</param>
        public void RevokeFromUsers(IEnumerable<int> userIds, int domainId) {
            Revoke(false, userIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Revokes this role from the specified group for the specified domain or globally.</summary>
        /// <param name="user">The group whose users are no longer the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is no longer valid. If the value is <c>null</c>, the group loses the global grant of this role, but keeps it for individually assigned domains.</param>
        public void RevokeFromGroup(Group group, Domain domain) {
            Revoke(true, new int[] {group.Id}, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Revokes this role from the specified groups for the specified domain or globally.</summary>
        /// <param name="groups">An array of the groups whose users are no longer the beneficiaries.</param>
        /// <param name="domain">The domain for which the role grant is no longer valid. If the value is <c>null</c>, the groups lose the global grant of this role, but keep it for individually assigned domains.</param>
        public void RevokeFromGroups(IEnumerable<Group> groups, Domain domain) {
            List<int> groupIds = new List<int>();
            foreach (Group group in groups) groupIds.Add(group.Id);
            Revoke(true, groupIds, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Revokes this role from the specified groups for the specified domain or globally.</summary>
        /// <param name="groupIds">An array of the database IDs of the groups whose users are no longer the beneficiaries.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is no longer valid. If the value is <c>0</c>, the groups lose the global grant of this role, but keep it for individually assigned domains.</param>
        public void RevokeFromGroups(IEnumerable<int> groupIds, int domainId) {
            Revoke(true, groupIds, domainId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Revokes this role from the specified users or groups for the specified domain or globally.</summary>
        /// <param name="forGroup">If <c>true</c>, the methods considers the given IDs as group IDs, otherwise as user IDs.</param>
        /// <param name="ids">An array of the database IDs of the users or groups that are the beneficiaries.</param>
        /// <param name="domainId">The database ID of the domain for which the role grant is no longer valid. If the value is <c>0</c>, the beneficiaries lose the global grant of this role, but keep it for individually assigned domains.</param>
        protected void Revoke(bool forGroup, IEnumerable<int> ids, int domainId) {
            if (ids == null) return;
            string valuesStr = String.Empty;
            bool hasIds = false;
            foreach (int id in ids) {
                if (hasIds) valuesStr += ", ";
                valuesStr += String.Format("({0},{1},{2})", id, Id, domainId == 0 ? "NULL" : domainId.ToString());
                hasIds = true;
            }
            if (hasIds) {
                context.Execute(String.Format("DELETE FROM rolegrant WHERE id_role={0} AND {1} IN ({2}) AND id_domain{3};", Id, forGroup ? "id_grp" : "id_usr", String.Join(",", ids), domainId == 0 ? " IS NULL" : String.Format("={0}", domainId))); // avoid duplicates
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Verifies whether this role is granted to the specified user on the specified domain or globally.</summary>
        /// <returns><c>true</c> if the role is granted, <c>false</c> otherwise.</returns>
        /// <param name="user">The user for which the check os performed.</param>
        /// <param name="domain">The domain for which the check is performed. If the value is <c>null</c>, the method checks whether the user has the role globally.</param>
        public bool IsGrantedTo(User user, Domain domain) {
            if (user == null) throw new Exception ("Invalid user");
            return IsGrantedTo(false, user.Id, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Verifies whether this role is granted to the specified group on the specified domain or globally.</summary>
        /// <returns><c>true</c> if the role is granted, <c>false</c> otherwise.</returns>
        /// <param name="group">The group for which the check is performed.</param>
        /// <param name="domain">The domain for which the check is performed. If the value is <c>null</c>, the method checks whether the group has the role globally.</param>
        public bool IsGrantedTo(Group group, Domain domain) {
            if (group == null) throw new Exception ("Invalid group");
            return IsGrantedTo(true, group.Id, domain == null ? 0 : domain.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Verifies whether this role is granted to the specified user on the specified domain or globally.</summary>
        /// <returns><c>true</c> if the role is granted, <c>false</c> otherwise.</returns>
        /// <param name="forGroup">If <c>true</c>, the methods considers the given ID as a group ID, otherwise as a user ID.</param>
        /// <param name="id">The database ID of the user or group for which the check is performed.</param>
        /// <param name="domainId">The database ID of the domain for which the check is performed. If the value is <c>0</c>, the method checks whether the user has the role globally.</param>
        public bool IsGrantedTo(bool forGroup, int id, int domainId) {
            if (id == 0) throw new Exception ("Invalid user or group");
            string sql = String.Format("SELECT COUNT(*) FROM rolegrant WHERE id_role={0} AND {1}={2} AND id_domain{3};", Id, forGroup ? "id_grp" : "id_usr", id, domainId == 0 ? " IS NULL" : String.Format("={0}", domainId));
            int count = context.GetQueryIntegerValue(sql);
            return count > 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the list of database IDs of users to whom this role is granted on the specified domain or globally.</summary>
        /// <returns>The database IDs of the users.</returns>
        /// <param name="domainId">The database ID of the domain in question. If the value is <c>0</c>, the method returns the users to whom the role is granted globally.</param>
        public int[] GetUsers(int domainId) {
            List<int> result = new List<int>();

            string sql = String.Format("SELECT id_usr FROM rolegrant WHERE id_usr IS NOT NULL AND id_role={0} AND id_domain{1};", Id, domainId == 0 ? " IS NULL" : String.Format("={0}", domainId));
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) result.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            return result.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the list of database IDs of users to whom this role is granted on the specified domain or globally.</summary>
        /// <returns>The database IDs of the users.</returns>
        /// <param name="domainId">The database ID of the domain in question. If the value is <c>0</c>, the method returns the users to whom the role is granted globally.</param>
        public int[] GetGroups(int domainId) {
            List<int> result = new List<int>();

            string sql = String.Format("SELECT id_grp FROM rolegrant WHERE id_grp IS NOT NULL AND id_role={0} AND id_domain{1};", Id, domainId == 0 ? " IS NULL" : String.Format("={0}", domainId));
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) result.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            return result.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the privileges associated to this role.</summary>
        /// <returns>An array of privileges.</returns>
        public Privilege[] GetPrivileges() { 
            List<Privilege> result = new List<Privilege> ();

            string sql = string.Format("SELECT id_priv FROM role_priv WHERE id_role={0};", Id);
            IDbConnection dbConnection = context.GetDbConnection ();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) result.Add(Privilege.Get(reader.GetInt32(0)));
            context.CloseQueryResult(reader, dbConnection);

            return result.ToArray();
        }
    }

}

