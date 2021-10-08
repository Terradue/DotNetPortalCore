using System;
using System.IO;
using System.Text;
using System.Net;
using Terradue.Hadoop.WebHDFS;

namespace Terradue.Cloud
{
	public class HDFSCloudComputingDriveInfo : CloudComputingDriveInfo
	{
		private WebHDFSClient hdfsClient;
        private ContentSummary contentSummary;

		public HDFSCloudComputingDriveInfo (string hostname, string userName) : base ("HDFS")
		{
            try { 
                string host = hostname;
                hdfsClient = new WebHDFSClient(new Uri("http://"+host+":50070"), userName);
                contentSummary = hdfsClient.GetContentSummary("/");
                byte[] myByteArray = System.Text.Encoding.UTF8.GetBytes("test");
                MemoryStream ms = new MemoryStream(myByteArray);
                hdfsClient.CreateFile(ms, "/test.hdfs", true);
            }
            catch ( SafeModeException e ) {
                this.error = e.Message;
            }
            catch ( Exception e ) {
                this.error = e.Message;
            }

		}

		public override long AvailableFreeSpace {
			get {
                if ( contentSummary != null )
                    return contentSummary.SpaceQuota-contentSummary.SpaceConsumed;
                return -1;
			}
		}
		
		public override long TotalSize {
			get {
                if ( contentSummary != null )
                    return contentSummary.SpaceQuota;
                return -1;
			}
		}
	}
}

