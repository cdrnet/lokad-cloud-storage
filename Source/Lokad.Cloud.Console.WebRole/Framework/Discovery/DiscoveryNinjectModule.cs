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