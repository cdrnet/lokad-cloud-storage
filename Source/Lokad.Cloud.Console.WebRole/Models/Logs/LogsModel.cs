using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Console.WebRole.Models.Logs
{
    public class LogsModel
    {
        public string NewestToken { get; set; }
        public string OldestToken { get; set; }
        public bool MoreAvailable { get; set; }
        public LogItem[] Entries { get; set; }
    }

    public class LogItem
    {
        public string Token { get; set; }
        public string Time { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public bool ShowDetails { get; set; }
    }
}