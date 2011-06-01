#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Lokad.Cloud.Provisioning.Instrumentation.Events;
using Lokad.Cloud.Storage.Shared.Logging;

namespace Lokad.Cloud.Diagnostics
{
    // TODO (ruegg, 2011-05-30): Temporary class to maintain logging via system events for now -> rework

    internal class CloudProvisioningLogger : Autofac.IStartable, IDisposable
    {
        private readonly IObservable<ICloudProvisioningEvent> _observable;
        private readonly ILog _log;
        private readonly List<IDisposable> _subscriptions;

        public CloudProvisioningLogger(IObservable<ICloudProvisioningEvent> observable, ILog log)
        {
            _observable = observable;
            _log = log;
            _subscriptions = new List<IDisposable>();
        }

        void Autofac.IStartable.Start()
        {
            if (_log == null || _observable == null)
            {
                return;
            }

            _subscriptions.Add(_observable.OfType<ProvisioningOperationRetriedEvent>()
                .Buffer(TimeSpan.FromMinutes(5))
                .Subscribe(events =>
                    {
                        foreach (var group in events.GroupBy(e => new { Type = e.Exception.GetType(), e.Exception.Message }))
                        {
                            _log.DebugFormat(group.First().Exception, "Provisioning: {0} retries on worker {1} because of {2}: {3}", group.Count(), CloudEnvironment.PartitionKey, group.Key.Type.Name, group.Key.Message);
                        }
                    }));
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }
}
