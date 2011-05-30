#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Provisioning.Instrumentation.Events;

namespace Lokad.Cloud.Provisioning.Instrumentation
{
    public class CloudProvisioningObserver : IDisposable, ICloudProvisioningObserver
    {
        readonly IObserver<ICloudProvisioningEvent>[] _observers;

        public CloudProvisioningObserver(IObserver<ICloudProvisioningEvent>[] observers)
        {
            _observers = observers;
        }

        public void Notify(ICloudProvisioningEvent @event)
        {
            // NOTE: Assuming event observers are light - else we may want to do this async
            foreach (var observer in _observers)
            {
                observer.OnNext(@event);
            }
        }

        public void Dispose()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
        }
    }
}
