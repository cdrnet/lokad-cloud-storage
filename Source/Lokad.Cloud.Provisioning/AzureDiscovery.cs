using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Lokad.Cloud.Provisioning.AzureManagement;

namespace Lokad.Cloud.Provisioning
{
    public class AzureDiscovery
    {
        public AzureDiscovery(AzureManagementClient client)
        {
            Client = client;
        }

        public AzureDiscovery(string subscriptionId, X509Certificate2 certificate)
        {
            Client = new AzureManagementClient(subscriptionId, certificate);
        }

        public AzureManagementClient Client { get; set; }

        public Task<HostedServiceInfo> DiscoverHostedService(string serviceName, CancellationToken cancellationToken)
        {
            return Client.DiscoverHostedService(Client.CreateHttpClient(), serviceName, cancellationToken);
        }

        public Task<HostedServiceInfo[]> DiscoverHostedServices(CancellationToken cancellationToken)
        {
            return Client.DiscoverHostedServices(Client.CreateHttpClient(), cancellationToken);
        }

        public Task<DeploymentReference> DiscoverDeployment(string deploymentPrivateId, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<DeploymentReference>();
            DoDiscoverDeploymentAsync(deploymentPrivateId, completionSource, cancellationToken);
            return completionSource.Task;
        }

        public void DoDiscoverDeploymentAsync(string deploymentPrivateId, TaskCompletionSource<DeploymentReference> completionSource, CancellationToken cancellationToken)
        {
            // TODO (ruegg, 2011-05-27): Weird design, refactor

            DiscoverHostedServices(cancellationToken).ContinuePropagateWith(completionSource, cancellationToken, task =>
            {
                foreach (var hostedService in task.Result)
                {
                    var deployment = hostedService.Deployments.FirstOrDefault(di => di.PrivateId == deploymentPrivateId);
                    if (deployment != null)
                    {
                        completionSource.TrySetResult(new DeploymentReference
                        {
                            HostedServiceName = hostedService.ServiceName,
                            DeploymentName = deployment.DeploymentName,
                            DeploymentPrivateId = deployment.PrivateId
                        });

                        return;
                    }
                }

                completionSource.TrySetException(new KeyNotFoundException());
            });
        }
    }
}
