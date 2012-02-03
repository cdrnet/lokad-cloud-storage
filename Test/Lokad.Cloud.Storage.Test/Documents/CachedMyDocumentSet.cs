#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Runtime.Caching;
using Lokad.Cloud.Storage.Documents;

namespace Lokad.Cloud.Storage.Test.Documents
{
    /// <summary>
    /// Simple document set
    /// </summary>
    public class CachedMyDocumentSet : DocumentSet<MyDocument, int>
    {
        private readonly MemoryCache _cache;

        public CachedMyDocumentSet(IBlobStorageProvider blobs)
            : base(blobs, key => new BlobLocation("document-container", key.ToString()))
        {
            _cache = MemoryCache.Default;
            Serializer = new CloudFormatter();
        }

        protected override bool TryGetCache(IBlobLocation location, out MyDocument document)
        {
            return null != (document = _cache.Get(location.ContainerName + "#" + location.Path) as MyDocument);
        }

        protected override void SetCache(IBlobLocation location, MyDocument document)
        {
            _cache.Set(
                location.ContainerName + "#" + location.Path,
                document,
                new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(1) });
        }

        protected override void RemoveCache(IBlobLocation location)
        {
            var prefix = location.ContainerName + "#" + location.Path;
            var items = _cache.Where(p => p.Key.StartsWith(prefix)).ToList();
            foreach (var item in items)
            {
                _cache.Remove(item.Key);
            }
        }
    }
}
