using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/provisioning/1.2"), Serializable]
    public enum DeploymentSlot
    {
        [EnumMember]
        Staging,

        [EnumMember]
        Production,
    }
}
