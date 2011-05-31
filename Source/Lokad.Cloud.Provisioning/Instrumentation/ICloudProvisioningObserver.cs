#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Provisioning.Instrumentation.Events;

namespace Lokad.Cloud.Provisioning.Instrumentation
{
    public interface ICloudProvisioningObserver
    {
        void Notify(ICloudProvisioningEvent @event);
    }
}