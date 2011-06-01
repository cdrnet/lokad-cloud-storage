#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Lokad.Cloud.Provisioning
{
    public static class ProvisioningErrorHandling
    {
        public static bool IsTransientError(Exception exception)
        {
            HttpStatusCode httpStatus;
            if (TryGetHttpStatusCode(exception, out httpStatus))
            {
                // For HTTP Errors only retry on Server Errors: 5xx
                // Exception: 403/Forbidden, which we get sporadically despide correct credentials
                var statusCode = (int)httpStatus;
                return statusCode == 403 || (statusCode >= 500 && statusCode < 600);
            }

            WebExceptionStatus webStatus;
            if (TryGetWebStatusCode(exception, out webStatus))
            {
                switch(webStatus)
                {
                    case WebExceptionStatus.Timeout:
                    case WebExceptionStatus.ConnectionClosed:
                    case WebExceptionStatus.ProtocolError:
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.ReceiveFailure:
                    case WebExceptionStatus.SendFailure:
                    case WebExceptionStatus.PipelineFailure:
                    case WebExceptionStatus.SecureChannelFailure:
                    case WebExceptionStatus.ServerProtocolViolation:
                    case WebExceptionStatus.KeepAliveFailure:
                    case WebExceptionStatus.Pending:
                    case WebExceptionStatus.UnknownError:
                        return true;
                    default:
                        return false;
                }
            }

            return exception is IOException;
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

        public static bool TryGetWebStatusCode(Exception exception, out WebExceptionStatus webStatusCode)
        {
            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                exception = aggregateException.GetBaseException();
            }

            var httpException = exception as HttpException;
            if (httpException == null)
            {
                webStatusCode = default(WebExceptionStatus);
                return false;
            }

            var webException = httpException.InnerException as WebException;
            if (webException == null)
            {
                webStatusCode = default(WebExceptionStatus);
                return false;
            }

            webStatusCode = webException.Status;
            return true;
        }
    }
}
