using System;
using Terradue.OpenSearch;




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------






namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a news article published on the portal.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    [EntityTable("article", EntityTableConfiguration.Custom, NameField = "title", IdentifierField = "identifier", HasExtensions = true)]
    public class Article : Entity {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the content of the text.</summary>
        public override string TextContent {
            get {
                return (this.Content != null ? this.Content : this.Abstract);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the title of the article.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public string Title { 
            get { return Name; }
            set { Name = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the abstract of the article.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("abstract")]
        public string Abstract { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the article content.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("content")]
        public string Content { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the publication time.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("time")]
        public DateTime Time { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the URL at which the article can be accessed.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("url")]
        public string Url { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the article author.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("author")]
        public string Author { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the tags for the article.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("tags")]
        public string Tags { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Article instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Article(IfyContext context) : base(context) {
            this.Time = DateTime.UtcNow;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Article instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created article object</returns>
        public static Article GetInstance(IfyContext context) {
            return new Article(context);
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static Article FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(Article));
            Article result = (Article)entityType.GetEntityInstanceFromId(context, id);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>
        /// Writes the item to the database.
        /// </summary>
        public override void Store() {
            if (base.Identifier == null) base.Identifier = "0";
            base.Store();
            if (base.Identifier == "0") {
                base.Identifier = String.Format(this.Id.ToString());
                base.Store();
            }
        }
    }

}

