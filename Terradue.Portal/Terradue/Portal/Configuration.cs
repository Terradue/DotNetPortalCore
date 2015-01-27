using System;
using System.Collections.Generic;
using System.Data;




/*!
\defgroup core_Configuration Configuration
@{
This component manages the configuration data in the database for all other components of the system. Basically, it saves and loads key/value pairs in the database on the behalf of all other components of the system.
\ingroup core
 
\section sec_core_ConfigurationDependencies Dependencies
 
- \ref core_DataModelAccess

@}
 */

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
    /// \ingroup core_Configuration
    /// \xrefitem uml "UML" "UML Diagram"
    public class Configuration : Entity {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Configuration instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public Configuration(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Configuration instance.</summary>
        /// \ingroup core_Configuration
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Configuration object</returns>
        */
        public static new Configuration GetInstance(IfyContext context) {
            return new Configuration(context);
        }

    }
        
}

