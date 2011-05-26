#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage.Events;

namespace Lokad.Cloud.Storage.SystemObservers
{
    public interface ICloudStorageSystemObserver
    {
        void Notify(ICloudStorageEvent @event);
    }
}