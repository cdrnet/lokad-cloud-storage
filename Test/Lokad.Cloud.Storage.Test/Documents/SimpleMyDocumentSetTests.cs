#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage.Documents;
using Lokad.Cloud.Storage.InMemory;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Documents
{
    [TestFixture]
    public class SimpleMyDocumentSetTests : DocumentSetTests
    {
        protected override IDocumentSet<MyDocument, int> BuildDocumentSet()
        {
            var blobs = new MemoryBlobStorageProvider();
            return new SimpleMyDocumentSet(blobs);
        }
    }
}
