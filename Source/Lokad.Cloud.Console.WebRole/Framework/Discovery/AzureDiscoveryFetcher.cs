#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Provisioning;
using Lokad.Cloud.Provisioning.AzureManagement;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public sealed class AzureDiscoveryFetcher
    {
        private readonly X509Certificate2 _certificate;
        private readonly string _subscriptionId;
        private readonly AzureDiscovery _discovery;

        public AzureDiscoveryFetcher()
        {
            _subscriptionId = CloudConfiguration.SubscriptionId;
            _certificate = CloudConfiguration.GetManagementCertificate();
            _discovery = new AzureDiscovery(_subscriptionId, _certificate);
        }

        public Task<AzureDiscoveryInfo> FetchAsync()
        {
            var started = DateTimeOffset.UtcNow;
            var completionSource = new TaskCompletionSource<AzureDiscoveryInfo>();
            var cancellationToken = CancellationToken.None;

            _discovery.DiscoverHostedServices(cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken, task => completionSource.TrySetResult(new AzureDiscoveryInfo
                    {
                        IsAvailable = true,
                        Timestamp = started,
                        FinishedTimestamp = DateTimeOffset.UtcNow,
                        LokadCloudDeployments = task.Result
                            .Where(h => h.Deployments.Any(d => d.Roles.Exists(r => r.RoleName == "Lokad.Cloud.WorkerRole")))
                            .Select(MapHostedService).OrderBy(h => h.ServiceName).ToList()
                    }));

            return completionSource.Task;
        }

        private static LokadCloudHostedService MapHostedService(HostedServiceInfo hostedService)
        {
            var lokadCloudDeployments = hostedService.Deployments.Select(deployment =>
                {
                    var workerRole = deployment.Roles.Single(r => r.RoleName == "Lokad.Cloud.WorkerRole");
                    var storageAccount = CloudStorageAccount.Parse(workerRole.Settings["DataConnectionString"]);
                    var accountAndKey = storageAccount.Credentials as StorageCredentialsAccountAndKey;

                    return new LokadCloudDeployment
                        {
                            DeploymentName = deployment.DeploymentName,
                            DeploymentLabel = deployment.DeploymentLabel,
                            Status = deployment.Status.ToString(),
                            Slot = deployment.Slot.ToString(),
                            InstanceCount = workerRole.ActualInstanceCount,
                            IsRunning = deployment.Status == DeploymentStatus.Running,
                            IsTransitioning = deployment.Status != DeploymentStatus.Running && deployment.Status != DeploymentStatus.Suspended,
                            StorageAccount = storageAccount,
                            StorageAccountName = storageAccount.Credentials.AccountName,
                            StorageAccountKeyPrefix = accountAndKey != null ? accountAndKey.Credentials.ExportBase64EncodedKey().Substring(0, 4) : null,
                        };
                }).ToList();

            return new LokadCloudHostedService
                {
                    ServiceName = hostedService.ServiceName,
                    ServiceLabel = hostedService.ServiceLabel,
                    Description = hostedService.Description,
                    Deployments = lokadCloudDeployments,
                    StorageAccount = lokadCloudDeployments[0].StorageAccount,
                    StorageAccountName = lokadCloudDeployments[0].StorageAccountName,
                    StorageAccountKeyPrefix = lokadCloudDeployments[0].StorageAccountKeyPrefix,
                };
        }
    }
}