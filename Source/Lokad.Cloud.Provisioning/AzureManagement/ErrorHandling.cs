using System;
using System.Net;
using System.Net.Http;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    public static class ErrorHandling
    {
        private static Random _random = new Random();

        public static bool RetryOnServerErrors(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            if (IsServerError(lastException) && currentRetryCount <= 30)
            {
                lock (_random)
                {
                    retryInterval = TimeSpan.FromMilliseconds(_random.Next(Math.Min(10000, 10 + currentRetryCount * currentRetryCount * 10)));
                }

                return true;
            }

            retryInterval = TimeSpan.Zero;
            return false;
        }

        public static bool IsServerError(Exception exception)
        {
            HttpStatusCode statusCode;
            return TryGetHttpStatusCode(exception, out statusCode) && (int)statusCode >= 500 && (int)statusCode < 600;
        }

        public static bool TryGetHttpStatusCode(Exception exception, out HttpStatusCode httpStatusCode)
        {
            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                exception = aggregateException.GetBaseException();
            }

            var httpException = exception as HttpException;
            if (httpException == null)
            {
                httpStatusCode = default(HttpStatusCode);
                return false;
            }

            var webException = httpException.InnerException as WebException;
            if (webException == null)
            {
                httpStatusCode = default(HttpStatusCode);
                return false;
            }

            var httpWebResponse = webException.Response as HttpWebResponse;
            if (httpWebResponse == null)
            {
                httpStatusCode = default(HttpStatusCode);
                return false;
            }

            httpStatusCode = httpWebResponse.StatusCode;
            return true;
        }
    }
}
