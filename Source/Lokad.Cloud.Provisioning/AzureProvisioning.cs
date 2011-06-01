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
using Lokad.Cloud.Provisioning.Instrumentation;

namespace Lokad.Cloud.Provisioning
{
    public class AzureProvisioning
    {
        readonly string _subscriptionId;
        readonly X509Certificate2 _certificate;
        readonly RetryPolicies _policies;

        public AzureProvisioning(string subscriptionId, X509Certificate2 certificate, ICloudProvisioningObserver observer = null)
        {
            _subscriptionId = subscriptionId;
            _certificate = certificate;
            _policies = new RetryPolicies(observer);
        }

        public Task<int> GetRoleInstanceCount(string serviceName, string roleName, DeploymentSlot deploymentSlot, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            var completionSource = new TaskCompletionSource<int>();

            DoGetDeploymentConfiguration(client, serviceName, deploymentSlot, cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value)));

            return completionSource.Task;
        }

        public Task<int> GetRoleInstanceCount(string serviceName, string roleName, string deploymentName, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            var completionSource = new TaskCompletionSource<int>();

            DoGetDeploymentConfiguration(client, serviceName, deploymentName, cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value)));

            return completionSource.Task;
        }

        public Task<int> GetCurrentInstanceCount(AzureCurrentDeployment currentDeployment, string roleName, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            var completionSource = new TaskCompletionSource<int>();

            currentDeployment.Discover(cancellationToken).ContinuePropagateWith(
                completionSource, cancellationToken,
                discoveryTask => DoGetDeploymentConfiguration(client, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, cancellationToken).ContinuePropagateWith(
                    completionSource, cancellationToken,
                    queryTask => completionSource.TrySetResult(Int32.Parse(GetInstanceCountConfigElement(queryTask.Result, roleName).Value))));

            return completionSource.Task;
        }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, DeploymentSlot deploymentSlot, int instanceCount, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            DoGetDeploymentConfiguration(client, serviceName, deploymentSlot, cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                    {
                        var config = queryTask.Result;

                        GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                        DoUpdateDeploymentConfiguration(client, serviceName, deploymentSlot, config, cancellationToken)
                            .ContinuePropagateWith(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                    });

            return completionSource.Task;
        }

        public Task UpdateRoleInstanceCount(string serviceName, string roleName, string deploymentName, int instanceCount, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            DoGetDeploymentConfiguration(client, serviceName, deploymentName, cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                    {
                        var config = queryTask.Result;

                        GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                        DoUpdateDeploymentConfiguration(client, serviceName, deploymentName, config, cancellationToken)
                            .ContinuePropagateWith(completionSource, cancellationToken, updateTask => completionSource.TrySetResult(updateTask.Result));
                    });

            return completionSource.Task;
        }

        public Task UpdateCurrentInstanceCount(AzureCurrentDeployment currentDeployment, string roleName, int instanceCount, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            var completionSource = new TaskCompletionSource<HttpStatusCode>();

            currentDeployment.Discover(cancellationToken)
                .ContinuePropagateWith(completionSource, cancellationToken, discoveryTask =>
                    DoGetDeploymentConfiguration(client, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, cancellationToken)
                        .ContinuePropagateWith(completionSource, cancellationToken, queryTask =>
                            {
                                var config = queryTask.Result;

                                GetInstanceCountConfigElement(config, roleName).Value = instanceCount.ToString();

                                DoUpdateDeploymentConfiguration(client, discoveryTask.Result.HostedServiceName, discoveryTask.Result.DeploymentName, config, cancellationToken)
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

        XAttribute GetInstanceCountConfigElement(XDocument xml, string roleName)
        {
            return xml.ServiceConfigElements("ServiceConfiguration", "Role")
                .Single(x => x.AttributeValue("name") == roleName)
                .ServiceConfigElement("Instances")
                .Attribute("count");
        }

        Task<XDocument> DoGetDeploymentConfiguration(HttpClient client, string serviceName, string deploymentName, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<XDocument>(
                string.Format("services/hostedservices/{0}/deployments/{1}", serviceName, deploymentName),
                cancellationToken, _policies.RetryOnTransientErrors,
                (xml, tcs) => tcs.TrySetResult(xml.AzureElement("Deployment").AzureConfiguration()));
        }

        Task<XDocument> DoGetDeploymentConfiguration(HttpClient client, string serviceName, DeploymentSlot deploymentSlot, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<XDocument>(
                string.Format("services/hostedservices/{0}/deploymentslots/{1}", serviceName, deploymentSlot.ToString().ToLower()),
                cancellationToken, _policies.RetryOnTransientErrors,
                (xml, tcs) => tcs.TrySetResult(xml.AzureElement("Deployment").AzureConfiguration()));
        }

        Task<HttpStatusCode> DoUpdateDeploymentConfiguration(HttpClient client, string serviceName, string deploymentName, XDocument configuration, CancellationToken cancellationToken)
        {
            return client.PostXmlAsync<HttpStatusCode>(
                string.Format("services/hostedservices/{0}/deployments/{1}/?comp=config", serviceName, deploymentName),
                new XDocument(AzureXml.Element("ChangeConfiguration", AzureXml.Configuration(configuration))),
                cancellationToken, _policies.RetryOnTransientErrors,
                (response, tcs) =>
                {
                    response.EnsureSuccessStatusCode();
                    tcs.TrySetResult(response.StatusCode);
                });
        }

        Task<HttpStatusCode> DoUpdateDeploymentConfiguration(HttpClient client, string serviceName, DeploymentSlot deploymentSlot, XDocument configuration, CancellationToken cancellationToken)
        {
            return client.PostXmlAsync<HttpStatusCode>(
                string.Format("services/hostedservices/{0}/deploymentslots/{1}/?comp=config", serviceName, deploymentSlot),
                new XDocument(AzureXml.Element("ChangeConfiguration", AzureXml.Configuration(configuration))),
                cancellationToken, _policies.RetryOnTransientErrors,
                (response, tcs) =>
                {
                    response.EnsureSuccessStatusCode();
                    tcs.TrySetResult(response.StatusCode);
                });
        }
    }
}
