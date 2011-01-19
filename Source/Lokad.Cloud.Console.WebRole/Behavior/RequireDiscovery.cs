#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Web;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Ninject.Modules;
using Ninject.Web.Mvc.FilterBindingSyntax;

namespace Lokad.Cloud.Console.WebRole.Behavior
{
    public sealed class RequireDiscoveryFilter : IActionFilter
    {
        private readonly AzureDiscoveryInfo _discoveryInfo;

        public RequireDiscoveryFilter(AzureDiscoveryInfo discoveryInfo)
        {
            _discoveryInfo = discoveryInfo;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (_discoveryInfo.IsAvailable)
            {
                return;
            }

            // do not use 'filterContext.RequestContext.HttpContext.Request.Url' because of Azure port forwarding
            // http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/9142db8d-0f85-47a2-91f7-418bb5a0c675/

            var scheme = filterContext.RequestContext.HttpContext.Request.Url.Scheme;
            var host = filterContext.RequestContext.HttpContext.Request.Headers["Host"];
            var path = filterContext.RequestContext.HttpContext.Request.RawUrl;
            var returnUrl = scheme + @"://" + host + path;

            filterContext.Result = new RedirectResult("~/Discovery/Index?returnUrl=" + HttpUtility.UrlEncode(returnUrl));
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireDiscoveryAttribute : Attribute { }

    public sealed class RequireDiscoveryModule : NinjectModule
    {
        public override void Load()
        {
            this.BindFilter<RequireDiscoveryFilter>(FilterScope.Controller, 0).WhenControllerHas<RequireDiscoveryAttribute>();
        }
    }
}