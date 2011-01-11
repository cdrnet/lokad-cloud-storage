using System.Linq;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Console.WebRole.Controllers.ObjectModel
{
    public abstract class TenantController : ApplicationController
    {
        protected LokadCloudHostedService HostedService { get; private set; }
        protected CloudStorageProviders Storage { get; private set; }

        protected TenantController(AzureDiscoveryInfo discoveryInfo) : base(discoveryInfo)
        {
        }

        protected void InitializeDeploymentTenant(string hostedServiceName)
        {
            CurrentHostedService = hostedServiceName;

            var services = DiscoveryInfo.LokadCloudDeployments;
            HostedService = services.Single(d => d.ServiceName == hostedServiceName);

            Storage = CloudStorage
                .ForAzureAccount(HostedService.StorageAccount)
                .WithDataSerializer(HostedService.DataSerializer)
                .BuildStorageProviders();
        }

        public abstract ActionResult ByHostedService(string hostedServiceName);

        public virtual ActionResult Index()
        {
            return RedirectToAction("ByHostedService", new { hostedServiceName = DiscoveryInfo.LokadCloudDeployments.First().ServiceName });
        }
    }
}