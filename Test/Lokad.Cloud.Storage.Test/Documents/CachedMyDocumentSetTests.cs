#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using System.Runtime.Caching;
using Lokad.Cloud.Storage.Documents;
using Lokad.Cloud.Storage.InMemory;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Documents
{
    [TestFixture]
    public class CachedMyDocumentSetTests : DocumentSetTests
    {
        protected override IDocumentSet<MyDocument, int> BuildDocumentSet()
        {
            // clear cache (since it is, after all, caching)
            var cache = MemoryCache.Default;
            var keys = cache.Select(p => p.Key).ToList();
            foreach (var key in keys)
            {
                cache.Remove(key);
            }

            var blobs = new MemoryBlobStorageProvider();
            return new CachedMyDocumentSet(blobs);
        }
    }
}
