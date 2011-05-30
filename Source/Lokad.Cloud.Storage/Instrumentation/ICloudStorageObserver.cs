#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage.Instrumentation.Events;

namespace Lokad.Cloud.Storage.Instrumentation
{
    public interface ICloudStorageObserver
    {
        void Notify(ICloudStorageEvent @event);
    }
}