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
                    LastDiscoveryUpdate = (DateTimeOffset.UtcNow - discoveryInfo.Timestamp).PrettyFormat() + " (" + (discoveryInfo.FinishedTimestamp - discoveryInfo.Timestamp).TotalSeconds.Round(1) + "s)"
                };

            var controllerName = GetType().Name;
            ViewBag.Navigation = _navigation = new NavigationModel
                {
                    ShowDeploymentSelector = discoveryInfo.IsAvailable,
                    HostedServiceNames = discoveryInfo.LokadCloudDeployments.Select(d => d.ServiceName).ToArray(),
                    CurrentController = controllerName.Substring(0, controllerName.Length - 10),
                    ControllerAction = discoveryInfo.IsAvailable ? "ByHostedService" : "Index"
                };
        }

        protected bool ShowWaitingForDiscoveryInsteadOfContent
        {
            get { return !_discovery.IsAvailable; }
            set { _discovery.IsAvailable = !value; }
        }

        protected string CurrentHostedService
        {
            get { return _navigation.CurrentHostedServiceName; }
            set
            {
                _navigation.CurrentHostedServiceName = value;
                ViewBag.TenantPath = String.Format("/{0}/{1}", _navigation.CurrentController, value);
            }
        }

        protected void HideDiscovery()
        {
            _navigation.ShowDeploymentSelector = false;
            _navigation.ControllerAction = "Index";
            _discovery.ShowLastDiscoveryUpdate = false;
        }
    }
}