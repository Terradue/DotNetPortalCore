using System;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Terradue.Hadoop.WebHDFS
{
	public class SafeModeException : System.Exception
	{
        private string message;
        public override string Message {
            get {
                return message;
            }
        }
        private string stackTrace;
        public override string StackTrace {
            get {
                return stackTrace;
            }
        }

        public SafeModeException ( string message ) : base(message) {
            Match match = Regex.Match(message,"^org\\.apache\\.hadoop\\.hdfs\\.server\\.namenode\\.SafeModeException:(?<error>.*)$",RegexOptions.Multiline);
            this.message = match.Groups["error"].Value;
            this.stackTrace = message;
        }
	}


}

