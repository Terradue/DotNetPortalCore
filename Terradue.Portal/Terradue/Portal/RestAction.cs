using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Runtime.Serialization;

namespace Terradue.Portal
{
	[Serializable]
	[DataContract]
	public class RestAction
	{
		public RestAction ( string url, string method, string name ){
			this.Url = new Uri(url);
			this.HttpMethod = method;
			this.ActionName = name;
		}
		
		[DataMember]
		public Uri Url { get; set; }
		
		[DataMember]
		public string HttpMethod { get; set; }
		
		[DataMember]
		public string ActionName { get; set; }
	}


	
   
}
