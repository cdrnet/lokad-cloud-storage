#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Collections.Generic;
using System.Threading;

namespace Lokad.Cloud.Storage.Shared.Policies
{
    sealed class RetryStateWithSleep : IRetryState
    {
        readonly IEnumerator<TimeSpan> _enumerator;
        readonly Action<Exception, TimeSpan> _onRetry;

        public RetryStateWithSleep(IEnumerable<TimeSpan> sleepDurations, Action<Exception, TimeSpan> onRetry)
        {
            _onRetry = onRetry;
            _enumerator = sleepDurations.GetEnumerator();
        }


        public bool CanRetry(Exception ex)
        {
            if (_enumerator.MoveNext())
            {
                var current = _enumerator.Current;
                _onRetry(ex, current);
                Thread.Sleep(current);
                return true;
            }
            return false;
        }
    }
}
