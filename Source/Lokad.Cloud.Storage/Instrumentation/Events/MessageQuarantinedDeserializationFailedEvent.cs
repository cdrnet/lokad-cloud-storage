#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a message is quarantined because it could not be deserialized.
    /// Useful to monitor for serialization and data transport errors, alarm when it happens to often.
    /// </summary>
    public class MessageQuarantinedDeserializationFailedEvent : ICloudStorageEvent
    {
        // TODO (ruegg, 2011-05-27): Drop properties that we don't actually need in practice

        public AggregateException Exceptions { get; private set; }
        public string QueueName { get; private set; }
        public string QuarantineStoreName { get; private set; }
        public Type MessageType { get; private set; }
        public CloudQueueMessage RawMessage { get; private set; }
        public byte[] Data { get; private set; }

        public MessageQuarantinedDeserializationFailedEvent(AggregateException exceptions, string queueName, string storeName, Type messageType, CloudQueueMessage rawMessage, byte[] data)
        {
            Exceptions = exceptions;
            QueueName = queueName;
            QuarantineStoreName = storeName;
            MessageType = messageType;
            RawMessage = rawMessage;
            Data = data;
        }

        public override string ToString()
        {
            return string.Format("Storage: A message in queue {0} failed to deserialize to type {1} and has been quarantined.",
                QueueName, MessageType.Name);
        }
    }
}
