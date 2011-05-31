#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Provisioning.Info;

namespace Lokad.Cloud.Provisioning
{
    public class AzureDiscovery
    {
        readonly string _subscriptionId;
        readonly X509Certificate2 _certificate;

        public ProvisioningErrorHandling.RetryPolicy ShouldRetryQuery { get; set; }

        public AzureDiscovery(string subscriptionId, X509Certificate2 certificate)
        {
            _subscriptionId = subscriptionId;
            _certificate = certificate;

            ShouldRetryQuery = ProvisioningErrorHandling.RetryOnTransientErrors;
        }

        public Task<HostedServiceInfo> DiscoverHostedService(string serviceName, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            return DoDiscoverHostedService(client, serviceName, cancellationToken);
        }

        public Task<HostedServiceInfo[]> DiscoverHostedServices(CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            return DoDiscoverHostedServices(client, cancellationToken);
        }

        public Task<DeploymentReference> DiscoverDeployment(string deploymentPrivateId, CancellationToken cancellationToken)
        {
            var client = HttpClientFactory.Create(_subscriptionId, _certificate);
            var completionSource = new TaskCompletionSource<DeploymentReference>();
            DoDiscoverDeploymentAsync(client, deploymentPrivateId, completionSource, cancellationToken);
            return completionSource.Task;
        }

        internal void DoDiscoverDeploymentAsync(HttpClient client, string deploymentPrivateId, TaskCompletionSource<DeploymentReference> completionSource, CancellationToken cancellationToken)
        {
            DoDiscoverHostedServices(client, cancellationToken).ContinuePropagateWith(completionSource, cancellationToken, task =>
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

        Task<HostedServiceInfo> DoDiscoverHostedService(HttpClient client, string serviceName, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<HostedServiceInfo>(
                string.Format("services/hostedservices/{0}?embed-detail=true", serviceName),
                cancellationToken, ShouldRetryQuery,
                (xml, tcs) =>
                {
                    var xmlService = xml.AzureElement("HostedService");
                    var xmlProperties = xmlService.AzureElement("HostedServiceProperties");

                    var info = new HostedServiceInfo
                    {
                        ServiceName = xmlService.AzureValue("ServiceName"),
                        Description = xmlProperties.AzureValue("Description"),
                        ServiceLabel = xmlProperties.AzureEncodedValue("Label"),

                        Deployments = xmlService.AzureElements("Deployments", "Deployment").Select(d =>
                        {
                            var config = d.AzureConfiguration();
                            var instanceCountPerRole = d.AzureElements("RoleInstanceList", "RoleInstance")
                                .GroupBy(ri => ri.AzureValue("RoleName"))
                                .ToDictionary(g => g.Key, g => g.Count());

                            return new DeploymentInfo
                            {
                                DeploymentName = d.AzureValue("Name"),
                                DeploymentLabel = d.AzureEncodedValue("Label"),
                                Slot = (DeploymentSlot)Enum.Parse(typeof(DeploymentSlot), d.AzureValue("DeploymentSlot")),
                                PrivateId = d.AzureValue("PrivateID"),
                                Status = (DeploymentStatus)Enum.Parse(typeof(DeploymentStatus), d.AzureValue("Status")),
                                Roles = d.AzureElements("RoleList", "Role").Select(r =>
                                {
                                    var roleName = r.AzureValue("RoleName");
                                    var roleConfig = config.ServiceConfigElements("ServiceConfiguration", "Role")
                                        .Single(role => role.AttributeValue("name") == roleName);

                                    return new RoleInfo
                                    {
                                        RoleName = roleName,
                                        ActualInstanceCount = instanceCountPerRole[roleName],
                                        ConfiguredInstanceCount = Int32.Parse(roleConfig.ServiceConfigElement("Instances").AttributeValue("count")),
                                        Settings = roleConfig.ServiceConfigElements("ConfigurationSettings", "Setting").ToDictionary(
                                            x => x.AttributeValue("name"), x => x.AttributeValue("value"))
                                    };
                                }).ToList()
                            };
                        }).ToList()
                    };

                    tcs.TrySetResult(info);
                });
        }

        Task<HostedServiceInfo[]> DoDiscoverHostedServices(HttpClient client, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<HostedServiceInfo[]>(
                "services/hostedservices",
                cancellationToken, ShouldRetryQuery,
                (xml, tcs) =>
                {
                    var serviceNames = xml.AzureElements("HostedServices", "HostedService")
                        .Select(e => e.AzureValue("ServiceName"))
                        .ToArray();

                    Task.Factory.ContinueWhenAll(
                        serviceNames.Select(serviceName => DoDiscoverHostedService(client, serviceName, cancellationToken)).ToArray(),
                        tasks =>
                        {
                            // TODO (ruegg, 2011-05-27): Check task fault state and deal with it

                            try
                            {
                                tcs.TrySetResult(tasks.Select(t => t.Result).ToArray());
                            }
                            catch (Exception e)
                            {
                                tcs.TrySetException(e);
                            }
                        },
                        cancellationToken);
                });
        }
    }
}
