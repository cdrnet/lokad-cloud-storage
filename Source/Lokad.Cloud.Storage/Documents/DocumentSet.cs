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
            var result = Blobs.GetBlob<TDocument>(LocationOfKey(key), Serializer);
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
            return Blobs.DeleteBlobIfExist(LocationOfKey(key));
        }

        /// <summary>
        /// Write the document. If it already exists, overwrite it.
        /// </summary>
        public void InsertOrReplace(TKey key, TDocument document)
        {
            Blobs.PutBlob(LocationOfKey(key), document, true, Serializer);
        }

        /// <summary>
        /// If the document already exists, update it. If it does not exist yet, do nothing.
        /// </summary>
        public TDocument UpdateIfExist(TKey key, Func<TDocument, TDocument> updateDocument)
        {
            return Blobs.UpdateBlobIfExist(LocationOfKey(key), updateDocument, Serializer)
                .GetValue(() => default(TDocument));
        }

        /// <summary>
        /// Load the current document, or create a default document if it does not exist yet.
        /// Then update the document with the provided update function and persist the result.
        /// </summary>
        public TDocument Update(TKey key, Func<TDocument, TDocument> updateDocument, Func<TDocument> defaultIfNotExist)
        {
            return Blobs.UpsertBlob(LocationOfKey(key), () => updateDocument(defaultIfNotExist()), updateDocument, Serializer);
        }

        /// <summary>
        /// If the document already exists, update it with the provided update function.
        /// If the document does not exist yet, insert a new document with the provided insert function.
        /// </summary>
        public TDocument UpdateOrInsert(TKey key, Func<TDocument, TDocument> updateDocument, Func<TDocument> insertDocument)
        {
            return Blobs.UpsertBlob(LocationOfKey(key), insertDocument, updateDocument, Serializer);
        }
    }
}
