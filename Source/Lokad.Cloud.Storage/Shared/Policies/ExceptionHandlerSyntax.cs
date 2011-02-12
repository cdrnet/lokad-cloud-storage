#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Storage.Shared.Policies
{
    /// <summary> Fluent API for defining <see cref="Policies.ActionPolicy"/> 
    /// that allows to handle exceptions. </summary>
    public static class ExceptionHandlerSyntax
    {
        /* Development notes
         * =================
         * If a stateful policy is returned by the syntax, 
         * it must be wrapped with the sync lock
         */

        static void DoNothing2(Exception ex, int count)
        {
        }

        static void DoNothing2(Exception ex, TimeSpan sleep)
        {
        }

        /// <summary>
        /// Builds <see cref="Policies.ActionPolicy"/> that will retry exception handling
        /// for a couple of times before giving up.
        /// </summary>
        /// <param name="syntax">The syntax.</param>
        /// <param name="retryCount">The retry count.</param>
        /// <returns>reusable instance of policy</returns>
        public static ActionPolicy Retry(this Syntax<ExceptionHandler> syntax, int retryCount)
        {
            if(null == syntax) throw new ArgumentNullException("syntax");

            Func<IRetryState> state = () => new RetryStateWithCount(retryCount, DoNothing2);
            return new ActionPolicy(action => RetryPolicy.Implementation(action, syntax.Target, state));
        }

        /// <summary>
        /// Builds <see cref="Policies.ActionPolicy"/> that will retry exception handling
        /// for a couple of times before giving up.
        /// </summary>
        /// <param name="syntax">The syntax.</param>
        /// <param name="retryCount">The retry count.</param>
        /// <param name="onRetry">The action to perform on retry (i.e.: write to log).
        /// First parameter is the exception and second one is its number in sequence. </param>
        /// <returns>reusable policy instance </returns>
        public static ActionPolicy Retry(this Syntax<ExceptionHandler> syntax, int retryCount, Action<Exception, int> onRetry)
        {
            if(null == syntax) throw new ArgumentNullException("syntax");
            if(retryCount <= 0) throw new ArgumentOutOfRangeException("retryCount");

            Func<IRetryState> state = () => new RetryStateWithCount(retryCount, onRetry);
            return new ActionPolicy(action => RetryPolicy.Implementation(action, syntax.Target, state));
        }

        /// <summary> Builds <see cref="Policies.ActionPolicy"/> that will keep retrying forever </summary>
        /// <param name="syntax">The syntax to extend.</param>
        /// <param name="onRetry">The action to perform when the exception could be retried.</param>
        /// <returns> reusable instance of policy</returns>
        public static ActionPolicy RetryForever(this Syntax<ExceptionHandler> syntax, Action<Exception> onRetry)
        {
            if(null == syntax) throw new ArgumentNullException("syntax");
            if(null == onRetry) throw new ArgumentNullException("onRetry");

            var state = new RetryState(onRetry);
            return new ActionPolicy(action => RetryPolicy.Implementation(action, syntax.Target, () => state));
        }

        /// <summary> <para>Builds the policy that will keep retrying as long as 
        /// the exception could be handled by the <paramref name="syntax"/> being 
        /// built and <paramref name="sleepDurations"/> is providing the sleep intervals.
        /// </para>
        /// </summary>
        /// <param name="syntax">The syntax.</param>
        /// <param name="sleepDurations">The sleep durations.</param>
        /// <param name="onRetry">The action to perform on retry (i.e.: write to log).
        /// First parameter is the exception and second one is the planned sleep duration. </param>
        /// <returns>new policy instance</returns>
        public static ActionPolicy WaitAndRetry(this Syntax<ExceptionHandler> syntax, IEnumerable<TimeSpan> sleepDurations,
            Action<Exception, TimeSpan> onRetry)
        {
            if(null == syntax) throw new ArgumentNullException("syntax");
            if(null == onRetry) throw new ArgumentNullException("onRetry");
            if(null == sleepDurations) throw new ArgumentNullException("sleepDurations");

            Func<IRetryState> state = () => new RetryStateWithSleep(sleepDurations, onRetry);
            return new ActionPolicy(action => RetryPolicy.Implementation(action, syntax.Target, state));
        }
    }
}
