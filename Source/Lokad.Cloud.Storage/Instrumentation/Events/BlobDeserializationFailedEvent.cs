#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a blob is ignored because it could not be deserialized.
    /// Useful to monitor for serialization and data transport errors, alarm when it happens to often.
    /// </summary>
    public class BlobDeserializationFailedEvent : ICloudStorageEvent
    {
        // TODO (ruegg, 2011-05-27): Drop properties that we don't actually need in practice

        public Exception Exception { get; private set; }
        public string ContainerName { get; private set; }
        public string BlobName { get; private set; }

        public BlobDeserializationFailedEvent(Exception exception, string containerName, string blobName)
        {
            Exception = exception;
            ContainerName = containerName;
            BlobName = blobName;
        }

        public override string ToString()
        {
            return string.Format("Storage: A blob was retrieved but failed to deserialize. Blob {0} in container {1}. Reason: {2}",
                BlobName, ContainerName, Exception != null ? Exception.Message : "unknown");
        }
    }
}
