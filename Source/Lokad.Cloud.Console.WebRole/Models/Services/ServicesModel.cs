using Lokad.Cloud.Management.Api10;
using Lokad.Cloud.Application;

namespace Lokad.Cloud.Console.WebRole.Models.Services
{
    public class ServicesModel
    {
        public QueueServiceModel[] QueueServices { get; set; }
        public CloudServiceInfo[] ScheduledServices { get; set; }
        public CloudServiceInfo[] CloudServices { get; set; }
        public CloudServiceInfo[] UnavailableServices { get; set; }
    }

    public class QueueServiceModel
    {
        public string ServiceName { get; set; }
        public bool IsStarted { get; set; }
        public QueueServiceDefinition Definition { get; set; }
    }
}