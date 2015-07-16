using System;
using Terradue.Util;

namespace Terradue.Portal {
    [EntityTable("priv", EntityTableConfiguration.Custom)]
    public class Privilege {

        private IfyContext context;

        /// <summary>Gets the Id</summary>
        [EntityDataField("id")]
        public int Id { get; set;}

        /// <summary>Gets the EntityType Id</summary>
        [EntityDataField("id_type", IsForeignKey = true)]
        public int EntityTypeId { get; protected set; }

        private EntityType entityType;
        public EntityType EntityType { 
            get { 
                return entityType;
            } 

            protected set {
                entityType = value;
                EntityTypeId = (entityType == null ? 0 : entityType.Id);
            }
        }

        /// <summary>Gets the position</summary>
        [EntityDataField("pos")]
        public int Position { get; set; }

        /// <summary>Gets the name</summary>
        [EntityDataField("name")]
        public string Name { get; set;}

        /// <summary>Gets the operation</summary>
        [EntityDataField("operation")]
        public string Operation { get; set;}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Privilege"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public Privilege(IfyContext context){
            this.context = context;
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static Privilege FromId(IfyContext context, int id){
            Privilege result = null;
            string sql = String.Format("SELECT id, id_type, name, pos, operation FROM priv WHERE id={0};", id);
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            if (reader.Read()) {
                result = new Privilege(context);
                result.Id = reader.GetInt32(0);
                result.EntityTypeId = reader.GetInt32(1);
                result.Name = reader.GetString(2);
                result.Position = reader.GetInt32(3);
                result.Operation = reader.GetString(4);
            }
            reader.Close();

            return result;
        }

        /// <summary>
        /// Froms the type and operation.
        /// </summary>
        /// <returns>The type and operation.</returns>
        /// <param name="context">Context.</param>
        /// <param name="type_id">Type identifier.</param>
        /// <param name="operation">Operation.</param>
        public static Privilege FromTypeAndOperation(IfyContext context, int type_id, string operation){
            Privilege result = null;
            string sql = String.Format("SELECT id, id_type, name, pos, operation FROM priv WHERE id_type={0} AND operation={1};",
                                       type_id,
                                       StringUtils.EscapeSql(operation));
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            if (reader.Read()) {
                result = new Privilege(context);
                result.Id = reader.GetInt32(0);
                result.EntityTypeId = reader.GetInt32(1);
                result.Name = reader.GetString(2);
                result.Position = reader.GetInt32(3);
                result.Operation = reader.GetString(4);
            }
            reader.Close();

            return result;
        }
    }
}

