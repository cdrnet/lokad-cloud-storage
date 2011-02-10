#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

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
                                    .Select(MapHostedService).OrderBy(hs => hs.ServiceName).ToList(),
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
                .Where(hs => hs.Deployments.Any(d => d.RoleInstanceList.Exists(ri => ri.RoleName == "Lokad.Cloud.WorkerRole")));
        }

        private static LokadCloudHostedService MapHostedService(HostedService hostedService)
        {
            var lokadCloudDeployments = hostedService.Deployments.Select(d =>
                {
                    var config = XElement.Parse(Base64Decode(hostedService.Deployments.First().Configuration));

                    var nn = config.Name.NamespaceName;
                    var xmlWorkerRole = config.Elements().Single(x => x.Attribute("name").Value == "Lokad.Cloud.WorkerRole");
                    var xmlConfigSettings = xmlWorkerRole.Element(XName.Get("ConfigurationSettings", nn));
                    var xmlDataConnectionString = xmlConfigSettings.Elements().Single(x => x.Attribute("name").Value == "DataConnectionString").Attribute("value").Value;

                    var storageAccount = CloudStorageAccount.Parse(xmlDataConnectionString);
                    var accountAndKey = storageAccount.Credentials as StorageCredentialsAccountAndKey;

                    return new LokadCloudDeployment
                        {
                            DeploymentName = d.Name,
                            DeploymentLabel = Base64Decode(d.Label),
                            Status = d.Status.ToString(),
                            Slot = d.DeploymentSlot.ToString(),
                            InstanceCount = d.RoleInstanceList.Count(ri => ri.RoleName == "Lokad.Cloud.WorkerRole"),
                            IsRunning = d.Status == DeploymentStatus.Running,
                            IsTransitioning =
                                d.Status != DeploymentStatus.Running && d.Status != DeploymentStatus.Suspended,
                            Configuration = config,
                            StorageAccount = storageAccount,
                            StorageAccountName = storageAccount.Credentials.AccountName,
                            StorageAccountKeyPrefix = accountAndKey != null ? accountAndKey.Credentials.ExportBase64EncodedKey().Substring(0, 4) : null,
                        };
                }).ToList();

            return new LokadCloudHostedService
                {
                    ServiceName = hostedService.ServiceName,
                    ServiceLabel = Base64Decode(hostedService.HostedServiceProperties.Label),
                    Description = hostedService.HostedServiceProperties.Description,
                    Deployments = lokadCloudDeployments,
                    Configuration = lokadCloudDeployments[0].Configuration,
                    StorageAccount = lokadCloudDeployments[0].StorageAccount,
                    StorageAccountName = lokadCloudDeployments[0].StorageAccountName,
                    StorageAccountKeyPrefix = lokadCloudDeployments[0].StorageAccountKeyPrefix,
                };
        }

        private static string Base64Decode(string value)
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}