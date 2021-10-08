using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Web;
using System.Threading.Tasks;

namespace Terradue.Hadoop.WebHDFS
{
    using System;
    using System.Globalization;

    // based off of : http://hadoop.apache.org/docs/r1.0.0/webhdfs.html
    //todo:
    //  Auth stuff
    //  X Error handling & exception parsing... = not processing exceptoin at this point... 
    // operations:
    //  X Set Permission
    //  X Set Owner
    //  X Set Replication Factor 
    //  X Set Access or Modification Time 
    //  Delegation Token's
    //  X == Move off of static to ctor
    //  X add documentation

    // todo - should split this into two classes - low level HTTP client and high level object model.
    public class WebHDFSClient
    {
        // todo - need to add ability to pass in cancellation token

        //  "http://localhost:50070/webhdfs/v1";

        public Uri BaseUri { get; private set; }
        private string homeDirectory = string.Empty;
        private string userName;

        private IHttpHandler handler;

        public string GetAbsolutePath(string hdfsPath)
        {
            if (string.IsNullOrEmpty(hdfsPath))
            {
                return "/";
            }
            else if (hdfsPath[0] == '/')
            {
                return hdfsPath;
            }
            else if (hdfsPath.Contains(":"))
            {
                Uri uri = new Uri(hdfsPath);
                return uri.AbsolutePath;
            }
            else
            {
                return this.homeDirectory + "/" + hdfsPath;
            }
        }

        public string GetFullyQualifiedPath(string path)
        {
            path = this.GetAbsolutePath(path);
            return "hdfs://" + this.BaseUri.Host + path;

        }

        public WebHDFSClient(Uri baseUri, string userName)
        {
            this.userName = userName;
            this.BaseUri = baseUri;
            var homeDirectoryTask = this.GetHomeDirectory();
            this.homeDirectory = homeDirectoryTask;
        }

        public WebHDFSClient(string hadoopUser, WebHDFSStorageAdapter handler)
        {
            this.homeDirectory = "/user/" + hadoopUser;
            this.BaseUri = handler.BaseUri;
            this.handler = handler;
        }

        #region "read"

        /// <summary>
        /// List the statuses of the files/directories in the given path if the path is a directory. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public DirectoryListing GetDirectoryStatus(string path)
        {
            JObject files = GetWebHDFS(path, "LISTSTATUS");
            return new DirectoryListing(files);
        }

        /// <summary>
        /// Return a file status object that represents the path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public DirectoryEntry GetFileStatus(string path)
        {
            JObject file = GetWebHDFS(path, "GETFILESTATUS");
            return new DirectoryEntry(file.Value<JObject>("FileStatus"));
        }

        /// <summary>
        /// Return the current user's home directory in this filesystem. 
        /// The default implementation returns "/user/$USER/". 
        /// </summary>
        /// <returns></returns>
        public string GetHomeDirectory()
        {
            if (!string.IsNullOrEmpty(this.homeDirectory))
            {
                JObject path = GetWebHDFS("/", "GETHOMEDIRECTORY");
                this.homeDirectory = path.Value<string>("Path");
                return this.homeDirectory;
            }
            return this.homeDirectory;
        }

        /// <summary>
        /// Return the ContentSummary of a given Path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ContentSummary GetContentSummary(string path)
        {
            JObject contentSummary = GetWebHDFS(path, "GETCONTENTSUMMARY");
            return new ContentSummary(contentSummary.Value<JObject>("ContentSummary"));
        }

        /// <summary>
        /// Get the checksum of a file
        /// </summary>
        /// <param name="path">The file checksum. The default return value is null, which 
        /// indicates that no checksum algorithm is implemented in the corresponding FileSystem. </param>
        /// <returns></returns>
        public FileChecksum GetFileChecksum(string path)
        {
            JObject fileChecksum = GetWebHDFS(path, "GETFILECHECKSUM");
            return new FileChecksum(fileChecksum.Value<JObject>("FileChecksum"));
        }

        // todo, overloads with offset & length
        /// <summary>
        /// Opens an FSDataInputStream at the indicated Path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Stream OpenFile(string path)
        {
            //WebClient client = new WebClient();
            //return client.OpenRead (new Uri(this.GetUriForOperation (path) + "op=OPEN"));

            HttpWebRequest wr = (HttpWebRequest)System.Net.WebRequest.Create(this.GetUriForOperation(path) + "op=OPEN");
            wr.Method = "GET";
            wr.KeepAlive = false;
            wr.AllowAutoRedirect = true;
            wr.Connection = null;
            MemoryStream content = new MemoryStream();
            using (HttpWebResponse wres = (HttpWebResponse)wr.GetResponse())
            {
                Stream oStreamOut = wres.GetResponseStream();
                oStreamOut.CopyTo(content);
                content.Seek(0, SeekOrigin.Begin);
                wres.Close();
            }
            wr.Abort();
            return content;
        }

        /// <summary>
        /// Opens an FSDataInputStream at the indicated Path.  The offset and length will allow 
        /// you to get a subset of the file.  
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset">This includes any header bytes</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Stream OpenFile(string path, int offset, int length)
        {
            WebClient hc = this.CreateHTTPClient();
            var resp = hc.OpenRead(this.GetUriForOperation(path) + "op=OPEN&offset=" + offset.ToString() + "&length=" + length.ToString());
            return resp;
        }

        #endregion

        #region "put"
        // todo: add permissions
        /// <summary>
        /// Make the given file and all non-existent parents into directories. 
        /// Has the semantics of Unix 'mkdir -p'. Existence of the directory hierarchy is not an error. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool CreateDirectory(string path)
        {
            WebClient hc = this.CreateHTTPClient();
            var resp = hc.UploadData(this.GetUriForOperation(path) + "op=MKDIRS", "PUT", null);
            var content = JObject.Parse(resp.ToString());
            return content.Value<bool>("boolean");

        }

        /// <summary>
        /// Renames Path src to Path dst.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newPath"></param>
        /// <returns></returns>
        public bool RenameDirectory(string path, string newPath)
        {
            WebClient hc = this.CreateHTTPClient();
            var resp = hc.UploadData(this.GetUriForOperation(path) + "op=RENAME&destination=" + newPath, "PUT", null);
            var content = JObject.Parse(resp.ToString());
            return content.Value<bool>("boolean");

        }

        /// <summary>
        /// Delete a file.  Note, this will not recursively delete and will
        /// not delete if directory is not empty
        /// </summary>
        /// <param name="path">the path to delete</param>
        /// <returns>true if delete is successful else false. </returns>
        public bool DeleteDirectory(string path)
        {
            return DeleteDirectory(path, false);
        }


        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="path">the path to delete</param>
        /// <param name="recursive">if path is a directory and set to true, the directory is deleted else throws an exception.
        /// In case of a file the recursive can be set to either true or false. </param>
        /// <returns>true if delete is successful else false. </returns>
        public bool DeleteDirectory(string path, bool recursive)
        {
            WebClient hc = this.CreateHTTPClient();
            string uri = this.GetUriForOperation(path) + "op=DELETE&recursive=" + recursive.ToString().ToLower();
            var resp = hc.OpenWrite(uri, "DELETE");
            var content = JObject.Parse(resp.ToString());
            return content.Value<bool>("boolean");
        }

        /// <summary>
        /// Set permission of a path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="permissions"></param>
        public bool SetPermissions(string path, string permissions)
        {
            WebClient hc = this.CreateHTTPClient();
            hc.OpenWrite(this.GetUriForOperation(path) + "op=SETPERMISSION&permission=" + permissions, "PUT");
            return true;
        }

        /// <summary>
        /// Sets the owner for the file 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="owner">If it is null, the original username remains unchanged</param>
        public bool SetOwner(string path, string owner)
        {
            // todo, add group
            WebClient hc = this.CreateHTTPClient();
            hc.OpenWrite(this.GetUriForOperation(path) + "op=SETOWNER&owner=" + owner, "PUT");
            return true;
        }

        /// <summary>
        /// Sets the group for the file 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="group">If it is null, the original groupname remains unchanged</param>
        public bool SetGroup(string path, string group)
        {
            // todo, add group
            WebClient hc = this.CreateHTTPClient();
            hc.OpenWrite(this.GetUriForOperation(path) + "op=SETOWNER&group=" + group, "PUT");
            return true;
        }

        /// <summary>
        /// Set replication for an existing file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="replicationFactor"></param>
        /// <returns></returns>
        public bool SetReplicationFactor(string path, int replicationFactor)
        {
            WebClient hc = this.CreateHTTPClient();
            var resp = hc.OpenWrite(this.GetUriForOperation(path) + "op=SETREPLICATION&replication=" + replicationFactor.ToString(), "PUT");
            var content = JObject.Parse(resp.ToString());
            return content.Value<bool>("boolean");
        }

        /// <summary>
        /// Set access time of a file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="accessTime">Set the access time of this file. The number of milliseconds since Jan 1, 1970. 
        /// A value of -1 means that this call should not set access time</param>
        public bool SetAccessTime(string path, string accessTime)
        {
            WebClient hc = this.CreateHTTPClient();
            hc.OpenWrite(this.GetUriForOperation(path) + "op=SETTIMES&accesstime=" + accessTime, "PUT");
            return true;
        }

        /// <summary>
        /// Set modification time of a file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="modificationTime">Set the modification time of this file. The number of milliseconds since Jan 1, 1970.
        /// A value of -1 means that this call should not set modification time</param>
        public bool SetModificationTime(string path, string modificationTime)
        {
            WebClient hc = this.CreateHTTPClient();
            hc.OpenWrite(this.GetUriForOperation(path) + "op=SETTIMES&modificationtime=" + modificationTime, "PUT");
            return true;
        }

        //private   Task ProcessPut (HttpClient hc, string putLocation, StreamContent sc)
        //{
        //    var response =   hc.Put (putLocation, sc);
        //    if (response.StatusCode == HttpStatusCode.TemporaryRedirect)
        //    {
        //        var uri = response.Content.Headers.ContentLocation;

        //    }
        //    else
        //    {
        //        response.EnsureSuccessStatusCode();
        //    }
        //}

        /// <summary>
        /// Opens an FSDataOutputStream at the indicated Path. Files are overwritten by default.
        /// </summary>
        /// <param name="localFile"></param>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public string CreateFile(string localFile, string remotePath)
        {
            WebClient hc = this.CreateHTTPClient(true);
            var uri = this.GetUriForOperation(remotePath) + "op=CREATE&overwrite=true";
            //            var resp =   hc.Put (uri, null);
            //            var putLocation = resp.Headers.Location;
            var putLocation = uri;
            var fs = System.IO.File.OpenRead(localFile);
            hc.UploadData(putLocation, "PUT", ReadFully(fs));
            return hc.Headers[HttpResponseHeader.Location].ToString();
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Opens an FSDataOutputStream at the indicated Path. Files are overwritten by default.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public string CreateFile(Stream content, string remotePath, bool overwrite = false)
        {
            try
            {
                string ow;
                ow = overwrite ? "&overwrite=true" : "";
                //WebClient hc = this.CreateHTTPClient(false);
                HttpWebRequest wr = (HttpWebRequest)System.Net.WebRequest.Create(this.GetUriForOperation(remotePath) + "op=CREATE" + ow);
                wr.Method = "PUT";
                wr.KeepAlive = false;
                wr.AllowAutoRedirect = false;
                wr.Connection = null;
                string location;
                using (HttpWebResponse wres = (HttpWebResponse)wr.GetResponse())
                {
                    location = wres.Headers[HttpResponseHeader.Location].ToString();
                    wres.Close();
                }
                wr.Abort();
                wr = (HttpWebRequest)HttpWebRequest.Create(location);
                wr.Method = "PUT";
                wr.KeepAlive = false;
                wr.AllowAutoRedirect = false;
                wr.SendChunked = true;
                wr.Connection = null;
                Stream oStreamOut = wr.GetRequestStream();
                content.Seek(0, SeekOrigin.Begin);
                content.CopyTo(oStreamOut);
                oStreamOut.Close();
                using (HttpWebResponse wres = (HttpWebResponse)wr.GetResponse())
                {
                    location = wres.Headers[HttpResponseHeader.Location].ToString();
                    wres.Close();
                }
                wr.Abort();
                return location;
            }
            catch (WebException we)
            {
                StreamReader sr = new StreamReader(we.Response.GetResponseStream());
                string err = sr.ReadToEnd();
                throw new RemoteException(JObject.Parse(err)).GetException();
            }
        }

        /// <summary>
        /// Reads to end.
        /// </summary>
        /// <returns>The to end.</returns>
        /// <param name="stream">Stream.</param>
        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        /// <summary>
        /// Append to an existing file (optional operation).
        /// </summary>
        /// <param name="localFile"></param>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public string AppendFile(string localFile, string remotePath)
        {
            WebClient hc = this.CreateHTTPClient(false);
            hc.OpenWrite(this.GetUriForOperation(remotePath) + "op=APPEND", "POST");
            var postLocation = hc.Headers[HttpResponseHeader.Location].ToString();
            var fs = System.IO.File.OpenRead(localFile);
            hc.UploadData(postLocation, "POST", ReadFully(fs));
            // oddly, this is returning a 403 forbidden 
            // due to: "IOException","javaClassName":"java.io.IOException","message":"java.io.IOException: 
            // Append to hdfs not supported. Please refer to dfs.support.append configuration parameter.
            return hc.Headers[HttpResponseHeader.Location].ToString();
        }

        /// <summary>
        /// Append to an existing file (optional operation).
        /// </summary>
        /// <param name="localFile"></param>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public string AppendFile(Stream content, string remotePath)
        {
            WebClient hc = this.CreateHTTPClient(false);
            hc.UploadData(this.GetUriForOperation(remotePath) + "op=APPEND", null);
            var postLocation = hc.Headers[HttpResponseHeader.Location].ToString();
            hc.UploadData(postLocation, "POST", ReadFully(content));
            return hc.Headers[HttpResponseHeader.Location].ToString();
        }

        #endregion

        private WebClient CreateHTTPClient(bool allowAutoRedirect = true)
        {
            // todo - should probably not create these each time.
            //return this.handler != null ? new HttpClient(this.handler) : new HttpClient(new WebRequestHandler { AllowAutoRedirect = allowAutoRedirect });
            return new WebClient();
        }

        public string GetRootUri()
        {
            // todo: move to some config based mechanism
            //  "http://localhost:50070/webhdfs/v1";
            return this.BaseUri + "webhdfs/v1";
        }

        private string GetUriForOperation(string path)
        {
            string uri = this.GetRootUri();
            if (!string.IsNullOrEmpty(path))
            {
                if (path[0] == '/')
                {
                    uri += path;
                }
                else
                {
                    uri += this.homeDirectory + "/" + path;
                }
            }
            uri += "?";
            if (!string.IsNullOrEmpty(this.userName))
            {
                uri += string.Format(CultureInfo.InvariantCulture, "user.name={0}&", this.userName);
            }
            return uri;
        }

        private JObject GetWebHDFS(string path, string operation)
        {
            WebClient hc = this.CreateHTTPClient();
            string uri = this.GetUriForOperation(path);
            uri += "op=" + operation;
            var resp = hc.DownloadString(uri);
            JObject files = JObject.Parse(resp);
            return files;
        }
    }

    public class NoKeepAlivesWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                ((HttpWebRequest)request).KeepAlive = false;
            }

            return request;
        }
    }

}
