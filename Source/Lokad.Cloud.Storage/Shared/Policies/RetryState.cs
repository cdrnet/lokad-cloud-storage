#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;

namespace Lokad.Cloud.Storage.Shared.Policies
{
    sealed class RetryState : IRetryState
    {
        readonly Action<Exception> _onRetry;

        public RetryState(Action<Exception> onRetry)
        {
            _onRetry = onRetry;
        }

        bool IRetryState.CanRetry(Exception ex)
        {
            _onRetry(ex);
            return true;
        }
    }
}
