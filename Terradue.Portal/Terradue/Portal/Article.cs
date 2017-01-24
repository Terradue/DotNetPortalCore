using System;
using Terradue.OpenSearch;




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using Terradue.OpenSearch.Result;
using Terradue.ServiceModel.Syndication;






namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a news article published on the portal.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    [EntityTable("article", EntityTableConfiguration.Custom, NameField = "title", IdentifierField = "identifier", HasExtensions = true)]
    public class Article : Entity, IAtomizable, IComparable<Article> {

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

        /// <summary>Gets or sets the article author image.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("author_img")]
        public string AuthorImage{ get; set; }

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


        #region IAtomizable implementation
        public bool IsSearchable (System.Collections.Specialized.NameValueCollection parameters) { 
            string name = (this.Title != null ? this.Title : this.Identifier);
            string text = (this.TextContent != null ? this.TextContent : "");

            if (!string.IsNullOrEmpty (parameters ["q"])) {
                string q = parameters ["q"].ToLower ();

                if (!(name.ToLower ().Contains (q)
                      || this.Identifier.ToLower ().Contains (q)
                      || this.Abstract.Contains (q)
                      || text.ToLower ().Contains (q)
                      || this.Tags.ToLower ().Contains (q)))
                    return false;
            }
            return true;
        }

        public Terradue.OpenSearch.Result.AtomItem ToAtomItem(System.Collections.Specialized.NameValueCollection parameters) {

            string name = (this.Title != null ? this.Title : this.Identifier);
            var entityType = EntityType.GetEntityType(typeof(Article));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            if (!IsSearchable(parameters)) return null;

            AtomItem result = new AtomItem();

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(name);
            result.Summary = new TextSyndicationContent(Abstract);
            result.Content = new TextSyndicationContent(Content);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

            result.ReferenceData = this;
            result.PublishDate = this.Time;
            SyndicationPerson author = new SyndicationPerson(null, this.Author, null);
            result.Authors.Add(author);
            result.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));

            foreach (var tag in this.Tags.Split(",".ToCharArray())) {
                result.Categories.Add(new SyndicationCategory("tag", null, tag));
            }
            return result;
        }
        public System.Collections.Specialized.NameValueCollection GetOpenSearchParameters() {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }
        #endregion

        #region IComparable implementation

        public int CompareTo(Article other) {
            if (other == null)
                return 1;
            else
                return this.Time.CompareTo(other.Time);
        }

        #endregion
    }

}

