#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Runtime.Serialization;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Shared.Logging;

namespace Lokad.Cloud.ServiceFabric
{
    /// <summary>Configuration state of the <seealso cref="ScheduledService"/>.</summary>
    [Serializable, DataContract]
    public class ScheduledServiceState
    {
        /// <summary>Indicates the frequency this service must be called.</summary>
        [DataMember]
        public TimeSpan TriggerInterval { get; set; }

        /// <summary>Date of the last execution.</summary>
        [DataMember]
        public DateTimeOffset LastExecuted { get; set; }

        /// <summary>
        /// Lease state info to support synchronized exclusive execution of this
        /// service (applies only to cloud scoped service, not per worker scheduled
        /// ones). If <c>null</c> then the service is not currently leased by any
        /// worker.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public SynchronizationLeaseState Lease { get; set; }

        /// <summary>
        /// Indicates whether this service is currently running
        /// (apply only to globally scoped services, not per worker ones)
        /// .</summary>
        [Obsolete("Use the Lease mechanism instead.")]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsBusy { get; set; }

        /// <summary>Indicates whether the services is called once
        /// every N seconds for the entire distributed app, or
        /// if the service is called once every N seconds on each
        /// worker.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool SchedulePerWorker { get; set; }
    }

    /// <summary>Strong typed blob name for <see cref="ScheduledServiceState"/>.</summary>
    public class ScheduledServiceStateName : BlobName<ScheduledServiceState>
    {
        public override string ContainerName
        {
            get { return ScheduledService.ScheduleStateContainer; }
        }

        /// <summary>Name of the service being referred to.</summary>
        [Rank(0)] public readonly string ServiceName;

        /// <summary>Instantiate the reference associated to the specified service.</summary>
        public ScheduledServiceStateName(string serviceName)
        {
            ServiceName = serviceName;
        }

        /// <summary>Helper for service states enumeration.</summary>
        public static ScheduledServiceStateName GetPrefix()
        {
            return new ScheduledServiceStateName(null);
        }
    }

    /// <summary>This cloud service is automatically called by the framework
    /// on scheduled basis. Scheduling options are provided through the
    /// <see cref="ScheduledServiceSettingsAttribute"/>.</summary>
    /// <remarks>A empty constructor is needed for instantiation through reflection.</remarks>
    public abstract class ScheduledService : CloudService, IDisposable
    {
        internal const string ScheduleStateContainer = "lokad-cloud-schedule-state";

        readonly bool _scheduledPerWorker;
        readonly string _workerKey;
        readonly TimeSpan _leaseTimeout;
        readonly TimeSpan _defaultTriggerPeriod;

        DateTimeOffset _workerScopeLastExecuted;

        bool _isLeaseOwner;

        /// <summary>Default Constructor</summary>
        protected ScheduledService()
        {
            // runtime fixed settings
            _leaseTimeout = ExecutionTimeout + TimeSpan.FromMinutes(5);
            _workerKey = CloudEnvironment.PartitionKey;

            // default setting
            _scheduledPerWorker = false;
            _defaultTriggerPeriod = TimeSpan.FromHours(1);

            // overwrite settings with config in the attribute - if available
            var settings = GetType().GetCustomAttributes(typeof(ScheduledServiceSettingsAttribute), true)
                                    .FirstOrDefault() as ScheduledServiceSettingsAttribute;
            if (settings != null)
            {
                _scheduledPerWorker = settings.SchedulePerWorker;

                if (settings.TriggerInterval > 0)
                {
                    _defaultTriggerPeriod = TimeSpan.FromSeconds(settings.TriggerInterval);
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            // Auto-register the service for finalization:
            // 1) Registration should not be made within the constructor
            //    because providers are not ready at this phase.
            // 2) Hasty finalization is needed only for cloud-scoped scheduled
            //    scheduled services (because they have a lease).
            if (!_scheduledPerWorker)
            {
                Providers.RuntimeFinalizer.Register(this);
            }
        }

        /// <seealso cref="CloudService.StartImpl"/>
        protected sealed override ServiceExecutionFeedback StartImpl()
        {
            var stateReference = new ScheduledServiceStateName(Name);

            // 1. SIMPLE WORKER-SCOPED SCHEDULING CASE

            if (_scheduledPerWorker)
            {
                var blobState = RuntimeProviders.BlobStorage.GetBlob(stateReference);
                if (!blobState.HasValue)
                {
                    // even though we will never change it from here, a state blob 
                    // still needs to exist so it can be configured by the console
                    var newState = GetDefaultState();
                    RuntimeProviders.BlobStorage.PutBlob(stateReference, newState);
                    blobState = newState;
                }

                var state = blobState.Value;

                var now = DateTimeOffset.UtcNow;
                if (now.Subtract(state.TriggerInterval) >= _workerScopeLastExecuted)
                {
                    _workerScopeLastExecuted = now;
                    StartOnSchedule();
                    return ServiceExecutionFeedback.DoneForNow;
                }

                return ServiceExecutionFeedback.Skipped;
            }

            // 2. CHECK WHETHER WE SHOULD EXECUTE NOW, ACQUIRE LEASE IF SO

            // checking if the last update is not too recent, and eventually
            // update this value if it's old enough. When the update fails,
            // it simply means that another worker is already on its ways
            // to execute the service.

            var resultIfChanged = RuntimeProviders.BlobStorage.UpsertBlobOrSkip(
                stateReference,
                () =>
                    {
                        // create new state and lease, and execute
                        var now = DateTimeOffset.UtcNow;
                        var newState = GetDefaultState();
                        newState.LastExecuted = now;
                        newState.Lease = CreateLease(now);
                        return newState;
                    },
                state =>
                    {
                        var now = DateTimeOffset.UtcNow;
                        if (now.Subtract(state.TriggerInterval) < state.LastExecuted)
                        {
                            // was recently executed somewhere; skip
                            return Maybe<ScheduledServiceState>.Empty;
                        }

                        if (state.Lease != null)
                        {
                            if (state.Lease.Timeout > now)
                            {
                                // update needed but blocked by lease; skip
                                return Maybe<ScheduledServiceState>.Empty;
                            }

                            Log.WarnFormat(
                                "ScheduledService {0}: Expired lease owned by {1} was reset after blocking for {2} minutes.",
                                Name, state.Lease.Owner, (int) (now - state.Lease.Acquired).TotalMinutes);
                        }

                        // create lease and execute
                        state.LastExecuted = now;
                        state.Lease = CreateLease(now);
                        return state;
                    });

            // 3. IF WE SHOULD NOT EXECUTE NOW, SKIP

            if (!resultIfChanged.HasValue)
            {
                return ServiceExecutionFeedback.Skipped;
            }

            _isLeaseOwner = true; // flag used for eventual runtime shutdown

            try
            {
                // 4. ACTUAL EXECUTION
                StartOnSchedule();
                return ServiceExecutionFeedback.DoneForNow;
            }
            finally
            {
                // 5. RELEASE THE LEASE
                SurrenderLease();
                _isLeaseOwner = false;
            }
        }

        /// <summary>The lease can be surrender in two situations:
        /// 1- the service completes normally, and we surrender the lease accordingly.
        /// 2- the runtime is being shutdown, and we can't hold the lease any further. 
        /// </summary>
        void SurrenderLease()
        {
            // we need a full update here (instead of just uploading the cached blob)
            // to ensure we do not overwrite changes made in the console in the meantime
            // (e.g. changed trigger interval), and to resolve the edge case when
            // a lease has been forcefully removed from the console and another service
            // has taken a lease in the meantime.

            RuntimeProviders.BlobStorage.UpdateBlobIfExistOrSkip(
                new ScheduledServiceStateName(Name),
                state =>
                    {
                        if (state.Lease == null || state.Lease.Owner != _workerKey)
                        {
                            // skip
                            return Maybe<ScheduledServiceState>.Empty;
                        }

                        // remove lease
                        state.Lease = null;
                        return state;
                    });
        }

        /// <summary>Don't call this method. Disposing the scheduled service
        /// should only be done by the <see cref="IRuntimeFinalizer"/> when
        /// the environment is being forcibly shut down.</summary>
        public void Dispose()
        {
            if(_isLeaseOwner)
            {
                SurrenderLease();
                _isLeaseOwner = false;
            }
        }

        /// <summary>
        /// Prepares this service's default state based on its settings attribute.
        /// In case no attribute is found then Maybe.Empty is returned.
        /// </summary>
        private ScheduledServiceState GetDefaultState()
        {
            return new ScheduledServiceState
                {
                    LastExecuted = DateTimeOffset.MinValue,
                    TriggerInterval = _defaultTriggerPeriod,
                    SchedulePerWorker = _scheduledPerWorker
                };
        }

        /// <summary>Prepares a new lease.</summary>
        private SynchronizationLeaseState CreateLease(DateTimeOffset now)
        {
            return new SynchronizationLeaseState
                {
                    Acquired = now,
                    Timeout = now + _leaseTimeout,
                    Owner = _workerKey
                };
        }

        /// <summary>Called by the framework.</summary>
        /// <remarks>We suggest not performing any heavy processing here. In case
        /// of heavy processing, put a message and use <see cref="QueueService{T}"/>
        /// instead.</remarks>
        protected abstract void StartOnSchedule();
    }
}