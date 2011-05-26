using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/provisioning/1.2"), Serializable]
    public class DeploymentReference
    {
        [DataMember(IsRequired = true)]
        public string HostedServiceName { get; set; }

        [DataMember(IsRequired = true)]
        public string DeploymentName { get; set; }

        [DataMember(IsRequired = false)]
        public string DeploymentPrivateId { get; set; }
    }
}
