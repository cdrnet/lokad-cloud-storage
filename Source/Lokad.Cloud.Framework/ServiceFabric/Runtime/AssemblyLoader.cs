#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

using Lokad.Cloud.Application;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
    /// <remarks>
    /// Since the assemblies are loaded in the current <c>AppDomain</c>, this class
    /// should be a natural candidate for a singleton design pattern. Yet, keeping
    /// it as a plain class facilitates the IoC instantiation.
    /// </remarks>
    public class AssemblyLoader
    {
        /// <summary>Name of the container used to store the assembly package.</summary>
        public const string ContainerName = "lokad-cloud-assemblies";

        /// <summary>Name of the blob used to store the assembly package.</summary>
        public const string PackageBlobName = "default";

        /// <summary>Name of the blob used to store the optional dependency injection configuration.</summary>
        public const string ConfigurationBlobName = "config";

        /// <summary>Frequency for checking for update concerning the assembly package.</summary>
        public static TimeSpan UpdateCheckFrequency
        {
            get { return TimeSpan.FromMinutes(1); }
        }

        readonly IBlobStorageProvider _provider;

        /// <summary>Etag of the assembly package. This property is set when
        /// assemblies are loaded. It can be used to monitor the availability of
        /// a new package.</summary>
        string _lastPackageEtag;

        string _lastConfigurationEtag;

        DateTimeOffset _lastPackageCheck;

        /// <summary>Build a new package loader.</summary>
        public AssemblyLoader(RuntimeProviders runtimeProviders)
        {
            _provider = runtimeProviders.BlobStorage;
        }

        /// <summary>Loads the assembly package.</summary>
        /// <remarks>This method is expected to be called only once. Call <see cref="CheckUpdate"/>
        /// afterward.</remarks>
        public void LoadPackage()
        {
            var buffer = _provider.GetBlob<byte[]>(ContainerName, PackageBlobName, out _lastPackageEtag);
            _lastPackageCheck = DateTimeOffset.UtcNow;

            // if no assemblies have been loaded yet, just skip the loading
            if (!buffer.HasValue)
            {
                return;
            }

            var reader = new CloudApplicationPackageReader();
            var package = reader.ReadPackage(buffer.Value, false);

            package.LoadAssemblies();
        }

        public Maybe<byte[]> LoadConfiguration()
        {
            return _provider.GetBlob<byte[]>(ContainerName, ConfigurationBlobName, out _lastConfigurationEtag);
        }

        /// <summary>
        /// Reset the update status to the currently available version,
        /// such that <see cref="CheckUpdate"/> does not cause an update to happen.
        /// </summary>
        public void ResetUpdateStatus()
        {
            _lastPackageEtag = _provider.GetBlobEtag(ContainerName, PackageBlobName);
            _lastConfigurationEtag = _provider.GetBlobEtag(ContainerName, ConfigurationBlobName);
            _lastPackageCheck = DateTimeOffset.UtcNow;
        }

        /// <summary>Check for the availability of a new assembly package
        /// and throw a <see cref="TriggerRestartException"/> if a new package
        /// is available.</summary>
        /// <param name="delayCheck">If <c>true</c> then the actual update
        /// check if performed not more than the frequency specified by 
        /// <see cref="UpdateCheckFrequency"/>.</param>
        public void CheckUpdate(bool delayCheck)
        {
            var now = DateTimeOffset.UtcNow;

            // limiting the frequency where the actual update check is performed.
            if (delayCheck && now.Subtract(_lastPackageCheck) <= UpdateCheckFrequency)
            {
                return;
            }

            var newPackageEtag = _provider.GetBlobEtag(ContainerName, PackageBlobName);
            var newConfigurationEtag = _provider.GetBlobEtag(ContainerName, ConfigurationBlobName);

            if (!string.Equals(_lastPackageEtag, newPackageEtag))
            {
                throw new TriggerRestartException("Assemblies update has been detected.");
            }

            if (!string.Equals(_lastConfigurationEtag, newConfigurationEtag))
            {
                throw new TriggerRestartException("Configuration update has been detected.");
            }
        }
    }
}