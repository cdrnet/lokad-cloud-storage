using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization]
    public sealed class DiscoveryController : ApplicationController
    {
        public DiscoveryController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public ActionResult Index(string returnUrl)
        {
            if (DiscoveryInfo.IsAvailable)
            {
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Index", "Overview");
            }

            ShowWaitingForDiscoveryInsteadOfContent = true;
            HideDiscovery();

            return View();
        }

        [HttpGet]
        public ActionResult Status()
        {
            return Json(new
                {
                    isAvailable = DiscoveryInfo.IsAvailable
                });
        }
    }
}
