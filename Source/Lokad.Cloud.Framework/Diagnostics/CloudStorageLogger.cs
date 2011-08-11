#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
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

            _subscriptions.Add(_observable.OfType<BlobDeserializationFailedEvent>().Subscribe(e => TryLog(e, e.Exception)));
            _subscriptions.Add(_observable.OfType<MessageDeserializationFailedQuarantinedEvent>().Subscribe(e => TryLog(e, e.Exceptions)));
            _subscriptions.Add(_observable.OfType<MessageProcessingFailedQuarantinedEvent>().Subscribe(e => TryLog(e)));

            _subscriptions.Add(_observable.OfType<StorageOperationRetriedEvent>()
                .ThrottleTokenBucket(TimeSpan.FromMinutes(15), 2)
                .Subscribe(@event =>
                    {
                        var e = @event.Item;
                        TryLog(string.Format("Storage: Retried on policy {0}{1} on {2}.{3}",
                            e.Policy,
                            e.Exception != null ? " because of " + e.Exception.GetType().Name : string.Empty,
                            CloudEnvironment.PartitionKey,
                            @event.DroppedItems > 0 ? string.Format(" There have been {0} similar events in the last 15 minutes.", @event.DroppedItems) : string.Empty),
                            e.Exception, LogLevel.Debug);
                    }));
        }

        void TryLog(object message, Exception exception = null, LogLevel level = LogLevel.Warn)
        {
            try
            {
                if (exception != null)
                {
                    _log.Log(level, exception, message);
                }
                else
                {
                    _log.Log(level, message);
                }
            }
            catch (Exception)
            {
                // If logging fails, ignore (we can't report)
            }
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
