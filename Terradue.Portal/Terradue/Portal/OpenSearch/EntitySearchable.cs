using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;

namespace Terradue.Portal.OpenSearch {

    public interface IEntitySearchable : IAtomizable {
        KeyValuePair<string, string> GetFilterForParameter(string parameter, string value);

        bool IsPostFiltered (NameValueCollection parameters);
    }

    /// <summary>
    /// Interface to implement a class as an item in a generic or heterogeneous OpenSearchable entity.
    /// </summary>
    public abstract class EntitySearchable : Entity, IEntitySearchable {

        public EntitySearchable(IfyContext context) : base(context) { }

        public NameValueCollection GetOpenSearchParameters() {
            NameValueCollection nvc = OpenSearchFactory.GetBaseOpenSearchParameter();
            nvc.Add("author", "{t2:author?}");
            nvc.Add("domain", "{t2:domain?}");
            return nvc;
        }

        public virtual KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "q":
                    return new KeyValuePair<string, string>("Name", "*" + value + "*");
                default:
                    return new KeyValuePair<string, string>();
            }
        }

        public abstract AtomItem ToAtomItem (NameValueCollection parameters);

        public virtual bool IsPostFiltered (NameValueCollection parameters) {
            return false;
        }
    }
}

