using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;

namespace Lokad.Cloud.Console.WebRole
{
    public class MvcApplication : HttpApplication
    {
        private CancellationTokenSource _discoveryCancellation;

        private static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        private static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Content/{*pathInfo}");

            routes.MapRoute("Overview", "", new { controller = "Overview", action = "Index" });
            routes.MapRoute("Account", "Account/{action}/{id}", new { controller = "Account", action = "Index", id = UrlParameter.Optional });
            routes.MapRoute("Discovery", "Discovery/{action}/{id}", new { controller = "Discovery", action = "Index", id = UrlParameter.Optional });

            routes.MapRoute("ByHostedService", "{controller}/{hostedServiceName}/{action}/{id}", new { action = "ByHostedService", id = UrlParameter.Optional });
            routes.MapRoute("Default", "{controller}/{action}/{id}", new { controller = "Overview", action = "Index", id = UrlParameter.Optional });

            routes.MapRoute("MenuIndex", "{controller}/{action}", new { controller = "Overview", action = "Index" });
            routes.MapRoute("MenuByHostedService", "{controller}/{hostedServiceName}/{action}", new { controller = "Overview", action = "ByHostedService" });
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            _discoveryCancellation = new CancellationTokenSource();
            var discovery = (AzureDiscoveryProvider)DependencyResolver.Current.GetService(typeof(AzureDiscoveryProvider));
            discovery.StartAutomaticCacheUpdate(_discoveryCancellation.Token);
        }

        protected void Application_End()
        {
            if (_discoveryCancellation != null)
            {
                _discoveryCancellation.Cancel();
                _discoveryCancellation = null;
            }
        }
    }
}