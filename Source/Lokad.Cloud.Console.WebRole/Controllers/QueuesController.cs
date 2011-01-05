using System.Linq;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Helpers;
using Lokad.Cloud.Console.WebRole.Models.Queues;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class QueuesController : TenantController
    {
        public QueuesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByDeployment(string deploymentName)
        {
            InitializeDeploymentTenant(deploymentName);

            var queueStorage = Storage.QueueStorage;

            return View(new QueuesModel
                {
                    Queues = queueStorage.List(null).Select(queueName => new AzureQueue
                        {
                            QueueName = queueName,
                            MessageCount = queueStorage.GetApproximateCount(queueName),
                            Latency = queueStorage.GetApproximateLatency(queueName).Convert(ts => ts.PrettyFormat(), string.Empty)
                        }).ToArray()
                });
        }
    }
}
