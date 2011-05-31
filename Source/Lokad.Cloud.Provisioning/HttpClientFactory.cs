#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Lokad.Cloud.Provisioning
{
    internal static class HttpClientFactory
    {
        public static HttpClient Create(string subscriptionId, X509Certificate2 certificate)
        {
            var channel = new HttpClientChannel();
            channel.ClientCertificates.Add(certificate);

            var client = new HttpClient(string.Format("https://management.core.windows.net/{0}/", subscriptionId))
            {
                Channel = channel
            };

            client.DefaultRequestHeaders.Add("x-ms-version", "2011-02-25");
            return client;
        }
    }
}
