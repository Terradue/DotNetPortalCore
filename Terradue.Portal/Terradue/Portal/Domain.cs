using System;
using System.Collections.Generic;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Domain</summary>
    /// <description>A Domain is an organizational unit to regroup \ref Entity</description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("domain", EntityTableConfiguration.Custom, NameField = "name")]
    public class Domain : Entity {
        
        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("description")]
        /// <summary>Description</summary>
        /// <description>Human readable description of the domain</description>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string Description { get; set; } 

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Domain instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Domain(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Domain instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created Domain object.</returns>
        public static new Domain GetInstance(IfyContext context) {
            return new Domain(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Domain instance representing the domain with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The domain ID.</param>
        /// <returns>The created Domain object.</returns>
        public static Domain FromId(IfyContext context, int id) {
            Domain result = new Domain(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
    }

}

