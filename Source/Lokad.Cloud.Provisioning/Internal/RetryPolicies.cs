#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;

namespace Lokad.Cloud.Provisioning.Internal
{
    /// <summary>
    /// Azure management api retry policies for corner-situation and server errors.
    /// </summary>
    internal class RetryPolicies
    {
        // NOTE (ruegg, 2011-05-24): Clone from Microsoft.WindowsAzure.StorageClient.ShouldRetry.
        // Justification: Avoid reference to the storage client library just for this delegate type
        internal delegate bool ShouldRetry(int retryCount, Exception lastException, out TimeSpan delay);
        internal delegate ShouldRetry RetryPolicy();

        private readonly ICloudProvisioningObserver _observer;

        /// <param name="observer">Can be <see langword="null"/>.</param>
        internal RetryPolicies(ICloudProvisioningObserver observer)
        {
            _observer = observer;
        }

        public ShouldRetry RetryOnTransientErrors()
        {
            Guid sequence = Guid.NewGuid();
            var random = new Random();

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (currentRetryCount > 30 || !ProvisioningErrorHandling.IsTransientError(lastException))
                {
                    retryInterval = TimeSpan.Zero;
                    return false;
                }

                retryInterval = TimeSpan.FromMilliseconds(random.Next(Math.Min(10000, 10 + currentRetryCount * currentRetryCount * 10)));

                if (_observer != null)
                {
                    _observer.Notify(new ProvisioningOperationRetriedEvent(lastException, "TransientServerError", currentRetryCount, retryInterval, sequence));
                }

                return true;
            };
        }
    }
}
