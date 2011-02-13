#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cloud.Storage
{
    /// <summary>Helpers for the <see cref="IBlobStorageProvider"/>.</summary>
    public static class BlobStorageExtensions
    {
        /// <summary>
        /// List the blob names of all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<T> ListBlobNames<T>(this IBlobStorageProvider provider, T blobNamePrefix) where T : UntypedBlobName
        {
            return provider.ListBlobNames(blobNamePrefix.ContainerName, blobNamePrefix.ToString())
                .Select(UntypedBlobName.Parse<T>);
        }

        /// <summary>
        /// List and get all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<T> ListBlobs<T>(this IBlobStorageProvider provider, BlobName<T> blobNamePrefix, int skip = 0)
        {
            return provider.ListBlobs<T>(blobNamePrefix.ContainerName, blobNamePrefix.ToString(), skip);
        }

        /// <summary>
        /// Deletes a blob if it exists.
        /// </summary>
        /// <remarks>
        /// <para>This method is idempotent.</para>
        /// </remarks>
        public static bool DeleteBlobIfExist<T>(this IBlobStorageProvider provider, BlobName<T> fullName)
        {
            return provider.DeleteBlobIfExist(fullName.ContainerName, fullName.ToString());
        }

        /// <summary>
        /// Delete all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is idempotent.</para>
        /// </remarks>
        public static void DeleteAllBlobs(this IBlobStorageProvider provider, UntypedBlobName blobNamePrefix)
        {
            provider.DeleteAllBlobs(blobNamePrefix.ContainerName, blobNamePrefix.ToString());
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
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), () => Maybe<T>.Empty, t => update(t));
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
        public static Maybe<T> UpdateBlobIfExistOrSkip<T>(
            this IBlobStorageProvider provider, BlobName<T> name, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), () => Maybe<T>.Empty, update);
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
        public static Maybe<T> UpdateBlobIfExistOrDelete<T>(
            this IBlobStorageProvider provider, BlobName<T> name, Func<T, Maybe<T>> update)
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
        public static Maybe<T> UpsertBlobOrDelete<T>(
            this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrDelete(name.ContainerName, name.ToString(), insert, update);
        }

        /// <summary>Checks that containerName is a valid DNS name, as requested by Azure</summary>
        public static bool IsContainerNameValid(string containerName)
        {
            return (Regex.IsMatch(containerName, @"(^([a-z]|\d))((-([a-z]|\d)|([a-z]|\d))+)$")
                && (3 <= containerName.Length) && (containerName.Length <= 63));
        }
    }
}
