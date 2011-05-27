#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Runtime
{
    public static class CloudStorageExtensions
    {
        public static RuntimeProviders BuildRuntimeProviders(this CloudStorage.CloudStorageBuilder builder)
        {
            // override formatter
            var providers = builder
                .WithDataSerializer(new CloudFormatter()) // (ruegg, 2011-05-26) do NOT change formatter here
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
