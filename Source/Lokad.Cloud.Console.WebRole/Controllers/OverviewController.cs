#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

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

        [HttpGet]
        public override ActionResult Index()
        {
            return View(new OverviewModel
                {
                    HostedServices = DiscoveryInfo.LokadCloudDeployments.ToArray()
                });
        }

        [HttpGet]
        public override ActionResult ByHostedService(string hostedServiceName)
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

            return RedirectToAction("ByHostedService");
        }
    }
}
