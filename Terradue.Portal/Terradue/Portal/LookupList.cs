using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

    

    /// <summary>Represents a service category that can be assigned to a service.</summary>
    /*!
        More than one service categories can be assigned to a service.
    */
    [EntityTable("lookuplist", EntityTableConfiguration.Custom, NameField = "name")]
    public class LookupList : Entity {
        
        private string values;
        
        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("max_length")]
        public int MaxLength { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("system")]
        public bool IsSystemList { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool Sort { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string Values { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new LookupList instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public LookupList(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new LookupList instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created LookupList object</returns>
        */
        public static new LookupList GetInstance(IfyContext context) {
            return new LookupList(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static LookupList FromId(IfyContext context, int id) {
            LookupList result = new LookupList(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static LookupList FromName(IfyContext context, string name) {
            LookupList result = new LookupList(context);
            result.Name = name;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new LookupList instance representing the lookup list matching the specified condition.</summary>
        public override void Load() {
            base.Load();
            
            if (IsSystemList) throw new EntityNotFoundException("The requested lookup list is protected", null, null);
            
            // Sort alphabetically if there is no caption/value pair with a fixed position
            Sort = (context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM lookup WHERE id_list={0} AND pos IS NOT NULL", Id)) == 0);

            string sql = String.Format("SELECT caption, value FROM lookup WHERE id_list={0} ORDER BY {1}caption;", Id, Sort ? String.Empty : "pos, ");
            values = null;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (values == null) values = String.Empty; else values += Environment.NewLine;
                values += context.GetValue(reader, 0) + " = " + context.GetValue(reader, 1);
            }
            context.CloseQueryResult(reader, dbConnection);
            Exists = true;

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override string AlternativeIdentifyingCondition { 
            get { return String.Format("t.name={0}", StringUtils.EscapeSql(Name)); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            string sql;
            if (Id == 0) {
                sql = "INSERT INTO lookuplist (name, system, max_length) VALUES ({1}, false, {2});";
                Exists = true;
            } else {
                sql = "UPDATE lookuplist SET max_length={2} WHERE id={0};";
            }
            
            //context.AddInfo(sql);
            //context.AddInfo(String.Format(sql, Id, StringUtils.EscapeSql(Name), MaxLength));
            
            //return;
            
            IDbConnection dbConnection = context.GetDbConnection();
            context.Execute(String.Format(sql, Id, StringUtils.EscapeSql(Name), MaxLength), dbConnection);
            
            if (Id == 0) Id = context.GetInsertId(dbConnection);
            context.CloseDbConnection(dbConnection);
            
            //return;
            
    
            if (values != null) {
                string pair;
                int line = 0;
                StringReader valueReader = new StringReader(values);
                    
                sql = null;
                
                while ((pair = valueReader.ReadLine()) != null) {
                    line++;
                    int pos = pair.IndexOf('=');
                    string caption = pair.Substring(0, pos).Trim();
                    string value = pair.Substring(pos + 1).Trim();
                        
                    if (sql == null) sql = String.Empty; else sql += ", ";
                    sql += String.Format("({0}, {1}, {2}, {3})", Id, Sort ? "NULL" : line.ToString(), StringUtils.EscapeSql(caption), StringUtils.EscapeSql(value));
                }

                context.Execute(String.Format("DELETE FROM lookup WHERE id_list={0}", Id));
                sql = "INSERT INTO lookup (id_list, pos, caption, value) VALUES " + sql;
                context.Execute(sql);
            }
        }
    }
}

