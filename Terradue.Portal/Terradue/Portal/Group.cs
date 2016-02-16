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
        public static new Group GetInstance(IfyContext context) {
            return new Group(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Group instance representing the group with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The group ID.</param>
        /// <returns>The created Group object.</returns>
        public static Group FromId(IfyContext context, int id) {
            Group result = new Group(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Group instance representing the group with the specified name.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The group ID.</param>
        /// <returns>The created Group object.</returns>
        public static Group FromIdentifier(IfyContext context, string identifier) {
            Group result = new Group(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }
        
        /// <summary>Creates a new Group instance representing the group with the specified name (alias for FromIdentifier).</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The group ID.</param>
        /// <returns>The created Group object.</returns>
        public static Group FromName(IfyContext context, string name) {
            return FromIdentifier(context, name);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the users associated to this group.</summary>
        public IEnumerable<User> GetUsers() {
            List<User> result = new List<User>();
            List<int> userIds = new List<int>();

            string sql = String.Format("SELECT t.id_usr FROM usr_grp AS t WHERE t.id_grp={0};", this.Id);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) userIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            foreach (int id in userIds) result.Add(User.FromId(context, id));

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Associates the specified users to this group.</summary>
        /// <param name="users">The users to be associated.</param>
        public void SetUsers(IEnumerable<User> users) {
            context.Execute(String.Format("DELETE FROM usr_grp WHERE id_grp={0};",this.Id));
            foreach (User user in users) {
                if (!user.Exists) user.Store();
                context.Execute(String.Format("INSERT INTO usr_grp (id_usr, id_grp) VALUES ({0},{1});", user.Id, this.Id));
            }
        }

    }

}

