using System;
using Lokad.Cloud.Storage;
using Lokad.Cloud.ServiceFabric.Runtime;

namespace Lokad.Cloud.Application
{
    public class CloudApplicationInspector
    {
        public const string ContainerName = "lokad-cloud-assemblies";
        public const string ApplicationDefinitionBlobName = "definition";

        private readonly IBlobStorageProvider _blobs;

        public CloudApplicationInspector(IBlobStorageProvider blobs)
        {
            _blobs = blobs;
        }

        public Maybe<CloudApplicationDefinition> Inspect()
        {
            var definitionBlob = _blobs.GetBlob<CloudApplicationDefinition>(ContainerName, ApplicationDefinitionBlobName);
            Maybe<byte[]> packageBlob;
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

            var serviceInspector = new ServiceInspector(packageData);

            return new CloudApplicationDefinition
                {
                    PackageETag = etag,
                    Timestamp = DateTimeOffset.UtcNow,
                    Assemblies = package.Assemblies.ToArray(),
                    QueueServices = serviceInspector.QueueServices.ToArray(),
                    ScheduledServices = serviceInspector.ScheduledServices.ToArray(),
                    CloudServices = serviceInspector.CloudServices.ToArray()
                };
        }
    }
}
