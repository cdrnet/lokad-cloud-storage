#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Storage.Documents
{
    public interface IEnumerableDocumentSet<TDocument, TKey, in TPrefix> : IDocumentSet<TDocument, TKey>
    {
        /// <summary>
        /// List the keys of all documents matching the provided prefix.
        /// Not all document sets will support this, those that do not will
        /// throw a NotSupportedException.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        IEnumerable<TKey> ListAllKeys(TPrefix prefix = default(TPrefix));

        /// <summary>
        /// Read all documents matching the provided prefix.
        /// </summary>
        IEnumerable<TDocument> GetAll(TPrefix prefix = default(TPrefix));

        /// <summary>
        /// Delete all document matching the provided prefix.
        /// </summary>
        void DeleteAll(TPrefix prefix = default(TPrefix));
    }
}