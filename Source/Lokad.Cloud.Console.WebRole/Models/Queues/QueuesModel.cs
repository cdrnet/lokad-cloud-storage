#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Application;

namespace Lokad.Cloud.Console.WebRole.Models.Queues
{
    public class QueuesModel
    {
        public AzureQueue[] Queues { get; set; }
        public AzureQuarantineQueue[] Quarantine { get; set; }
        public bool HasQuarantinedMessages { get; set; }
    }

    public class AzureQueue
    {
        public string QueueName { get; set; }
        public int MessageCount { get; set; }
        public string Latency { get; set; }
        public QueueServiceDefinition[] Services { get; set; }
    }

    public class AzureQuarantineQueue
    {
        public string QueueName { get; set; }
        public AzureQuarantineMessage[] Messages { get; set; }
    }

    public class AzureQuarantineMessage
    {
        public string Inserted { get; set; }
        public string Persisted { get; set; }
        public string Reason { get; set; }
        public string Content { get; set; }
        public string Key { get; set; }
        public bool HasData { get; set; }
        public bool HasXml { get; set; }
    }
}