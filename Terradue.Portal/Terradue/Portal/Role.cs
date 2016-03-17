using System;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





using System.Data;
using System.Text;





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents a user group</summary>
    public class Role : Entity {

        private const string PrivilegeBaseQuery = "SELECT MAX(CASE WHEN int_value IS NULL THEN 0 ELSE int_value END) FROM usr AS u LEFT JOIN usr_grp AS ug ON u.id=ug.id_usr LEFT JOIN grp AS g ON ug.id_grp=g.id INNER JOIN role_grant AS rg ON rg.id_usr=u.id OR rg.id_grp=g.id INNER JOIN role_priv AS rp ON rp.id_role=rg.id_role INNER JOIN priv AS p ON rp.id_priv=p.id";

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ManagerRole instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Role(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ManagerRole instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created ManagerRole object</returns>
        public static new Role GetInstance(IfyContext context) {
            return new Role(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="user">The user in question.</param>
        /// <param name="domain">The domain in question. If the domain is <em>null<em>, it is calculated whether the user has the specified privilege globally for the entire system.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        public static bool DoesUserHavePrivilege(IfyContext context, User user, Domain domain, string identifier) {
            // TODO To be implemented
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user in question.</param>
        /// <param name="domainId">The database ID of the domain in question. If the domain is <em>null<em>, it is calculated whether the user has the specified privilege globally for the entire system.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        public static bool DoesUserHavePrivilege(IfyContext context, int userId, int domainId, string identifier) {
            return DoesUserHavePrivilegeInternal(context, userId, domainId, String.Format("p.identifier={0}", identifier));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates whether a user has the specified privilege for the specified domain.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user in question.</param>
        /// <param name="domainId">The database ID of the domain in question. If the domain is <em>null<em>, it is calculated whether the user has the specified privilege globally for the entire system.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns><i>true</i> if the user has the privilege.</returns>
        public static bool DoesUserHavePrivilege(IfyContext context, int userId, int domainId, EntityType entityType, EntityOperationType operation) {
            return DoesUserHavePrivilegeInternal(context, userId, domainId, String.Format("p.id_type={0} AND p.operation={1}", entityType.Id, operation.ToString()));
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
            sql.Append(";");

            Console.WriteLine("SQL = {0}", sql.ToString());

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql.ToString(), dbConnection);
            bool result = reader.Read();
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Enumeration of generic operations regarding entities.</summary>
    public enum EntityOperationType {

        /// <summary>View an entity item.</summary>
        View = 'v',

        /// <summary>View additional information related to an entity item.</summary>
        ViewExtended = 'V',

        /// <summary>Create a new entity item.</summary>
        Create = 'c',

        /// <summary>Change an existing entity item.</summary>
        Change = 'm',

        /// <summary>Make owned entity item available to others.</summary>
        Publish = 'p',

        /// <summary>Make an entity item available to others.</summary>
        Delete = 'd',

        /// <summary>Assign resource item permissions to users or groups of same domain.</summary>
        Assign = 'a',
            
        /// <summary>Assign resource item permissions to any user or group.</summary>
        AssignGlobal = 'A'
    }

}

