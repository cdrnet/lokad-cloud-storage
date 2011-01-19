#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Ninject;
using Ninject.Modules;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public class DiscoveryNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<AzureDiscoveryFetcher>().ToSelf().InSingletonScope();
            Bind<AzureDiscoveryProvider>().ToSelf();
            Bind<AzureUpdater>().ToSelf();
            
            Bind<AzureDiscoveryInfo>()
                .ToMethod(c => c.Kernel.Get<AzureDiscoveryProvider>().GetDiscoveryInfo())
                .InRequestScope();
        }
    }
}