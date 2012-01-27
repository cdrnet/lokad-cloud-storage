#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Net;
using Autofac;
using Lokad.Cloud.Storage.Instrumentation;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Storage.Autofac
{
    /// <summary>
    /// IoC Module that provides storage providers linked to Windows Azure storage:
    /// - CloudStorageProviders
    /// - IBlobStorageProvider
    /// - IQueueStorageProvider
    /// - ITableStorageProvider
    /// 
    /// Expected external registrations:
    /// - Microsoft.WindowsAzure.CloudStorageAccount
    /// </summary>
    public sealed class AzureStorageModule : Module
    {
        private readonly CloudStorageAccount _account;

        public AzureStorageModule()
        {
        }

        public AzureStorageModule(CloudStorageAccount account)
        {
            _account = Patch(account);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => CloudStorage
                .ForAzureAccount(_account ?? Patch(c.Resolve<CloudStorageAccount>()))
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<IStorageObserver>())
                .BuildStorageProviders())
                .OnRelease(p => p.QueueStorage.AbandonAll());

            builder.Register(c => CloudStorage
                .ForAzureAccount(_account ?? Patch(c.Resolve<CloudStorageAccount>()))
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<IStorageObserver>())
                .BuildBlobStorage());

            builder.Register(c => CloudStorage
                .ForAzureAccount(_account ?? Patch(c.Resolve<CloudStorageAccount>()))
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<IStorageObserver>())
                .BuildQueueStorage())
                .OnRelease(p => p.AbandonAll());

            builder.Register(c => CloudStorage
                .ForAzureAccount(_account ?? Patch(c.Resolve<CloudStorageAccount>()))
                .WithDataSerializer(c.Resolve<IDataSerializer>())
                .WithObserver(c.ResolveOptional<IStorageObserver>())
                .BuildTableStorage());

            builder.Register(c => new NeutralLogStorage
            {
                BlobStorage = CloudStorage.ForAzureAccount(_account ?? Patch(c.Resolve<CloudStorageAccount>())).WithDataSerializer(new CloudFormatter()).BuildBlobStorage()
            });
        }

        private CloudStorageAccount Patch(CloudStorageAccount account)
        {
            ServicePointManager.FindServicePoint(account.BlobEndpoint).UseNagleAlgorithm = false;
            ServicePointManager.FindServicePoint(account.TableEndpoint).UseNagleAlgorithm = false;
            ServicePointManager.FindServicePoint(account.QueueEndpoint).UseNagleAlgorithm = false;
            return account;
        }
    }
}
