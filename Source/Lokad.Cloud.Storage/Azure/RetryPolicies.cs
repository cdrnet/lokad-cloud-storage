#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>
    /// Azure retry policies for corner-situation and server errors.
    /// </summary>
    internal class RetryPolicies
    {
        private readonly ICloudStorageObserver _observer;

        /// <param name="observer">Can be <see langword="null"/>.</param>
        internal RetryPolicies(ICloudStorageObserver observer)
        {
            _observer = observer;
        }

        /// <summary>
        /// Retry policy for optimistic concurrency retrials.
        /// </summary>
        public ShouldRetry OptimisticConcurrency()
        {
            var random = new Random();
            Guid sequence = Guid.NewGuid();

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
                {
                    if (currentRetryCount >= 30)
                    {
                        retryInterval = TimeSpan.Zero;
                        return false;
                    }

                    retryInterval = TimeSpan.FromMilliseconds(random.Next(Math.Min(10000, 10 + currentRetryCount * currentRetryCount * 10)));

                    if (_observer != null)
                    {
                        _observer.Notify(new StorageOperationRetriedEvent(lastException, "OptimisticConcurrency", currentRetryCount, retryInterval, sequence));
                    }

                    return true;
                };
        }

        /// <summary>
        /// Retry policy which is applied to all Azure storage clients. Ignores the actual exception.
        /// </summary>
        public ShouldRetry ForAzureStorageClient()
        {
            // [abdullin]: in short this gives us MinBackOff + 2^(10)*Rand.(~0.5.Seconds()) at the last retry.

            // TODO (ruegg, 2011-05-26): This policy might actually be counterproductive and interfere with the other policies. Investigate.

            var random = new Random();
            Guid sequence = Guid.NewGuid();

            double deltaBackoff = TimeSpan.FromSeconds(0.5).TotalMilliseconds;
            double minBackoff = Microsoft.WindowsAzure.StorageClient.RetryPolicies.DefaultMinBackoff.TotalMilliseconds;
            double maxBackoff = Microsoft.WindowsAzure.StorageClient.RetryPolicies.DefaultMaxBackoff.TotalMilliseconds;

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
                {
                    if (currentRetryCount >= 10)
                    {
                        retryInterval = TimeSpan.Zero;
                        return false;
                    }

                    retryInterval = TimeSpan.FromMilliseconds(Math.Min(
                        maxBackoff,
                        minBackoff + ((Math.Pow(2.0, currentRetryCount) - 1.0) * random.Next((int)(deltaBackoff * 0.8), (int)(deltaBackoff * 1.2)))));

                    if (_observer != null)
                    {
                        _observer.Notify(new StorageOperationRetriedEvent(lastException, "StorageClient", currentRetryCount, retryInterval, sequence));
                    }

                    return true;
                };
        }

        /// <summary>
        /// Retry policy to temporarily back off in case of transient Azure server
        /// errors, system overload or in case the denial of service detection system
        /// thinks we're a too heavy user. Blocks the thread while backing off to
        /// prevent further requests for a while (per thread).
        /// </summary>
        public ShouldRetry TransientServerErrorBackOff()
        {
            Guid sequence = Guid.NewGuid();

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
                {
                    if (currentRetryCount >= 30 || !TransientServerErrorExceptionFilter(lastException))
                    {
                        retryInterval = TimeSpan.Zero;
                        return false;
                    }

                    // quadratic backoff, capped at 5 minutes
                    var c = currentRetryCount + 1;
                    retryInterval = TimeSpan.FromSeconds(Math.Min(300, c * c));

                    if (_observer != null)
                    {
                        _observer.Notify(new StorageOperationRetriedEvent(lastException, "TransientServerError", currentRetryCount, retryInterval, sequence));
                    }

                    return true;
                };
        }

        /// <summary>Similar to <see cref="TransientServerErrorBackOff"/>, yet
        /// the Table Storage comes with its own set or exceptions/.</summary>
        public ShouldRetry TransientTableErrorBackOff()
        {
            Guid sequence = Guid.NewGuid();

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (currentRetryCount >= 30 || !TransientTableErrorExceptionFilter(lastException))
                {
                    retryInterval = TimeSpan.Zero;
                    return false;
                }

                // quadratic backoff, capped at 5 minutes
                var c = currentRetryCount + 1;
                retryInterval = TimeSpan.FromSeconds(Math.Min(300, c * c));

                if (_observer != null)
                {
                    _observer.Notify(new StorageOperationRetriedEvent(lastException, "TransientTableError", currentRetryCount, retryInterval, sequence));
                }

                return true;
            };
        }

        /// <summary>
        /// Very patient retry policy to deal with container, queue or table instantiation
        /// that happens just after a deletion.
        /// </summary>
        public ShouldRetry SlowInstantiation()
        {
            Guid sequence = Guid.NewGuid();

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (currentRetryCount >= 30 || !SlowInstantiationExceptionFilter(lastException))
                {
                    retryInterval = TimeSpan.Zero;
                    return false;
                }

                // linear backoff
                retryInterval = TimeSpan.FromMilliseconds(100 * currentRetryCount);

                if (_observer != null)
                {
                    _observer.Notify(new StorageOperationRetriedEvent(lastException, "SlowInstantiation", currentRetryCount, retryInterval, sequence));
                }

                return true;
            };
        }

        /// <summary>
        /// Limited retry related to MD5 validation failure.
        /// </summary>
        public ShouldRetry NetworkCorruption()
        {
            Guid sequence = Guid.NewGuid();

            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (currentRetryCount >= 3 || !NetworkCorruptionExceptionFilter(lastException))
                {
                    retryInterval = TimeSpan.Zero;
                    return false;
                }

                // no backoff, retry immediately
                retryInterval = TimeSpan.Zero;

                if (_observer != null)
                {
                    _observer.Notify(new StorageOperationRetriedEvent(lastException, "NetworkCorruption", currentRetryCount, retryInterval, sequence));
                }

                return true;
            };
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

            var webException = exception as WebException;
            if (webException != null &&
                (webException.Status == WebExceptionStatus.ConnectionClosed ||
                 webException.Status == WebExceptionStatus.Timeout))
            {
                return true;
            }

            var ioException = exception as IOException;
            if (ioException != null)
            {
                return true;
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

            // The remote server returned an error: (500) Internal Server Error, or some timeout
            // The server should not timeout in theory (that's why there are limits and pagination)
            var webException = exception as WebException;
            if (webException != null &&
                (webException.Status == WebExceptionStatus.ProtocolError ||
                 webException.Status == WebExceptionStatus.ConnectionClosed ||
                 webException.Status == WebExceptionStatus.Timeout))
            {
                return true;
            }

            var ioException = exception as IOException;
            if (ioException != null)
            {
                return true;
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

        /// <summary>Hack around lack of proper way of retrieving the error code through a property.</summary>
        public static string GetErrorCode(DataServiceRequestException ex)
        {
            var r = new Regex(@"<code>(\w+)</code>", RegexOptions.IgnoreCase);
            var match = r.Match(ex.InnerException.Message);
            return match.Groups[1].Value;
        }

        // HACK: just duplicating the other overload of 'GetErrorCode'
        /// <summary>Hack around lack of proper way of retrieving the error code through a property.</summary>
        public static string GetErrorCode(DataServiceQueryException ex)
        {
            var r = new Regex(@"<code>(\w+)</code>", RegexOptions.IgnoreCase);
            var match = r.Match(ex.InnerException.Message);
            return match.Groups[1].Value;
        }
    }
}