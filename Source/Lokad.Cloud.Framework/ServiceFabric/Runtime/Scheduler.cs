#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Lokad.Cloud.Instrumentation;
using Lokad.Cloud.Instrumentation.Events;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
    /// <summary>
    /// Round robin scheduler with adaptive modifications: tasks that claim to have
    /// more work ready are given the chance to continue until they reach a fixed
    /// time limit (greedy), and the scheduling is slowed down when all available
    /// services skip execution consecutively.
    /// </summary>
    public class Scheduler
    {
        readonly ICloudRuntimeObserver _observer;

        readonly List<CloudService> _services;
        readonly Func<CloudService, ServiceExecutionFeedback> _schedule;
        readonly object _sync = new object();

        /// <summary>Duration to keep pinging the same cloud service if service is active.</summary>
        readonly TimeSpan _moreOfTheSame = TimeSpan.FromSeconds(60);

        /// <summary>Resting duration.</summary>
        readonly TimeSpan _idleSleep = TimeSpan.FromSeconds(10);

        CloudService _currentService;
        volatile bool _isRunning;

        /// <summary>
        /// Creates a new instance of the Scheduler class.
        /// </summary>
        /// <param name="services">cloud services</param>
        /// <param name="schedule">Action to be invoked when a service is scheduled to run</param>
        public Scheduler(List<CloudService> services, Func<CloudService, ServiceExecutionFeedback> schedule, ICloudRuntimeObserver observer = null)
        {
            _observer = observer;
            _services = services;
            _schedule = schedule;
        }

        public CloudService CurrentlyScheduledService
        {
            get { return _currentService; }
        }

        public IEnumerable<Action> Schedule()
        {
            var services = _services;
            var currentServiceIndex = -1;
            var skippedConsecutively = 0;

            _isRunning = true;

            if (_observer != null)
            {
                _observer.Notify(new CloudRuntimeBusyEvent(DateTimeOffset.UtcNow));
            }

            while (_isRunning)
            {
                currentServiceIndex = (currentServiceIndex + 1) % services.Count;
                _currentService = services[currentServiceIndex];

                var result = ServiceExecutionFeedback.DontCare;
                var isRunOnce = false;

                // 'more of the same pattern'
                // as long the service is active, keep triggering the same service
                // for at least 1min (in order to avoid a single service to monopolize CPU)
                var start = DateTimeOffset.UtcNow;
                
                while (DateTimeOffset.UtcNow.Subtract(start) < _moreOfTheSame && _isRunning && DemandsImmediateStart(result))
                {
                    yield return () =>
                        {
                            result = _schedule(_currentService);
                        };
                    isRunOnce |= WasSuccessfullyExecuted(result);
                }

                skippedConsecutively = isRunOnce ? 0 : skippedConsecutively + 1;
                if (skippedConsecutively >= services.Count && _isRunning)
                {
                    // We are not using 'Thread.Sleep' because we want the worker
                    // to terminate fast if 'Stop' is requested.

                    if (_observer != null)
                    {
                        _observer.Notify(new CloudRuntimeIdleEvent(DateTimeOffset.UtcNow));
                    }

                    lock (_sync)
                    {
                        Monitor.Wait(_sync, _idleSleep);
                    }

                    if (_observer != null)
                    {
                        _observer.Notify(new CloudRuntimeBusyEvent(DateTimeOffset.UtcNow));
                    }

                    skippedConsecutively = 0;
                }
            }

            _currentService = null;
        }

        /// <summary>Waits until the current service completes, and stop the scheduling.</summary>
        /// <remarks>This method CANNOT be used in case the environment is stopping,
        /// because the termination is going to be way too slow.</remarks>
        public void AbortWaitingSchedule()
        {
            _isRunning = false;
            lock (_sync)
            {
                Monitor.Pulse(_sync);
            }
        }

        /// <summary>
        /// The service was successfully executed and it might make sense to execute
        /// it again immediately (greedy).
        /// </summary>
        bool DemandsImmediateStart(ServiceExecutionFeedback feedback)
        {
            return feedback == ServiceExecutionFeedback.WorkAvailable
                || feedback == ServiceExecutionFeedback.DontCare;
        }

        /// <summary>
        /// The service was actually executed (not skipped) and did not fail.
        /// </summary>
        bool WasSuccessfullyExecuted(ServiceExecutionFeedback feedback)
        {
            return feedback != ServiceExecutionFeedback.Skipped
                && feedback != ServiceExecutionFeedback.Failed;
        }
    }
}
