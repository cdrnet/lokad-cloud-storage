namespace Lokad.Cloud.Console.WebRole.Models.Shared
{
    public class NavigationModel
    {
        public string CurrentController { get; set; }
        public string ControllerAction { get; set; }

        public bool ShowDeploymentSelector { get; set; }
        public string CurrentDeploymentName { get; set; }
        public string[] DeploymentNames { get; set; }
    }
}