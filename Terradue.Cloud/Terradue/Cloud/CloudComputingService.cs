using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Runtime.Serialization;
using System.ServiceProcess;





namespace Terradue.Cloud
{

    

	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------

    
	/// <summary>
	/// Cloud computing service.
	/// </summary>
	public abstract class CloudComputingService
	{

		public abstract string Id { get; protected set; }

		public abstract string Status { get; protected set; }

        public abstract string Debug { get; protected set; }

		public abstract string Name { get; set; }

		public virtual Uri Link { get; set; }

		public CloudComputingService ()
		{
		}

        public abstract void Start();

        public abstract void Stop();

	}


}

