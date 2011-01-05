using System;
using System.Linq;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Helpers;
using Lokad.Cloud.Console.WebRole.Models.Shared;

namespace Lokad.Cloud.Console.WebRole.Controllers.ObjectModel
{
    public abstract class ApplicationController : Controller
    {
        private readonly NavigationModel _navigation;
        private readonly DiscoveryModel _discovery;

        protected AzureDiscoveryInfo DiscoveryInfo { get; private set; }

        protected ApplicationController(AzureDiscoveryInfo discoveryInfo)
        {
            DiscoveryInfo = discoveryInfo;

            ViewBag.Discovery = _discovery = new DiscoveryModel
                {
                    IsAvailable = discoveryInfo.IsAvailable,
                    ShowLastDiscoveryUpdate = discoveryInfo.IsAvailable,
                    LastDiscoveryUpdate = (DateTimeOffset.UtcNow - discoveryInfo.Timestamp).PrettyFormat()
                };

            var controllerName = GetType().Name;
            ViewBag.Navigation = _navigation = new NavigationModel
                {
                    ShowDeploymentSelector = discoveryInfo.IsAvailable,
                    DeploymentNames = discoveryInfo.LokadCloudDeployments.Select(d => d.Name).ToArray(),
                    CurrentController = controllerName.Substring(0, controllerName.Length - 10),
                    ControllerAction = discoveryInfo.IsAvailable ? "ByDeployment" : "Index"
                };
        }

        protected bool ShowWaitingForDiscoveryInsteadOfContent
        {
            get { return !_discovery.IsAvailable; }
            set { _discovery.IsAvailable = !value; }
        }

        protected string CurrentDeployment
        {
            get { return _navigation.CurrentDeploymentName; }
            set { _navigation.CurrentDeploymentName = value; }
        }

        protected void HideDiscovery(bool hideDiscoveryUpdate)
        {
            _navigation.ShowDeploymentSelector = false;
            _navigation.ControllerAction = "Index";

            if (hideDiscoveryUpdate)
            {
                _discovery.ShowLastDiscoveryUpdate = false;
            }
        }
    }
}