using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Provisioning.AzureManagement;

namespace Lokad.Cloud.Provisioning
{
    public class AzureProvisioning
    {
        public AzureProvisioning(AzureManagementClient client)
        {
            Client = client;
        }

        public AzureProvisioning(string subscriptionId, X509Certificate2 certificate)
        {
            Client = new AzureManagementClient(subscriptionId, certificate);
        }

        public AzureManagementClient Client { get; set; }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, DeploymentSlot deploymentSlot, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            Client.GetDeploymentConfiguration(channel, serviceName, deploymentSlot, cancellationToken)
                .ContinueWithPropagate(completionSource, cancellationToken, queryTask =>
                    {
                        var config = queryTask.Result;
                        var instanceCountAttribute = config.ServiceConfigElements("ServiceConfiguration", "Role")
                            .Single(x => x.AttributeValue("name") == roleName)
                            .ServiceConfigElement("Instances")
                            .Attribute("count");

                        instanceCountAttribute.Value = instanceCount.ToString();

                        Client.UpdateDeploymentConfiguration(channel, serviceName, deploymentSlot, config, cancellationToken)
                            .ContinueWithPropagate(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                    });

            return completionSource.Task;
        }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, string deploymentName, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            Client.GetDeploymentConfiguration(channel, serviceName, deploymentName, cancellationToken)
                .ContinueWithPropagate(completionSource, cancellationToken, queryTask =>
                {
                    var config = queryTask.Result;
                    var instanceCountAttribute = config.ServiceConfigElements("ServiceConfiguration", "Role")
                        .Single(x => x.AttributeValue("name") == roleName)
                        .ServiceConfigElement("Instances")
                        .Attribute("count");

                    instanceCountAttribute.Value = instanceCount.ToString();

                    Client.UpdateDeploymentConfiguration(channel, serviceName, deploymentName, config, cancellationToken)
                        .ContinueWithPropagate(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                });

            return completionSource.Task;
        }
    }
}
