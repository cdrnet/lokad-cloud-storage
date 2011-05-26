namespace Lokad.Cloud.Storage.Events.Observers
{
    public interface ICloudStorageObserver
    {
        void Notify(ICloudStorageEvent @event);
    }
}