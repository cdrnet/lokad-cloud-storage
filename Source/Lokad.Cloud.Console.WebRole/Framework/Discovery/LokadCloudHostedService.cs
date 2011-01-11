using System.Collections.Generic;
using System.Xml.Linq;
using Lokad.Serialization;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public class LokadCloudHostedService
    {
        public string ServiceName { get; set; }
        public string ServiceLabel { get; set; }
        public string Description { get; set; }
        public List<LokadCloudDeployment> Deployments { get; set; }
        public XElement Configuration { get; set; }

        public CloudStorageAccount StorageAccount { get; set; }
        public string StorageAccountName { get; set; }
        public string StorageAccountKeyPrefix { get; set; }

        public IDataSerializer DataSerializer { get; set; }
    }

    public class LokadCloudDeployment
    {
        public string DeploymentName { get; set; }
        public string DeploymentLabel { get; set; }
        public string Status { get; set; }
        public string Slot { get; set; }
        public int InstanceCount { get; set; }
        public bool IsRunning { get; set; }
        public bool IsTransitioning { get; set; }

        public XElement Configuration { get; set; }
        public CloudStorageAccount StorageAccount { get; set; }
        public string StorageAccountName { get; set; }
        public string StorageAccountKeyPrefix { get; set; }
    }
}