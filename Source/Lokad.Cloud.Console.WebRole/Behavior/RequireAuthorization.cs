using System.Web;
using System.Web.Mvc;

namespace Lokad.Cloud.Console.WebRole.Behavior
{
    public sealed class RequireAuthorizationAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // do not use 'filterContext.RequestContext.HttpContext.Request.Url' because of Azure port forwarding
            // http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/9142db8d-0f85-47a2-91f7-418bb5a0c675/

            var request = filterContext.RequestContext.HttpContext.Request;
            var returnUrl = request.Url.Scheme + @"://" + request.Headers["Host"] + request.RawUrl;
            filterContext.Result = new RedirectResult("~/Account/Login?returnUrl=" + HttpUtility.UrlEncode(returnUrl));
        }
    }
}