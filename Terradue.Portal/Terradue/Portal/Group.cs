using System;
using System.Collections.Generic;
using Terradue.Util;




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Data;





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>User Group</summary>
    /// <description></description>>Represents a basic user group</description>
    /// \ingroup Authorisation
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("grp", EntityTableConfiguration.Custom, HasDomainReference = true, IdentifierField = "name")]
    public class Group : Entity {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Name, the unique identifier, of this group.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public override string Name {
            get { return Identifier; }
            set { Identifier = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Description of this group.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("description")]
        public string Description { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Optional privilege level of this group.</summary>
        /// <remarks>Some applications may use this property to specify a level of privileges for a group, but the meaning of those privileges is defined elsewhere.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("priority")]
        public int Priority { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether this group is automatically assigned to new users.</summary>
        [EntityDataField("is_default")]
        public bool IsDefault { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether new restricted resources will automatically give access to members of this group.</summary>
        [EntityDataField("all_resources")]
        public bool AllResources { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Group instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Group(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Group instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created Group object.</returns>
        public static Group GetInstance(IfyContext context) {
            return new Group(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Group instance representing the group with the specified database ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the group.</param>
        /// <returns>The created Group object.</returns>
        public static Group FromId(IfyContext context, int id) {
            Group result = new Group(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Group instance representing the group with the specified identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the group.</param>
        /// <returns>The created Group object.</returns>
        public static Group FromIdentifier(IfyContext context, string identifier) {
            Group result = new Group(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }
        
        /// <summary>Creates a new Group instance representing the group with the specified name (alias for FromIdentifier).</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="name">The group ID.</param>
        /// <returns>The created Group object.</returns>
        public static Group FromName(IfyContext context, string name) {
            return FromIdentifier(context, name);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the users associated to this group.</summary>
        public IEnumerable<User> GetUsers() {
            List<User> result = new List<User>();
            List<int> userIds = new List<int>();

            string sql = String.Format("SELECT id_usr FROM usr_grp WHERE id_grp={0};", Id);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) userIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            foreach (int id in userIds) result.Add(User.FromId(context, id));

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the specified users as the only users belonging to this group.</summary>
        /// <param name="users">The users to be associated.</param>
        public void SetUsers(IEnumerable<User> users) {
            AssignUsers(users, true);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns the specified user to this group.</summary>
        /// <param name="user">The user to be assigned.</param>
        public void AssignUser(User user) {
            AssignUsers(new int[] {user.Id});
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns the specified users to this group.</summary>
        /// <param name="users">The users to be assigned.</param>
        /// <param name="removeOthers">Whether or not the specified users will be the only ones in this group.</param>
        public void AssignUsers(IEnumerable<User> users, bool removeOthers = false) {
            List<int> userIds = new List<int>();
            foreach (User user in users) userIds.Add(user.Id);
            AssignUsers(userIds, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Assigns the specified users to this group.</summary>
        /// <param name="user">The database IDs of the users to be assigned.</param>
        /// <param name="removeOthers">Whether or not the specified users will be the only ones in this group.</param>
        public void AssignUsers(IEnumerable<int> userIds, bool removeOthers = false) {
            if (userIds == null) return;
            string valuesStr = String.Empty;
            bool hasIds = false;
            foreach (int userId in userIds) {
                if (hasIds) valuesStr += ", ";
                valuesStr += String.Format("({0},{1})", userId, Id);
                hasIds = true;
            }
            if (removeOthers) context.Execute(String.Format("DELETE FROM usr_grp WHERE id_grp={0};", Id));
            if (hasIds) {
                if (!removeOthers) context.Execute(String.Format("DELETE FROM usr_grp WHERE id_grp={0} AND id_usr IN ({1});", Id, String.Join(",", userIds))); // avoid duplicates
                context.Execute(String.Format("INSERT INTO usr_grp (id_usr, id_grp) VALUES {0};", valuesStr));        
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Unassigns the specified user from this group.</summary>
        /// <param name="user">The user to be unassigned.</param>
        public void UnassignUser(User user) {
            AssignUsers(new int[] {user.Id});
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Unassigns the specified users from this group.</summary>
        /// <param name="user">The users to be unassigned.</param>
        public void UnassignUsers(IEnumerable<User> users) {
            List<int> userIds = new List<int>();
            foreach (User user in users) userIds.Add(user.Id);
            AssignUsers(userIds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Unassigns the specified users from this group.</summary>
        /// <param name="user">The database IDs of the users to be unassigned.</param>
        public void UnassignUsers(IEnumerable<int> userIds) {
            if (userIds == null) return;
            string valuesStr = String.Empty;
            bool hasIds = false;
            foreach (int userId in userIds) {
                if (hasIds) valuesStr += ", ";
                valuesStr += String.Format("({0},{1})", userId, Id);
                hasIds = true;
            }
            if (hasIds) {
                context.Execute(String.Format("DELETE FROM usr_grp WHERE id_grp={0} AND id_usr IN ({1});", Id, String.Join(",", userIds))); // avoid duplicates
            }
        }

    }

}

