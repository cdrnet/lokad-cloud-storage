#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

using Lokad.Cloud.Application;
using Lokad.Cloud.Console.WebRole.Helpers;
using Lokad.Cloud.Console.WebRole.Models.Queues;
using Lokad.Cloud.Console.WebRole.Models.Services;
using Lokad.Cloud.Management;
using Lokad.Cloud.Management.Api10;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Console.WebRole.Framework.Services
{
    public class AppDefinitionWithLiveDataProvider
    {
        const string FailingMessagesStoreName = "failing-messages";
        private readonly RuntimeProviders _runtimeProviders;

        public AppDefinitionWithLiveDataProvider(RuntimeProviders runtimeProviders)
        {
            _runtimeProviders = runtimeProviders;
        }

        public ServicesModel QueryServices()
        {
            var serviceManager = new CloudServices(_runtimeProviders);
            var services = serviceManager.GetServices();

            var inspector = new CloudApplicationInspector(_runtimeProviders);
            var applicationDefinition = inspector.Inspect();

            if (!applicationDefinition.HasValue)
            {
                return new ServicesModel
                    {
                        QueueServices = new QueueServiceModel[0],
                        ScheduledServices = new CloudServiceInfo[0],
                        CloudServices = new CloudServiceInfo[0],
                        UnavailableServices = new CloudServiceInfo[0]
                    };
            }

            var appDefinition = applicationDefinition.Value;

            var queueServices = services.Join(
                appDefinition.QueueServices,
                s => s.ServiceName,
                d => d.TypeName,
                (s, d) => new QueueServiceModel { ServiceName = s.ServiceName, IsStarted = s.IsStarted, Definition = d }).ToArray();

            var scheduledServices = services.Where(s => appDefinition.ScheduledServices.Exists(ads => ads.TypeName.StartsWith(s.ServiceName))).ToArray();
            var otherServices = services.Where(s => appDefinition.CloudServices.Exists(ads => ads.TypeName.StartsWith(s.ServiceName))).ToArray();
            var unavailableServices = services
                .Where(s => !queueServices.Exists(d => d.ServiceName == s.ServiceName))
                .Except(scheduledServices).Except(otherServices).ToArray();

            return new ServicesModel
                {
                    QueueServices = queueServices,
                    ScheduledServices = scheduledServices,
                    CloudServices = otherServices,
                    UnavailableServices = unavailableServices
                };
        }

        public QueuesModel QueryQueues()
        {
            var queueStorage = _runtimeProviders.QueueStorage;
            var inspector = new CloudApplicationInspector(_runtimeProviders);
            var applicationDefinition = inspector.Inspect();

            var failingMessages = queueStorage.ListPersisted(FailingMessagesStoreName)
                .Select(key => queueStorage.GetPersisted(FailingMessagesStoreName, key))
                .Where(m => m.HasValue)
                .Select(m => m.Value)
                .OrderByDescending(m => m.PersistenceTime)
                .Take(50)
                .ToList();

            return new QueuesModel
                {
                    Queues = queueStorage.List(null).Select(queueName => new AzureQueue
                    {
                        QueueName = queueName,
                        MessageCount = queueStorage.GetApproximateCount(queueName),
                        Latency = queueStorage.GetApproximateLatency(queueName).Convert(ts => ts.PrettyFormat(), string.Empty),
                        Services = applicationDefinition.Convert(cd => cd.QueueServices.Where(d => d.QueueName == queueName).ToArray(), new QueueServiceDefinition[0])
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
                                    Reason = HttpUtility.HtmlEncode(m.Reason),
                                    Content = FormatQuarantinedLogEntryXmlContent(m),
                                    Key = m.Key,
                                    HasData = m.IsDataAvailable,
                                    HasXml = m.DataXml.HasValue
                                })
                                .ToArray()
                        })
                        .ToArray()
                };
        }

        static string FormatQuarantinedLogEntryXmlContent(PersistedMessage message)
        {
            if (!message.IsDataAvailable || !message.DataXml.HasValue)
            {
                return string.Empty;
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
    }
}