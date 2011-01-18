using Lokad.Cloud.Management.Api10;

namespace Lokad.Cloud.Console.WebRole.Models.Services
{
    public class ServicesModel
    {
        public CloudServiceInfo[] QueueServices { get; set; }
        public CloudServiceInfo[] ScheduledServices { get; set; }
        public CloudServiceInfo[] CloudServices { get; set; }
        public CloudServiceInfo[] UnavailableServices { get; set; }
    }
}