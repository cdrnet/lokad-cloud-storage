#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.SystemEvents
{
    /// <summary>
    /// Raised whenever a message is quarantined because it failed to be processed multiple times.
    /// </summary>
    public class MessageQuarantinedAfterRetrialsEvent : ICloudStorageEvent
    {
        // TODO (ruegg, 2011-05-27): Drop properties that we don't actually need in practice

        public string QueueName { get; private set; }
        public string QuarantineStoreName { get; private set; }
        public Type MessageType { get; private set; }
        public CloudQueueMessage RawMessage { get; private set; }
        public byte[] Data { get; private set; }

        public MessageQuarantinedAfterRetrialsEvent(string queueName, string storeName, Type messageType, CloudQueueMessage rawMessage, byte[] data)
        {
            QueueName = queueName;
            QuarantineStoreName = storeName;
            MessageType = messageType;
            RawMessage = rawMessage;
            Data = data;
        }

        public override string ToString()
        {
            return string.Format("Storage: A message of type {0} in queue {1} failed to process repeatedly and has been quarantined.",
                MessageType.Name, QueueName);
        }
    }
}
