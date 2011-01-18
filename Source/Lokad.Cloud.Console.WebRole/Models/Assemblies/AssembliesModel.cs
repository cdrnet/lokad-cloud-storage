using Lokad.Cloud.Application;

namespace Lokad.Cloud.Console.WebRole.Models.Assemblies
{
    public class AssembliesModel
    {
        public Maybe<CloudApplicationAssemblyInfo[]> ApplicationAssemblies { get; set; }
    }
}