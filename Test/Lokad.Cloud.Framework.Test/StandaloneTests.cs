#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using NUnit.Framework;

namespace Lokad.Cloud.Test
{
    [TestFixture]
    public class StandaloneTests
    {
        [Test]
        public void CanCreatePopulatedProvidersFromSettings()
        {
            var settings = RoleConfigurationSettings.LoadFromRoleEnvironment();
            CloudInfrastructureProviders providers;
            if (settings.HasValue)
            {
                providers = Standalone.CreateProviders(settings.Value);
                VerifyBlobProviderWorks(providers);
            }
            else
            {
                providers = Standalone.CreateProviders(
                    new RoleConfigurationSettings { DataConnectionString = "UseDevelopmentStorage=true" });
                VerifyBlobProviderWorks(providers);
            }
            Assert.IsInstanceOf(typeof(Cloud.Storage.Azure.BlobStorageProvider), providers.BlobStorage);
        }

        [Test]
        public void CanCreatePopulatedProvidersFromConnectionString()
        {
            var providers = Standalone.CreateProviders("UseDevelopmentStorage=true");
            VerifyBlobProviderWorks(providers);
            Assert.IsInstanceOf(typeof(Cloud.Storage.Azure.BlobStorageProvider), providers.BlobStorage);
        }

        [Test]
        public void CanCreatePopulatedProvidersFromAppconfig()
        {
            var providers = Standalone.CreateProvidersFromConfiguration("autofac");
            VerifyBlobProviderWorks(providers);
            Assert.IsInstanceOf(typeof(Cloud.Storage.Azure.BlobStorageProvider), providers.BlobStorage);
        }

        [Test]
        public void CanCreatePopulatedDevelopmentStorageProviders()
        {
            var providers = Standalone.CreateDevelopmentStorageProviders();
            VerifyBlobProviderWorks(providers);
            Assert.IsInstanceOf(typeof(Cloud.Storage.Azure.BlobStorageProvider), providers.BlobStorage);
        }

        [Test]
        public void CanCreatePopulatedMockStorageProviders()
        {
            var providers = Standalone.CreateMockProviders();
            VerifyBlobProviderWorks(providers);
            Assert.IsInstanceOf(typeof(Cloud.Storage.InMemory.MemoryBlobStorageProvider), providers.BlobStorage);
        }

        static void VerifyBlobProviderWorks(CloudInfrastructureProviders providers)
        {
            // no verification that the providers actually work, this is not an integration test.
            Assert.NotNull(providers);
            Assert.NotNull(providers.BlobStorage);
            Assert.NotNull(providers.QueueStorage);
            Assert.NotNull(providers.TableStorage);
        }
    }
}
