#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.Storage.Shared;
using Lokad.Cloud.Storage.Shared.Diagnostics;
using Lokad.Cloud.Storage.Shared.Logging;
using Lokad.Cloud.Storage.Shared.Threading;
using Lokad.Cloud.Storage.SystemObservers;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>Provides access to the Blob Storage.</summary>
    /// <remarks>
    /// All the methods of <see cref="BlobStorageProvider"/> are thread-safe.
    /// </remarks>
    public class BlobStorageProvider : IBlobStorageProvider
    {
        /// <summary>Custom meta-data used as a work-around of an issue of the StorageClient.</summary>
        /// <remarks>[vermorel 2010-11] The StorageClient for odds reasons do not enable the
        /// retrieval of the Content-MD5 property when performing a GET on blobs. In order to validate
        /// the integrity during the entire roundtrip, we need to apply a suplementary header
        /// used to perform the MD5 check.</remarks>
        private const string MetadataMD5Key = "LokadContentMD5";

        readonly CloudBlobClient _blobStorage;
        readonly IDataSerializer _serializer;
        readonly ILog _log;
        readonly AzurePolicies _policies;

        // Instrumentation
        readonly ExecutionCounter _countPutBlob;
        readonly ExecutionCounter _countGetBlob;
        readonly ExecutionCounter _countGetBlobIfModified;
        readonly ExecutionCounter _countUpsertBlobOrSkip;
        readonly ExecutionCounter _countDeleteBlob;

        /// <summary>IoC constructor.</summary>
        /// <param name="systemObserver">Can be <see langword="null"/>.</param>
        public BlobStorageProvider(CloudBlobClient blobStorage, IDataSerializer serializer, ICloudStorageSystemObserver systemObserver, ILog log = null)
        {
            _policies = new AzurePolicies(systemObserver);
            _blobStorage = blobStorage;
            _serializer = serializer;
            _log = log;

            // Instrumentation
            ExecutionCounters.Default.RegisterRange(new[]
                {
                    _countPutBlob = new ExecutionCounter("BlobStorageProvider.PutBlob", 0, 0),
                    _countGetBlob = new ExecutionCounter("BlobStorageProvider.GetBlob", 0, 0),
                    _countGetBlobIfModified = new ExecutionCounter("BlobStorageProvider.GetBlobIfModified", 0, 0),
                    _countUpsertBlobOrSkip = new ExecutionCounter("BlobStorageProvider.UpdateBlob", 0, 0),
                    _countDeleteBlob = new ExecutionCounter("BlobStorageProvider.DeleteBlob", 0, 0),
                });
        }

        public IEnumerable<string> ListContainers(string containerNamePrefix = null)
        {
            var enumerator = String.IsNullOrEmpty(containerNamePrefix)
                ? _blobStorage.ListContainers().GetEnumerator()
                : _blobStorage.ListContainers(containerNamePrefix).GetEnumerator();

            // TODO: Parallelize

            while (true)
            {
                if (!Retry.Get(_policies.TransientServerErrorBackOff, enumerator.MoveNext))
                {
                    yield break;
                }

                // removing /container/ from the blob name (dev storage: /account/container/)
                yield return enumerator.Current.Name;
            }
        }

        public bool CreateContainerIfNotExist(string containerName)
        {
            //workaround since Azure is presently returning OutOfRange exception when using a wrong name.
            if (!BlobStorageExtensions.IsContainerNameValid(containerName))
                throw new NotSupportedException("containerName is not compliant with azure constraints on container naming");

            var container = _blobStorage.GetContainerReference(containerName);
            try
            {
                Retry.Do(_policies.TransientServerErrorBackOff, container.Create);
                return true;
            }
            catch(StorageClientException ex)
            {
                if(ex.ErrorCode == StorageErrorCode.ContainerAlreadyExists
                    || ex.ErrorCode == StorageErrorCode.ResourceAlreadyExists)
                {
                    return false;
                }

                throw;
            }
        }

        public bool DeleteContainerIfExist(string containerName)
        {
            var container = _blobStorage.GetContainerReference(containerName);
            try
            {
                Retry.Do(_policies.TransientServerErrorBackOff, container.Delete);
                return true;
            }
            catch(StorageClientException ex)
            {
                if(ex.ErrorCode == StorageErrorCode.ContainerNotFound
                    || ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }

                throw;
            }
        }

        public IEnumerable<string> ListBlobNames(string containerName, string blobNamePrefix = null)
        {
            // Enumerated blobs do not have a "name" property,
            // thus the name must be extracted from their URI
            // http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/c5e36676-8d07-46cc-b803-72621a0898b0/?prof=required

            if (blobNamePrefix == null)
            {
                blobNamePrefix = string.Empty;
            }

            var container = _blobStorage.GetContainerReference(containerName);

            var options = new BlobRequestOptions
            {
                UseFlatBlobListing = true
            };

            // if no prefix is provided, then enumerate the whole container
            IEnumerator<IListBlobItem> enumerator;
            if (string.IsNullOrEmpty(blobNamePrefix))
            {
                enumerator = container.ListBlobs(options).GetEnumerator();
            }
            else
            {
                // 'CloudBlobDirectory' must be used for prefixed enumeration
                var directory = container.GetDirectoryReference(blobNamePrefix);

                // HACK: [vermorel] very ugly override, but otherwise an "/" separator gets forcibly added
                typeof(CloudBlobDirectory).GetField("prefix", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(directory, blobNamePrefix);

                enumerator = directory.ListBlobs(options).GetEnumerator();
            }

            // TODO: Parallelize

            while (true)
            {
                try
                {
                    if (!Retry.Get(_policies.TransientServerErrorBackOff, enumerator.MoveNext))
                    {
                        yield break;
                    }
                }
                catch (StorageClientException ex)
                {
                    // if the container does not exist, empty enumeration
                    if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
                    {
                        yield break;
                    }
                    throw;
                }

                // removing /container/ from the blob name (dev storage: /account/container/)
                yield return Uri.UnescapeDataString(enumerator.Current.Uri.AbsolutePath.Substring(container.Uri.LocalPath.Length + 1));
            }
        }

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

        public bool DeleteBlobIfExist(string containerName, string blobName)
        {
            var timestamp = _countDeleteBlob.Open();

            var container = _blobStorage.GetContainerReference(containerName);

            try
            {
                var blob = container.GetBlockBlobReference(blobName);
                Retry.Do(_policies.TransientServerErrorBackOff, blob.Delete);
                _countDeleteBlob.Close(timestamp);
                return true;
            }
            catch (StorageClientException ex) // no such container, return false
            {
                if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
                    || ex.ErrorCode == StorageErrorCode.BlobNotFound
                        || ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                throw;
            }
        }

        public void DeleteAllBlobs(string containerName, string blobNamePrefix = null)
        {
            // TODO: Parallelize
            foreach (var blobName in ListBlobNames(containerName, blobNamePrefix))
            {
                DeleteBlobIfExist(containerName, blobName);
            }
        }

        public Maybe<T> GetBlob<T>(string containerName, string blobName)
        {
            string ignoredEtag;
            return GetBlob<T>(containerName, blobName, out ignoredEtag);
        }

        public Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag)
        {
            return GetBlob(containerName, blobName, typeof(T), out etag)
                .Convert(o => (T)o, Maybe<T>.Empty);
        }

        public Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag)
        {
            var timestamp = _countGetBlob.Open();

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            using (var stream = new MemoryStream())
            {
                etag = null;

                // if no such container, return empty
                try
                {
                    Retry.Do(_policies.NetworkCorruption, _policies.TransientServerErrorBackOff, () =>
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            blob.DownloadToStream(stream);
                            VerifyContentHash(blob, stream, containerName, blobName);
                        });

                    etag = blob.Properties.ETag;
                }
                catch (StorageClientException ex)
                {
                    if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
                        || ex.ErrorCode == StorageErrorCode.BlobNotFound
                            || ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                    {
                        return Maybe<object>.Empty;
                    }

                    throw;
                }

                stream.Seek(0, SeekOrigin.Begin);
                var deserialized = _serializer.TryDeserialize(stream, type);

                _countGetBlob.Close(timestamp);

                if (!deserialized.IsSuccess && _log != null)
                {
                    _log.WarnFormat(deserialized.Error,
                        "Cloud Storage: A blob was retrieved for GeBlob but failed to deserialize. Blob {0} in container {1}.",
                        blobName, containerName);
                }

                return deserialized.IsSuccess ? new Maybe<object>(deserialized.Value) : Maybe<object>.Empty;
            }
        }

        public Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag)
        {
            etag = null;

            var formatter = _serializer as IIntermediateDataSerializer;
            if (formatter == null)
            {
                return Maybe<XElement>.Empty;
            }

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            using (var stream = new MemoryStream())
            {
                // if no such container, return empty
                try
                {
                    Retry.Do(_policies.NetworkCorruption, _policies.TransientServerErrorBackOff, () =>
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            blob.DownloadToStream(stream);
                            VerifyContentHash(blob, stream, containerName, blobName);
                        });

                    etag = blob.Properties.ETag;
                }
                catch (StorageClientException ex)
                {
                    if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
                        || ex.ErrorCode == StorageErrorCode.BlobNotFound
                            || ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                    {
                        return Maybe<XElement>.Empty;
                    }

                    throw;
                }

                stream.Seek(0, SeekOrigin.Begin);
                var unpacked = formatter.TryUnpackXml(stream);
                return unpacked.IsSuccess ? unpacked.Value : Maybe<XElement>.Empty;
            }
        }

        /// <summary>As many parallel requests than there are blob names.</summary>
        public Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags)
        {
            var tempResult = blobNames.SelectInParallel(blobName =>
            {
                string etag;
                var blob = GetBlob<T>(containerName, blobName, out etag);
                return new Tuple<Maybe<T>, string>(blob, etag);
            }, blobNames.Length);

            etags = new string[blobNames.Length];
            var result = new Maybe<T>[blobNames.Length];

            for (int i = 0; i < tempResult.Length; i++)
            {
                result[i] = tempResult[i].Item1;
                etags[i] = tempResult[i].Item2;
            }

            return result;
        }

        public Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag)
        {
            // 'oldEtag' is null, then behavior always match simple 'GetBlob'.
            if (null == oldEtag)
            {
                return GetBlob<T>(containerName, blobName, out newEtag);
            }

            var timestamp = _countGetBlobIfModified.Open();

            newEtag = null;

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            try
            {
                var options = new BlobRequestOptions
                {
                    AccessCondition = AccessCondition.IfNoneMatch(oldEtag)
                };

                using (var stream = new MemoryStream())
                {
                    Retry.Do(_policies.NetworkCorruption, _policies.TransientServerErrorBackOff, () =>
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            blob.DownloadToStream(stream, options);
                            VerifyContentHash(blob, stream, containerName, blobName);
                        });

                    newEtag = blob.Properties.ETag;

                    stream.Seek(0, SeekOrigin.Begin);
                    var deserialized = _serializer.TryDeserializeAs<T>(stream);

                    _countGetBlobIfModified.Close(timestamp);

                    if (!deserialized.IsSuccess && _log != null)
                    {
                        _log.WarnFormat(deserialized.Error,
                            "Cloud Storage: A blob was retrieved for GetBlobIfModified but failed to deserialize. Blob {0} in container {1}.",
                            blobName, containerName);
                    }

                    return deserialized.IsSuccess ? deserialized.Value : Maybe<T>.Empty;
                }
            }
            catch (StorageClientException ex)
            {
                // call fails because blob has not been modified (usual case)
                if (ex.ErrorCode == StorageErrorCode.ConditionFailed ||
                    // HACK: BUG in StorageClient 1.0 
                    // see http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/4817cafa-12d8-4979-b6a7-7bda053e6b21
                    ex.Message == @"The condition specified using HTTP conditional header(s) is not met.")
                {
                    return Maybe<T>.Empty;
                }

                // call fails due to misc problems
                if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
                    || ex.ErrorCode == StorageErrorCode.BlobNotFound
                        || ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return Maybe<T>.Empty;
                }

                throw;
            }
        }

        public string GetBlobEtag(string containerName, string blobName)
        {
            var container = _blobStorage.GetContainerReference(containerName);

            try
            {
                var blob = container.GetBlockBlobReference(blobName);
                Retry.Do(_policies.TransientServerErrorBackOff, blob.FetchAttributes);
                return blob.Properties.ETag;
            }
            catch (StorageClientException ex)
            {
                if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
                    || ex.ErrorCode == StorageErrorCode.BlobNotFound
                        || ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return null;
                }
                throw;
            }
        }

        public void PutBlob<T>(string containerName, string blobName, T item)
        {
            PutBlob(containerName, blobName, item, true);
        }

        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite)
        {
            string ignored;
            return PutBlob(containerName, blobName, item, overwrite, out ignored);
        }

        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag)
        {
            return PutBlob(containerName, blobName, item, typeof(T), overwrite, out etag);
        }

        public bool PutBlob<T>(string containerName, string blobName, T item, string expectedEtag)
        {
            string outEtag;
            return PutBlob(containerName, blobName, item, typeof (T), true, expectedEtag, out outEtag);
        }

        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, out string outEtag)
        {
            var timestamp = _countPutBlob.Open();

            using (var stream = new MemoryStream())
            {
                _serializer.Serialize(item, stream);

                var container = _blobStorage.GetContainerReference(containerName);

                Func<Maybe<string>> doUpload = () =>
                {
                    var blob = container.GetBlockBlobReference(blobName);

                    // single remote call
                    var result = UploadBlobContent(blob, stream, overwrite, expectedEtag);

                    return result;
                };

                try
                {
                    var result = doUpload();
                    if (!result.HasValue)
                    {
                        outEtag = null;
                        return false;
                    }

                    outEtag = result.Value;
                    _countPutBlob.Close(timestamp);
                    return true;
                }
                catch (StorageClientException ex)
                {
                    // if the container does not exist, it gets created
                    if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
                    {
                        // caution: the container might have been freshly deleted
                        // (multiple retries are needed in such a situation)
                        var tentativeEtag = Maybe<string>.Empty;
                        Retry.Do(_policies.SlowInstantiation, () =>
                            {
                                Retry.Get(_policies.TransientServerErrorBackOff, container.CreateIfNotExist);

                                tentativeEtag = doUpload();
                            });

                        if (!tentativeEtag.HasValue)
                        {
                            outEtag = null;
                            return false;
                        }

                        outEtag = tentativeEtag.Value;
                        _countPutBlob.Close(timestamp);
                        return true;
                    }

                    if (ex.ErrorCode == StorageErrorCode.BlobAlreadyExists && !overwrite)
                    {
                        // See http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/fff78a35-3242-4186-8aee-90d27fbfbfd4
                        // and http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/86b9f184-c329-4c30-928f-2991f31e904b/

                        outEtag = null;
                        return false;
                    }

                    var result = doUpload();
                    if (!result.HasValue)
                    {
                        outEtag = null;
                        return false;
                    }

                    outEtag = result.Value;
                    _countPutBlob.Close(timestamp);
                    return true;
                }
            }
        }

        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string outEtag)
        {
            return PutBlob(containerName, blobName, item, type, overwrite, null, out outEtag);
        }

        /// <param name="overwrite">If <c>false</c>, then no write happens if the blob already exists.</param>
        /// <param name="expectedEtag">When specified, no writing occurs unless the blob etag
        /// matches the one specified as argument.</param>
        /// <returns>The ETag of the written blob, if it was written.</returns>
        Maybe<string> UploadBlobContent(CloudBlob blob, Stream stream, bool overwrite, string expectedEtag)
        {
            BlobRequestOptions options;

            if (!overwrite) // no overwrite authorized, blob must NOT exists
            {
                options = new BlobRequestOptions { AccessCondition = AccessCondition.IfNotModifiedSince(DateTime.MinValue) };
            }
            else // overwrite is OK
            {
                options = string.IsNullOrEmpty(expectedEtag) ?
                                                                // case with no etag constraint
                    new BlobRequestOptions { AccessCondition = AccessCondition.None } :
                                                                                        // case with etag constraint
                    new BlobRequestOptions { AccessCondition = AccessCondition.IfMatch(expectedEtag) };
            }

            ApplyContentHash(blob, stream);

            try
            {
                Retry.Do(_policies.NetworkCorruption, _policies.TransientServerErrorBackOff, () =>
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        blob.UploadFromStream(stream, options);
                    });
            }
            catch (StorageClientException ex)
            {
                if (ex.ErrorCode == StorageErrorCode.ConditionFailed)
                {
                    return Maybe<string>.Empty;
                }

                throw;
            }

            return blob.Properties.ETag;
        }

        public Maybe<T> UpdateBlobIfExist<T>(
            string containerName, string blobName, Func<T, T> update)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, t => update(t));
        }

        public Maybe<T> UpdateBlobIfExistOrSkip<T>(
            string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update);
        }

        public Maybe<T> UpdateBlobIfExistOrDelete<T>(
            string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            var result = UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update);
            if (!result.HasValue)
            {
                DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        public T UpsertBlob<T>(string containerName, string blobName, Func<T> insert, Func<T, T> update)
        {
            return UpsertBlobOrSkip<T>(containerName, blobName, () => insert(), t => update(t)).Value;
        }

        public Maybe<T> UpsertBlobOrSkip<T>(
            string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            var timestamp = _countUpsertBlobOrSkip.Open();

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            Maybe<T> output;

            var optimisticPolicy = _policies.OptimisticConcurrency();
            TimeSpan retryInterval = TimeSpan.Zero;
            int retryCount = 0;
            do
            {
                // 0. IN CASE OF RETRIAL, WAIT UNTIL NEXT TRIAL (retry policy)

                if (retryInterval > TimeSpan.Zero)
                {
                    Thread.Sleep(retryInterval);
                }

                // 1. DOWNLOAD EXISTING INPUT BLOB, IF IT EXISTS

                Maybe<T> input;
                bool inputBlobExists = false;
                string inputETag = null;

                try
                {
                    using (var readStream = new MemoryStream())
                    {
                        Retry.Do(_policies.NetworkCorruption, _policies.TransientServerErrorBackOff, () =>
                            {
                                readStream.Seek(0, SeekOrigin.Begin);
                                blob.DownloadToStream(readStream);
                                VerifyContentHash(blob, readStream, containerName, blobName);
                            });

                        inputETag = blob.Properties.ETag;
                        inputBlobExists = !String.IsNullOrEmpty(inputETag);

                        readStream.Seek(0, SeekOrigin.Begin);

                        var deserialized = _serializer.TryDeserializeAs<T>(readStream);
                        if (!deserialized.IsSuccess && _log != null)
                        {
                            _log.WarnFormat(deserialized.Error,
                                "Cloud Storage: A blob was retrieved for Upsert but failed to deserialize. An Insert will be performed instead, overwriting the corrupt blob {0} in container {1}.",
                                blobName, containerName);
                        }

                        input = deserialized.IsSuccess ? deserialized.Value : Maybe<T>.Empty;
                    }
                }
                catch (StorageClientException ex)
                {
                    // creating the container when missing
                    if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
                        || ex.ErrorCode == StorageErrorCode.BlobNotFound
                            || ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                    {
                        input = Maybe<T>.Empty;

                        // caution: the container might have been freshly deleted
                        // (multiple retries are needed in such a situation)
                        Retry.Do(_policies.SlowInstantiation, () => Retry.Get(_policies.TransientServerErrorBackOff, container.CreateIfNotExist));
                    }
                    else
                    {
                        throw;
                    }
                }

                // 2. APPLY UPADTE OR INSERT (DEPENDING ON INPUT)

                output = input.HasValue ? update(input.Value) : insert();

                // 3. IF EMPTY OUTPUT THEN WE CAN SKIP THE WHOLE OPERATION

                if (!output.HasValue)
                {
                    _countUpsertBlobOrSkip.Close(timestamp);
                    return output;
                }

                // 4. TRY TO INSERT OR UPDATE BLOB

                using (var writeStream = new MemoryStream())
                {
                    _serializer.Serialize(output.Value, writeStream);
                    writeStream.Seek(0, SeekOrigin.Begin);

                    // Semantics:
                    // Insert: Blob must not exist -> do not overwrite
                    // Update: Blob must exists -> overwrite and verify matching ETag

                    bool succeeded = inputBlobExists
                        ? UploadBlobContent(blob, writeStream, true, inputETag).HasValue
                        : UploadBlobContent(blob, writeStream, false, null).HasValue;

                    if (succeeded)
                    {
                        _countUpsertBlobOrSkip.Close(timestamp);
                        return output;
                    }
                }
            } while (optimisticPolicy(retryCount++, null, out retryInterval));

            throw new TimeoutException("Failed to resolve optimistic concurrency errors within a limited number of retrials");
        }

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

        private static string ComputeContentHash(Stream source)
        {
            byte[] hash;
            source.Seek(0, SeekOrigin.Begin);
            using (var md5 = MD5.Create())
            {
                hash = md5.ComputeHash(source);
            }

            source.Seek(0, SeekOrigin.Begin);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Apply a content hash to the blob to verify upload and roundtrip consistency.
        /// </summary>
        private static void ApplyContentHash(CloudBlob blob, Stream stream)
        {
            var hash = ComputeContentHash(stream);

            // HACK: [Vermorel 2010-11] StorageClient does not apply MD5 on smaller blobs.
            // Reflector indicates that the behavior threshold is at 32MB
            // so manually disable hasing for larger blobs
            if (stream.Length < 0x2000000L)
            {
                blob.Properties.ContentMD5 = hash;
            }

            // HACK: [vermorel 2010-11] StorageClient does not provide a way to retrieve
            // MD5 so we add our own MD5 check which let perform our own validation when
            // downloading the blob (full roundtrip validation). 
            blob.Metadata[MetadataMD5Key] = hash;
        }

        /// <summary>
        /// Throws a DataCorruptionException if the content hash is available but doesn't match.
        /// </summary>
        private static void VerifyContentHash(CloudBlob blob, Stream stream, string containerName, string blobName)
        {
            var expectedHash = blob.Metadata[MetadataMD5Key];
            if (string.IsNullOrEmpty(expectedHash))
            {
                return;
            }

            if (expectedHash != ComputeContentHash(stream))
            {
                throw new DataCorruptionException(
                    string.Format("MD5 mismatch on blob retrieval {0}/{1}.", containerName, blobName));
            }
        }

        public Result<string> TryAcquireLease(string containerName, string blobName)
        {
            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);
            var credentials = _blobStorage.Credentials;
            var uri = new Uri(credentials.TransformUri(blob.Uri.ToString()));
            var request = BlobRequest.Lease(uri, 90, LeaseAction.Acquire, null);
            credentials.SignRequest(request);

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException we)
            {
                var statusCode = ((HttpWebResponse) we.Response).StatusCode;
                switch (statusCode)
                {
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.InternalServerError:
                        return Result<string>.CreateError(statusCode.ToString());
                    default:
                        throw;
                }
            }

            try
            {
                return response.StatusCode == HttpStatusCode.Created
                    ? Result<string>.CreateSuccess(response.Headers["x-ms-lease-id"])
                    : Result<string>.CreateError(response.StatusCode.ToString());
            }
            finally
            {
                response.Close();
            }
        }

        public bool TryReleaseLease(string containerName, string blobName, string leaseId)
        {
            return TryLeaseAction(containerName, blobName, LeaseAction.Release, leaseId);
        }

        public bool TryRenewLease(string containerName, string blobName, string leaseId)
        {
            return TryLeaseAction(containerName, blobName, LeaseAction.Renew, leaseId);
        }

        private bool TryLeaseAction(string containerName, string blobName, LeaseAction action, string leaseId = null)
        {
            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);
            var credentials = _blobStorage.Credentials;
            var uri = new Uri(credentials.TransformUri(blob.Uri.ToString()));
            var request = BlobRequest.Lease(uri, 90, action, leaseId);
            credentials.SignRequest(request);

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException we)
            {
                var statusCode = ((HttpWebResponse) we.Response).StatusCode;
                switch(statusCode)
                {
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.InternalServerError:
                        return false;
                    default:
                        throw;
                }
            }

            try
            {
                var expectedCode = action == LeaseAction.Break ? HttpStatusCode.Accepted : HttpStatusCode.OK;
                return response.StatusCode == expectedCode;
            }
            finally
            {
                response.Close();
            }
        }
    }
}