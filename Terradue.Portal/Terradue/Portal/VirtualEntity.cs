using System;

namespace Terradue.Portal {
    public class VirtualEntity {

        protected IfyContext context;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the database ID, i.e. the numeric key value of an entity item.</summary>
        /// <remarks>The value is <c>0</c> if the item is not (yet) persistently stored in the database.</remarks>
        public int Id { get; protected set; }
        public string Identifier { get; set; }
        public virtual string Name { get; set; }

        public VirtualEntity(IfyContext context) {
            this.context = context;
        }
    }
}

