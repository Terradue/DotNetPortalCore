using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Terradue.Hadoop.WebHDFS
{
    // todo - make abstract
    public class ContentSummary : Resource
    {
        // todo - should makt these immutable.
        public Int64 DirectoryCount { get; set; }
        public Int64 FileCount { get; set; }
        public Int64 Length { get; set; }
        public Int64 Quota { get; set; }
        public Int64 SpaceConsumed { get; set; }

        private Int64 spaceQuota;

        public Int64 SpaceQuota {
            get {
                return spaceQuota;
            }
        }

        public ContentSummary(JObject value)
        {
            DirectoryCount = value.Value<Int64>("directoryCount");
            FileCount = value.Value<Int64>("fileCount");
            Length = value.Value<Int64>("length");
            Quota = value.Value<Int64>("quota");
            SpaceConsumed = value.Value<Int64>("spaceConsumed");
            spaceQuota = value.Value<Int64>("spaceQuota");
            Info = value;
        }
    }
}
