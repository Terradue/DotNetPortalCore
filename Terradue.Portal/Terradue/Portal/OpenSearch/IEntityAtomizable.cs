//
//  IAtomizable.cs
//
//  Author:
//       Emmanuel Mathot <emmanuel.mathot@terradue.com>
//
//  Copyright (c) 2014 Terradue

using System;
using Terradue.OpenSearch;
using System.Collections.Specialized;

namespace Terradue.Portal.OpenSearch {

    /// <summary>
    /// Interface to implement a class as an item in a generic or heterogeneous OpenSearchable entity.
    /// </summary>
    public interface IEntityAtomizable : IAtomizable {

        bool IsSearchable (System.Collections.Specialized.NameValueCollection parameters);
    }
}

