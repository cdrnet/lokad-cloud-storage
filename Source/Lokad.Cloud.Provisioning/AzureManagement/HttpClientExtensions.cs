using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    internal static class HttpClientExtensions
    {
        public static Task<T> GetXmlAsync<T>(this HttpClient httpClient, string requestUri, CancellationToken cancellationToken, ShouldRetry shouldRetry, Action<XDocument, TaskCompletionSource<T>> handle)
        {
            var tcs = new TaskCompletionSource<T>();
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            SendXmlAsync(httpClient, request, tcs, cancellationToken, shouldRetry, 0, handle);
            return tcs.Task;
        }

        private static void SendXmlAsync<T>(
            HttpClient httpClient, HttpRequestMessage request, TaskCompletionSource<T> completionSource, CancellationToken cancellationToken,
            ShouldRetry shouldRetry, int retryCount,
            Action<XDocument, TaskCompletionSource<T>> handle)
        {
            httpClient.SendAsync(request, cancellationToken).ContinueWith(task =>
            {
                try
                {
                    bool isCancellationRequested = cancellationToken.IsCancellationRequested;

                    if (task.IsFaulted)
                    {
                        var baseException = task.Exception.GetBaseException();

                        // If cancelled: HttpExceptions are expected, hence we ignore them and cancel.
                        if (isCancellationRequested && baseException is HttpException)
                        {
                            completionSource.TrySetCanceled();
                            return;
                        }

                        // Fail task if we don't retry
                        TimeSpan retryDelay;
                        if (shouldRetry == null || !shouldRetry(retryCount, baseException, out retryDelay))
                        {
                            completionSource.TrySetException(baseException);
                            return;
                        }

                        // If cancelled: We would normally retry, but it was cancelled, hence we ignore the fault and cancel.
                        if (isCancellationRequested)
                        {
                            completionSource.TrySetCanceled();
                            return;
                        }

                        // Retry immediately
                        if (retryDelay <= TimeSpan.Zero)
                        {
                            SendXmlAsync(httpClient, request, completionSource, cancellationToken, shouldRetry, retryCount + 1, handle);
                            return;
                        }

                        // Retry later
                        new Timer(self =>
                            {
                                // Consider to use TaskEx.Delay instead once available
                                ((IDisposable)self).Dispose();
                                SendXmlAsync(httpClient, request, completionSource, cancellationToken, shouldRetry, retryCount + 1, handle);
                            }).Change(retryDelay, TimeSpan.FromMilliseconds(-1));

                        return;
                    }

                    if (task.IsCanceled)
                    {
                        completionSource.TrySetCanceled();
                        return;
                    }

                    var response = task.Result;
                    response.EnsureSuccessStatusCode();

                    handle(XDocument.Load(response.Content.ContentReadStream), completionSource);
                }
                catch (Exception e)
                {
                    // we do not retry if the exception happened in the continuation
                    completionSource.TrySetException(e);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
