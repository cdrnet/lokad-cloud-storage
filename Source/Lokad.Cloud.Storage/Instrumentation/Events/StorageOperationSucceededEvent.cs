#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a storage operation has succeeded.
    /// Useful for collecting usage statistics.
    /// </summary>
    public class StorageOperationSucceededEvent : ICloudStorageEvent
    {
        public StorageOperationType OperationType { get; private set; }
        public TimeSpan Duration { get; private set; }

        public StorageOperationSucceededEvent(StorageOperationType operationType, TimeSpan duration)
        {
            OperationType = operationType;
            Duration = duration;
        }
    }
}
