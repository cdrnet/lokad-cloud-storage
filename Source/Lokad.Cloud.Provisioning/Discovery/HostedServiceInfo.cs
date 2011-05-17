using System.Collections.Generic;

namespace Lokad.Cloud.Provisioning.Discovery
{
    public class HostedServiceInfo
    {
        public string ServiceName { get; set; }
        public string ServiceLabel { get; set; }
        public string Description { get; set; }
        public List<DeploymentInfo> Deployments { get; set; }
    }
}
