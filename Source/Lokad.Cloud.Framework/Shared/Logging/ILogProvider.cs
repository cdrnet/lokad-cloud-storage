#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

namespace Lokad.Cloud.Storage.Shared.Logging
{
    /// <remarks></remarks>
    public interface ILogProvider
    {
        /// <remarks></remarks>
        ILog Get(string key);
    }
}
