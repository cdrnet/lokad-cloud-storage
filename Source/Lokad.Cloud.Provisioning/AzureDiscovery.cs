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
    }
}
