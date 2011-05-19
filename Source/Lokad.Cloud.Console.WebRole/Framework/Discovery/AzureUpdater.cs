#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Provisioning;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public sealed class AzureUpdater
    {
        private readonly AzureDiscoveryInfo _info;
        private readonly AzureProvisioning _provisioning;

        public AzureUpdater(AzureDiscoveryInfo discoveryInfo)
        {
            _info = discoveryInfo;
            _provisioning = new AzureProvisioning(
                CloudConfiguration.SubscriptionId,
                CloudConfiguration.GetManagementCertificate());
        }

        public Task UpdateInstanceCountAsync(string hostedServiceName, string slot, int instanceCount)
        {
            var hostedService = _info.LokadCloudDeployments.Single(hs => hs.ServiceName == hostedServiceName);
            var deployment = hostedService.Deployments.Single(d => d.Slot == slot);
            deployment.IsRunning = false;
            deployment.IsTransitioning = true;

            var task = _provisioning.UpdateLokadCloudWorkerCount(
                hostedServiceName,
                deployment.DeploymentName,
                instanceCount,
                CancellationToken.None);

            task.ContinueWith(t =>
                {
                    if (t.IsCompleted)
                    {
                        deployment.InstanceCount = instanceCount;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

            return task;
        }
    }
}