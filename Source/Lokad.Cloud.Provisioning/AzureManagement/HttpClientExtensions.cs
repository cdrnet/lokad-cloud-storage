using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
            var completionSource = new TaskCompletionSource<T>();
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            SendXmlAsync(httpClient, request, completionSource, cancellationToken, shouldRetry, 0, response =>
                {
                    response.EnsureSuccessStatusCode();
                    handle(XDocument.Load(response.Content.ContentReadStream), completionSource);
                });

            return completionSource.Task;
        }

        public static Task<T> PostXmlAsync<T>(this HttpClient httpClient, string requestUri, XDocument content, CancellationToken cancellationToken, ShouldRetry shouldRetry, Action<HttpResponseMessage, TaskCompletionSource<T>> handle)
        {
            var completionSource = new TaskCompletionSource<T>();

            var stream = new MemoryStream();
            content.Declaration = new XDeclaration("1.0", "utf-8", "yes");
            content.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml") { CharSet = "utf-8"};
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = streamContent };

            SendXmlAsync(httpClient, request, completionSource, cancellationToken, shouldRetry, 0, response => handle(response, completionSource));

            completionSource.Task.ContinueWith(task => stream.Dispose());
            return completionSource.Task;
        }

        private static void SendXmlAsync<T>(
            HttpClient httpClient, HttpRequestMessage request, TaskCompletionSource<T> completionSource, CancellationToken cancellationToken,
            ShouldRetry shouldRetry, int retryCount,
            Action<HttpResponseMessage> handle)
        {
            httpClient.SendAsync(request, cancellationToken).ContinueWith(task =>
            {
                try
                {
                    bool isCancellationRequested = cancellationToken.IsCancellationRequested;

                    if (task.IsFaulted)
                    {
                        var baseException = task.Exception.GetBaseException();

                        // If cancelled: HttpExceptions are assumed to be caused by the cancellation, hence we ignore them and cancel.
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

                    handle(task.Result);

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
