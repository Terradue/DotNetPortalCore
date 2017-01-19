using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Terradue.OpenSearch;

namespace Terradue.Portal.OpenSearch {

    public interface IEntitySearchable {
        KeyValuePair<string, string> GetFilterForParameter(string parameter, string value);
    }

    /// <summary>
    /// Interface to implement a class as an item in a generic or heterogeneous OpenSearchable entity.
    /// </summary>
    public class EntitySearchable : Entity, IEntitySearchable {

        public EntitySearchable(IfyContext context) : base(context) { }

        public NameValueCollection GetOpenSearchParameters() {
            NameValueCollection nvc = OpenSearchFactory.GetBaseOpenSearchParameter();
            nvc.Add("author", "{t2:author?}");
            nvc.Add("domain", "{t2:domain?}");
            return nvc;
        }

        public KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                default:
                    return new KeyValuePair<string, string>();
            }
        }
    }
}

