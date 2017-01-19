using System;
using System.Collections.Specialized;
using Terradue.OpenSearch;

namespace Terradue.Portal.OpenSearch {

    /// <summary>
    /// Interface to implement a class as an item in a generic or heterogeneous OpenSearchable entity.
    /// </summary>
    public interface IEntitySearchable : IAtomizable {

        NameValueCollection GetParameters();

        bool IsSearchable(NameValueCollection parameters);

        string GetSqlCondition(NameValueCollection parameters);
    }
}

