#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage.Azure;
using Lokad.Cloud.Storage.InMemory;
using Microsoft.WindowsAzure;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test
{
    [TestFixture]
    public class CloudStorageTests
    {
        [Test]
        public void CanBuildStorageProvidersForDevelopmentStorage()
        {
            Verify<BlobStorageProvider>(CloudStorage.ForDevelopmentStorage().BuildStorageProviders());
        }

        [Test]
        public void CanBuildStorageProvidersForInMemoryStorage()
        {
            Verify<MemoryBlobStorageProvider>(CloudStorage.ForInMemoryStorage().BuildStorageProviders());
        }

        [Test]
        public void CanBuildStorageProvidersForAzureAccount()
        {
            Verify<BlobStorageProvider>(CloudStorage.ForAzureAccount(CloudStorageAccount.Parse("USeDevelopmentStorage=true")).BuildStorageProviders());
        }

        [Test]
        public void CanBuildStorageProvidersForAzureConnectionString()
        {
            Verify<BlobStorageProvider>(CloudStorage.ForAzureConnectionString("USeDevelopmentStorage=true").BuildStorageProviders());
        }

        [Test]
        public void CanSetCustomDataSerializer()
        {
            var serializerInstance = new CloudFormatter();
            var providersCustom = CloudStorage.ForInMemoryStorage().WithDataSerializer(serializerInstance).BuildStorageProviders();
            Assert.AreSame(serializerInstance, ((MemoryBlobStorageProvider)providersCustom.BlobStorage).DefaultSerializer);
            Assert.AreSame(serializerInstance, ((MemoryTableStorageProvider)providersCustom.TableStorage).DataSerializer);
            Assert.AreSame(serializerInstance, ((MemoryQueueStorageProvider)providersCustom.QueueStorage).DefaultSerializer);

            var providersDefault = CloudStorage.ForInMemoryStorage().BuildStorageProviders();
            Assert.AreNotSame(serializerInstance, ((MemoryBlobStorageProvider)providersDefault.BlobStorage).DefaultSerializer);
            Assert.AreNotSame(serializerInstance, ((MemoryTableStorageProvider)providersDefault.TableStorage).DataSerializer);
            Assert.AreNotSame(serializerInstance, ((MemoryQueueStorageProvider)providersDefault.QueueStorage).DefaultSerializer);
        }

        static void Verify<TBlob>(CloudStorageProviders providers)
        {
            // no verification that the providers actually work, this is not an integration test.
            Assert.NotNull(providers);
            Assert.NotNull(providers.BlobStorage);
            Assert.NotNull(providers.QueueStorage);
            Assert.NotNull(providers.TableStorage);
            Assert.IsInstanceOf(typeof(TBlob), providers.BlobStorage);
        }
    }
}
