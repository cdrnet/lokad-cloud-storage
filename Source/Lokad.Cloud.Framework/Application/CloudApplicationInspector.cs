#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.Storage;
using Lokad.Cloud.ServiceFabric.Runtime;

namespace Lokad.Cloud.Application
{
    public class CloudApplicationInspector
    {
        public const string ContainerName = "lokad-cloud-assemblies";
        public const string ApplicationDefinitionBlobName = "definition";

        private readonly IBlobStorageProvider _blobs;

        public CloudApplicationInspector(RuntimeProviders runtimeProviders)
        {
            _blobs = runtimeProviders.BlobStorage;
        }

        public Maybe<CloudApplicationDefinition> Inspect()
        {
            var definitionBlob = _blobs.GetBlob<CloudApplicationDefinition>(ContainerName, ApplicationDefinitionBlobName);
            Storage.Shared.Monads.Maybe<byte[]> packageBlob;
            string packageETag;

            if (definitionBlob.HasValue)
            {
                packageBlob = _blobs.GetBlobIfModified<byte[]>(AssemblyLoader.ContainerName, AssemblyLoader.PackageBlobName, definitionBlob.Value.PackageETag, out packageETag);
                if (!packageBlob.HasValue || definitionBlob.Value.PackageETag == packageETag)
                {
                    return definitionBlob.Value;
                }
            }
            else
            {
                packageBlob = _blobs.GetBlob<byte[]>(AssemblyLoader.ContainerName, AssemblyLoader.PackageBlobName, out packageETag);
            }

            if (!packageBlob.HasValue)
            {
                return Maybe<CloudApplicationDefinition>.Empty;
            }

            var definition = Analyze(packageBlob.Value, packageETag);
            _blobs.PutBlob(ContainerName, ApplicationDefinitionBlobName, definition);
            return definition;
        }

        private static CloudApplicationDefinition Analyze(byte[] packageData, string etag)
        {
            var reader = new CloudApplicationPackageReader();
            var package = reader.ReadPackage(packageData, true);
            var inspectionResult = ServiceInspector.Inspect(packageData);

            return new CloudApplicationDefinition
                {
                    PackageETag = etag,
                    Timestamp = DateTimeOffset.UtcNow,
                    Assemblies = package.Assemblies.ToArray(),
                    QueueServices = inspectionResult.QueueServices.ToArray(),
                    ScheduledServices = inspectionResult.ScheduledServices.ToArray(),
                    CloudServices = inspectionResult.CloudServices.ToArray()
                };
        }
    }
}
