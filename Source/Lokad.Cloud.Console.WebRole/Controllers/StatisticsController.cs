using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Statistics;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class StatisticsController : TenantController
    {
        public StatisticsController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByDeployment(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);

            var cloudServices = new CloudServices(Storage.BlobStorage);

            return View(new StatisticsModel
                {
                    Services = cloudServices.GetServices().ToArray()
                });
        }
    }
}
