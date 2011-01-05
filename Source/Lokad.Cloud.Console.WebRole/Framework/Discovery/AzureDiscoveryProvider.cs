using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public class AzureDiscoveryProvider
    {
        private readonly AzureDiscoveryFetcher _fetcher;

        public AzureDiscoveryProvider(AzureDiscoveryFetcher fetcher)
        {
            _fetcher = fetcher;
        }

        public AzureDiscoveryInfo GetDiscoveryInfo()
        {
            // Hardcoded against HttpRuntime Cache for simplicity
            return HttpRuntime.Cache.Get("lokadcloud-DiscoveryInfo") as AzureDiscoveryInfo
                ?? new AzureDiscoveryInfo
                    {
                        IsAvailable = false,
                        Timestamp = DateTimeOffset.UtcNow,
                        LokadCloudDeployments = new List<LokadCloudHostedService>()
                    };
        }

        public Task<AzureDiscoveryInfo> UpdateAsync()
        {
            var fetchTask = _fetcher.FetchAsync();

            // Insert into HttpRuntime Cache once finished
            fetchTask.ContinueWith(discoveryInfo => HttpRuntime.Cache.Insert(
                "lokadcloud-DiscoveryInfo", discoveryInfo.Result,
                null, DateTime.UtcNow.AddMinutes(60), Cache.NoSlidingExpiration),
                TaskContinuationOptions.OnlyOnRanToCompletion);
            
            return fetchTask;
        }

        public void StartAutomaticCacheUpdate(CancellationToken cancellationToken)
        {
            // Handler is reentrant. Doesn't make sense in practice but we don't prevent it technically.
            var timer = new Timer(state => UpdateAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
            cancellationToken.Register(timer.Dispose);
        }
    }
}