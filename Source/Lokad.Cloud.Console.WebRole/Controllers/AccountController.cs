#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    public sealed class AccountController : ApplicationController
    {
        private static readonly OpenIdRelyingParty OpenId = new OpenIdRelyingParty();

        public AccountController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
            ShowWaitingForDiscoveryInsteadOfContent = false;
            HideDiscovery(true);
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Authenticate(string returnUrl)
        {
            var response = OpenId.GetResponse();

            if (response == null)
            {
                // Stage 2: user submitting Identifier
                Identifier id;
                if (Identifier.TryParse(Request.Form["openid_identifier"], out id))
                {
                    try
                    {
                        OpenId.CreateRequest(Request.Form["openid_identifier"]).RedirectToProvider();
                    }
                    catch (ProtocolException)
                    {
                        ViewBag.Message = "No such endpoint can be found.";
                        return View("Login");
                    }
                }
                else
                {
                    ViewBag.Message = "Invalid identifier.";
                    return View("Login");
                }
            }
            else
            {
                // HACK: Filtering users based on their registrations
                if (!Users.IsAdministrator(response.ClaimedIdentifier))
                {
                    ViewBag.Message = "This user does not have access rights.";
                    return View("Login");
                }

                // Stage 3: OpenID Provider sending assertion response
                switch (response.Status)
                {
                    case AuthenticationStatus.Authenticated:
                        Session["FriendlyIdentifier"] = response.FriendlyIdentifierForDisplay;
                        FormsAuthentication.SetAuthCookie(response.ClaimedIdentifier, false);
                        if (!string.IsNullOrEmpty(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Overview");
                        }
                    case AuthenticationStatus.Canceled:
                        ViewBag.Message = "Canceled at provider";
                        return View("Login");
                    case AuthenticationStatus.Failed:
                        ViewBag.Message = response.Exception.Message;
                        return View("Login");
                }
            }
            return new EmptyResult();
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Overview");
        }
    }
}
