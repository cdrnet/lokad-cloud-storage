using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Config;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class ConfigController : TenantController
    {
        public ConfigController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);

            var cloudConfiguration = new Management.CloudConfiguration(Storage.BlobStorage);

            return View(new ConfigModel
                {
                    Configuration = cloudConfiguration.GetConfigurationString()
                });
        }

        [HttpPost]
        [ValidateInput(false)] // we're expecting xml
        public ActionResult ByHostedService(string hostedServiceName, ConfigModel model)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudConfiguration = new Management.CloudConfiguration(Storage.BlobStorage);

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.Configuration))
                {
                    cloudConfiguration.RemoveConfiguration();
                }
                else
                {
                    cloudConfiguration.SetConfiguration(model.Configuration.Trim());
                }
            }

            return View(model);
        }
    }
}
