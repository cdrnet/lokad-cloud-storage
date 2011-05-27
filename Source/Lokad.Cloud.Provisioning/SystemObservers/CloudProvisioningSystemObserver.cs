#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Provisioning.SystemEvents;

namespace Lokad.Cloud.Provisioning.SystemObservers
{
    public class CloudProvisioningSystemObserver : IDisposable, ICloudProvisioningSystemObserver
    {
        readonly IObserver<ICloudProvisioningEvent>[] _observers;

        public CloudProvisioningSystemObserver(IObserver<ICloudProvisioningEvent>[] observers)
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
