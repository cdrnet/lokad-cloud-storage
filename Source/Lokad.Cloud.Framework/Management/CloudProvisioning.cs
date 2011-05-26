#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cloud.Provisioning;
using Lokad.Cloud.Provisioning.AzureManagement;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Shared.Logging;

namespace Lokad.Cloud.Management
{
    /// <summary>Azure Management API Provider, Provisioning Provider.</summary>
    public class CloudProvisioning : IProvisioningProvider
    {
        private readonly ILog _log;

        private readonly AzureCurrentDeployment _currentDeployment;
        private readonly AzureProvisioning _provisioning;

        /// <summary>IoC constructor.</summary>
        public CloudProvisioning(ICloudConfigurationSettings settings, ILog log)
        {
            _log = log;

            // try get settings and certificate
            var currentDeploymentPrivateId = CloudEnvironment.AzureDeploymentId;
            Maybe<X509Certificate2> certificate = Maybe<X509Certificate2>.Empty;
            if (!String.IsNullOrWhiteSpace(settings.SelfManagementCertificateThumbprint))
            {
                certificate = CloudEnvironment.GetCertificate(settings.SelfManagementCertificateThumbprint);
            }

            // early evaluate management status for intrinsic fault states, to skip further processing
            if (!currentDeploymentPrivateId.HasValue || !certificate.HasValue || string.IsNullOrWhiteSpace(settings.SelfManagementSubscriptionId))
            {
                _log.DebugFormat("Provisioning: Not available because either the certificate or the subscription was not provided correctly.");
                return;
            }

            // ok
            var managementClient = new AzureManagementClient(settings.SelfManagementSubscriptionId, certificate.Value);
            _provisioning = new AzureProvisioning(managementClient);
            _currentDeployment = new AzureCurrentDeployment(currentDeploymentPrivateId.Value, managementClient);

            _currentDeployment.Discover(CancellationToken.None);
        }

        public bool IsAvailable
        {
            get { return _provisioning != null; }
        }

        /// <remarks>
        /// Logs exceptions, hence failing to handle a task fault at the calling side
        /// will not cause an unhandled exception at finalization
        /// </remarks>
        public Task<int> GetWorkerInstanceCount(CancellationToken cancellationToken)
        {
            var task = _provisioning.GetCurrentLokadCloudWorkerCount(_currentDeployment, cancellationToken);

            task.ContinueWith(t =>
                {
                    if (t.IsCompleted)
                    {
                        // TODO: Drop
                        _log.DebugFormat("Provisioning: Getting the current worker instance count succeeded.");
                    }
                    else if (t.IsFaulted)
                    {
                        if (ProvisioningErrorHandling.IsTransientError(t.Exception))
                        {
                            _log.DebugFormat(task.Exception.GetBaseException(), "Provisioning: Getting the current worker instance count failed with a transient error.");
                        }
                        else
                        {
                            _log.WarnFormat(task.Exception.GetBaseException(), "Provisioning: Getting the current worker instance count failed with a permanent error.");
                        }
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

            return task;
        }

        /// <remarks>
        /// Logs exceptions, hence failing to handle a task fault at the calling side
        /// will not cause an unhandled exception at finalization
        /// </remarks>
        public Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken)
        {
            if (count <= 0 && count > 500)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            var task = _provisioning.UpdateCurrentLokadCloudWorkerCount(_currentDeployment, count, cancellationToken);

            // TODO (ruegg, 2011-05-24): Consider to move out (not strictly a concern of provisioning)
            task.ContinueWith(t =>
                {
                    if (t.IsCompleted)
                    {
                        _log.InfoFormat("Provisioning: Updating the worker instance count to {1}.", count);
                    }
                    else if (t.IsFaulted)
                    {
                        HttpStatusCode httpStatus;
                        if (ProvisioningErrorHandling.TryGetHttpStatusCode(t.Exception, out httpStatus))
                        {
                            if (httpStatus == HttpStatusCode.Conflict)
                            {
                                _log.DebugFormat("Provisioning: Updating the worker instance count to {1} failed because another deployment update is already in progress.", count);
                            }
                            else
                            {
                                _log.DebugFormat("Provisioning: Updating the worker instance count failed with HTTP Status {0} ({1}).", httpStatus, (int)httpStatus);
                            }
                        }
                        else if (ProvisioningErrorHandling.IsTransientError(t.Exception))
                        {
                            _log.DebugFormat(task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a transient error.");
                        }
                        else
                        {
                            _log.WarnFormat(task.Exception.GetBaseException(), "Provisioning: Updating the worker instance count failed with a permanent error.");
                        }
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

            return task;
        }
    }
}