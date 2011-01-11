using System.IO;
using System.Web;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Assemblies;
using Lokad.Cloud.Management;
using Lokad.Diagnostics;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class AssembliesController : TenantController
    {
        public AssembliesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudAssemblies = new CloudAssemblies(Storage.BlobStorage, NullLog.Instance);

            return View(new AssembliesModel
                {
                    Assemblies = cloudAssemblies.GetAssemblies().ToArray()
                });
        }

        [HttpPost]
        public ActionResult UploadPackage(string hostedServiceName, HttpPostedFileBase package)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudAssemblies = new CloudAssemblies(Storage.BlobStorage, NullLog.Instance);

            byte[] bytes;
            using (var reader = new BinaryReader(package.InputStream))
            {
                bytes = reader.ReadBytes(package.ContentLength);
            }

            switch ((Path.GetExtension(package.FileName) ?? string.Empty).ToLowerInvariant())
            {
                case ".dll":
                    cloudAssemblies.UploadAssemblyDll(bytes, package.FileName);
                    break;

                default:
                    cloudAssemblies.UploadAssemblyZipContainer(bytes);
                    break;
            }

            return RedirectToAction("ByHostedService");
        }
    }
}
