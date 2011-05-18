using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/provisioning/1.2"), Serializable]
    public class HostedServiceInfo
    {
        [DataMember(IsRequired = true)]
        public string ServiceName { get; set; }

        [DataMember(IsRequired = true)]
        public string ServiceLabel { get; set; }

        [DataMember(IsRequired = false)]
        public string Description { get; set; }

        [DataMember(IsRequired = true)]
        public List<DeploymentInfo> Deployments { get; set; }
    }
}
