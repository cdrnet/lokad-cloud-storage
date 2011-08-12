#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Autofac;
using Lokad.Cloud.Management;
using Lokad.Cloud.Runtime;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;
using Lokad.Cloud.Storage.Shared.Logging;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>IoC module that registers
    /// <see cref="BlobStorageProvider"/>, <see cref="QueueStorageProvider"/> and
    /// <see cref="TableStorageProvider"/> from the <see cref="ICloudConfigurationSettings"/>.</summary>
    public sealed class StorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(StorageAccountFromSettings);
            builder.RegisterType<CloudFormatter>().As<IDataSerializer>();

            builder.Register(BlobStorageProvider);
            builder.Register(QueueStorageProvider);
            builder.Register(TableStorageProvider);

            builder.Register(RuntimeProviders);
            builder.Register(CloudStorageProviders);
            builder.Register(CloudInfrastructureProviders);

            // Storage Observer Subject
            builder.Register(StorageObserver)
                .As<ICloudStorageObserver, IObservable<ICloudStorageEvent>>()
                .ExternallyOwned().SingleInstance();
        }

        private static CloudStorageAccount StorageAccountFromSettings(IComponentContext c)
        {
            var settings = c.Resolve<ICloudConfigurationSettings>();
            CloudStorageAccount account;
            if (CloudStorageAccount.TryParse(settings.DataConnectionString, out account))
            {
                // http://blogs.msdn.com/b/windowsazurestorage/archive/2010/06/25/nagle-s-algorithm-is-not-friendly-towards-small-requests.aspx
                ServicePointManager.FindServicePoint(account.BlobEndpoint).UseNagleAlgorithm = false;
                ServicePointManager.FindServicePoint(account.TableEndpoint).UseNagleAlgorithm = false;
                ServicePointManager.FindServicePoint(account.QueueEndpoint).UseNagleAlgorithm = false;

                return account;
            }
            throw new InvalidOperationException("Failed to get valid connection string");
        }

        static RuntimeProviders RuntimeProviders(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildRuntimeProviders(c.ResolveOptional<ILog>());
        }

        static CloudInfrastructureProviders CloudInfrastructureProviders(IComponentContext c)
        {
            return new CloudInfrastructureProviders(
                c.Resolve<CloudStorageProviders>(),
                c.ResolveOptional<IProvisioningProvider>(),
                c.ResolveOptional<ILog>());
        }

        static CloudStorageProviders CloudStorageProviders(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildStorageProviders();
        }

        static ITableStorageProvider TableStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildTableStorage();
        }

        static IQueueStorageProvider QueueStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildQueueStorage();
        }

        static IBlobStorageProvider BlobStorageProvider(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.Resolve<ICloudStorageObserver>())
                .WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
                .BuildBlobStorage();
        }

        static CloudStorageInstrumentationSubject StorageObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new CloudStorageInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudStorageEvent>>>().ToArray());
        }
    }
}