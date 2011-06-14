#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.InMemory;

namespace Lokad.Cloud.Mock
{
    /// <remarks></remarks>
    public sealed class MockStorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => CloudStorage.ForInMemoryStorage().BuildStorageProviders());

            builder.Register(c => new MemoryBlobStorageProvider()).As<IBlobStorageProvider>();
            builder.Register(c => new MemoryQueueStorageProvider()).As<IQueueStorageProvider>();
            builder.Register(c => new MemoryTableStorageProvider()).As<ITableStorageProvider>();

            builder.Register(c => new MemoryLogger()).As<Storage.Shared.Logging.ILog>();
            builder.Register(c => new MemoryMonitor()).As<IServiceMonitor>();
            builder.Register(c => new MemoryProvisioning()).As<IProvisioningProvider>();
        }
    }
}
