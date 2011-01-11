using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Logs;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class LogsController : TenantController
    {
        private const int InitialEntriesCount = 15;
        private const int MoreEntriesCount = 50;

        public LogsController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);

            var cloudLogger = new CloudLogger(Storage.BlobStorage, string.Empty);
            var entryList = cloudLogger.GetLogsOfLevelOrHigher(LogLevel.Info).Take(InitialEntriesCount).ToArray();

            return View(new LogsModel
                {
                    NewestToken = entryList.Length > 0 ? EntryToToken(entryList[0]) : string.Empty,
                    OldestToken = entryList.Length > 0 ?  EntryToToken(entryList[entryList.Length-1]) : string.Empty,
                    MoreAvailable = entryList.Length == InitialEntriesCount,
                    Entries = entryList
                });
        }

        public ActionResult EntriesAfterJson(string hostedServiceName, int skip, string oldestToken, string threshold)
        {
            InitializeDeploymentTenant(hostedServiceName);

            var cloudLogger = new CloudLogger(Storage.BlobStorage, string.Empty);
            var entries = cloudLogger.GetLogsOfLevelOrHigher(EnumUtil.Parse<LogLevel>(threshold, true), skip);
            int requestedCount = InitialEntriesCount;
            if(!string.IsNullOrEmpty(oldestToken))
            {
                requestedCount = MoreEntriesCount;
                entries = entries.SkipWhile(entry => string.Compare(EntryToToken(entry), oldestToken) >= 0);
            }
            var entryList = entries.Take(requestedCount).ToArray();

            return Json(new
                {
                    newestToken = entryList.Length > 0 ? EntryToToken(entryList[0]) : string.Empty,
                    oldestToken = entryList.Length > 0 ? EntryToToken(entryList[entryList.Length - 1]) : string.Empty,
                    moreAvailable = entryList.Length == requestedCount,
                    entries = (entryList.Select(entry => new
                        {
                            date = HttpUtility.HtmlEncode(entry.DateTimeUtc),
                            level = HttpUtility.HtmlEncode(entry.Level),
                            message = HttpUtility.HtmlEncode(entry.Message),
                            error = HttpUtility.HtmlEncode(entry.Error ?? string.Empty)
                        })).ToArray()
                }, JsonRequestBehavior.AllowGet);
        }

        string EntryToToken(LogEntry entry)
        {
            return entry.DateTimeUtc.ToString("yyyyMMddHHmmssffff");
        }
    }
}
