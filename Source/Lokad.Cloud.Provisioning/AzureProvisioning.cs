#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.Provisioning.Info;

namespace Lokad.Cloud.Provisioning
{
    public class AzureProvisioning
    {
        readonly string _subscriptionId;
        readonly X509Certificate2 _certificate;

        public ProvisioningErrorHandling.RetryPolicy ShouldRetryQuery { get; set; }
        public ProvisioningErrorHandling.RetryPolicy ShouldRetryCommand { get; set; }

        public AzureProvisioning(string subscriptionId, X509Certificate2 certificate)
        {
            _subscriptionId = subscriptionId;
            _certificate = certificate;

            ShouldRetryQuery = ProvisioningErrorHandling.RetryOnTransientErrors;
            ShouldRetryCommand = ProvisioningErrorHandling.RetryOnTransientErrors;
        }

        HttpClient CreateHttpClient()
        {
            var channel = new HttpClientChannel();
            channel.ClientCertificates.Add(_certificate);

            var client = new HttpClient(string.Format("https://management.core.windows.net/{0}/", _subscriptionId))
            {
                Channel = channel
            };

            client.DefaultRequestHeaders.Add("x-ms-version", "2011-02-25");
            return client;
        }

        public Task<int> GetRoleInstanceCount(string serviceName, string roleName, DeploymentSlot deploymentSlot, CancellationToken cancellationToken)
        {
            var channel = CreateHttpClient();
            var completionSource = new TaskCompletionSource<int>();

            GetDeploymentConfiguration(channel, serviceName, deploymentSlot, cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value)));

            return completionSource.Task;
        }

        public Task<int> GetRoleInstanceCount(string serviceName, string roleName, string deploymentName, CancellationToken cancellationToken)
        {
            var channel = CreateHttpClient();
            var completionSource = new TaskCompletionSource<int>();

            GetDeploymentConfiguration(channel, serviceName, deploymentName, cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value)));

            return completionSource.Task;
        }

        public Task<int> GetCurrentInstanceCount(AzureCurrentDeployment currentDeployment, string roleName, CancellationToken cancellationToken)
        {
            var channel = CreateHttpClient();
            var completionSource = new TaskCompletionSource<int>();

            currentDeployment.Discover(cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                discoveryTask => GetDeploymentConfiguration(channel, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, cancellationToken).ContinuePropagateWith(
                    completionSource, cancellationToken,
                    queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value))));

            return completionSource.Task;
        }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, DeploymentSlot deploymentSlot, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            GetDeploymentConfiguration(channel, serviceName, deploymentSlot, cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                    {
                        var config = queryTask.Result;

                        GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                        UpdateDeploymentConfiguration(channel, serviceName, deploymentSlot, config, cancellationToken)
                            .ContinuePropagateWith(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                    });

            return completionSource.Task;
        }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, string deploymentName, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            GetDeploymentConfiguration(channel, serviceName, deploymentName, cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                    {
                        var config = queryTask.Result;

                        GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                        UpdateDeploymentConfiguration(channel, serviceName, deploymentName, config, cancellationToken)
                            .ContinuePropagateWith(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                    });

            return completionSource.Task;
        }

        public Task UpdateCurrentInstanceCount(AzureCurrentDeployment currentDeployment, string roleName, int instanceCount, CancellationToken cancellationToken)
        {
            var channel = CreateHttpClient();
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            currentDeployment.Discover(cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, discoveryTask =>
                    GetDeploymentConfiguration(channel, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, cancellationToken)
                        .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                            {
                                var config = queryTask.Result;

                                GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                                UpdateDeploymentConfiguration(channel, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, config, cancellationToken)
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

        Task<XDocument> GetDeploymentConfiguration(HttpClient client, string serviceName, string deploymentName, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<XDocument>(
                string.Format("services/hostedservices/{0}/deployments/{1}", serviceName, deploymentName),
                cancellationToken, ShouldRetryQuery,
                (xml, tcs) => tcs.TrySetResult(xml.AzureElement("Deployment").AzureConfiguration()));
        }

        Task<XDocument> GetDeploymentConfiguration(HttpClient client, string serviceName, DeploymentSlot deploymentSlot, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<XDocument>(
                string.Format("services/hostedservices/{0}/deploymentslots/{1}", serviceName, deploymentSlot.ToString().ToLower()),
                cancellationToken, ShouldRetryQuery,
                (xml, tcs) => tcs.TrySetResult(xml.AzureElement("Deployment").AzureConfiguration()));
        }

        Task<HttpStatusCode> UpdateDeploymentConfiguration(HttpClient client, string serviceName, string deploymentName, XDocument configuration, CancellationToken cancellationToken)
        {
            return client.PostXmlAsync<HttpStatusCode>(
                string.Format("services/hostedservices/{0}/deployments/{1}/?comp=config", serviceName, deploymentName),
                new XDocument(AzureXml.Element("ChangeConfiguration", AzureXml.Configuration(configuration))),
                cancellationToken, ShouldRetryCommand,
                (response, tcs) =>
                {
                    response.EnsureSuccessStatusCode();
                    tcs.TrySetResult(response.StatusCode);
                });
        }

        Task<HttpStatusCode> UpdateDeploymentConfiguration(HttpClient client, string serviceName, DeploymentSlot deploymentSlot, XDocument configuration, CancellationToken cancellationToken)
        {
            return client.PostXmlAsync<HttpStatusCode>(
                string.Format("services/hostedservices/{0}/deploymentslots/{1}/?comp=config", serviceName, deploymentSlot),
                new XDocument(AzureXml.Element("ChangeConfiguration", AzureXml.Configuration(configuration))),
                cancellationToken, ShouldRetryCommand,
                (response, tcs) =>
                {
                    response.EnsureSuccessStatusCode();
                    tcs.TrySetResult(response.StatusCode);
                });
        }
    }
}
