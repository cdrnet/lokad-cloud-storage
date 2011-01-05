using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Overview;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class OverviewController : ApplicationController
    {
        public OverviewController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
            HideDiscovery(false);
        }

        public ActionResult Index()
        {
            return View(new OverviewModel
                {
                    HostedServices = DiscoveryInfo.LokadCloudDeployments.ToArray()
                });
        }
    }
}
