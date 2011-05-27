#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Provisioning.SystemEvents;

namespace Lokad.Cloud.Provisioning.SystemObservers
{
    public interface ICloudProvisioningSystemObserver
    {
        void Notify(ICloudProvisioningEvent @event);
    }
}