#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Application;
using Lokad.Cloud.Management.Api10;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.ServiceFabric.Runtime;

namespace Lokad.Cloud.Management
{
    /// <summary>Management facade for cloud assemblies.</summary>
    public class CloudAssemblies : ICloudAssembliesApi
    {
        readonly RuntimeProviders _runtimeProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudAssemblies"/> class.
        /// </summary>
        public CloudAssemblies(RuntimeProviders runtimeProviders)
        {
            _runtimeProviders = runtimeProviders;
        }

        public Maybe<CloudApplicationDefinition> GetApplicationDefinition()
        {
            var inspector = new CloudApplicationInspector(_runtimeProviders);
            return inspector.Inspect();
        }

        /// <summary>
        /// Enumerate infos of all configured cloud service assemblies.
        /// </summary>
        public List<CloudApplicationAssemblyInfo> GetAssemblies()
        {
            return GetApplicationDefinition().Convert(p => p.Assemblies.ToList(), new List<CloudApplicationAssemblyInfo>());
        }

        /// <summary>
        /// Configure a .dll assembly file as the new cloud service assembly.
        /// </summary>
        public void UploadAssemblyDll(byte[] data, string fileName)
        {
            using (var tempStream = new MemoryStream())
            {
                using (var zip = new ZipOutputStream(tempStream))
                {
                    zip.PutNextEntry(new ZipEntry(fileName));
                    zip.Write(data, 0, data.Length);
                    zip.CloseEntry();
                }

                UploadAssemblyZipContainer(tempStream.ToArray());
            }
        }

        /// <summary>
        /// Configure a zip container with one or more assemblies as the new cloud services.
        /// </summary>
        public void UploadAssemblyZipContainer(byte[] data)
        {
            _runtimeProviders.BlobStorage.PutBlob(
                AssemblyLoader.ContainerName,
                AssemblyLoader.PackageBlobName,
                data,
                true);
        }

        /// <summary>
        /// Verify whether the provided zip container is valid.
        /// </summary>
        public bool IsValidZipContainer(byte[] data)
        {
            try
            {
                using (var dataStream = new MemoryStream(data))
                using (var zipStream = new ZipInputStream(dataStream))
                {
                    ZipEntry entry;
                    while ((entry = zipStream.GetNextEntry()) != null)
                    {
                        var buffer = new byte[entry.Size];
                        zipStream.Read(buffer, 0, buffer.Length);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
