#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage.Documents
{
    /// <summary>
    /// Represents a set of documents and how they are persisted.
    /// </summary>
    public class DocumentSet<TDocument, TKey> : IDocumentSet<TDocument, TKey>
    {
        public DocumentSet(IBlobStorageProvider blobs, Func<TKey, IBlobLocation> locationOfKey, IDataSerializer serializer = null)
        {
            Blobs = blobs;
            Serializer = serializer;
            LocationOfKey = locationOfKey;
        }

        protected IBlobStorageProvider Blobs { get; private set; }
        protected Func<TKey, IBlobLocation> LocationOfKey { get; private set; }
        protected IDataSerializer Serializer { get; set; }

        /// <summary>
        /// Try to read the document, if it exists.
        /// </summary>
        public bool TryGet(TKey key, out TDocument document)
        {
            var location = LocationOfKey(key);
            if (TryGetCache(location, out document))
            {
                return true;
            }

            var result = Blobs.GetBlob<TDocument>(location, Serializer);
            if (!result.HasValue)
            {
                document = default(TDocument);
                return false;
            }

            document = result.Value;
            return true;
        }

        /// <summary>
        /// Delete the document, if it exists.
        /// </summary>
        public bool DeleteIfExist(TKey key)
        {
            var location = LocationOfKey(key);
            RemoveCache(location);
            return Blobs.DeleteBlobIfExist(location);
        }

        /// <summary>
        /// Write the document. If it already exists, overwrite it.
        /// </summary>
        public void InsertOrReplace(TKey key, TDocument document)
        {
            var location = LocationOfKey(key);
            if (Blobs.PutBlob(location, document, true, Serializer))
            {
                SetCache(location, document);
            }
        }

        /// <summary>
        /// If the document already exists, update it. If it does not exist yet, do nothing.
        /// </summary>
        public TDocument UpdateIfExist(TKey key, Func<TDocument, TDocument> updateDocument)
        {
            var location = LocationOfKey(key);
            var result = Blobs.UpdateBlobIfExist(location, updateDocument, Serializer);
            if (!result.HasValue)
            {
                return default(TDocument);
            }

            SetCache(location, result.Value);
            return result.Value;
        }

        /// <summary>
        /// Load the current document, or create a default document if it does not exist yet.
        /// Then update the document with the provided update function and persist the result.
        /// </summary>
        public TDocument Update(TKey key, Func<TDocument, TDocument> updateDocument, Func<TDocument> defaultIfNotExist)
        {
            var location = LocationOfKey(key);
            var document = Blobs.UpsertBlob(location, () => updateDocument(defaultIfNotExist()), updateDocument, Serializer);
            SetCache(location, document);
            return document;
        }

        /// <summary>
        /// If the document already exists, update it with the provided update function.
        /// If the document does not exist yet, insert a new document with the provided insert function.
        /// </summary>
        public TDocument UpdateOrInsert(TKey key, Func<TDocument, TDocument> updateDocument, Func<TDocument> insertDocument)
        {
            var location = LocationOfKey(key);
            var document = Blobs.UpsertBlob(location, insertDocument, updateDocument, Serializer);
            SetCache(location, document);
            return document;
        }

        /// <summary>
        /// Override this method to plug in your cache provider, if needed.
        /// By default, no caching is performed.
        /// </summary>
        protected virtual bool TryGetCache(IBlobLocation location, out TDocument document)
        {
            document = default(TDocument);
            return false;
        }

        /// <summary>
        /// Override this method to plug in your cache provider, if needed.
        /// By default, no caching is performed.
        /// </summary>
        protected virtual void SetCache(IBlobLocation location, TDocument document)
        {
        }

        /// <summary>
        /// Override this method to plug in your cache provider, if needed.
        /// By default, no caching is performed.
        /// </summary>
        protected virtual void RemoveCache(IBlobLocation location)
        {
        }
    }
}
