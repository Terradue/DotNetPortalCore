




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents an action that is executed by the background agent.</summary>
    [EntityTable("action", EntityTableConfiguration.Full)]
    public class Action : Entity {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Action instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Action(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Action instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created Action object.</returns>
        public static new Action GetInstance(IfyContext context) {
            return new Action(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Action instance representing the action with the specified database ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the action.</param>
        /// <returns>The created Action object.</returns>
        public static Action FromId(IfyContext context, int id) {
            Action result = new Action(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Action instance representing the action with the specified unique name.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="name">The unique name of the action.</param>
        /// <returns>The created Action object.</returns>
        public static Action FromIdentifier(IfyContext context, string identifier) {
            Action result = new Action(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }
        
    }
}

