using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
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
    
    

    [EntityTable("filter", EntityTableConfiguration.Custom, IdentifierField = "token", NameField = "name", HasOwnerReference = true)]
    public class Filter : Entity {
        
        private EntityType entityType;

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the fixed code of the entity for which the filter applies.</summary>
        /*!
            This property redefines the Entity.EntityCode property and has a different meaning.
        */
        
        [EntityDataField("id_type", IsForeignKey = true)]
        public int EntityTypeId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public new EntityType EntityType { 
            get { 
                return entityType;
            } 
            
            protected set {
                entityType = value;
                EntityTypeId = (entityType == null ? 0 : entityType.Id);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the absolute or relative URL, for which the filter was originally created.</summary>
        [EntityDataField("url")]
        public string Url { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the filter definition, a tab-separated list of <i>key=value</i> pairs.</summary>
        [EntityDataField("definition")]
        public string Definition { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Get or sets the filter definition that can be used as a query string where the <i>key=value</i> pairs are separated by <i>&</i>.</summary>
        public string QueryString {
            get {
                if (Definition == null) return String.Empty;
                return Definition.Replace("&", "%26").Replace('\t','&');
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Filter instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public Filter(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Filter instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Filter object</returns>
        */
        public static Filter GetInstance(IfyContext context) {
            return new Filter(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a Filter instance representing the filter with the specified token.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="name">the unique filter token</param>
        */
        public static Filter FromToken(IfyContext context, string token) {
            Filter result = new Filter(context);
            result.Identifier = token;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static Filter ForEntityType(IfyContext context, EntityType entityType) {
            Filter result = new Filter(context);
            result.EntityType = entityType;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Filter instance representing the task filter with the specified ID.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the task filter ID</param>
        /// <returns>the created Filter object</returns>
        */
        public static Filter FromId(IfyContext context, int id) {
            Filter result = new Filter(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
    }
        
}

