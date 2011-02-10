#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;
using Lokad.Cloud.Diagnostics.Persistence;
using Lokad.Cloud.Storage;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>Cloud Diagnostics IoC Module.</summary>
    public class DiagnosticsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(CloudLogger);
            builder.Register(CloudLogger).As<Storage.Shared.Logging.ILog>().DefaultOnly();
            builder.Register(CloudLogProvider).As<Storage.Shared.Logging.ILogProvider>().DefaultOnly();

            // Cloud Monitoring
            builder.Register<BlobDiagnosticsRepository>().As<ICloudDiagnosticsRepository>().DefaultOnly();
            builder.Register<ServiceMonitor>().As<IServiceMonitor>();
            builder.Register<DiagnosticsAcquisition>()
                .OnActivating(ActivatingHandler.InjectUnsetProperties)
                .FactoryScoped();
        }

        static CloudLogger CloudLogger(IContext c)
        {
            return new CloudLogger(BlobStorageForDiagnostics(c), string.Empty);
        }

        static CloudLogProvider CloudLogProvider(IContext c)
        {
            return new CloudLogProvider(BlobStorageForDiagnostics(c));
        }

        static IBlobStorageProvider BlobStorageForDiagnostics(IContext c)
        {
            // No log is provided here (WithLog method) since the providers
            // used for logging obviously can't log themselves (cyclic dependency)

            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(new CloudFormatter())
                .BuildBlobStorage();
        }
    }
}
