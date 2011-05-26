using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Provisioning.AzureManagement;

namespace Lokad.Cloud.Provisioning
{
    public class AzureCurrentDeployment
    {
        public AzureCurrentDeployment(string deploymentPrivateId, AzureManagementClient client)
        {
            DeploymentPrivateId = deploymentPrivateId;
            Client = client;
        }

        public AzureCurrentDeployment(string deploymentPrivateId, string subscriptionId, X509Certificate2 certificate)
        {
            DeploymentPrivateId = deploymentPrivateId;
            Client = new AzureManagementClient(subscriptionId, certificate);
        }

        public string DeploymentPrivateId { get; set;}
        public AzureManagementClient Client { get; set; }

        readonly object _currentDeploymentDiscoveryLock = new object();
        Task<DeploymentReference> _currentDeploymentDiscoveryTask;
        DeploymentReference _currentDeployment;

        public Task<DeploymentReference> Discover(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<DeploymentReference>();
            Task<DeploymentReference> previousTask;
            var discovery = new AzureDiscovery(Client);

            // If we have already succeeded, just pass on the result from the last time (shortcut)
            lock (_currentDeploymentDiscoveryLock)
            {
                if (_currentDeployment != null)
                {
                    completionSource.TrySetResult(_currentDeployment);
                    return completionSource.Task;
                }

                previousTask = _currentDeploymentDiscoveryTask;
                _currentDeploymentDiscoveryTask = completionSource.Task;
            }

            // If this is the first time this is called, create a new query and return
            if (previousTask == null)
            {
                discovery.DoDiscoverDeploymentAsync(DeploymentPrivateId, completionSource, cancellationToken);
                return completionSource.Task;
            }

            // We have already called but have not got the result yet.
            // In case there is already a task running (this is likely in our scenarios) wait for the result.
            // Retry once in case it will fail, or, more importantly, if it has already failed (the last time).
            previousTask.ContinueWith(task =>
            {
                if (task.IsFaulted || (task.IsCanceled && !cancellationToken.IsCancellationRequested))
                {
                    // Make sure the task doesn't throw at finalization
                    task.Exception.GetBaseException();

                    discovery.DoDiscoverDeploymentAsync(DeploymentPrivateId, completionSource, cancellationToken);
                    return;
                }

                if (task.IsCanceled)
                {
                    completionSource.TrySetCanceled();
                    return;
                }

                completionSource.TrySetResult(task.Result);
            }, TaskContinuationOptions.ExecuteSynchronously);

            completionSource.Task.ContinueWith(t =>
            {
                lock (_currentDeploymentDiscoveryLock)
                {
                    _currentDeployment = t.Result;
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return completionSource.Task;
        }
    }
}
