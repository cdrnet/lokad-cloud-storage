#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Console.WebRole
{
    public class CloudConfiguration
    {
        public static string Admins
        {
            get { return GetSetting("Admins"); }
        }

        public static string SubscriptionId
        {
            get { return GetSetting("SubscriptionId"); }
        }

        public static X509Certificate2 GetManagementCertificate()
        {
            var thumbprint = GetSetting("ManagementCertificateThumbprint");
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            
            store.Open(OpenFlags.ReadOnly);
            try
            {
                return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)[0];
            }
            finally
            {
                store.Close();
            }
        }

        private static string GetSetting(string name)
        {
            return RoleEnvironment.IsAvailable
                ? RoleEnvironment.GetConfigurationSettingValue(name)
                : ConfigurationManager.AppSettings[name];
        }
    }
}