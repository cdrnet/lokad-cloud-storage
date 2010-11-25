#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cloud.Storage
{
    public static class BlobStorageExtensions
    {
        /// <summary>
        /// Updates a blob if it already exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist.</returns>
        public static Maybe<T> UpdateBlobIfExist<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, t => update(t)).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist.</returns>
        public static Maybe<T> UpdateBlobIfExist<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), () => Maybe<T>.Empty, t => update(t)).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or no change was applied.</returns>
        public static Maybe<T> UpdateBlobIfExistOrSkip<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or no change was applied.</returns>
        public static Maybe<T> UpdateBlobIfExistOrSkip<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), () => Maybe<T>.Empty, update).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will be deleted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or was deleted.</returns>
        public static Maybe<T> UpdateBlobIfExistOrDelete<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            var result = provider.UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update);
            if (!result.HasValue)
            {
                provider.DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will be deleted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or was deleted.</returns>
        public static Maybe<T> UpdateBlobIfExistOrDelete<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T, Maybe<T>> update)
        {
            return provider.UpdateBlobIfExistOrDelete(name.ContainerName, name.ToString(), update);
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda.</returns>
        public static T UpsertBlob<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T> insert, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip<T>(containerName, blobName, () => insert(), t => update(t)).Value;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda.</returns>
        public static T UpsertBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T> insert, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip<T>(name.ContainerName, name.ToString(), () => insert(), t => update(t)).Value;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then no change was applied.</returns>
        public static Maybe<T> UpsertBlobOrSkip<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), insert, update);
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will be deleted (if it exists).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then the blob has been deleted.</returns>
        public static Maybe<T> UpsertBlobOrDelete<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            var result = provider.UpsertBlobOrSkip(containerName, blobName, insert, update);
            if (!result.HasValue)
            {
                provider.DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will be deleted (if it exists).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then the blob has been deleted.</returns>
        public static Maybe<T> UpsertBlobOrDelete<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrDelete(name.ContainerName, name.ToString(), insert, update);
        }

        public static bool DeleteBlobIfExists<T>(this IBlobStorageProvider provider, BlobName<T> fullName)
        {
            return provider.DeleteBlobIfExist(fullName.ContainerName, fullName.ToString());
        }

        static readonly Random _rand = new Random();

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead. This method will be removed in future versions.")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
        {
            Result<T> tmpResult = null;
            RetryUpdate(() => provider.UpdateIfNotModified(containerName, blobName, updater, out tmpResult));

            result = tmpResult;
        }

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead. This method will be removed in future versions.")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>, T> updater, out T result)
        {
            T tmpResult = default(T);
            RetryUpdate(() => provider.UpdateIfNotModified(containerName, blobName, updater, out tmpResult));

            result = tmpResult;
        }

        /// <summary>Retry an update method until it succeeds. Timing
        /// increases to avoid overstressing the storage for nothing. 
        /// Maximal delay is set to 10 seconds.</summary>
        [Obsolete("This method will be removed in future versions.")]
        static void RetryUpdate(Func<bool> func)
        {
            // HACK: hard-coded constant, the whole counter system have to be perfected.
            const int step = 10;
            const int maxDelayInMilliseconds = 10000;

            int retryAttempts = 0;
            while (!func())
            {
                retryAttempts++;
                var sleepTime = _rand.Next(Math.Max(retryAttempts * retryAttempts * step, maxDelayInMilliseconds)).Milliseconds();
                Thread.Sleep(sleepTime);

            }
        }

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead. This method will be removed in future versions.")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
        {
            AtomicUpdate(provider, name.ContainerName, name.ToString(), updater, out result);
        }

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead. This method will be removed in future versions.")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>, T> updater, out T result)
        {
            AtomicUpdate(provider, name.ContainerName, name.ToString(), updater, out result);
        }

        [Obsolete("Use DeleteBlobIfExist instead. This method will be removed in future versions. This method will be removed in future versions.")]
        public static bool DeleteBlob<T>(this IBlobStorageProvider provider, BlobName<T> fullName)
        {
            return provider.DeleteBlobIfExist(fullName.ContainerName, fullName.ToString());
        }

        public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, BlobName<T> name)
        {
            return provider.GetBlob<T>(name.ContainerName, name.ToString());
        }

        public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, out string etag)
        {
            return provider.GetBlob<T>(name.ContainerName, name.ToString(), out etag);
        }

        public static string GetBlobEtag<T>(this IBlobStorageProvider provider, BlobName<T> name)
        {
            return provider.GetBlobEtag(name.ContainerName, name.ToString());
        }

        /// <summary>Gets the corresponding object. If the deserialization fails
        /// just delete the existing copy.</summary>
        public static Maybe<T> GetBlobOrDelete<T>(this IBlobStorageProvider provider, string containerName, string blobName)
        {
            try
            {
                return provider.GetBlob<T>(containerName, blobName);
            }
            catch (SerializationException)
            {
                provider.DeleteBlobIfExist(containerName, blobName);
                return Maybe<T>.Empty;
            }
            catch (InvalidCastException)
            {
                provider.DeleteBlobIfExist(containerName, blobName);
                return Maybe<T>.Empty;
            }
        }

        /// <summary>Gets the corresponding object. If the deserialization fails
        /// just delete the existing copy.</summary>
        public static Maybe<T> GetBlobOrDelete<T>(this IBlobStorageProvider provider, BlobName<T> name)
        {
            return provider.GetBlobOrDelete<T>(name.ContainerName, name.ToString());
        }

        public static void PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item)
        {
            provider.PutBlob(name.ContainerName, name.ToString(), item);
        }

        public static bool PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item, bool overwrite)
        {
            return provider.PutBlob(name.ContainerName, name.ToString(), item, overwrite);
        }

        /// <summary>Push the blob only if etag is matching the etag of the blob in BlobStorage</summary>
        public static bool PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item, string etag)
        {
            return provider.PutBlob(name.ContainerName, name.ToString(), item, etag);
        }

        public static IEnumerable<T> List<T>(
            this IBlobStorageProvider provider, T prefix) where T : UntypedBlobName
        {
            return provider.List(prefix.ContainerName, prefix.ToString())
                .Select(UntypedBlobName.Parse<T>);
        }

        [Obsolete("The naming of this method is misleading and a likely cause for bugs. Use one of the alternatives instead. This method will be removed in future versions.", false)]
        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater, out result);
        }

        [Obsolete("The naming of this method is misleading and a likely cause for bugs. Use one of the alternatives instead. This method will be removed in future versions.", false)]
        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, T> updater, out T result)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater, out result);
        }

        [Obsolete("The naming of this method is misleading and a likely cause for bugs. Use one of the alternatives instead. This method will be removed in future versions.", false)]
        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, Result<T>> updater)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater);
        }

        [Obsolete("The naming of this method is misleading and a likely cause for bugs. Use one of the alternatives instead. This method will be removed in future versions.", false)]
        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, T> updater)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater);
        }
    }
}
