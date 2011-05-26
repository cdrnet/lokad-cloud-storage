using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public Task<int> GetRoleInstanceCount(string serviceName, string roleName, DeploymentSlot deploymentSlot, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<int>();

            Client.GetDeploymentConfiguration(channel, serviceName, deploymentSlot, cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value)));

            return completionSource.Task;
        }

        public Task<int> GetRoleInstanceCount(string serviceName, string roleName, string deploymentName, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<int>();

            Client.GetDeploymentConfiguration(channel, serviceName, deploymentName, cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value)));

            return completionSource.Task;
        }

        public Task<int> GetCurrentInstanceCount(AzureCurrentDeployment currentDeployment, string roleName, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<int>();

            currentDeployment.Discover(cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                discoveryTask => Client.GetDeploymentConfiguration(channel, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, cancellationToken).ContinuePropagateWith(
                    completionSource, cancellationToken,
                    queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value))));

            return completionSource.Task;
        }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, DeploymentSlot deploymentSlot, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            Client.GetDeploymentConfiguration(channel, serviceName, deploymentSlot, cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                    {
                        var config = queryTask.Result;

                        GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                        Client.UpdateDeploymentConfiguration(channel, serviceName, deploymentSlot, config, cancellationToken)
                            .ContinuePropagateWith(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                    });

            return completionSource.Task;
        }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, string deploymentName, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            Client.GetDeploymentConfiguration(channel, serviceName, deploymentName, cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                    {
                        var config = queryTask.Result;

                        GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                        Client.UpdateDeploymentConfiguration(channel, serviceName, deploymentName, config, cancellationToken)
                            .ContinuePropagateWith(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                    });

            return completionSource.Task;
        }

        public Task UpdateCurrentInstanceCount(AzureCurrentDeployment currentDeployment, string roleName, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = Client.CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            currentDeployment.Discover(cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, discoveryTask =>
                    Client.GetDeploymentConfiguration(channel, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, cancellationToken)
                        .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                            {
                                var config = queryTask.Result;

                                GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                                Client.UpdateDeploymentConfiguration(channel, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, config, cancellationToken)
                                    .ContinuePropagateWith(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                            }));

            return completionSource.Task;
        }

        public Task<int> GetLokadCloudWorkerCount(string serviceName, DeploymentSlot deploymentSlot, CancellationToken cancellationToken)
        {
            return GetRoleInstanceCount(serviceName, "Lokad.Cloud.WorkerRole", deploymentSlot, cancellationToken);
        }

        public Task<int> GetLokadCloudWorkerCount(string serviceName, string deploymentName, CancellationToken cancellationToken)
        {
            return GetRoleInstanceCount(serviceName, "Lokad.Cloud.WorkerRole", deploymentName, cancellationToken);
        }

        public Task<int> GetCurrentLokadCloudWorkerCount(AzureCurrentDeployment currentDeployment, CancellationToken cancellationToken)
        {
            return GetCurrentInstanceCount(currentDeployment, "Lokad.Cloud.WorkerRole", cancellationToken);
        }

        public Task UpdateLokadCloudWorkerCount(string serviceName, DeploymentSlot deploymentSlot, int instanceCount, CancellationToken cancellationToken)
        {
            return UpdateRoleInstanceCount(serviceName, "Lokad.Cloud.WorkerRole", deploymentSlot, instanceCount, cancellationToken);
        }

        public Task UpdateLokadCloudWorkerCount(string serviceName, string deploymentName, int instanceCount, CancellationToken cancellationToken)
        {
            return UpdateRoleInstanceCount(serviceName, "Lokad.Cloud.WorkerRole", deploymentName, instanceCount, cancellationToken);
        }

        public Task UpdateCurrentLokadCloudWorkerCount(AzureCurrentDeployment currentDeployment, int instanceCount, CancellationToken cancellationToken)
        {
            return UpdateCurrentInstanceCount(currentDeployment, "Lokad.Cloud.WorkerRole", instanceCount, cancellationToken);
        }

        private XAttribute GetInstanceCountConfigElement(XDocument xml, string roleName)
        {
            return xml.ServiceConfigElements("ServiceConfiguration", "Role")
                .Single(x => x.AttributeValue("name") == roleName)
                .ServiceConfigElement("Instances")
                .Attribute("count");
        }
    }
}
