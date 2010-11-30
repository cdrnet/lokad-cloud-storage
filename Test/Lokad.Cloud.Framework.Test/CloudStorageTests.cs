#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Reflection;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Azure;
using Lokad.Cloud.Storage.InMemory;
using Microsoft.WindowsAzure;
using NUnit.Framework;

namespace Lokad.Cloud.Test
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
            Assert.AreSame(serializerInstance, ((MemoryBlobStorageProvider)providersCustom.BlobStorage).DataSerializer);
            Assert.AreSame(serializerInstance, ((MemoryTableStorageProvider)providersCustom.TableStorage).DataSerializer);
            Assert.AreSame(serializerInstance, ((MemoryQueueStorageProvider)providersCustom.QueueStorage).DataSerializer);

            var providersDefault = CloudStorage.ForInMemoryStorage().BuildStorageProviders();
            Assert.AreNotSame(serializerInstance, ((MemoryBlobStorageProvider)providersDefault.BlobStorage).DataSerializer);
            Assert.AreNotSame(serializerInstance, ((MemoryTableStorageProvider)providersDefault.TableStorage).DataSerializer);
            Assert.AreNotSame(serializerInstance, ((MemoryQueueStorageProvider)providersDefault.QueueStorage).DataSerializer);
        }

        [Test]
        public void CanSetCustomRuntimeFinalizer()
        {
            var finalizerInstance = new RuntimeFinalizer();
            var finalizerField = typeof(QueueStorageProvider).GetField("_runtimeFinalizer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(finalizerField);

            var providersCustom = CloudStorage.ForDevelopmentStorage().WithRuntimeFinalizer(finalizerInstance).BuildStorageProviders();
            Assert.AreSame(finalizerInstance, finalizerField.GetValue(providersCustom.QueueStorage));

            var providersDefault = CloudStorage.ForDevelopmentStorage().BuildStorageProviders();
            Assert.AreNotSame(finalizerInstance, finalizerField.GetValue(providersDefault.QueueStorage));
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
