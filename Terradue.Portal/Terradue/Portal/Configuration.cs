using System;
using System.Collections.Generic;
using System.Data;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents the set of portal configuration variables.</summary>
    /// \ingroup Context
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class Configuration : Entity {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Configuration instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public Configuration(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Configuration instance.</summary>
        /// \ingroup Context
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Configuration object</returns>
        */
        public static new Configuration GetInstance(IfyContext context) {
            return new Configuration(context);
        }

    }
        
}

