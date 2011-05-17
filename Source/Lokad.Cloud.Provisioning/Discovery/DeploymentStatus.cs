namespace Lokad.Cloud.Provisioning.Discovery
{
    public enum DeploymentStatus
    {
        Running,
        Suspended,
        RunningTransitioning,
        SuspendedTransitioning,
        Starting,
        Suspending,
        Deploying,
        Deleting,
    }
}