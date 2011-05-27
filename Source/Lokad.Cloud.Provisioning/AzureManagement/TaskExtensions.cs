using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    public static class TaskExtensions
    {
        public static void ContinuePropagateWith<TCompletion, TTask>(this Task<TTask> task, TaskCompletionSource<TCompletion> completionSource, CancellationToken cancellationToken, Action<Task<TTask>> handleCompleted)
        {
            task.ContinueWith(t =>
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

                    // TODO (ruegg, 2011-05-27): Catch and forward errors to completionSOurce

                    handleCompleted(t);

                }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
