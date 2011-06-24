#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>
    /// Facade to collect internal and external diagnostics statistics (pull or push)
    /// </summary>
    public class DiagnosticsAcquisition
    {
        readonly PartitionMonitor _partitionMonitor;
        readonly ServiceMonitor _serviceMonitor;

        public DiagnosticsAcquisition(ICloudDiagnosticsRepository repository)
        {
            _partitionMonitor = new PartitionMonitor(repository);
            _serviceMonitor = new ServiceMonitor(repository);
        }
        
        /// <summary>
        /// Collect (pull) internal and external diagnostics statistics and persists
        /// them in the diagnostics repository.
        /// </summary>
        public void CollectStatistics()
        {
            _partitionMonitor.UpdateStatistics();
            _serviceMonitor.UpdateStatistics();
        }

        /// <summary>
        /// Remove all statistics older than the provided time stamp from the
        /// persistent diagnostics repository.
        /// </summary>
        public void RemoveStatisticsBefore(DateTimeOffset before)
        {
            _partitionMonitor.RemoveStatisticsBefore(before);
            _serviceMonitor.RemoveStatisticsBefore(before);
        }

        /// <summary>
        /// Remove all statistics older than the provided number of periods from the
        /// persistent diagnostics repository (0 removes all but the current period).
        /// </summary>
        public void RemoveStatisticsBefore(int numberOfPeriods)
        {
            _partitionMonitor.RemoveStatisticsBefore(numberOfPeriods);
            _serviceMonitor.RemoveStatisticsBefore(numberOfPeriods);
        }
    }
}
