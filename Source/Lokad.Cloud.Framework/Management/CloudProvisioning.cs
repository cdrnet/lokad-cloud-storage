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
using Lokad.Cloud.Provisioning.Instrumentation;
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
        public CloudProvisioning(ICloudConfigurationSettings settings, ILog log, ICloudProvisioningObserver provisioningObserver = null)
        {
            _log = log;

            // try get settings and certificate
            if (!CloudEnvironment.IsAvailable)
            {
                _log.WarnFormat("Provisioning: RoleEnvironment not available on worker {0}.", CloudEnvironment.PartitionKey);
                return;
            }

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

            // detect dev fabric
            if (currentDeploymentPrivateId.Value.StartsWith("deployment("))
            {
                _log.DebugFormat("Provisioning: Not available in dev fabric instance '{0}'.", CloudEnvironment.AzureCurrentInstanceId.GetValue("N/A"));
                return;
            }

            // ok
            _provisioning = new AzureProvisioning(settings.SelfManagementSubscriptionId, certificate.Value, provisioningObserver);
            _currentDeployment = new AzureCurrentDeployment(currentDeploymentPrivateId.Value, settings.SelfManagementSubscriptionId, certificate.Value, provisioningObserver);

            _currentDeployment.Discover(CancellationToken.None).ContinueWith(t =>
                {
                    var baseException = t.Exception.GetBaseException();

                    if (ProvisioningErrorHandling.IsTransientError(baseException))
                    {
                        _log.DebugFormat(baseException, "Provisioning: Initial discovery failed with a transient error.");
                        return;
                    }

                    HttpStatusCode httpStatus;
                    if (ProvisioningErrorHandling.TryGetHttpStatusCode(baseException, out httpStatus))
                    {
                        switch(httpStatus)
                        {
                            case HttpStatusCode.Forbidden:
                                _log.WarnFormat(baseException, "Provisioning: Initial discovery failed with HTTP 403 Forbidden. We tried using subscription '{0}' and certificate '{1}' ({2}) {3} private key.",
                                    settings.SelfManagementSubscriptionId, certificate.Value.FriendlyName, certificate.Value.Thumbprint, certificate.Value.HasPrivateKey ? "with" : "without");
                                return;
                            default:
                                _log.WarnFormat(baseException, "Provisioning: Initial discovery failed with a permanent HTTP {0} {1} error.", (int)httpStatus, httpStatus);
                                return;
                        }
                    }

                    _log.WarnFormat(baseException, "Provisioning: Initial discovery failed with a permanent error.");
                }, TaskContinuationOptions.OnlyOnFaulted);
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

            // TODO (ruegg, 2011-05-30): Replace with system events
            task.ContinueWith(t =>
                {
                    try
                    {
                        if (t.IsFaulted)
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
                    }
                    catch (Exception)
                    {
                        // We don't really care, it's only logging that failed
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

            _log.InfoFormat("Provisioning: Updating the worker instance count to {0}.", count);

            var task = _provisioning.UpdateCurrentLokadCloudWorkerCount(_currentDeployment, count, cancellationToken);

            // TODO (ruegg, 2011-05-30): Replace with system events
            task.ContinueWith(t =>
                {
                    try
                    {
                        if (t.IsFaulted)
                        {
                            HttpStatusCode httpStatus;
                            if (ProvisioningErrorHandling.TryGetHttpStatusCode(t.Exception, out httpStatus))
                            {
                                if (httpStatus == HttpStatusCode.Conflict)
                                {
                                    _log.DebugFormat("Provisioning: Updating the worker instance count to {0} failed because another deployment update is already in progress.", count);
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
                    }
                    catch (Exception)
                    {
                        // We don't really care, it's only logging that failed
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

            return task;
        }
    }
}