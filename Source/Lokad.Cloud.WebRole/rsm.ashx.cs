#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Diagnostics.Rsm;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Web
{
    /// <summary>Really Simple Monitoring endpoint.</summary>
    /// <remarks>This class grabs data to be pushed through the monitoring endpoint.</remarks>
    public class RsmHttpHandler : IHttpHandler
    {
        readonly CloudLogger _logger = (CloudLogger)GlobalSetup.Container.Resolve<Storage.Shared.Logging.ILog>();
        readonly IQueueStorageProvider _queues = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/xml";

            var query = HttpContext.Current.Request.Url.Query;
            if (!query.Contains(CloudEnvironment.GetConfigurationSetting("MonitoringApiKey").Value))
            {
                context.Response.StatusCode = 403; // access forbidden
                context.Response.Write("You do not have access to the monitoring endpoint.");
                return;
            }

            try
            {
                var doc = new RsmReport
                    {
                        Messages = ListLogMessages().ToList(),
                        Indicators = ListQueueIndicators()
                            .Concat(ListQueueQuarantineIndicators())
                            .ToList()
                    };

                context.Response.Write(doc.ToString());
            }
            catch (Exception ex)
            {
                // helper to facilitate troubleshooting the endpoint if needed
                context.Response.Write(ex.ToString());
            }
        }

        private IEnumerable<MonitoringMessageReport> ListLogMessages()
        {
            return _logger.GetLogsOfLevelOrHigher(Storage.Shared.Logging.LogLevel.Warn)
                .Take(20)
                .Select(entry => new MonitoringMessageReport
                    {
                        Id = entry.DateTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-ffff"),
                        Updated = entry.DateTimeUtc,
                        Title = entry.Message,
                        Summary = entry.Error,
                        Tags = RsmReport.GetTags("log", entry.Level.ToLower())
                    });
        }

        private IEnumerable<MonitoringIndicatorReport> ListQueueIndicators()
        {
            var now = DateTime.UtcNow;

            return _queues.List(null)
                .SelectMany(name => new[]
                    {
                        new MonitoringIndicatorReport
                            {
                                Name = string.Format("/queues/messages/{0}/count", name),
                                Value = _queues.GetApproximateCount(name).ToString(),
                                Updated = now
                            },
                        new MonitoringIndicatorReport
                            {
                                Name = string.Format("/queues/messages/{0}/latency", name),
                                Value = _queues.GetApproximateLatency(name).Convert(latency => latency.ToString(), "none"),
                                Updated = now
                            }
                    });
        }

        private IEnumerable<MonitoringIndicatorReport> ListQueueQuarantineIndicators()
        {
            var now = DateTime.UtcNow;

            return new[]
                {
                    new MonitoringIndicatorReport
                        {
                            Name = "/queues/quarantine/count",
                            Value = _queues.ListPersisted(Workloads.FailingMessagesStoreName).Count().ToString(),
                            Updated = now
                        }
                };
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}