#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Services;
using Lokad.Cloud.Storage;

// TODO: blobs are sequentially enumerated, performance issue
// if there are more than a few dozen services

namespace Lokad.Cloud.Management
{
    /// <summary>Management facade for scheduled cloud services.</summary>
    public class CloudServiceScheduling
    {
        readonly IBlobStorageProvider _blobProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudServiceScheduling"/> class.
        /// </summary>
        public CloudServiceScheduling(RuntimeProviders runtimeProviders)
        {
            _blobProvider = runtimeProviders.BlobStorage;
        }

        /// <summary>
        /// Enumerate infos of all cloud service schedules.
        /// </summary>
        public List<CloudServiceSchedulingInfo> GetSchedules()
        {
            // TODO: Redesign to make it self-contained (so that we don't need to pass the name as well)

            return _blobProvider.ListBlobNames(ScheduledServiceStateName.GetPrefix())
                .Select(name => System.Tuple.Create(name, _blobProvider.GetBlob(name)))
                .Where(pair => pair.Item2.HasValue)
                .Select(pair =>
                    {
                        var state = pair.Item2.Value;
                        var info = new CloudServiceSchedulingInfo
                            {
                                ServiceName = pair.Item1.ServiceName,
                                TriggerInterval = state.TriggerInterval,
                                LastExecuted = state.LastExecuted,
                                WorkerScoped = state.SchedulePerWorker,
                                LeasedBy = Maybe<string>.Empty,
                                LeasedSince = Maybe<DateTimeOffset>.Empty,
                                LeasedUntil = Maybe<DateTimeOffset>.Empty
                            };

                        if (state.Lease != null)
                        {
                            info.LeasedBy = state.Lease.Owner;
                            info.LeasedSince = state.Lease.Acquired;
                            info.LeasedUntil = state.Lease.Timeout;
                        }

                        return info;
                    })
                .ToList();
        }

        /// <summary>
        /// Gets infos of one cloud service schedule.
        /// </summary>
        public CloudServiceSchedulingInfo GetSchedule(string serviceName)
        {
            var blob = _blobProvider.GetBlob(new ScheduledServiceStateName(serviceName));

            var state = blob.Value;
            var info = new CloudServiceSchedulingInfo
            {
                ServiceName = serviceName,
                TriggerInterval = state.TriggerInterval,
                LastExecuted = state.LastExecuted,
                WorkerScoped = state.SchedulePerWorker,
                LeasedBy = Maybe<string>.Empty,
                LeasedSince = Maybe<DateTimeOffset>.Empty,
                LeasedUntil = Maybe<DateTimeOffset>.Empty
            };

            if (state.Lease != null)
            {
                info.LeasedBy = state.Lease.Owner;
                info.LeasedSince = state.Lease.Acquired;
                info.LeasedUntil = state.Lease.Timeout;
            }

            return info;
        }

        /// <summary>
        /// Enumerate the names of all scheduled cloud service.
        /// </summary>
        public List<string> GetScheduledServiceNames()
        {
            return _blobProvider.ListBlobNames(ScheduledServiceStateName.GetPrefix())
                .Select(reference => reference.ServiceName).ToList();
        }

        /// <summary>
        /// Enumerate the names of all scheduled user cloud service (system services are skipped).
        /// </summary>
        public List<string> GetScheduledUserServiceNames()
        {
            var systemServices =
                new[]
                    {
                        typeof(GarbageCollectorService),
                        typeof(DelayedQueueService),
                        typeof(MonitoringService),
                        typeof(MonitoringDataRetentionService),
                        typeof(AssemblyConfigurationUpdateService)
                    }
                    .Select(type => type.FullName)
                    .ToList();

            return GetScheduledServiceNames()
                .Where(service => !systemServices.Contains(service)).ToList();
        }

        /// <summary>
        /// Set the trigger interval of a cloud service.
        /// </summary>
        public void SetTriggerInterval(string serviceName, TimeSpan triggerInterval)
        {
            _blobProvider.UpdateBlobIfExist(
                new ScheduledServiceStateName(serviceName),
                state =>
                    {
                        state.TriggerInterval = triggerInterval;
                        return state;
                    });
        }

        /// <summary>
        /// Remove the scheduling information of a cloud service
        /// </summary>
        public void ResetSchedule(string serviceName)
        {
            _blobProvider.DeleteBlobIfExist(new ScheduledServiceStateName(serviceName));
        }

        /// <summary>
        /// Forcibly remove the synchronization lease of a periodic cloud service
        /// </summary>
        public void ReleaseLease(string serviceName)
        {
            _blobProvider.UpdateBlobIfExist(
                new ScheduledServiceStateName(serviceName),
                state =>
                    {
                        state.Lease = null;
                        return state;
                    });
        }
    }
}
