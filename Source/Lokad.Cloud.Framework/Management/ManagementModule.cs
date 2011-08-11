#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lokad.Cloud.Provisioning.Instrumentation;
using Lokad.Cloud.Provisioning.Instrumentation.Events;

namespace Lokad.Cloud.Management
{
    /// <summary>
    /// IoC module for Lokad.Cloud management classes.
    /// </summary>
    public class ManagementModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // in some cases (like standalone mock storage) the RoleConfigurationSettings
            // will not be available. That's ok, since in this case Provisioning is not
            // available anyway and there's no need to make Provisioning resolveable.
            builder.Register(c => new CloudProvisioning(
                    c.Resolve<ICloudConfigurationSettings>(), 
                    c.Resolve<Storage.Shared.Logging.ILog>()))
                .As<CloudProvisioning, IProvisioningProvider>()
                .SingleInstance();

            // Provisioning Observer Subject
            builder.Register(ProvisioningObserver)
                .As<ICloudProvisioningObserver, IObservable<ICloudProvisioningEvent>>()
                .ExternallyOwned().SingleInstance();
        }

        static CloudProvisioningInstrumentationSubject ProvisioningObserver(IComponentContext c)
        {
            // will include any registered storage event observers, if there are any, as fixed subscriptions
            return new CloudProvisioningInstrumentationSubject(c.Resolve<IEnumerable<IObserver<ICloudProvisioningEvent>>>().ToArray());
        }
    }
}
