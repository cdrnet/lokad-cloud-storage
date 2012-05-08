#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.FileSystem
{
    /// <summary>
    /// Azure retry policies for corner-situation and server errors.
    /// </summary>
    internal class RetryPolicies
    {
        internal RetryPolicies()
        {
        }

        /// <summary>
        /// Retry policy for optimistic concurrency retrials.
        /// </summary>
        public ShouldRetry OptimisticConcurrency()
        {
            var random = new Random();

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
                {
                    if (currentRetryCount >= 30 || !(lastException is IOException))
                    {
                        retryInterval = TimeSpan.Zero;
                        return false;
                    }

                    retryInterval = TimeSpan.FromMilliseconds(random.Next(Math.Min(1000, 5 + currentRetryCount * currentRetryCount * 5)));
                    return true;
                };
        }
    }
}