using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Overview;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class OverviewController : TenantController
    {
        private readonly AzureUpdater _updater;

        public OverviewController(AzureDiscoveryInfo discoveryInfo, AzureUpdater updater)
            : base(discoveryInfo)
        {
            _updater = updater;
        }

        public override ActionResult Index()
        {
            return View(new OverviewModel
                {
                    HostedServices = DiscoveryInfo.LokadCloudDeployments.ToArray()
                });
        }

        public override ActionResult ByDeployment(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);

            return View(new DeploymentModel
                {
                    HostedService = HostedService
                });
        }

        [HttpPost]
        public ActionResult InstanceCount(string hostedServiceName, string slot, int instanceCount)
        {
            InitializeDeploymentTenant(hostedServiceName);

            _updater.UpdateInstanceCountAsync(hostedServiceName, slot, instanceCount).Wait();

            return RedirectToAction("ByDeployment");
        }
    }
}
