using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    public class AzureManagementClient
    {
        public string SubscriptionId { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public ShouldRetry ShouldRetryQuery { get; set; }
        public ShouldRetry ShouldRetryCommand { get; set; }

        public AzureManagementClient(string subscriptionId, X509Certificate2 certificate)
        {
            SubscriptionId = subscriptionId;
            Certificate = certificate;

            ShouldRetryQuery = ErrorHandling.RetryOnTransientErrors;
            ShouldRetryCommand = ErrorHandling.RetryOnTransientErrors;
        }

        //public void Configure(ICloudConfigurationSettings settings)
        //{
        //    if (!string.IsNullOrEmpty(settings.SelfManagementSubscriptionId))
        //    {
        //        SubscriptionId = settings.SelfManagementSubscriptionId;
        //    }

        //    if (!string.IsNullOrEmpty(settings.SelfManagementCertificateThumbprint))
        //    {
        //        var certificate = CloudEnvironment.GetCertificate(settings.SelfManagementCertificateThumbprint);
        //        if (certificate.HasValue)
        //        {
        //            Certificate = certificate.Value;
        //        }
        //    }
        //}

        public HttpClient CreateHttpClient()
        {
            var channel = new HttpClientChannel();
            channel.ClientCertificates.Add(Certificate);

            var client = new HttpClient(string.Format("https://management.core.windows.net/{0}/", SubscriptionId))
            {
                Channel = channel
            };

            client.DefaultRequestHeaders.Add("x-ms-version", "2011-02-25");
            return client;
        }

        public Task<HostedServiceInfo> DiscoverHostedService(HttpClient client, string serviceName, CancellationToken cancellationToken)
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

        public Task<HostedServiceInfo[]> DiscoverHostedServices(HttpClient client, CancellationToken cancellationToken)
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
                        serviceNames.Select(serviceName => DiscoverHostedService(client, serviceName, cancellationToken)).ToArray(),
                        tasks =>
                            {
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

        public Task<XDocument> GetDeploymentConfiguration(HttpClient client, string serviceName, string deploymentName, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<XDocument>(
                string.Format("services/hostedservices/{0}/deployments/{1}", serviceName, deploymentName),
                cancellationToken, ShouldRetryQuery,
                (xml, tcs) => tcs.TrySetResult(xml.AzureElement("Deployment").AzureConfiguration()));
        }

        public Task<XDocument> GetDeploymentConfiguration(HttpClient client, string serviceName, DeploymentSlot deploymentSlot, CancellationToken cancellationToken)
        {
            return client.GetXmlAsync<XDocument>(
                string.Format("services/hostedservices/{0}/deploymentslots/{1}", serviceName, deploymentSlot.ToString().ToLower()),
                cancellationToken, ShouldRetryQuery,
                (xml, tcs) => tcs.TrySetResult(xml.AzureElement("Deployment").AzureConfiguration()));
        }

        public Task<HttpStatusCode> UpdateDeploymentConfiguration(HttpClient client, string serviceName, string deploymentName, XDocument configuration, CancellationToken cancellationToken)
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

        public Task<HttpStatusCode> UpdateDeploymentConfiguration(HttpClient client, string serviceName, DeploymentSlot deploymentSlot, XDocument configuration, CancellationToken cancellationToken)
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
