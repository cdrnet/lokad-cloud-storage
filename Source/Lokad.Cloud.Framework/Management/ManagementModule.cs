#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Lokad.Cloud.Management.Api10;

namespace Lokad.Cloud.Management
{
    /// <summary>
    /// IoC module for Lokad.Cloud management classes.
    /// </summary>
    public class ManagementModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CloudConfiguration>().As<ICloudConfigurationApi>().InstancePerDependency();
            builder.RegisterType<CloudAssemblies>().As<ICloudAssembliesApi>().InstancePerDependency();
            builder.RegisterType<CloudServices>().As<ICloudServicesApi>().InstancePerDependency();
            builder.RegisterType<CloudServiceScheduling>().As<ICloudServiceSchedulingApi>().InstancePerDependency();
            builder.RegisterType<CloudStatistics>().As<ICloudStatisticsApi>().InstancePerDependency();

            // in some cases (like standalone mock storage) the RoleConfigurationSettings
            // will not be available. That's ok, since in this case Provisioning is not
            // available anyway and there's no need to make Provisioning resolveable.
            builder.Register(c => new CloudProvisioning(
                    c.Resolve<ICloudConfigurationSettings>(), 
                    c.Resolve<Storage.Shared.Logging.ILog>()))
                .As<CloudProvisioning, IProvisioningProvider>()
                .SingleInstance();
        }
    }
}
