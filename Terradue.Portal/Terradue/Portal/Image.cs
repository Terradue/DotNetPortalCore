using System;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a featured image on the portal</summary>
    [EntityTable("image", EntityTableConfiguration.Custom)]
    public class Image : Entity {

        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        /// <value>The caption.</value>
        [EntityDataField("caption")]
        public string Caption { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [EntityDataField("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        [EntityDataField("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the small URL.
        /// </summary>
        /// <value>The small URL.</value>
        [EntityDataField("small_url")]
        public string SmallUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Image instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public Image(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Image instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Image object</returns>
        */
        public static new Image GetInstance(IfyContext context) {
            return new Image(context);
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static Image FromId(IfyContext context, int id) {
            Image result = new Image(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
    }

}

