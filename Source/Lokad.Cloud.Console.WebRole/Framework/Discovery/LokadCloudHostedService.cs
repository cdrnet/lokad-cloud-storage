using System.Collections.Generic;
using System.Xml.Linq;
using Lokad.Serialization;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public class LokadCloudHostedService
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public List<LokadCloudDeployment> Deployments { get; set; }
        public XElement Configuration { get; set; }
        public CloudStorageAccount StorageAccount { get; set; }
        public IDataSerializer DataSerializer { get; set; }
    }

    public class LokadCloudDeployment
    {
        public string Label { get; set; }
        public string Status { get; set; }
        public string Slot { get; set; }
        public int InstanceCount { get; set; }
    }
}