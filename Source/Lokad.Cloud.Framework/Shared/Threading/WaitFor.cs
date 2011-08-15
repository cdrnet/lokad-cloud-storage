#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Diagnostics;
using System.Threading;

namespace Lokad.Cloud.Shared.Threading
{
    /// <summary>
    /// Helper class for invoking tasks with timeout.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    internal sealed class WaitFor<TResult>
    {
        readonly TimeSpan _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitFor{T}"/> class, 
        /// using the specified timeout for all operations.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        private WaitFor(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        /// <summary>
        /// Executes the specified function within the current thread, aborting it
        /// if it does not complete within the specified timeout interval. 
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>result of the function</returns>
        /// <remarks>
        /// The performance trick is that we do not interrupt the current
        /// running thread. Instead, we just create a watcher that will sleep
        /// until the originating thread terminates or until the timeout is
        /// elapsed.
        /// </remarks>
        /// <exception cref="ArgumentNullException">if function is null</exception>
        /// <exception cref="TimeoutException">if the function does not finish in time </exception>
        private TResult Run(Func<TResult> function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            // CAUTION: do not refactor unless you know what you're doing.

            var sync = new object();

            // run has finished
            // only written to in main thread
            var isCompleted = false;
            
            // run was cancelled because of a timeout
            // only written to in the watcher thread
            var isCancelled = false;

            var stopwatch = Stopwatch.StartNew();

            WaitCallback watcher = obj =>
            {
                var watchedThread = (Thread)obj;

                lock (sync)
                {
                    // ReSharper disable AccessToModifiedClosure
                    if (!isCompleted)
                    {
                        isCancelled = !Monitor.Wait(sync, _timeout);
                    }

                    if (isCompleted)
                    {
                        isCancelled = false;
                    }
                    // ReSharper restore AccessToModifiedClosure
                }

                if (isCancelled)
                {
                    watchedThread.Abort();
                }
            };

            try
            {
                ThreadPool.QueueUserWorkItem(watcher, Thread.CurrentThread);
                return function();
            }
            catch (ThreadAbortException)
            {
                bool cancelled;
                lock (sync)
                {
                    cancelled = isCancelled;
                }

                if (!cancelled)
                {
                    // This is not our own exception.
                    throw;
                }

                Thread.ResetAbort();
                throw new TimeoutException(string.Format("The operation with timeout {0} has been aborted after {1}.", _timeout, stopwatch.Elapsed));
            }
            finally
            {
                lock (sync)
                {
                    isCompleted = true;
                    Monitor.Pulse(sync);
                }
            }
        }

        /// <summary>
        /// Executes the specified function within the current thread, aborting it
        /// if it does not complete within the specified timeout interval.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="function">The function.</param>
        /// <returns>result of the function</returns>
        /// <remarks>
        /// The performance trick is that we do not interrupt the current
        /// running thread. Instead, we just create a watcher that will sleep
        /// until the originating thread terminates or until the timeout is
        /// elapsed.
        /// </remarks>
        /// <exception cref="ArgumentNullException">if function is null</exception>
        /// <exception cref="TimeoutException">if the function does not finish in time </exception>
        public static TResult Run(TimeSpan timeout, Func<TResult> function)
        {
            return new WaitFor<TResult>(timeout).Run(function);
        }
    }
}
