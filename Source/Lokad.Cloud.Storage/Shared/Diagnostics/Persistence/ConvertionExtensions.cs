#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System.Linq;
using Lokad.Cloud.Storage.Shared.Diagnostics;

namespace Lokad.Diagnostics.Persist
{
    /// <summary>
    /// Helper extensions for converting to/from data classes in the Diagnostics namespace
    /// </summary>
    public static class ConversionExtensions
    {
        /// <summary>
        /// Converts immutable statistics objects to the persistence objects
        /// </summary>
        /// <param name="statisticsArray">The immutable statistics objects.</param>
        /// <returns>array of persistence objects</returns>
        public static ExecutionData[] ToPersistence(this ExecutionStatistics[] statisticsArray)
        {
            return statisticsArray.Select(es => new ExecutionData
            {
                CloseCount = es.CloseCount,
                Counters = es.Counters,
                Name = es.Name,
                OpenCount = es.OpenCount,
                RunningTime = es.RunningTime
            }).ToArray();
        }

        /// <summary>
        /// Converts persistence objects to immutable statistics objects
        /// </summary>
        /// <param name="dataArray">The persistence data objects.</param>
        /// <returns>array of statistics objects</returns>
        public static ExecutionStatistics[] FromPersistence(this ExecutionData[] dataArray)
        {
            return dataArray.Select(
                d => new ExecutionStatistics(
                    d.Name,
                    d.OpenCount,
                    d.CloseCount,
                    d.Counters,
                    d.RunningTime)).ToArray();
        }
    }
}