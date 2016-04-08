using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
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
    public class ServiceParameterConfiguration : Entity {
        
        private string values;
        private RequestParameterCollection requestParameters;
        
        //public string
        
        public Service Service { get; protected set; }
        
        public RequestParameter ServiceParameter { get; protected set; }
        
        public int SubjectType { get; protected set; }
        
        public int SubjectId { get; protected set; }

        // public bool Sort { get; protected set; }
        
        protected bool Defining { get; set; }

        protected bool Error { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new LookupList instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public ServiceParameterConfiguration(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new LookupList instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created LookupList object</returns>
        */
        public static ServiceParameterConfiguration GetInstance(IfyContext context) {
            return new ServiceParameterConfiguration(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static ServiceParameterConfiguration FromService(IfyContext context, Service service) {
            ServiceParameterConfiguration result = new ServiceParameterConfiguration(context);
            result.Service = service;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static ServiceParameterConfiguration FromServiceParameter(IfyContext context, RequestParameter requestParameter, int subjectType, int subjectId) { // !!! change to ServiceParameter
            ServiceParameterConfiguration result = new ServiceParameterConfiguration(context);
            result.ServiceParameter = requestParameter;
            result.Service = requestParameter.Service;
            result.SubjectType = subjectType;
            result.SubjectId = subjectId;

            /*XmlElement element = result.Service.GetParameterElement(result.ServiceParameter.Name);
            if (element == null || !result.ServiceParameter.GetXmlInformation(element, false, false, true)) {
                context.ReturnError(new ArgumentException("Parameter not defined"), null);
            }*/ // TODO-NEW-SERVICE
            
            //if (result.ServiceParameter

            if (subjectType != 0) {
                result.LoadConfiguration(
                        String.Format("id_service={0} AND name={1} AND id_grp{2} AND id_usr{3}",
                                result.Service.Id,
                                StringUtils.EscapeSql(result.ServiceParameter.Name),
                                subjectType == ConfigurationSubjectType.Group ? "=" + subjectId : " IS NULL",
                                subjectType == ConfigurationSubjectType.User ? "=" + subjectId : " IS NULL"
                        )
                );
            }
                    
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new LookupList instance representing the lookup list matching the specified condition.</summary>
        protected void LoadConfiguration(string condition) {
            // Sort alphabetically if there is no caption/value pair with a fixed position
            // Sort = (context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM lookup WHERE id_list={0} AND pos IS NOT NULL", Id)) == 0);

            values = null;
            IDbConnection dbConnection = context.GetDbConnection();
            if (ServiceParameter.IsConstant) {
                string sql = String.Format("SELECT value FROM serviceconfig WHERE {0} ORDER BY caption;", condition);
                IDataReader reader = context.GetQueryResult(sql, dbConnection);
                if (reader.Read()) values = context.GetValue(reader, 0);
                reader.Close();
            } else {
                string sql = String.Format("SELECT caption, value FROM serviceconfig WHERE {0} ORDER BY caption;", condition);
                IDataReader reader = context.GetQueryResult(sql, dbConnection);
                while (reader.Read()) {
                    if (values == null) values = String.Empty; else values += Environment.NewLine;
                    values += context.GetValue(reader, 0) + " = " + context.GetValue(reader, 1);
                }
                reader.Close();
            }
            context.CloseDbConnection(dbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            string sql = null;
            string valuesPrefix = String.Format("{0}, {1}{2}",
                    Service.Id,
                    StringUtils.EscapeSql(ServiceParameter.Name),
                    SubjectType == ConfigurationSubjectType.Group || SubjectType == ConfigurationSubjectType.User ? ", " + SubjectId.ToString() : String.Empty
            );
                
            if (values != null) {
                if (ServiceParameter.IsConstant) {
                    sql = String.Format("({0}, {1})",
                            valuesPrefix,
                            StringUtils.EscapeSql(values)
                    );
                    
                } else {
                    string pair;
                    int line = 0;
                    StringReader valueReader = new StringReader(values);
                        
                    while ((pair = valueReader.ReadLine()) != null) {
                        line++;
                        int pos = pair.IndexOf('=');
                        string caption = pair.Substring(0, pos).Trim();
                        string value = pair.Substring(pos + 1).Trim();
                            
                        if (sql == null) sql = String.Empty; else sql += ", ";
                        sql += String.Format("({0}, {1}, {2})",
                                valuesPrefix,
                                StringUtils.EscapeSql(caption),
                                StringUtils.EscapeSql(value)
                        );
                    }
                }
            }
            context.Execute(
                    String.Format("DELETE FROM serviceconfig WHERE id_service={0} AND name={1} AND {2}",
                            Service.Id,
                            StringUtils.EscapeSql(ServiceParameter.Name),
                            SubjectType == ConfigurationSubjectType.Group ? "id_grp=" + SubjectId : SubjectType == ConfigurationSubjectType.User ? "id_usr=" + SubjectId : "true" 
                    )
            );
            if (sql != null) {
                sql = String.Format("INSERT INTO serviceconfig (id_service, name{0}, {1}value) VALUES {2};", 
                        SubjectType == ConfigurationSubjectType.Group ? ", id_grp" : SubjectType == ConfigurationSubjectType.User ? ", id_usr" : String.Empty,
                        ServiceParameter.IsConstant ? String.Empty : "caption, ",
                        sql
                );
                //context.AddError(sql);
                context.Execute(sql);
            }
        }
    }
    
    public class ConfigurationSubjectType {
        
        public const int All = 1;
        
        public const int Group = 2;

        public const int User = 3;
    }
}

