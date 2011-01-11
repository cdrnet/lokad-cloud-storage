namespace Lokad.Cloud.Console.WebRole.Models.Shared
{
    public class NavigationModel
    {
        public string CurrentController { get; set; }
        public string ControllerAction { get; set; }

        public bool ShowDeploymentSelector { get; set; }
        public string CurrentHostedServiceName { get; set; }
        public string[] HostedServiceNames { get; set; }
    }
}