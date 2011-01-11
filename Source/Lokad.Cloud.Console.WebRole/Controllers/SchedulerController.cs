using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Scheduler;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class SchedulerController : TenantController
    {
        public SchedulerController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);

            var cloudServiceScheduling = new CloudServiceScheduling(Storage.BlobStorage);

            return View(new SchedulerModel
                {
                    Schedules = cloudServiceScheduling.GetSchedules().ToArray()
                });
        }
    }
}
