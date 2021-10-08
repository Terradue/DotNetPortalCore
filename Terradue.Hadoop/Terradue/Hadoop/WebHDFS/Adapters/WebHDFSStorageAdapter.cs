using System;
using System.Net;
using System.Web;

namespace Terradue.Hadoop.WebHDFS
{
	public abstract class WebHDFSStorageAdapter : IHttpHandler
	{
		public abstract Uri BaseUri { get; }

		#region IHttpHandler implementation

		public void ProcessRequest (HttpContext context)
		{
			throw new NotImplementedException ();
		}

		public bool IsReusable {
			get {
				return true;
			}
		}

		#endregion
	}
}

