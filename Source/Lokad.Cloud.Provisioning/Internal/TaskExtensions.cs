#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Provisioning.Internal
{
    internal static class TaskExtensions
    {
        /// <remarks>Only put short operations in this continuation, or do them async, as the continuation is executed synchronously.</remarks>
        public static void ContinuePropagateWith<TCompletion, TTask>(this Task<TTask> task, TaskCompletionSource<TCompletion> completionSource, CancellationToken cancellationToken, Action<Task<TTask>> handleCompleted)
        {
            task.ContinueWith(t =>
                {
                    try
                    {
                        if (t.IsFaulted)
                        {
                            var baseException = t.Exception.GetBaseException();

                            if (cancellationToken.IsCancellationRequested && baseException is HttpException)
                            {
                                // If cancelled: HttpExceptions are assumed to be caused by the cancellation, hence we ignore them and cancel.
                                completionSource.TrySetCanceled();
                            }
                            else
                            {
                                completionSource.TrySetException(baseException);
                            }
                            return;
                        }

                        if (t.IsCanceled)
                        {
                            completionSource.TrySetCanceled();
                            return;
                        }

                        handleCompleted(t);
                    }
                    catch (Exception exception)
                    {
                        completionSource.TrySetException(exception);
                    }

                }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
