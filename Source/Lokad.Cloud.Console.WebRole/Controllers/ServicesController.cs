using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Services;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class ServicesController : TenantController
    {
        public ServicesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByDeployment(string deploymentName)
        {
            InitializeDeploymentTenant(deploymentName);

            var cloudServices = new CloudServices(Storage.BlobStorage);

            return View(new ServicesModel
                {
                    Services = cloudServices.GetServices().ToArray()
                });
        }

        [HttpPut, ActionName("Status")]
        public ActionResult StatusPut(string deploymentName, string id, bool isStarted)
        {
            InitializeDeploymentTenant(deploymentName);
            var cloudServices = new CloudServices(Storage.BlobStorage);

            // TODO: Validate id (security)

            if (isStarted)
            {
                cloudServices.EnableService(id);
            }
            else
            {
                cloudServices.DisableService(id);
            }

            return Json(new
                {
                    serviceName = id,
                    isStarted,
                });
        }
    }
}
