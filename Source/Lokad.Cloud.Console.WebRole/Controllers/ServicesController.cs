#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Framework.Services;
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

        [HttpGet]
        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var provider = new AppDefinitionWithLiveDataProvider(Storage);
            return View(provider.QueryServices());
        }

        [HttpPut]
        public ActionResult Status(string hostedServiceName, string id, bool isStarted)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudServices = new CloudServices(Storage.BlobStorage);

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
