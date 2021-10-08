using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Terradue.Hadoop.WebHDFS
{
    // todo - make abstract
    public class FileChecksum : Resource
    {
        // todo - should makt these immutable.
        public string Algorithm { get; set; }
        public string Checksum { get; set; }
        public int Length { get; set; }

        public FileChecksum(JObject value)
        {
            Algorithm = value.Value<string>("algorithm");
			Checksum = value.Value<string>("bytes");
			Length = value.Value<int>("length");
            Info = value;
        }
    }
}
