using System.Linq;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Services;
using Lokad.Cloud.Management;
using Lokad.Cloud.Application;
using Lokad.Cloud.Management.Api10;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class ServicesController : TenantController
    {
        public ServicesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var serviceManager = new CloudServices(Storage.BlobStorage);
            var services = serviceManager.GetServices();

            var inspector = new CloudApplicationInspector(Storage.BlobStorage);
            var appDefinition = inspector.Inspect();

            if(!appDefinition.HasValue)
            {
                return View(new ServicesModel
                    {
                        QueueServices = new QueueServiceModel[0],
                        ScheduledServices = new CloudServiceInfo[0],
                        CloudServices = new CloudServiceInfo[0],
                        UnavailableServices = new CloudServiceInfo[0]
                    });
            }

            var queueServices = services.Join(appDefinition.Value.QueueServices, s => s.ServiceName, d => d.TypeName, (s, d) => new QueueServiceModel
                {
                    ServiceName = s.ServiceName,
                    IsStarted = s.IsStarted,
                    Definition = d
                }).ToArray();

            var scheduledServices = services.Where(s => appDefinition.Value.ScheduledServices.Exists(ads => ads.TypeName.StartsWith(s.ServiceName))).ToArray();
            var otherServices = services.Where(s => appDefinition.Value.CloudServices.Exists(ads => ads.TypeName.StartsWith(s.ServiceName))).ToArray();
            var unavailableServices = services
                .Where(s => !queueServices.Exists(d => d.ServiceName == s.ServiceName))
                .Except(scheduledServices).Except(otherServices).ToArray();

            return View(new ServicesModel
                {
                    QueueServices = queueServices,
                    ScheduledServices = scheduledServices,
                    CloudServices = otherServices,
                    UnavailableServices = unavailableServices
                });
        }

        [HttpPut]
        public ActionResult JsonStatus(string hostedServiceName, string id, bool isStarted)
        {
            InitializeDeploymentTenant(hostedServiceName);
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
