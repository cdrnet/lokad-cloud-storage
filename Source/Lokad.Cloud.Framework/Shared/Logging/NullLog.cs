#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;

namespace Lokad.Cloud.Storage.Shared.Logging
{
    /// <summary> <see cref="ILog"/> that does not do anything.</summary>
    [Serializable]
    public sealed class NullLog : ILog
    {
        /// <summary>
        /// Singleton instance of the <see cref="NullLog"/>.
        /// </summary>
        public static readonly ILog Instance = new NullLog();

        /// <summary>
        /// Singleton instance of the <see cref="NullLogProvider"/>.
        /// </summary>
        public static readonly ILogProvider Provider = new NullLogProvider();

        NullLog()
        {
        }

        void ILog.Log(LogLevel level, object message)
        {
        }

        void ILog.Log(LogLevel level, Exception ex, object message)
        {
        }

        bool ILog.IsEnabled(LogLevel level)
        {
            return false;
        }
    }

    /// <summary>Returns the <see cref="ILog"/> that does not do anything.</summary>
    public sealed class NullLogProvider : ILogProvider
    {
        /// <remarks></remarks>
        public ILog Get(string key)
        {
            return NullLog.Instance;
        }
    }
}
