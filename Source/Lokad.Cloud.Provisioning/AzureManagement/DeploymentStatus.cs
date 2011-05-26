using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/provisioning/1.2"), Serializable]
    public enum DeploymentStatus
    {
        [EnumMember]
        Running,

        [EnumMember]
        Suspended,

        [EnumMember]
        RunningTransitioning,

        [EnumMember]
        SuspendedTransitioning,

        [EnumMember]
        Starting,

        [EnumMember]
        Suspending,

        [EnumMember]
        Deploying,

        [EnumMember]
        Deleting,
    }
}