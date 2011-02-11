#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Logs;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Storage.Shared.Monads;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    using System.Collections.Generic;

    [RequireAuthorization, RequireDiscovery]
    public sealed class LogsController : TenantController
    {
        private const int InitialEntriesCount = 15;

        public LogsController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        [HttpGet]
        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);

            var entries = new CloudLogger(Providers.BlobStorage, string.Empty)
                .GetLogsOfLevelOrHigher(Storage.Shared.Logging.LogLevel.Info)
                .Take(InitialEntriesCount);

            return View(LogEntriesToModel(entries.ToArray(), InitialEntriesCount));
        }

        [HttpGet]
        public ActionResult Entries(string hostedServiceName, string threshold, int skip = 0, int count = InitialEntriesCount, string olderThanToken = null, string newerThanToken = null)
        {
            if(count < 1 || count > 100) throw new ArgumentOutOfRangeException("count", "Must be in range [1;100].");

            InitializeDeploymentTenant(hostedServiceName);

            var entries = new CloudLogger(Providers.BlobStorage, string.Empty)
                .GetLogsOfLevelOrHigher(ParseLogLevel(threshold), skip);

            if (!string.IsNullOrWhiteSpace(olderThanToken))
            {
                entries = entries.SkipWhile(entry => string.Compare(EntryToToken(entry), olderThanToken) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(newerThanToken))
            {
                entries = entries.TakeWhile(entry => string.Compare(EntryToToken(entry), newerThanToken) > 0);
            }

            entries = entries.Take(count);

            return Json(LogEntriesToModel(entries.ToArray(), count), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult HasNewerEntries(string hostedServiceName, string threshold, string newerThanToken)
        {
            InitializeDeploymentTenant(hostedServiceName);

            var entry = new CloudLogger(Providers.BlobStorage, string.Empty)
                .GetLogsOfLevelOrHigher(ParseLogLevel(threshold))
                .FirstOrEmpty();

            if (entry.HasValue && string.Compare(EntryToToken(entry.Value), newerThanToken) > 0)
            {
                return Json(new { HasMore = true, NewestToken = EntryToToken(entry.Value) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { HasMore = false }, JsonRequestBehavior.AllowGet);
        }

        private static LogsModel LogEntriesToModel(IList<LogEntry> entryList, int requestedCount)
        {
            if (entryList.Count == 0)
            {
                return new LogsModel { NewestToken = string.Empty, Groups = new LogGroup[0] };
            }

            var logsModel = new LogsModel
                {
                    NewestToken = EntryToToken(entryList[0]),
                    Groups = entryList.GroupBy(EntryToGroupKey).Select(group => new LogGroup
                        {
                            Key = group.Key,
                            Title = group.First().DateTimeUtc.ToLongDateString(),
                            NewestToken = EntryToToken(group.First()),
                            OldestToken = EntryToToken(group.Last()),
                            Entries = group.Select(entry => new LogItem
                                {
                                    Token = EntryToToken(entry),
                                    Time = entry.DateTimeUtc.ToString("HH:mm:ss"),
                                    Level = entry.Level,
                                    Message = HttpUtility.HtmlEncode(entry.Message),
                                    Error = HttpUtility.HtmlEncode(entry.Error ?? string.Empty)
                                }).ToArray()
                        }).ToArray()
                };

            if (entryList.Count == requestedCount)
            {
                logsModel.Groups.Last().OlderAvailable = true;
            }

            return logsModel;
        }

        static Storage.Shared.Logging.LogLevel ParseLogLevel(string str)
        {
            switch (str.ToLowerInvariant())
            {
                case "debug":
                    return Storage.Shared.Logging.LogLevel.Debug;
                case "info":
                    return Storage.Shared.Logging.LogLevel.Info;
                case "warn":
                    return Storage.Shared.Logging.LogLevel.Warn;
                case "error":
                    return Storage.Shared.Logging.LogLevel.Error;
                case "fatal":
                    return Storage.Shared.Logging.LogLevel.Fatal;
            }

            throw new ArgumentOutOfRangeException();
        }

        static string EntryToToken(LogEntry entry)
        {
            return entry.DateTimeUtc.ToString("yyyyMMddHHmmssffff");
        }

        static int EntryToGroupKey(LogEntry entry)
        {
            var date = entry.DateTimeUtc.Date;
            return (date.Year * 100 + date.Month) * 100 + date.Day;
        }
    }
}
