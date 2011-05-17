using System.Collections.Generic;

namespace Lokad.Cloud.Provisioning.Discovery
{
    public class RoleInfo
    {
        public string RoleName { get; set; }
        public int ActualInstanceCount { get; set; }
        public int ConfiguredInstanceCount { get; set; }
        public Dictionary<string, string> Settings { get; set; }
    }
}
