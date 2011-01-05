namespace Lokad.Cloud.Console.WebRole.Models.Shared
{
    public class DiscoveryModel
    {
        public bool IsAvailable { get; set; }

        public bool ShowLastDiscoveryUpdate { get; set; }
        public string LastDiscoveryUpdate { get; set; }
    }
}