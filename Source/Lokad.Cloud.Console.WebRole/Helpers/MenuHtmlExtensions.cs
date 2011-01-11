using System.Web.Mvc;
using System.Web.Mvc.Html;

using Lokad.Cloud.Console.WebRole.Models.Shared;

namespace Lokad.Cloud.Console.WebRole.Helpers
{
    public static class MenuHtmlExtensions
    {
        public static MvcHtmlString NavigationMenuEntry(this HtmlHelper htmlHelper, NavigationModel navigationModel, string text, string controller)
        {
            if (controller == navigationModel.CurrentController || string.IsNullOrEmpty(navigationModel.CurrentHostedServiceName))
            {
                return BuildMenuEntry(
                    controller == navigationModel.CurrentController,
                    htmlHelper.MenuIndexLink(text, controller));
            }

            return BuildMenuEntry(false, htmlHelper.MenuByHostedServiceLink(text, controller, navigationModel.CurrentHostedServiceName));
        }

        public static MvcHtmlString DeploymentMenuEntry(this HtmlHelper htmlHelper, NavigationModel navigationModel, string text, string hostedServiceName)
        {
            return BuildMenuEntry(
                navigationModel.CurrentHostedServiceName == hostedServiceName,
                htmlHelper.MenuByHostedServiceLink(text, navigationModel.CurrentController, hostedServiceName));
        }

        private static MvcHtmlString BuildMenuEntry(bool isActive, MvcHtmlString linkHtml)
        {
            return MvcHtmlString.Create(string.Format("<li{0}>{1}</li>", isActive ? @" class=""active""" : string.Empty, linkHtml));
        }

        public static MvcHtmlString MenuIndexLink(this HtmlHelper htmlHelper, string text, string controller)
        {
            return htmlHelper.RouteLink(text, "MenuIndex", new { controller, action = "Index" });
        }

        public static MvcHtmlString MenuByHostedServiceLink(this HtmlHelper htmlHelper, string text, string controller, string hostedServiceName)
        {
            return htmlHelper.RouteLink(text, "MenuByHostedService", new { controller, hostedServiceName, action = "ByHostedService" });
        }
    }
}