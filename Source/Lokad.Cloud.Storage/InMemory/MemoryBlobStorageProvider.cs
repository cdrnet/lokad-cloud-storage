#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.InMemory
{
    /// <summary>Mock in-memory Blob Storage.</summary>
    /// <remarks>
    /// All the methods of <see cref="MemoryBlobStorageProvider"/> are thread-safe.
    /// Note that the blob lease implementation is simplified such that leases do not time out.
    /// </remarks>
    public class MemoryBlobStorageProvider : IBlobStorageProvider
    {
        /// <summary> Containers Property.</summary>
        Dictionary<string, MockContainer> Containers { get { return _containers;} }
        readonly Dictionary<string, MockContainer> _containers;
        
        /// <summary>naive global lock to make methods thread-safe.</summary>
        readonly object _syncRoot;

        internal IDataSerializer DataSerializer { get; set; }

        /// <remarks></remarks>
        public MemoryBlobStorageProvider()
        {
            _containers = new Dictionary<string, MockContainer>();
            _syncRoot = new object();
            DataSerializer = new CloudFormatter();
        }

        /// <remarks></remarks>
        public IEnumerable<string> ListContainers(string prefix = null)
        {
            lock (_syncRoot)
            {
                if (String.IsNullOrEmpty(prefix))
                {
                    return Containers.Keys;
                }

                return Containers.Keys.Where(key => key.StartsWith(prefix));
            }
        }

        /// <remarks></remarks>
        public bool CreateContainerIfNotExist(string containerName)
        {
            lock (_syncRoot)
            {
                if (!BlobStorageExtensions.IsContainerNameValid(containerName))
                {
                    throw new NotSupportedException("the containerName is not compliant with azure constraints on container names");
                }

                if (Containers.Keys.Contains(containerName))
                {
                    return false;
                }
                
                Containers.Add(containerName, new MockContainer());
                return true;
            }	
        }

        /// <remarks></remarks>
        public bool DeleteContainerIfExist(string containerName)
        {
            lock (_syncRoot)
            {
                if (!Containers.Keys.Contains(containerName))
                {
                    return false;
                }

                Containers.Remove(containerName);
                return true;
            }
        }

        /// <remarks></remarks>
        public IEnumerable<string> ListBlobNames(string containerName, string blobNamePrefix = null)
        {
            lock (_syncRoot)
            {
                if (!Containers.Keys.Contains(containerName))
                {
                    return Enumerable.Empty<string>();
                }

                var names = Containers[containerName].BlobNames;
                return String.IsNullOrEmpty(blobNamePrefix) ? names : names.Where(name => name.StartsWith(blobNamePrefix));
            }
        }

        /// <remarks></remarks>
        public IEnumerable<T> ListBlobs<T>(string containerName, string blobNamePrefix = null, int skip = 0)
        {
            var names = ListBlobNames(containerName, blobNamePrefix);

            if (skip > 0)
            {
                names = names.Skip(skip);
            }

            return names.Select(name => GetBlob<T>(containerName, name))
                .Where(blob => blob.HasValue)
                .Select(blob => blob.Value);
        }

        /// <remarks></remarks>
        public bool DeleteBlobIfExist(string containerName, string blobName)
        {
            lock (_syncRoot)
            {
                if (!Containers.Keys.Contains(containerName) || !Containers[containerName].BlobNames.Contains(blobName))
                {
                    return false;
                }

                Containers[containerName].RemoveBlob(blobName);
                return true;
            }
        }

        /// <remarks></remarks>
        public void DeleteAllBlobs(string containerName, string blobNamePrefix = null)
        {
            foreach (var blobName in ListBlobNames(containerName, blobNamePrefix))
            {
                DeleteBlobIfExist(containerName, blobName);
            }
        }

        /// <remarks></remarks>
        public Maybe<T> GetBlob<T>(string containerName, string blobName)
        {
            string ignoredEtag;
            return GetBlob<T>(containerName, blobName, out ignoredEtag);
        }

        /// <remarks></remarks>
        public Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag)
        {
            return GetBlob(containerName, blobName, typeof(T), out etag)
                .Convert(o => o is T ? (T)o : Maybe<T>.Empty, Maybe<T>.Empty);
        }

        /// <remarks></remarks>
        public Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag)
        {
            lock (_syncRoot)
            {
                if (!Containers.ContainsKey(containerName)
                    || !Containers[containerName].BlobNames.Contains(blobName))
                {
                    etag = null;
                    return Maybe<object>.Empty;
                }

                etag = Containers[containerName].BlobsEtag[blobName];
                return Containers[containerName].GetBlob(blobName);
            }
        }

        /// <remarks></remarks>
        public Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag)
        {
            etag = null;

            var formatter = DataSerializer as IIntermediateDataSerializer;
            if (formatter == null)
            {
                return Maybe<XElement>.Empty;
            }

            object data;
            lock (_syncRoot)
            {
                if (!Containers.ContainsKey(containerName)
                    || !Containers[containerName].BlobNames.Contains(blobName))
                {
                    return Maybe<XElement>.Empty;
                }

                etag = Containers[containerName].BlobsEtag[blobName];
                data = Containers[containerName].GetBlob(blobName);
            }

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(data, stream, data.GetType());
                stream.Position = 0;
                return formatter.UnpackXml(stream);
            }
        }

        /// <remarks></remarks>
        public Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags)
        {
            // Copy-paste from BlobStorageProvider.cs

            var tempResult = blobNames.Select(blobName =>
            {
                string etag;
                var blob = GetBlob<T>(containerName, blobName, out etag);
                return new System.Tuple<Maybe<T>, string>(blob, etag);
            }).ToArray();

            etags = new string[blobNames.Length];
            var result = new Maybe<T>[blobNames.Length];

            for (int i = 0; i < tempResult.Length; i++)
            {
                result[i] = tempResult[i].Item1;
                etags[i] = tempResult[i].Item2;
            }

            return result;
        }

        /// <remarks></remarks>
        public Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag)
        {
            lock (_syncRoot)
            {
                string currentEtag = GetBlobEtag(containerName, blobName);

                if (currentEtag == oldEtag)
                {
                    newEtag = null;
                    return Maybe<T>.Empty;
                }

                newEtag = currentEtag;
                return GetBlob<T>(containerName, blobName);
            }
        }

        /// <remarks></remarks>
        public string GetBlobEtag(string containerName, string blobName)
        {
            lock (_syncRoot)
            {
                return (Containers.ContainsKey(containerName) && Containers[containerName].BlobNames.Contains(blobName))
                    ? Containers[containerName].BlobsEtag[blobName]
                    : null;
            }
        }

        /// <remarks></remarks>
        public void PutBlob<T>(string containerName, string blobName, T item)
        {
            PutBlob(containerName, blobName, item, true);
        }

        /// <remarks></remarks>
        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite)
        {
            string ignored;
            return PutBlob(containerName, blobName, item, overwrite, out ignored);
        }

        /// <remarks></remarks>
        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag)
        {
            return PutBlob(containerName, blobName, item, typeof(T), overwrite, out etag);
        }

        /// <remarks></remarks>
        public bool PutBlob<T>(string containerName, string blobName, T item, string expectedEtag)
        {
            string ignored;
            return PutBlob(containerName, blobName, item, typeof (T), true, expectedEtag, out ignored);
        }

        /// <remarks></remarks>
        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string etag)
        {
            return PutBlob(containerName, blobName, item, type, overwrite, null, out etag);
        }

        /// <remarks></remarks>
        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, out string etag)
        {
            lock(_syncRoot)
            {
                etag = null;
                if(Containers.ContainsKey(containerName))
                {
                    if(Containers[containerName].BlobNames.Contains(blobName))
                    {
                        if(!overwrite || expectedEtag != null && expectedEtag != Containers[containerName].BlobsEtag[blobName])
                        {
                            return false;
                        }

                        using (var stream = new MemoryStream())
                        {
                            DataSerializer.Serialize(item, stream, type);
                        }

                        Containers[containerName].SetBlob(blobName, item);
                        etag = Containers[containerName].BlobsEtag[blobName];
                        return true;
                    }

                    Containers[containerName].AddBlob(blobName, item);
                    etag = Containers[containerName].BlobsEtag[blobName];
                    return true;
                }

                if (!BlobStorageExtensions.IsContainerNameValid(containerName))
                {
                    throw new NotSupportedException("the containerName is not compliant with azure constraints on container names");
                }

                Containers.Add(containerName, new MockContainer());

                using (var stream = new MemoryStream())
                {
                    DataSerializer.Serialize(item, stream, type);
                }

                Containers[containerName].AddBlob(blobName, item);
                etag = Containers[containerName].BlobsEtag[blobName];
                return true;
            }
        }

        /// <remarks></remarks>
        public Maybe<T> UpdateBlobIfExist<T>(string containerName, string blobName, Func<T, T> update)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, t => update(t));
        }

        /// <remarks></remarks>
        public Maybe<T> UpdateBlobIfExistOrSkip<T>(string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update);
        }

        /// <remarks></remarks>
        public Maybe<T> UpdateBlobIfExistOrDelete<T>(string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            var result = UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update);
            if (!result.HasValue)
            {
                DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        /// <remarks></remarks>
        public T UpsertBlob<T>(string containerName, string blobName, Func<T> insert, Func<T, T> update)
        {
            return UpsertBlobOrSkip<T>(containerName, blobName, () => insert(), t => update(t)).Value;
        }

        /// <remarks></remarks>
        public Maybe<T> UpsertBlobOrSkip<T>(
            string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            lock (_syncRoot)
            {
                Maybe<T> input;
                if (Containers.ContainsKey(containerName))
                {
                    if (Containers[containerName].BlobNames.Contains(blobName))
                    {
                        var blobData = Containers[containerName].GetBlob(blobName);
                        input = blobData == null ? Maybe<T>.Empty : (T)blobData;
                    }
                    else
                    {
                        input = Maybe<T>.Empty;
                    }
                }
                else
                {
                    Containers.Add(containerName, new MockContainer());
                    input = Maybe<T>.Empty;
                }

                var output = input.HasValue ? update(input.Value) : insert();

                if (output.HasValue)
                {
                    Containers[containerName].SetBlob(blobName, output.Value);
                }

                return output;
            }
        }

        /// <remarks></remarks>
        public Maybe<T> UpsertBlobOrDelete<T>(
            string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            var result = UpsertBlobOrSkip(containerName, blobName, insert, update);
            if (!result.HasValue)
            {
                DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        class MockContainer
        {
            readonly Dictionary<string, object> _blobSet;
            readonly Dictionary<string, string> _blobsEtag;
            readonly Dictionary<string, string> _blobsLeases;

            public string[] BlobNames { get { return _blobSet.Keys.ToArray(); } }

            public Dictionary<string, string> BlobsEtag { get { return _blobsEtag; } }
            public Dictionary<string, string> BlobsLeases { get { return _blobsLeases; } }

            public MockContainer()
            {
                _blobSet = new Dictionary<string, object>();
                _blobsEtag = new Dictionary<string, string>();
                _blobsLeases = new Dictionary<string, string>();
            }

            public void SetBlob(string blobName, object item)
            {
                _blobSet[blobName] = item;
                _blobsEtag[blobName] = Guid.NewGuid().ToString();
            }

            public object GetBlob(string blobName)
            {
                return _blobSet[blobName];
            }

            public void AddBlob(string blobName, object item)
            {
                _blobSet.Add(blobName, item);
                _blobsEtag.Add(blobName, Guid.NewGuid().ToString());
            }

            public void RemoveBlob(string blobName)
            {
                _blobSet.Remove(blobName);
                _blobsEtag.Remove(blobName);
                _blobsLeases.Remove(blobName);
            }
        }

        /// <remarks></remarks>
        public Result<string> TryAcquireLease(string containerName, string blobName)
        {
            lock (_syncRoot)
            {
                if (!Containers[containerName].BlobsLeases.ContainsKey(blobName))
                {
                    var leaseId = Guid.NewGuid().ToString("N");
                    Containers[containerName].BlobsLeases[blobName] = leaseId;
                    return Result.CreateSuccess(leaseId);
                }

                return Result<string>.CreateError("Conflict");
            }
        }

        /// <remarks></remarks>
        public bool TryReleaseLease(string containerName, string blobName, string leaseId)
        {
            lock (_syncRoot)
            {
                string actualLeaseId;
                if (Containers[containerName].BlobsLeases.TryGetValue(blobName, out actualLeaseId) && actualLeaseId == leaseId)
                {
                    Containers[containerName].BlobsLeases.Remove(blobName);
                    return true;
                }

                return false;
            }
        }

        /// <remarks></remarks>
        public bool TryRenewLease(string containerName, string blobName, string leaseId)
        {
            lock (_syncRoot)
            {
                string actualLeaseId;
                return Containers[containerName].BlobsLeases.TryGetValue(blobName, out actualLeaseId)
                    && actualLeaseId == leaseId;
            }
        }
    }
}
