#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.Azure
{
    internal static class Retry
    {
        public static void Do(this RetryPolicy retryPolicy, Action action)
        {
            var policy = retryPolicy();
            int retryCount = 0;

            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (policy(retryCount, exception, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        public static void Do(this RetryPolicy firstPolicy, RetryPolicy secondPolicy, Action action)
        {
            var first = firstPolicy();
            var second = secondPolicy();
            int retryCount = 0;

            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (first(retryCount, exception, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    if (second(retryCount, exception, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        public static T Get<T>(this RetryPolicy retryPolicy, Func<T> action)
        {
            var policy = retryPolicy();
            int retryCount = 0;

            while (true)
            {
                try
                {
                    var result = action();
                    return result;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (policy(retryCount, exception, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        public static T Get<T>(this RetryPolicy firstPolicy, RetryPolicy secondPolicy, Func<T> action)
        {
            var first = firstPolicy();
            var second = secondPolicy();
            int retryCount = 0;

            while (true)
            {
                try
                {
                    var result = action();
                    return result;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (first(retryCount, exception, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    if (second(retryCount, exception, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        /// <remarks>Policy must support exceptions being null.</remarks>
        public static void DoUntilTrue(this RetryPolicy retryPolicy, Func<bool> action)
        {
            var policy = retryPolicy();
            int retryCount = 0;

            while (true)
            {
                try
                {
                    if (action())
                    {
                        return;
                    }

                    TimeSpan delay;
                    if (policy(retryCount, null, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw new TimeoutException("Failed to reach a successful result in a limited number of retrials");
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (policy(retryCount, exception, out delay))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }
    }
}
