using System;

namespace Terradue.Portal {

    /// <summary>
    /// Feature.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("feature", EntityTableConfiguration.Custom)]
    public class Feature : Entity {

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("image_url")]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the image style, used to customize the image apperance.
        /// </summary>
        /// <value>The image style.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("image_style")]
        public string ImageStyle { get; set; }

        /// <summary>
        /// Gets or sets the button text.
        /// </summary>
        /// <value>The button text.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("button_text")]
        public string ButtonText { get; set; }

        /// <summary>
        /// Gets or sets the button link.
        /// </summary>
        /// <value>The button link.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("button_link")]
        public string ButtonLink { get; set; }

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
    }
}

