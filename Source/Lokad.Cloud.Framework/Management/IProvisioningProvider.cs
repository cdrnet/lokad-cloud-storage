#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Management
{
    /// <summary>Defines an interface to auto-scale your cloud app.</summary>
    /// <remarks>The implementation relies on the Management API on Windows Azure.</remarks>
    public interface IProvisioningProvider
    {
        bool IsAvailable { get; }

        /// <summary>Defines the number of regular VM instances to get allocated
        /// for the cloud app.</summary>
        /// <param name="count"></param>
        Task SetWorkerInstanceCount(int count, CancellationToken cancellationToken);

        /// <summary>Indicates the number of VM instances currently allocated
        /// for the cloud app.</summary>
        /// <remarks>If <see cref="IsAvailable"/> is <c>false</c> this method
        /// will be returning a <c>null</c> value.</remarks>
        Task<int> GetWorkerInstanceCount(CancellationToken cancellationToken);
    }
}