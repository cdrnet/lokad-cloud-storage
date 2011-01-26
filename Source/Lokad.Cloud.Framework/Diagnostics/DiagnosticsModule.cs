#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;
using Lokad.Cloud.Diagnostics.Persistence;
using Lokad.Cloud.Storage;
using Lokad.Quality;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Diagnostics
{
    /// <summary>
    /// Cloud Diagnostics IoC Module
    /// </summary>
    [NoCodeCoverage]
    public class DiagnosticsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(CloudLogger);
            builder.Register(CloudLogger).As<ILog>().DefaultOnly();
            builder.Register(CloudLogProvider).As<ILogProvider>().DefaultOnly();

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
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .WithDataSerializer(new CloudFormatter())
                .BuildBlobStorage();
        }
    }
}
