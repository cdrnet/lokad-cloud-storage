#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Web.Mvc;

using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Framework.Services;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class QueuesController : TenantController
    {
        const string FailingMessagesStoreName = "failing-messages";

        public QueuesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        [HttpGet]
        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var provider = new AppDefinitionWithLiveDataProvider(Storage);
            return View(provider.QueryQueues());
        }

        [HttpDelete]
        public EmptyResult Queue(string hostedServiceName, string id)
        {
            InitializeDeploymentTenant(hostedServiceName);
            Storage.QueueStorage.DeleteQueue(id);
            return null;
        }

        [HttpDelete]
        public EmptyResult QuarantinedMessage(string hostedServiceName, string id)
        {
            InitializeDeploymentTenant(hostedServiceName);
            Storage.QueueStorage.DeletePersisted(FailingMessagesStoreName, id);
            return null;
        }

        [HttpPost]
        public EmptyResult RestoreQuarantinedMessage(string hostedServiceName, string id)
        {
            InitializeDeploymentTenant(hostedServiceName);
            Storage.QueueStorage.RestorePersisted(FailingMessagesStoreName, id);
            return null;
        }
    }
}
