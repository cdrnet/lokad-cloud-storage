#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable CheckNamespace
// ReSharper disable CSharpWarnings::CS1591

namespace Lokad.Cloud.Storage
{
    /// <summary>Async Helpers for the <see cref="IBlobStorageProvider"/>.</summary>
    public static class BlobStorageAsyncExtensions
    {
        // GetBlobAsync

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync(containerName, blobName, typeof(T), CancellationToken.None, serializer)
                .Then(b => b == null ? null : new BlobWithETag<T> { Blob = (T)b.Blob, ETag = b.ETag });
        }

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync<T>(location.ContainerName, location.Path, serializer);
        }

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync<T>(location.ContainerName, location.Path, serializer);
        }

        // GetBlobEtagAsync

        public static Task<string> GetBlobEtagAsync(this IBlobStorageProvider provider, string containerName, string blobName)
        {
            return provider.GetBlobEtagAsync(containerName, blobName, CancellationToken.None);
        }

        // PutBlobAsync

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), true, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), overwrite, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, string expectedEtag, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), true, expectedEtag, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync(this IBlobStorageProvider provider, string containerName, string blobName, object item, Type type, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, type, overwrite, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, overwrite, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, overwrite, serializer);
        }

        /// <summary>Push the blob only if etag is matching the etag of the blob in BlobStorage</summary>
        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, string etag, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, etag, serializer);
        }

        // Upsert Variants

        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, insert, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(containerName, blobName, () => insert(), t => update(t), CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, insert, update, CancellationToken.None, serializer)
                .Then(b =>
                    {
                        if (b == null)
                        {
                            provider.DeleteBlobIfExist(containerName, blobName);
                        }

                        return b;
                    });
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, t => update(t), CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, update, CancellationToken.None, serializer)
                .Then(b =>
                    {
                        if (b == null)
                        {
                            provider.DeleteBlobIfExist(containerName, blobName);
                        }

                        return b;
                    });
        }

        /// <summary>
        /// ASYNC: Updates a blob if it already exists.
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
        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), CancellationToken.None, serializer);
        }

        /// <summary>
        /// ASYNC: Updates a blob if it already exists.
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
        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), CancellationToken.None, serializer);
        }


        /// <summary>
        /// ASYNC: Updates a blob if it already exists.
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
        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(
            this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, update, CancellationToken.None, serializer);
        }

        /// <summary>
        /// ASYNC: Updates a blob if it already exists.
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
        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(
            this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpdateBlobIfExistOrDeleteAsync(location.ContainerName, location.Path, update, serializer);
        }

        /// <summary>
        /// ASYNC: Inserts or updates a blob depending on whether it already exists or not.
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
        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(location.ContainerName, location.Path, () => insert(), t => update(t), CancellationToken.None, serializer);
        }

        /// <summary>
        /// ASYNC: Inserts or updates a blob depending on whether it already exists or not.
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
        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(location.ContainerName, location.Path, () => insert(), t => update(t), CancellationToken.None, serializer);
        }

        /// <summary>
        /// ASYNC: Inserts or updates a blob depending on whether it already exists or not.
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
        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider,
            IBlobLocationAndType<T> location, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, insert, update, CancellationToken.None, serializer);
        }

        /// <summary>
        /// ASYNC: Inserts or updates a blob depending on whether it already exists or not.
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
        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(
            this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrDeleteAsync(location.ContainerName, location.Path, insert, update, serializer);
        }
    }
}
