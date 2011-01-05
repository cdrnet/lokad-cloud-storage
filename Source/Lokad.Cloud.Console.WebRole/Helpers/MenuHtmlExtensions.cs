using System.Web.Mvc;
using System.Web.Mvc.Html;
using Lokad.Cloud.Console.WebRole.Models.Shared;

namespace Lokad.Cloud.Console.WebRole.Helpers
{
    public static class MenuHtmlExtensions
    {
        public static MvcHtmlString MenuNavigationListEntry(this HtmlHelper htmlHelper, NavigationModel navigationModel, string text, string controller, string action = null)
        {
            var actionName = action ?? navigationModel.ControllerAction;
            return MvcHtmlString.Create(string.Format("<li{0}>{1}</li>",
                navigationModel.CurrentController == controller ? @" class=""active""" : string.Empty,
                htmlHelper.ActionLink(text, actionName, controller, actionName == "ByDeployment" ? new { deploymentName = navigationModel.CurrentDeploymentName } : null, null)));
        }

        public static MvcHtmlString MenuDeploymentListEntry(this HtmlHelper htmlHelper, NavigationModel navigationModel, string text, string deploymentName)
        {
            return MvcHtmlString.Create(string.Format("<li{0}>{1}</li>",
                navigationModel.CurrentDeploymentName == deploymentName ? @" class=""active""" : string.Empty,
                htmlHelper.ActionLink(text, "ByDeployment", navigationModel.CurrentController, new { deploymentName }, null)));
        }
    }
}