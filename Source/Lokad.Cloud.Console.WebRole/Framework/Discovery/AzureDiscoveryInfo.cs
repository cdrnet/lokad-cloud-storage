#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Console.WebRole.Framework.Discovery
{
    public class AzureDiscoveryInfo
    {
        public bool IsAvailable { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset FinishedTimestamp { get; set; }
        public List<LokadCloudHostedService> LokadCloudDeployments { get; set; }
    }
}