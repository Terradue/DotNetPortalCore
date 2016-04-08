using System;
using System.Data;
using System.Web;
using System.Text.RegularExpressions;
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

    

    /// <summary>Represents a location on the Internet to which task results are published and from where they can be downloaded.</summary>
    /*!
        <b>Database</b>: Table <i>pubserver</i>
        
        <b>Relations to other entities</b>
        <ul>
            <li>A publish server can be shared (i.e. it belongs to all users) or owned by a specific <b>user</b></li>
            <li>Other entities using</b>: Publish servers are part of the attributes of <b>tasks</b> and <b>task schedulers</li>
            <li>Connection password (optional)</li>
            <li>Hostname</li>
            <li>Root path (to publish results of different tasks in different paths, the path should contain the <i>$(UID)</i> placeholder representing the unique task identifier)</li>
        </ul>

        <b></b>. 
        
        Ifynet allows defining publish servers as default publish servers, where one publish server can be the default shared publish server, and one publish server per user can be defined as default.
        These are selected by default in the task definition interface, so that it does not need to be explicitly specified or selected when creating a task or task scheduler.
        
        One publish server has the specific role of receiving the metadata describing task results when a publishing job has finished. This is usually the same machine as the portal, in order to have full file system control, useful for removing the results of old tasks.
        
        Publish servers must define an <b>upload URL</b> consisting of 
        <ul>
            <li>Connection protocol</li>
            <li>Connection username (optional for some protocols)</li>
            <li>Connection password (optional)</li>
            <li>Hostname</li>
            <li>Root path (to publish results of different tasks in different paths, the path should contain the <i>$(UID)</i> placeholder representing the unique task identifier)</li>
        </ul>
        
        Publish servers may define a <b>download URL</b> different from the upload URL; if it is not defined, the download URL is assumed being the same as the upload URL.
    */
    [EntityTable("pubserver", EntityTableConfiguration.Custom, NameField = "name", HasOwnerReference = true, HasPermissionManagement = true)]
    public class PublishServer : Entity {
        

        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("protocol")]
        public string Protocol { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("hostname")]
        public string Hostname { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("port")]
        public int Port { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("path")]
        public string Path { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("username")]
        private string Username { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("password")]
        private string Password { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the publish server root URL to which the results of a task are uploaded.</summary>
        [EntityDataField("upload_url")]
        public string UploadUrl { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the publish server root URL from which the results of a task can be downloaded.</summary>
        [EntityDataField("download_url")]
        public string DownloadUrl { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the publish server local directory at which the result files of a task can be accessed via the file system.</summary>
        [EntityDataField("file_root")]
        public string FileRootDir { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the publish server is the default publish server for its owner or for all users.</summary>
        /// <remarks>If the publish server has no owner, it is considered as shared by all users. There can be only one shared default publish server and only one per user.</remarks>
        [EntityDataField("is_default")]
        public bool IsDefaultForOwner { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the publish server is used for storing result metadata files of completed tasks.</summary>
        /// <remarks>Only one publish server per web portal may have be used for this purpose. It is usually an FTP server on the web portal host.</remarks>
        [EntityDataField("metadata")]
        public bool ReceivesTaskResultMetadata { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new PublishServer instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public PublishServer(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new PublishServer instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created PublishServer object</returns>
        public static PublishServer GetInstance(IfyContext context) {
            return new PublishServer(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new PublishServer instance representing the publish server with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the publish server ID</param>
        /// <returns>the created PublishServer object</returns>
        public static PublishServer FromId(IfyContext context, int id) {
            PublishServer result = new PublishServer(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new PublishServer instance representing the publish server with the specified address or caption.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="address">the publish server address or caption</param>
        /// <returns>the created PublishServer object</returns>
        */
        public static PublishServer FromAddress(IfyContext context, string address) {
            PublishServer result = new PublishServer(context);
            result.Hostname = address;
            result.Name = address;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a PublishServer instance representing the publish server with the specified ID, address or caption.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="s">a search value that must match the publish server ID (preferred), address or caption</param>
        */
        public static PublishServer FromString(IfyContext context, string s) {
            int id = 0;
            Int32.TryParse(s, out id);
            PublishServer result = new PublishServer(context);
            result.Id = id;
            result.Hostname = s;
            result.Name = s;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the publish server information from the database.</summary>
        /*!
        /// <param name="condition">SQL conditional expression without WHERE keyword</param>
        */
        public override void Load() {
            base.Load();
            
            if (UploadUrl == null) {
                UploadUrl = String.Format("{0}://{1}{2}{3}{4}{5}{6}",
                        Protocol,
                        Username == null ? String.Empty : Username,
                        Username == null || Password == null ? String.Empty : ":" + Password,
                        Username == null ? String.Empty : "@",
                        Hostname,
                        Port == 0 ? String.Empty : ":" + Port,
                        Path == null ? String.Empty : "/" + Path
                        
                );
            }
            
            if (DownloadUrl == null && UploadUrl != null) DownloadUrl = Regex.Replace(UploadUrl, @"://.*@", "://");
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            base.Store();
            string credentials = (Username == null ? String.Empty : HttpUtility.UrlEncode(Username) + (Password == null ? String.Empty : ":" + HttpUtility.UrlEncode(Password)) + "@");
            UploadUrl = String.Format("{0}://{1}{2}{3}{4}",
                    Protocol,
                    credentials,
                    Hostname,
                    Port == 0 ? String.Empty : String.Format(":{0}", Port),
                    Path == null ? String.Empty : String.Format("/{0}", Path)
            );
            if (IsDefaultForOwner) context.Execute(String.Format("UPDATE pubserver SET is_default=(id={0}) WHERE id_usr{1};", Id, (OwnerId == 0 ? " IS NULL" : String.Format("={0}", OwnerId))));
            if (ReceivesTaskResultMetadata) context.Execute(String.Format("UPDATE pubserver SET metadata=(id={0});", Id));

        }
        
    }

}

