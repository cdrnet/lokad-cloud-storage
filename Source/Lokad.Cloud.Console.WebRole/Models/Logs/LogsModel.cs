using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Console.WebRole.Models.Logs
{
    public class LogsModel
    {
        public string NewestToken { get; set; }
        public string OldestToken { get; set; }
        public bool MoreAvailable { get; set; }
        public LogEntry[] Entries { get; set; }
    }
}