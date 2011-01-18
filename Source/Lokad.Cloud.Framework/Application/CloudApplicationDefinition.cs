using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Application
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class CloudApplicationDefinition
    {
        [DataMember(IsRequired = true)]
        public string PackageETag { get; set; }

        [DataMember(IsRequired = true)]
        public DateTimeOffset Timestamp { get; set; }

        [DataMember(IsRequired = true)]
        public CloudApplicationAssemblyInfo[] Assemblies { get; set; }

        [DataMember(IsRequired = true)]
        public QueueServiceDefinition[] QueueServices { get; set; }

        [DataMember(IsRequired = true)]
        public ScheduledServiceDefinition[] ScheduledServices { get; set; }

        [DataMember(IsRequired = true)]
        public CloudServiceDefinition[] CloudServices { get; set; }
    }

    public interface ICloudServiceDefinition
    {
        string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class QueueServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string MessageTypeName { get; set; }

        [DataMember(IsRequired = true)]
        public string QueueName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class ScheduledServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/application/1.1"), Serializable]
    public class CloudServiceDefinition : ICloudServiceDefinition
    {
        [DataMember(IsRequired = true)]
        public string TypeName { get; set; }
    }
}
