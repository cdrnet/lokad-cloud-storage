#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Storage.Documents
{
    public class EnumerableDocumentSet<TDocument, TKey, TPrefix> : DocumentSet<TDocument, TKey>, IEnumerableDocumentSet<TDocument, TKey, TPrefix>
    {
        private readonly Func<TPrefix, IBlobLocation> _locationOfPrefix;

        public EnumerableDocumentSet(
            IBlobStorageProvider blobs,
            Func<TKey, IBlobLocation> locationOfKey,
            Func<TPrefix, IBlobLocation> locationOfPrefix,
            IDataSerializer serializer = null)
            : base(blobs, locationOfKey, serializer)
        {
            _locationOfPrefix = locationOfPrefix;
        }

        protected IBlobLocation LocationOfPrefix(TPrefix prefix)
        {
            return _locationOfPrefix(prefix);
        }

        /// <summary>
        /// List the keys of all documents matching the provided prefix.
        /// Not all document sets will support this, those that do not will
        /// throw a NotSupportedException.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public virtual IEnumerable<TKey> ListAllKeys(TPrefix prefix = default(TPrefix))
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Read all documents matching the provided prefix.
        /// </summary>
        public IEnumerable<TDocument> GetAll(TPrefix prefix = default(TPrefix))
        {
            return Blobs.ListBlobs<TDocument>(LocationOfPrefix(prefix), serializer: Serializer);
        }

        /// <summary>
        /// Delete all document matching the provided prefix.
        /// </summary>
        public void DeleteAll(TPrefix prefix = default(TPrefix))
        {
            Blobs.DeleteAllBlobs(LocationOfPrefix(prefix));
        }
    }
}
