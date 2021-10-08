using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace Terradue.Hadoop.WebHDFS {
    public class RemoteException : Resource {

        // todo - should makt these immutable.
        public string Exception { get; set; }
        public string JavaClassName { get; set; }
        public string Message { get; set; }

        public RemoteException() {
        }


        public RemoteException(JObject value)
        {
            Exception = value.Value<string>("exception");
            JavaClassName = value.Value<string>("javaClassName");
            Message = value.Value<string>("message");
            Info = value;
        }

        public Exception GetException (){
            if (Message.Contains("org.apache.hadoop.hdfs.server.namenode.SafeModeException"))
                return new SafeModeException(Message);
            return new WebException(this.Message);
        }

    }
}

