#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
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
            builder.Register(CloudLogger).As<Storage.Shared.Logging.ILog>().PreserveExistingDefaults();
            builder.Register(CloudLogProvider).As<Storage.Shared.Logging.ILogProvider>().PreserveExistingDefaults();

            // Cloud Monitoring
            builder.RegisterType<BlobDiagnosticsRepository>().As<ICloudDiagnosticsRepository>().PreserveExistingDefaults();
            builder.RegisterType<ServiceMonitor>().As<IServiceMonitor>();
            builder.RegisterType<DiagnosticsAcquisition>()
                .PropertiesAutowired(true)
                .InstancePerDependency();
        }

        static CloudLogger CloudLogger(IComponentContext c)
        {
            return new CloudLogger(BlobStorageForDiagnostics(c), string.Empty);
        }

        static CloudLogProvider CloudLogProvider(IComponentContext c)
        {
            return new CloudLogProvider(BlobStorageForDiagnostics(c));
        }

        static IBlobStorageProvider BlobStorageForDiagnostics(IComponentContext c)
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
