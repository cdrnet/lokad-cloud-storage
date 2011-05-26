#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

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
            fetchTask.ContinueWith(discoveryInfo =>
                HttpRuntime.Cache.Insert(
                    "lokadcloud-DiscoveryInfo", discoveryInfo.Result,
                    null, DateTime.UtcNow.AddMinutes(60), Cache.NoSlidingExpiration),
                TaskContinuationOptions.OnlyOnRanToCompletion);

            fetchTask.ContinueWith(task =>
                {
                    // Ensure the Task doesn't throw at finalization
                    var exception = task.Exception.GetBaseException();

                    // TODO (ruegg, 2011-05-19): report error to the user

                }, TaskContinuationOptions.OnlyOnFaulted);
            
            return fetchTask;
        }

        public void StartAutomaticCacheUpdate(CancellationToken cancellationToken)
        {
            // Handler is reentrant. Doesn't make sense in practice but we don't prevent it technically.
            var timer = new Timer(state => UpdateAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            cancellationToken.Register(timer.Dispose);
        }
    }
}