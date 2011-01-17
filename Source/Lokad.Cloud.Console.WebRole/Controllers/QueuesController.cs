using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Helpers;
using Lokad.Cloud.Console.WebRole.Models.Queues;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class QueuesController : TenantController
    {
        const string FailingMessagesStoreName = "failing-messages";
        const string DataNotAvailableMessage = "Raw data was lost, message not restoreable. Maybe the queue was deleted in the meantime.";
        const string XmlNotAvailableMegssage = "XML representation not available, but message is restoreable.";

        public QueuesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var queueStorage = Storage.QueueStorage;

            var failingMessages = queueStorage.ListPersisted(FailingMessagesStoreName)
                .Select(key => queueStorage.GetPersisted(FailingMessagesStoreName, key))
                .Where(m => m.HasValue)
                .Select(m => m.Value)
                .OrderByDescending(m => m.PersistenceTime)
                .Take(50)
                .ToList();

            return View(new QueuesModel
                {
                    Queues = queueStorage.List(null).Select(queueName => new AzureQueue
                        {
                            QueueName = queueName,
                            MessageCount = queueStorage.GetApproximateCount(queueName),
                            Latency = queueStorage.GetApproximateLatency(queueName).Convert(ts => ts.PrettyFormat(), string.Empty)
                        }).ToArray(),

                    HasQuarantinedMessages = failingMessages.Count > 0,

                    Quarantine = failingMessages
                        .GroupBy(m => m.QueueName)
                        .Select(group => new AzureQuarantineQueue
                            {
                                QueueName = group.Key,
                                Messages = group.OrderByDescending(m => m.PersistenceTime)
                                    .Select(m => new AzureQuarantineMessage
                                        {
                                            Inserted = FormatUtil.TimeOffsetUtc(m.InsertionTime.UtcDateTime),
                                            Persisted = FormatUtil.TimeOffsetUtc(m.PersistenceTime.UtcDateTime),
                                            Reason = FormatReason(m),
                                            Content = FormatContent(m),
                                            Key = m.Key
                                        })
                                    .ToArray()
                            })
                        .ToArray()
                });
        }

        [HttpDelete]
        public EmptyResult JsonQueue(string hostedServiceName, string id)
        {
            InitializeDeploymentTenant(hostedServiceName);
            Storage.QueueStorage.DeleteQueue(id);
            return null;
        }

        [HttpDelete]
        public EmptyResult JsonQuarantinedMessage(string hostedServiceName, string id)
        {
            InitializeDeploymentTenant(hostedServiceName);
            Storage.QueueStorage.DeletePersisted(FailingMessagesStoreName, id);
            return null;
        }

        [HttpPost]
        public EmptyResult JsonRestoreQuarantinedMessage(string hostedServiceName, string id)
        {
            InitializeDeploymentTenant(hostedServiceName);
            Storage.QueueStorage.RestorePersisted(FailingMessagesStoreName, id);
            return null;
        }

        static string FormatContent(PersistedMessage message)
        {
            if (!message.IsDataAvailable)
            {
                return DataNotAvailableMessage;
            }

            if (!message.DataXml.HasValue)
            {
                return XmlNotAvailableMegssage;
            }

            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = true
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                message.DataXml.Value.WriteTo(writer);
                writer.Flush();
            }

            return HttpUtility.HtmlEncode(sb.ToString());
        }

        static string FormatReason(PersistedMessage message)
        {
            if (String.IsNullOrEmpty(message.Reason))
            {
                return "Reason unknown";
            }

            return HttpUtility.HtmlEncode(message.Reason);
        }
    }
}
