#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;

namespace Lokad.Cloud.Storage.Shared.Policies
{
    interface IRetryState
    {
        bool CanRetry(Exception ex);
    }
}
