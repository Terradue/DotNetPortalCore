using System;
using System.Collections.Generic;
using System.Data;
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
    public class ServiceCategory : Entity {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ServiceCategory instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public ServiceCategory(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ServiceCategory instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created ServiceCategory object</returns>
        */
        public static ServiceCategory GetInstance(IfyContext context) {
            return new ServiceCategory(context);
        }
        
    }
}

