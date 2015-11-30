using System;

namespace Terradue.Portal {

    /// <summary>
    /// A Feature is a container for an item on the portal or on the web that the portal features
    /// in a way or another depending on the user interface (e.g. front page carousel). It has all the properties
    /// for storing the cnecessary contents (title, url, image, description).
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("feature", EntityTableConfiguration.Custom)]
    public class Feature : Entity, IComparable<Feature> {

        /// <summary>
        /// Title.
        /// </summary>
        /// <value>The title.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("title")]
        public string Title { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        /// <value>The description.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("description")]
        public string Description { get; set; }

        /// <summary>
        /// Image url representing the feature
        /// </summary>
        /// <value>The image.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("image_url")]
        public string Image { get; set; }

        /// <summary>
        /// Image style, used to customize the image apperance.
        /// </summary>
        /// <value>The image style.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("image_style")]
        public string ImageStyle { get; set; }

        /// <summary>
        /// Link text.
        /// </summary>
        /// <value>The button text.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("button_text")]
        public string ButtonText { get; set; }

        /// <summary>
        /// Link to the feature
        /// </summary>
        /// <value>The button link.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("button_link")]
        public string ButtonLink { get; set; }

        /// <summary>
        /// Position in all the features
        /// </summary>
        /// <value>The position.</value>
        [EntityDataField("pos")]
        public int Position { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Features"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public Feature(IfyContext context) : base(context) {}

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static Feature FromId(IfyContext context, int id) {
            Feature feat = new Feature(context);
            feat.Id = id;
            feat.Load();
            return feat;
        }

        /// <summary>
        /// Writes the item to the database.
        /// </summary>
//        public override void Store(){
//            //if no position, we set it to the max
//            //otherwise we increase existing ones (in case inserted amongst others)
//            if (this.Position == 0)
//                this.Position = context.GetQueryIntegerValue(string.Format("SELECT MAX(pos) FROM feature;")) + 1;
//            else {
//                if(context.GetQueryIntegerValue(string.Format("SELECT COUNT(*) FROM feature WHERE pos={0};", this.Position)) != 0)
//                    context.Execute(string.Format("UPDATE feature SET pos = pos + 1 WHERE pos >= {0};", this.Position));
//            }
//            
//            base.Store();
//        }

        #region IComparable implementation

        public int CompareTo(Feature other) {
            if (other == null)
                return 1;
            else
                return this.Position.CompareTo(other.Position);
        }

        #endregion
    }
}

