namespace Lokad.Cloud.Console.WebRole.Models.Queues
{
    public class QueuesModel
    {
        public AzureQueue[] Queues { get; set; }
    }

    public class AzureQueue
    {
        public string QueueName { get; set; }
        public int MessageCount { get; set; }
        public string Latency { get; set; }
    }
}