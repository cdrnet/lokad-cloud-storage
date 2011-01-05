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

        protected void InitializeDeploymentTenant(string deploymentName)
        {
            CurrentDeployment = deploymentName;

            var services = DiscoveryInfo.LokadCloudDeployments;
            HostedService = services.Single(d => d.Name == deploymentName);

            Storage = CloudStorage
                .ForAzureAccount(HostedService.StorageAccount)
                .WithDataSerializer(HostedService.DataSerializer)
                .BuildStorageProviders();
        }

        public abstract ActionResult ByDeployment(string deploymentName);

        public virtual ActionResult Index()
        {
            return RedirectToAction("ByDeployment", new { deploymentName = DiscoveryInfo.LokadCloudDeployments.First().Name });
        }
    }
}