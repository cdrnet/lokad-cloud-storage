#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Lokad.Cloud.Storage.Instrumentation.Events;
using Lokad.Cloud.Storage.Shared.Logging;

namespace Lokad.Cloud.Diagnostics
{
    // TODO (ruegg, 2011-05-30): Temporary class to maintain logging via system events for now -> rework

    internal class CloudStorageLogger : Autofac.IStartable, IDisposable
    {
        private readonly IObservable<ICloudStorageEvent> _observable;
        private readonly ILog _log;
        private readonly List<IDisposable> _subscriptions;

        public CloudStorageLogger(IObservable<ICloudStorageEvent> observable, ILog log)
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

            _subscriptions.Add(_observable.OfType<BlobDeserializationFailedEvent>().Subscribe(e => _log.Warn(e.Exception, e)));
            _subscriptions.Add(_observable.OfType<MessageDeserializationFailedQuarantinedEvent>().Subscribe(e => _log.Warn(e.Exceptions, e)));
            _subscriptions.Add(_observable.OfType<MessageProcessingFailedQuarantinedEvent>().Subscribe(e => _log.Warn(e)));

            _subscriptions.Add(_observable.OfType<StorageOperationRetriedEvent>()
                .Buffer(TimeSpan.FromMinutes(5))
                .Subscribe(events =>
                    {
                        foreach (var group in events.GroupBy(e => new { Type = e.Exception.GetType(), e.Exception.Message }))
                        {
                            _log.DebugFormat(group.First().Exception, "Storage: {0} retries because of {1}: {2}", group.Count(), group.Key.Type.Name, group.Key.Message);
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
