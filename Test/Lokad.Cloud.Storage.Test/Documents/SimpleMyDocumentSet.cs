#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage.Documents;

namespace Lokad.Cloud.Storage.Test.Documents
{
    /// <summary>
    /// Simple document set
    /// </summary>
    public class SimpleMyDocumentSet : DocumentSet<MyDocument, int>
    {
        public SimpleMyDocumentSet(IBlobStorageProvider blobs)
            : base(blobs,
                key => new BlobLocation("document-container", key.ToString()),
                () => new BlobLocation("document-container", ""),
                new CloudFormatter())
        {
        }
    }
}
