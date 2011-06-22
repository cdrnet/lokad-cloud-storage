#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion
using System;

namespace Lokad.Cloud.Storage.Shared.Diagnostics
{
    /// <summary>
    /// Statistics about some execution counter
    /// </summary>
    [Serializable]
    public sealed class ExecutionStatistics
    {
        readonly long _openCount;
        readonly long _closeCount;
        readonly long[] _counters;
        readonly long _runningTime;
        readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionStatistics"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="openCount">The open count.</param>
        /// <param name="closeCount">The close count.</param>
        /// <param name="counters">The counters.</param>
        /// <param name="runningTime">The running time.</param>
        public ExecutionStatistics(string name, long openCount, long closeCount, long[] counters, long runningTime)
        {
            _openCount = openCount;
            _closeCount = closeCount;
            _counters = counters;
            _runningTime = runningTime;
            _name = name;
        }

        /// <summary>
        /// Gets the number of times the counter has been opened
        /// </summary>
        /// <value>The open count.</value>
        public long OpenCount
        {
            get { return _openCount; }
        }

        /// <summary>
        /// Gets the number of times the counter has been properly closed.
        /// </summary>
        /// <value>The close count.</value>
        public long CloseCount
        {
            get { return _closeCount; }
        }

        /// <summary>
        /// Gets the native counters collected by this counter.
        /// </summary>
        /// <value>The counters.</value>
        public long[] Counters
        {
            get { return _counters; }
        }

        /// <summary>
        /// Gets the total running time between open and close statements in ticks.
        /// </summary>
        /// <value>The running time expressed in 100-nanosecond units.</value>
        public long RunningTime
        {
            get { return _runningTime; }
        }

        /// <summary>
        /// Gets the name for this counter.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return _name; }
        }
    }
}
