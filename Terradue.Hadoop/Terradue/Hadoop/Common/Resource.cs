using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Terradue.Hadoop
{
    public abstract class Resource
    {
        public JObject Info { get; set; }
    }

    public enum Version
    {
        V1
    }

    //public enum TypeOfJob
    //{
    //    Hive,
    //    Pig,
    //    MapReduce
    //}
}
