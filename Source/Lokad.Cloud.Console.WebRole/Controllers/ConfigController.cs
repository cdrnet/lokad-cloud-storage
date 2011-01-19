#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

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

        [HttpGet]
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
        public ActionResult Configuration(string hostedServiceName, ConfigModel model)
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

            return RedirectToAction("ByHostedService");
        }
    }
}
