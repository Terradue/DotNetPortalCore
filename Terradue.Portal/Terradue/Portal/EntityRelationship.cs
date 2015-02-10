using System;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------




namespace Terradue.Portal {

    public abstract class EntityRelationship<TOne, TMany> : Entity where TOne : Entity where TMany : Entity {

        public TOne ReferringItem { get; set; }

        /// <summary>Gets or sets the referenced item.</summary>
        /// <remarks>The referenced item is entity that is linked to the parent item.</remarks>
        public TMany ReferencedItem { get; set; }

        public EntityRelationship(IfyContext context) : base(context) {
        }

        public override void Load() {
            TMany referencedItem = null;
            referencedItem.Load();
        }

        public override void Store() {

        }

    }



}

