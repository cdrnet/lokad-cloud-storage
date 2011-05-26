#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Lokad.Cloud.Storage.Events;
using Lokad.Cloud.Storage.Events.Observers;
using Lokad.Cloud.Storage.Shared.Policies;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>
    /// Azure retry policies for corner-situation and server errors.
    /// </summary>
    internal class AzurePolicies
    {
        private readonly ICloudStorageSystemObserver _systemObserver;

        /// <summary>
        /// Retry policy to temporarily back off in case of transient Azure server
        /// errors, system overload or in case the denial of service detection system
        /// thinks we're a too heavy user. Blocks the thread while backing off to
        /// prevent further requests for a while (per thread).
        /// </summary>
        public ActionPolicy TransientServerErrorBackOff { get; private set; }

        /// <summary>Similar to <see cref="TransientServerErrorBackOff"/>, yet
        /// the Table Storage comes with its own set or exceptions/.</summary>
        public ActionPolicy TransientTableErrorBackOff { get; private set; }

        /// <summary>
        /// Very patient retry policy to deal with container, queue or table instantiation
        /// that happens just after a deletion.
        /// </summary>
        public ActionPolicy SlowInstantiation { get; private set; }

        /// <summary>
        /// Limited retry related to MD5 validation failure.
        /// </summary>
        public ActionPolicy NetworkCorruption { get; private set; }

        /// <summary>
        /// Static Constructor
        /// </summary>
        /// <param name="systemObserver">Can be <see langword="null"/>.</param>
        internal AzurePolicies(ICloudStorageSystemObserver systemObserver)
        {
            _systemObserver = systemObserver;

            // Initialize Policies
            TransientServerErrorBackOff = ActionPolicy.With(TransientServerErrorExceptionFilter)
                .Retry(30, OnTransientServerErrorRetry);

            TransientTableErrorBackOff = ActionPolicy.With(TransientTableErrorExceptionFilter)
                .Retry(30, OnTransientTableErrorRetry);

            SlowInstantiation = ActionPolicy.With(SlowInstantiationExceptionFilter)
                .Retry(30, OnSlowInstantiationRetry);

            NetworkCorruption = ActionPolicy.With(NetworkCorruptionExceptionFilter)
                .Retry(2, OnNetworkCorruption);
        }

        /// <summary>
        /// Retry policy for optimistic concurrency retrials.
        /// </summary>
        /// <remarks>Fitting the <see cref="ShouldRetry"/> delegate.</remarks>
        public bool ShouldRetryOptimisticConcurrency(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            if (currentRetryCount >= 30)
            {
                retryInterval = TimeSpan.Zero;
                return false;
            }

            if (_systemObserver != null)
            {
                _systemObserver.Notify(new OperationRetriedEvent(lastException, "OptimisticConcurrency", currentRetryCount));
            }

            var random = new Random();
            retryInterval = TimeSpan.FromMilliseconds(random.Next(Math.Min(10000, 10 + currentRetryCount * currentRetryCount * 10)));
            return true;
        }

        /// <summary>
        /// Retry policy which is applied to all Azure storage clients.
        /// </summary>
        /// <remarks>Fitting the <see cref="ShouldRetry"/> delegate.</remarks>
        public bool ShouldRetryInAzureStorageClient(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            // [abdullin]: in short this gives us MinBackOff + 2^(10)*Rand.(~0.5.Seconds()) at the last retry.

            // TODO (ruegg, 2011-05-26): This policy might actually be counterproductive and interfere with the other policies. Investigate.

            if (currentRetryCount >= 10)
            {
                retryInterval = TimeSpan.Zero;
                return false;
            }

            if (_systemObserver != null)
            {
                _systemObserver.Notify(new OperationRetriedEvent(lastException, "StorageClient", currentRetryCount));
            }

            var random = new Random();

            double deltaBackoff = TimeSpan.FromSeconds(0.5).TotalMilliseconds;
            double minBackoff = RetryPolicies.DefaultMinBackoff.TotalMilliseconds;
            double maxBackoff = RetryPolicies.DefaultMaxBackoff.TotalMilliseconds;

            retryInterval = TimeSpan.FromMilliseconds(Math.Min(
                maxBackoff,
                minBackoff + ((Math.Pow(2.0, currentRetryCount) - 1.0) * random.Next((int)(deltaBackoff * 0.8), (int)(deltaBackoff * 1.2)))));
            return true;
        }

        void OnTransientServerErrorRetry(Exception exception, int count)
        {
            if (_systemObserver != null)
            {
                _systemObserver.Notify(new OperationRetriedEvent(exception, "TransientServerError", count));
            }

            // quadratic backoff, capped at 5 minutes
            var c = count + 1;
            Thread.Sleep(TimeSpan.FromSeconds(Math.Min(300, c*c)));
        }

        void OnTransientTableErrorRetry(Exception exception, int count)
        {
            if (_systemObserver != null)
            {
                _systemObserver.Notify(new OperationRetriedEvent(exception, "TransientTableError", count));
            }

            // quadratic backoff, capped at 5 minutes
            var c = count + 1;
            Thread.Sleep(TimeSpan.FromSeconds(Math.Min(300, c * c)));
        }

        void OnSlowInstantiationRetry(Exception exception, int count)
        {
            if (_systemObserver != null)
            {
                _systemObserver.Notify(new OperationRetriedEvent(exception, "SlowInstantiation", count));
            }

            // linear backoff
            Thread.Sleep(TimeSpan.FromMilliseconds(100 * count));
        }

        void OnNetworkCorruption(Exception exception, int count)
        {
            if (_systemObserver != null)
            {
                _systemObserver.Notify(new OperationRetriedEvent(exception, "NetworkCorruption", count));
            }

            // no backoff, retry immediately
        }

        static bool IsErrorCodeMatch(StorageException exception, params StorageErrorCode[] codes)
        {
            return exception != null
                && codes.Contains(exception.ErrorCode);
        }

        static bool IsErrorStringMatch(StorageException exception, params string[] errorStrings)
        {
            return exception != null && exception.ExtendedErrorInformation != null
                && errorStrings.Contains(exception.ExtendedErrorInformation.ErrorCode);
        }

        static bool IsErrorStringMatch(string exceptionErrorString, params string[] errorStrings)
        {
            return errorStrings.Contains(exceptionErrorString);
        }

        static bool TransientServerErrorExceptionFilter(Exception exception)
        {
            var serverException = exception as StorageServerException;
            if (serverException != null)
            {
                if (IsErrorCodeMatch(serverException,
                    StorageErrorCode.ServiceInternalError,
                    StorageErrorCode.ServiceTimeout))
                {
                    return true;
                }

                if (IsErrorStringMatch(serverException,
                    StorageErrorCodeStrings.InternalError,
                    StorageErrorCodeStrings.ServerBusy,
                    StorageErrorCodeStrings.OperationTimedOut))
                {
                    return true;
                }

                return false;
            }

            // HACK: StorageClient does not catch internal errors very well.
            // Hence we end up here manually catching exception that should have been correctly 
            // typed by the StorageClient:

            // System.Net.InternalException is internal, but uncaught on some race conditions.
            // We therefore assume this is a transient error and retry.
            var exceptionType = exception.GetType();
            if (exceptionType.FullName == "System.Net.InternalException")
            {
                return true;
            }

            return false;
        }

        static bool TransientTableErrorExceptionFilter(Exception exception)
        {
            var dataServiceRequestException = exception as DataServiceRequestException;
            if (dataServiceRequestException != null)
            {
                if (IsErrorStringMatch(GetErrorCode(dataServiceRequestException),
                    StorageErrorCodeStrings.InternalError,
                    StorageErrorCodeStrings.ServerBusy,
                    StorageErrorCodeStrings.OperationTimedOut,
                    TableErrorCodeStrings.TableServerOutOfMemory))
                {
                    return true;
                }
            }

            var dataServiceQueryException = exception as DataServiceQueryException;
            if (dataServiceQueryException != null)
            {
                if (IsErrorStringMatch(GetErrorCode(dataServiceQueryException),
                    StorageErrorCodeStrings.InternalError,
                    StorageErrorCodeStrings.ServerBusy,
                    StorageErrorCodeStrings.OperationTimedOut,
                    TableErrorCodeStrings.TableServerOutOfMemory))
                {
                    return true;
                }
            }

            // HACK: StorageClient does not catch internal errors very well.
            // Hence we end up here manually catching exception that should have been correctly 
            // typed by the StorageClient:

            // The remote server returned an error: (500) Internal Server Error.
            var webException = exception as WebException;
            if (null != webException &&
                (webException.Status == WebExceptionStatus.ProtocolError ||
                 webException.Status == WebExceptionStatus.ConnectionClosed))
            {
                return true;
            }

            // System.Net.InternalException is internal, but uncaught on some race conditions.
            // We therefore assume this is a transient error and retry.
            var exceptionType = exception.GetType();
            if (exceptionType.FullName == "System.Net.InternalException")
            {
                return true;
            }

            return false;
        }

        static bool SlowInstantiationExceptionFilter(Exception exception)
        {
            var storageException = exception as StorageClientException;

            // Blob Storage or Queue Storage exceptions
            // Table Storage may throw exception of type 'StorageClientException'
            if (storageException != null)
            {
                // 'client' exceptions reflect server-side problems (delayed instantiation)

                if (IsErrorCodeMatch(storageException,
                    StorageErrorCode.ResourceNotFound,
                    StorageErrorCode.ContainerNotFound))
                {
                    return true;
                }

                if (IsErrorStringMatch(storageException,
                    QueueErrorCodeStrings.QueueNotFound,
                    QueueErrorCodeStrings.QueueBeingDeleted,
                    StorageErrorCodeStrings.InternalError,
                    StorageErrorCodeStrings.ServerBusy,
                    TableErrorCodeStrings.TableServerOutOfMemory,
                    TableErrorCodeStrings.TableNotFound,
                    TableErrorCodeStrings.TableBeingDeleted))
                {
                    return true;
                }
            }

            // Table Storage may also throw exception of type 'DataServiceQueryException'.
            var dataServiceException = exception as DataServiceQueryException;
            if (null != dataServiceException)
            {
                if (IsErrorStringMatch(GetErrorCode(dataServiceException),
                    TableErrorCodeStrings.TableBeingDeleted,
                    TableErrorCodeStrings.TableNotFound,
                    TableErrorCodeStrings.TableServerOutOfMemory))
                {
                    return true;
                }
            }

            return false;
        }

        static bool NetworkCorruptionExceptionFilter(Exception exception)
        {
            // Upload MD5 mismatch
            var clientException = exception as StorageClientException;
            if (clientException != null
                && clientException.ErrorCode == StorageErrorCode.BadRequest
                && clientException.ExtendedErrorInformation != null
                && clientException.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.InvalidHeaderValue
                && clientException.ExtendedErrorInformation.AdditionalDetails["HeaderName"] == "Content-MD5")
            {
                // network transport corruption (automatic), try again
                return true;
            }

            // Download MD5 mismatch
            if (exception is DataCorruptionException)
            {
                // network transport corruption (manual), try again
                return true;
            }

            return false;
        }

        public static string GetErrorCode(DataServiceRequestException ex)
        {
            var r = new Regex(@"<code>(\w+)</code>", RegexOptions.IgnoreCase);
            var match = r.Match(ex.InnerException.Message);
            return match.Groups[1].Value;
        }

        // HACK: just dupplicating the other overload of 'GetErrorCode'
        public static string GetErrorCode(DataServiceQueryException ex)
        {
            var r = new Regex(@"<code>(\w+)</code>", RegexOptions.IgnoreCase);
            var match = r.Match(ex.InnerException.Message);
            return match.Groups[1].Value;
        }
    }
}