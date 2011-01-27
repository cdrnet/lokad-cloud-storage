#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Runtime
{
    public static class CloudStorageExtensions
    {
        public static RuntimeProviders BuildRuntimeProviders(this CloudStorage.CloudStorageBuilder builder)
        {
            var formatter = new CloudFormatter();

            var diagnosticsStorage = builder
                .WithLog(null)
                .WithDataSerializer(formatter)
                .BuildBlobStorage();

            var providers = builder
                .WithLog(new CloudLogger(diagnosticsStorage, string.Empty))
                .WithDataSerializer(formatter)
                .BuildStorageProviders();

            return new RuntimeProviders(
                providers.BlobStorage,
                providers.QueueStorage,
                providers.TableStorage,
                providers.RuntimeFinalizer,
                providers.Log);
        }
    }
}
