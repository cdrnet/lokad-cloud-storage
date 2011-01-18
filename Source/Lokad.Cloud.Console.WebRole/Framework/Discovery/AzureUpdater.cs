using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.Management.Azure;
using Lokad.Cloud.Management.Azure.InputParameters;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public sealed class AzureUpdater
    {
        private readonly X509Certificate2 _certificate;
        private readonly string _subscriptionId;
        private readonly ManagementClient _client;
        private readonly AzureDiscoveryInfo _info;

        public AzureUpdater(AzureDiscoveryInfo discoveryInfo)
        {
            _info = discoveryInfo;
            _subscriptionId = CloudConfiguration.SubscriptionId;
            _certificate = CloudConfiguration.GetManagementCertificate();
            _client = new ManagementClient(_certificate);
        }

        public Task UpdateInstanceCountAsync(string hostedServiceName, string slot, int instanceCount)
        {
            var hostedService = _info.LokadCloudDeployments.Single(hs => hs.ServiceName == hostedServiceName);
            var deployment = hostedService.Deployments.Single(d => d.Slot == slot);

            var config = deployment.Configuration;

            var instanceCountAttribute = config
                .Descendants()
                .Single(d => d.Name.LocalName == "Role" && d.Attributes().Single(a => a.Name.LocalName == "name").Value == "Lokad.Cloud.WorkerRole")
                .Elements()
                .Single(e => e.Name.LocalName == "Instances")
                .Attributes()
                .Single(a => a.Name.LocalName == "count");

            // Note: The side effect on the cached discovery info is intended/by design
            instanceCountAttribute.Value = instanceCount.ToString();
            deployment.IsRunning = false;
            deployment.IsTransitioning = true;

            return Task.Factory.StartNew(() =>
                {
                    var proxy = _client.CreateChannel();
                    try
                    {
                        proxy.ChangeConfiguration(_subscriptionId, hostedService.ServiceName, deployment.DeploymentName, new ChangeConfigurationInput
                            {
                                Configuration = Base64Encode(config.ToString(SaveOptions.DisableFormatting))
                            });
                    }
                    finally
                    {
                        _client.CloseChannel(proxy);
                    }
                });
        }

        private static string Base64Encode(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }
    }
}