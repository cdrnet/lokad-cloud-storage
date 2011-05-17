using System.Collections.Generic;

namespace Lokad.Cloud.Provisioning.Discovery
{
    public class DeploymentInfo
    {
        public string DeploymentName { get; set; }
        public string DeploymentLabel { get; set; }
        public DeploymentSlot Slot { get; set; }
        public string PrivateId { get; set; }
        public DeploymentStatus Status { get; set; }
        public List<RoleInfo> Roles { get; set; }
    }
}
