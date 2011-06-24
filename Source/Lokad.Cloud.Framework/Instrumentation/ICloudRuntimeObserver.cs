#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Instrumentation.Events;

namespace Lokad.Cloud.Instrumentation
{
    public interface ICloudRuntimeObserver
    {
        void Notify(ICloudRuntimeEvent @event);
    }
}