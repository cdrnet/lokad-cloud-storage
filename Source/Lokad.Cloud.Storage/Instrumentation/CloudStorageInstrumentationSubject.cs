#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Storage.Instrumentation
{
    /// <summary>
    /// Cloud storage observer that implements a hot Rx Observable, forwarding all events synchronously
    /// (similar to Rx's FastSubject). Use this class if you want an easy way to observe Lokad.Cloud.Storage
    /// using Rx. Alternatively you can implement your own storage observer instead, or not use any observers at all.
    /// </summary>
    public class CloudStorageInstrumentationSubject : ICloudStorageObserver, IObservable<ICloudStorageEvent>, IDisposable
    {
        readonly IObserver<ICloudStorageEvent>[] _fixedObservers;
        readonly List<IObserver<ICloudStorageEvent>> _observers;

        /// <param name="fixedObservers">Optional externally managed fixed observers, will neither be completed nor disposed by this class.</param>
        public CloudStorageInstrumentationSubject(IObserver<ICloudStorageEvent>[] fixedObservers = null)
        {
            _fixedObservers = fixedObservers ?? new IObserver<ICloudStorageEvent>[0];
            _observers = new List<IObserver<ICloudStorageEvent>>();
        }

        void ICloudStorageObserver.Notify(ICloudStorageEvent @event)
        {
            IObserver<ICloudStorageEvent>[] observers;
            lock (_observers)
            {
                observers = _observers.ToArray();
            }

            // Assuming event observers are light - else we may want to do this async
            foreach (var observer in _fixedObservers)
            {
                observer.OnNext(@event);
            }
            foreach (var observer in observers)
            {
                observer.OnNext(@event);
            }
        }

        public void Dispose()
        {
            IObserver<ICloudStorageEvent>[] observers;
            lock (_observers)
            {
                observers = _observers.ToArray();
                _observers.Clear();
            }

            foreach (var observer in observers)
            {
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<ICloudStorageEvent> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }

            lock (_observers)
            {
                _observers.Add(observer);
                return new Subscription(this, observer);
            }
        }

        private class Subscription : IDisposable
        {
            private readonly CloudStorageInstrumentationSubject _subject;
            private IObserver<ICloudStorageEvent> _observer;

            public Subscription(CloudStorageInstrumentationSubject subject, IObserver<ICloudStorageEvent> observer)
            {
                _subject = subject;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null)
                {
                    lock (_subject._observers)
                    {
// ReSharper disable ConditionIsAlwaysTrueOrFalse
                        if (_observer != null)
// ReSharper restore ConditionIsAlwaysTrueOrFalse
                        {
                            _subject._observers.Remove(_observer);
                            _observer = null;
                        }
                    }
                }
            }
        }
    }
}
