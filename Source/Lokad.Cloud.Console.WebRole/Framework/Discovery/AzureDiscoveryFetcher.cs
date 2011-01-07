using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.Management.Azure;
using Lokad.Cloud.Management.Azure.Entities;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public sealed class AzureDiscoveryFetcher
    {
        private readonly X509Certificate2 _certificate;
        private readonly string _subscriptionId;
        private readonly ManagementClient _client;

        public AzureDiscoveryFetcher()
        {
            _subscriptionId = CloudConfiguration.SubscriptionId;
            _certificate = CloudConfiguration.GetManagementCertificate();
            _client = new ManagementClient(_certificate);
        }

        public Task<AzureDiscoveryInfo> FetchAsync()
        {
            return Task.Factory.StartNew(() =>
                {
                    var started = DateTimeOffset.UtcNow;
                    var proxy = _client.CreateChannel();
                    try
                    {
                        return new AzureDiscoveryInfo
                            {
                                LokadCloudDeployments = FetchLokadCloudHostedServices(proxy)
                                    .Select(MapHostedService).OrderBy(hs => hs.Name).ToList(),
                                IsAvailable = true,
                                Timestamp = started,
                                FinishedTimestamp = DateTimeOffset.UtcNow
                            };
                    }
                    finally
                    {
                        _client.CloseChannel(proxy);
                    }
                });
        }

        private IEnumerable<HostedService> FetchLokadCloudHostedServices(IAzureServiceManagement proxy)
        {
            return proxy
                .ListHostedServices(_subscriptionId)
                .AsParallel()
                .Select(service => proxy.GetHostedServiceWithDetails(_subscriptionId, service.ServiceName, true))
                .Where(hs => hs.Deployments.Exists(d => d.RoleInstanceList.Exists(ri => ri.RoleName == "Lokad.Cloud.WorkerRole")));
        }

        private static LokadCloudHostedService MapHostedService(HostedService hostedService)
        {
            var config = XElement.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(hostedService.Deployments.First().Configuration)));

            var nn = config.Name.NamespaceName;
            var xmlWorkerRole = config.Elements().Single(x => x.Attribute("name").Value == "Lokad.Cloud.WorkerRole");
            var xmlConfigSettings = xmlWorkerRole.Element(XName.Get("ConfigurationSettings", nn));
            var xmlDataConnectionString = xmlConfigSettings.Elements().Single(x => x.Attribute("name").Value == "DataConnectionString").Attribute("value").Value;

            var storageAccount = CloudStorageAccount.Parse(xmlDataConnectionString);
            var accountAndKey = storageAccount.Credentials as StorageCredentialsAccountAndKey;

            // TODO: Find out the correct matching DataSerializer in some way or another
            var serializer = new CloudFormatter();

            return new LokadCloudHostedService
                {
                    Name = hostedService.ServiceName,
                    Label = Encoding.UTF8.GetString(Convert.FromBase64String(hostedService.HostedServiceProperties.Label)),
                    Description = hostedService.HostedServiceProperties.Description,
                    Deployments = hostedService.Deployments.Select(d => new LokadCloudDeployment
                        {
                            Label = Encoding.UTF8.GetString(Convert.FromBase64String(d.Label)),
                            Status = d.Status.ToString(),
                            Slot = d.DeploymentSlot.ToString(),
                            InstanceCount = d.RoleInstanceList.Count(ri => ri.RoleName == "Lokad.Cloud.WorkerRole"),
                            IsRunning = d.Status == DeploymentStatus.Running,
                            IsTransitioning = d.Status != DeploymentStatus.Running && d.Status != DeploymentStatus.Suspended,
                        }).ToList(),
                    Configuration = config,
                    StorageAccount = storageAccount,
                    StorageAccountName = storageAccount.Credentials.AccountName,
                    StorageAccountKeyPrefix = accountAndKey != null ? accountAndKey.Credentials.ExportBase64EncodedKey().Substring(0,4) : null,
                    DataSerializer = serializer
                };
        }
    }
}